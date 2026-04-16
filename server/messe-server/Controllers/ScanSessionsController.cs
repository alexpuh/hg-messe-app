using System.Net;
using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanSessionsController(ScanSessionService scanSessionService, ILogger<ScanSessionsController> logger) : ControllerBase
{
    [HttpPost(Name = nameof(CreateScanSession))]
    public async Task<ActionResult<DtoScanSession>> CreateScanSession(
        [FromQuery] ScanSessionType sessionType,
        [FromQuery] int? dispatchSheetId = null)
    {
        logger.LogDebug("Scan Session started: SessionType={SessionType}, DispatchSheetId={DispatchSheetId}", sessionType, dispatchSheetId);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (sessionType)
        {
            case ScanSessionType.ProcessDispatchList when dispatchSheetId == null:
                return BadRequest("dispatchSheetId ist erforderlich für SessionType 'ProcessDispatchList'.");
            case ScanSessionType.Inventory when dispatchSheetId != null:
                return BadRequest("dispatchSheetId darf nicht angegeben werden für SessionType 'Inventory'.");
        }

        var scanSessionId = await scanSessionService.CreateScanSessionAsync(sessionType, dispatchSheetId);
        var scanSession = await scanSessionService.GetScanSessionAsync(scanSessionId);
        
        if (scanSession == null)
        {
            return Problem("Failed to create scan session");
        }
        
        var dto = new DtoScanSession 
        { 
            Id = scanSession.Id, 
            StartedAt = scanSession.StartedAt,
            SessionType = scanSession.SessionType,
            DispatchSheetId = scanSession.DispatchSheetId, 
            UpdatedAt = scanSession.UpdatedAt 
        };
        
        return CreatedAtRoute(nameof(GetScanSession), new { id = scanSession.Id }, dto);
    }
    
    [HttpGet("current", Name = nameof(GetCurrentScanSession))]
    public async Task<ActionResult<DtoScanSession>> GetCurrentScanSession()
    {
        var result = await scanSessionService.GetCurrentScanSessionAsync();
        if (result == null)
        {
            return NotFound();
        }
        return new DtoScanSession { Id = result.Id, StartedAt = result.StartedAt, SessionType = result.SessionType, DispatchSheetId = result.DispatchSheetId, UpdatedAt = result.UpdatedAt};
    }
    
    [HttpGet("{id:int}", Name = nameof(GetScanSession))]
    public async Task<ActionResult<DtoScanSession>> GetScanSession(int id)
    {
        var result = await scanSessionService.GetScanSessionAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return new DtoScanSession { Id = result.Id, StartedAt = result.StartedAt, SessionType = result.SessionType, DispatchSheetId = result.DispatchSheetId, UpdatedAt = result.UpdatedAt};
    }
    
    [HttpGet("{id:int}/articles", Name = nameof(GetScanSessionArticles))]
    public async Task<ActionResult<DtoScanSessionArticle[]>> GetScanSessionArticles(int id)
    {
        var result = await scanSessionService.GetScanSessionArticlesAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return result.Value.articles;
    }
    
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    [Route("{id:int}/articles/excel", Name = nameof(GetScanSessionArticlesExcel))]
    [ProducesResponseType(typeof(FileContentResult), (int)HttpStatusCode.OK)]    
    public async Task<IActionResult> GetScanSessionArticlesExcel(
        int id,
        [FromServices] ScanSessionExcelExportService excelReportService
        )
    {
        var result = await scanSessionService.GetScanSessionArticlesAsync(id);
        if (result is null)
        {
            return NotFound();
        }
        using var memoryStream = new MemoryStream();
        excelReportService.Generate(
            memoryStream, result.Value.dispatchSheetName, result.Value.articles, 
            DateTime.Today.ToString("yyyy-MM-dd"), 
            result.Value.sessionType == ScanSessionType.ProcessDispatchList);
        var data = memoryStream.ToArray();
        return new FileContentResult(data, ExcelContentType)
        {
            FileDownloadName = "result.xlsx"
        };
    }
    
}