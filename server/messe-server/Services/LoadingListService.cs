using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class LoadingListService(MesseAppDbContext dbContext, ILogger<LoadingListService> logger)
{
    public async Task<IEnumerable<DtoLoadingList>> GetLoadingListsAsync()
    {
        var events = await dbContext.LoadingLists
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();

        return events.Select(e => new DtoLoadingList
        {
            Id = e.Id,
            Name = e.Name
        });
    }
    
    public async Task<DtoLoadingList?> GetLoadingListByIdAsync(int id)
    {
        var loadingList = await dbContext.LoadingLists
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (loadingList == null)
        {
            return null;
        }

        return new DtoLoadingList
        {
            Id = loadingList.Id,
            Name = loadingList.Name
        };
    }
    
    public async Task<DtoLoadingList> AddAsync(DtoLoadingList loadingList)
    {
        var entity = new LoadingList
        {
            Name = loadingList.Name,
            CreatedAt = DateTime.Now
        };

        dbContext.LoadingLists.Add(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Beladeliste erstellt: Id={Id}, Name={Name}", entity.Id, entity.Name);

        return new DtoLoadingList
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public async Task<bool> UpdateAsync(int id, DtoLoadingList loadingList)
    {
        var entity = await dbContext.LoadingLists.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        entity.Name = loadingList.Name;
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Beladeliste aktualisiert: Id={Id}, Name={Name}", id, loadingList.Name);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await dbContext.LoadingLists.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        dbContext.LoadingLists.Remove(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Beladeliste gelöscht: Id={Id}", id);
        return true;
    }

    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für eine Beladeliste
    /// </summary>
    /// <param name="loadingListId">ID der Beladeliste</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <param name="count">Erforderliche Anzahl</param>
    /// <returns>true wenn erfolgreich, false wenn Beladeliste nicht gefunden wurde</returns>
    public async Task<bool> SetRequiredUnitsAsync(int loadingListId, int unitId, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }
        
        // Prüfe ob Beladeliste existiert
        var loadingListExists = await dbContext.LoadingLists.AnyAsync(t => t.Id == loadingListId);
        if (!loadingListExists)
        {
            logger.LogWarning("Beladeliste {LoadingListId} nicht gefunden", loadingListId);
            return false;
        }
        
        var existing = await dbContext.LoadingListRequiredUnits
            .FirstOrDefaultAsync(t => t.LoadingListId == loadingListId && t.UnitId == unitId);
        
        if (existing != null)
        {
            // Aktualisiere die Anzahl
            existing.RequiredCount = count;
            existing.UpdatedAt = DateTime.Now;
            logger.LogDebug("Aktualisiert: Required unit {UnitId} für Beladeliste {LoadingListId}: {Count}", unitId, loadingListId, count);
        }
        else
        {
            // Erstelle neuen Eintrag
            var newEntry = new LoadingListRequiredUnit
            {
                LoadingListId = loadingListId,
                UnitId = unitId,
                RequiredCount = count,
                UpdatedAt = DateTime.Now
            };
            dbContext.LoadingListRequiredUnits.Add(newEntry);
            logger.LogDebug("Hinzugefügt: Required unit {UnitId} zu Beladeliste {LoadingListId}: {Count}", unitId, loadingListId, count);
        }
        
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für eine Beladeliste
    /// </summary>
    /// <param name="loadingListId">ID der Beladeliste</param>
    /// <returns>Dictionary mit UnitId als Key und erforderlicher Anzahl als Value</returns>
    public async Task<IDictionary<int, int>> GetRequiredUnitsAsync(int loadingListId)
    {
        var requiredUnits = await dbContext.LoadingListRequiredUnits
            .AsNoTracking()
            .Where(t => t.LoadingListId == loadingListId)
            .ToDictionaryAsync(t => t.UnitId, t => t.RequiredCount);
        
        logger.LogDebug("Abgerufen: {Count} required units für Beladeliste {LoadingListId}", requiredUnits.Count, loadingListId);
        
        return requiredUnits;
    }

    /// <summary>
    /// Löscht eine erforderliche Artikeleinheit für eine Beladeliste
    /// </summary>
    /// <param name="loadingListId">ID der Beladeliste</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <returns>true wenn gelöscht wurde, false wenn nicht vorhanden (kein Fehler)</returns>
    public async Task DeleteRequiredUnitAsync(int loadingListId, int unitId)
    {
        var existing = await dbContext.LoadingListRequiredUnits
            .FirstOrDefaultAsync(t => t.LoadingListId == loadingListId && t.UnitId == unitId);
        
        if (existing == null)
        {
            logger.LogDebug("Required unit {UnitId} für Beladeliste {LoadingListId} nicht gefunden - keine Aktion", unitId, loadingListId);
            return;
        }
        
        dbContext.LoadingListRequiredUnits.Remove(existing);
        await dbContext.SaveChangesAsync();
        
        logger.LogDebug("Required unit {UnitId} für Beladeliste {LoadingListId} gelöscht", unitId, loadingListId);
    }

    /// <summary>
    /// Holt alle Artikeleinheiten für ein Beladeliste Display-Ready
    /// </summary>
    /// <param name="loadingListId">ID der Beladeliste</param>
    /// <returns>Liste der Artikel/Artikeleinheiten, sowohl mit Mindestanforderung, wie auch ohne. </returns>
    public async Task<DtoLoadingLIstArticleUnit[]?> GetLoadingListArticleUnits(int loadingListId)
    {
        // Prüfe ob LoadingList existiert
        var loadingListExists = await dbContext.LoadingLists.AnyAsync(t => t.Id == loadingListId);
        if (!loadingListExists)
        {
            logger.LogWarning("Beladeliste {LoadingListId} nicht gefunden", loadingListId);
            return null;
        }
        
        // Lade alle ArticleUnits
        var articleUnits = await dbContext.ArticleUnits
            .AsNoTracking()
            .Where(a => !a.IsArticleDisabled && !a.IsUnitDisabled)
            .OrderBy(a => a.ArtNr)
            .ThenBy(a => a.UnitId)
            .ToListAsync();
        
        // Lade erforderliche Mengen für diese Beladeliste
        var requiredUnits = await dbContext.LoadingListRequiredUnits
            .AsNoTracking()
            .Where(r => r.LoadingListId == loadingListId)
            .ToDictionaryAsync(r => r.UnitId, r => r.RequiredCount);
        
        // Mappe zu DTOs
        var result = articleUnits.Select(au => new DtoLoadingLIstArticleUnit
        {
            Id = au.UnitId, // Verwende UnitId als Id
            UnitId = au.UnitId,
            ArticleNr = au.ArtNr,
            ArticleDisplayName = au.DisplayName,
            UnitWeight = au.Weight,
            Ean = au.EanUnit ?? au.EanBox ?? string.Empty,
            RequiredCount = requiredUnits.TryGetValue(au.UnitId, out var requiredCount) ? requiredCount : null
        }).ToArray();
        
        logger.LogDebug("Abgerufen: {Count} ArticleUnits für Beladeliste {LoadingListId} ({RequiredCount} mit Anforderung)", 
            result.Length, loadingListId, requiredUnits.Count);
        
        return result;
    }
}
