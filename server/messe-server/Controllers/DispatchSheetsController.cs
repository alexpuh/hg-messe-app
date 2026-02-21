using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatchSheetsController(DispatchSheetService dispatchSheetService) : ControllerBase
{
    /// <summary>
    /// Holt alle Beladeliste
    /// </summary>
    [HttpGet(Name = nameof(GetDispatchSheets))]
    public async Task<ActionResult<DtoDispatchSheet[]>> GetDispatchSheets()
    {
        var events = await dispatchSheetService.GetDispatchSheetsAsync();
        return Ok(events.ToArray());
    }
    
    /// <summary>
    /// Holt eine einzelne Beladeliste anhand ihrer ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetDispatchSheetById))]
    public async Task<ActionResult<DtoDispatchSheet>> GetDispatchSheetById(int id)
    {
        var dispatchSheet = await dispatchSheetService.GetDispatchSheetByIdAsync(id);
        if (dispatchSheet == null)
        {
            return NotFound(new { Message = "Verladeschein nicht gefunden", Id = id });
        }
        return Ok(dispatchSheet);
    }
    
    /// <summary>
    /// Erstellt eine neue Beladeliste
    /// </summary>
    [HttpPost(Name = nameof(AddDispatchSheet))]
    public async Task<ActionResult<DtoDispatchSheet>> AddDispatchSheet(DtoDispatchSheet dispatchSheet)
    {
        if (dispatchSheet.Id != null)
        {
            return BadRequest("Id must be null");
        }
        
        var created = await dispatchSheetService.AddAsync(dispatchSheet);
        return CreatedAtRoute(nameof(GetDispatchSheetById), new { id = created.Id }, created);
    }
    
    /// <summary>
    /// Aktualisiert ein bestehendes Verladeschein
    /// </summary>
    [HttpPut("{id:int}", Name = nameof(UpdateDispatchSheet))]
    public async Task<IActionResult> UpdateDispatchSheet(int id, DtoDispatchSheet dispatchSheet)
    {
        var success = await dispatchSheetService.UpdateAsync(id, dispatchSheet);
        if (!success)
        {
            return NotFound(new { Message = "Verladeschein nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Löscht eine Verladeschein
    /// </summary>
    [HttpDelete("{id:int}", Name = nameof(DeleteDispatchSheet))]
    public async Task<IActionResult> DeleteDispatchSheet(int id)
    {
        var success = await dispatchSheetService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { Message = "Verladeschein nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für eine Verladeschein
    /// </summary>
    [HttpPost("{dispatchSheetId:int}/required-units", Name = nameof(SetRequiredUnits))]
    public async Task<IActionResult> SetRequiredUnits(int dispatchSheetId, [FromBody] SetRequiredUnitsRequest request)
    {
        var success = await dispatchSheetService.SetRequiredUnitsAsync(dispatchSheetId, request.UnitId, request.Count);
        if (!success)
        {
            return NotFound(new { Message = "Verladeschein nicht gefunden", DispatchSheetId = dispatchSheetId });
        }
        return NoContent();
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für eine Verladeschein
    /// </summary>
    [HttpGet("{dispatchSheetId:int}/required-units", Name = nameof(GetRequiredUnits))]
    public async Task<ActionResult<IDictionary<int, int>>> GetRequiredUnits(int dispatchSheetId)
    {
        var requiredUnits = await dispatchSheetService.GetRequiredUnitsAsync(dispatchSheetId);
        return Ok(requiredUnits);
    }

    /// <summary>
    /// Löscht eine erforderliche Artikeleinheit für eine Verladeschein
    /// </summary>
    [HttpDelete("{dispatchSheetId:int}/required-units/{unitId:int}", Name = nameof(DeleteRequiredUnit))]
    public async Task<IActionResult> DeleteRequiredUnit(int dispatchSheetId, int unitId)
    {
        await dispatchSheetService.DeleteRequiredUnitAsync(dispatchSheetId, unitId);
        return NoContent();
    }
    
    /// <summary>
    /// Holt alle Artikeleinheiten für eine Beladeliste und ihre Mindestanzahl
    /// </summary>
    [HttpGet("{dispatchSheetId:int}/units", Name = nameof(GetDispatchSheetArticleUnits))]
    public async Task<ActionResult<DtoDispatchSheetArticleUnit[]>> GetDispatchSheetArticleUnits(int dispatchSheetId)
    {
        var ret = await dispatchSheetService.GetDispatchSheetArticleUnits(dispatchSheetId);
        if (ret == null)
        {
            return NotFound();
        }
        return ret;
    }
}

/// <summary>
/// Request-Modell zum Setzen der erforderlichen Einheiten
/// </summary>
public record SetRequiredUnitsRequest(int UnitId, int Count);
