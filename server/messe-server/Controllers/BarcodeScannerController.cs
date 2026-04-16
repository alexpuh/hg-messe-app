using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodeScannerController(
    BarcodeScannerService scannerService,
    ILogger<BarcodeScannerController> logger)
    : ControllerBase
{
    /// <summary>
    /// Gibt den Verbindungsstatus des Barcode-Scanners zurück
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var isConnected = scannerService.IsConnected();
        logger.LogInformation("Scanner-Status abgefragt: {Status}", isConnected ? "Verbunden" : "Nicht verbunden");
        
        return Ok(new
        {
            IsConnected = isConnected,
            Message = isConnected ? "Scanner ist verbunden" : "Scanner ist nicht verbunden"
        });
    }
}

