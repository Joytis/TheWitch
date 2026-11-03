# Live2D for the Witch

Status: **ships with the Witch (Windows x64); first models in (2026-11-02 drop, idle rigs only,
no motions yet).** It switches on by itself wherever the extension is installed: after
`setup-live2d.ps1` + a build locally, and for players from the Workshop bundle
(`bundle-workshop.ps1` ships `live2d/` next to the dll). Three models, one per scene:

| Folder (`res://TheWitch/live2d/…`) | Scene | Mount node |
|---|---|---|
| `witch/` | `witch_visuals.tscn` (combat, game over, unlock, …) | `VisualsRoot/%Visuals` (the creature body) |
| `witch_rest/` | `witch_rest_site.tscn` | `Visuals` |
| `witch_shop/` | `witch_merchant.tscn` | `Visuals` |

**Live2D is required — there is no static-sprite fallback.** A platform without a build, a missing
extension or a missing model leaves the Witch invisible and logs a `[live2d]` error. So every
platform the game ships on (Windows, macOS, Linux/Steam Deck) needs a native build before release;
only Windows x64 exists so far.

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
| part draw order via `z_index` = Cubism render order (escapes the model: parts drew over the pause overlay/UI) | sibling order, `z_index` left at 0, re-sorted only when the order changes (`internal_cubism_renderer_2d.cpp`) |

The patch throws if upstream stops matching what it expects. Bump `$ForkRev` in the script when
the transforms change; the checkout is then re-cloned and rebuilt. Left as-is: the entry symbol
(looked up per dll) and the `*.motion3.json` → `Animation` resource loader (both copies would
register one; we never load motions through `ResourceLoader`, GDCubism reads them itself).

## How it hooks into the mod

A GDExtension is a native dll, and a native dll can't live inside a `.pck`. So:

| Piece | Where | How it gets there |
|---|---|---|
| `witch_cubism.gdextension` + `bin/libwitch_cubism.windows.release.x86_64.dll` + `licenses/` | `mods/TheWitch/live2d/` (disk) | `tools/live2d/setup-live2d.ps1` stages into `TheWitch/live2d/` (gitignored); the csproj `CopyLive2DToModsFolder` target copies it on build; `bundle-workshop.ps1` ships it as `content/live2d/` |
| model folders (`*.model3.json`, `.moc3`, textures, motions) | inside `TheWitch.pck`: `res://TheWitch/live2d/<name>/` (committed like other art) | export `include_filter="*.moc3"`; json/png are normal resources. **Not on disk:** the game's ModManager parses every `*.json` under `mods/` as a mod manifest and logs an error per file. |
| shaders (`res://TheWitch/live2d/shader/`, patched in by the fork) | inside `TheWitch.pck` | setup script copies them to `TheWitch/TheWitch/live2d/shader/` (gitignored). Needs `dotnet publish` |

At runtime ([TheWitchCode/Live2D/](../TheWitch/TheWitchCode/Live2D/)):

- `Live2DRuntime` loads the extension on first use with `GDExtensionManager.LoadExtension(<abs path>)`
  (Windows x64 only so far; other platforms log an error). The game never registers it. The game's own Godot build already runs native extensions (FMOD,
  Spine), and runtime loading has been verified in-game.
- **Mount point = a `Node2D` named `Visuals`** with metadata `live2d_model` (folder name above).
  In combat it is the creature body itself (`%Visuals`, unique name — `NCreatureVisuals` requires
  it, any `Node2D` will do), so every game effect aimed at the body acts on the model: revive fade,
  `SurroundedPower` flip (negates the body's X scale), Doom VFX reparent. It sits under
  `VisualsRoot`, so the lunge/recoil/death-tumble tracks apply too. The model hangs under it at
  native canvas size, bottom-centre of the canvas on the node's origin, so **`Visuals`'s
  position/scale in the .tscn is the only placement knob**.
- `WitchLive2DMount.TryMount` (in `WitchLive2DVisualsPatch.cs`) finds the node and mounts the model.
  Hooks: `CharacterModel.CreateVisuals` postfix (Witch only, same shape as `WitchPetVisualsPatch`),
  `NRestSiteCharacter._Ready` postfix (Witch players only), `NMerchantCharacter._Ready` postfix
  (gated by the `live2d_model` metadata; the node doesn't know its player). Model lookup:
  `res://TheWitch/live2d/<live2d_model>/`. Blink and breath effect nodes are added to every model.
- The driver node watches the existing `witch_visuals.tscn` AnimationTree state machine. It
  mirrors each state entry (`Attack`, `AttackSilent`, `Hit`, `PowerUp`, `Dead`) onto the
  **Live2D motion group of the same name**, and returns to a random `Idle` motion when a
  one-shot finishes. Rest site and merchant have no tree: they just loop `Idle`. A missing
  `AttackSilent` group falls back to `Attack`.

## Placing a model

Each `Visuals` mount node has an `EditorPreview` Sprite2D child showing the model's flattened rest pose
(`live2d/<model>/editor_preview.png`: the artist's flat PNG export at half size, drawn at scale 2
with `offset = (-w/2, -h)`), which covers exactly the rectangle the model's canvas will occupy.
Open the scene in the Godot editor, select `Visuals` and move/scale it until the preview
sits where she should stand (feet on the ground line, against `Bounds`/`Hitbox`, familiar
markers, etc.). Never move or scale `EditorPreview` itself, only `Visuals`. `WitchLive2DMount`
deletes `EditorPreview` at runtime before mounting, so it never shows in game.

- Don't save `Visuals` hidden: the model is its child, so she'd be invisible in game.
- When a drop changes a model's canvas, regenerate its preview from the new flat PNG
  (`magick <flat>.png -resize 50% editor_preview.png`) and fix the offset if the size changed.
- `publish`, then check in game (`./tools/launch.ps1 -Character Witch -Solo -Bootstrap` for
  combat; walk to a rest site / shop for the others).

## What the Witch's model needs (artist contract)

- Export from Cubism Editor 5.x as moc3. The `5-r.3` core reads moc3 up to v5.0.
- `model3.json` motion groups: `Idle` (several variations, picked at random), `Attack`, `Hit`,
  `PowerUp`, `Dead`, optional `AttackSilent`. Rest and shop models: `Idle` only (the rest
  model's "Fire glow" parameter is animated by its idle motion).
- Motions are exported as `*.motion3.json` and registered in `model3.json` under `Motions`:
  Cubism Editor → select the animation/scene → File → Export For Runtime → Export motion file
  ([Live2D manual](https://docs.live2d.com/en/cubism-editor-manual/export-moc3-motion3-files/)).
  The model3.json then needs re-exporting (or hand-editing) so `Motions` lists them by group.
- Standard parameter ids (`ParamEyeLOpen`, `ParamBreath`, …) so the blink and breath effects work,
  and the eye params listed in the `EyeBlink` group (the blink effect reads that group).
- Placement is tuned per scene via `Visuals`, so canvas framing no longer has to be exact; keep
  each model's canvas stable between drops so the tuned root values keep fitting.

## Run it

```powershell
./tools/setup.ps1 -Character Witch      # or tools/live2d/setup-live2d.ps1; license prompt once, then download + build + stage the extension
dotnet publish TheWitch/TheWitch.csproj # pck with shaders + models; copies live2d/ next to the dll
./tools/launch.ps1 -Character Witch -Solo -Bootstrap -TailLog   # = VS Code task "Debug: Combat"
```

`setup-live2d.ps1` pins GDCubism `v0.9.1` + SDK `5-r.3` and caches everything under `build/live2d/`
(clone, SDK zip, build). `-AcceptLive2DLicense` skips the prompt, `-SdkZip` uses a
hand-downloaded zip, and `-Rebuild` starts the clone and build over.

Log lines are tagged `[live2d]`. Any failure (missing files, load error) is logged as an error and
leaves the Witch invisible.

## Shipping

- **Licensing:** cleared for the Witch as a free product. The bundle carries the GDCubism,
  godot-cpp and Cubism SDK license/notice files in `live2d/licenses/`. The SDK's sample models (Hiyori etc.) are under
  the Free Material License and must never ship; setup no longer stages them.
- `bundle-workshop.ps1` refuses to bundle the Witch without the staged extension and shaders,
  while a stale `live2d/samples/` folder exists, or when the pck still contains sample models or
  lacks the shaders.
- Workshop updates (untested): a loaded native dll is locked on Windows. It only loads at the
  Witch's first visuals (combat, game over, ...), so an update applied before that should still
  replace it; Steam skips files whose content didn't change anyway.

## Open items

- Platforms: Windows x64 only. macOS/Linux players would need their own builds plus extra
  `[libraries]` entries; until then the Witch is invisible there. **Release blocker.** macOS also
  needs a check that the game's signed app allows loading a mod-built dylib (library validation).
- Character select is still the static scene.
- 2026-11-02 drop: no motion3.json yet (models stand still apart from blink/breath effects),
  `EyeBlink` group empty, no physics3.json, no .cmo3/.can3 sources.
- Combat `Cast` trigger: the Witch's cards fire `Cast` 21× but `witch_visuals.tscn` has no `Cast`
  animation/state, so BaseLib drops it and the driver never sees it. Add a `Cast` state before
  asking for a `Cast` motion.
- Rest site `HideFlameGlow` (Spine `_tracks/light_off`) has no Live2D equivalent yet.
- Editor preview is a static stand-in (see "Placing a model"), not the live model: the TheWitch
  Godot project doesn't load the extension (`live2d/.gdignore`), so motions can't be previewed.
- `NCreatureVisuals` effects that swap `_body.Material` (liquid overlay) don't reach the model.
