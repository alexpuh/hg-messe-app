using System.Net;
using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoriesController(InventoryService inventoryService, ILogger<InventoriesController> logger) : ControllerBase
{
    [HttpPost(Name = nameof(CreateInventory))]
    public async Task<ActionResult<DtoEventInventory>> CreateInventory([FromQuery] int? tradeEventId = null)
    {
        var inventoryId = await inventoryService.CreateInventoryAsync(tradeEventId);
        var inventory = await inventoryService.GetInventoryAsync(inventoryId);
        
        if (inventory == null)
        {
            return Problem("Failed to create inventory");
        }
        
        var dto = new DtoEventInventory 
        { 
            Id = inventory.Id, 
            StartedAt = inventory.StartedAt, 
            TradeEventId = inventory.TradeEventId, 
            UpdatedAt = inventory.UpdatedAt 
        };
        
        return CreatedAtRoute(nameof(GetInventory), new { id = inventory.Id }, dto);
    }
    
    [HttpGet("current", Name = nameof(GetCurrentInventory))]
    public async Task<ActionResult<DtoEventInventory>> GetCurrentInventory()
    {
        var result = await inventoryService.GetCurrentInventoryAsync();
        if (result == null)
        {
            return NotFound();
        }
        return new DtoEventInventory { Id = result.Id, StartedAt = result.StartedAt, TradeEventId = result.TradeEventId, UpdatedAt = result.UpdatedAt};
    }
    
    [HttpGet("{id:int}", Name = nameof(GetInventory))]
    public async Task<ActionResult<DtoEventInventory>> GetInventory(int id)
    {
        var result = await inventoryService.GetInventoryAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return new DtoEventInventory { Id = result.Id, StartedAt = result.StartedAt, TradeEventId = result.TradeEventId, UpdatedAt = result.UpdatedAt};
    }
    
    [HttpGet("{id:int}/stock", Name = nameof(GetInventoryStockItems))]
    public async Task<ActionResult<DtoInventoryStockItem[]>> GetInventoryStockItems(int id)
    {
        var result = await inventoryService.GetInventoryResultsAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return result;
    }
    
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    [Route("{id:int}/stock/excel", Name = nameof(GetInventoryStockItemsExcel))]
    [ProducesResponseType(typeof(FileContentResult), (int)HttpStatusCode.OK)]    
    public async Task<IActionResult> GetInventoryStockItemsExcel(
        int id,
        [FromServices] InventoryExcelExportService excelReportService
        )
    {
        var result = await inventoryService.GetInventoryResultsAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        using var memoryStream = new MemoryStream();
        excelReportService.Generate(memoryStream, result, DateTime.Today.ToString("yyyy-MM-dd"));
        var data = memoryStream.ToArray();
        return new FileContentResult(data, ExcelContentType)
        {
            FileDownloadName = "result.xlsx"
        };
    }
    
}