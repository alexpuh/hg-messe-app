using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradeEventsController(TradeEventsService tradeEventsService) : ControllerBase
{
    [HttpGet(Name = "GetTradeEvents")]
    public DtoTradeEvent[] GetList()
    {
        return tradeEventsService.GetTradeEvents().ToArray();
    }
    
    [HttpGet(Name = "AddTradeEvent")]
    public IActionResult Add(DtoTradeEvent tradeEvent)
    {
        if (tradeEvent.Id != null)
        {
            return BadRequest("Id must be null");
        }
        tradeEventsService.Add(tradeEvent);
        return Ok();
    }
}