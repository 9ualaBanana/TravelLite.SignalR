using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR(_ => { });
builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(_ => _.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials());

app.MapHub<Huh>("/huh");

app.MapGet("/", () => { return "Huh?"; });

app.Run();


// Context.ConnectionAborted is preserved during whole connection lifetime.
public class Huh : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
    }

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
                timestamp = DateTime.UtcNow.ToString()
            }),
            cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }

    //[Authorize]
    public async Task Echo(string user, string message)
    {
        await Clients.All.SendAsync("receive", user, message);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Disconnected: {Context.ConnectionId}");
    }
}
