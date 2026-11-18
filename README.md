# TheWicken — Slay the Spire 2 character mods

**Slay the Spire 2** character mods — Godot 4.5 / C# (net9.0), loaded by the game at runtime. One mod per character, each its own Godot project:

| Folder | Mod | Notes |
|---|---|---|
| `TheWitch/` | The Witch | released on the Steam Workshop |
| `TheAugur/` | The Augur | prototype (Foretell / Omen / Augury) |
| `BirdDebug/` | Bird Debug Tools | dev only, never shipped: launch flags, smoke tests, FX/Icon labs |
| `Common/` | — | source shared by every character mod (analytics, Workshop self-update) |

## Setup

1. Install Slay the Spire 2 (the build finds it via the Steam registry; override with `local.props` if needed).
2. Place the decompiled game source at `gamedata/` (local-only, gitignored — proprietary, never committed).
3. **Required — run the project setup** (VS Code task **Setup: Project**, idempotent):

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\setup.ps1            # -Character Witch|Augur|All
   ```

   It creates the gamedata junctions (below) and the editor addons (`addons/mega_text`, plus `addons/spine` for the Witch) in each project. `tools\setup-gamedata-links.ps1` runs the junction step on its own.

   The junction step creates per-project junctions `scenes/`, `materials/`, `shaders/`, `images/`, `src/`, `themes/`, `fonts/` → `gamedata/<same>` so the Godot editor resolves base-game `res://` paths referenced by mod scenes (at runtime they resolve from the game's pck regardless). The script is idempotent — re-run it any time the editor reports broken dependencies (e.g. `Scene 'res://themes/…' has broken dependencies: res://fonts/…`), which means a junction is missing.

   The junctions are gitignored and excluded from export in each `export_presets.cfg` — don't remove those excludes, or game assets get packed into the mod `.pck`.

4. **For releases — cache both game branches** (optional for day-to-day work). The mods are one source tree built against two game versions, the public branch and the beta branch, so a release needs both installs side by side. Steam keeps only one, so each is copied into `game-installs/<branch>/` (gitignored, ~3 GB each):

   ```powershell
   # Steam > Slay the Spire 2 > Properties > Betas: pick the branch, let it update, then:
   powershell -ExecutionPolicy Bypass -File tools\setup-game-install.ps1 -Branch beta
   # switch Steam to the other branch, let it update, then:
   powershell -ExecutionPolicy Bypass -File tools\setup-game-install.ps1 -Branch public
   ```

   The script refuses if Steam is on a different branch than the one named. Re-run it whenever the game patches that branch. Builds and tools pick an install with `-p:Sts2Branch=public|beta` / `-Branch public|beta`; a beta build defines `STS2_BETA`, which is how the code handles game-API differences (`#if STS2_BETA`). Without a cached `public` copy the Steam install stands in for it.

## Build

```powershell
dotnet build                              # every project (TheWicken.slnx); each deploys into the game's mods/ folder
dotnet build TheWitch/TheWitch.csproj     # one character
dotnet publish TheWitch/TheWitch.csproj   # additionally exports that character's .pck (needs Godot 4.5.x mono, see Directory.Build.props)
./tools/launch.ps1 -Character Augur -Build publish -Bootstrap   # publish + launch straight into a combat
```

Test by launching Slay the Spire 2 — there is no standalone app.

## Release

```powershell
./tools/deploy.ps1 -VerifyOnly     # publish + smoke tests + AutoSlay on both game branches; touches nothing else
./tools/deploy.ps1                 # full release of the committed version (after /version-bump)
./tools/deploy.ps1 -Resume -Ack    # continue after a manual gate; -Resume alone retries a failed stage
```

`deploy.ps1` verifies both branches first, then tags, uploads each build to the Workshop and pushes. Failures stop the run and are posted (stage, cause, log path) to the Discord webhook in `tools/deploy-webhook.local.txt` (gitignored) or `$env:DEPLOY_WEBHOOK_URL`. Logs and state: `build/deploy/<ModId>-<version>/`.
