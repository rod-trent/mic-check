using Microsoft.AspNetCore.Mvc;

namespace MicCheckBot.Controllers;

/// <summary>
/// Lets the panel ask the bot to join a meeting (by its join URL) so it can start
/// listening. The actual join with application-hosted media is implemented in the
/// media-platform module; this skeleton returns 501 with a pointer so the contract is
/// visible and the panel can be wired against it.
/// </summary>
[ApiController]
[Route("api/calls")]
public sealed class CallsController(ILogger<CallsController> logger) : ControllerBase
{
    [HttpPost("join")]
    public IActionResult Join([FromBody] JoinRequest request)
    {
        logger.LogInformation("Join requested for meeting URL: {Url}", request.MeetingJoinUrl);
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Media-platform module not deployed. See bot/media-platform/README.md.",
            request.MeetingJoinUrl,
        });
    }
}

public sealed record JoinRequest(string MeetingJoinUrl);
