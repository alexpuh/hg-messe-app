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
    [HttpGet(Name = nameof(GetAllScanSessions))]
    public async Task<ActionResult<DtoScanSession[]>> GetAllScanSessions()
    {
        var sessions = await scanSessionService.GetAllScanSessionsAsync();
        return sessions.Select(MapToDto).ToArray();
    }

    [HttpPost(Name = nameof(CreateScanSession))]
    public async Task<ActionResult<DtoScanSession>> CreateScanSession(
        [FromQuery] ScanSessionType sessionType,
        [FromQuery] Ort? ort,
        [FromQuery] int? dispatchSheetId = null)
    {
        if (ort == null)
            return BadRequest("ort ist erforderlich.");

        logger.LogDebug("Scan Session started: SessionType={SessionType}, Ort={Ort}, DispatchSheetId={DispatchSheetId}", sessionType, ort, dispatchSheetId);

        // Validate Ort + SessionType + DispatchSheetId combinations
        if (sessionType == ScanSessionType.ProcessDispatchList)
        {
            if (ort == Ort.Stand)
                return BadRequest("Ort 'Stand' ist für eine Beladung (ProcessDispatchList) nicht erlaubt.");
            if (dispatchSheetId == null)
                return BadRequest("dispatchSheetId ist erforderlich für SessionType 'ProcessDispatchList'.");
        }
        else if (sessionType == ScanSessionType.Inventory)
        {
            if (ort == Ort.Lager && dispatchSheetId == null)
                return BadRequest("dispatchSheetId ist erforderlich für Bestandsaufnahme am Lager.");
            if (ort == Ort.Stand && dispatchSheetId != null)
                return BadRequest("dispatchSheetId darf nicht angegeben werden für Bestandsaufnahme am Stand.");
        }

        int scanSessionId;
        try
        {
            scanSessionId = await scanSessionService.CreateScanSessionAsync(sessionType, ort.Value, dispatchSheetId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        var scanSession = await scanSessionService.GetScanSessionAsync(scanSessionId);
        
        if (scanSession == null)
        {
            return Problem("Failed to create scan session");
        }
        
        return CreatedAtRoute(nameof(GetScanSession), new { id = scanSession.Id }, MapToDto(scanSession));
    }
    
    [HttpGet("current", Name = nameof(GetCurrentScanSession))]
    public async Task<ActionResult<DtoScanSession>> GetCurrentScanSession()
    {
        var result = await scanSessionService.GetCurrentScanSessionAsync();
        if (result == null)
        {
            return NotFound();
        }
        return MapToDto(result);
    }
    
    [HttpGet("{id:int}", Name = nameof(GetScanSession))]
    public async Task<ActionResult<DtoScanSession>> GetScanSession(int id)
    {
        var result = await scanSessionService.GetScanSessionAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return MapToDto(result);
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
        
        bool showExpectation = result.Value.sessionType == ScanSessionType.ProcessDispatchList
                            || result.Value.ort == Ort.Lager;
        
        string title = result.Value.sessionType == ScanSessionType.ProcessDispatchList
            ? "Beladung"
            : result.Value.ort == Ort.Lager
                ? "Bestandsaufnahme Lager"
                : "Messestand";
        
        using var memoryStream = new MemoryStream();
        excelReportService.Generate(
            memoryStream, result.Value.dispatchSheetName, result.Value.articles, 
            DateTime.Today.ToString("yyyy-MM-dd"), 
            showExpectation, title);
        var data = memoryStream.ToArray();
        return new FileContentResult(data, ExcelContentType)
        {
            FileDownloadName = "result.xlsx"
        };
    }

    [HttpGet("combined", Name = nameof(GetCombinedArticles))]
    public async Task<ActionResult<DtoCombinedArticle[]>> GetCombinedArticles(
        [FromQuery] int standSessionId,
        [FromQuery] int lagerSessionId)
    {
        if (standSessionId <= 0 || lagerSessionId <= 0)
            return BadRequest("Gültige standSessionId und lagerSessionId sind erforderlich.");

        var result = await scanSessionService.GetCombinedArticlesAsync(standSessionId, lagerSessionId);
        if (result is null)
        {
            return NotFound("Eine oder beide Sessions wurden nicht gefunden oder haben den falschen Ort-Typ.");
        }
        return result.Value.articles;
    }

    [HttpGet("combined/excel", Name = nameof(GetCombinedArticlesExcel))]
    [ProducesResponseType(typeof(FileContentResult), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCombinedArticlesExcel(
        [FromQuery] int standSessionId,
        [FromQuery] int lagerSessionId,
        [FromServices] ScanSessionExcelExportService excelReportService)
    {
        if (standSessionId <= 0 || lagerSessionId <= 0)
            return BadRequest("Gültige standSessionId und lagerSessionId sind erforderlich.");

        var result = await scanSessionService.GetCombinedArticlesAsync(standSessionId, lagerSessionId);
        if (result is null)
        {
            return NotFound("Eine oder beide Sessions wurden nicht gefunden oder haben den falschen Ort-Typ.");
        }
        using var memoryStream = new MemoryStream();
        excelReportService.GenerateCombined(memoryStream, result.Value.standSession, result.Value.lagerSession, result.Value.articles);
        var data = memoryStream.ToArray();
        return new FileContentResult(data, ExcelContentType)
        {
            FileDownloadName = $"Messeabschluss_{DateTime.Today:yyyy-MM-dd}.xlsx"
        };
    }

    private static DtoScanSession MapToDto(ScanSession s) => new()
    {
        Id = s.Id,
        StartedAt = s.StartedAt,
        SessionType = s.SessionType,
        Ort = s.Ort,
        DispatchSheetId = s.DispatchSheetId,
        UpdatedAt = s.UpdatedAt
    };
}
