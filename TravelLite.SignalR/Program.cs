using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();
builder.Services.AddAuthentication().AddJwtBearer(_ =>
{
    var secret = builder.Configuration.GetValue<string>("Jwt") ?? throw new Exception();
    // Configure JWT
    _.TokenValidationParameters = new()
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),

        ValidateIssuer = false,
        ValidateAudience = false
    };
    // SignalR sends access token as query parameter (except for initial connection as `Authorization: Bearer` header).
    _.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSignalR(_ => { });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors(_ => _.SetIsOriginAllowed(_ => true)
                  .AllowCredentials()
                  .AllowAnyHeader()
                  .AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<Huh>("/huh");

app.MapGet("/", (HttpContext ctx) =>
{
    return $"Huh? {ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)}?";
})
    .RequireAuthorization();

app.MapGet("/auth", () =>
{
    var secret = builder.Configuration.GetValue<string>("Jwt") ?? throw new Exception();
    var now = DateTime.UtcNow;
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ]),
        Audience = "any",
        IssuedAt = now,
        Expires = now.AddMinutes(5),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha512Signature)
    };
    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwtToken = tokenHandler.WriteToken(token);
    return jwtToken;
});

app.Run();


// Context.ConnectionAborted is preserved during whole connection lifetime.
public class Huh : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
    }

    // BUG: Many invocations of the method will result in more messages being sent.
    // Add multiclient support.
    [Authorize]
    public void Feed() => _ = FeedAsync(Clients.Caller, Context.ConnectionAborted);
    static async Task FeedAsync(IClientProxy client, CancellationToken cancellationToken)
    {
        string data;
        while (!cancellationToken.IsCancellationRequested)
        {
            data = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 100)
                .Select(s => s[new Random().Next(s.Length)]).ToArray());

            await client.SendAsync("data", JsonSerializer.Serialize(
            new
            {
                data,
                price = Math.Round(100_000 * Random.Shared.NextDouble(), 2),
                timestamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff")
            }),
            cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Disconnected: {Context.ConnectionId}");
    }
}
