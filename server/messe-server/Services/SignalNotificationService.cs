using Microsoft.AspNetCore.SignalR;

namespace Herrmann.MesseApp.Server.Services;

public class SignalNotificationService(ILogger<SignalNotificationService> logger, IHubContext<NotificationHub> hubContext)
{
    public async Task SendBarcodeScanned(string ean)
    {
        logger.LogDebug("Signal BarcodeScanned: {Ean}", ean);
        await hubContext.Clients.All.SendAsync("BarcodeScanned", ean);
    }

    public async Task SendBarcodeError(string ean, string errorMessage)
    {
        logger.LogDebug("Signal BarcodeError: {Ean}", ean);
        await hubContext.Clients.All.SendAsync("BarcodeError", ean, errorMessage);
    }

    public async Task SendScannerStatusChanged(bool status)
    {
        logger.LogDebug("Signal StatusChanged: {status}", status);
        await hubContext.Clients.All.SendAsync("ScannerStatusChanged", status);
    }
}