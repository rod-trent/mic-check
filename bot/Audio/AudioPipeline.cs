using MicCheckBot.Services;

namespace MicCheckBot.Audio;

/// <summary>
/// The single entry point every audio source feeds: run VAD on a frame and broadcast the
/// resulting speaking state. Both the mock source and the real media-platform source call
/// <see cref="ProcessAsync"/>, so the signaling half of the bot is identical in dev and prod.
/// </summary>
public sealed class AudioPipeline(IVoiceActivityDetector vad, SpeakingStateBroadcaster broadcaster)
{
    public Task ProcessAsync(AudioFrame frame)
    {
        var speaking = vad.IsSpeech(frame);
        return broadcaster.UpdateAsync(frame.MeetingId, frame.ParticipantId, frame.DisplayName, speaking);
    }
}
