# Requesting Mic Check for your organization

Don't have Teams admin rights? Publishing a custom app to the org catalog requires a
**Teams Administrator**. Use the template below to ask yours to add Mic Check. The
technical steps for them live in [DEPLOYMENT.md](DEPLOYMENT.md).

## What to attach / link

- The app package: **`appPackage.zip`** — download from the repo's
  [**Releases**](https://github.com/rod-trent/mic-check/releases/latest), or attach the
  copy you built locally.
- App id: `56fcd8fb-8331-473c-8f78-beaa8e73a868` · version `1.1.0`

## Copy-paste request

> **Subject: Request to add an internal Teams app — "Mic Check"**
>
> Hi [admin],
>
> Could you add a small custom Teams app to our org app catalog? It's an in-meeting
> panel that shows a live mic-level meter so people can confirm they're being heard
> before a meeting starts — no more "can you hear me?"
>
> **Why it's low-risk:**
> - No data collection — mic audio is analyzed **locally in the panel**; nothing is
>   recorded, stored, or transmitted.
> - No bot, no Graph/mailbox permissions, no external calls beyond the app's own static
>   host.
> - Open source (MIT): https://github.com/rod-trent/mic-check — privacy policy:
>   https://rod-trent.github.io/mic-check/privacy.html
> - App id: `56fcd8fb-8331-473c-8f78-beaa8e73a868`, version 1.1.0.
>
> **What I'm asking for:**
> 1. **Teams admin center → Manage apps → Upload new app** → the attached
>    `appPackage.zip` (or download from the repo's Releases).
> 2. Optionally, an **app setup policy** to pin it for users so it's always available.
>
> Step-by-step (for you):
> https://github.com/rod-trent/mic-check/blob/main/docs/DEPLOYMENT.md
>
> Thanks!
> [you]

## What the admin needs to know

- **Data handling:** Mic Check uses the browser's Web Audio API to measure microphone
  input level only. No audio, transcript, or telemetry leaves the client. See the
  [privacy policy](https://rod-trent.github.io/mic-check/privacy.html).
- **Permissions requested:** only `devicePermissions: ["media"]` (the in-app microphone
  prompt). No bots, no Microsoft Graph resource-specific consent, no mailbox/calendar
  access.
- **Hosting:** static files served from GitHub Pages
  (`https://rod-trent.github.io/mic-check/`). Manifest changes require a re-upload;
  UI/JS changes deploy via Pages with no re-upload.
- **Effort:** a few minutes to upload; optional setup policy to pin it for users.
