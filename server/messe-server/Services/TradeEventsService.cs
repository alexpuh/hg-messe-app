using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class TradeEventsService
{
    private readonly MesseAppDbContext _dbContext;
    private readonly ILogger<TradeEventsService> _logger;

    public TradeEventsService(MesseAppDbContext dbContext, ILogger<TradeEventsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<IEnumerable<DtoTradeEvent>> GetTradeEventsAsync()
    {
        var events = await _dbContext.TradeEvents
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
        var tradeEvent = await _dbContext.TradeEvents
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

        _dbContext.TradeEvents.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Trade Event erstellt: Id={Id}, Name={Name}", entity.Id, entity.Name);

        return new DtoTradeEvent
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public async Task<bool> UpdateAsync(int id, DtoTradeEvent tradeEvent)
    {
        var entity = await _dbContext.TradeEvents.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        entity.Name = tradeEvent.Name;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Trade Event aktualisiert: Id={Id}, Name={Name}", id, tradeEvent.Name);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _dbContext.TradeEvents.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _dbContext.TradeEvents.Remove(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Trade Event gelöscht: Id={Id}", id);
        return true;
    }
}

