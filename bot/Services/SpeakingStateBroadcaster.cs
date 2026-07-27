using System.Collections.Concurrent;
using MicCheckBot.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MicCheckBot.Services;

/// <summary>
/// Tracks the last known speaking state per participant per meeting and pushes a
/// "speakingStateChanged" event to the meeting's SignalR group only on transitions
/// (so we don't spam a message every 20 ms frame).
/// </summary>
public sealed class SpeakingStateBroadcaster(IHubContext<PresenceHub> hub)
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _meetings = new();

    public async Task UpdateAsync(string meetingId, string participantId, string displayName, bool speaking)
    {
        var meeting = _meetings.GetOrAdd(meetingId, static _ => new ConcurrentDictionary<string, bool>());

        var changed = true;
        if (meeting.TryGetValue(participantId, out var previous))
        {
            changed = previous != speaking;
        }
        meeting[participantId] = speaking;

        if (!changed)
        {
            return;
        }

        await hub.Clients.Group(meetingId).SendAsync("speakingStateChanged", new
        {
            meetingId,
            participantId,
            displayName,
            audioPresent = speaking,
        });
    }
}
