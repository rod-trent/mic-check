/*
 * Can U Hear Me Now — Mic Check side panel
 * -----------------------------------------
 * Tier 1: confirms the user's OWN mic is producing audio, using the Web Audio API.
 * All analysis is local — no audio is recorded, buffered to disk, or sent anywhere.
 *
 * The Teams JS SDK is used for: theme sync, meeting context, and (best-effort) the
 * local speaking-state signal as a second confirmation.
 */
(() => {
  "use strict";

  const teams = window.microsoftTeams;

  // --- DOM refs ---------------------------------------------------------------
  const el = (id) => document.getElementById(id);
  const idle = el("idle");
  const live = el("live");
  const startBtn = el("startBtn");
  const stopBtn = el("stopBtn");
  const permHint = el("permHint");
  const meterFill = el("meterFill");
  const meter = el("meter");
  const verdict = el("verdict");
  const verdictText = el("verdictText");
  const teamsSignal = el("teamsSignal");
  const teamsSignalText = el("teamsSignalText");
  const deviceName = el("deviceName");
  const statusLine = el("statusLine");

  // --- Audio state ------------------------------------------------------------
  let audioCtx = null;
  let stream = null;
  let analyser = null;
  let rafId = null;
  let running = false;

  // Rolling loudness tracking to decide the verdict.
  let peakRecent = 0;          // decays over time, captures recent loudness
  let framesAboveGood = 0;     // consecutive frames with a clearly audible level
  let framesSilent = 0;        // consecutive near-silent frames

  // Level thresholds on the 0..100 scale.
  const GOOD_LEVEL = 22;       // "clearly picking you up"
  const QUIET_LEVEL = 8;       // some signal but weak
  const GOOD_FRAMES = 8;       // ~130ms of good audio → confident yes
  const SILENT_FRAMES = 120;   // ~2s of silence → warn

  // ---------------------------------------------------------------------------
  // Teams initialization (non-fatal if it fails — the panel still works in a
  // plain browser for local testing).
  // ---------------------------------------------------------------------------
  async function initTeams() {
    if (!teams) return;
    try {
      await teams.app.initialize();
      const ctx = await teams.app.getContext();
      applyTheme(ctx.app.theme);
      teams.app.registerOnThemeChangeHandler(applyTheme);
      teams.app.notifySuccess();
    } catch (e) {
      // Running outside Teams (e.g. local dev) — that's fine.
      console.info("Teams SDK not active:", e && e.message);
    }
  }

  function applyTheme(theme) {
    document.body.classList.remove("theme-default", "theme-dark", "theme-contrast");
    if (theme === "dark") document.body.classList.add("theme-dark");
    else if (theme === "contrast") document.body.classList.add("theme-contrast");
    else document.body.classList.add("theme-default");
  }

  // Best-effort: ask Teams whether it thinks the local user is speaking.
  // Requires meeting context; guarded so failure is silent.
  function wireTeamsSpeakingSignal() {
    if (!teams || !teams.meeting || !teams.meeting.registerSpeakingStateChangeHandler) return;
    try {
      teams.meeting.registerSpeakingStateChangeHandler((speakingState) => {
        const isSpeaking = !!(speakingState && speakingState.isSpeaking);
        teamsSignal.hidden = false;
        teamsSignalText.textContent = isSpeaking
          ? "Teams confirms you're speaking — you're live."
          : "Teams isn't detecting speech right now.";
        teamsSignal.querySelector(".ts-dot").style.background =
          isSpeaking ? "var(--good)" : "var(--text-sub)";
      });
    } catch (e) {
      // Not in a meeting or permission not granted — rely on the local meter only.
    }
  }

  // ---------------------------------------------------------------------------
  // Mic check lifecycle
  // ---------------------------------------------------------------------------
  async function start() {
    permHint.hidden = false;
    setStatus("Requesting microphone…");
    startBtn.disabled = true;

    try {
      stream = await navigator.mediaDevices.getUserMedia({
        audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
      });
    } catch (err) {
      startBtn.disabled = false;
      permHint.hidden = false;
      setStatus(micErrorMessage(err));
      return;
    }

    // Label the active input device.
    const track = stream.getAudioTracks()[0];
    deviceName.textContent = track ? "🎙 " + (track.label || "Default microphone") : "";

    audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (audioCtx.state === "suspended") await audioCtx.resume();
    const src = audioCtx.createMediaStreamSource(stream);
    analyser = audioCtx.createAnalyser();
    analyser.fftSize = 2048;
    analyser.smoothingTimeConstant = 0.6;
    src.connect(analyser);

    idle.hidden = true;
    live.hidden = false;
    running = true;
    peakRecent = 0; framesAboveGood = 0; framesSilent = 0;
    setStatus("Listening — say something like you normally would.");
    wireTeamsSpeakingSignal();

    const buf = new Uint8Array(analyser.fftSize);
    const loop = () => {
      if (!running) return;
      analyser.getByteTimeDomainData(buf);

      // RMS of the waveform → perceived loudness.
      let sum = 0;
      for (let i = 0; i < buf.length; i++) {
        const x = (buf[i] - 128) / 128;
        sum += x * x;
      }
      const rms = Math.sqrt(sum / buf.length);

      // Map RMS to a 0..100 display scale (log-ish so quiet speech is visible).
      const level = Math.min(100, Math.round(Math.pow(rms, 0.5) * 180));

      // Smooth peak with decay so the verdict reflects recent loudness.
      peakRecent = Math.max(level, peakRecent * 0.92);

      meterFill.style.width = level + "%";
      meter.setAttribute("aria-valuenow", String(level));

      updateVerdict(level);
      rafId = requestAnimationFrame(loop);
    };
    rafId = requestAnimationFrame(loop);
  }

  function updateVerdict(level) {
    if (level >= GOOD_LEVEL) { framesAboveGood++; framesSilent = 0; }
    else if (level < QUIET_LEVEL) { framesSilent++; framesAboveGood = 0; }
    else { framesAboveGood = 0; framesSilent = 0; }

    if (framesAboveGood >= GOOD_FRAMES) {
      setVerdict("good", "You're being heard 👍");
    } else if (framesSilent >= SILENT_FRAMES) {
      setVerdict("silent", "Nothing coming through — check you're not muted");
    } else if (peakRecent >= QUIET_LEVEL && peakRecent < GOOD_LEVEL) {
      setVerdict("quiet", "Faint — move closer or speak up");
    } else if (verdict.dataset.state === "waiting") {
      setVerdict("waiting", "Listening…");
    }
  }

  function setVerdict(state, text) {
    verdict.dataset.state = state;
    verdictText.textContent = text;
  }

  function stop() {
    running = false;
    if (rafId) cancelAnimationFrame(rafId);
    if (stream) stream.getTracks().forEach((t) => t.stop());
    if (audioCtx) audioCtx.close().catch(() => {});
    stream = null; audioCtx = null; analyser = null;

    live.hidden = true;
    idle.hidden = false;
    startBtn.disabled = false;
    permHint.hidden = true;
    setStatus("Done. Re-run anytime before you speak up.");
  }

  function micErrorMessage(err) {
    const name = err && err.name;
    if (name === "NotAllowedError" || name === "SecurityError")
      return "Microphone blocked. Allow mic access for Teams, then try again.";
    if (name === "NotFoundError" || name === "OverconstrainedError")
      return "No microphone found. Check your input device.";
    return "Couldn't open the microphone: " + (err && err.message || "unknown error");
  }

  function setStatus(msg) { statusLine.textContent = msg; }

  // --- Wire up ----------------------------------------------------------------
  startBtn.addEventListener("click", start);
  stopBtn.addEventListener("click", stop);
  window.addEventListener("pagehide", stop);

  initTeams();
})();
