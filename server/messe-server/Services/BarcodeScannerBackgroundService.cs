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
        scannerService.ConnectionChanged += OnConnectionChanged;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
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
                    }
                }, stoppingToken);

                // Wenn Verbindung fehlgeschlagen ist, warte 15 Sekunden und versuche erneut
                if (!scannerService.IsConnected())
                {
                    logger.LogInformation("Verbindung fehlgeschlagen. Warte 15 Sekunden vor erneutem Versuch...");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

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

                // Wenn Scan-Prozess beendet wurde (z.B. durch Verbindungsverlust)
                if (!scannerService.IsConnected())
                {
                    logger.LogInformation("Verbindung verloren. Warte 15 Sekunden vor Wiederverbindung...");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            }
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
            scannerService.ConnectionChanged -= OnConnectionChanged;
            scannerService.Disconnect();
        }
    }

    private async void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        logger.LogInformation("Barcode empfangen: {Barcode}", e.Barcode);

        try
        {
            // Create a scope to get scoped services
            using var scope = serviceProvider.CreateScope();
            var scanSessionService = scope.ServiceProvider.GetRequiredService<ScanSessionService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<SignalNotificationService>();

            // Prüfe ob aktuelle scan session existiert
            var currentScanSession = await scanSessionService.GetCurrentScanSessionAsync();
            if (currentScanSession == null)
            {
                logger.LogWarning("Keine aktive Scan-Session gefunden");
                await notificationService.SendBarcodeError(e.Barcode, "Keine aktive Scan-Session gefunden");
                e.IsProcessed = false;
                return;
            }
            
            var (success, errorMessage) = await scanSessionService.AddBarcodeAsync(currentScanSession.Id, e.Barcode);
            
            if (success)
            {
                await notificationService.SendBarcodeScanned(e.Barcode);
                logger.LogDebug("Artikel erfolgreich zur Scan-Session hinzugefügt");
                e.IsProcessed = true;
            }
            else
            {
                await notificationService.SendBarcodeError(e.Barcode, errorMessage);
                logger.LogError("Fehler beim Hinzufügen des Artikels zur Scan-Session");
                e.IsProcessed = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler bei der Barcode-Verarbeitung");
            e.IsProcessed = false;
        }
    }

    private async void OnConnectionChanged(object? sender, ConnectionChangedEventArgs e)
    {
        logger.LogInformation("Scanner-Verbindungsstatus geändert: {IsConnected}", e.IsConnected);
        
        try
        {
            using var scope = serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<SignalNotificationService>();
            
            // Benachrichtige Clients über Verbindungsänderung
            await notificationService.SendScannerStatusChanged(e.IsConnected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Verarbeiten der Verbindungsänderung");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Barcode Scanner Background Service wird gestoppt");
        scannerService.Disconnect();
        await base.StopAsync(cancellationToken);
    }
}

