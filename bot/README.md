# MicCheckBot — Tier 2 signaling service

The cross-side half of Mic Check: it takes per-participant "is this person's audio reaching
the meeting?" signals and pushes them to the panel in real time over SignalR. See the full
design in **[../docs/TIER2.md](../docs/TIER2.md)**.

This project builds and runs **cross-platform with no external NuGet packages** and no
Azure. The Windows-only media capture lives in [media-platform/](media-platform/).

## Run the local demo (no Azure, no meeting)

```bash
cd bot
dotnet run
```

By default in Development, a **mock audio source** emits two fake participants taking turns
"speaking" in meeting `demo-meeting`. Watch it drive the pipeline:

- Health: <http://localhost:5000/healthz> → `{"status":"ok","tier":2}`
- SignalR hub: `/hubs/presence`

Connect a SignalR client, call `JoinMeeting("demo-meeting")`, and you'll receive
`speakingStateChanged` events as Ava/Ben start and stop. The panel client
([../src/tier2.js](../src/tier2.js)) does exactly this.

## Endpoints

| Route | Purpose |
|---|---|
| `POST /api/calling` | Graph call notifications webhook (media module owns the lifecycle) |
| `POST /api/calls/join` | Panel asks the bot to join a meeting by join URL |
| `/hubs/presence` (SignalR) | Panel subscribes per-meeting; receives `speakingStateChanged` |
| `GET /healthz` | Liveness |

## Structure

```
Program.cs                     Host + DI + routes
Options/BotOptions.cs          Config (AAD app, media endpoint, cert)
Controllers/CallingController   /api/calling webhook (stub → media module)
Controllers/CallsController     /api/calls/join (stub → media module)
Hubs/PresenceHub.cs            SignalR hub the panel connects to
Audio/Vad.cs                   AudioFrame + energy VAD (per-participant smoothing)
Audio/AudioPipeline.cs         frame → VAD → broadcast (shared by mock + real source)
Audio/MockParticipantAudioSource.cs   synthetic audio for local demo
Services/SpeakingStateBroadcaster.cs  transition-only SignalR fan-out per meeting
media-platform/                Windows media capture (separate build) — the real source
```

## Config

`appsettings.json` → `Bot` section. Provide secrets via environment variables in
production (`Bot__AppSecret`, etc.), never commit them. `Bot:EnableMockAudio` is `true`
in Development and `false` in the base settings.
