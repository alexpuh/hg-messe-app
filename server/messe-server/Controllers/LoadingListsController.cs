using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoadingListsController(LoadingListService loadingListService) : ControllerBase
{
    /// <summary>
    /// Holt alle Loading Lists
    /// </summary>
    [HttpGet(Name = nameof(GetLoadingLists))]
    public async Task<ActionResult<DtoLoadingList[]>> GetLoadingLists()
    {
        var events = await loadingListService.GetLoadingListsAsync();
        return Ok(events.ToArray());
    }
    
    /// <summary>
    /// Holt eine einzelne Beladeliste anhand ihrer ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetLoadingListById))]
    public async Task<ActionResult<DtoLoadingList>> GetLoadingListById(int id)
    {
        var loadingList = await loadingListService.GetLoadingListByIdAsync(id);
        if (loadingList == null)
        {
            return NotFound(new { Message = "Beladeliste nicht gefunden", Id = id });
        }
        return Ok(loadingList);
    }
    
    /// <summary>
    /// Erstellt ein neues Loading List
    /// </summary>
    [HttpPost(Name = nameof(AddLoadingList))]
    public async Task<ActionResult<DtoLoadingList>> AddLoadingList(DtoLoadingList loadingList)
    {
        if (loadingList.Id != null)
        {
            return BadRequest("Id must be null");
        }
        
        var created = await loadingListService.AddAsync(loadingList);
        return CreatedAtRoute(nameof(GetLoadingListById), new { id = created.Id }, created);
    }
    
    /// <summary>
    /// Aktualisiert ein bestehendes Loading List
    /// </summary>
    [HttpPut("{id:int}", Name = nameof(UpdateLoadingList))]
    public async Task<IActionResult> UpdateLoadingList(int id, DtoLoadingList loadingList)
    {
        var success = await loadingListService.UpdateAsync(id, loadingList);
        if (!success)
        {
            return NotFound(new { Message = "Beladeliste nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Löscht eine Beladeliste
    /// </summary>
    [HttpDelete("{id:int}", Name = nameof(DeleteLoadingList))]
    public async Task<IActionResult> DeleteLoadingList(int id)
    {
        var success = await loadingListService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { Message = "Beladeliste nicht gefunden", Id = id });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Setzt die erforderliche Anzahl einer Artikeleinheit für eine Beladeliste
    /// </summary>
    [HttpPost("{loadingListId:int}/required-units", Name = nameof(SetRequiredUnits))]
    public async Task<IActionResult> SetRequiredUnits(int loadingListId, [FromBody] SetRequiredUnitsRequest request)
    {
        var success = await loadingListService.SetRequiredUnitsAsync(loadingListId, request.UnitId, request.Count);
        if (!success)
        {
            return NotFound(new { Message = "Beladeliste nicht gefunden", LoadingListId = loadingListId });
        }
        return NoContent();
    }

    /// <summary>
    /// Holt alle erforderlichen Artikeleinheiten für eine Beladeliste
    /// </summary>
    [HttpGet("{loadingListId:int}/required-units", Name = nameof(GetRequiredUnits))]
    public async Task<ActionResult<IDictionary<int, int>>> GetRequiredUnits(int loadingListId)
    {
        var requiredUnits = await loadingListService.GetRequiredUnitsAsync(loadingListId);
        return Ok(requiredUnits);
    }

    /// <summary>
    /// Löscht eine erforderliche Artikeleinheit für eine Beladeliste
    /// </summary>
    [HttpDelete("{loadingListId:int}/required-units/{unitId:int}", Name = nameof(DeleteRequiredUnit))]
    public async Task<IActionResult> DeleteRequiredUnit(int loadingListId, int unitId)
    {
        await loadingListService.DeleteRequiredUnitAsync(loadingListId, unitId);
        return NoContent();
    }
    
    /// <summary>
    /// Holt alle Artikeleinheiten für eine Beladeliste und ihre Mindestanzahl
    /// </summary>
    [HttpGet("{loadingListId:int}/units", Name = nameof(GetLoadingListArticleUnits))]
    public async Task<ActionResult<DtoLoadingListArticleUnit[]>> GetLoadingListArticleUnits(int loadingListId)
    {
        var ret = await loadingListService.GetLoadingListArticleUnits(loadingListId);
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
