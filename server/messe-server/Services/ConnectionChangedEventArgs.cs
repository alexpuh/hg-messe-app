namespace Herrmann.MesseApp.Server.Services;

public class ConnectionChangedEventArgs(bool isConnected) : EventArgs
{
    public bool IsConnected { get; } = isConnected;
}