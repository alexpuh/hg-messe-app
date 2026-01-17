namespace Herrmann.MesseApp.Server.Services;

public class BarcodeScannerBackgroundService(
    ILogger<BarcodeScannerBackgroundService> logger,
    BarcodeScannerService scannerService,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Barcode Scanner Background Service wird gestartet");

        // Event-Handler registrieren
        scannerService.BarcodeScanned += OnBarcodeScanned;

        try
        {
            // Mit Scanner verbinden
            await Task.Run(() =>
            {
                try
                {
                    scannerService.Connect();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fehler beim Verbinden mit dem Scanner");
                    throw;
                }
            }, stoppingToken);

            // Scan-Prozess starten (blockierend, läuft in Background Task)
            await Task.Run(() =>
            {
                try
                {
                    scannerService.StartScan();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fehler im Scan-Prozess");
                }
            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Barcode Scanner Background Service wird beendet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unerwarteter Fehler im Barcode Scanner Background Service");
        }
        finally
        {
            scannerService.BarcodeScanned -= OnBarcodeScanned;
            scannerService.Disconnect();
        }
    }

    private async void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        logger.LogInformation("Barcode empfangen: {Barcode}", e.Barcode);

        try
        {
            // Create a scope to get InventoryService
            using var scope = serviceProvider.CreateScope();
            var inventoriesService = scope.ServiceProvider.GetRequiredService<InventoryService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<SignalNotificationService>();

            // Prüfe ob aktuelles Event-Inventar existiert
            var currentInventory = await inventoriesService.GetCurrentInventoryAsync();
            if (currentInventory == null)
            {
                logger.LogWarning("Kein aktives Event-Inventar gefunden");
                await notificationService.SendBarcodeError(e.Barcode, "Kein aktives Event-Inventar gefunden");
                e.IsProcessed = false;
                return;
            }
            
            // Füge zum Inventar hinzu
            var (success, erromessage) = await inventoriesService.AddBarcodeAsync(currentInventory.Id, e.Barcode);
            
            if (success)
            {
                await notificationService.SendBarcodeScanned(e.Barcode);
                logger.LogDebug("Artikel erfolgreich zum Inventar hinzugefügt");
                e.IsProcessed = true;
            }
            else
            {
                await notificationService.SendBarcodeError(e.Barcode, erromessage);
                logger.LogError("Fehler beim Hinzufügen des Artikels zum Inventar");
                e.IsProcessed = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler bei der Barcode-Verarbeitung");
            e.IsProcessed = false;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Barcode Scanner Background Service wird gestoppt");
        scannerService.Disconnect();
        await base.StopAsync(cancellationToken);
    }
}

