# Contributing to Mic Check

Thanks for your interest! Mic Check is a small, dependency-light project, so getting
started is quick.

## Ground rules

- Be kind. We follow the spirit of the [Contributor Covenant](https://www.contributor-covenant.org/).
- Keep it simple. The panel is intentionally vanilla HTML/CSS/JS with **no build step**
  and **no runtime dependencies** — please keep it that way unless there's a strong reason.
- Privacy is a feature. Any change that would record, buffer, or transmit audio is a
  non-starter for the Tier 1 panel.

## Dev setup

You need [Node.js](https://nodejs.org) (for the dev server) and Python 3 (for icon
generation and manifest validation).

```bash
git clone https://github.com/rod-trent/mic-check.git
cd mic-check
npm run dev        # serves src/ at http://localhost:3000
```

Open http://localhost:3000 and click **Start mic check**. The panel degrades gracefully
outside Teams, so the meter and verdict work in a plain browser.

## Before you open a PR

Run the same checks CI runs:

```bash
python scripts/make-icons.py        # (re)generate icons
python scripts/validate-manifest.py # validate the Teams manifest
pwsh scripts/package.ps1 -BaseUrl "https://example.com/mic-check"
```

All three should pass. CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) runs
them on every push and pull request.

## Project layout

See the [README](README.md#project-layout). In short: `src/` is the hosted web app,
`appPackage/` is the Teams manifest + icons, `scripts/` builds the package.

## Commit & PR conventions

- Use clear, imperative commit messages ("Add high-contrast meter colors").
- One logical change per PR where possible.
- Update the README if you change behavior, setup, or the manifest.
- Link any related issue in the PR description.

## Ideas we'd love help with

- **Tier 2 media bot** — the real "they can hear you" backend (see the README roadmap).
- Accessibility improvements (screen-reader announcements for the verdict).
- Localization of the panel strings.
- Real branded icons to replace the generated placeholders.

Not sure where to start? Open an issue and say hi.
