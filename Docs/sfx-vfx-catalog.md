# SFX / VFX Catalog & API (base game reference)

Distilled from `gamedata/` sweep (2026-07). Companion: [sfx-vfx-proposal.md](sfx-vfx-proposal.md) (Witch usage plan), helper `TheWitchCode/Extensions/WitchFx.cs`.

## API

### Attack builder (`DamageCmd.Attack(...)` → `AttackCommand`, `gamedata/src/Core/Commands/Builders/AttackCommand.cs`)
- `.WithHitFx(vfx, sfx, tmpSfx)` — vfx = scene path (`"vfx/vfx_attack_slash"`), sfx = FMOD event string, tmpSfx = debug mp3 (`"blunt_attack.mp3"`). Most common.
- `.WithAttackerFx(vfx, sfx, tmpSfx)` — on attacker; also `Func<Node2D?>` overload.
- `.WithHitVfxNode(t => NScratchVfx.Create(t, goingRight: true))` — custom node per target.
- `.SpawningHitVfxOnEachCreature()` (AoE per-target), `.WithHitVfxSpawnedAtBase()`, `.WithNoAttackerAnim()`, `.WithAttackerAnim(name, delay)`, `.BeforeDamage(func)` (per-hit sfx).

### Direct VFX
- `VfxCmd.PlayOnCreatureCenter(creature, path)` / `PlayOnCreatures` / `PlayOnSide` / `PlayFullScreenInCombat` (`Commands/VfxCmd.cs`). Named consts: `VfxCmd.slashPath/bluntPath/healPath/gazePath`.
- Node factories `N*Vfx.Create(...)` (~191 classes, `Nodes/Vfx/`): return node; caller attaches via `NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(node)`. **Exception: `NPowerUpVfx.CreateNormal/CreateGhostly` self-attach.** All return null in TestMode; nodes self-free.
- `VfxColor` enum: `Red Green Blue Purple Black White Cyan Gold Orange Swamp DarkGray`. Many factories take raw `Godot.Color` (e.g. `new Color("83eb85")` = poison green).
- Potion drink splash: `NCombatRoom.Instance?.PlaySplashVfx(target, tint)`.

### SFX
- `SfxCmd.Play("event:/sfx/...")` — FMOD, string-keyed (no enum). `PlayLoop/StopLoop/SetParam` variants.
- Debug/custom audio: `NDebugAudioManager.Instance?.Play("file.mp3")` → loads `res://debug_audio/<file>`; also the `tmpSfx:` builder param. Registry consts in `Audio/FmodSfx.cs` + `Audio/Debug/TmpSfx.cs`.
- **Automatic (never re-add):** block gain/break/hit, heal, buff/debuff apply, monster hurt/death, player attack/cast/powerup (from `CharacterModel.AttackSfx` etc.), potion procure (`gain_potion.mp3`), potion slosh, gold gain.

### Preloading (important)
- Card-level: `protected override IEnumerable<string> ExtraRunAssetPaths => NXxxVfx.AssetPaths;` (concat for several). Powers have **no** such hook — vfx spawned by a power must be globally preloaded or registered by the granting card.
- Globally preloaded (`VfxCmd.AssetPaths`): all string-path vfx below, plus nodes: `NBigSlash(Impact)`, `NCardTransformShine`, `NDaggerSpray*`, `NFireBurning`, `NFireBurst`, `NGaseousImpact`, `NGoopyImpact`, `NHyperbeam*`, `NItemThrow`, `NLargeMagicMissile`, `NLiquidOverlay`, `NLowHpBorder`, `NMinionDiveBomb`, `NPoisonImpact`, `NScratch`, `NShivThrow`, `NSmallMagicMissile`, `NSplash`, `NSporeImpact`, `NSweepingBeam*`, `NWormyImpact`, `NHealNum`.
- NOT global (register per card): `NSmokePuffVfx`, `NPowerUpVfx`, `NThinSliceVfx`, `NGroundFireVfx`, `NFireSmokePuffVfx`, `NCardEnchantVfx`, `NCardTransformVfx`, and most monster-specific nodes.

## VFX catalog (by theme)

| Theme | String paths (`vfx/...`) | Node factories |
|---|---|---|
| Slash | `vfx_attack_slash`, `vfx_flying_slash`, `vfx_giant_horizontal_slash`, `vfx_thrash`, `vfx_scratch` | `NBigSlashVfx(+Impact)`, `NThinSliceVfx(color)`, `NScratchVfx(creature, goingRight)` |
| Blunt | `vfx_attack_blunt`, `vfx_heavy_blunt`, `vfx_rock_shatter`, `vfx_sandy_impact`, `vfx_slime_impact` | — |
| Pierce | `vfx_dramatic_stab`, `vfx_dagger_throw`, `vfx_dagger_spray` | `NStabVfx(color)`, `NFanOfKnivesVfx`, `NShivThrowVfx` |
| Beast | `vfx_bite`, `vfx_scratch`, `vfx_thrash` | `NScratchVfx` |
| Poison/gas | — | `NPoisonImpactVfx(creature)`, `NGaseousImpactVfx(creature, Color)`, `NSmokePuffVfx(creature, Green\|Purple)`, `NSporeImpactVfx(creature, Color)`, `NSmokyVignetteVfx` |
| Fire | — | `NGroundFireVfx(creature, VfxColor)`, `NFireBurstVfx(creature, scale)`, `NFireBurningVfx(creature, scale, goingRight)`, `NFireSmokePuffVfx(creature)` |
| Occult/dark | `vfx_gaze`, `vfx_scream`, `vfx_spooky_scream` | `NGroundFireVfx(Purple)`, `NPowerUpVfx.CreateGhostly`, `NNightmareHandsVfx`, `NDoomOverlayVfx`, `NSoulNexusVfx` |
| Blood | `vfx_bloody_impact` | (`vfx/vfx_blood_wall` scene) |
| Projectile | — | `NItemThrowVfx(srcPos, tgtPos, texture)`, `NBolasVfx`, `NSmallMagicMissileVfx`/`NLargeMagicMissileVfx(pos, tint)` |
| Beam/lightning | `vfx_attack_lightning` | `NHyperbeamVfx`, `NSweepingBeamVfx`, `NLaserVfx` |
| Cosmic | `vfx_starry_impact` | `NStardust` |
| Heal/buff | `vfx_cross_heal`, `vfx_block`, `vfx_chain` | `NPowerUpVfx.CreateNormal/CreateGhostly(creature)` (self-attach), `NHealNumVfx` |
| Economy | `vfx_coin_explosion_small/regular/jumbo` | `NCoinExplosion` |
| Card UI | — | `NCardTransformVfx`, `NCardUpgradeVfx`, `NCardEnchantVfx(card)`, `NCardTrailVfx`, `NPotionFlashVfx` |
| Fullscreen | `vfx_adrenaline` | `NFullscreenTextVfx`, `NSmokyVignetteVfx` |

## SFX catalog (reusable ids)

FMOD combat: `event:/sfx/block_gain|block_break|block_hit|buff|debuff|heal`, `event:/sfx/characters/attack_fire`.
Character/necro: `event:/sfx/characters/necrobinder/necrobinder_summon`, `.../necrobinder_doom_kill`, `event:/sfx/characters/ironclad/ironclad_bloodwall`, `event:/sfx/characters/osty/osty_attack`, `event:/sfx/byrdpip/byrdpip_attack`, per-char `event:/sfx/characters/{id}/{id}_attack|cast|die|select`.
UI/economy: `event:/sfx/ui/gold/gold_1..3`, `event:/sfx/ui/gain_energy`, `event:/sfx/ui/enchant_shimmer`, `event:/sfx/ui/cards/card_transform`, `event:/sfx/ui/relic_activate_general|_draw`.
Monster impact materials (`DamageSfxType`): `event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_{armor|armor_big|fur|insect|magic|plant|slime|stone}`.
Debug mp3s (tmpSfx / `NDebugAudioManager`): `heavy_attack.mp3`, `slash_attack.mp3`, `blunt_attack.mp3`, `dagger_throw.mp3`, `doom_apply.mp3`, `lightning_orb_evoke.mp3`, `gain_potion.mp3`, `potion_slosh_1..3.mp3`, `hiss.mp3`, `hey.mp3`, `card_exhaust`, `battle_start_1/2.mp3`.

## Mod custom audio/vfx
- Custom vfx: ship `.tscn`, load via `ResourceLoader.Load<PackedScene>("res://TheWitch/...")` + `Instantiate<Node2D>()` + `AddChildSafely`; register path for preload.
- Custom sfx: mp3 via `NDebugAudioManager` route (`res://debug_audio/` pathing; needs preload registration). FMOD banks not moddable practically — reuse base events.
