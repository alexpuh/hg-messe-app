using Herrmann.MesseApp.Server.Dto;
using Microsoft.AspNetCore.SignalR;

namespace Herrmann.MesseApp.Server.Services;

public class EventInventoriesService(ArticlesService articlesService, IHubContext<NotificationHub> hub, ILogger<EventInventoriesService> logger)
{
    private DtoEventInventory? currentEventInventory;
    private readonly List<DtoEventInventory> eventInventories = [];
    private readonly Dictionary<int, Dictionary<int, DtoStockItem>> eventInventoryStock = new();

    public IEnumerable<DtoEventInventory> GetList()
    {
        return eventInventories;
    }

    public void AddAndStart(DtoEventInventory eventInventory)
    {
        eventInventory.StartedAt = DateTime.Now;
        eventInventory.Id = (eventInventories.Max(x => x.Id) ?? 0) + 1;
        eventInventories.Add(eventInventory);
        currentEventInventory = eventInventory;
    }

    public DtoEventInventory? GetCurrentEventInventory()
    {
        if (currentEventInventory != null && currentEventInventory.StartedAt!.Value.AddHours(5) < DateTime.Now)
        {
            currentEventInventory = null;
        }
        return currentEventInventory;
    }
    
    public async Task<bool> TryAddStockItem(int unitId, bool box)
    {
        if (currentEventInventory == null)
        {
            return false;
        }
        
        if (!eventInventoryStock.TryGetValue(currentEventInventory.Id!.Value, out var stock))
        {
            stock = new Dictionary<int, DtoStockItem>();
            eventInventoryStock[currentEventInventory.Id.Value] = stock;
        }
        
        if (!stock.TryGetValue(unitId, out var stockItem))
        {
            if (!articlesService.TryGetArticleUnit(unitId, out var articleUnit))
            {
                return false;
            }
            stockItem = new DtoStockItem
            {
                UnitId = unitId,
                ArticleNr = articleUnit!.ArticleNr,
                ArticleDisplayName = articleUnit.ArticleName,
                QuantityPerBox = articleUnit.UnitsPerBox ?? 0,
                Required = 7
            };
            stock[unitId] = stockItem;
        }
        stockItem.QuantityUnits += box ? 0 : 1;
        stockItem.QuantityBox += box ? 1 : 0;
        stockItem.updatedAt = DateTime.Now;
        logger.LogDebug("Before sending stock changed notification");
        await hub.Clients.All.SendAsync("StockChanged");
        return true;
    }

    public bool TrySetCurrentEventInventory(int id, out DtoEventInventory o)
    {
        var eventInventory = eventInventories.FirstOrDefault(x => x.Id == id);
        if (eventInventory == null)
        {
            o = null!;
            return false;
        }
        currentEventInventory = eventInventory;
        o = eventInventory;
        return true;
    }

    public bool TryGetStockFromInventory(int id, out DtoStockItem[] stock)
    {
        if (!eventInventoryStock.TryGetValue(id, out var stockDict))
        {
            stock = null!;
            return false;
        }
        stock = stockDict.Values.OrderByDescending(x => x.updatedAt).ToArray();
        return true;
    }
}