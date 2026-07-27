namespace MicCheckBot.Audio;

/// <summary>
/// Emits synthetic audio for two fake participants who take turns "speaking", so the whole
/// panel↔hub↔VAD path can be exercised locally with no Azure, no meeting, and no media SDK.
/// Connect the panel in demo mode to meeting id "demo-meeting" to watch the state flip.
/// Disabled by setting Bot:EnableMockAudio=false.
/// </summary>
public sealed class MockParticipantAudioSource(
    AudioPipeline pipeline,
    ILogger<MockParticipantAudioSource> logger) : BackgroundService
{
    public const string MeetingId = "demo-meeting";

    private static readonly (string Id, string Name)[] Participants =
    [
        ("demo-user-1", "Ava (demo)"),
        ("demo-user-2", "Ben (demo)"),
    ];

    private const int SampleRate = 16000;
    private const int FrameSamples = SampleRate / 50; // 20 ms
    private const int FramesPerTurn = 100;            // ~2 s per speaker turn

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "MockParticipantAudioSource emitting demo audio for meeting '{MeetingId}'.", MeetingId);

        var rng = new Random(1234);
        var tick = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var speakerIndex = (tick / FramesPerTurn) % Participants.Length;
                for (var i = 0; i < Participants.Length; i++)
                {
                    var (id, name) = Participants[i];
                    var speaking = i == speakerIndex;
                    await pipeline.ProcessAsync(MakeFrame(id, name, speaking, rng));
                }

                tick++;
                await Task.Delay(20, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private static AudioFrame MakeFrame(string id, string name, bool speaking, Random rng)
    {
        var samples = new short[FrameSamples];
        if (speaking)
        {
            // Loud-ish noise, comfortably above the VAD threshold.
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = (short)rng.Next(-8000, 8000);
            }
        }
        // else: leave as silence.

        return new AudioFrame(MeetingId, id, name, samples, SampleRate);
    }
}
