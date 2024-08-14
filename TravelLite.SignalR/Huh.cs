using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
// Context.ConnectionAborted is preserved during whole connection lifetime.
[Authorize]
public class Huh : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected: {Context.ConnectionId}");
        _ = FeedAsync(Clients.Caller, Context.ConnectionAborted);
    }

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
