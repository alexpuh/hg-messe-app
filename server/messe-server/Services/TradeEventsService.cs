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

        logger.LogInformation("Trade Event erstellt: Id={Id}, Name={Name}", entity.Id, entity.Name);

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

        logger.LogInformation("Trade Event aktualisiert: Id={Id}, Name={Name}", id, tradeEvent.Name);
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

        logger.LogInformation("Trade Event gelöscht: Id={Id}", id);
        return true;
    }

    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für ein Trade Event
    /// </summary>
    /// <param name="unitId">ID der Artikeleinheit</param>
    /// <param name="tradeEventId">ID des Trade Events</param>
    /// <param name="count">Erforderliche Anzahl (0 oder negativ zum Löschen)</param>
    public async Task SetRequiredUnitsAsync(int unitId, int tradeEventId, int count)
    {
        var existing = await dbContext.TradeEventRequiredUnits
            .FirstOrDefaultAsync(t => t.TradeEventId == tradeEventId && t.UnitId == unitId);
        
        if (existing != null)
        {
            if (count <= 0)
            {
                // Wenn count 0 oder negativ, lösche den Eintrag
                dbContext.TradeEventRequiredUnits.Remove(existing);
                logger.LogInformation("Entfernt: Required unit {UnitId} von TradeEvent {TradeEventId}", unitId, tradeEventId);
            }
            else
            {
                // Aktualisiere die Anzahl
                existing.RequiredCount = count;
                existing.UpdatedAt = DateTime.Now;
                logger.LogInformation("Aktualisiert: Required unit {UnitId} für TradeEvent {TradeEventId}: {Count}", unitId, tradeEventId, count);
            }
        }
        else if (count > 0)
        {
            // Erstelle neuen Eintrag
            var newEntry = new TradeEventRequiredUnit
            {
                TradeEventId = tradeEventId,
                UnitId = unitId,
                RequiredCount = count,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            dbContext.TradeEventRequiredUnits.Add(newEntry);
            logger.LogInformation("Hinzugefügt: Required unit {UnitId} zu TradeEvent {TradeEventId}: {Count}", unitId, tradeEventId, count);
        }
        
        await dbContext.SaveChangesAsync();
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
        
        logger.LogInformation("Abgerufen: {Count} required units für TradeEvent {TradeEventId}", requiredUnits.Count, tradeEventId);
        
        return requiredUnits;
    }
}

