using Herrmann.MesseApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Herrmann.MesseApp.Server.Controllers;

/// <summary>
/// Debug endpoint for development and E2E testing only.
/// Returns 404 Not Found in non-Development environments.
/// Hidden from OpenAPI/Swagger document so it is never included in generated clients.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DebugController(
    ScanSessionService scanSessionService,
    SignalNotificationService signalNotificationService,
    IWebHostEnvironment env,
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
        if (!env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(ean))
            return BadRequest(new { Message = "ean is required." });

        var currentSession = await scanSessionService.GetCurrentScanSessionAsync();
        if (currentSession == null)
        {
            logger.LogWarning("Debug scan: no active session");
            return BadRequest(new { Message = "No active session.", Ean = ean });
        }

        try
        {
            var (success, errorMessage) = await scanSessionService.AddBarcodeAsync(currentSession.Id, ean);

            if (success)
            {
                await signalNotificationService.SendBarcodeScanned(ean);
                logger.LogInformation("Debug scan successful: EAN={Ean}, SessionId={SessionId}", ean, currentSession.Id);
                return Ok(new { Message = "Barcode processed successfully", Ean = ean, SessionId = currentSession.Id });
            }
            else
            {
                var message = errorMessage ?? "Scan failed";
                await signalNotificationService.SendBarcodeError(ean, message);
                logger.LogWarning("Debug scan failed: EAN={Ean}, Error={Error}", ean, message);
                return BadRequest(new { Message = message, Ean = ean });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Debug scan threw an unexpected exception: EAN={Ean}", ean);
            return StatusCode(500, new { Message = "Internal server error during scan simulation." });
        }
    }
}
