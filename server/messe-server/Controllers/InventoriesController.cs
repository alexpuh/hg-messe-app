using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoriesController(InventoryService inventoryService, ILogger<InventoriesController> logger) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetInventoryStockItems))]
    public async Task<ActionResult<DtoInventoryStockItem[]>> GetInventoryStockItems(int id)
    {
        var result = await inventoryService.GetInventoryResultsAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return result;
    }
    
}