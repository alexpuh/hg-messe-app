using Microsoft.AspNetCore.SignalR;

namespace Herrmann.MesseApp.Server.Services;

public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    private static int _connectedClients = 0;
    
    public async Task SendMessage(string message)
    {
        logger.LogInformation("SendMessage called with: {Message}", message);
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectedClients);
        logger.LogInformation("Client connected: {ConnectionId}. Total clients: {TotalClients}", Context.ConnectionId, _connectedClients);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectedClients);
        logger.LogInformation("Client disconnected: {ConnectionId}. Total clients: {TotalClients}. Exception: {Exception}", 
            Context.ConnectionId, _connectedClients, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }
}