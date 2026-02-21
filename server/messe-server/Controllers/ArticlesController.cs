using Herrmann.MesseApp.Server.Dto;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController(
    ArticlesService articlesService, 
    ILogger<ArticlesController> logger
    ) : ControllerBase
{
    /// <summary>
    /// Importiert Artikel aus einer JSON-Datei
    /// </summary>
    /// <param name="filePath">Pfad zur JSON-Datei (relativ zum Server)</param>
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromJson([FromBody] string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("Dateipfad darf nicht leer sein");
            }

            var count = await articlesService.ImportFromJsonFileAsync(filePath);
            return Ok(new { ImportedCount = count, Message = $"{count} Artikel erfolgreich importiert" });
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Datei nicht gefunden: {FilePath}", filePath);
            return NotFound(new { Message = "Datei nicht gefunden", FilePath = filePath });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Importieren von Artikeln");
            return StatusCode(500, new { Message = "Fehler beim Import", Error = ex.Message });
        }
    }

    /// <summary>
    /// Sucht einen Artikel anhand seiner EAN
    /// </summary>
    [HttpGet("by-ean/{ean}")]
    public IActionResult GetByEan(string ean)
    {
        if (articlesService.TryFindEan(ean, out var articleUnit))
        {
            return Ok(articleUnit);
        }
        return NotFound(new { Message = "Artikel mit dieser EAN nicht gefunden", Ean = ean });
    }
    
    /// <summary>
    /// Sucht einen Artikel anhand seiner EAN
    /// </summary>
    [HttpGet("units")]
    public ActionResult<EanUnit[]> GetUnitList()
    {
        return articlesService.GetAllEanUnits().ToArray();
    }

    /// <summary>
    /// Holt einen Artikel anhand seiner UnitId
    /// </summary>
    [HttpGet("{unitId:int}")]
    public IActionResult GetByUnitId(int unitId)
    {
        if (articlesService.TryGetArticleUnit(unitId, out var articleUnit))
        {
            return Ok(articleUnit);
        }
        return NotFound(new { Message = "Artikel mit dieser UnitId nicht gefunden", UnitId = unitId });
    }

    [HttpPost("upload-articles", Name = nameof(UploadArticleList))]
    public async Task<ActionResult> UploadArticleList([FromForm] DtoArticlesFile articlesFile)
    {
        if (articlesFile.File == null || articlesFile.File.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }
        
        var tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            await using (var fs = new FileStream(
                             tempFileName,
                             FileMode.CreateNew,
                             FileAccess.ReadWrite,
                             FileShare.ReadWrite,
                             bufferSize: 4096))
            {
                await articlesFile.File.CopyToAsync(fs);
            }

            var count = await articlesService.ImportFromJsonFileAsync(tempFileName);
            return Ok(new { ImportedCount = count, Message = $"{count} Artikel erfolgreich importiert" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
        finally
        {
            System.IO.File.Delete(tempFileName);
        }
    }
}

public class DtoArticlesFile
{
    public string FileName { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}

