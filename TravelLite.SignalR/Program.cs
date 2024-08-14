using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? throw new Exception("JWT is not configured.");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();
builder.Services.AddAuthentication().AddJwtBearer(_ =>
{
    _.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Iss,
        
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwtOptions.Key.Value,

        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero,

        ValidateAudience = true,
        RequireAudience = true,
        IgnoreTrailingSlashWhenValidatingAudience = true,
        ValidAudience = jwtOptions.Aud,

        ValidateTokenReplay = true,
        TokenReplayCache = new TokenReplayCache(),

        IncludeTokenOnFailedValidation = true
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

app.UseSwagger();
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI();

//app.UseHttpsRedirection();
// Loose CORS in case you want to implement custom client.
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
    var now = DateTime.UtcNow;
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ]),
        Issuer = jwtOptions.Iss,
        Audience = jwtOptions.Aud,
        IssuedAt = now,
        NotBefore = now,
        Expires = now.AddSeconds(jwtOptions.Exp),
        SigningCredentials = new SigningCredentials(jwtOptions.Key.Value, SecurityAlgorithms.HmacSha512Signature)
    };
    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwtToken = tokenHandler.WriteToken(token);
    return jwtToken;
});

app.Run();

class TokenReplayCache(/*IDistributedCache Cache*/) : ITokenReplayCache
{
    readonly Dictionary<string, DateTime> _ = [];

    public bool TryAdd(string securityToken, DateTime expiresOn)
        => _.TryAdd(securityToken, expiresOn);

    public bool TryFind(string securityToken)
        => _.TryGetValue(securityToken, out DateTime _);
}
