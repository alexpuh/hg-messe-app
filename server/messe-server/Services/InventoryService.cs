using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class InventoryService(
    MesseAppDbContext dbContext,
    ArticlesService articlesService,
    ILogger<InventoryService> logger)
{
    /// <summary>
    /// Fügt einen gescannten Barcode zum Inventory hinzu
    /// </summary>
    /// <param name="inventoryId">ID des Inventars</param>
    /// <param name="ean">Gescannter EAN-Code</param>
    /// <returns>true wenn erfolgreich, false wenn Inventory nicht gefunden oder EAN unbekannt</returns>
    public async Task<(bool, string)> AddBarcodeAsync(int inventoryId, string ean)
    {
        // Prüfe ob Inventory existiert
        var inventory = await dbContext.Inventories
            .Include(i => i.StockItems)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        
        if (inventory == null)
        {
            logger.LogWarning("Inventory {InventoryId} nicht gefunden", inventoryId);
            return (false, $"Inventory {inventoryId} nicht gefunden");
        }

        // Suche Artikel anhand des EAN-Codes
        if (!articlesService.TryFindEan(ean, out var articleUnit))
        {
            logger.LogWarning("Artikel mit EAN {Ean} nicht gefunden", ean);
            return (false, $"Artikel mit EAN {ean} nicht gefunden");
        }

        var unitId = articleUnit!.UnitId;
        var now = DateTime.Now;

        // Suche oder erstelle StockItem
        var stockItem = inventory.StockItems.FirstOrDefault(s => s.UnitId == unitId);
        
        if (stockItem == null)
        {
            // Erstelle neues StockItem
            stockItem = new StockItem
            {
                UnitId = unitId,
                QuantityUnits = 0,
                UpdatedAt = now
            };
            inventory.StockItems.Add(stockItem); // EF Core setzt InventoryId automatisch
            
            logger.LogInformation("Neues StockItem erstellt: InventoryId={InventoryId}, UnitId={UnitId}", inventoryId, unitId);
        }

        // Erstelle BarcodeScan und füge als related entity zum StockItem hinzu
        var barcodeScan = new BarcodeScan
        {
            Ean = ean,
            ScannedAt = now
        };
        stockItem.BarcodeScans.Add(barcodeScan);

        // Aktualisiere Quantity (ein Scan = eine Einheit)
        stockItem.QuantityUnits++;
        stockItem.UpdatedAt = now;

        // Setze UpdatedAt des Inventory auf den Timestamp des StockItems
        inventory.UpdatedAt = now;

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Barcode gescannt: InventoryId={InventoryId}, EAN={Ean}, UnitId={UnitId}, Quantity={Quantity}", 
            inventoryId, ean, unitId, stockItem.QuantityUnits);

        return (true, string.Empty);
    }

    /// <summary>
    /// Erstellt ein neues Inventory
    /// </summary>
    public async Task<int> CreateInventoryAsync(int? tradeEventId = null)
    {
        var inventory = new Inventory
        {
            StartedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            TradeEventId = tradeEventId
        };

        dbContext.Inventories.Add(inventory);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Inventory erstellt: Id={Id}, TradeEventId={TradeEventId}", inventory.Id, tradeEventId);

        return inventory.Id;
    }

    /// <summary>
    /// Holt ein Inventory mit allen StockItems
    /// </summary>
    public async Task<Inventory?> GetInventoryAsync(int inventoryId)
    {
        return await dbContext.Inventories
            .Include(i => i.StockItems)
            .ThenInclude(s => s.BarcodeScans)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
    }

    /// <summary>
    /// Holt das aktuelle Inventory (mit dem neuesten UpdatedAt-Wert)
    /// </summary>
    public async Task<Inventory?> GetCurrentInventoryAsync()
    {
        return await dbContext.Inventories
            .OrderByDescending(i => i.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Holt alle StockItems für ein Inventory
    /// </summary>
    public async Task<List<StockItem>> GetStockItemsAsync(int inventoryId)
    {
        return await dbContext.StockItems
            .Where(s => s.InventoryId == inventoryId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Holt alle BarcodeScans für ein StockItem
    /// </summary>
    public async Task<List<BarcodeScan>> GetBarcodeScansAsync(int stockItemId)
    {
        return await dbContext.BarcodeScans
            .Where(b => b.StockItemId == stockItemId)
            .OrderByDescending(b => b.ScannedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Holt Inventory-Ergebnisse als DTO mit RequiredCount aus TradeEvent
    /// Gibt sowohl gescannte Items als auch nicht-gescannte Items mit RequiredCount > 0 zurück
    /// </summary>
    /// <param name="inventoryId">ID des Inventars</param>
    /// <returns>Null if inventoryId not found or collection</returns>
    public async Task<(string? tradeEventName, DtoInventoryStockItem[] items)?> GetInventoryResultsAsync(int inventoryId)
    {
        // Lade Inventory mit StockItems
        var inventory = await dbContext.Inventories
            .Include(i => i.StockItems)
            .Include(i => i.TradeEvent)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);

        if (inventory == null)
        {
            return null;
        }

        // Sammle alle UnitIds (gescannte + required)
        var scannedUnitIds = inventory.StockItems.Select(s => s.UnitId).ToHashSet();
        var allUnitIds = new HashSet<int>(scannedUnitIds);

        // Lade RequiredUnits für das TradeEvent (falls vorhanden)
        Dictionary<int, int>? requiredUnits = null;
        if (inventory.TradeEventId.HasValue)
        {
            requiredUnits = await dbContext.TradeEventRequiredUnits
                .Where(r => r.TradeEventId == inventory.TradeEventId.Value && r.RequiredCount > 0)
                .ToDictionaryAsync(r => r.UnitId, r => r.RequiredCount);
            
            // Füge alle UnitIds mit RequiredCount > 0 hinzu
            foreach (var unitId in requiredUnits.Keys)
            {
                allUnitIds.Add(unitId);
            }
        }

        if (allUnitIds.Count == 0)
        {
            return (inventory.TradeEvent?.Name, []);
        }

        // Lade ArticleUnits für alle UnitIds (gescannte + required)
        var articleUnits = await dbContext.ArticleUnits
            .Where(a => allUnitIds.Contains(a.UnitId))
            .ToDictionaryAsync(a => a.UnitId);

        // Erstelle DTOs für gescannte Items
        var results = new List<DtoInventoryStockItem>();
        
        foreach (var stockItem in inventory.StockItems)
        {
            articleUnits.TryGetValue(stockItem.UnitId, out var articleUnit);
            
            int? requiredCount = null;
            if (requiredUnits != null && requiredUnits.TryGetValue(stockItem.UnitId, out var required))
            {
                requiredCount = required;
            }

            results.Add(new DtoInventoryStockItem
            {
                Id = stockItem.Id,
                UnitId = stockItem.UnitId,
                ArticleNr = articleUnit?.ArtNr,
                ArticleDisplayName = articleUnit?.DisplayName,
                UnitWeight = articleUnit?.Weight ?? 0,
                UpdatedAt = stockItem.UpdatedAt,
                Ean = articleUnit?.EanUnit ?? string.Empty,
                Count = stockItem.QuantityUnits,
                RequiredCount = requiredCount
            });
        }

        // Füge nicht-gescannte Items mit RequiredCount > 0 hinzu
        if (requiredUnits != null)
        {
            foreach (var (unitId, requiredCount) in requiredUnits)
            {
                // Überspringe bereits gescannte Items
                if (scannedUnitIds.Contains(unitId))
                {
                    continue;
                }

                articleUnits.TryGetValue(unitId, out var articleUnit);

                results.Add(new DtoInventoryStockItem
                {
                    Id = 0, // Kein StockItem vorhanden
                    UnitId = unitId,
                    ArticleNr = articleUnit?.ArtNr,
                    ArticleDisplayName = articleUnit?.DisplayName,
                    UnitWeight = articleUnit?.Weight ?? 0,
                    UpdatedAt = null, // Noch nicht gescannt
                    Ean = articleUnit?.EanUnit ?? string.Empty,
                    Count = 0, // Noch nicht gescannt
                    RequiredCount = requiredCount
                });
            }
        }

        return (inventory.TradeEvent?.Name, results.ToArray());
    }
}
