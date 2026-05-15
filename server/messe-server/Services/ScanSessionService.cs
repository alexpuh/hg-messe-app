using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class ScanSessionService(
    MesseAppDbContext dbContext,
    ArticlesService articlesService,
    ILogger<ScanSessionService> logger)
{
    /// <summary>
    /// Fügt einen gescannten Barcode zur Scan Session hinzu
    /// </summary>
    /// <param name="sessionId">ID der Scan Session</param>
    /// <param name="ean">Gescannter EAN-Code</param>
    /// <returns>true wenn erfolgreich, false wenn Scan Session nicht gefunden oder EAN unbekannt</returns>
    public async Task<(bool, string)> AddBarcodeAsync(int sessionId, string ean)
    {
        // Prüfe ob Scan Session existiert
        var scanSession = await dbContext.ScanSessions
            .Include(i => i.ScannedArticles)
            .FirstOrDefaultAsync(i => i.Id == sessionId);
        
        if (scanSession == null)
        {
            logger.LogWarning("ScanSession {SessionId} nicht gefunden", sessionId);
            return (false, $"Session {sessionId} nicht gefunden");
        }

        // Suche Artikel anhand des EAN-Codes
        if (!articlesService.TryFindEan(ean, out var articleUnit))
        {
            logger.LogWarning("Artikel mit EAN {Ean} nicht gefunden", ean);
            return (false, $"Artikel mit EAN {ean} nicht gefunden");
        }

        var unitId = articleUnit!.UnitId;
        var now = DateTime.Now;

        // Suche oder erstelle ScannedArticle
        var scannedArticle = scanSession.ScannedArticles.FirstOrDefault(s => s.UnitId == unitId);
        
        if (scannedArticle == null)
        {
            // Erstelle neue ScannedArticle
            scannedArticle = new ScannedArticle
            {
                UnitId = unitId,
                QuantityUnits = 0,
                UpdatedAt = now
            };
            scanSession.ScannedArticles.Add(scannedArticle); // EF Core setzt SessionId automatisch
            
            logger.LogInformation("Neues ScanedArticle erstellt: SessionId={SessionId}, UnitId={UnitId}", sessionId, unitId);
        }

        // Erstelle BarcodeScan und füge als related entity zum StockItem hinzu
        var barcodeScan = new BarcodeScan
        {
            Ean = ean,
            ScannedAt = now
        };
        scannedArticle.BarcodeScans.Add(barcodeScan);

        // Aktualisiere Quantity (ein Scan = eine Einheit)
        scannedArticle.QuantityUnits++;
        scannedArticle.UpdatedAt = now;

        // Setze UpdatedAt des Scan Session auf den Timestamp des letzten Scans
        scanSession.UpdatedAt = now;

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Barcode gescannt: SessionId={SessionId}, EAN={Ean}, UnitId={UnitId}, Quantity={Quantity}", 
            sessionId, ean, unitId, scannedArticle.QuantityUnits);

        return (true, string.Empty);
    }

    /// <summary>
    /// Erstellt ein neue Scan Session
    /// </summary>
    public async Task<int> CreateScanSessionAsync(ScanSessionType sessionType, Ort ort, int? dispatchSheetId = null)
    {
        // Service-layer invariant enforcement
        if (sessionType == ScanSessionType.ProcessDispatchList && ort == Ort.Stand)
            throw new ArgumentException("Ort 'Stand' ist für eine Beladung (ProcessDispatchList) nicht erlaubt.");
        if (sessionType == ScanSessionType.ProcessDispatchList && dispatchSheetId == null)
            throw new ArgumentException("DispatchSheetId ist erforderlich für SessionType 'ProcessDispatchList'.");
        if (sessionType == ScanSessionType.Inventory && ort == Ort.Lager && dispatchSheetId == null)
            throw new ArgumentException("DispatchSheetId ist erforderlich für Bestandsaufnahme am Lager.");
        if (sessionType == ScanSessionType.Inventory && ort == Ort.Stand && dispatchSheetId != null)
            throw new ArgumentException("DispatchSheetId darf nicht angegeben werden für Bestandsaufnahme am Stand.");

        var scanSession = new ScanSession
        {
            StartedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SessionType = sessionType,
            Ort = ort,
            DispatchSheetId = dispatchSheetId
        };

        dbContext.ScanSessions.Add(scanSession);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Scan session erstellt: Id={SessionId}, SessionType={SessionType}, Ort={Ort}, DispatchSheetId={DispatchSheetId}", scanSession.Id, sessionType, ort, dispatchSheetId);

        return scanSession.Id;
    }

    /// <summary>
    /// Holt eine ScanSession mit allen ScannedArticles
    /// </summary>
    public async Task<ScanSession?> GetScanSessionAsync(int sessionId)
    {
        return await dbContext.ScanSessions
            .Include(i => i.ScannedArticles)
            .ThenInclude(s => s.BarcodeScans)
            .FirstOrDefaultAsync(i => i.Id == sessionId);
    }

    /// <summary>
    /// Holt das aktuelle Scan Session (mit dem neuesten UpdatedAt-Wert)
    /// </summary>
    public async Task<ScanSession?> GetCurrentScanSessionAsync()
    {
        return await dbContext.ScanSessions
            .OrderByDescending(i => i.UpdatedAt)
            .FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// Holt Scan Session-Ergebnisse als DTO mit RequiredCount aus der Verladeschein
    /// Gibt sowohl gescannte Items als auch nicht-gescannte Items mit RequiredCount > 0 zurück
    /// </summary>
    /// <param name="sessionId">ID der ScanSession</param>
    /// <returns>Null if sessionId not found</returns>
    public async Task<(string? dispatchSheetName, ScanSessionType sessionType, Ort ort, DtoScanSessionArticle[] articles)?> GetScanSessionArticlesAsync(int sessionId)
    {
        // Lade Scan Session mit Articles
        var scanSession = await dbContext.ScanSessions
            .Include(i => i.ScannedArticles)
            .Include(i => i.DispatchSheet)
            .FirstOrDefaultAsync(i => i.Id == sessionId);

        if (scanSession == null)
        {
            return null;
        }

        // Sammle alle UnitIds (gescannte + required)
        var scannedUnitIds = scanSession.ScannedArticles.Select(s => s.UnitId).ToHashSet();
        var allUnitIds = new HashSet<int>(scannedUnitIds);

        Dictionary<int, int>? requiredUnits = null;
        if (scanSession.DispatchSheetId.HasValue)
        {
            requiredUnits = await dbContext.DispatchSheetRequiredUnits
                .Where(r => r.DispatchSheetId == scanSession.DispatchSheetId.Value && r.RequiredCount > 0)
                .ToDictionaryAsync(r => r.UnitId, r => r.RequiredCount);
            
            // Füge alle UnitIds mit RequiredCount > 0 hinzu
            foreach (var unitId in requiredUnits.Keys)
            {
                allUnitIds.Add(unitId);
            }
        }

        if (allUnitIds.Count == 0)
        {
            return (scanSession.DispatchSheet?.Name, scanSession.SessionType, scanSession.Ort, []);
        }

        // Lade ArticleUnits für alle UnitIds (gescannte + required)
        var articleUnits = await dbContext.ArticleUnits
            .Where(a => allUnitIds.Contains(a.UnitId))
            .ToDictionaryAsync(a => a.UnitId);

        // Erstelle DTOs für gescannte Items
        var results = new List<DtoScanSessionArticle>();
        
        foreach (var stockItem in scanSession.ScannedArticles)
        {
            if (!articleUnits.TryGetValue(stockItem.UnitId, out var articleUnit))
            {
                throw new ApplicationException($"UnitId {stockItem.UnitId} nicht gefunden"); //TODO: Review
            }
            
            int? requiredCount = null;
            if (requiredUnits != null && requiredUnits.TryGetValue(stockItem.UnitId, out var required))
            {
                requiredCount = required;
            }

            results.Add(new DtoScanSessionArticle
            {
                Id = stockItem.Id,
                UnitId = stockItem.UnitId,
                ArticleNr = articleUnit!.ArtNr,
                ArticleDisplayName = articleUnit.DisplayName,
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

                if (!articleUnits.TryGetValue(unitId, out var articleUnit))
                {
                    throw new ApplicationException($"UnitId {unitId} nicht gefunden"); //TODO: Review
                }

                results.Add(new DtoScanSessionArticle
                {
                    Id = 0, // Kein StockItem vorhanden
                    UnitId = unitId,
                    ArticleNr = articleUnit!.ArtNr,
                    ArticleDisplayName = articleUnit.DisplayName,
                    UnitWeight = articleUnit.Weight,
                    UpdatedAt = null, // Noch nicht gescannt
                    Ean = articleUnit.EanUnit ?? string.Empty,
                    Count = 0, // Noch nicht gescannt
                    RequiredCount = requiredCount
                });
            }
        }

        return (scanSession.DispatchSheet?.Name, scanSession.SessionType, scanSession.Ort, results.ToArray());
    }

    /// <summary>
    /// Gibt alle Scan Sessions zurück, sortiert nach UpdatedAt absteigend
    /// </summary>
    public async Task<ScanSession[]> GetAllScanSessionsAsync()
    {
        return await dbContext.ScanSessions
            .OrderByDescending(s => s.UpdatedAt)
            .ToArrayAsync();
    }

    /// <summary>
    /// Kombiniert zwei Scan Sessions (Stand + Lager) zu einer gemeinsamen Artikelübersicht
    /// </summary>
    public async Task<(ScanSession standSession, ScanSession lagerSession, DtoCombinedArticle[] articles)?> GetCombinedArticlesAsync(int standSessionId, int lagerSessionId)
    {
        var standSession = await dbContext.ScanSessions
            .Include(s => s.ScannedArticles)
            .FirstOrDefaultAsync(s => s.Id == standSessionId);

        if (standSession == null || standSession.Ort != Ort.Stand)
            return null;

        var lagerSession = await dbContext.ScanSessions
            .Include(s => s.ScannedArticles)
            .FirstOrDefaultAsync(s => s.Id == lagerSessionId);

        if (lagerSession == null || lagerSession.Ort != Ort.Lager)
            return null;

        var standCounts = standSession.ScannedArticles
            .GroupBy(a => a.UnitId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.QuantityUnits));
        var lagerCounts = lagerSession.ScannedArticles
            .GroupBy(a => a.UnitId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.QuantityUnits));

        var allUnitIds = standCounts.Keys.Union(lagerCounts.Keys).ToHashSet();

        Dictionary<int, int>? requiredUnits = null;
        if (lagerSession.DispatchSheetId.HasValue)
        {
            requiredUnits = await dbContext.DispatchSheetRequiredUnits
                .Where(r => r.DispatchSheetId == lagerSession.DispatchSheetId.Value && r.RequiredCount > 0)
                .ToDictionaryAsync(r => r.UnitId, r => r.RequiredCount);

            foreach (var unitId in requiredUnits.Keys)
                allUnitIds.Add(unitId);
        }

        if (allUnitIds.Count == 0)
            return (standSession, lagerSession, []);

        var articleUnits = await dbContext.ArticleUnits
            .Where(a => allUnitIds.Contains(a.UnitId))
            .ToDictionaryAsync(a => a.UnitId);

        var results = new List<DtoCombinedArticle>();

        foreach (var unitId in allUnitIds)
        {
            if (!articleUnits.TryGetValue(unitId, out var articleUnit))
                throw new ApplicationException($"UnitId {unitId} nicht gefunden");

            var countStand = standCounts.GetValueOrDefault(unitId, 0);
            var countAnhaenger = lagerCounts.GetValueOrDefault(unitId, 0);
            var total = countStand + countAnhaenger;

            int? requiredCount = requiredUnits != null && requiredUnits.TryGetValue(unitId, out var req) ? req : null;
            int? fehlt = requiredCount.HasValue && total < requiredCount.Value ? requiredCount.Value - total : null;

            results.Add(new DtoCombinedArticle
            {
                UnitId = unitId,
                ArticleNr = articleUnit.ArtNr,
                ArticleDisplayName = articleUnit.DisplayName,
                UnitWeight = articleUnit.Weight,
                Ean = articleUnit.EanUnit ?? string.Empty,
                CountStand = countStand,
                CountAnhaenger = countAnhaenger,
                Total = total,
                RequiredCount = requiredCount,
                Fehlt = fehlt
            });
        }

        return (standSession, lagerSession, results.OrderBy(r => r.ArticleNr).ThenBy(r => r.UnitWeight).ToArray());
    }
}
