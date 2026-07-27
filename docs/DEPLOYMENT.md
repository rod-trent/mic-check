# Deploying Mic Check to your organization

This guide covers getting Mic Check off your own machine (sideloading) and into the
hands of your whole org — optionally **auto-installed and pinned** so it's always
present, without every user uploading it themselves.

> **Reality check on "always in every meeting":** Teams does **not** let any
> third-party meeting side-panel app auto-open in every meeting. What org deployment
> buys you is that the app is **installed and pinned for everyone** — always one click
> away in the meeting **Apps** picker and in the app bar (via the personal tab) — not
> that it auto-launches inside each meeting. Add it to a **recurring meeting** once and
> it persists for the whole series.

## Distribution options at a glance

| Path | Audience | Who does it | Microsoft review? |
|---|---|---|---|
| Sideload (custom app upload) | Just you | You | No |
| **Org app catalog** | Your tenant | A Teams admin | No |
| Microsoft Teams Store | Public | You submit | Yes |

Most internal rollouts want the **org app catalog** path below.

---

## Prerequisites

- The built package: `build/appPackage.zip` (see the [README](../README.md#build-the-teams-package)).
- A **Teams Administrator** (or Global Administrator) for the catalog + policy steps.
- The app hosted on HTTPS (the included GitHub Pages workflow already does this).

---

## 1. Upload to the org app catalog

1. Go to the **Teams admin center** → <https://admin.teams.microsoft.com>.
2. **Teams apps → Manage apps**.
3. Click **Upload new app** (top of the list) → **Upload** → select `appPackage.zip`.
4. The app appears in Manage apps as a **Custom** app, publisher **Rod Trent**.

At this point the app exists in the tenant catalog but may not yet be allowed or
pinned for users. Continue below.

## 2. Allow the app (App permission policy)

If your tenant blocks third-party/custom apps by default:

1. **Teams apps → Permission policies**.
2. Edit the relevant policy (e.g. **Global (Org-wide default)**) or create a new one.
3. Under **Custom apps**, make sure Mic Check is **Allowed** (either allow all custom
   apps, or add Mic Check specifically).
4. **Save**.

## 3. Auto-install and pin (App setup policy)

This is what makes the app "just be there" for users.

1. **Teams apps → Setup policies**.
2. Edit **Global (Org-wide default)** or create a policy for a pilot group.
3. Under **Installed apps** → **Add apps** → search **Mic Check** → **Add**. This
   **pre-installs** it for users the policy applies to.
4. Under **Pinned apps** → **Add apps** → add **Mic Check** to pin it in the app bar
   (this surfaces the personal tab). Reorder as desired.
5. **Save**.

## 4. Assign the policy (if you made a pilot policy)

If you edited the Global default, you're done. For a custom/pilot policy:

1. **Teams apps → Setup policies** → your policy → **Manage users**, **or**
2. Assign to a **group** via **Groups → policy assignment** for a larger rollout.

## 5. Verify

- Policy/app propagation can take a few hours (sometimes up to 24h).
- As a target user, restart Teams. You should see **Mic Check** pinned in the app bar
  (personal tab) and available in any meeting's **Apps** picker.
- Open the personal tab → **Start mic check** → allow the mic prompt → confirm the meter
  moves.

---

## Updating the app later

Mic Check uses a **stable app id** (`.appid.txt`), so updates replace the existing app:

1. Bump `version` in `appPackage/manifest.json` (e.g. `1.1.0` → `1.1.1`). Teams requires
   a higher version to accept an update.
2. Rebuild: `pwsh scripts/package.ps1 -BaseUrl "https://rod-trent.github.io/mic-check"`.
3. **Manage apps → Mic Check → Update** (or **Upload new app** and pick the new zip).
4. Because the host content (GitHub Pages) is separate from the manifest, **pure UI/JS
   changes to `src/` go live on the next Pages deploy with no re-upload needed** — you
   only repackage when the *manifest* changes (name, tabs, permissions, version).

---

## Enabling custom-app upload (for sideload testing)

If teammates can't sideload for testing, an admin enables it:

- **Teams apps → Setup policies** → the user's policy → toggle **Upload custom apps**
  to **On**.

---

## Publishing to the Microsoft Teams Store (public)

Only needed if you want Mic Check discoverable to *all* Teams users worldwide. Summary
(full checklist lives in the [README](../README.md#before-you-publish-to-the-teams-store)):

1. Create/verify a **Partner Center** account and a **Publisher** profile.
2. Replace placeholder icons with real brand artwork; prepare **screenshots**, a long
   description, and a **valid privacy policy + terms** (already hosted on Pages).
3. Complete **Publisher Attestation** (security/compliance questionnaire).
4. Submit via **Teams admin center → Manage apps → Publish**, or through Partner Center,
   and respond to validation feedback.

Expect an iterative review. For internal-only use, the org catalog path above is faster
and needs no Microsoft review.

---

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| App not visible after assigning policy | Propagation delay (wait up to 24h) or user needs to restart Teams |
| "Your org doesn't allow custom apps" | App permission policy blocks custom apps (step 2) |
| No **Apps** button in a meeting | Meeting-apps or permission policy gating |
| Mic never prompts | OS-level mic permission for Teams, not the package |
| Update rejected | `version` not incremented above the installed one |
