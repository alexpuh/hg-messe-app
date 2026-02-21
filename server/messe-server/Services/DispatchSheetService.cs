using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class DispatchSheetService(MesseAppDbContext dbContext, ILogger<DispatchSheetService> logger)
{
    public async Task<IEnumerable<DtoDispatchSheet>> GetDispatchSheetsAsync()
    {
        var events = await dbContext.DispatchSheets
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();

        return events.Select(e => new DtoDispatchSheet
        {
            Id = e.Id,
            Name = e.Name
        });
    }
    
    public async Task<DtoDispatchSheet?> GetDispatchSheetByIdAsync(int id)
    {
        var dispatchSheet = await dbContext.DispatchSheets
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (dispatchSheet == null)
        {
            return null;
        }

        return new DtoDispatchSheet
        {
            Id = dispatchSheet.Id,
            Name = dispatchSheet.Name
        };
    }
    
    public async Task<DtoDispatchSheet> AddAsync(DtoDispatchSheet dispatchSheet)
    {
        var entity = new DispatchSheet
        {
            Name = dispatchSheet.Name,
            CreatedAt = DateTime.Now
        };

        dbContext.DispatchSheets.Add(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Verladeschein erstellt: Id={Id}, Name={Name}", entity.Id, entity.Name);

        return new DtoDispatchSheet
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public async Task<bool> UpdateAsync(int id, DtoDispatchSheet dispatchSheet)
    {
        var entity = await dbContext.DispatchSheets.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        entity.Name = dispatchSheet.Name;
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Verladeschein aktualisiert: Id={Id}, Name={Name}", id, dispatchSheet.Name);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await dbContext.DispatchSheets.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        dbContext.DispatchSheets.Remove(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Verladeschein gelöscht: Id={Id}", id);
        return true;
    }

    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für eine Verladeschein
    /// </summary>
    /// <param name="dispatchSheetId">ID der Verladeschein</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <param name="count">Erforderliche Anzahl</param>
    /// <returns>true wenn erfolgreich, false wenn Verladeschein nicht gefunden wurde</returns>
    public async Task<bool> SetRequiredUnitsAsync(int dispatchSheetId, int unitId, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
        }
        
        // Prüfe ob Verladeschein existiert
        var dispatchSheetExists = await dbContext.DispatchSheets.AnyAsync(t => t.Id == dispatchSheetId);
        if (!dispatchSheetExists)
        {
            logger.LogWarning("Verladeschein {DispatchSheetId} nicht gefunden", dispatchSheetId);
            return false;
        }
        
        var existing = await dbContext.DispatchSheetRequiredUnits
            .FirstOrDefaultAsync(t => t.DispatchSheetId == dispatchSheetId && t.UnitId == unitId);
        
        if (existing != null)
        {
            // Aktualisiere die Anzahl
            existing.RequiredCount = count;
            existing.UpdatedAt = DateTime.Now;
            logger.LogDebug("Aktualisiert: Required unit {UnitId} für Verladeschein {DispatchSheetId}: {Count}", unitId, dispatchSheetId, count);
        }
        else
        {
            // Erstelle neuen Eintrag
            var newEntry = new DispatchSheetRequiredUnit
            {
                DispatchSheetId = dispatchSheetId,
                UnitId = unitId,
                RequiredCount = count,
                UpdatedAt = DateTime.Now
            };
            dbContext.DispatchSheetRequiredUnits.Add(newEntry);
            logger.LogDebug("Hinzugefügt: Required unit {UnitId} zu Verladeschein {DispatchSheetId}: {Count}", unitId, dispatchSheetId, count);
        }
        
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für eine Verladeschein
    /// </summary>
    /// <param name="dispatchSheetId">ID der Verladeschein</param>
    /// <returns>Dictionary mit UnitId als Key und erforderlicher Anzahl als Value</returns>
    public async Task<IDictionary<int, int>> GetRequiredUnitsAsync(int dispatchSheetId)
    {
        var requiredUnits = await dbContext.DispatchSheetRequiredUnits
            .AsNoTracking()
            .Where(t => t.DispatchSheetId == dispatchSheetId)
            .ToDictionaryAsync(t => t.UnitId, t => t.RequiredCount);
        
        logger.LogDebug("Abgerufen: {Count} required units für Verladeschein {DispatchSheetId}", requiredUnits.Count, dispatchSheetId);
        
        return requiredUnits;
    }

    /// <summary>
    /// Löscht eine erforderliche Artikeleinheit für eine Verladeschein
    /// </summary>
    /// <param name="dispatchSheetId">ID der Verladeschein</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <returns>true wenn gelöscht wurde, false wenn nicht vorhanden (kein Fehler)</returns>
    public async Task DeleteRequiredUnitAsync(int dispatchSheetId, int unitId)
    {
        var existing = await dbContext.DispatchSheetRequiredUnits
            .FirstOrDefaultAsync(t => t.DispatchSheetId == dispatchSheetId && t.UnitId == unitId);
        
        if (existing == null)
        {
            logger.LogDebug("Required unit {UnitId} für Verladeschein {DispatchSheetId} nicht gefunden - keine Aktion", unitId, dispatchSheetId);
            return;
        }
        
        dbContext.DispatchSheetRequiredUnits.Remove(existing);
        await dbContext.SaveChangesAsync();
        
        logger.LogDebug("Required unit {UnitId} für Verladeschein {DispatchSheetId} gelöscht", unitId, dispatchSheetId);
    }

    /// <summary>
    /// Holt alle Artikeleinheiten für ein Verladeschein Display-Ready
    /// </summary>
    /// <param name="dispatchSheetId">ID der Verladeschein</param>
    /// <returns>Liste der Artikel/Artikeleinheiten, sowohl mit Mindestanforderung, wie auch ohne. </returns>
    public async Task<DtoDispatchSheetArticleUnit[]?> GetDispatchSheetArticleUnits(int dispatchSheetId)
    {
        // Prüfe ob DispatchSheet existiert
        var dispatchSheetExists = await dbContext.DispatchSheets.AnyAsync(t => t.Id == dispatchSheetId);
        if (!dispatchSheetExists)
        {
            logger.LogWarning("Verladeschein {DispatchSheetId} nicht gefunden", dispatchSheetId);
            return null;
        }
        
        // Lade alle ArticleUnits
        var articleUnits = await dbContext.ArticleUnits
            .AsNoTracking()
            .Where(a => !a.IsArticleDisabled && !a.IsUnitDisabled)
            .OrderBy(a => a.ArtNr)
            .ThenBy(a => a.UnitId)
            .ToListAsync();
        
        // Lade erforderliche Mengen für diese Verladeschein
        var requiredUnits = await dbContext.DispatchSheetRequiredUnits
            .AsNoTracking()
            .Where(r => r.DispatchSheetId == dispatchSheetId)
            .ToDictionaryAsync(r => r.UnitId, r => r.RequiredCount);
        
        // Mappe zu DTOs
        var result = articleUnits.Select(au => new DtoDispatchSheetArticleUnit
        {
            Id = au.UnitId, // Verwende UnitId als Id
            UnitId = au.UnitId,
            ArticleNr = au.ArtNr,
            ArticleDisplayName = au.DisplayName,
            UnitWeight = au.Weight,
            Ean = au.EanUnit ?? au.EanBox ?? string.Empty,
            RequiredCount = requiredUnits.TryGetValue(au.UnitId, out var requiredCount) ? requiredCount : null
        }).ToArray();
        
        logger.LogDebug("Abgerufen: {Count} ArticleUnits für Verladeschein {DispatchSheetId} ({RequiredCount} mit Anforderung)", 
            result.Length, dispatchSheetId, requiredUnits.Count);
        
        return result;
    }
}
