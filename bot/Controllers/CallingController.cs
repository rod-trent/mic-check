using Microsoft.AspNetCore.Mvc;

namespace MicCheckBot.Controllers;

/// <summary>
/// Webhook that Microsoft Graph Communications posts call notifications to. In the full
/// system the media-platform module owns the call lifecycle (answer/join + media session)
/// and this endpoint hands notifications to it. Here it acknowledges so the signaling
/// service builds and runs without the Windows media SDK present.
/// </summary>
[ApiController]
[Route("api/calling")]
public sealed class CallingController(ILogger<CallingController> logger) : ControllerBase
{
    [HttpPost]
    public IActionResult OnNotification()
    {
        // TODO: forward to the media-platform module (see bot/media-platform/README.md),
        // which parses the CommsNotifications, answers/joins with application-hosted media,
        // and attaches the AudioPipeline to each participant's unmixed audio stream.
        logger.LogInformation("Received /api/calling notification (media-platform module not deployed).");
        return Accepted();
    }
}
