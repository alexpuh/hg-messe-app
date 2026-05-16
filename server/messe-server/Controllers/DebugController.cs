using Herrmann.MesseApp.Server.Filters;
using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

/// <summary>
/// Debug endpoint for development and E2E testing.
/// Returns 404 Not Found in non-Development environments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[DevelopmentOnly]
public class DebugController(
    ScanSessionService scanSessionService,
    SignalNotificationService signalNotificationService,
    ILogger<DebugController> logger) : ControllerBase
{
    /// <summary>
    /// Simulates a barcode scan for the current active session.
    /// Replicates the same pipeline as the physical scanner:
    /// AddBarcodeAsync → SignalNotificationService.
    /// </summary>
    /// <param name="ean">EAN code to scan</param>
    [HttpPost("scan")]
    public async Task<IActionResult> SimulateScan([FromQuery] string ean)
    {
        if (string.IsNullOrWhiteSpace(ean))
            return BadRequest("ean ist erforderlich.");

        var currentSession = await scanSessionService.GetCurrentScanSessionAsync();
        if (currentSession == null)
        {
            logger.LogWarning("Debug scan: no active session");
            return BadRequest("Keine aktive Session");
        }

        var (success, errorMessage) = await scanSessionService.AddBarcodeAsync(currentSession.Id, ean);

        if (success)
        {
            await signalNotificationService.SendBarcodeScanned(ean);
            logger.LogInformation("Debug scan successful: EAN={Ean}, SessionId={SessionId}", ean, currentSession.Id);
            return Ok(new { Message = "Barcode erfolgreich verarbeitet", Ean = ean, SessionId = currentSession.Id });
        }
        else
        {
            await signalNotificationService.SendBarcodeError(ean, errorMessage);
            logger.LogWarning("Debug scan failed: EAN={Ean}, Error={Error}", ean, errorMessage);
            return BadRequest(new { Message = errorMessage, Ean = ean });
        }
    }
}
