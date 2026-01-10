using Herrmann.MesseApp.Server.Dto;

namespace Herrmann.MesseApp.Server.Services;

public class TradeEventsService
{
    private readonly List<DtoTradeEvent> tradeEvents =
    [
        new() { Id = 1, Name = "Berlin" },
        new() { Id = 2, Name = "Frankfurt" }
    ];
    
    public IEnumerable<DtoTradeEvent> GetTradeEvents()
    {
        return tradeEvents;
    }
    
    public void Add(DtoTradeEvent tradeEvent)
    {
        tradeEvent.Id = tradeEvents.Max(x => x.Id) + 1;
        tradeEvents.Add(tradeEvent);
    } 
}