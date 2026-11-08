# Live2D → Spine bake: handoff (parked)

Status: **idea, parked 2026-11-07.** Nothing built. This page records the feasibility findings so
a future session can pick it up without redoing the research. Current Live2D setup:
[live2d.md](live2d.md); art intake: [live2d-art-handoff.md](live2d-art-handoff.md).

## Why

The Witch ships Live2D through our GDCubism fork, a native GDExtension. That costs us:

- one native library per platform (Linux/macOS builds exist but are untested in game; macOS
  library validation may block loading entirely);
- the fork, the `live2d-build` CI workflow, and Live2D Core redistribution terms;
- a hard failure mode: no extension or model means the Witch is invisible;
- a parallel animation path (`NWitchLive2D` mirrors the AnimationTree) instead of the game's own.

The game renders every base character with Spine. If the Witch were Spine data, all of that goes
away and she runs on the game's own renderer on every platform.

## Findings (2026-11-07)

- **No Live2D → Spine converter exists.** Cubism (warp/rotation deformers blended over parameters)
  and Spine (bones, weighted meshes, per-animation deform keys) don't map. Esoteric says so:
  [Spine forum thread](https://zh.esotericsoftware.com/forum/d/29194-can-cubism-live2d-animations-be-opened-or-converted-to-spine-files-using-spine).
  `dragonbones-tools` chains Live2D → DragonBones → Spine
  ([DragonBones/Tools](https://github.com/DragonBones/Tools)), but its Live2D importer reads the
  old Cubism 2 format, not moc3 (from memory, not verified).
- **Game Spine runtime: 4.2.43** (header of `gamedata/animations/characters/<name>/<name>.skel`). Bindings in
  `gamedata/src/Core/Bindings/MegaSpine/`.
- **Native integration:** `NCreatureVisuals` treats the body as Spine when
  `_body.GetClass() == "SpineSprite"` and wraps it in `MegaSprite`; that unlocks hue-shift,
  liquid overlay, skins, phobia mode, and `IsPlayingHurtAnimation` (`"hurt"`).
- **Animation names the game drives:** `idle_loop`, `attack`, `cast`, `hurt`, `die`. Mapping
  from our motion groups: `Idle`→`idle_loop`, `Attack`/`AttackSilent`→`attack`, `PowerUp` (and the
  missing `Cast`)→`cast`, `Hit`→`hurt`, `Dead`→`die`.
- **Proof that mods can ship Spine:** the Runesmith 2 Workshop mod
  (`steamapps/workshop/content/2868840/3747609123/`) has ~15 Spine 4.2.43 skeletons in its pck,
  imported as `.godot/imported/*.spskel` / `*.spatlas` with `SpineSkeletonDataResource` /
  `SpineAtlasResource` / `SpineSkeletonFileResource` / `SpineSprite`. So they exported from a
  Spine-enabled Godot editor.

## Approach: bake, don't convert

Play each Live2D motion and record the result as Spine vertex animation (like an Alembic bake).

1. **Sample.** Step each motion at a fixed timestep (30 fps) through the Cubism runtime we already
   build. Per step, read every drawable's vertex positions, opacity, render order (and
   multiply/screen colors if used). Physics, breath and blink play during sampling and get baked
   in. Two ways in: the Cubism Core C API (`csmGetDrawableVertexPositions`, `csmGetDrawableOpacities`,
   `csmGetDrawableRenderOrders`) driven by the SDK Framework for motions/physics; or inside Godot,
   reading the mesh arrays GDCubism builds per part (it already runs motions + physics).
2. **Emit Spine JSON (4.2).** One `root` bone. One slot + mesh attachment per drawable; texture
   pages reused as-is (one atlas region per page, so the drawable UVs carry over unchanged). Per
   animation: `deform` timelines (vertex offsets from setup pose), slot `rgba` for opacity,
   `drawOrder` keys when render order changes. Legacy Multiply/Additive → slot `blend`.
3. **Reduce.** Drop deform keys that linear interpolation reproduces within a pixel tolerance;
   skip meshes that never move in an animation.
4. **Name** animations to the game's names above.
5. **Integrate.** Body node becomes a `SpineSprite` with a `SpineSkeletonDataResource`; delete the
   Live2D mount/driver, the fork, the CI workflow and the bundle checks once it lands.

## Risks to settle first

- **Clipping masks (biggest).** Live2D masks by texture alpha, any mask set per drawable; faces use
  them (pupils in eye whites). Spine clipping is a polygon, one active clip over a contiguous slot
  range, CPU-costly. Clipping attachments take deform keys, so the mask mesh's hull can follow the
  bake, but fidelity depends on how tightly the artist meshed the mask parts. Fallback: artist
  rebuilds only the masked face parts as Spine clipping shapes.
- **File size.** Per-vertex keys get heavy (≈ drawables × vertices × fps × seconds); measure after
  key reduction. Binary `.skel` is smaller than JSON.
- **Lost interactivity.** Fixed clips: no live physics response, no parameter blending. Spine
  crossfades between animations, which covers transitions.
- **Tooling.** Our stock Godot 4.5 mono editor can't import `.skel`/`.spine-json`. Options: an
  Esoteric prebuilt Spine-enabled Godot editor (must be 4.5.x and Spine 4.2 to match the game), or
  loading the data at runtime from raw files in the pck (spine-godot resource loaders; unverified).
- **Licensing (user's call).** Spine runtime license assumes a Spine license holder; inspecting or
  tweaking the bake in the Spine editor needs Spine Professional (meshes/deforms aren't in
  Essential).

## First step when resumed

Spike: bake the current combat model's idle (placeholder breathing) to Spine JSON, mount it in a
`-bird-bootstrap` combat, compare against the Live2D version side by side. Answers: mask fidelity,
file size, and whether the export path works. Decide go/no-go from that.
