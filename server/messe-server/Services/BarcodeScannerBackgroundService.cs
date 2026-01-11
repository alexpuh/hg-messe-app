﻿namespace Herrmann.MesseApp.Server.Services;

public class BarcodeScannerBackgroundService : BackgroundService
{
    private readonly ILogger<BarcodeScannerBackgroundService> logger;
    private readonly BarcodeScannerService scannerService;
    private readonly IServiceProvider serviceProvider;
    private readonly EventInventoriesService inventoriesService;

    public BarcodeScannerBackgroundService(
        ILogger<BarcodeScannerBackgroundService> logger,
        BarcodeScannerService scannerService,
        IServiceProvider serviceProvider,
        EventInventoriesService inventoriesService)
    {
        this.logger = logger;
        this.scannerService = scannerService;
        this.serviceProvider = serviceProvider;
        this.inventoriesService = inventoriesService;
    }

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
            // Prüfe ob aktuelles Event-Inventar existiert
            var currentInventory = inventoriesService.GetCurrentEventInventory();
            if (currentInventory == null)
            {
                logger.LogWarning("Kein aktives Event-Inventar gefunden");
                e.IsProcessed = false;
                return;
            }

            // Create a scope to get ArticlesService
            using var scope = serviceProvider.CreateScope();
            var articlesService = scope.ServiceProvider.GetRequiredService<ArticlesService>();

            // Suche Artikel anhand des EAN-Codes
            if (!articlesService.TryFindEan(e.Barcode, out var articleUnit))
            {
                logger.LogWarning("Artikel mit EAN {EAN} nicht gefunden", e.Barcode);
                e.IsProcessed = false;
                return;
            }

            // Bestimme ob Box oder Einheit gescannt wurde
            bool isBox = articleUnit!.EanBox == e.Barcode;
            
            logger.LogInformation(
                "Artikel gefunden: UnitId={UnitId}, Artikel={Article}, Type={Type}",
                articleUnit.UnitId,
                articleUnit.ArticleName,
                isBox ? "Box" : "Unit");

            // Füge zum Inventar hinzu
            var success = await inventoriesService.TryAddStockItem(articleUnit.UnitId, isBox);
            
            if (success)
            {
                logger.LogInformation("Artikel erfolgreich zum Inventar hinzugefügt");
                e.IsProcessed = true;
            }
            else
            {
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

