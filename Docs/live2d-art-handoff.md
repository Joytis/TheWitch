# Live2D art drop: intake handoff

For the agent who brings the artist's next Live2D drop into the Witch. Background and architecture
are in [live2d.md](live2d.md); this page is the intake checklist and the traps found on the first
drop (2026-11-02, `Witch_Idle_Animations1`).

## Where things stand (2026-11-03)

- Three models ship, one per scene, each mounted under a `Node2D` named **`Visuals`** that carries
  `metadata/live2d_model` (the folder name). There is **no static-sprite fallback**: if Live2D
  fails, the Witch is invisible and a `[live2d]` error is logged.

  | Folder `TheWitch/TheWitch/live2d/…` | Scene | Canvas (1st drop) |
  |---|---|---|
  | `witch/` | `scenes/witch_visuals.tscn` (combat; `Visuals` is the creature body, `%Visuals`) | 2100×2900 = PSD |
  | `witch_rest/` | `scenes/witch_rest_site.tscn` | 2100×2300 = PSD |
  | `witch_shop/` | `scenes/witch_merchant.tscn` | **1800×2600 ≠ PSD 2100×2900** (PSD scaled 0.939, shifted -125,-41) |

- The first drop's models are **rigs without motions** (`motion groups: { }` in the log), and
  every part exported as **Normal** blend. Two stopgaps cover that, both meant to be deleted:
  1. [Live2DBlendOverrides.cs](../TheWitch/TheWitchCode/Live2D/Live2DBlendOverrides.cs): a table
     of ArtMesh ids, per model, whose shader is swapped to Multiply/Additive (matched to the PSD's
     Multiply / Linear Dodge layers by bounding box).
  2. **Placeholder breathing**: GDCubism's `EffectBreath` on every model (head sway on
     `ParamAngleX/Y/Z`, `ParamBodyAngleX`, `ParamBreath`), combat at `live2d_breath_intensity = 1.5`
     (metadata on its `Visuals`). The old `Idle` scale keyframes in `witch_visuals.tscn` were
     removed in favour of it; the `Idle` animation is now empty but must stay.
- The artist was asked (by the user) to rework the models: legacy blend modes and motions.

## What the next drop should contain

Check it against this; report gaps to the user rather than working around them silently.

- Per model: `*.model3.json`, `*.moc3`, `*.cdi3.json`, texture folder, ideally `*.physics3.json`,
  and `*.motion3.json` files listed under `Motions` in the model3.json.
- Motion groups: combat `Idle` (several variations OK, picked at random), `Attack`, `Hit`,
  `PowerUp`, `Dead` (should end on a held pose), optional `AttackSilent`. Rest and shop: `Idle`
  (the rest `Idle` also animates the `Fire glow` parameter).
- Blend modes set as **legacy** ("Cubism 5.2 blend mode or earlier" / "Multiply (Before 5.3)").
- `EyeBlink` group in model3.json listing the eye parameter(s) (it was `Ids: []`, so the blink
  effect did nothing).
- The PSDs and flat PNGs again (needed for blend checks and editor previews); `.cmo3`/`.can3`
  sources if the artist will share them.

## Intake steps

1. **Inventory.** Unpack into a fresh folder outside the repo. Per model check:
   - moc3 version byte: `xxd -s 4 -l 1 -p <file>.moc3` must be **`05` or lower** (our Core is SDK
     5-r.3; Cubism 5.3 exports are v6 and will not load at all — see "5.3" below).
   - `model3.json`: `Motions` present and grouped as above; `Groups` → `EyeBlink` has ids.
   - Parameters in `cdi3.json` (the first drop had only Angle X/Y, plus Blink/Breath on combat and
     Fire glow on rest).
2. **Copy** each model's files into its folder above, replacing the old model files. Keep the
   folder names (they're the scene metadata); file names are free (the first `*.model3.json` in
   the folder is used). Leave `editor_preview.png` until step 5.
3. **Build + run**: `dotnet publish TheWitch/TheWitch.csproj` (game must be closed — the loaded
   Live2D dll is locked), then:
   - combat: `./tools/launch.ps1 -Character Witch -Solo -Bootstrap`
   - rest / shop: run the game exe with `-bird-character=Witch -bird-shot=restsite` (or `shop`)
     `-bird-shot-out=<png> -bird-shot-size=1920x1080` — it starts a run, enters the room, saves
     a screenshot and quits (BirdDebug `MarketingShots.cs`).
   Then read `%APPDATA%\SlayTheSpire2\logs\godot.log` for, per model:
   ```
   [live2d] …model3.json: canvas (W, H), origin (…), mounted under Visuals
   [live2d] model ready, motion groups: { "Idle": 3, … }
   [live2d] …model3.json: parts by blend shader: 2d_cubism_norm_mix N, 2d_cubism_norm_mul …
   [live2d] <model>: blend overrides for missing parts …     <- stale override table
   ```
4. **Blend modes.** List the PSD's non-Normal layers:
   `py -I tools/live2d/psd_layers.py <model>.psd` (`mul` = Multiply, `lddg` = Linear Dodge/Add).
   Shader names: `norm_*` unmasked, `mask_*` clipped; `_mix` Normal, `_mul` Multiply, `_add` Add.
   - The **blend-shader counts must come from the export itself.** Temporarily make
     `Live2DBlendOverrides.Apply` return early (or delete the model's entry) and re-run: expected
     counts = the PSD's `mul`/`lddg` layers. First drop, for reference: combat 2 mul + 2 add,
     shop 2 mul + 1 add, rest 2 mul + 10 add; part count = PSD layer count (36 / 28 / 31).
   - If the export carries them: delete `Live2DBlendOverrides.cs`, its call in
     `NWitchLive2D._Process`, and the "Stopgap" paragraph in live2d.md.
   - If it still exports Normal: tell the user; don't silently re-map. Re-mapping means finding the
     ids again — ArtMesh ids (`ArtMesh0…N`) can renumber between drops, and a stale table quietly
     turns the *wrong* part Multiply. Method used last time: temporarily log each `MeshInstance2D`
     child of the model with `Mesh.GetAabb()` (mesh coords = canvas px minus `origin`), compare to
     the PSD layer boxes from `psd_layers.py`; draw order also helps (last drawable = top layer).
5. **Canvas + editor previews.** Compare each logged `canvas (W, H)` with the PSD size.
   - Equal: preview = `magick <flat>.png -resize 50% -depth 8 editor_preview.png`, offset
     `(-W/4, -H/2)` at scale 2 (i.e. `(-525, -1450)` for 2100×2900).
   - Different (shop, 1st drop): the model holds the PSD scaled/shifted. Fit scale + offset from
     part bounds vs PSD layer boxes (use lower-body parts: legs, shadow — the upper body may be
     posed), then `magick -size WxH xc:none ( <flat>.png -resize S% ) -geometry +X+Y -composite
     -resize 50% -depth 8 editor_preview.png`, offset `(-W/4, -H/2)`. Update the
     `EditorPreview` node's `offset` in the scene.
   - If a canvas changed, **tell the user to re-check placement**: the `Visuals` position/scale in
     each scene was hand-placed against the old preview. Placement is the user's call; don't move
     `Visuals` yourself unless asked.
6. **Motions.** The combat driver (`NWitchLive2D`) mirrors the `witch_visuals.tscn` AnimationTree
   state onto the motion group of the same name; rest/shop just loop `Idle`.
   - **`Cast` gap:** 21 Witch cards fire the `Cast` trigger but `witch_visuals.tscn` has no `Cast`
     animation/state, so BaseLib drops it. A `Cast` motion will never play until a `Cast` state is
     added. Ask the user before adding it.
   - **Placeholder breathing:** once real `Idle` motions exist, `EffectBreath` layers extra head
     sway on top. Confirm with the user, then remove it (`NWitchLive2D.Attach` effect loop, the
     `live2d_breath_intensity` metadata + read in `WitchLive2DMount.TryMount`, the PLACEHOLDER
     section in live2d.md). If the motions are short of a full set, the user may want to keep it
     on some models. The blink effect can stay (it needs the `EyeBlink` ids).
7. **Visual check**: pause overlay over combat (the whole model must sit *under* it — the fork's
   draw-order patch), multiply shadows darken rather than tint, glows brighten, rest/shop render the
   model (not a flat image — a flat image means the mount never ran; check the log).

## Traps

- **Godot editor open on a scene** writes its in-memory copy back on save, undoing text edits to
  the `.tscn`. Tell the user to reload the scene after you edit it.
- **One game instance at a time.** All instances share `godot.log`; the user may be running
  AutoSlay or another agent may have the game open. Check before launching, and close the game
  you launched (it locks the Witch dll for builds).
- **Shop `_Ready` crash:** the base merchant code wraps child 0 in a Spine binding and throws for
  Live2D scenes; `WitchLive2DMerchantPatch` skips it (BaseLib's own skip misses auto-converted
  scenes). Symptom if it regresses: `Expected BoundObject to be a SpineSprite` and the flat
  preview showing in the shop.
- **Never save `Visuals` hidden** in a scene: the model is its child.
- **Model files go in the pck, never loose under `mods/`** (the game parses every `*.json` there as
  a mod manifest).
- **Cubism 5.3 is not supported.** Its blend modes need the 5.3 SDK, which is a renderer port of
  GDCubism (abandoned upstream), not a version bump. Exports for ≤ 5.2 drop 5.3 blend modes to
  Normal; a v6 moc3 won't load. Ask for legacy blend modes instead.
- **Plugin rebuilds** are only needed when `tools/live2d/gdcubism_fork.py` changes (bump
  `FORK_REV`). New models never need one. CI (`live2d-build`, Windows/Linux/macOS) is
  **manual-only** by the user's choice: don't trigger it unless asked. After a run:
  `./tools/live2d/setup-live2d.ps1 -FromCI`, then publish. `bundle-workshop.ps1` refuses to ship
  without all three platform libraries.
- **Never commit or push**; the user does that.

## Files

| What | Where |
|---|---|
| Mount, scene hooks, merchant fix | `TheWitch/TheWitchCode/Live2D/WitchLive2DVisualsPatch.cs` |
| Model build, effects, motion driver, blend log | `TheWitch/TheWitchCode/Live2D/NWitchLive2D.cs` |
| Blend stopgap | `TheWitch/TheWitchCode/Live2D/Live2DBlendOverrides.cs` |
| Extension loading, platforms, model lookup | `TheWitch/TheWitchCode/Live2D/Live2DRuntime.cs` |
| PSD blend/bounds lister | `tools/live2d/psd_layers.py` |
| Plugin fork + build | `tools/live2d/gdcubism_fork.py`, `tools/live2d/setup-live2d.ps1`, `.github/workflows/live2d-build.yml` |
| Rest/shop screenshot harness | `BirdDebug/BirdDebugCode/MarketingShots.cs` (`-bird-shot=restsite|shop`) |
