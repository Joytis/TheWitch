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
3. **Required — create the gamedata junctions in every project:**

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\setup-gamedata-links.ps1 -Character All
   ```

   This creates per-project junctions `scenes/`, `materials/`, `shaders/`, `images/`, `src/`, `themes/`, `fonts/` → `gamedata/<same>` so the Godot editor resolves base-game `res://` paths referenced by mod scenes (at runtime they resolve from the game's pck regardless). The script is idempotent — re-run it any time the editor reports broken dependencies (e.g. `Scene 'res://themes/…' has broken dependencies: res://fonts/…`), which means a junction is missing.

   The junctions are gitignored and excluded from export in each `export_presets.cfg` — don't remove those excludes, or game assets get packed into the mod `.pck`.

## Build

```powershell
dotnet build                              # every project (TheWicken.slnx); each deploys into the game's mods/ folder
dotnet build TheWitch/TheWitch.csproj     # one character
dotnet publish TheWitch/TheWitch.csproj   # additionally exports that character's .pck (needs Godot 4.5.x mono, see Directory.Build.props)
./tools/launch.ps1 -Character Augur -Build publish -Bootstrap   # publish + launch straight into a combat
```

Test by launching Slay the Spire 2 — there is no standalone app.
