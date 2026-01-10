using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventInventoriesController(EventInventoriesService eventInventoriesService) : ControllerBase
{
    [HttpGet(Name = "GetEventInventories")]
    public IEnumerable<DtoEventInventory> GetList([FromQuery]int? count)
    {
        var q = eventInventoriesService.GetList(); 
        q = q.OrderByDescending(x => x.StartedAt);
        if (count != null)
        {
            q = q.Take(count.Value);
        }
        return q.ToArray();
    }

    [HttpGet("current", Name = "GetCurrentInventory")]
    public ActionResult<DtoEventInventory> GetCurrent()
    {
        var current = eventInventoriesService.GetCurrentEventInventory();
        if (current == null)
        {
            return NotFound();
        }
        return current;
    }

    [HttpPost("current/{id:int}", Name = "SetCurrentInventory")]
    public ActionResult<DtoEventInventory> SetCurrent(int id)
    {
        return !eventInventoriesService.TrySetCurrentEventInventory(id, out var eventInventory) ? NotFound() : eventInventory;
    }

    [HttpPost(Name = "AddEventInventory")]
    public DtoEventInventory Add(DtoEventInventory eventInventory)
    {
        eventInventoriesService.AddAndStart(eventInventory);
        return eventInventory;
    }

    private readonly Random r = new();
    [HttpPost("test", Name = "Test")]
    public async Task<ActionResult> Test()
    {
        var id = r.Next(0, 4) + 1;
        var ret = await eventInventoriesService.TryAddStockItem(id, r.Next(0, 3) == 0);
        if (!ret)
        {
            return BadRequest();
        }
        return Ok();
    }
    
    [HttpGet("stock/{id:int}", Name = "GetStockFromCurrentInventory")]
    public ActionResult<DtoStockItem[]> GetStockFromInventory(int id)
    {
        if (!eventInventoriesService.TryGetStockFromInventory(id, out var stock))
        {
            return NotFound();
        }
        return stock;
    }
}