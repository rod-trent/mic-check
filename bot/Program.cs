using MicCheckBot.Audio;
using MicCheckBot.Hubs;
using MicCheckBot.Options;
using MicCheckBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));

builder.Services.AddControllers();
builder.Services.AddSignalR();

// CORS: the Teams tab (panel) connects to the SignalR hub from the app's host.
// TODO: replace the permissive dev policy with the exact panel origin(s) in production.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(_ => true)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Audio pipeline: source -> VAD -> broadcaster -> SignalR.
builder.Services.AddSingleton<IVoiceActivityDetector, EnergyVoiceActivityDetector>();
builder.Services.AddSingleton<SpeakingStateBroadcaster>();
builder.Services.AddSingleton<AudioPipeline>();

// Mock source for local, Azure-free end-to-end testing. Disable in production once the
// media-platform module is feeding real AudioFrames into the AudioPipeline.
if (builder.Configuration.GetValue("Bot:EnableMockAudio", true))
{
    builder.Services.AddHostedService<MockParticipantAudioSource>();
}

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<PresenceHub>("/hubs/presence");
app.MapGet("/healthz", () => Results.Ok(new { status = "ok", tier = 2 }));

app.Run();
