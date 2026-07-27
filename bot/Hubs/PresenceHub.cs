using Microsoft.AspNetCore.SignalR;

namespace MicCheckBot.Hubs;

/// <summary>
/// The panel connects here. It joins a per-meeting group and then receives
/// "speakingStateChanged" events for participants in that meeting. Each panel filters to
/// its own participant id to show "the meeting is receiving your audio".
/// </summary>
public sealed class PresenceHub : Hub
{
    public Task JoinMeeting(string meetingId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, meetingId);

    public Task LeaveMeeting(string meetingId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, meetingId);
}
