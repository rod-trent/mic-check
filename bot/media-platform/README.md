# Media platform module (Windows + Graph Communications Media SDK)

This is the part of Tier 2 that actually captures per-participant audio. It is **separate
from the main `MicCheckBot` project on purpose** because it has hard requirements the
signaling service doesn't:

- **Windows only** — the media platform ships native Windows components.
- **Public media endpoint** — a reachable DNS name + TCP port (default 8445) with a valid
  **TLS certificate**. It cannot run behind a standard App Service; use an Azure **VM** or
  **VMSS** (or Cloud Service).
- **Azure AD app permissions** (admin-consented) — see [../../docs/TIER2.md](../../docs/TIER2.md).

## Packages

Create a Windows-targeted project (e.g. `net8.0-windows`) and reference:

```
Microsoft.Graph.Communications.Calling
Microsoft.Graph.Communications.Calling.Media
Microsoft.Graph.Communications.Client
Microsoft.Skype.Bots.Media
```

> Pin versions from the current [Microsoft Graph Communications samples](https://github.com/microsoftgraph/microsoft-graph-comms-samples).
> Type names and namespaces drift between SDK releases.

## How it fits

```
Teams meeting ──audio──▶ Media platform (this module) ──AudioFrame──▶ AudioPipeline
                                                                          │
                                                          (same pipeline the mock uses)
                                                                          ▼
                                                     VAD ▶ SpeakingStateBroadcaster ▶ SignalR ▶ panel
```

[`GraphMediaSource.cs.sample`](GraphMediaSource.cs.sample) shows the shape: build a local
media session with `ReceiveUnmixedMeetingAudio = true`, handle `AudioMediaReceived`, copy
each unmixed buffer to PCM16, resolve the participant, and call `AudioPipeline.ProcessAsync`.
Rename it to `.cs` and compile it under the `MEDIA_PLATFORM` symbol in your Windows project.

## Wiring

- Reference the `AudioPipeline`, `IVoiceActivityDetector`, and `SpeakingStateBroadcaster`
  types from the main project (project reference), so the VAD + SignalR half is shared.
- Point `CallingController` / `CallsController` in the main project at your
  `GraphMediaSource` instance (DI), replacing the 501/Accepted stubs.
- Set `Bot:EnableMockAudio=false` so only real audio flows.

## Local testing without Azure

You can't run the media platform locally without the infra. Until it's stood up, use the
**mock source** in the main project (`Bot:EnableMockAudio=true`) to develop and test the
panel↔hub↔VAD path end to end.
