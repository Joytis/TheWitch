# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

> **Merge note (2026-07-11):** entries 173–175 below were done 2026-07-08 on another machine and merged in after the 123–172 rework batch (renumbered from their original 122/132/133 to avoid collisions). Two other entries from that machine were dropped as superseded by the rework: *Rename Plunder → The Hunt* (remote renamed it Pick Clean instead, entry 123) and the *Oxidizers choice-prompt replay fix* (Oxidizers was cut entirely, entry 125 — the `OxidizersReplayPatch.cs` it introduced was removed in the merge).

### 220. New card: Moonbeam (2E Uncommon Attack)
- **Done:** 2026-07-12 (claude) — [Moonbeam.cs](../TheWitchCode/Cards/Moonbeam.cs) + [MoonbeamPower.cs](../TheWitchCode/Powers/MoonbeamPower.cs): Deal 10 (+3), then apply Moonbeam (debuff, Amount = card damage) — the target takes that damage again at the start of each of the APPLIER's turns (`Applier` guard, so MP-correct; blockable non-attack, `Unpowered` so Strength/Vigor don't re-scale the tick; starry hit fx, purple flame tick). Stacks add (second play = +10/turn). Skipped if the initial hit kills. Tagged None·Standalone.
- **Verified:** build 0/0; regen. ⚠️ Playtest: tick fires at your turn start each turn, respects Block; stacking adds. ⚠️ Art missing: moonbeam.png.

### 219. Taste of Blood — rework: Vigor+draw Skill → multi-hit Attack
- **Done:** 2026-07-12 (claude) — [TasteOfBlood.cs](../TheWitchCode/Cards/TasteOfBlood.cs): was "Gain 5 (+3) Vigor, draw 2" (1E Uncommon Skill, Self). Now 1E Uncommon Attack (AnyEnemy): Deal 4 (+2) damage 2 times (`WithHitCount(2)`, bite fx), draw 2. Tags retagged Debuff/Buff·Generator → None·Standalone (no pillar interaction anymore).
- **Verified:** build 0/0; regen (TESTED cleared).

### 218. Cut Bramble Shield; its art reused for Wicker Form
- **Done:** 2026-07-12 (claude) — User call: duplicate effect of Hide in a Bush. Deleted `BrambleShield.cs` + `.uid` + `THEWITCH-BRAMBLE_SHIELD.*` loc keys (no dangling refs). Art: `{,big/}bramble_shield.png` git-mv'd over `{,big/}wicker_form.png` (old Wicker Form placeholder art replaced); all stale `.import`s deleted. **⚠️ User action: run "Godot: Import assets".**
- **Verified:** build 0/0; regen (99 cards, −Bramble Shield; Wicker Form art found).

### 217. New card: Capture Soul (1E Rare Attack)
- **Done:** 2026-07-12 (claude) — [CaptureSoul.cs](../TheWitchCode/Cards/CaptureSoul.cs): Deal 10 (+3), apply 1 Hex; on kill the Hex is skipped and the copy permanently gains +1 Hex — `[SavedProperty] BonusHex` on the card, incremented on both the combat instance and `DeckVersion` (base-game GeneticAlgorithm pattern), so it **persists across combats, save/load, and MP** (SavedProperties rides `SerializableCard.Props`). Live display via Barrage CalculatedVar shape (base 1 + 1 × BonusHex). Exhaust. Upgrade: +3 damage (user didn't specify — my call; base Hex/kill-bonus untouched).
- **Verified:** build 0/0; regen. ⚠️ Playtest: kill → next combat shows 2 Hex; save/quit/resume keeps it. ⚠️ Art missing: capture_soul.png.

### 216. New card: Accursed Needles (1E Uncommon Attack)
- **Done:** 2026-07-12 (claude) — [AccursedNeedles.cs](../TheWitchCode/Cards/AccursedNeedles.cs): Deal 3, apply 1 (+1 upgraded) Hex, +1 Hex per prior play this combat (1st play 1, 2nd 2, …). Live number on the card face via Barrage `CalculatedVar` shape (`CalculationBase` 1 + `CalculationExtra` 1 × play count); play counter is a combat-scoped mutable-instance field (`AssertMutable`, UpMySleeve pattern) so it resets each combat and needs no `DeepCloneFields` (int, not a collection). Upgrade bumps `CalculationBase`.
- **Verified:** build 0/0; regen. ⚠️ Playtest: number ticks 1→2→3 across plays and resets next combat; upgraded shows 2 base. ⚠️ Art missing: accursed_needles.png.

### 215. New card: Plague (3E Rare Skill)
- **Done:** 2026-07-12 (claude) — [Plague.cs](../TheWitchCode/Cards/Plague.cs): Apply 3 (+1) Hex to ALL enemies; every `IRatCard` in the Exhaust pile moves back to hand (plain `CardPileCmd.Add` — moving existing cards, not generation; capped by hand space, excess stays exhausted).
- **Verified:** build 0/0; regen. ⚠️ Playtest: exhausted-Rats return incl. hand-full cap. ⚠️ Art missing: plague.png.

### 214. New card: Throw Bait (1E Uncommon Attack)
- **Done:** 2026-07-12 (claude) — [ThrowBait.cs](../TheWitchCode/Cards/ThrowBait.cs): Deal 6 (+3), then every `FamiliarPower` on the owner runs one full production round (per-stack, user-confirmed; Sack of Treats composes). Enabler: [FamiliarPower.cs](../TheWitchCode/Powers/FamiliarPower.cs) turn-start loop extracted into public `GenerateCards(player, combatState)` — `BeforeHandDraw` now delegates to it, zero behavior change.
- **Verified:** build 0/0; regen. ⚠️ Art missing: throw_bait.png.

### 213. New card: Trash Diving (1E Common Skill)
- **Done:** 2026-07-12 (claude) — [TrashDiving.cs](../TheWitchCode/Cards/TrashDiving.cs): choose 1 card from Discard → Hand (Rummage/Dredge pattern, skipped if hand full) + CREATE 1 Rats in hand (generated path, previewed). Exhaust; upgrade removes Exhaust. Loc + selectionScreenPrompt added. User's art brief noted for card-briefs page: "A cute mouse bounds out of a pile of trash, holding a glittering ring in their mouth."
- **Verified:** build 0/0; regen. ⚠️ Art missing: trash_diving.png. Brief NOT auto-added to card-briefs.json (authored via the card-designs page).

### 212. New relic: Sack of Treats (Rare)
- **Done:** 2026-07-12 (claude) — [SackOfTreats.cs](../TheWitchCode/Relics/SackOfTreats.cs) (passive, no hooks) + [FamiliarPower.cs](../TheWitchCode/Powers/FamiliarPower.cs): `BeforeHandDraw` checks `player.GetRelic<SackOfTreats>()` — with it, each stack calls new virtual `CreateAllTurnStartCards` (default = the single card; `LootTableFamiliarPower` overrides via new `FamiliarLootTable.CreateAll` = one of EACH entry, table order). Relic flashes when it fires. Wolf (single-card) unchanged by design. Loc in relics.json; art-tracker assets.json entry added with the user's brief.
- **Verified:** build 0/0; regen. ⚠️ Playtest: Bear/Crow/Cat/Owl/Rat produce full sets per stack; hand-size overflow behavior. ⚠️ Art missing: relics/big/sack_of_treats.png.

### 211. Thirst → Ritual Casting (rename + rework)
- **Done:** 2026-07-12 (claude) — Full rename cascade: `Thirst{,Power}` → `RitualCasting{,Power}` (git mv incl. `.uid`s), loc keys `THEWITCH-THIRST{,_POWER}.*` → `THEWITCH-RITUAL_CASTING{,_POWER}.*`, art `{,big/}thirst.png` → `ritual_casting.png`, stale `.import`s deleted. New effect: was "whenever you draw a card, gain N Vigor" → now "for every 3 cards you draw, apply N Hex to ALL enemies" (N = power Amount, 1 (+1); 1E Rare Power unchanged). Draw counter is a plain instance field (`_drawsSinceTrigger`) — safe per SP/MP-lockstep rules, resets only on mid-combat MP rejoin. Vigor hover tip → Hex.
- **Verified:** build 0/0; regen sees remove+add (Thirst tags/TESTED reset). **⚠️ User action: run "Godot: Import assets"** for the renamed PNGs. ⚠️ Playtest: 3-draw cadence incl. turn-start hand draw; stacked casts apply N per trigger.

### 210. New card: Rot Bloom (2E Uncommon Attack)
- **Done:** 2026-07-12 (claude) — [RotBloom.cs](../TheWitchCode/Cards/RotBloom.cs): Deal 15 (+5) damage, then duplicate ALL the target's debuffs (each debuff re-applied at its snapshotted current amount via non-generic `PowerCmd.Apply`; Rend's filter — `TypeForCurrentAmount == Debuff`, `ITemporaryPower` excluded, amount > 0; skipped if hit kills). Exhaust. Green gas puff on the bloom. No art yet (placeholder fallback).
- **Verified:** build 0/0; regen. ⚠️ Playtest: Hex duplication interacts with Hex's per-attack decrement; artifact-style effects; multi-debuff stacks double correctly. ⚠️ Art missing: rot_bloom.png.
- **Done:** 2026-07-12 (claude) — [PocketRats.cs](../TheWitchCode/Cards/PocketRats.cs): rarity Rare→Uncommon, `CardsVar` 3→2, upgrade +1 (→3). Loc unchanged (var-driven).
- **Verified:** build 0/0; regen (TESTED cleared).

### 208. Stuck in a Bush — 6 (+2) Brambles, 1 Vulnerable
- **Done:** 2026-07-12 (claude) — [StuckInABush.cs](../TheWitchCode/Cards/StuckInABush.cs): Brambles 10→6, upgrade +3→+2, Vulnerable 2→1. Loc unchanged (var-driven).
- **Verified:** build 0/0; regen (TESTED cleared).

### 207. Polymorph — Exhaust removed; upgrade = card becomes TWO Rats
- **Done:** 2026-07-12 (claude) — [Polymorph.cs](../TheWitchCode/Cards/Polymorph.cs): `CanonicalKeywords` (Exhaust) dropped; new `CardsVar(1)`, upgrade +1 replaces old RemoveKeyword upgrade. Chosen card still transforms via `CardCmd.Transform`; extra Rats (upgraded) generated into the Draw pile at a random position (`AddGeneratedCardToCombat`) — transform can't split one card into two. Loc: "It becomes {Cards:diff()} [gold]Rats[/gold]."
- **Verified:** build 0/0; regen (TESTED cleared). ⚠️ Playtest: upgraded play → 2 Rats total in draw pile; card-generation payoffs (Cloak of Moonlight) trigger on the extra Rats only.

### 206. Distill — Uncommon → Rare
- **Done:** 2026-07-12 (claude) — [Distill.cs](../TheWitchCode/Cards/Distill.cs): ctor rarity only. (Note: DONE 200's text/mechanics mismatch flag still stands.)
- **Verified:** build 0/0; regen (TESTED cleared).

### 205. Hexblast — rework: flat AoE damage + Hex
- **Done:** 2026-07-12 (claude) — User call: was "apply 1 Hex to ALL, then deal 10 × each enemy's Hex". Now: deal 10 to ALL (`TargetingAllOpponents`), then apply 2 Hex to ALL; upgrade Hex 2→3 (damage upgrade removed). Kept purple ground-fire hit vfx + heavy_attack sting + screen shake. 2E Rare unchanged.
- **Files:** `TheWitchCode/Cards/Hexblast.cs`, `TheWitch/localization/eng/cards.json`. **Verified:** build 0/0; regen (TESTED cleared).

### 204. Ritual Sacrifice — Strength payoff → Hex payoff
- **Done:** 2026-07-12 (claude) — [RitualSacrifice.cs](../TheWitchCode/Cards/RitualSacrifice.cs): "Gain 5 (+3) Strength" → "Apply 5 Hex (upgrade 8)" on a chosen enemy; TargetType Self→AnyEnemy. Draw 3 unchanged; effects still gated on a familiar actually being sacrificed. Loc third line swapped.
- **Verified:** build 0/0; regen (TESTED cleared). ⚠️ Playtest: no-familiar play = target selected but nothing happens (pre-existing gate, now also skips Hex).

### 203. Eye of Newt — stacks now multiply potion damage (display stays truthful)
- **Done:** 2026-07-11 (claude) — [EyeOfNewtPower.cs](../TheWitchCode/Powers/EyeOfNewtPower.cs): stacking made multiplicative via `TryModifyPowerAmountReceived` on the live instance — applying x onto Amount A adds x·(1+A), so the multiplier composes as (1+A)(1+x): double+double = ×4 (buff shows +300%), triple+triple = ×9 (+800%). Damage formula unchanged (×(1+Amount)), so the existing `+{Amount}00%` smartDescription in powers.json stays accurate with no loc change — that's the "visually clear" guarantee. First application is unscaled (a not-yet-applied power isn't a hook listener).
- **Verified:** build 0/0. ⚠️ Playtest: play two Eye of Newts → buff reads +300% and a 10-dmg potion hits for 40; upgraded pair reads +800%.

### 202. Rats token — heal removed
- **Done:** 2026-07-11 (claude) — User call: lifesteal too strong. [Rats.cs](../TheWitchCode/Cards/Familiar/Rats.cs): dropped the `Heal` var + `CreatureCmd.Heal` line + loc "Heal {Heal:diff()} HP." line. Now 5 (+3) damage, draw 1, Exhaust.
- **Verified:** build 0/0; regen (TESTED cleared).

### 201. Rename: Dance Around the Cauldron → Cauldron Dance
- **Done:** 2026-07-11 (claude) — Full cascade: classes/files `DanceAroundTheCauldron{,Power}` → `CauldronDance{,Power}` (git mv incl. `.uid`s), loc keys `THEWITCH-DANCE_AROUND_THE_CAULDRON{,_POWER}.*` → `THEWITCH-CAULDRON_DANCE{,_POWER}.*` in cards.json/powers.json, art renamed to `{,big/}cauldron_dance.png`, stale `.import`s deleted. Regen sees it as remove+add, so curated tags/TESTED for the old name were reset — retag if needed. **⚠️ User action: run the "Godot: Import assets" task** so the renamed PNGs import.
- **Verified:** build 0/0; regen (94 cards, +Cauldron Dance / −Dance Around the Cauldron; art found, not in missing list).

### 200. Distill — reword to "Transform … into Rare Potions" (text only)
- **Done:** 2026-07-11 (claude) — User call (revised mid-loop from "Brew" to "Transform"): `THEWITCH-DISTILL.description` → "Transform {Potions:diff()} random {Potions:plural:Potion|Potions} into {Potions:plural:a [gold]Rare Potion[/gold]|[gold]Rare Potions[/gold]}." Mechanics untouched (rarity+1 same-orientation). ⚠️ **Text/mechanics mismatch flagged:** a Common potion transforms to Uncommon, not Rare — text overpromises unless mechanics later change to straight-to-Rare.
- **Files:** `TheWitch/localization/eng/cards.json`. **Verified:** build 0/0; regen (TESTED cleared).

### 199. Weathered Witch Hat rework: Block + play a random Skill from your draw pile
- **Done:** 2026-07-11 (claude) — User call: replaced the NextSkillDiscount rider with "play a random [gold]Skill[/gold] from your [gold]Draw Pile[/gold]" (free auto-play, nothing if none). Random pick + `CardCmd.AutoPlay(ctx, card, null)` follow base-game Catastrophe (`StableShuffle(Rng.Shuffle).FirstOrDefault()`); Unplayable skills excluded. Block/cost/rarity unchanged (2E Common, 10 Block, +3 upgrade). `NextSkillDiscountPower` NOT orphaned — Broom Strike still applies it.
- **Files:** `TheWitchCode/Cards/WeatheredWitchHat.cs`, `TheWitch/localization/eng/cards.json`. **Verified:** build 0/0; regen (TESTED cleared). ⚠️ Playtest: auto-played skill resolves targeting correctly; empty/no-skill draw pile no-ops.

### 198. Familiar pets: one pet per stack, clustered by type
- **Done:** 2026-07-11 (claude) — [FamiliarPower.cs](../TheWitchCode/Powers/FamiliarPower.cs): pet count now synced to stack count in `AfterPowerAmountChanged` (fires for initial apply AND every stack change — spawn missing / kill excess), `AfterApplied` keeps only the summon flourish, `AfterRemoved` sweeps leftovers. New [WitchPetClusterPatch.cs](../TheWitchCode/Monsters/WitchPetClusterPatch.cs): Harmony prefix on `NCombatRoom.PositionPlayersAndPets` stably regroups WitchPet nodes by (owner, pet type) in-place — only witch-pet slots rewritten, players/Osty/base-game pets untouched, so same-type familiars sit adjacent and a new summon lands next to its kin.
- **Verified:** build 0/0. ⚠️ Playtest (visual, compile-checked only): N stacks = N pets; Wolf+Owl+Wolf shows wolves adjacent; stack loss removes one pet; layout with Byrdpip/MP.

### 197. Pact of Fury: Exhaust
- **Done:** 2026-07-11 (claude) — User call: repeatable permanent team-wide Strength was the problem; numbers untouched (Weak 5 / +4 Str, upgrade +2). `CanonicalKeywords => [CardKeyword.Exhaust]` on [PactOfFury.cs](../TheWitchCode/Cards/PactOfFury.cs) (loc untouched — Exhaust renders as keyword, PactOfAgony precedent).
- **Verified:** build 0/0; regen.

### 196. Bottle Barrage counts only the owner's potions (MP)
- **Done:** 2026-07-11 (claude) — [PotionsCreatedTracker.cs](../TheWitchCode/Patches/PotionsCreatedTracker.cs) re-keyed: weak table still keyed by `ICombatState` (keeps the automatic per-combat reset) but now holds a per-`Player` count map; `CountFor(combat, player)`; [BottleBarrage.cs](../TheWitchCode/Cards/BottleBarrage.cs) passes `card.Owner`. Single-player behavior identical.
- **Verified:** build 0/0. ⚠️ MP playtest: two players brewing — each Bottle Barrage shows only its owner's count.

### 195. Ferocity includes itself in the hit count
- **Done:** 2026-07-11 (claude) — [Ferocity.cs](../TheWitchCode/Cards/Familiar/Ferocity.cs): multiplier is now `AttacksPlayedThisTurnBefore(card) + 1` — history query still excludes the card itself, flat +1 added instead, so the pre-play preview equals the hits dealt (during OnPlay its own play is already in history). Dead `hits <= 0` early-out removed (always ≥1 now). Loc unchanged — "for each Attack played this turn" was already inclusive wording.
- **Verified:** build 0/0; regen. ⚠️ Playtest: unplayed-this-turn Ferocity previews 1 hit; after 2 Attacks previews 3 and deals 3.

### 194. Crystal Bottle MP fix: per-replay choice contexts
- **Done:** 2026-07-11 (claude) — User report: bottled draft potion (Attack/Skill/Power/Colorless Potion `FromChooseACardScreen`) sometimes broke MP at turn start. Root cause: the whole turn-start window shares ONE `HookPlayerChoiceContext` per player and it supports exactly one player-choice game action; a second selection hits the `"Tried to interrupt action"` error path and opens unsynced. [NeverendingPotionPower.cs](../TheWitchCode/Powers/NeverendingPotionPower.cs) now wraps each bottled replay in its OWN `HookPlayerChoiceContext(this, LocalContext.NetId, combat, GameActionType.CombatPlayPhaseOnly)` + `AssignTaskAndWaitForPauseOrCompletion` (base-game `Hook.AfterDeath` pattern) — a choice-prompting replay pauses into its own queued action and resolves at Play-phase start; non-choice replays complete inline as before. Ordering caveat commented: a later bottle's instant effect can resolve before an earlier bottle's pending selection. Audited all other mod turn-start powers (CursedSpellbook, DeepRoots, HiddenInSmoke, RottingRoots, VigorNextTurn, WickerForm, FamiliarPower) — none open selections; this was the only offender. Gotcha added to CLAUDE.md.
- **Verified:** build 0/0. ⚠️ MP playtest: bottle two draft potions — both selection screens queue cleanly, remote client waits on each.

### 193. Crystal Bottle fix: bottled potions no longer persist across combats (+ BonfirePower same class)
- **Done:** 2026-07-11 (claude) — User report: Neverending Potion replays carried into later combats. Root cause: models clone via `MemberwiseClone()` (shallow) and `NeverendingPotionPower._bottled` was a `readonly List` initialized at declaration — every mutable clone shared the CANONICAL model's list, so `Bottle()` wrote into process-lifetime state (power itself was correctly removed at combat end; the list survived on the canonical). Fix: field non-readonly + `DeepCloneFields()` override assigning a fresh list (new list, not `Clear()` — clearing would mutate the shared canonical). Same treatment applied to [BonfirePower.cs](../TheWitchCode/Powers/BonfirePower.cs) `_substituted` (milder symptom — stale-entry leak only). Audited remaining collection fields: `FamiliarPower._entries` (static loot-table config, safe), `PotionTraits._cache` (intentionally static). Gotcha added to CLAUDE.md.
- **Verified:** build 0/0. ⚠️ Playtest: bottle a potion combat 1 → combat 2 starts with no Neverending Potion buff; re-arming replays only the newly drunk potion.

### 192. CUT Hemlock
- **Done:** 2026-07-11 (claude) — User call. Deleted [Hemlock.cs] (Rare Power card) + [HemlockPower.cs] (passive marker), loc keys `THEWITCH-HEMLOCK.*` / `THEWITCH-HEMLOCK_POWER.*`, art `hemlock.png` both sizes + `.import`s, and the Hemlock branch in [BramblesPower.cs](../TheWitchCode/Powers/BramblesPower.cs) `BeforeDamageReceived` (retaliation no longer checks for the marker). Historical mentions in `sfx-vfx-proposal.md` / `nameideas.md` left as design record (Scout Weakness precedent). No other references.
- **Verified:** build 0/0; regen (94 cards, −Hemlock). ⚠️ Playtest: bramble retaliation still damages + decrements normally.

### 191. New card: Volatile Vapors — potion payoff Power
- **Done:** 2026-07-11 (claude) — User pick (item 20): "damage a random enemy". Uncommon Power, 1E, Self: apply [VolatileVaporsPower](../TheWitchCode/Powers/VolatileVaporsPower.cs) 6 (upgrade +3; user-tuned from my 4/+2) — whenever you use OR create a Potion, deal `Amount` damage to a random enemy. Hooks `AfterPotionUsed` + `AfterPotionProcured` (Cloak of Moonlight pair); random target = `Rng.CombatTargets.NextItem(HittableEnemies)` (base-game AttackCommand pattern); `ValueProp.Unpowered`, Counter stack. Tagged Potions/Payoff. **Missing art** — placeholder fallback; follow-up: `big/volatile_vapors.png` → `Images: Generate missing sizes` → `Godot: Import assets`. Numbers (4/+2) are a first guess — tune after play.
- **Verified:** build 0/0. ⚠️ Playtest: triggers on both drink and brew; both hooks fire once each when a potion is created *then* used; random targeting in multi-enemy fights.

### 190. Bottle Barrage → Common (item 15 resolution)
- **Done:** 2026-07-11 (claude) — User pick: instead of reworking a Common generator, [BottleBarrage.cs](../TheWitchCode/Cards/BottleBarrage.cs) moves Uncommon → Common, filling the zero-potion-payoffs-at-Common gap while keeping the brew-trio generators intact. Stats untouched (10 dmg × potions created, upgrade +3). TESTED auto-cleared.
- **Verified:** build 0/0 (shared gate with 191); regen run. ⚠️ Playtest: Common reward pools now offer it.

### 189. Familiar tokens created BEFORE the hand draw (front of hand)
- **Done:** 2026-07-11 (claude) — First pass kept `AfterPlayerTurnStart` + `CardPilePosition.Top` (front-of-hand already held); user then pointed at the base-game **SentryModePower** — [FamiliarPower.cs](../TheWitchCode/Powers/FamiliarPower.cs) now overrides `BeforeHandDraw(player, ctx, combatState)` instead, so tokens enter the hand *before* the turn's draw (matching base-game turn-start card generation). Kept `Top` to stay in front of retained cards; generated-card funnel unchanged (Cloak of Moonlight still triggers).
- **Verified:** build 0/0. ⚠️ Playtest: tokens appear before the draw animation; retained-cards ordering; Cloak of Moonlight still procs on familiar tokens.

### 188. Extract Essence — always creates a Common potion
- **Done:** 2026-07-11 (claude) — [ExtractEssence.cs](../TheWitchCode/Cards/ExtractEssence.cs): removed the encounter-tier rarity switch (Boss→Rare / Elite→Uncommon); on-unblocked-damage potion is now always `PotionRarity.Common`. Loc unchanged (never mentioned rarity).
- **Verified:** build 0/0 (shared gate with 185–188).

### 187. Witch Strike cards tagged CardTag.Strike
- **Done:** 2026-07-11 (claude) — `CanonicalTags => { CardTag.Strike }` added to [StrikeFear.cs](../TheWitchCode/Cards/StrikeFear.cs), [VexingStrike.cs](../TheWitchCode/Cards/VexingStrike.cs), [BroomStrike.cs](../TheWitchCode/Cards/BroomStrike.cs) (StrikeWitch already had it). These are all the Strike-named Witch attacks — Strike-tag synergies (e.g. Fasten-style Defend/Strike lookups) now see them.
- **Verified:** build 0/0 (shared gate).

### 186. Knowledge can no longer copy Knowledge
- **Done:** 2026-07-11 (claude) — Infinite-spell loop: [Knowledge.cs](../TheWitchCode/Cards/Familiar/Knowledge.cs) hand-select now filters `c => c is not Knowledge` — by TYPE, not instance, since two Knowledges copying each other is the same loop. Loc: "a non-[gold]Knowledge[/gold] card". TESTED auto-cleared.
- **Verified:** build 0/0 (shared gate). ⚠️ Playtest: Knowledge greyed out in its own select screen.

### 185. Pocket Rats upgrade no longer upgrades the Rats
- **Done:** 2026-07-11 (claude) — [PocketRats.cs](../TheWitchCode/Cards/PocketRats.cs): `CreateFamiliarCards<Rats>(..., isUpgraded: false)` + hover tip shows plain Rats; loc dropped `{IfUpgraded:show:Rats+|Rats}`. Upgrade = +1 Rat only. (Refuse Pile still grants upgraded Rats — note named only Pocket Rats.)
- **Verified:** build 0/0 (shared gate).

### 184. Vial of Smoke — usable on allies (MP)
- **Done:** 2026-07-11 (claude) — [VialOfSmoke.cs](../TheWitchCode/Potions/VialOfSmoke.cs): `TargetType.Self` → `AnyPlayer` (base-game BlockPotion shape — self in SP, self-or-ally selection in MP); `OnUse` now blocks the chosen `target` with `AssertValidForTargetedPotion` guard. Loc unchanged — base game keeps "Gain {Block} Block" wording for AnyPlayer potions.
- **Verified:** build 0/0. ⚠️ Playtest (MP): target ally → ally gets the 8 Block; SP unchanged.

### 183. Mushroom Extract — mushroomed cards hide their tooltips
- **Done:** 2026-07-11 (claude) — Keyword/status hover tips leaked the real card through the gibberish. New `MushroomedHoverTipsPatch` in [MushroomedCards.cs](../TheWitchCode/Patches/MushroomedCards.cs): Harmony postfix on `CardModel.get_HoverTips` returns empty for marked cards (that one getter feeds keywords, GainsBlock, enchantment, ExtraHoverTips — all suppressed).
- **Verified:** build 0/0. ⚠️ Playtest: hover a mushroomed Exhaust/Block card → no tooltips.

### 182. Bottomless Cauldron power now stacks
- **Done:** 2026-07-11 (claude) — Note said "Noxious brew power effect should stack"; the power creating Noxious Brews is [BottomlessCauldronPower.cs](../TheWitchCode/Powers/BottomlessCauldronPower.cs). `PowerStackType.Single` → `Counter`; each potion used now procures `Amount` brews (loop). Loc smartDescription shows `{Amount}` with plural tag. Casting Bottomless Cauldron twice → 2 brews per potion.
- **Verified:** build 0/0. ⚠️ Playtest: 2 stacks → 2 brews; brew-exclusion loop guard intact.

### 181. Tinder — dropped "Can only be played if" description line
- **Done:** 2026-07-11 (claude) — `THEWITCH-TINDER.description` first line removed; now "Lose 1 [gold]Brambles[/gold]. Gain {Energy:diff()} [gold]Energy[/gold]." `IsPlayable` gate + gold glow unchanged in [Tinder.cs](../TheWitchCode/Cards/Tinder.cs).
- **Verified:** build 0/0 (shared gate with 176–180).

### 180. Wax and Wane — upgrade is now +4 Block only (no extra Hex)
- **Done:** 2026-07-11 (claude) — [WaxAndWane.cs](../TheWitchCode/Cards/WaxAndWane.cs) `OnUpgrade`: Block +2 → **+4**, `HexPower.UpgradeValueBy(1m)` removed (Hex stays 1). **Design call:** note didn't specify the block amount; +4 chosen to match peer uncommon-skill upgrades (Refuse Pile) — adjust if you wanted a different number.
- **Verified:** build 0/0 (shared gate).

### 179. Refuse Pile — 2 Energy, 12 Block
- **Done:** 2026-07-11 (claude) — [RefusePile.cs](../TheWitchCode/Cards/RefusePile.cs): cost 1 → 2, base Block 11 → 12. Upgrade (+4 Block) and the 2+2 Rats unchanged.
- **Verified:** build 0/0 (shared gate).

### 178. Brambleburst — upgrade +13 → +2 damage
- **Done:** 2026-07-11 (claude) — [Brambleburst.cs](../TheWitchCode/Cards/Brambleburst.cs) `OnUpgrade` `UpgradeValueBy(13m)` → `2m` (10 → 12 per bramble hit upgraded).
- **Verified:** build 0/0 (shared gate).

### 177. CUT Unstable Reaction
- **Done:** 2026-07-11 (claude) — User: "Not a fun card." Deleted `UnstableReaction.cs`, loc keys `THEWITCH-UNSTABLE_REACTION.*`, art `unstable_reaction.png` both sizes + `.import`s. No orphans — card granted nothing; grep clean (only Docs, fixed by regen).
- **Verified:** build 0/0 (shared gate); regen run at end of batch.

### 176. Salt and Ash — debuff check missed sign-flipping powers (bug)
- **Done:** 2026-07-11 (claude) — Root cause: bonus-block condition used `p.Type == PowerType.Debuff`, but powers like negative Strength/Dexterity have `Type = Buff` and only read as debuffs via `TypeForCurrentAmount` (base-game Misery/Rend pattern). So with e.g. −2 Strength the bonus 6 Block never triggered. [SaltAndAsh.cs](../TheWitchCode/Cards/SaltAndAsh.cs) now checks `TypeForCurrentAmount`. (Note: Frail *reducing* both block gains is correct game behavior, not this bug.)
- **Verified:** build 0/0 (shared gate). ⚠️ Playtest: bonus triggers with negative Strength; still triggers with Frail/Weak/Vulnerable.

### 175. Build/portability hardening
- **Done:** 2026-07-08 (claude) — All 7 sub-items:
  1. `local.props` import wired at top of [Directory.Build.props](../Directory.Build.props) (+ example block in comments); `/local.props` gitignored.
  2. `GodotPath` moved out of tracked props into machine-local `local.props` (created here with the megadot path — untracked).
  3. Pinned `Version="*"` → BaseLib `3.3.*`, ModAnalyzers `0.1.*` in [TheWitch.csproj](../TheWitch.csproj) (restores 3.3.5 / 0.1.9 today).
  4. `UpdateDependencyVersions` hardened: `Lines="$([MSBuild]::Escape($(NewContent)))"` (no `;` splitting), `Encoding` attribute dropped (UTF-8 no BOM on .NET SDK MSBuild), message fixed to `$(ActiveBaseLibVersion)`.
  5. Steam secondary-library gap documented in [Sts2PathDiscovery.props](../Sts2PathDiscovery.props) header (escape hatch = `Sts2Path` in local.props) — chose docs over vdf parsing per the item's own alternative.
  6. Manifest version: stale sub-item — already `v0.0.3` and bumped per release; no change.
  7. [.gitattributes](../.gitattributes): explicit `binary` markers for png/jpg/webp/pck/dll/pdb/exe/mp3/wav/ogg/ttf/otf.
- **Verified:** build 0/0; `dotnet msbuild -getProperty:GodotPath` resolves via local.props; round-trip test: semicolon'd description + forced `min_version` rewrite → semicolon survived, version updated to 3.3.5, first bytes `7b 0a` (no BOM). Test semicolon reverted.

### 174. CUT Scout Weakness
- **Done:** 2026-07-08 (claude) — Crow token card deleted: `ScoutWeakness.cs(.uid)`, loc keys `THEWITCH-SCOUT_WEAKNESS.*`, art `familiar/scout_weakness.png` both sizes + `.import`s. [CrowFamiliarPower.cs](../TheWitchCode/Powers/CrowFamiliarPower.cs) loot table now ClawEyes 2 / Shiny 1; hover tip removed from [CrowFamiliar.cs](../TheWitchCode/Cards/CrowFamiliar.cs). No orphans (Vulnerable = base-game). Historical mention left in sfx-vfx-proposal.md (design record).
- **Verified:** build 0/0; regen (101 cards, −Scout Weakness). ⚠️ Playtest: Crow turn-start roll (2:1 ClawEyes/Shiny).

### 173. Brew puff vfx → fire smoke puff
- **Done:** 2026-07-08 (claude) — Global potion-creation signature no longer reads as farting (user pick: fire smoke puff). [WitchFx.cs](../TheWitchCode/Extensions/WitchFx.cs) `BrewPuff` now spawns `NFireSmokePuffVfx` (fiery smoke + embers); EnergyPotion yellow tint kept via the same Clouds-material recolor (node name "Clouds" exists in both scenes). [Witch.cs](../TheWitchCode/Character/Witch.cs) `ExtraAssetPaths`: `NSmokePuffVfx`→`NFireSmokePuffVfx`. Affects ALL Witch potion creation (Extract Essence, brews, Experiment, …).
- **Verified:** build 0/0. ⚠️ Playtest: puff look + yellow tint on Energy Potion (Hasty Brew) — tint recolors a fire cloud now, verify it still reads.

### 172. New card: Big Batch — create Noxious Brews
- **Done:** 2026-07-10
- **Changed:** New 2E Common Skill (Self): create 2 Noxious Brews (upgrade +1). `TryToProcure<NoxiousBrew>` loop, Noxious Brew hover tip, `{Brews:diff()}` + plural loc. **Missing art** — placeholder fallback; follow-up: add `big/big_batch.png` → `Images: Generate missing sizes` → `Godot: Import assets`.
- **Files:** new `TheWitchCode/Cards/BigBatch.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** brews land in belt (or overflow behavior when belt full).

### 171. Content cut: Slicing Brew; Prices Paid → Noxious Brew
- **Done:** 2026-07-10
- **Changed:** Deleted SlicingBrew potion (card-only payload). Prices Paid now procures NoxiousBrew (hover tip + card loc: "Create N Noxious Brews"). Removed potions.json keys, PotionTraits.Manual row, art-tracker assets.json row (no art file existed). No other refs.
- **Files:** deleted `TheWitchCode/Potions/SlicingBrew.cs(.uid)`; `TheWitchCode/Cards/PricesPaid.cs`, `TheWitchCode/Potions/Brewing/PotionTraits.cs`, `TheWitch/localization/eng/{cards,potions}.json`, `Docs/art-tracker/assets.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 170. Bugfix: Neverending Potion + Distilled Chaos turn lock
- **Done:** 2026-07-10
- **Changed:** Replay hook moved `AfterPlayerTurnStart` → `AfterAutoPrePlayPhaseEntered`. Root cause: replay ran during the `Start` turn phase; a bottled Distilled Chaos auto-plays cards there, the phase never advanced to `Play`, and the whole turn locked (no cards playable, effect looked like it "never procced"). `AutoPrePlay` is the game's designated hook for turn-start card auto-play (base-game Mayhem pattern); all bottled potions now replay there (after hand draw — slightly later, same turn).
- **Files:** `TheWitchCode/Powers/NeverendingPotionPower.cs`
- **Verified:** dotnet build 0/0. **Playtest flag:** bottle Distilled Chaos, confirm turn proceeds + 3 cards auto-play; sanity-check a damage and a buff potion still replay.

### 169. Card change: Read the Bones — upgrade adds +1 Hex
- **Done:** 2026-07-10
- **Changed:** `OnUpgrade` now bumps both Cards +1 (existing) and HexPower +1 (new). Base 1 Hex unchanged. (Staging note revised mid-loop from "base +1 hex" to "on upgrade" — final state matches the revision.)
- **Files:** `TheWitchCode/Cards/ReadTheBones.cs`, regenerated docs
- **Verified:** dotnet build 0/0

### 168. Card rework: Eye of Newt — stacking potion-damage multiplier
- **Done:** 2026-07-10 (revised same day per user: upgrade = triple, not quadruple)
- **Changed:** Power multiplier now **linear**: `1 + Amount` (+100% potion damage per stack) instead of old +Amount% (was +50%, upgrade +25%). Card = 1 stack (double); upgrade = 2 stacks (triple); stacks add (two base copies also triple). Card loc via `{IfUpgraded:show:triple|double}`; power smartDescription `+{Amount}00%`.
- **Files:** `TheWitchCode/Cards/EyeOfNewt.cs`, `TheWitchCode/Powers/EyeOfNewtPower.cs`, `TheWitch/localization/eng/cards.json`, `powers.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** two copies played = 4x; upgraded = 4x per card.

### 167. Card change: Tinder — requires Brambles to play
- **Done:** 2026-07-10
- **Changed:** Added `IsPlayable => GetPower<BramblesPower>() is { Amount: > 0 }` + `ShouldGlowGoldInternal` (base-game Clash pattern → greys out / shows unplayable reason at 0 Brambles). `OnPlay` now early-returns without granting Energy if forced-played with no Brambles. Loc: "Can only be played if you have Brambles."
- **Files:** `TheWitchCode/Cards/Tinder.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** auto-play sources (Distilled Chaos, Mayhem) route it to discard unplayed at 0 Brambles.

### 166. Card change: Lavender and Sage — back to 1 draw, upgrade +1 draw
- **Done:** 2026-07-10
- **Changed:** Reverted to pre-7843832 shape: `CardsVar(1)`, `OnUpgrade` = Cards +1 (Brambles upgrade removed; base 4 Brambles unchanged). Matches git history exactly.
- **Files:** `TheWitchCode/Cards/LavenderAndSage.cs`, regenerated docs
- **Verified:** dotnet build 0/0

### 165. Card change: Polymorph — becomes a Rats, Exhausts
- **Done:** 2026-07-09
- **Changed:** Transform target: random `IFamiliarSummon` summon → the **Rats** token (note's "a rat"). Added `Exhaust` to `CanonicalKeywords` — the existing `OnUpgrade → RemoveKeyword(Exhaust)` was previously dangling (card never had Exhaust); now it's the real upgrade. 0E Rare Skill unchanged; combat-scoped `CardCmd.Transform` kept; Rats hover tip added; loc + selection prompt updated (keyword banner renders itself).
- **Files:** `TheWitchCode/Cards/Polymorph.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** transform persists for the combat; upgraded copy replays after redraw.

### 164. Fix: Wisdom upgrade typo (user WIP)
- **Done:** 2026-07-09
- **Changed:** `DynamicVars.Cards..UpgradeValueBy(1m)` (double dot) → single dot; unblocked the build gate. Wisdom's new upgrade (+1 draw) is the user's own in-progress edit, untouched otherwise.
- **Files:** `TheWitchCode/Cards/Familiar/Wisdom.cs`
- **Verified:** dotnet build 0/0

### 163. Card change: Knowledge — copy a card in hand
- **Done:** 2026-07-09
- **Changed:** Owl token was upgrade-a-card (ALL when upgraded); now **"Create a copy of a card in your Hand"**, upgrade → **2 copies**. Base-game Dual Wield pattern: `CardSelectCmd.FromHand` (no type filter) → `CreateClone()` per copy → `CardPileCmd.AddGeneratedCardToCombat` (creation payoffs fire). Kept the enchant shimmer vfx. Loc + selectionScreenPrompt.
- **Files:** `TheWitchCode/Cards/Familiar/Knowledge.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** copies of X-cost/upgraded cards clone correctly; Cloak of Moonlight triggers per copy.

### 162. Content cut: Find Familiar
- **Done:** 2026-07-09
- **Changed:** Deleted the familiar-power tutor (select `IFamiliarSummon` cards from draw pile) — card, loc (incl. selectionScreenPrompt), art. No other code refs.
- **Files:** deleted `TheWitchCode/Cards/FindFamiliar.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}find_familiar.png(.import)`, loc keys, regenerated docs
- **Verified:** dotnet build 0/0

### 161. New familiar card: Swarm (Rat token)
- **Done:** 2026-07-09
- **Changed:** New rat token (0E Skill, Token, `IRatCard`, Exhaust via base): **"Shuffle 5 Rats into your Draw Pile."** Upgrade +2 (→7). Call the Pack pattern (`FamiliarCardRegistry.CreateFamiliarCards<Rats>` → `AddGeneratedCardsToCombat(Draw, Random)` + pile-add preview); passes `IsUpgraded`, so Swarm+ shuffles **Rats+** (matches summon upgrade flow; loc shows `Rats+`). Rats hover tip.
- **Files:** new `TheWitchCode/Cards/Familiar/Swarm.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Placeholder art** — needs `card_portraits/{,big/}familiar/swarm.png` (regen flags it). Playtest: generated-card payoffs (Cloak of Moonlight) fire per rat.

### 160. New familiar card: Rummage (Rat token)
- **Done:** 2026-07-09
- **Changed:** New rat token (0E Skill, Token, `IRatCard`, Exhaust via base): **"Put 1 card from your Discard Pile into your Hand."** Upgrade +1. Base-game Dredge pattern (`CardSelectCmd.FromCombatPile` on discard + hand-cap clamp). SelectionScreenPrompt loc included.
- **Files:** new `TheWitchCode/Cards/Familiar/Rummage.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Placeholder art** — needs `card_portraits/{,big/}familiar/rummage.png`. Playtest: empty discard = no-op; full hand clamps.

### 159. Content cut: Plague (Rat token)
- **Done:** 2026-07-09
- **Changed:** Deleted the Hex-AoE rat token — card, loc, art (in the `familiar/` subfolders). Removed from `RatFamiliarPower` loot table; comment refs updated (`IRatCard`, `CombatHistoryQueries`). New table: **Rats / Rummage / Swarm equal weight**.
- **Files:** deleted `TheWitchCode/Cards/Familiar/Plague.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}familiar/plague.png(.import)`; edited `Powers/RatFamiliarPower.cs`, `Cards/Familiar/IRatCard.cs`, `Extensions/CombatHistoryQueries.cs`; loc keys, regenerated docs
- **Verified:** dotnet build 0/0

### 158. Card change: Rat Familiar — Rare
- **Done:** 2026-07-09
- **Changed:** Rarity Uncommon→**Rare** (1E Power unchanged). "Upgrade → cards are upgraded" was already the standard summon behavior (`GainFamiliar` → `GrantsUpgradedCards`) — no code change. Hover tips now Rats / Rummage / Swarm.
- **Files:** `TheWitchCode/Cards/RatFamiliar.cs`, regenerated docs
- **Verified:** dotnet build 0/0

### 157. Card change: Ferocity — hits scale with Attacks played this turn
- **Done:** 2026-07-09
- **Changed:** Cat token was "Deal 7 damage twice"; now **"Deal 7 damage for each Attack played this turn"** — Barrage live hit-count shape (`CalculationBaseVar(0) + CalculationExtraVar(1) + CalculatedVar("CalculatedHits")`), count from `CombatManager.Instance.History.CardPlaysStarted` (Normality pattern): this-turn Attack plays by owner, **excluding Ferocity itself** so the in-hand preview equals the hits dealt (0 prior attacks = whiff). Upgrade kept +3 dmg. Loc uses the `{InCombat:(Hits …)}` display.
- **Files:** `TheWitchCode/Cards/Familiar/Ferocity.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** hit count updates live as attacks are played; confirm self-exclusion feels right (played as first attack = whiff).

### 156. Card change: Mutilate — 3E / 40 damage
- **Done:** 2026-07-09
- **Changed:** Bear token: 2E→**3E**, 22→**40** damage, upgrade +8→**+15**. Unblockable mechanic + vfx unchanged.
- **Files:** `TheWitchCode/Cards/Familiar/Mutilate.cs`, regenerated docs
- **Verified:** dotnet build 0/0

### 155. Card change: Hibernate — block now, Vigor next turn
- **Done:** 2026-07-09
- **Changed:** Bear token: was 15 Block + heal 2; now **"Gain 15 Block. Next turn, gain 10 Vigor."** Upgrade +5 Block / +3 Vigor (was +5/+2 heal). New **`VigorNextTurnPower`** (no base-game Vigor variant of the *NextTurn powers): `AfterPlayerTurnStart` → apply Amount Vigor → remove self; Counter stacks so two Hibernates pool into one payout. New power loc.
- **Files:** `TheWitchCode/Cards/Familiar/Hibernate.cs`, new `TheWitchCode/Powers/VigorNextTurnPower.cs`, `TheWitch/localization/eng/{cards,powers}.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** Vigor lands exactly once at next turn start; power icon needs art (`images/powers/vigor_next_turn_power.png` — placeholder fallback for now).

### 154. Card change: Taste of Blood — Vigor cantrip
- **Done:** 2026-07-09
- **Changed:** Was 0E Common Attack (3 dmg + 3 Vigor); now **1E Uncommon Skill: "Gain 5 Vigor. Draw 2 cards."** Upgrade +3 Vigor (draw not upgraded). Damage/target dropped; Cast anim added.
- **Files:** `TheWitchCode/Cards/TasteOfBlood.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 153. Content cut: Woe and Whimsy
- **Done:** 2026-07-09
- **Changed:** Deleted card (Vigor + Block skill) + loc + art. No other code refs.
- **Files:** deleted `TheWitchCode/Cards/WoeAndWhimsy.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}woe_and_whimsy.png(.import)`, loc keys, regenerated docs
- **Verified:** dotnet build 0/0

### 152. Content cut: Nettles
- **Done:** 2026-07-09
- **Changed:** Deleted card (bramble-scaling AoE) + loc + art files. Art content preserved — moved onto Deep Roots first (item 151). No other code refs.
- **Files:** deleted `TheWitchCode/Cards/Nettles.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}nettles.png(.import)`, loc keys, regenerated docs
- **Verified:** dotnet build 0/0

### 151. Art move: Nettles → Deep Roots
- **Done:** 2026-07-09
- **Changed:** Overwrote `deep_roots.png` (small + big) with the Nettles art bytes; kept Deep Roots' existing `.import` files (same path — Godot just re-imports). Nettles art files then deleted with the cut.
- **Files:** `TheWitch/images/card_portraits/{,big/}deep_roots.png`
- **Verified:** files copied before delete. **Run Godot "Import assets"** to re-import.

### 150. Card rename: Scavengers → Rats
- **Done:** 2026-07-09
- **Changed:** Rat familiar token renamed (mechanics untouched: 0E Token Attack, 5 dmg / heal 1 / draw 1, upgrade +3). Class/file (+`.uid`) `Scavengers` → `Rats`; loc keys `THEWITCH-SCAVENGERS.*` → `THEWITCH-RATS.*`; refs updated in `RatFamiliarPower` loot table, `PocketRats`, `RefusePile`, `RatFamiliar`, `IRatCard`, `CombatHistoryQueries`; `REFUSE_PILE`/`POCKET_RATS` loc text now says `{IfUpgraded:show:Rats+|Rats}`. Art lived in the **familiar/** subfolder — renamed `card_portraits/{,big/}familiar/scavengers.png` → `rats.png`, stale `.import` deleted (correction after initial miss; run Godot "Import assets").
- **Files:** `TheWitchCode/Cards/Familiar/Rats.cs(.uid)` (renamed), `Powers/RatFamiliarPower.cs`, `Cards/{PocketRats,RefusePile,RatFamiliar}.cs`, `Cards/Familiar/IRatCard.cs`, `Extensions/CombatHistoryQueries.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 149. Content cut: Pact of Beasts
- **Done:** 2026-07-09
- **Changed:** Deleted the familiar tutor (1E Rare Skill: lose 3 HP, pull all familiar cards to hand) — card, loc, art. No power/payload of its own.
- **Files:** deleted `TheWitchCode/Cards/PactOfBeasts.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}pact_of_beasts.png(.import)`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 148. Content cut: Soul Knot
- **Done:** 2026-07-09
- **Changed:** Deleted card + `SoulKnotPower` (debuff-mirror power) + loc (card + power) + art. Grep confirmed no other references.
- **Files:** deleted `TheWitchCode/Cards/SoulKnot.cs(.uid)`, `TheWitchCode/Powers/SoulKnotPower.cs(.uid)`, art, loc keys in `cards.json`/`powers.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 147. Content cut: Hedge Prison
- **Done:** 2026-07-09
- **Changed:** Deleted card + `HedgePrisonPower` + loc (card + power) + art. Cascade: removed the `GetPowerAmount<HedgePrisonPower>()` permanence check in `BramblesPower.BeforeDamageReceived` — bramble retaliation always decrements again.
- **Files:** deleted `TheWitchCode/Cards/HedgePrison.cs(.uid)`, `TheWitchCode/Powers/HedgePrisonPower.cs(.uid)`, art; edited `TheWitchCode/Powers/BramblesPower.cs`; loc keys in `cards.json`/`powers.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 146. Card change: Hexblast — Hex-scaling AoE detonation
- **Done:** 2026-07-09
- **Changed:** Was "deal 20 per unique debuff on one enemy, then remove them" (2E Rare). Now **"Apply 3 Hex to ALL enemies. Then deal 10 damage to each for every Hex on them."** — TargetType AllEnemies; Hex applied first so the fresh 3 count; per-enemy single hit of `10 × that enemy's Hex`; debuffs no longer removed. Cost/rarity kept; upgrade kept as +3 per-Hex damage (note silent on both). Kept purple ground-fire vfx + screen shake. Hex hover tip added.
- **Files:** `TheWitchCode/Cards/Hexblast.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** per-enemy damage with mixed Hex counts; 13/hex upgraded is a big number — sanity-check balance in-game.

### 145. Card change: Experiment → Eye of Newt — potion damage amp
- **Done:** 2026-07-09
- **Changed:** Full rename + redesign. Was 1E Common Skill (block + swap random belt potion); now **Eye of Newt, 1E Uncommon Power: "Your Potions deal 50% more damage"**, upgrade +25% (→75%). New `EyeOfNewtPower`: potion damage carries no potion identity into the damage pipeline, so `BeforePotionUsed`/`AfterPotionUsed` bracket the use with a transient bool and `ModifyDamageMultiplicative` returns `1 + Amount/100` while flagged and `dealer == Owner` (verified: `CreatureCmd.Damage` → `Hook.ModifyDamage` runs the full hook chain for potion damage; plain field safe per SP/lockstep-MP rules). Counter stacking: second cast = +100%. Renamed class/file (+`.uid`), loc keys `THEWITCH-EXPERIMENT.*` → `THEWITCH-EYE_OF_NEWT.*`, new power loc, art `experiment.png` → `eye_of_newt.png` (small+big), stale `.import` deleted. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/EyeOfNewt.cs(.uid)` (renamed), new `TheWitchCode/Powers/EyeOfNewtPower.cs`, `TheWitch/localization/eng/{cards,powers}.json`, `TheWitch/images/card_portraits/{,big/}eye_of_newt.png`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** fire/attack potion damage ×1.5 while power active; damage from potion-applied poison ticks should NOT be amplified; MP rejoin loses the transient flag mid-potion only (accepted). **Run Godot "Import assets"** for the renamed art.

### 144. Content cut: Rake (cascade from Wicker Form redesign)
- **Done:** 2026-07-09
- **Changed:** User-confirmed cascade-cut of the orphaned Rake token: deleted `Cards/Rake.cs(.uid)`, loc keys `THEWITCH-RAKE.*`, and art (`rake.png` small/big + `.import`). No remaining references (grep clean). **Follow-up:** run the Godot "Import assets" task to drop the stale import cache.
- **Files:** deleted `TheWitchCode/Cards/Rake.cs(.uid)`, `TheWitch/images/card_portraits/{,big/}rake.png(.import)`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 143. Fix: pre-existing compile errors in Plague / Read the Bones
- **Done:** 2026-07-09
- **Changed:** Two API typos in uncommitted WIP (not from this loop) blocked the build gate: `Plague.OnUpgrade` used nonexistent `UpgradeBy(1)` → `UpgradeValueBy(1m)`; `ReadTheBones.OnUpgrade` used `DynamicVars.CardsVar` → `DynamicVars.Cards`.
- **Files:** `TheWitchCode/Cards/Familiar/Plague.cs`, `TheWitchCode/Cards/ReadTheBones.cs`
- **Verified:** dotnet build 0/0

### 142. Card change: Wicker Form — turn-start bramble engine
- **Done:** 2026-07-09
- **Changed:** Redesigned from "creations become Rake" to **"At the start of your turn, gain 20 Brambles"** (3E Rare Power unchanged; upgrade now +10 Brambles instead of cost −1). `WickerFormPower` reworked to the DeepRootsPower shape (`AfterPlayerTurnStart` → `PowerCmd.Apply<BramblesPower>`); **deleted `Patches/WickerFormReplacementPatch.cs`** (would otherwise still hijack all card/potion creation). Loc updated (card + power). **Orphan:** `Cards/Rake.cs` (+ loc/art) is now created by nothing — left in place pending user call on cascade-cut. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/WickerForm.cs`, `TheWitchCode/Powers/WickerFormPower.cs`, deleted `TheWitchCode/Patches/WickerFormReplacementPatch.cs(.uid)`, `TheWitch/localization/eng/{cards,powers}.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** turn-start bramble grant + no more Rake replacement.

### 141. Card change: A Little Sip — +1 Strength per potion
- **Done:** 2026-07-09
- **Changed:** `ALittleSipPower` now also grants **1 Strength per potion used** (flat, not upgraded — heal still scales with Amount). Strength applied via `PowerCmd.Apply<StrengthPower>` with `new ThrowingPlayerChoiceContext()` (base-game ReptileTrinket pattern — `AfterPotionUsed` has no choiceContext). Loc updated (card + power). TESTED auto-cleared.
- **Files:** `TheWitchCode/Powers/ALittleSipPower.cs`, `TheWitch/localization/eng/{cards,powers}.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** strength stacks per potion incl. token potions.

### 140. Card change: Cursed Spellbook — energy for HP
- **Done:** 2026-07-09
- **Changed:** Power now **"At the start of your turn, gain 1 Energy and lose 1 HP"** (upgrade: 2/2 — one shared Amount drives both). Dropped the draw-1-fewer penalty (`ModifyHandDraw` removed). Energy kept via `ModifyEnergyGain`; HP tick via `AfterPlayerTurnStart` + `CreatureCmd.Damage` Unblockable|Unpowered (Wormy pattern). Card shell (0E Rare Power, `EnergyVar(1)`, upgrade +1) unchanged. Loc updated (card + power). TESTED auto-cleared.
- **Files:** `TheWitchCode/Powers/CursedSpellbookPower.cs`, `TheWitch/localization/eng/{cards,powers}.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** HP loss can kill the player — confirm intended; Single stack means replay refreshes, never 2×.

### 139. Card change: Overrun — one AoE hit scaling with familiars
- **Done:** 2026-07-09
- **Changed:** Now **"Deal 8 damage to ALL enemies. Deals 8 additional damage for each Familiar."** — single hit, Soul Storm live-calc shape (`CalculationBaseVar(8) + ExtraDamageVar(8) + CalculatedDamageVar.WithMultiplier(Familiars.Count)`), TargetType → AllEnemies. Was: 8 single-target + 8×familiars AoE hit-count. Upgrade: +4 per-familiar (ExtraDamage). Loc uses the SOUL_STORM token convention. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Overrun.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** displayed damage should update live as familiars are summoned.

### 138. Card change: Brambleburst — hits ALL enemies
- **Done:** 2026-07-09
- **Changed:** TargetType AnyEnemy → **AllEnemies**, `Targeting(target)` → `TargetingAllOpponents` (per-bramble hit count + lose-all-brambles unchanged). Loc: "to ALL enemies" added. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Brambleburst.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 137. Card change: Tinder — burns a bramble instead of a card
- **Done:** 2026-07-09
- **Changed:** Now **"Lose 1 Brambles. Gain 2 Energy."** (0E Common Skill, upgrade +1 Energy unchanged). Replaced the exhaust-a-card selection with best-effort `GetPower<BramblesPower>()` → `PowerCmd.Decrement` (design call: no play requirement — 0 brambles still gains energy). Brambles hover tip added. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Tinder.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 136. Card change: Lavender and Sage — draw 2, brambles 4, upgrade +2 brambles
- **Done:** 2026-07-09
- **Changed:** Draw 1→**2**, Brambles 5→**4**; upgrade now **+2 Brambles** (was +1 card). Loc text already var-driven — no change needed. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/LavenderAndSage.cs`, regenerated docs
- **Verified:** dotnet build 0/0

### 135. Card change: Broom Strike — 1E hit + next-Skill discount
- **Done:** 2026-07-09
- **Changed:** 2E→**1E**, damage 12→**8** (upgrade +3 kept); replaced the play-random-Skill-from-draw effect with **"Your next Skill costs 1 less"** — reuses the existing `NextSkillDiscountPower` (Weathered Witch Hat's power; FreeSkillPower consume pattern) via `PowerCmd.Apply(1)`. Hover tip added. Loc updated. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/BroomStrike.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** discount stacks if two Broom Strikes played (2 next skills, 1 less each — power is Counter).

### 134. Card change: Ambush! — Rare, 10 AoE, auto-plays when drawn
- **Done:** 2026-07-09
- **Changed:** Common→**Rare**, damage 8→**10** (upgrade +3 kept, note didn't specify); added **"Whenever you draw this card, play it for free"** — `AfterCardDrawn` (card == this) → `CardCmd.AutoPlay(this, null)` (KinglyPunch/Void hook + BroomStrike AutoPlay pattern; AutoPlay spends no energy). Loc updated. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Ambush.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** draw-triggered auto-play mid-draw (opening hand, multi-draw effects) — verify no re-entrancy/ordering weirdness.

### 133. Card change: Ambush! — flat AoE
- **Done:** 2026-07-09
- **Changed:** Now just "Deal 8 damage to ALL enemies." (base was already 8; upgrade +3 kept). Removed the "Create a random Familiar card" line — `FamiliarCardRegistry.CreateRandom` + generated-cards block and the loc sentence. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Ambush.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 132. Card change: Catalyst — copy a random hand card on potion use
- **Done:** 2026-07-09
- **Changed:** `CatalystPower` redesigned: was "duplicate every potion you create" (`AfterPotionProcured` copy), now **"Whenever you use a Potion, create a copy of a random card in your Hand"** — `AfterPotionUsed` picks via seeded `Rng.CombatCardSelection`, clones with `CardModel.CreateClone()` (Dual Wield pattern), and inserts through `CardPileCmd.AddGeneratedCardToCombat` so creation payoffs (Cloak of Moonlight) fire. No choiceContext needed. Card shell (1E Ancient Power, upgrade cost −1) unchanged. Loc updated (card + power + smartDescription). TESTED auto-cleared.
- **Files:** `TheWitchCode/Powers/CatalystPower.cs`, `TheWitchCode/Cards/Catalyst.cs`, `TheWitch/localization/eng/{cards,powers}.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** copy fires per potion use incl. token potions; empty hand = no-op.

### 131. Card change: Wormy — removed "Gain 1 Weak"
- **Done:** 2026-07-09
- **Changed:** Wormy status token now just "Lose 1 HP." — dropped the `PowerVar<WeakPower>` var, the `PowerCmd.Apply<WeakPower>` line, the Weak hover tip (+ now-unused imports), and the loc line. Retain/Exhaust/vfx untouched. TESTED auto-cleared.
- **Files:** `TheWitchCode/Cards/Wormy.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 130. General change: combat-created potions can never heal the player
- **Done:** 2026-07-09
- **Changed:** `PotionCatalog.Query` gained `excludeHealing = true` (default ON) — every in-combat creation roll (Experiment, Grind Down, Extract Essence, Rip Soul, the orientation-brew trio, BrewBook brews, Distill) now filters `PotionTraits.IsHealing` automatically. `PotionUpgrade`'s now-redundant inline filter removed. `ShareTheBrew` uses the base-game `PotionFactory` (per-player color pools, MP) — filtered there by bounded re-roll (20 tries) instead of a pool query. Copy powers (Gather Herbs copy/Catalyst) untouched: they duplicate a potion the player already received, not a creation roll — and Catalyst is being redesigned (item 132). Out-of-combat drops/shops unaffected. Fixed payloads (Vial of Smoke, Noxious Brew) non-healing, unaffected.
- **Files:** `TheWitchCode/Potions/Brewing/PotionCatalog.cs`, `TheWitchCode/Potions/Brewing/PotionUpgrade.cs`, `TheWitchCode/Cards/ShareTheBrew.cs`
- **Verified:** dotnet build 0/0. **Playtest flag:** confirm brew/make-potion effects never yield Blood/Regen/Fairy/Fruit Juice/Wormy Apple; ShareTheBrew is MP-only (compile-checked only).

### 129. Card change: Distill — always Rare, prefer un-upgraded inputs, never healing potions
- **Done:** 2026-07-09
- **Changed:** Rewrote `PotionUpgrade.UpgradeRandomPotions` (Distill is its only caller; BrewBook/PotionMerge untouched): input pick shuffles the belt then sorts **non-Rare potions first** ("prioritize potions that aren't upgraded" read as not-already-Rare); result is always a **random Rare of the same orientation**, input excluded, **healing potions excluded**; falls back to any-orientation Rare if the orientation has no eligible Rare (mirrors BrewBook fallback). Added shared classifier `PotionTraits.IsHealing` — manual set (BloodPotion, RegenPotion, FairyInABottle, FruitJuice, WormyApple, TheCauldron) + inference fallback (HealVar/MaxHpVar/PowerVar&lt;RegenPower&gt;), reused by item 130. Loc: "Distill N random Potion(s) into random Rare Potion(s)." TESTED auto-cleared.
- **Files:** `TheWitchCode/Potions/Brewing/PotionUpgrade.cs`, `TheWitchCode/Potions/Brewing/PotionTraits.cs`, `TheWitchCode/Cards/Distill.cs` (doc), `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** distilling a Rare rerolls it into a different Rare (always-Rare rule); confirm that feels right.

### 127. Card change: Ritual Sacrifice → draw + Strength
- **Done:** 2026-07-09
- **Changed:** Redesigned to **1E, Uncommon, Skill — Sacrifice a Familiar. Draw 3 cards. Gain 5 Strength. Upgrade: +3 Strength (→8)**. Dropped Block(25)/Damage(25) + their upgrades, `GainsBlock`, enemy targeting (now `TargetType.Self`). Sacrifice-gating kept: no familiar = no effect (unchanged behavior). Loc rewritten with `{Strength:diff()}`. TESTED auto-cleared by regen.
- **Files:** `TheWitchCode/Cards/RitualSacrifice.cs`, `TheWitch/localization/eng/cards.json`, regenerated docs
- **Verified:** dotnet build 0/0

### 126. Redesign Hex — random evil-effect table
- **Done:** 2026-07-09
- **Changed:** Replaced the decrementing Strength-drain Hex with: **at end of the hexed enemy's turn, one random evil effect per stack, stacks persist (no decrement)**. Table: 10 damage (Unpowered, blockable, source = applier witch) / 1 Weak / 1 Vulnerable / steal 1 Strength (enemy −1, applier +1; grant fizzles if applier dead) / 6 Poison — base-game `WeakPower`/`VulnerablePower`/`StrengthPower`/`PoisonPower`. Roll via seeded `Rng.CombatTargets` (`NextInt(5)` per stack), reached through `Owner.CombatState.RunState` so it needs no live player ref. Kept `AfterApplied` HexGaze fx + `AfterSideTurnEnd` timing. Loc rewritten (effect table spelled out; plural tags). Design decisions: stacks persist per the "geometric scaling" intent (note didn't request decrement); damage left blockable (spec said "deal damage", not "lose HP"). Hex-applying cards' text untouched (they only say "Apply N Hex"). Memory `hex-design.md` rewritten.
- **Files:** `TheWitchCode/Powers/HexPower.cs`, `TheWitch/localization/eng/powers.json`
- **Verified:** dotnet build 0/0. **Playtest flag:** table weights are uniform, amounts (10/1/1/1/6) first-pass; check end-of-turn ordering vs Poison ticks and multi-stack spam pacing.

### 125. Cut Oxidizers (starter) — deck shrinks, Extract Essence becomes Rip Soul's transcendence source
- **Done:** 2026-07-09
- **Changed:** Cut card `Oxidizers` + `OxidizersPower` + art + loc ("weird and lame"). Starter deck now 11 cards (Oxidizers line removed from `Witch.cs`; user chose shrink over replacement). Rip Soul kept: `AncientTranscendencePatch` now maps **Extract Essence → Rip Soul** (user call; Extract Essence already a starter). Doc comments updated (`RipSoul.cs`, `NeverendingPotionPower.cs` no longer cites OxidizersPower — the OnUse-reflection pattern doc now lives there). `Docs/sfx-vfx-proposal.md` still mentions Oxidizers (stale design doc, left per surgical-change rule). Regen ran.
- **Files:** deleted `TheWitchCode/Cards/Oxidizers.cs(+.uid)`, `TheWitchCode/Powers/OxidizersPower.cs(+.uid)`, art `{,big/}oxidizers.png(+.import)`; edited `TheWitchCode/Character/Witch.cs`, `TheWitchCode/Patches/AncientTranscendencePatch.cs`, `TheWitchCode/Cards/RipSoul.cs`, `TheWitchCode/Powers/NeverendingPotionPower.cs`, loc `{cards,powers}.json`; regenerated docs
- **Verified:** dotnet build 0/0. **Playtest flag:** Archaic Tooth should now offer Rip Soul off Extract Essence.

### 125b. Rename Bottled Magic power → Neverending Potion
- **Done:** 2026-07-09
- **Changed:** User rename of Crystal Bottle's replay buff. Class/file `BottledMagicPower` → `NeverendingPotionPower`, loc keys `THEWITCH-BOTTLED_MAGIC_POWER.*` → `THEWITCH-NEVERENDING_POTION_POWER.*` (title "Neverending Potion"), refs in `CrystalBottle.cs` + `CrystalBottlePower.cs`. Power icon still placeholder (`powers/neverending_potion_power.png` missing).
- **Files:** `TheWitchCode/Powers/NeverendingPotionPower.cs`, `TheWitchCode/Powers/CrystalBottlePower.cs`, `TheWitchCode/Cards/CrystalBottle.cs`, `TheWitch/localization/eng/powers.json`
- **Verified:** dotnet build 0/0

### 125a. Cut Roomy Satchel (full cascade)
- **Done:** 2026-07-09
- **Changed:** User cut ("doesn't feel good"; the later staging note "Potion satchel doesn't feel good. Cut it." treated as duplicate of this). Full cascade per user choice: card `RoomySatchel`, its tracker power `RoomySatchelPower` (only the card applied it), and `PotionBeltShrinkPatch` (belt-shrink UI Harmony patch — nothing shrinks the belt anymore; LargePockets only grows it). Loc keys removed from cards.json + powers.json; art `{,big/}roomy_satchel.png(+.import)` deleted. CLAUDE.md's two bullets citing these files reworded to point at git history. Docs regenerated.
- **Files:** deleted `TheWitchCode/Cards/RoomySatchel.cs(+.uid)`, `TheWitchCode/Powers/RoomySatchelPower.cs(+.uid)`, `TheWitchCode/Potions/PotionBeltShrinkPatch.cs(+.uid)`, art ×4; edited `TheWitch/localization/eng/{cards,powers}.json`, `CLAUDE.md`; regenerated docs
- **Verified:** dotnet build 0/0

### 124. New card: Crystal Bottle
- **Done:** 2026-07-09
- **Changed:** New Uncommon Power, 2E (upgrade: 1E): "The next Potion you use becomes a Buff. Use its effect again at the start of each turn." Two-power design: `CrystalBottlePower` (armed counter; `AfterPotionUsed` captures the consumed potion instance) hands off to `BottledMagicPower` (per-turn replay buff; Amount = bottled count). Replay uses the Oxidizers reflection pattern (protected `PotionModel.OnUse` invoked directly — `OnUseWrapper` would throw on the removed potion + re-fire hooks). Targeting per spec: single-enemy potions → random living enemy re-rolled each turn (`Rng.CombatTargets`); self/player → owner; AllEnemies/None → null (potion fans out itself). Selection-prompting potions get the real `AfterPlayerTurnStart` choiceContext. The Cauldron excluded without consuming a stack (stateful instance; mirrors NextPotionCopiedPower guard). Caveats flagged: bottled list is plain power state — not save/reload persistent, not MP-synced. Placeholder art (no `crystal_bottle.png` yet). Tagged mechanics=Potions, role=Payoff.
- **Files:** `TheWitchCode/Cards/CrystalBottle.cs`, `TheWitchCode/Powers/CrystalBottlePower.cs`, `TheWitchCode/Powers/BottledMagicPower.cs`, `TheWitch/localization/eng/cards.json`, `TheWitch/localization/eng/powers.json`, regenerated docs
- **Verified:** dotnet build 0 errors / 0 warnings. **Needs in-game playtest** (hook + reflection behavior compile-checked only): arm → drink potion → buff replays at turn start; random-enemy targeting; multi-stack; power icons are placeholder-missing too (`powers/crystal_bottle_power.png`, `powers/bottled_magic_power.png`).

### 123. Rename Plunder → Pick Clean
- **Done:** 2026-07-09
- **Changed:** Full rename ("Plunder" too piratey; user picked "Pick Clean" — Scavenge rejected, Defect clash). Class/file `Plunder` → `PickClean` (+ paired `.cs.uid`), loc keys `THEWITCH-PLUNDER.*` → `THEWITCH-PICK_CLEAN.*`, art `plunder.png` → `pick_clean.png` (both sizes, stale `.import` deleted), doc comment in `Familiars.cs`. Mechanics untouched. Regen ran (docs + art tracker updated). **Follow-up: run Godot "Import assets" task** for renamed PNGs. Note: rename reset the card's TESTED flag in cards.json (key-based preservation).
- **Files:** `TheWitchCode/Cards/PickClean.cs`, `TheWitchCode/Powers/Familiars.cs`, `TheWitch/localization/eng/cards.json`, `TheWitch/images/card_portraits/{,big/}pick_clean.png`, regenerated `Docs/card-data/cards.json` + `pages/art-tracker.html`
- **Verified:** dotnet build 0/0

<!-- Append completed items above this line. Template:

### <title>
- **Done:** <date>
- **Changed:** <one line>
- **Files:** <list>
- **Verified:** dotnet build OK
-->
