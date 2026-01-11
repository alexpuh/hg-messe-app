using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradeEventsController(TradeEventsService tradeEventsService) : ControllerBase
{
    /// <summary>
    /// Holt alle Trade Events
    /// </summary>
    [HttpGet(Name = "GetTradeEvents")]
    public async Task<ActionResult<DtoTradeEvent[]>> GetList()
    {
        var events = await tradeEventsService.GetTradeEventsAsync();
        return Ok(events.ToArray());
    }
    
    /// <summary>
    /// Holt ein einzelnes Trade Event anhand seiner ID
    /// </summary>
    [HttpGet("{id:int}", Name = "GetTradeEventById")]
    public async Task<ActionResult<DtoTradeEvent>> GetById(int id)
    {
        var tradeEvent = await tradeEventsService.GetTradeEventByIdAsync(id);
        if (tradeEvent == null)
        {
            return NotFound(new { Message = "Trade Event nicht gefunden", Id = id });
        }
        return Ok(tradeEvent);
    }
    
    /// <summary>
    /// Erstellt ein neues Trade Event
    /// </summary>
    [HttpPost(Name = "AddTradeEvent")]
    public async Task<ActionResult<DtoTradeEvent>> Add(DtoTradeEvent tradeEvent)
    {
        if (tradeEvent.Id != null)
        {
            return BadRequest("Id must be null");
        }
        
        var created = await tradeEventsService.AddAsync(tradeEvent);
        return CreatedAtRoute("GetTradeEventById", new { id = created.Id }, created);
    }
    
    /// <summary>
    /// Aktualisiert ein bestehendes Trade Event
    /// </summary>
    [HttpPut("{id:int}", Name = "UpdateTradeEvent")]
    public async Task<IActionResult> Update(int id, DtoTradeEvent tradeEvent)
    {
        var success = await tradeEventsService.UpdateAsync(id, tradeEvent);
        if (!success)
        {
            return NotFound(new { Message = "Trade Event nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Löscht ein Trade Event
    /// </summary>
    [HttpDelete("{id:int}", Name = "DeleteTradeEvent")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await tradeEventsService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { Message = "Trade Event nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für ein Trade Event
    /// </summary>
    [HttpPost("{tradeEventId:int}/required-units", Name = "SetRequiredUnits")]
    public async Task<IActionResult> SetRequiredUnits(int tradeEventId, [FromBody] SetRequiredUnitsRequest request)
    {
        try
        {
            await tradeEventsService.SetRequiredUnitsAsync(request.UnitId, tradeEventId, request.Count);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Fehler beim Setzen der Required Units", Error = ex.Message });
        }
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für ein Trade Event
    /// </summary>
    [HttpGet("{tradeEventId:int}/required-units", Name = "GetRequiredUnits")]
    public async Task<ActionResult<IDictionary<int, int>>> GetRequiredUnits(int tradeEventId)
    {
        var requiredUnits = await tradeEventsService.GetRequiredUnitsAsync(tradeEventId);
        return Ok(requiredUnits);
    }
}

/// <summary>
/// Request-Modell zum Setzen der erforderlichen Einheiten
/// </summary>
public record SetRequiredUnitsRequest(int UnitId, int Count);
