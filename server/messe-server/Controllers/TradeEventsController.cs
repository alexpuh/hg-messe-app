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
}

