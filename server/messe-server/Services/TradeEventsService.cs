using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class TradeEventsService(MesseAppDbContext dbContext, ILogger<TradeEventsService> logger)
{
    public async Task<IEnumerable<DtoTradeEvent>> GetTradeEventsAsync()
    {
        var events = await dbContext.TradeEvents
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();

        return events.Select(e => new DtoTradeEvent
        {
            Id = e.Id,
            Name = e.Name
        });
    }
    
    public async Task<DtoTradeEvent?> GetTradeEventByIdAsync(int id)
    {
        var tradeEvent = await dbContext.TradeEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (tradeEvent == null)
        {
            return null;
        }

        return new DtoTradeEvent
        {
            Id = tradeEvent.Id,
            Name = tradeEvent.Name
        };
    }
    
    public async Task<DtoTradeEvent> AddAsync(DtoTradeEvent tradeEvent)
    {
        var entity = new TradeEvent
        {
            Name = tradeEvent.Name,
            CreatedAt = DateTime.Now
        };

        dbContext.TradeEvents.Add(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Trade Event erstellt: Id={Id}, Name={Name}", entity.Id, entity.Name);

        return new DtoTradeEvent
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public async Task<bool> UpdateAsync(int id, DtoTradeEvent tradeEvent)
    {
        var entity = await dbContext.TradeEvents.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        entity.Name = tradeEvent.Name;
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Trade Event aktualisiert: Id={Id}, Name={Name}", id, tradeEvent.Name);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await dbContext.TradeEvents.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        dbContext.TradeEvents.Remove(entity);
        await dbContext.SaveChangesAsync();

        logger.LogDebug("Trade Event gelöscht: Id={Id}", id);
        return true;
    }

    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für ein Trade Event
    /// </summary>
    /// <param name="tradeEventId">ID des Trade Events</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <param name="count">Erforderliche Anzahl (0 oder negativ zum Löschen)</param>
    /// <returns>true wenn erfolgreich, false wenn TradeEvent nicht gefunden wurde</returns>
    public async Task<bool> SetRequiredUnitsAsync(int tradeEventId, int unitId, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }
        
        // Prüfe ob TradeEvent existiert
        var tradeEventExists = await dbContext.TradeEvents.AnyAsync(t => t.Id == tradeEventId);
        if (!tradeEventExists)
        {
            logger.LogWarning("TradeEvent {TradeEventId} nicht gefunden", tradeEventId);
            return false;
        }
        
        var existing = await dbContext.TradeEventRequiredUnits
            .FirstOrDefaultAsync(t => t.TradeEventId == tradeEventId && t.UnitId == unitId);
        
        if (existing != null)
        {
            // Aktualisiere die Anzahl
            existing.RequiredCount = count;
            existing.UpdatedAt = DateTime.Now;
            logger.LogDebug("Aktualisiert: Required unit {UnitId} für TradeEvent {TradeEventId}: {Count}", unitId, tradeEventId, count);
        }
        else
        {
            // Erstelle neuen Eintrag
            var newEntry = new TradeEventRequiredUnit
            {
                TradeEventId = tradeEventId,
                UnitId = unitId,
                RequiredCount = count,
                UpdatedAt = DateTime.Now
            };
            dbContext.TradeEventRequiredUnits.Add(newEntry);
            logger.LogDebug("Hinzugefügt: Required unit {UnitId} zu TradeEvent {TradeEventId}: {Count}", unitId, tradeEventId, count);
        }
        
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für ein Trade Event
    /// </summary>
    /// <param name="tradeEventId">ID des Trade Events</param>
    /// <returns>Dictionary mit UnitId als Key und erforderlicher Anzahl als Value</returns>
    public async Task<IDictionary<int, int>> GetRequiredUnitsAsync(int tradeEventId)
    {
        var requiredUnits = await dbContext.TradeEventRequiredUnits
            .AsNoTracking()
            .Where(t => t.TradeEventId == tradeEventId)
            .ToDictionaryAsync(t => t.UnitId, t => t.RequiredCount);
        
        logger.LogDebug("Abgerufen: {Count} required units für TradeEvent {TradeEventId}", requiredUnits.Count, tradeEventId);
        
        return requiredUnits;
    }

    /// <summary>
    /// Löscht eine erforderliche Artikeleinheit für ein Trade Event
    /// </summary>
    /// <param name="tradeEventId">ID des Trade Events</param>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <returns>true wenn gelöscht wurde, false wenn nicht vorhanden (kein Fehler)</returns>
    public async Task DeleteRequiredUnitAsync(int tradeEventId, int unitId)
    {
        var existing = await dbContext.TradeEventRequiredUnits
            .FirstOrDefaultAsync(t => t.TradeEventId == tradeEventId && t.UnitId == unitId);
        
        if (existing == null)
        {
            logger.LogDebug("Required unit {UnitId} für TradeEvent {TradeEventId} nicht gefunden - keine Aktion", unitId, tradeEventId);
            return;
        }
        
        dbContext.TradeEventRequiredUnits.Remove(existing);
        await dbContext.SaveChangesAsync();
        
        logger.LogDebug("Required unit {UnitId} für TradeEvent {TradeEventId} gelöscht", unitId, tradeEventId);
    }
}
