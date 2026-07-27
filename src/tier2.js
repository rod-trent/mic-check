/*
 * Tier 2 panel client (OPTIONAL — disabled by default)
 * ----------------------------------------------------
 * Connects the Mic Check panel to the MicCheckBot SignalR hub so it can show a real,
 * receiving-side confirmation: "The meeting is receiving your audio ✅".
 *
 * This is inert unless a bot URL is configured. To enable:
 *   1. Deploy MicCheckBot (see ../bot and ../docs/TIER2.md).
 *   2. In index.html, before this script, set:
 *        <script>window.MICCHECK_BOT_URL = "https://your-bot-host";</script>
 *      and (for correlation) optionally window.MICCHECK_SELF_ID = "<the user's participant id>".
 *   3. Add this script:  <script src="tier2.js"></script>
 *   4. Update the page CSP to allow the bot origin in connect-src and the SignalR CDN in
 *      script-src (see docs/TIER2.md → "Wiring the panel").
 *
 * Kept separate from app.js so Tier 1 ships untouched.
 */
(() => {
  "use strict";

  const BOT_URL = window.MICCHECK_BOT_URL;
  if (!BOT_URL) return; // Tier 2 not configured — do nothing.

  const SIGNALR_CDN =
    "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js";

  // Demo default matches MockParticipantAudioSource.MeetingId.
  const DEMO_MEETING = "demo-meeting";

  function loadSignalR() {
    return new Promise((resolve, reject) => {
      if (window.signalR) return resolve(window.signalR);
      const s = document.createElement("script");
      s.src = SIGNALR_CDN;
      s.onload = () => resolve(window.signalR);
      s.onerror = () => reject(new Error("Failed to load SignalR client"));
      document.head.appendChild(s);
    });
  }

  async function resolveMeetingId() {
    // In a real meeting, derive a stable id from Teams context; fall back to the demo id.
    try {
      const ctx = await window.microsoftTeams.app.getContext();
      return ctx.meeting?.id || ctx.chat?.id || DEMO_MEETING;
    } catch {
      return DEMO_MEETING;
    }
  }

  function showBotVerdict(displayName, audioPresent) {
    const signal = document.getElementById("teamsSignal");
    const text = document.getElementById("teamsSignalText");
    if (!signal || !text) return;
    signal.hidden = false;
    text.textContent = audioPresent
      ? "✅ The meeting is receiving your audio (confirmed by the bot)."
      : `Bot isn't detecting audio from ${displayName || "you"} right now.`;
    const dot = signal.querySelector(".ts-dot");
    if (dot) dot.style.background = audioPresent ? "var(--good)" : "var(--text-sub)";
  }

  async function start() {
    const signalR = await loadSignalR();
    const meetingId = await resolveMeetingId();
    const selfId = window.MICCHECK_SELF_ID || null;

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BOT_URL.replace(/\/$/, "")}/hubs/presence`)
      .withAutomaticReconnect()
      .build();

    conn.on("speakingStateChanged", (evt) => {
      // If we know who "we" are, only react to our own participant; otherwise (demo) show any.
      if (selfId && evt.participantId !== selfId) return;
      showBotVerdict(evt.displayName, !!evt.audioPresent);
    });

    conn.onreconnected(() => conn.invoke("JoinMeeting", meetingId).catch(() => {}));

    try {
      await conn.start();
      await conn.invoke("JoinMeeting", meetingId);
      console.info("Tier 2 connected to bot for meeting", meetingId);
    } catch (e) {
      console.warn("Tier 2 bot connection failed:", e && e.message);
    }
  }

  start();
})();
