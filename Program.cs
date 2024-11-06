using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

app.UseWebSockets();

app.MapControllers();

// WebSocket endpoint for location updates
app.Map("/ws/location", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await SendMovingAnimalLocations(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// WebSocket endpoint for pulse updates
app.Map("/ws/pulse", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await SendPulseUpdates(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

async Task SendMovingAnimalLocations(WebSocket webSocket)
{
    var rand = new Random();
    var cancellationToken = new CancellationTokenSource();

    // Initialize with a location in Zambia (Lusaka)
    double latitude = -15.3875;
    double longitude = 28.3228;

    while (webSocket.State == WebSocketState.Open)
    {
        // Simulate small movements to create gradual, animal-like movement
        latitude += (rand.NextDouble() - 0.5) * 0.001; // Adjust multiplier for movement speed
        longitude += (rand.NextDouble() - 0.5) * 0.001;

        // Ensure latitude and longitude stay within bounds
        latitude = Math.Clamp(latitude, -90, 90);
        longitude = Math.Clamp(longitude, -180, 180);

        // Create a location object
        var location = new
        {
            Latitude = latitude,
            Longitude = longitude,
            Timestamp = DateTime.UtcNow
        };

        // Serialize location to JSON
        var jsonMessage = JsonSerializer.Serialize(location);
        var messageBuffer = Encoding.UTF8.GetBytes(jsonMessage);

        // Send the message to the client
        await webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancellationToken.Token);

        // Delay before sending the next location update
        await Task.Delay(1000, cancellationToken.Token); // Sends a new location every second
    }
}



async Task SendPulseUpdates(WebSocket webSocket)
{
    var rand = new Random();
    var cancellationToken = new CancellationTokenSource();

    while (webSocket.State == WebSocketState.Open)
    {
        // Generate random pulse information
        var pulseInfo = new PulseInfo
        {
            Bpm = rand.Next(60, 120)
        };

        // Serialize pulse information to JSON
        var jsonMessage = JsonSerializer.Serialize(pulseInfo);
        var messageBuffer = Encoding.UTF8.GetBytes(jsonMessage);

        // Send the message to the client
        await webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancellationToken.Token);

        // Delay before sending the next pulse update
        await Task.Delay(2000, cancellationToken.Token); // Sends a new pulse update every 2 seconds
    }
}

public class PulseInfo
{
    public int Bpm { get; set; }
    // Add other pulse-related properties as needed
}