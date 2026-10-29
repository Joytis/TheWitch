# Live2D for the Witch

Status: **ships with the Witch (Windows x64), no Witch model yet.** It switches on by itself
wherever the extension is installed: after `setup-live2d.ps1` + a build locally, and for players
from the Workshop bundle (`bundle-workshop.ps1` ships `live2d/` next to the dll). Until a model
lands in `res://TheWitch/live2d/witch/`, players keep the static sprite.

## Plugin

[GDCubism](https://github.com/MizunagiKB/gd_cubism) (MIT) is the only maintained Godot 4
Live2D plugin. It is a GDExtension wrapping Live2D's Cubism SDK for Native. Its latest release
is v0.9.1 (2025-03), for Godot 4.3+, built here against SDK `5-r.3`. The project has been
quiet since that release, so treat it as something we may have to maintain ourselves. The
alternative, Live2D's official Cubism SDK for Unity/Web, has no Godot path.

GDCubism's `GDCubismUserModel` (a `Node2D` since 0.9, no SubViewport) loads a `*.model3.json`
and gives you `.moc3` playback, physics, pose, expressions, motions (`start_motion(group, no,
priority)`, `motion_finished` signal), plus effect nodes (`GDCubismEffectEyeBlink`, `...Breath`,
`...TargetPoint`, `...HitArea`).

## The fork

We ship our own build, not stock GDCubism, so another mod shipping GDCubism can't collide with
ours. Two copies would otherwise fight: Godot keeps whichever extension registers a class name
first (the other one only logs "Class already registered"), so both mods would run on one dll
whatever its version. And a pck loaded later overrides earlier files at the same `res://` path,
so both would also share one set of shaders.

The fork is upstream `v0.9.1` plus scripted transforms in `setup-live2d.ps1` (`Invoke-ForkPatch`),
applied to a fresh clone. No fork repo to maintain.

| Upstream | Fork |
|---|---|
| `GDCubism*` classes (`GDCubismUserModel`, `GDCubismEffectBreath`, ...) | `WitchCubism*` (`Live2DRuntime.ClassPrefix`) |
| shaders at `res://addons/gd_cubism/res/shader/` | `res://TheWitch/live2d/shader/` (`Live2DRuntime.ShaderResDir`) |
| `gd_cubism.gdextension`, `libgd_cubism.*.dll` | `witch_cubism.gdextension`, `libwitch_cubism.*.dll` |

The patch throws if upstream stops matching what it expects. Bump `$ForkRev` in the script when
the transforms change; the checkout is then re-cloned and rebuilt. Left as-is: the entry symbol
(looked up per dll) and the `*.motion3.json` → `Animation` resource loader (both copies would
register one; we never load motions through `ResourceLoader`, GDCubism reads them itself).

## How it hooks into the mod

A GDExtension is a native dll, and a native dll can't live inside a `.pck`. So:

| Piece | Where | How it gets there |
|---|---|---|
| `witch_cubism.gdextension` + `bin/libwitch_cubism.windows.release.x86_64.dll` + `licenses/` | `mods/TheWitch/live2d/` (disk) | `tools/live2d/setup-live2d.ps1` stages into `TheWitch/live2d/` (gitignored); the csproj `CopyLive2DToModsFolder` target copies it on build; `bundle-workshop.ps1` ships it as `content/live2d/` |
| model folders (`*.model3.json`, `.moc3`, textures, motions) | inside `TheWitch.pck`: `res://TheWitch/live2d/<name>/` (real models, commit like other art) or `.../live2d/samples/<name>/` (SDK samples, gitignored) | export `include_filter="*.moc3"`; json/png are normal resources. **Not on disk:** the game's ModManager parses every `*.json` under `mods/` as a mod manifest and logs an error per file. `bundle-workshop.ps1` refuses to run while `samples/` exists |
| shaders (`res://TheWitch/live2d/shader/`, patched in by the fork) | inside `TheWitch.pck` | setup script copies them to `TheWitch/TheWitch/live2d/shader/` (gitignored). Needs `dotnet publish` |

At runtime ([TheWitchCode/Live2D/](../TheWitch/TheWitchCode/Live2D/)):

- `Live2DRuntime` loads the extension on first use with `GDExtensionManager.LoadExtension(<abs path>)`
  (Windows x64 only; other platforms skip straight to the sprite). The game never registers it. The game's own Godot build already runs native extensions (FMOD,
  Spine), and runtime loading has been verified in-game.
- `WitchLive2DVisualsPatch` is a Harmony postfix on `CharacterModel.CreateVisuals`, the same
  shape as `WitchPetVisualsPatch`. It runs whenever the extension loads. Model lookup:
  `res://TheWitch/live2d/witch/`, else the first folder under `.../live2d/samples/`.
- `NWitchLive2D.Attach` parents the model under the `%Visuals` sprite and hides only the
  sprite's own texture (`SelfModulate`). Modulate, visibility and the `VisualsRoot` animation
  tracks (lunge, recoil, death tumble) all still apply. The model is scaled to 300 px with its
  feet on the creature origin, and blink and breath effect nodes are added.
- The driver node watches the existing `witch_visuals.tscn` AnimationTree state machine. It
  mirrors each state entry (`Attack`, `AttackSilent`, `Hit`, `PowerUp`, `Dead`) onto the
  **Live2D motion group of the same name**, and returns to a random `Idle` motion when a
  one-shot finishes. Sample models fall back to `TapBody`.

## What the Witch's model needs (artist contract)

- Export from Cubism Editor 5.x as moc3. The `5-r.3` core reads moc3 up to v5.0.
- `model3.json` motion groups: `Idle` (several variations, picked at random), `Attack`, `Hit`,
  `PowerUp`, `Dead`, optional `AttackSilent`.
- Standard parameter ids (`ParamEyeLOpen`, `ParamBreath`, …) so the blink and breath effects work.
- Feet at the bottom of the canvas, because the canvas bottom lands on the creature origin.

## Run it

```powershell
./tools/setup.ps1 -Character Witch      # or tools/live2d/setup-live2d.ps1; license prompt once, then download + build + stage Hiyori
dotnet publish TheWitch/TheWitch.csproj # pck with shaders + sample; copies live2d/ next to the dll
./tools/launch.ps1 -Character Witch -Solo -Bootstrap -TailLog   # = VS Code task "Debug: Combat"
```

`setup-live2d.ps1` pins GDCubism `v0.9.1` + SDK `5-r.3` and caches everything under `build/live2d/`
(clone, SDK zip, build). `-AcceptLive2DLicense` skips the prompt, `-SdkZip` uses a
hand-downloaded zip, and `-Rebuild` starts the clone and build over.

Log lines are tagged `[live2d]`. Any failure (missing files, load error) falls back to the
static sprite.

## Shipping

- **Licensing:** cleared for the Witch as a free product. The bundle carries the GDCubism,
  godot-cpp and Cubism SDK license/notice files in `live2d/licenses/`. Sample models are under
  the Free Material License and must not ship as the Witch.
- `bundle-workshop.ps1` refuses to bundle the Witch without the staged extension and shaders,
  while `samples/` exists, or when the pck still contains sample models or lacks the shaders.
- Workshop updates (untested): a loaded native dll is locked on Windows. It only loads at the
  Witch's first visuals (combat, game over, ...), so an update applied before that should still
  replace it; Steam skips files whose content didn't change anyway.

## Open items

- Platforms: Windows x64 only. macOS/Linux players would need their own builds plus extra
  `[libraries]` entries; until then they get the sprite.
- Rest site, merchant and character select are still the static scenes.
- Editor preview: the TheWitch Godot project doesn't load the extension, so models can't be
  placed or previewed in the editor yet.
- `NCreatureVisuals` effects that swap `_body.Material` (liquid overlay) don't reach the model.
