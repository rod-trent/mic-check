using System.Collections.Concurrent;

namespace MicCheckBot.Audio;

/// <summary>A frame of 16-bit PCM audio for one participant.</summary>
/// <param name="MeetingId">Correlates to the SignalR group the panel joined.</param>
/// <param name="ParticipantId">Stable participant identity (e.g. AAD object id / MRI).</param>
/// <param name="DisplayName">Human-readable name for logging/diagnostics.</param>
/// <param name="Samples">Mono PCM16 samples.</param>
/// <param name="SampleRate">Samples per second (typically 16000 from the media platform).</param>
public sealed record AudioFrame(
    string MeetingId,
    string ParticipantId,
    string DisplayName,
    short[] Samples,
    int SampleRate);

public interface IVoiceActivityDetector
{
    /// <summary>Returns true while the participant is producing speech-level audio.</summary>
    bool IsSpeech(AudioFrame frame);
}

/// <summary>
/// Simple energy-based VAD with per-participant attack/release smoothing so brief pauses
/// don't flap the "speaking" state. Good enough to answer "is this person's audio
/// reaching the meeting?"; swap for a WebRTC/ML VAD if you need noise robustness.
/// </summary>
public sealed class EnergyVoiceActivityDetector : IVoiceActivityDetector
{
    private const double SpeechRmsThreshold = 0.02; // ~ -34 dBFS
    private const int AttackFrames = 2;             // ~40ms of energy to latch on
    private const int ReleaseFrames = 10;           // ~200ms of quiet to latch off

    private readonly ConcurrentDictionary<string, State> _byParticipant = new();

    public bool IsSpeech(AudioFrame frame)
    {
        var rms = Rms(frame.Samples);
        var state = _byParticipant.GetOrAdd(frame.ParticipantId, static _ => new State());

        lock (state)
        {
            if (rms >= SpeechRmsThreshold) { state.Above++; state.Below = 0; }
            else { state.Below++; state.Above = 0; }

            if (!state.Speaking && state.Above >= AttackFrames) state.Speaking = true;
            else if (state.Speaking && state.Below >= ReleaseFrames) state.Speaking = false;

            return state.Speaking;
        }
    }

    private static double Rms(short[] samples)
    {
        if (samples.Length == 0) return 0;
        double sum = 0;
        foreach (var s in samples)
        {
            var x = s / 32768.0;
            sum += x * x;
        }
        return Math.Sqrt(sum / samples.Length);
    }

    private sealed class State
    {
        public int Above;
        public int Below;
        public bool Speaking;
    }
}
