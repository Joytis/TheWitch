# SFX / VFX Augmentation Proposal — Witch Cards

Source: full sweep of `gamedata/` (decompiled). Two halves: (1) how the base game uses sfx/vfx, classified; (2) proposed per-card augmentations reusing base assets (no custom Godot scenes / FMOD banks required).

## 1. How base game does it

### API (what we call)
- **Attacks**: `DamageCmd.Attack(n).FromCard(this)...` builder:
  - `.WithHitFx(vfx, sfx, tmpSfx)` — vfx = scene path string (`"vfx/vfx_attack_slash"`), sfx = FMOD event (`"event:/sfx/..."`), tmpSfx = debug mp3 (`"blunt_attack.mp3"`). Most common.
  - `.WithAttackerFx(vfx, sfx, tmpSfx)` — plays on attacker.
  - `.WithHitVfxNode(target => NScratchVfx.Create(target, color))` — custom node per target.
  - `.SpawningHitVfxOnEachCreature()`, `.WithHitVfxSpawnedAtBase()`.
- **Non-attack vfx**: `VfxCmd.PlayOnCreatureCenter(creature, path)`, `PlayOnSide`, `PlayFullScreenInCombat`.
- **Node factories** (`src/Core/Nodes/Vfx/`, ~191): `NXxxVfx.Create(...)` then auto-added / `NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(...)`. Many take `VfxColor` (`Red, Green, Blue, Purple, Black, White, Cyan, Gold, Orange, Swamp, DarkGray`) — recolor = base game's own theming trick (green poison, purple occult).
- **Self-buff aura**: `NPowerUpVfx.CreateNormal(creature)` / `CreateGhostly(creature)` (ghostly = purple occult aura; used by Corruption etc.).
- **Potions**: `NCombatRoom.Instance?.PlaySplashVfx(target, tintColor)` — colored splash per potion.
- **SFX**: `SfxCmd.Play("event:/sfx/...")` (FMOD, string-keyed, no enum) or debug files via tmpSfx / `NDebugAudioManager` (`res://debug_audio/*.mp3`). Buff/debuff/block/heal sounds are **automatic** (central in `NCreature`/`CreatureCmd`) — don't re-add.
- **Preload rule**: any non-default vfx used by a card must be declared: `protected override IEnumerable<string> ExtraRunAssetPaths => NGroundFireVfx.AssetPaths;` (see base `Wither.cs`, `BloodWall.cs`).

### Thematic classification of base usage
| Theme | VFX | SFX | Base examples |
|---|---|---|---|
| Basic slash | `vfx/vfx_attack_slash` | char AttackSfx (auto), `slash_attack.mp3` | Strike, Reap, Pillage |
| Blunt/heavy | `vfx/vfx_attack_blunt`, `vfx_heavy_blunt` | `blunt_attack.mp3`, `heavy_attack.mp3` | Bash, Bludgeon |
| Pierce/dagger | `vfx_dramatic_stab`, `vfx_dagger_throw/spray`, `NStabVfx`, `NThinSliceVfx(color)` | `dagger_throw.mp3` | Poisoned Stab, Finisher, Skewer |
| Beast/claw/bite | `vfx/vfx_bite`, `vfx_scratch`, `vfx_thrash`, `NScratchVfx` | Fur-impact family | Claw, Maul, Feed, Thrash |
| Poison/gas/nature | `NPoisonImpactVfx`, `NGaseousImpactVfx` (green), `NSmokePuffVfx(Green)`, `NSporeImpactVfx`, `NSmokyVignetteVfx` | — (visual-led) | Deadly Poison, Noxious Fumes, Haze |
| Fire | `NGroundFireVfx(color)`, `NFireBurstVfx`, `NFireBurningVfx`, `NFireSmokePuffVfx` | `event:/sfx/characters/attack_fire` | Burn, Inflame, Fire Potion |
| Dark/occult | `NGroundFireVfx(Purple)`, `NPowerUpVfx.CreateGhostly`, `NNightmareHandsVfx`, `NSmokyVignetteVfx(purple)`, `vfx/vfx_gaze`, `vfx_spooky_scream`, `NDoomOverlayVfx` | `necrobinder_summon`, `necrobinder_doom_kill`, `doom_apply.mp3` | Forgotten Ritual, Nightmare, Evil Eye |
| Blood/self-cost | `vfx/vfx_bloody_impact`, `vfx/vfx_blood_wall` | `ironclad_bloodwall` | Bloodletting, Blood Wall, Hemokinesis |
| Projectile/throw | `NItemThrowVfx`, `NBolasVfx`, `NSmallMagicMissileVfx`/`NLargeMagicMissileVfx` (tintable) | `dagger_throw.mp3` | Bolas, Comet, Shiv |
| Heal/buff | `vfx/vfx_cross_heal`, `NPowerUpVfx`, `NHealNumVfx` | `event:/sfx/heal` (auto) | — |
| Economy | `vfx/vfx_coin_explosion_small/regular/jumbo` | `event:/sfx/ui/gold/gold_1..3` | Hand of Greed |
| Cards/transform | `NCardTransformVfx`, `NCardUpgradeVfx`, `NCardEnchantVfx` | `event:/sfx/ui/cards/card_transform`, `enchant_shimmer` | upgrades/transforms |
| Fullscreen drama | `VfxCmd.PlayFullScreenInCombat` | — | Adrenaline, Dramatic Entrance |

## 2. Witch palette (house style)

Pick one recolor identity per mechanic so pool reads coherently:
- **Potions** → green/swamp splashes + gas (`PlaySplashVfx` tints, `NGaseousImpactVfx`, `NSmokePuffVfx(Green)`).
- **Brambles** → Swamp/Green `NThinSliceVfx` / `vfx_scratch` (thorn = slice) + `NSporeImpactVfx`.
- **Familiars** → beast set (`vfx_bite`, `NScratchVfx`, `vfx_thrash`) + summon = `NPowerUpVfx.CreateGhostly` + `necrobinder_summon` sfx.
- **Hex/curse/pact** → purple: `NGroundFireVfx(Purple)`, `vfx_gaze`, `doom_apply.mp3`, `NSmokyVignetteVfx`.
- **Blood pacts** → `vfx_bloody_impact`.

## 3. Per-card proposals

Format: **Card — hit/self vfx | sfx**. Defaults (plain slash + auto char sfx) omitted where fine (Strike, Defend, plain block skills keep base).

### Potions mechanic
- **Extract Essence** — hit `vfx/vfx_attack_slash`; on successful extract add `PlaySplashVfx(Owner.Creature, green)` + `gain_potion.mp3` already auto via TryToProcure.
- **Oxidizers** — self `NGaseousImpactVfx.Create(Owner.Creature, green)` on replay trigger.
- **Experiment / Herbal Brew / Stony Brew / Wicked Brew (OrientationBrewCard)** — self `NSmokePuffVfx(Green)` on create; splash tint by orientation: offensive=Red, defensive=Blue, utility=Green.
- **Bottle Barrage** — per-hit `NItemThrowVfx` (thrown bottle) or `WithHitFx("vfx/vfx_rock_shatter", null, "blunt_attack.mp3")` — glass-shatter read.
- **Rattling Bottles** — hit `vfx/vfx_rock_shatter` + `heavy_attack.mp3`; rocks fill = `potion_slosh_*.mp3`.
- **Prices Paid** — attacker `vfx/vfx_bloody_impact` (HP cost) + normal slash hit.
- **Unstable Reaction** — `NFireBurstVfx` on each enemy + `event:/sfx/characters/attack_fire`; destroy phase `NFireSmokePuffVfx` on self.
- **Distill / Gather Herbs / Light the Candle** — self `NCardEnchantVfx`/`enchant_shimmer` sfx (upgrade/copy read); Light the Candle add small `NFireBurningVfx` on self.
- **Grind Down** — `NCardTransformVfx` + `card_transform` sfx.
- **Bottle Wall / Roomy Satchel / A Little Sip / Bottomless Cauldron / Catalyst / Hidden in Smoke / Share the Brew** — powers: keep `NPowerUpVfx.CreateNormal`; Hidden in Smoke turn-start `NSmokePuffVfx(Green)`.
- **Witchcraft (Cauldron pour)** — `PlaySplashVfx(Owner.Creature, purple)` per potion poured + `potion_slosh_*.mp3` each.

### Brambles mechanic
- **Needle Whip / Rake** — hit `NThinSliceVfx.Create(target, VfxColor.Swamp)` + `slash_attack.mp3`.
- **Nettles** — AoE `NScratchVfx` per enemy (Swamp) `.SpawningHitVfxOnEachCreature()`.
- **Brambleburst** — hit `vfx/vfx_thrash` + `heavy_attack.mp3`; self `NSporeImpactVfx` (brambles spent).
- **Mulch** — per-exhaust `NSporeImpactVfx` on self; hit plain slash.
- **Fertilize / Lavender and Sage / Spines / Bramble Shield / Creeping Vines / Stuck in a Bush / Wild Growth** — on Bramble gain: `NSporeImpactVfx.Create(Owner.Creature)` or `NSmokePuffVfx(Green)` (pick one, use everywhere = mechanic signature).
- **Deep Roots / Hedge Prison / Hemlock / Bonfire** — powers: `CreateGhostly` for Hemlock (curse-adjacent), Bonfire add `NFireBurningVfx` on self at apply; Bonfire spend-trigger `NGroundFireVfx(Owner.Creature, VfxColor.Green)`.
- **BramblesPower retaliation** — `NThinSliceVfx(attacker, Swamp)` when thorns fire (big feel win, one call in `BramblesPower`).

### Familiars mechanic
- **All summon powers (Wolf/Bear/Cat/Owl/Rat/Crow, Embrace the Wilds)** — replace bare `PowerUp` anim with `NPowerUpVfx.CreateGhostly(Owner.Creature)` + `SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon")`. Signature moment of the class.
- **Call the Pack / Stampede / Ambush! / Pillage / Feast With Wolves** — hit `vfx/vfx_bite` or `NScratchVfx`; Stampede familiar-damage phase: `vfx_thrash` per enemy.
- **Ritual Sacrifice / Broken Pact** — sacrifice: `vfx/vfx_spooky_scream` on self + `doom_apply.mp3`; Broken Pact heal keeps auto heal sfx + `vfx/vfx_cross_heal`.
- **Pact of Beasts / Find Familiar / Pocket Rats / Refuse Pile / Polymorph** — `NCardTransformVfx`/`card_transform` for Polymorph; others `hiss.mp3` (rats!) — Pocket Rats/Refuse Pile specifically.
- Token cards: **Gnash/Ferocity/Mutilate/Nibble/Claw Eyes/Rats** — hit `vfx/vfx_bite` (Gnash/Nibble/Rats), `NScratchVfx` (Claw Eyes/Ferocity), `vfx_heavy_blunt`+`heavy_attack.mp3` (Mutilate). **Shiny!** — `vfx/vfx_coin_explosion_small` + `event:/sfx/ui/gold/gold_1`. **Plague/Scout Weakness** — `NGaseousImpactVfx(green)` / `vfx/vfx_gaze`.

### Hex / debuff / pact
- **Vexing Strike / Wax and Wane / Bind in Blood / Hexblast / Rip Soul** — Hex apply visual: `VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_gaze")` + `doom_apply.mp3`. Hexblast hit: `NGroundFireVfx(target, VfxColor.Purple)` + `heavy_attack.mp3`.
- **Bewitching Grin / Strike Fear / Circle of Rot** — AoE debuff: `vfx/vfx_gaze` per enemy (`SpawningHitVfxOnEachCreature` / VfxCmd loop).
- **Bind in Blood / Pact of Agony / Prices Paid / Pact of Beasts (HP cost)** — self `vfx/vfx_bloody_impact`.
- **Soul Knot / Thirst / Cursed Spellbook / Wicker Form** — powers: `CreateGhostly`; Cursed Spellbook also `vfx/vfx_spooky_scream` at apply (loud rare).
- **Forbidden Magic** — hit `NLargeMagicMissileVfx` (purple tint) or `vfx/vfx_starry_impact`; sfx `heavy_attack.mp3`.
- **Consume Youth / Extract Life** — hit `vfx/vfx_bloody_impact` (life-drain read) + self `vfx/vfx_cross_heal` for Extract Life? (no heal — skip); Consume Youth `dramatic_stab`.
- **Rotting Roots** — turn-start tick: `NGaseousImpactVfx(green)` on all enemies (mirror NoxiousFumesPower), heal keeps auto.
- **Lich Powder** — self `NSmokePuffVfx` (white/gray) + `CreateGhostly`.

### Misc
- **Broom Strike** — hit `vfx/vfx_heavy_blunt` + `blunt_attack.mp3` (broom = blunt).
- **Bag of Teeth** — 4 hits `vfx/vfx_bite` + Fur-ish rapid; keep multi-hit default sfx.
- **Read the Bones** — attack branch `vfx/vfx_attack_slash`; draw branch `vfx/vfx_gaze` on self (divination).
- **Double, Double / Dance Around the Cauldron / Woe and Whimsy / Salt and Ash / Weathered Witch Hat** — keep Cast defaults; Dance add self `NGroundFireVfx(Green)` (cauldron fire under it).
- **Hide in a Bush** — self `NSmokePuffVfx(Green)`.
- **Wormy** — self `NWormyImpactVfx` (exists in base!).

## 4. Implementation notes

1. Attacks: pass through existing `DamageCmd.Attack(...)` chains — add `.WithHitFx(...)` / `.WithHitVfxNode(...)`; zero structural change.
2. Non-attacks: one `VfxCmd.PlayOnCreatureCenter` / `NXxxVfx.Create` call in `OnPlay` next to existing `TriggerAnim`.
3. **Every card using an `N*Vfx` node must add `ExtraRunAssetPaths => NXxxVfx.AssetPaths`** (concat for multiple) or vfx may miss preload.
4. Never add sfx for block/heal/buff/debuff/potion-procure — central auto sounds already fire.
5. FMOD event strings are safe to reuse cross-character (`necrobinder_summon`, `attack_fire`, `gold_1`). Custom audio later = mp3 via tmpSfx param / `NDebugAudioManager` (`res://debug_audio/` pathing — would need preload registration; defer).
6. Suggested rollout order: (a) BramblesPower retaliation slice, (b) familiar summon ghostly+sound, (c) potion-create smoke/splash signature, (d) Hex gaze+doom_apply, (e) per-card attack hit fx.
