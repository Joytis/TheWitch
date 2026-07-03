# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

### 108. Card Redesign: Cackle — pour potions into the Cauldron
- **Done:** 2026-07-02
- **Changed:** Cackle (kept `2e, Skill, Rare, Exhaust`, upgrade −1e) now: **discard every belt potion except The Cauldron** (creating one if absent), then pour the discarded count in. **The Cauldron rewritten as an accumulator** (old AoE dmg/Block/Brambles/Weak effect gone): per poured potion **+2 Strength, +3 HP heal** (cumulative across casts); one Cackle cast pouring **2+ unlocks "Gain 2 Energy"**, **3+ "Remove one debuff"**, **4+ "Gain 1 Intangible"** (thresholds unlock, don't stack — note only marked Str/heal cumulative). On use: apply Strength, heal, energy, remove first debuff, Intangible. Empty Cauldron does nothing. Loc rewritten (card + potion; potion text shows live var amounts).
- **⚠️ Known limitation:** potions serialize as **id + slot only** (`SerializablePotion`) — poured state lives in the mutable instance's DynamicVars and **does not survive save/quit/resume** (Cauldron reverts to empty). Same class of caveat as `FamiliarPower.GrantsUpgradedCards`. Flagged rather than blocked; needs a custom save patch if persistence matters.
- **Files:** [Cackle.cs](TheWickenCode/Cards/Cackle.cs), [TheCauldron.cs](TheWickenCode/Potions/TheCauldron.cs); `cards.json`, `potions.json`; [PotionTraits.cs](TheWickenCode/Potions/Brewing/PotionTraits.cs) comment; regen (TESTED cleared).
- **Verified:** build 0/0, regen `--check` clean. ⚠️ Playtest: belt-text refresh after pouring (BaseValue mutation display), threshold unlocks, discard-then-procure with a full belt.

### 107. New Card: Bonfire — 2e Rare Power (Brambles pay for cards)
- **Done:** 2026-07-02
- **Changed:** New Power card + `BonfirePower` (Buff/**Counter — Amount is the Bramble PRICE**, 5 base / 4 upgraded via `PowerVar` upgrade −1): **when possible, spend `Amount` Brambles to play cards instead of Energy.** No cost-substitution hook exists in the game, so it's built from two hooks: `TryModifyEnergyCostInCombat` zeroes a card's cost while (cost > 0 ∧ Brambles ≥ price), remembering the card in a transient `_substituted` set; `BeforeCardPlayed` consumes that entry and burns `price` Brambles via `PowerCmd.ModifyAmount` on the BramblesPower. Cards already at 0 cost (natively or via earlier hooks) burn nothing.
- **Design calls:** Re-playing Bonfire never stacks the price up — card `OnPlay` applies fresh or lowers the existing Amount to the cheaper price, never adds. Substitution is mandatory while affordable (per the note's "when possible"). `_substituted` is transient combat-local state (not serialized) — MP/save-mid-play edge flagged.
- **Files:** [Bonfire.cs](TheWickenCode/Cards/Bonfire.cs), [BonfirePower.cs](TheWickenCode/Powers/BonfirePower.cs) (new); `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0. ⚠️ No art (`bonfire.png`) — placeholder active. **Playtest hard**: cost display turns 0 at ≥5 Brambles, Brambles burn exactly once per played card, X-cost cards, auto-play interactions.

### 106. New Card: Repurpose — 1e Rare Power
- **Done:** 2026-07-02
- **Changed:** New Power card + `RepurposePower` (Buff/Single): **at the start of your turn, draw a random card from your Discard Pile, then discard a card.** Pull = `rng.NextItem(discard)` + plain `CardPileCmd.Add` (moving an existing card, not generating); discard = base-game DaggerThrow pattern (`CardSelectCmd.FromHandForDiscard` → `CardCmd.Discard`). Skips entirely when the discard pile is empty (no free filter-discard). Note gave no upgrade → chose **−1 Energy**.
- **Files:** [Repurpose.cs](TheWickenCode/Cards/Repurpose.cs), [RepurposePower.cs](TheWickenCode/Powers/RepurposePower.cs) (new); `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0. ⚠️ No art (`repurpose.png`) — placeholder active. Playtest: turn-start pull + discard prompt ordering vs familiar tokens.

### 105. New Card: Salt and Ash — 1e Uncommon Skill
- **Done:** 2026-07-02
- **Changed:** New card: **Gain 8 Block. Gain 6 more Block if you have a debuff** (`Owner.Creature.Powers.Any(p => p.Type == PowerType.Debuff)`). Note gave no upgrade → chose **+3 base / +2 bonus** (8→11, 6→8). Second `BlockVar("BonusBlock", …)` for the conditional line.
- **Files:** [SaltAndAsh.cs](TheWickenCode/Cards/SaltAndAsh.cs) (new); `cards.json`; regen.
- **Verified:** build 0/0. ⚠️ No art yet (`salt_and_ash.png` small+big) — placeholder fallback active; run `py tools/gen-image-sizes.py` + Godot import once art exists.

### 104. Card Change: Bottle Barrage — live hit-count display
- **Done:** 2026-07-02
- **Changed:** Card face now shows how many times it will hit (= potions created this combat), using the base-game **Barrage** pattern verbatim: `CalculationBaseVar(0) + CalculationExtraVar(1) + CalculatedVar("CalculatedHits").WithMultiplier(→ PotionsCreatedTracker.CountFor)`. `OnPlay` reads the hit count from the CalculatedVar (single source of truth). Loc adds Barrage's `{InCombat:\n(Hits {CalculatedHits:diff()} …)|}` line — count only renders in combat.
- **Files:** [BottleBarrage.cs](TheWickenCode/Cards/BottleBarrage.cs); `cards.json`; regen (TESTED cleared).
- **Verified:** build 0/0. ⚠️ Playtest: count updates as potions are created.

### 103. Familiar tokens go to the front of the hand
- **Done:** 2026-07-02
- **Changed:** `FamiliarPower.AfterPlayerTurnStart` now passes `CardPilePosition.Top` to `CardPileCmd.AddGeneratedCardToCombat` (the game's own optional positioning param; default was `Bottom`). Generated-card path (history + `AfterCardGeneratedForCombat`) unchanged.
- **Files:** [FamiliarPower.cs](TheWickenCode/Powers/FamiliarPower.cs).
- **Verified:** build 0/0. ⚠️ Playtest: tokens appear at the left/front of the hand at turn start.

### 102. Bug Fix: Roomy Satchel — potions stranded when the belt shrinks (confirmed)
- **Done:** 2026-07-02
- **Root cause (confirmed by reading game source):** when max potion count shrinks at combat end, `Player.SetMaxPotionCountInternal` migrates potions from doomed slots into earlier empty slots **silently — no event fires**. The UI node for a migrated potion therefore still lives in a doomed holder, and `PotionBeltShrinkPatch` freed that holder — freeing the potion's node with it. Result: a potion that exists in the model but is invisible and unusable on screen (reload restores it). This matches both reported symptoms ("visuals strange", "lose access"). The *slot count* itself reverts correctly (`RoomySatchelPower.AfterCombatEnd` → `LoseMaxPotionCount(Amount)`); no permanent slot loss found in the model layer.
- **Fix:** the patch now re-seats stranded potions before/after trimming — for every kept holder whose displayed potion doesn't match `player.PotionSlots[slot]`, it creates a fresh `NPotion` node in the right holder.
- **Files:** [PotionBeltShrinkPatch.cs](TheWickenCode/Potions/PotionBeltShrinkPatch.cs).
- **Verified:** build 0/0. ⚠️ Playtest required (Harmony/UI): fill bonus slots, end combat, confirm surviving potions stay visible/usable and belt shrinks cleanly.

### 101. Brew-card trio rename + rework (Stony Brew / Wicked Brew / Herbal Brew)
- **Done:** 2026-07-02
- **Changed:** Full renames: **Skin of Stone (`StoneSkin`) → Stony Brew**, **Something Wicked (`SomethingWicked`) → Wicked Brew**, **Toil and Trouble (`ToilAndTrouble`) → Herbal Brew**. All three now identical shape: `1e, Common, Skill, Self, Exhaust` — create a **Common** potion of their orientation (Defensive/Offensive/Utility); **upgrade −1 Energy** (replaces the old Common→Uncommon rarity upgrade). All three keep/gain the `NextPotionRarePower`/`NextPotionUpgradedPower` rarity hooks (Stony Brew previously lacked them).
- **Design calls:** The existing **Wicked Brew potion → Noxious Brew** (`NoxiousBrew`; name freed for the card — loc keys share one namespace so they'd collide). Updated all refs: Favorite Spellbook, Bottomless Cauldron (card + power), `PotionMerge` (`SingleToNoxiousBrew`), `PotionTraits.Manual`. Art renamed (`stony_brew`/`wicked_brew`/`herbal_brew` small+big; stale `.import` deleted). Dropped the `{IfUpgraded:show:Potion+|Potion}` marker from card text — upgrade no longer changes the potion.
- **Files:** [StonyBrew.cs](TheWickenCode/Cards/StonyBrew.cs), [WickedBrew.cs](TheWickenCode/Cards/WickedBrew.cs), [HerbalBrew.cs](TheWickenCode/Cards/HerbalBrew.cs), [NoxiousBrew.cs](TheWickenCode/Potions/NoxiousBrew.cs), [FavoriteSpellbook.cs](TheWickenCode/Cards/FavoriteSpellbook.cs), [BottomlessCauldron.cs](TheWickenCode/Cards/BottomlessCauldron.cs), [BottomlessCauldronPower.cs](TheWickenCode/Powers/BottomlessCauldronPower.cs), [PotionMerge.cs](TheWickenCode/Potions/Brewing/PotionMerge.cs), [PotionTraits.cs](TheWickenCode/Potions/Brewing/PotionTraits.cs); `cards.json`, `potions.json`, `powers.json`; art renames.
- **Verified:** build 0/0, regen OK. ⚠️ **Run the "Godot: Import assets" task** (renamed art needs re-import). Playtest the three brews + Bottomless Cauldron loop guard.

### 100. Bug Fix: Brew could pick the same potion twice
- **Done:** 2026-07-02
- **Changed:** `PotionUpgrade.UpgradeRandomPotions` re-rolled the live belt each iteration — it could hit the same potion twice and re-upgrade its own freshly created output. Now: snapshot the belt once, `UnstableShuffle` (game's own shuffle extension), upgrade the first N distinct potions. A potion with no higher tier is skipped (`continue`), not a dead stop.
- **Files:** [PotionUpgrade.cs](TheWickenCode/Potions/Brewing/PotionUpgrade.cs).
- **Verified:** build 0/0. ⚠️ Playtest: Brew+ (2 upgrades) with 2+ potions upgrades two different ones.

### 99. Card Change: Extract Essence — random potion by encounter tier
- **Done:** 2026-07-02
- **Changed:** Dropped the enemy-property-themed pick (`EnemyEssence.RollThematicPotion`); on unblocked damage the card now grants `PotionCatalog.Random(Query(rarity:))` where rarity = `Owner.RunState.CurrentMapPoint?.PointType` → Boss=Rare, Elite=Uncommon, else Common. Deleted the now-orphaned `Brewing/EnemyEssence.cs` (only this card used it). Loc unchanged ("extract a Potion" still accurate).
- **Files:** [ExtractEssence.cs](TheWickenCode/Cards/ExtractEssence.cs); deleted `EnemyEssence.cs`.
- **Verified:** build 0/0. ⚠️ Playtest: elite/boss fights yield Uncommon/Rare potions.

### 98. Card Change: Embrace the Wilds — Skill → Power
- **Done:** 2026-07-02
- **Changed:** `CardType.Skill` → `CardType.Power`; dropped the `Exhaust` keyword (Powers leave the deck inherently). Effect unchanged: apply `EmbraceTheWildsPower` (draw −3/turn) + summon 5 random familiars; upgrade still +1 familiar.
- **Files:** [EmbraceTheWilds.cs](TheWickenCode/Cards/EmbraceTheWilds.cs); regen (TESTED cleared).
- **Verified:** build 0/0.

### 97. Card Change: Plague — apply 1 Hex to ALL enemies, upgrade −1e
- **Done:** 2026-07-02
- **Changed:** Rat token reworked from `1e Attack AllEnemies` (4 dmg AoE + Strength-down) to **`1e, Skill, AllEnemies` — Apply 1 Hex to ALL enemies. Upgrade: −1 Energy** (was +2 dmg). Uses `PowerCmd.Apply<HexPower>` on `CombatState.HittableEnemies`. Original staging note said single Hex; a follow-up staging note (same day) revised to ALL enemies — implemented the revision. Loc rewritten.
- **Files:** [Plague.cs](TheWickenCode/Cards/Familiar/Plague.cs); `cards.json`; regen (TESTED cleared).
- **Verified:** build 0/0.

### 96. Card Change: Nettles — 8 AoE +1 per Bramble, upgrade +3
- **Done:** 2026-07-02
- **Changed:** Already on the `CalculatedDamageVar` (Soul Storm) shape; `ExtraDamageVar` 2→**1** per Bramble, upgrade changed from +1 extra-damage to **+3 base** (`DynamicVars.CalculationBase.UpgradeValueBy(3m)`). Cost/type/rarity untouched (2e Uncommon AoE Attack). Loc unchanged (diff() renders the base bump).
- **Files:** [Nettles.cs](TheWickenCode/Cards/Nettles.cs); regen (TESTED cleared).
- **Verified:** build 0/0.

### 95. Card Change: Stuck in the Bush — 0e Rare: 8 Brambles + 2 Vulnerable
- **Done:** 2026-07-02
- **Changed:** Reworked from `2e Uncommon` (20 Block + 2 self-Vulnerable) to **`0e, Skill, Rare, Self` — Gain 8 Brambles. Gain 2 Vulnerable.** Note gave no upgrade → chose **+3 Brambles** (8→11), Vulnerable fixed at 2. Loc rewritten.
- **Files:** [StuckInTheBush.cs](TheWickenCode/Cards/StuckInTheBush.cs); `cards.json`; regen (TESTED cleared).
- **Verified:** build 0/0.

### 94. Card Change: Ambush! — 2e, 15 damage
- **Done:** 2026-07-02
- **Changed:** Cost 3→**2**, AoE damage 20→**15**. Kept Exhaust, random-familiar summon, and the +5 damage upgrade.
- **Files:** [Ambush.cs](TheWickenCode/Cards/Ambush.cs); regen (TESTED cleared).
- **Verified:** build 0/0.

### 93. Cut: Concoct (+ orphaned Villainous Brew potion)
- **Done:** 2026-07-02
- **Changed:** Deleted `Cards/Concoct.cs` (+`.uid`), loc keys, art. Cutting it orphaned the **Villainous Brew** potion (Token rarity; only Concoct+ granted it) — user approved cutting it too: deleted `Potions/VillainousBrew.cs` (+`.uid`), `potions.json` keys, `PotionTraits.Manual` entry. **Wicked Brew potion kept** (still granted by Favorite Spellbook, Bottomless Cauldron, PotionMerge); its comment ref to Concoct updated to Favorite Spellbook.
- **Files:** deleted `Concoct.cs`, `VillainousBrew.cs`, art; `cards.json`, `potions.json`, [PotionTraits.cs](TheWickenCode/Potions/Brewing/PotionTraits.cs), [WickedBrew.cs](TheWickenCode/Potions/WickedBrew.cs) (comment).
- **Verified:** build 0/0, regen OK (removed: Concoct).

### 92. Cut: Bottled Rot (potion)
- **Done:** 2026-07-02
- **Changed:** Deleted `Potions/BottledRot.cs` (+`.uid`), `potions.json` keys, `PotionTraits.Manual` entry, art (`images/potions/bottled_rot.png` + `.import`). Reason: Poison-ALL overlap with Silent. No card/relic granted it (grep clean).
- **Files:** deleted `BottledRot.cs`, art; `potions.json`, [PotionTraits.cs](TheWickenCode/Potions/Brewing/PotionTraits.cs).
- **Verified:** build 0/0.

### 91. Cut: Infernal Chant
- **Done:** 2026-07-02
- **Changed:** Deleted `Cards/InternalChant.cs` (+`.uid`) — file/id spelled "Internal", note said "infernal", same card (only chant card in pool). Removed `THEWICKEN-INTERNAL_CHANT.*` loc keys + art (`internal_chant.png` small/big + `.import`). No dangling refs.
- **Files:** deleted `InternalChant.cs`, art; `cards.json`.
- **Verified:** build 0/0, regen OK (removed: Internal Chant).

### 90. Cut: Rip Veins
- **Done:** 2026-07-02
- **Changed:** Deleted `Cards/RipVeins.cs` (+`.uid`) — identical to a Necrobinder card. Removed `THEWICKEN-RIP_VEINS.*` loc keys + art (`rip_veins.png` small/big + `.import`). No dangling refs.
- **Files:** deleted `RipVeins.cs`, art; `cards.json`.
- **Verified:** build 0/0, regen OK (removed: Rip Veins).

### 89. Card Change: Hidden in Smoke — Intangible skill → turn-start smoke Power
- **Done:** 2026-06-30
- **Changed:** Reworked from `2e Rare Skill` (gain Intangible, Exhaust) to `2e, Rare, Power, Self` — **At the start of your turn, create a Vial of Smoke potion.** New `Powers/HiddenInSmokePower.cs` (Buff/Single, `AfterPlayerTurnStart` → `PotionCmd.TryToProcure<VialOfSmoke>(player)`). Card now applies the power; hover tips show the power + the `VialOfSmoke` potion. Loc rewritten (cards.json + new `THEWICKEN-HIDDEN_IN_SMOKE_POWER.*`).
- **Design calls:** "Bottle of Smoke" → the mod's existing card-only `VialOfSmoke` (Token, defensive Block potion; same one Light the Candle brews). Note gave no upgrade → kept the prior **−1 Energy** upgrade. Dropped Intangible + Exhaust entirely. `IntangiblePower` is base-game — no orphan.
- **Files:** [HiddenInSmoke.cs](TheWickenCode/Cards/HiddenInSmoke.cs), `Powers/HiddenInSmokePower.cs` (new); `cards.json`, `powers.json`; regen (TESTED cleared).
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: a Vial of Smoke appears in the belt each turn start (respects potion-slot limits via TryToProcure).

### 88. Card Change: Ritual Sacrifice — add card draw
- **Done:** 2026-06-30
- **Changed:** Kept `1e, Uncommon, Skill, Self` + sacrifice-a-Familiar → **Gain 25 Block. Draw 3 cards.** Added `CardsVar(3)` and a `CardPileCmd.Draw(ctx, Cards, Owner)` inside the existing `if (sacrificed)` gate (draw only fires on a successful sacrifice, matching the block). Upgrade now **+5 Block, +2 cards** (was +5 block only). Loc adds the draw line with `{Cards:plural:card|cards}`.
- **Files:** [RitualSacrifice.cs](TheWickenCode/Cards/RitualSacrifice.cs); `cards.json`; regen (TESTED cleared).
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: no sacrifice available → no block, no draw (both gated).

### 87. Card Change: Rotting Roots — skill → turn-start rot Power
- **Done:** 2026-06-30
- **Changed:** Reworked from `1e Uncommon Skill` (self-Weak + enemy Strength-down) to `1e, Uncommon, Power, Self` — **At the start of your turn, ALL enemies lose 5 HP and you gain 1 HP.** New `Powers/RottingRootsPower.cs` (Counter, `AfterPlayerTurnStart`): unblockable HP-loss to `combat.HittableEnemies` (`ValueProp.Unblockable | Unpowered`) then `CreatureCmd.Heal(Owner, 1)`. Card applies the power with Amount = 5. Loc rewritten (cards.json + new `THEWICKEN-ROTTING_ROOTS_POWER.*`).
- **Design calls:** Note gave **no upgrade** → chose **+2 enemy HP-loss** (5→7); heal fixed at 1 (constant, not a var). ⚠️ The old `RottingRootsStrengthDownPower.cs` is now **orphaned dead code** (only Rotting Roots referenced it) — left in place per surgical-change rule; flag for deletion if unwanted.
- **Files:** [RottingRoots.cs](TheWickenCode/Cards/RottingRoots.cs), `Powers/RottingRootsPower.cs` (new); `cards.json`, `powers.json`; regen (TESTED cleared).
- **Verified:** build 0/0, regen OK. ⚠️ Playtest turn-start damage-all + self-heal; combat-hook behavior compile-checked only.

### 86. Card Change: Soul Knot — damage-reflect → debuff-spread
- **Done:** 2026-06-30
- **Changed:** Reworked `SoulKnotPower` from "attacks on you splash all enemies" (`AfterDamageReceived`) to **"debuffs you apply to an enemy also apply to ALL enemies"** (`AfterPowerAmountChanged`, base-game **OutbreakPower** pattern). Card cost 3→2 (`2e, Rare, Power, Self`); upgrade kept as −1 Energy per the (revised) staging note. Loc updated in cards.json + powers.json.
- **Design calls:** Hook fires on the player's powers for every power change → filtered to `applier == Owner && amount > 0 && power.Type == Debuff && landed enemy (Side != player)`. Copy uses `ModelDb.DebugPower(power.GetType()).ToMutable()` + non-generic `PowerCmd.Apply(power,…)` so any debuff type spreads generically. Two guards: a `_spreading` bool (kills the infinite re-trigger loop) and an "enemy doesn't already have this power" filter (stops already-AoE cards from compounding stacks). `silent: true` on the copies to avoid pause spam.
- **Files:** [SoulKnot.cs](TheWickenCode/Cards/SoulKnot.cs), [SoulKnotPower.cs](TheWickenCode/Powers/SoulKnotPower.cs); `cards.json`, `powers.json`; regen (TESTED cleared).
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: single-target debuff (Hex/Weak/Vuln) spreads to all enemies once; no infinite loop; already-AoE debuff cards don't double up (the "lacking it" filter). Harmony/combat-hook behavior is compile-checked only.

### 85. Card Rename + Change: Cursed Bottles → Wax and Wane
- **Done:** 2026-06-30
- **Changed:** Full rename `CursedBottles` → `WaxAndWane` (class + file + `.cs.uid` removed; loc key `THEWICKEN-CURSED_BOTTLES` → `THEWICKEN-WAX_AND_WANE`; art `cursed_bottles.png`/`big` → `wax_and_wane.png`/`big` via `git mv`, stale `.import` deleted). New design: converted multi-hit Attack (3×3 dmg + Hex) → `1e, Uncommon, Skill, AnyEnemy` — **Gain 9 Block. Apply 1 Hex.** Upgrade **+2 Block, +1 Hex.** Vars `BlockVar(9, Move)` + `PowerVar<HexPower>(1)`; `GainsBlock => true` (Bottle Wall pattern).
- **Design calls:** Kept `TargetType.AnyEnemy` — a Skill that gains Block (self) *and* applies Hex needs an enemy target for the debuff; block goes to self via `Owner.Creature`. No dangling refs to the old class (grep clean outside its own file + historical DONE).
- **Files:** `Cards/WaxAndWane.cs` (new), `Cards/CursedBottles.cs`(+`.uid`) removed; `cards.json`; art renamed; regen (+Wax and Wane / −Cursed Bottles).
- **Verified:** build 0/0, regen OK. ⚠️ Art files renamed — user must run **Godot: Import assets** to regenerate `.import`. Playtest block+hex + upgrade.

### 84. New Card: Hemlock — Bramble → Hex payoff Power
- **Done:** 2026-06-30
- **Changed:** New `1e, Power, Rare` card (`Cards/Hemlock.cs`) applying a new `HemlockPower` (`Powers/HemlockPower.cs`, Buff/Single marker). Bramble retaliation now checks the bramble owner for `HemlockPower` and, if present, applies **1 Hex** to the creature it damaged (`BramblesPower.BeforeDamageReceived`, right after the return-damage). Upgrade = **Innate** (`AddKeyword(CardKeyword.Innate)`, Aggression pattern). Loc added: `THEWICKEN-HEMLOCK.*` (cards.json), `THEWICKEN-HEMLOCK_POWER.*` (powers.json).
- **Design calls:** Modeled on base-game Aggression (1e/Power/Rare/Self, apply-power + Innate-on-upgrade). `HexPower` name collides with the game's own `HexPower` — fully-qualified `TheWicken.TheWickenCode.Powers.HexPower` in both Hemlock's hover tips and BramblesPower's apply call (and qualified the game's `BramblesPower` in Hemlock's tip). Effect lives in BramblesPower (single trigger site), power is a passive toggle.
- **Files:** `Cards/Hemlock.cs` (new), `Powers/HemlockPower.cs` (new), [BramblesPower.cs](TheWickenCode/Powers/BramblesPower.cs); `cards.json` + `powers.json`; regen (+added Hemlock).
- **Verified:** build 0/0, regen OK. ⚠️ Placeholder art — needs `images/card_portraits/hemlock.png` (+big) then Godot import. ⚠️ Playtest: bramble retaliation applies 1 Hex when Hemlock active; upgraded copy is Innate.

### 83. Card Change: Hexblast — detonate debuffs
- **Done:** 2026-06-30
- **Changed:** Reworked from "apply Hex, then deal damage × unique-debuff count" to a **detonate**: counts unique debuff types on the target, deals `10 × count` in one hit, then removes every one of those debuff powers (`PowerCmd.Remove`). No longer applies Hex. Vars trimmed to a single `DamageVar(10)`; upgrade still `+3` per-debuff (→13). Chose **unique debuff type** (not per-stack) to match the prior tally; removed hover tip (no longer references Hex).
- **Files:** [Hexblast.cs](TheWickenCode/Cards/Hexblast.cs); `cards.json` (loc + Docs regen).
- **Verified:** build 0/0, regen (TESTED cleared). ⚠️ Playtest: damage = 10 × distinct debuffs, all debuffs cleared after.

### 82. Card Change: Bewitching Grin — Attack → AoE Weak skill
- **Done:** 2026-06-30
- **Changed:** Converted from a single-target Attack (damage + Hex) to `1e, Common, Skill, AllEnemies` — **ALL enemies gain 2 Weak. Exhaust.** Upgrade `+1 Weak` (2→3). Vars now just `PowerVar<WeakPower>(2)`; added `Exhaust` keyword; applies Weak to `CombatState.HittableEnemies` (Plague/Pact of Agony pattern); hover tip swapped Hex→Weak.
- **Files:** [BewitchingGrin.cs](TheWickenCode/Cards/BewitchingGrin.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen (TESTED cleared).

### 81. Card Change: Vexing Thwack — single hit
- **Done:** 2026-06-30
- **Changed:** Dropped the `RepeatVar(2)` multi-hit; now a single-hit Attack — **Deal 8 damage. Apply 3 Hex.** Damage 10→8, base Hex 2→3. Cost/rarity/type/target unchanged (`3e, Common, Attack, AnyEnemy` — note specified no new cost) and upgrade left as-is (`+2 Hex`).
- **Files:** [VexingThwack.cs](TheWickenCode/Cards/VexingThwack.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen (TESTED cleared).

### 80. Card Change: Bind in Blood — pure Hex skill
- **Done:** 2026-06-30
- **Changed:** Reworked from `1e Uncommon Attack` (damage + 2 Wounds + Hex) to `0e, Common, Skill, AnyEnemy` — **Lose 3 HP. Apply 3 Hex.** Upgrade `+2 Hex` (3→5). Vars now `HpLossVar(3)` + `PowerVar<HexPower>(3)`; self HP loss via `CreatureCmd.Damage(Unblockable|Unpowered|Move)` (Pact of Agony pattern), Hex applied to target; hover tips trimmed to Hex only.
- **Files:** [BindInBlood.cs](TheWickenCode/Cards/BindInBlood.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen (TESTED cleared).

### 78. Card Change: Nibble — damage scaling → hit-count scaling
- **Done:** 2026-06-30
- **Changed:** Nibble now deals a flat 1 damage but **hits** `1 + RatCardsPlayedThisCombat` times (was: flat 1 hit with live-scaling per-hit damage via `CalculatedDamageVar`). Swapped to the Brambleburst/Hexblast flat-`DamageVar` + `WithHitCount(count)` shape (hit count computed in `OnPlay`, same as those cards' scaling display). Upgrade changed from "+1 extra damage per rat" to "+1 base damage per hit" (1→2), since rat-count scaling now lives entirely in hit count.
- **Files:** [Nibble.cs](TheWickenCode/Cards/Familiar/Nibble.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED cleared). ⚠️ Playtest: hit count = 1 (self) + prior Rat-card plays this combat; upgrade raises per-hit damage 1→2.

### 77. Card Change: Familiar pet art should reflect summon-card art
- **Done:** 2026-06-30
- **Changed:** Verified only — no code change. All 8 `WickenPet` subclasses (`Monsters/*Pet.cs`) already set `TexturePath` to their own summon card's portrait via `"<name>_familiar.png".CardImagePath()`, and every `FamiliarPower.Pet` maps 1:1 to the matching pet class (Bear→BearPet, Cat→CatPet, Chimera→ChimeraPet, Crow→CrowPet, Owl→OwlPet, Porcupine→PorcupinePet, Rat→RatPet, Wolf→WolfPet). The visible mismatch the user is seeing is that 4 of 9 familiar art files (`chimera_familiar.png`, `crow_familiar.png`, `porcupine_familiar.png`, `rat_familiar.png`) are still the shared default-fallback placeholder (identical file hash to `find_familiar.png`) — a missing-art gap, not a pet/card art linkage bug.
- **Files:** none.
- **Verified:** code inspection only (no build needed — no `.cs` touched). Real art still needed for Chimera/Crow/Porcupine/Rat familiars (card portrait, which the pet will automatically pick up once authored).

### 76. Card Bug: Rotting Roots — broken variable token
- **Done:** 2026-06-30
- **Changed:** `RottingRoots.cs` builds its Weak var as `new PowerVar<WeakPower>(1m)`, which keys itself `"WeakPower"` (per `PowerVar<T>`'s ctor — matches base game's `DynamicVarSet.Weak` accessor reading `_vars["WeakPower"]`), but the localization string referenced `{Weak:diff()}`, a key that doesn't exist — hence the broken display. Fixed the loc token to `{WeakPower:diff()}`.
- **Files:** `cards.json` (`THEWICKEN-ROTTING_ROOTS.description`).
- **Verified:** build 0/0. Text now renders "Gain 1 Weak." correctly.

### 75. Card Change: Grind Down — wording
- **Done:** 2026-06-30
- **Changed:** Description first line reworded from "Exhaust a card to create a Potion." to "Exhaust a card. Turn it into a Potion." per the user's requested phrasing; kept the existing second line (type→effect, rarity→potency). No `.cs`/effect change.
- **Files:** `cards.json` (`THEWICKEN-GRIND_DOWN.description`).
- **Verified:** build 0/0.

### 74. Card Change: Chromatic Claws — redesign
- **Done:** 2026-06-29
- **Changed:** Dropped the belt-count damage scaling (`CalculatedDamageVar`) for flat `DamageVar(8, Move)`. New effect: deal 8, then if the belt has ≥1 potion, discard a random one (`Owner.Potions` + `rng.NextItem` → `PotionCmd.Discard`) and create a random potion (`PotionCatalog.Random(PotionCatalog.Query())` → `TryToProcure`). Guarded so the create only fires when a discard happened (empty belt → just the attack). 1e Common Attack kept. **Design call:** note gave no upgrade → +3 damage (8→11).
- **Files:** [ChromaticClaws.cs](TheWickenCode/Cards/ChromaticClaws.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED cleared). ⚠️ Playtest: empty belt = no create; otherwise swaps one potion; created potion is from the Randomizable pool (any orientation/rarity).

### 73. Card Change: Cursed Bottles — Power → Attack
- **Done:** 2026-06-29
- **Changed:** Redesigned from a Hex-on-potion-use Power to a 2e Uncommon **Attack** (AnyEnemy): deal 3 damage ×3 (`WithHitCount(3)`) + apply 3 Hex (`PowerVar<HexPower>(3)` → `Apply<HexPower>`). Kept the Hex hover tip. Deleted the orphaned `CursedBottlesPower` (.cs/.uid + cards.json/powers.json keys) — only this card used it. **Design call:** note gave no upgrade → +1 damage per hit (3→4).
- **Files:** [CursedBottles.cs](TheWickenCode/Cards/CursedBottles.cs); deleted `CursedBottlesPower.cs`; `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0 (loc analyzer clean = power keys gone), regen OK (TESTED cleared). ⚠️ Playtest: 3×3 damage + 3 Hex stacks; Hex rebounds on the enemy's next attack.

### 72. Card Change: Woe and Whimsy — redesign
- **Done:** 2026-06-29
- **Changed:** `WoeAndWhimsy` redesigned from "add 2 random Familiar cards" to "Gain 5 Vigor. Gain 5 Block." Upgrade → +2 Vigor, +2 Block. `PowerVar<VigorPower>(5)` + `BlockVar(5)` (Patter/InternalChant pattern); added Vigor hover tip; dropped the `FamiliarCardRegistry`/`Models` usings. Loc token `{VigorPower:diff()}` (matches base-game `WeakPower`-style token naming; C# access via `DynamicVars["VigorPower"]`). **Design call:** note didn't mention cost/rarity — kept 0e Common Skill. ⚠️ Flag: 5 Block + 5 Vigor for 0 energy on a Common is a strong stat package; worth a balance look in playtest.
- **Files:** [WoeAndWhimsy.cs](TheWickenCode/Cards/WoeAndWhimsy.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED cleared). ⚠️ Playtest: gains 5 Vigor + 5 Block; upgrade +2/+2; check 0-cost balance.

### 71. Bug fix: Roomy Satchel potion slots don't visually contract
- **Done:** 2026-06-29
- **Changed:** Root cause is base-game UI: `NPotionContainer` is the only `MaxPotionCountChanged` subscriber and its `GrowPotionHolders` handler **only adds** holder nodes (`for i = _holders.Count; i < newMax`) — it never removes them on shrink, because the base game never lowers max potion count mid-run. Roomy Satchel reverts its bonus slots in `AfterCombatEnd`, so the model contracted but the empty holders lingered. Added a Harmony **postfix** on `NPotionContainer.GrowPotionHolders` that, when the new count < current holder count, `QueueFree`s the surplus holders, trims `_holders`, and re-runs `UpdateNavigation` (private members reached via `Traverse`). Mirrors the existing `WickenPetVisualsPatch` patch style; model already discards/migrates potions out of the doomed slots before the event, so trimmed holders are empty.
- **Files:** new [PotionBeltShrinkPatch.cs](TheWickenCode/Potions/PotionBeltShrinkPatch.cs).
- **Verified:** build 0/0. ⚠️ Harmony UI patch — compile-only; **must playtest**: play Roomy Satchel, finish the fight, confirm the belt shrinks back to base count (and an over-cap potion is dropped as before).

### 70. Familiar localized-text + tooltip pass
- **Done:** 2026-06-29
- **Changed:** Reworked every familiar's display. Summon-card text simplified to just "Summon a/an `<Name>` Familiar" (dropped the inline "at start of turn add X" clause). Each `FamiliarPower` description now reads "At the start of your turn, create a random `<Name>` Familiar card" with a count-scaling `smartDescription`. Summon-card `ExtraHoverTips` now lead with the familiar power tip, then list **every** loot-table token: fixed Cat (was Ferocity ×2 → Ferocity + Curiosity), added the missing Owl→Knowledge and Porcupine→Bristle, and added the power tip to Bear/Crow/Rat/Wolf. **Chimera** gets a power-only tip (no per-card tips — pulls the whole pool); per a user decision its power text keeps the **accurate** full behavior ("draw 1 fewer card and create 2 random Familiar cards") rather than the note's simplified line, so the downside + 2-card count stay visible.
- **Files:** all 8 summon cards (`OwlFamiliar`/`CatFamiliar`/`BearFamiliar`/`CrowFamiliar`/`RatFamiliar`/`PorcupineFamiliar`/`WolfFamiliar`/`ChimeraFamiliar`.cs); `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0, regen OK (8 familiar summons TESTED-cleared). ⚠️ Playtest: hover each summon card — power tip + correct token list; Chimera shows power only; live power tooltip scales with stacks.

### 69. Card Change: Plague — redesign
- **Done:** 2026-06-29
- **Changed:** Rat-token `Plague` redesigned from a Skill (draw + self/enemy Strength loss) to an **Attack**: "Deal 4 damage to ALL enemies. ALL enemies lose 1 Strength." Upgrade → +2 damage (was +1 enemy Strength). AoE via `TargetingAllOpponents` + `PowerCmd.Apply<StrengthPower>(-1)` on `HittableEnemies` (Ambush AoE pattern). Kept `IRatCard`/Token; base exhaust still applies. **Design call:** note said "All enemies take 4 damage and lose 1 strength" with no card type — made it an Attack (matches Rats/ClawEyes damage-token precedent); dropped the old draw + self-debuff.
- **Files:** [Plague.cs](TheWickenCode/Cards/Familiar/Plague.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED cleared). ⚠️ Playtest: AoE damage + Strength loss; Nibble's rat-count scaling still sees Plague (`IRatCard` intact).

### 68. Cut: Sloth familiar
- **Done:** 2026-06-29
- **Changed:** Removed the Sloth familiar entirely — summon `SlothFamiliar`, power `SlothFamiliarPower`, pet `SlothPet`, token `Laze`. Deleted all `.cs`(+`.uid`) and art (`sloth_familiar.png`/`laze.png` big+small +`.import`), removed loc keys (cards.json: LAZE, SLOTH_FAMILIAR; powers.json: SLOTH_FAMILIAR_POWER), and dropped "Sloth" from the `FamiliarCardRegistry` doc comment. Registration is reflection-based, so deleting the files deregisters; Chimera/Embrace-the-Wilds pull the live `WickenFamiliarCard` pool, so Laze auto-drops with no hardcoded reference to fix.
- **Files:** deleted `SlothFamiliar.cs`, `SlothFamiliarPower.cs`, `SlothPet.cs`, `Laze.cs` (+uids + art); [FamiliarCardRegistry.cs](TheWickenCode/Cards/Familiar/FamiliarCardRegistry.cs); `cards.json`, `powers.json`; regen (97 cards, −2).
- **Verified:** build 0/0 (loc analyzer clean = no orphan keys), grep clean for Sloth/Laze in code. ⚠️ Playtest: Chimera/Embrace-the-Wilds no longer roll Sloth/Laze.

### 67. Card Change: Dance Around the Cauldron — redesign
- **Done:** 2026-06-29
- **Changed:** `DanceAroundTheCauldronPower` now draws 1 card per Skill played this turn (was: make a Wicked Brew). Switched hook `BeforeCardPlayed`→`AfterCardPlayed` (Draw needs a `PlayerChoiceContext`, which only the After hook provides); preserved the original self-exclusion by capturing the source card in `AfterApplied` and skipping it via `ReferenceEquals`. Card gained `Exhaust` keyword; removed the WickedBrew hover tip + `Potions` import. Upgrade stays −1 energy.
- **Files:** [DanceAroundTheCauldronPower.cs](TheWickenCode/Powers/DanceAroundTheCauldronPower.cs), [DanceAroundTheCauldron.cs](TheWickenCode/Cards/DanceAroundTheCauldron.cs); `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED cleared). ⚠️ Playtest: each non-Dance Skill draws 1; Dance itself doesn't; buff clears at turn end; card exhausts. Note: `sourceCard` is plain power state (not serialized for save/MP), fine for a turn-scoped buff.

### 66. Card Change: Double, Double — redesign
- **Done:** 2026-06-29
- **Changed:** `DoubleDouble` now deals 5 damage twice (was 4×2) and enchants a random **Draw Pile** card with Replay 1 (HiddenGem/ExtractLife pattern: `BaseReplayCount += Replay` + `CardCmd.Preview`). Dropped the `NextPotionDoubledPower` apply + hover tip; upgrade is now +3 damage (was +2). **Design call:** note said "a random card in your deck" → targeted the Draw Pile (worded "Draw Pile" in loc) to differentiate from ExtractLife's hand-enchant. Deleted the now-orphaned `NextPotionDoubledPower` (.cs/.uid + 3 powers.json keys) — only DoubleDouble referenced it.
- **Files:** [DoubleDouble.cs](TheWickenCode/Cards/DoubleDouble.cs); deleted `NextPotionDoubledPower.cs`(+.uid); `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0, regen OK (TESTED auto-cleared). ⚠️ Playtest: Replay lands on a Draw Pile card; double-hit damage.

### 65. Card Change: Claw Eyes
- **Done:** 2026-06-29
- **Changed:** Crow familiar token `ClawEyes` now an **Attack** (was Skill): deal 5 damage + apply 1 Weak (was Weak only). Upgrade → +3 damage (replaces the old +1 Weak upgrade; base Weak stays 1).
- **Files:** [ClawEyes.cs](TheWickenCode/Cards/Familiar/ClawEyes.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: damage + Weak, Crow loot-table roll still grants it.

### 64. New card: Cursed Spellbook
- **Done:** 2026-06-29
- **Changed:** New `CursedSpellbook` (0e Rare Power, Self) — applies `CursedSpellbookPower`: at each turn start you draw 1 fewer card but gain extra Energy (`ModifyHandDraw −1` + `ModifyEnergyGain +Amount`). Amount = the card's `EnergyVar` (1, +1 per upgrade). Upgrade → +1 Energy granted.
- **Decisions:** Power is `StackType.Single` — replaying refreshes rather than compounding the draw penalty; an upgraded copy just raises the Energy (Amount). Draw penalty is a flat −1 (the book's curse), not per-stack. Energy bonus lives in `Amount` (not DynamicVars) — same save/MP caveat as `GrantsUpgradedCards`. Marked `PowerType.Buff` (net engine).
- **Files:** new `Cards/CursedSpellbook.cs`, `Powers/CursedSpellbookPower.cs`; `cards.json`, `powers.json`; regen. Placeholder art `cursed_spellbook.png` (+big) — needs art + Godot import.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: −1 draw + extra energy each turn persists, upgrade raises energy.

### 63. New card: Rip Veins
- **Done:** 2026-06-29
- **Changed:** New `RipVeins` (2e Uncommon Attack, AllEnemies) — deal 15 damage to all enemies (`TargetingAllOpponents`), then apply 2 `VulnerablePower` to **all characters** = every enemy (`CombatState.HittableEnemies`) **and the player** (self-Vulnerable downside). Upgrade → +5 damage.
- **Decisions:** "ALL characters" read as enemies + self (StS "character" = any creature) — the self-Vulnerable is the intended risk. Upgrade = +5 damage (note silent — design call). AoE pattern from Ambush.
- **Files:** new `Cards/RipVeins.cs`; `cards.json`; regen. Placeholder art `rip_veins.png` (+big) — needs art + Godot import.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: AoE damage + Vulnerable on enemies *and* self.

### 62. New card: Grind Down
- **Done:** 2026-06-29
- **Changed:** New `GrindDown` (1e Uncommon Skill, Self, Exhaust) — `CardSelectCmd.FromHand` to pick a hand card, `CardCmd.Exhaust` it, then grant a potion whose **orientation** comes from the card type (Attack→Offensive, Skill→Defensive, Power→Utility, else Utility) and **rarity** from the card rarity (Rare→Rare, Uncommon→Uncommon, else Common), via `PotionCatalog.Random(Query(orientation, rarity))`. Upgrade → −1e.
- **Decisions:** Null-pool fallback drops the rarity filter (orientation-only) so a card with no matching potion still yields something. Status/Curse types default to Utility. Grants via `.ToMutable()` + `PotionCmd.TryToProcure`.
- **Files:** new `Cards/GrindDown.cs`; `cards.json`; regen. Placeholder art `grind_down.png` (+big) — needs art + Godot import.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: hand-select + exhaust, type/rarity → potion mapping, empty-hand cancel.

### 61. Card Rework: Hexblast (supersedes item 49)
- **Done:** 2026-06-29
- **Changed:** Hexblast now 2e (was 1e): apply 3 `HexPower` first, then deal **12 damage per unique debuff** on the target (`Powers.Where(Type==Debuff).Select(GetType).Distinct().Count()`). Hex applied before the count so it always tallies ≥1. Upgrade → +3 per-debuff damage.
- **Decisions:** Per-debuff value shown as flat `DamageVar(12)` with "for each unique debuff" text (Brambleburst pattern) — no live `CalculatedDamageVar`, since the target (and its debuffs) isn't known until played. Counts *unique debuff types*, not stacks.
- **Files:** [Hexblast.cs](TheWickenCode/Cards/Hexblast.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: debuff-count scaling incl. the just-applied Hex.

### 60. New card: Refuse Pile
- **Done:** 2026-06-29
- **Changed:** New `RefusePile` (2e Uncommon Skill, Self) — gain 11 Block, then create `Rats` tokens via `CombatState.CreateCard<Rats>` and `AddGeneratedCardToCombat` ×2 into the draw pile and ×2 into the discard pile. Upgrade → +4 Block.
- **Decisions:** "Add 2 rats to your draw and discard pile" = 2 into each pile (4 total). Generated path (not plain Add) so card-creation payoffs fire. Upgrade = +4 Block (note silent — design call).
- **Files:** new `Cards/RefusePile.cs`; `cards.json`; regen. Placeholder art `refuse_pile.png` (+big) — needs art + Godot import.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: block + 2 Rats in draw/discard each.

### 59. Card Change: Bramble Shield (re-note — no-op)
- **Done:** 2026-06-29
- **Changed:** Nothing. Re-note ("2e, gain 10 block, gain 10 Brambles") already matches the item-58 state; kept the +3/+3 upgrade (re-note silent on it). Verified current code matches, no edit made.
- **Verified:** matches existing; build untouched.

### 58. Card Change: Bramble Shield
- **Done:** 2026-06-29
- **Changed:** New design (2e Uncommon Skill, Self) — Gain 10 Block + gain 10 `BramblesPower`. Upgrade → +3 Block, +3 Brambles. Dropped the old "7 + 2/bramble-created-this-turn" scaling (and `BramblesCreatedThisTurn` usage).
- **Decisions:** Read the note's "Upgrade +3 brambles, +3 damage" as **+3 Brambles / +3 Block** — Brambles already covers the retaliation "damage", so the second clause reads as the card's defensive value (Block). Flag for sign-off if "+3 damage" meant something else.
- **Files:** [BrambleShield.cs](TheWickenCode/Cards/BrambleShield.cs); `cards.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: block + brambles, upgrade values.

### 57. Card Redesign: Vicious Barbs
- **Done:** 2026-06-29
- **Changed:** New design (2e Uncommon Attack, AnyEnemy) — Discard your hand; deal **5 damage and gain 4 Brambles per card discarded** (lump `Damage×count` hit + `BramblesPower×count`). Upgrade → +2 damage/card. Replaces the old "Brambles deal extra retaliation damage" power.
- **Decisions:** **User-resolved** (AskUserQuestion) — per-card payload = 5 damage + 4 Brambles ("block" was a typo, dropped). Per-card values shown on the card face (Brambleburst pattern, "for each card discarded"), total computed in `OnPlay`; empty hand = no-op. **Orphan:** old `ViciousBarbsPower` (brambles-retaliation boost, read by `BramblesPower.BeforeDamageReceived`) now unreferenced — left registered (build green) but dead; flagged for removal if undesired.
- **Files:** [ViciousBarbs.cs](TheWickenCode/Cards/ViciousBarbs.cs); `cards.json`; regen. (`Powers/ViciousBarbsPower.cs` now orphaned.)
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: discard-hand count, damage + brambles scaling, empty-hand no-op.

### 56. Card Redesign: Rotting Roots
- **Done:** 2026-06-29
- **Changed:** New design (1e Uncommon Skill, AnyEnemy, Exhaust) — Gain 1 Weak (self) + target enemy loses 10 Strength **this turn** (reverts at turn end). Upgrade → +3 Strength loss. New `RottingRootsStrengthDownPower : TemporaryStrengthPower, ICustomModel` (the base-game DarkShackles pattern — Title/Description/icon inherited from the base + `OriginModel`, so no mod loc needed; `ICustomModel` added to get the mod ID prefix, silencing analyzer STS003). Replaces the old "gain Brambles on potion use" power identity.
- **Decisions:** Used the canonical `TemporaryStrengthPower` "lose X Strength this turn" primitive (DarkShackles) rather than permanent `-Strength`. Self-Weak is a flat 1 (not upgraded). **Orphan:** the old `RottingRootsPower` (brambles-on-potion-use) is now unreferenced — left registered (its loc keys still present, build green) but dead; flagged for removal if undesired.
- **Files:** new `Powers/RottingRootsStrengthDownPower.cs`; [RottingRoots.cs](TheWickenCode/Cards/RottingRoots.cs); `cards.json`; regen. (`Powers/RottingRootsPower.cs` now orphaned.)
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: strength reverts at turn end, self-Weak, **temp-strength-down icon renders** (modded `TemporaryStrengthPower` subclass — icon path unverified).

### 55. Card Change: Rattling Bottles → Attack
- **Done:** 2026-06-29
- **Changed:** Rattling Bottles is now an **Attack** (was Skill): 2e Rare, `DamageVar(15)` dealt to `AnyEnemy`, then fills every empty potion slot with `PotionShapedRock` (unchanged). Upgrade → +5 damage (was "remove Exhaust"). Keeps Exhaust at both levels.
- **Files:** [RattlingBottles.cs](TheWickenCode/Cards/RattlingBottles.cs); `cards.json` (desc += damage clause); regen.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: damage + rock-fill, Exhaust.

### 54. Card Change: Bear Familiar rarity
- **Done:** 2026-06-29
- **Changed:** Bear Familiar `CardRarity.Rare` → `Uncommon`. Nothing else touched.
- **Files:** [BearFamiliar.cs](TheWickenCode/Cards/BearFamiliar.cs); regen.
- **Verified:** build 0/0, regen OK.

### 53. Card Change: Chimera Familiar tuning
- **Done:** 2026-06-29
- **Changed:** `ChimeraFamiliarPower` `CardsPerStack` 3→2, `DrawReductionPerStack` 2→1. Updated power doc comment + card localization ("draw 1 fewer card… add 2 random Familiar cards").
- **Files:** [ChimeraFamiliarPower.cs](TheWickenCode/Powers/ChimeraFamiliarPower.cs); `cards.json` (CHIMERA_FAMILIAR.description); regen.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: per-turn token count + draw reduction.

### 52. Card Change: Gather Herbs cost
- **Done:** 2026-06-29
- **Changed:** Cost 0 → **1**; upgrade now reduces cost back to **0** (`EnergyCost.UpgradeBy(-1)`, replacing the old "remove Exhaust" upgrade). Effect (copy next potion created) + Exhaust unchanged.
- **Decisions:** Read the note's "Upgrade +1e" as **restoring the free cost** (cost-down upgrade), the only sensible reading once base cost rose to 1. Flagged in the item; no further sign-off needed.
- **Files:** [GatherHerbs.cs](TheWickenCode/Cards/GatherHerbs.cs); regen (no loc text change — cost shows via the card frame).
- **Verified:** build 0/0, regen OK.

### 51. New card: Nibble (Rat familiar token)
- **Done:** 2026-06-29
- **Changed:** New `Nibble` token (0e Attack, Token, `IRatCard`) — deals **1 damage per Rat card played this combat, including itself**, via the Soul Storm `CalculatedDamageVar` pattern (`CalculationBaseVar(1)` = itself + `ExtraDamageVar(1)` × prior-rat count). New `IRatCard` marker on `Rats`/`Plague`/`Nibble`; new `CombatHistoryQueries.RatCardsPlayedThisCombat`. `RatFamiliarPower` converted `FamiliarPower<Plague>` → `LootTableFamiliarPower` rolling **Plague + Nibble** (equal weight); RatFamiliar hover tips now preview both. Upgrade → +1 per-rat damage.
- **Decisions:** **User-resolved** (AskUserQuestion) — Rat familiar spawn pool = Plague + Nibble; Rats stays a Pocket-Rats-only token. Nibble counts **itself** (base 1) so a fresh Nibble with no prior rats still hits for 1, not 0. Live count excludes the in-progress play (`CardPlaysFinished`), so base-1 supplies the "+1 for itself".
- **Files:** new `Cards/Familiar/Nibble.cs`, `Cards/Familiar/IRatCard.cs`; [RatFamiliarPower.cs](TheWickenCode/Powers/RatFamiliarPower.cs), [RatFamiliar.cs](TheWickenCode/Cards/RatFamiliar.cs), `Cards/Familiar/Rats.cs`, `Cards/Familiar/Plague.cs`, `Extensions/CombatHistoryQueries.cs`; `cards.json`; regen. Placeholder art `nibble.png` (+big) — needs art.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: live damage scaling renders, loot-table roll, hover previews. Needs art.

### 50. New card: Feast With Wolves
- **Done:** 2026-06-29
- **Changed:** New `FeastWithWolves` (1e Uncommon Attack, AnyEnemy) — deal 9 damage, then draw one card at a time until an Attack is drawn (loop exits on `Draw == null`, i.e. empty piles / full hand). Upgrade → +3 damage.
- **Decisions:** Upgrade = +3 damage (note silent on upgrades for new cards).
- **Files:** new `Cards/FeastWithWolves.cs`; `cards.json`; regen. Placeholder art `feast_with_wolves.png` (+big) — needs art.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: draw-until-attack loop, no soft-lock on empty deck. Needs art.

### 49. New card: Hexblast
- **Done:** 2026-06-29
- **Changed:** New `Hexblast` (1e Uncommon Attack, AnyEnemy) — deal 12 damage, apply 3 `HexPower`. Hover tip for Hex. Upgrade → +3 damage.
- **Decisions:** Upgrade = +3 damage (note silent).
- **Files:** new `Cards/Hexblast.cs`; `cards.json`; regen. Placeholder art `hexblast.png` (+big) — needs art.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest. Needs art.

### 48. New card: Consume Youth
- **Done:** 2026-06-29
- **Changed:** New `ConsumeYouth` (2e Uncommon Attack, AnyEnemy) — deal 20 damage, **doubled vs a target above half HP** (`CurrentHp*2 > MaxHp`). Upgrade → +6 base damage.
- **Decisions:** Conditional double computed in `OnPlay` (not a live `CalculatedDamageVar`) — the doubling depends on the chosen target's HP, not a board count, so a simple branch is correct and the card face shows the base 20. Upgrade = +6 (note silent).
- **Files:** new `Cards/ConsumeYouth.cs`; `cards.json`; regen. Placeholder art `consume_youth.png` (+big) — needs art.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: double-damage threshold. Needs art.

### 47. Card Redesign: Embrace the Wilds
- **Done:** 2026-06-29
- **Changed:** New design (3e Rare Skill, Exhaust) — apply `EmbraceTheWildsPower` (new combat-scoped Debuff, draws **3 fewer** cards/turn via `ModifyHandDraw`, `Amount` = cards reduced) and **summon 5 random familiars** by applying one stack of a random `FamiliarPower` pulled from `ModelDb.AllPowers.Where(is FamiliarPower)`. Upgrade → +1 familiar (6). Replaces the old "transform hand into random familiar cards".
- **Decisions:** "Random familiars" = every registered `FamiliarPower` (Wolf/Bear/Crow/Cat/Owl/Porcupine/Rat/Sloth/**Chimera** included — accepted that Chimera can compound the draw penalty). Summon applies the power directly (`PotionCmd`-style `.ToMutable()` + non-generic `PowerCmd.Apply`) so pets/turn-start tokens all wire up via `AfterApplied`; stacks if the same familiar rolls twice. Draw penalty modeled as `Amount`-cards (apply 3 stacks) so the smartDescription number matches the reduction.
- **Files:** new `Powers/EmbraceTheWildsPower.cs`; [EmbraceTheWilds.cs](TheWickenCode/Cards/EmbraceTheWilds.cs); `cards.json`, `powers.json`; regen. Placeholder art `embrace_the_wilds_power.png` (power) — needs art.
- **Verified:** build 0/0, regen OK. ⚠️ **Heavy playtest**: persistent −3 draw across turns, 5 random summons + pets, MP power-apply path. Needs power art.

### 46. Card Change: Bottle Wall
- **Done:** 2026-06-29
- **Changed:** New design (2e Uncommon Skill, Self) — Gain 7 Block + create a `Fortifier` potion (`PotionCmd.TryToProcure<Fortifier>`). Upgrade → +1 Block. Dropped the old "8 + 6/potion-used-this-turn" scaling (and the `PotionsUsedThisTurn` query usage).
- **Files:** [BottleWall.cs](TheWickenCode/Cards/BottleWall.cs); `cards.json`; regen. Fortifier hover tip added.
- **Verified:** build 0/0, regen OK. ⚠️ Playtest: block + potion grant.

### 45. New relic: Wormy's Apple
- **Done:** 2026-06-28
- **Changed:** New `WormysApple` (Wicken-unique relic, Uncommon) — on pickup gain 10 Max HP (`CreatureCmd.GainMaxHp`, `MaxHpVar`, per BigMushroom); every combat, `BeforeHandDraw` on turn 1 adds 1 `Wormy` to your hand (per base-game Toolbox). Reuses the `Wormy` status from item 44.
- **Decisions:** "Gain 10 HP" read as **+10 Max HP** (StS relic convention) — flagged for sign-off. Wicken pool via `WickenRelic` `[Pool]`. Null-guarded `PlayerCombatState` (mod has Nullable on).
- **Files:** new `Relics/WormysApple.cs`; `relics.json`. Placeholder art `wormysapple.png` (+ `_outline`/`big`).
- **Verified:** build 0/0. ⚠️ Needs art + playtest (Max-HP on pickup, Wormy every combat) + HP-interpretation sign-off.

### 44. New potion: Wormy Apple (+ Wormy status card)
- **Done:** 2026-06-28
- **Changed:** New `WormyApple` potion (Uncommon, CombatOnly, Self) — heal 15 life, then add 3 `Wormy` to your hand (`CreateCard<Wormy>` ×3 + `AddGeneratedCardsToCombat`). New `Wormy` card (Status, Token rarity, 1e, Self, Retain+Exhaust, `MaxUpgradeLevel 0`) — on play: lose 1 life (`HpLossVar`, Unblockable|Unpowered|Move) + gain 1 Weak (self). `PotionTraits.Manual` += Defensive.
- **Decisions:** Wormy is a *playable* nuisance (1e to clear, Retain keeps it sticky, Exhaust removes once played). Token rarity keeps it out of random rewards while staying registered/generatable. Placed in the character `WickenCardPool` (reachable by `AllCardPools` → no "You monster!"). Weak applies to the player (downside). Potion tagged Defensive (net heal).
- **Files:** new `Potions/WormyApple.cs`, `Cards/Wormy.cs`; `Potions/Brewing/PotionTraits.cs`; `potions.json`, `cards.json`; regen. Placeholder art `wormyapple.png` + `wormy.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs art + playtest (status generation, Retain/Exhaust, self-Weak).

### 43. Removed card: Rootcraft (+ Bursting Roots chain)
- **Done:** 2026-06-28
- **Changed:** Deleted `Rootcraft` card. Its power `BurstingRootsPower` was applied by no other card, so removed it too — plus its loc and the `CardUpgradeTracker` Bursting-Roots branch + `TakePendingBrambles`/`PendingBrambles` (the tracker's Twinroot-relic branch is kept). Removed art (`rootcraft.png` + `big/` + `.import`).
- **Decisions:** Removing the card orphaned the power and its driver code → removed the whole exclusive chain for a clean delete; left `CardUpgradeTracker` in place for Twinroot.
- **Files:** deleted `Cards/Rootcraft.cs` (+uid), `Powers/BurstingRootsPower.cs` (+uid), art; edited `Patches/CardUpgradeTracker.cs`; `cards.json`, `powers.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Twinroot still drives `CardUpgradeTracker` — playtest that relic to confirm untouched.

### 42. New familiar token: Knowledge (Owl table)
- **Done:** 2026-06-28
- **Changed:** New `Knowledge` (Owl familiar token, 1e Skill, Self) — upgrade 1 card in your hand (Upgraded: all upgradable hand cards). In-combat `CardCmd.Upgrade`, so it lasts only the rest of the fight. Mirrors base-game `Armaments` (branches on `IsUpgraded`; uses `CardSelectCmd.FromHandForUpgrade`). Converted `OwlFamiliarPower` from `FamiliarPower<Wisdom>` to `LootTableFamiliarPower` (Wisdom + Knowledge).
- **Decisions:** "Rest of combat" is automatic — combat upgrades don't persist to the master deck. Upgraded form (all-hand) = the auto-upgraded token when the Owl summon is upgraded.
- **Files:** new `Cards/Familiar/Knowledge.cs`; `Powers/OwlFamiliarPower.cs`; `cards.json`; regen. Placeholder art `knowledge.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs art + playtest (Owl now rolls Wisdom OR Knowledge).

### 41. New familiar token: Curiosity (Cat table)
- **Done:** 2026-06-28
- **Changed:** New `Curiosity` (Cat familiar token, 0e Skill, Self) — draw 2 cards, then put 1 card from your hand on top of your draw pile. Upgraded: draw 3 (user-chosen). Mirrors base-game `ThinkingAhead` (`CardSelectCmd.FromHand` → `CardPileCmd.Add(..., PileType.Draw, CardPilePosition.Top)`). Converted `CatFamiliarPower` (`FamiliarPower<Ferocity>`) to `LootTableFamiliarPower` (Ferocity + Curiosity).
- **Decisions:** Upgrade = draw 3 (put 1 back), per user.
- **Files:** new `Cards/Familiar/Curiosity.cs`; `Powers/CatFamiliarPower.cs`; `cards.json`; regen. Placeholder art `curiosity.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs art + playtest.

### 40. New familiar token: Bristle (Porcupine table)
- **Done:** 2026-06-28
- **Changed:** New `Bristle` (Porcupine familiar token, 0e Skill, Self) — gain 8 Brambles (Upgraded: 10). Converted `PorcupineFamiliarPower` (`FamiliarPower<Quills>`) to `LootTableFamiliarPower` (Quills + Bristle).
- **Files:** new `Cards/Familiar/Bristle.cs`; `Powers/PorcupineFamiliarPower.cs`; `cards.json`; regen. Placeholder art `bristle.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs art + playtest.

### 39. Card rework: Quills
- **Done:** 2026-06-28
- **Changed:** Quills (Porcupine token) → 1e (was 0), Attack: deal 3 damage 4 times (`DamageVar` + `RepeatVar(4)` + `WithHitCount`). Upgraded: 4×4. Dropped the old Brambles gain + its hover tip/usings.
- **Files:** `Cards/Familiar/Quills.cs`; `cards.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest.

### 38. Card rework: Pact of Agony
- **Done:** 2026-06-28
- **Changed:** Pact of Agony → Skill, Common (was Uncommon), 1e, Exhaust: lose 3 life (`HpLossVar`, Unblockable|Unpowered|Move), add 2 `Wound` to discard (per `FightThrough`), ALL enemies gain 3 Weak (`WeakPower` → `HittableEnemies`). Upgrade: +2 Weak (→5). Dropped the old Vulnerable-self / Strength-drain design + its vars.
- **Decisions:** User-finalized note + chose Weak base 3 / upgrade +2. Kept `TargetType.Self` (AoE Weak applied in code). Wound → discard (base-game default).
- **Files:** `Cards/PactOfAgony.cs`; `cards.json`; regen.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest.

### 37. New potion: Mushroom Extract (+ gibberish card patches)
- **Done:** 2026-06-28
- **Changed:** New `MushroomExtract` potion (Rare, CombatOnly, Self) — discard your hand, draw 6 cards; those 6 (your real cards) are "mushroomed" for the rest of combat: `SetToFreeThisCombat()` (cost 0) + random-letter gibberish name/description + `mystery.png` art. New `Patches/MushroomedCards.cs`: a per-instance registry (`ConditionalWeakTable<CardModel, Gibberish>`, generated once, auto-GC'd at combat end) + 3 Harmony postfixes — `CardModel.get_Title`, `GetDescriptionForPile(PileType, Creature)`, `get_Portrait` — that substitute gibberish/mystery art for tagged instances only. `PotionTraits.Manual` += Utility.
- **Decisions:** User-resolved — cost-0 = rest of combat; gibberish sticks to the real cards (combat-scoped); gibberish = procedural random letters/syllables (stable per card); art → `mystery.png`. Cost handled via `SetToFreeThisCombat` (no extra power). Cosmetic override done at the model layer (non-virtual `get_Portrait`/`GetDescriptionForPile` catch all card types; `get_Title` is virtual so a rare base-game card that overrides Title — e.g. Wither — would show its real title; acceptable edge).
- **Files:** new `Potions/MushroomExtract.cs`, `Patches/MushroomedCards.cs`; `Potions/Brewing/PotionTraits.cs`; `potions.json`. Needs art: `mystery.png` (card portrait, falls back to `card.png`) + potion icon `mushroomextract.png`.
- **Verified:** build 0/0. ⚠️ **Cannot runtime-verify** — Harmony UI patches (title/desc/portrait), draw/discard, and cost-0 are compile-only. **Needs in-game playtest** (gibberish renders + layout, art swap, cost-0 persists for combat, no leak to non-mushroomed cards) + art (`mystery.png`, `mushroomextract.png`).

### 36. Card rework: Creeping Vines
- **Done:** 2026-06-28
- **Changed:** Creeping Vines now single-player self-target: spend X energy, gain 7 Brambles per hit, X times. Dropped the `MultiplayerOnly` constraint + random-ally flinging; loop now applies to `Owner.Creature`. Base 5→7, upgrade still +2 (→9). Keeps `HasEnergyCostX`. Loc "to a random ally" → "[b]X[/b] times" (self).
- **Decisions:** Per user — retarget to self, single-player (was MP co-op). Upgrade +2 per note.
- **Files:** `Cards/CreepingVines.cs`; `cards.json`; regen `cards.json`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest (X-cost loop).

### 35. Card rework: Nettles
- **Done:** 2026-06-28
- **Changed:** Nettles → 2e (was 1), Uncommon (was Common), Attack/AllEnemies. Deal 8 + 2 per Bramble to all enemies, now via live `CalculatedDamageVar` (Rend shape: `CalculationBase 8` + `ExtraDamage 2` × `Owner.Creature.GetPowerAmount<BramblesPower>()`) so the Bramble-scaled total renders on the card face instead of being computed only at play. Upgrade: per-Bramble 2→3 (`ExtraDamage +1`).
- **Decisions:** User chose "+1 per bramble" upgrade. Converted the old static-display + play-time-bonus to the live CalculatedDamage pattern per CLAUDE.md (combat-scaled numbers must render live).
- **Files:** `Cards/Nettles.cs`; `cards.json`; regen `cards.json`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest (live Bramble scaling on AoE).

### 34. New card: Internal Chant
- **Done:** 2026-06-28
- **Changed:** New `InternalChant` (Skill, Common, 1, Self) — gain 8 Block, then gain Vigor equal to the total debuffs across **all creatures** (player + every enemy). Debuff count uses the base-game `Rend` filter (`PowerType.Debuff`, excluding `ITemporaryPower`); Vigor applied via `PowerCmd.Apply<VigorPower>` only when count > 0. Vigor hover tip.
- **Decisions:** Raw note "gain 1 vigorous for All debuffs" → user clarified "Vigor per debuff on ALL characters" → counts `CombatState.Creatures`. Note omitted cost/type/rarity → 1e Common Skill. Name set by user (was placeholder "Spite"). No upgrade specified → none added (default MaxUpgradeLevel still applies; no `OnUpgrade`).
- **Files:** new `Cards/InternalChant.cs`; `cards.json`; regen `cards.json`. Placeholder art `internalchant.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest + art (`Images: Generate missing sizes` → `Godot: Import assets`).

### 33. New card: Extract Life
- **Done:** 2026-06-28
- **Changed:** New `ExtractLife` (Attack, Rare, 1, AnyEnemy) — deal 12 damage, then a random card in your hand gains Replay 2 and Exhaust. Replay enchant via base-game `HiddenGem` pattern (`BaseReplayCount += Replay`, `CardCmd.Preview`); Exhaust via public `AddKeyword`. Upgrade: +1 Replay. Replay static hover tip.
- **Decisions:** Picks from Hand (the played card is in Play pile, so excluded); skips Unplayable cards. Upgrade adds Replay (matches HiddenGem) per note "Add one replay".
- **Files:** new `Cards/ExtractLife.cs`; `cards.json`; regen `cards.json`. Placeholder art `extractlife.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest (Replay enchant + Exhaust interaction) + art.

### 32. New card: Chromatic Claws
- **Done:** 2026-06-28
- **Changed:** New `ChromaticClaws` (Attack, Common, 1, AnyEnemy) — deal 8 damage for each potion in your belt. Live-scaling via the Soul Storm `CalculatedDamageVar` shape (`CalculationBase 0` + `ExtraDamage 8` × `Owner.PotionSlots.Count(p => p != null)`), not BaseValue mutation. Upgrade: +4 per potion.
- **Decisions:** Belt count = non-null `PotionSlots` (per `RattlingBottles`). Name set by user (was placeholder "Bottle Toss"). Note omitted upgrade → +4 ExtraDamage.
- **Files:** new `Cards/ChromaticClaws.cs`; `cards.json`; regen `cards.json`. Placeholder art `chromaticclaws.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest + art.

### 31. Card rework: Bind in Blood
- **Done:** 2026-06-28
- **Changed:** Bind in Blood retyped Skill→**Attack**, Common→**Uncommon**, target Self→AnyEnemy. Now: deal 10 damage, add 2 `Wound` to discard (`CombatState.CreateCard<Wound>` + `AddGeneratedCardToCombat` to Discard, per `FightThrough`), apply 3 Hex. Upgrade: +3 damage, +1 Hex. Removed now-orphaned `BindInBloodPower` (+ loc, + uid). Wound + Hex hover tips.
- **Decisions:** Wound destination = discard (base-game Wound convention). `BindInBloodPower` had no other refs → deleted.
- **Files:** `Cards/BindInBlood.cs`; deleted `Powers/BindInBloodPower.cs` (+uid); `cards.json`, `powers.json`; regen `cards.json`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest.

### 30. Card rework: Broom Strike
- **Done:** 2026-06-28
- **Changed:** Broom Strike rider changed from "next Familiar power free" to "next **Power** free **this turn**" (Attack body/damage unchanged). New `NextPowerFreePower` (any `CardType.Power` → cost 0; consume in `BeforeCardPlayed`; self-remove in `AfterSideTurnEnd` so it expires that turn). Retired `NextFamiliarFreePower` (no other refs) and its loc; renamed loc key to `NEXT_POWER_FREE_POWER`. Hover tip updated.
- **Decisions:** "This turn" expiry modeled on base-game `RagePower` (`AfterSideTurnEnd` + `participants.Contains(Owner)` → `PowerCmd.Remove`).
- **Files:** `Cards/BroomStrike.cs`; new `Powers/NextPowerFreePower.cs`; deleted `Powers/NextFamiliarFreePower.cs` (+uid); `cards.json`, `powers.json`; regen `cards.json`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest (energy-discount + turn-end expiry).

### 29. Card rework: Chimera Familiar
- **Done:** 2026-06-28
- **Changed:** `ChimeraFamiliarPower` now draws **2 fewer cards/stack** each turn (`ModifyHandDraw` −2 × Amount) and creates **3 random familiar cards/stack** at turn start (override `AfterPlayerTurnStart`, `FamiliarCardRegistry.CreateRandom(count = 3 × Amount)`). Card itself unchanged (Rare Power 1e, −1 energy upgrade). Loc updated.
- **Decisions:** Both effects scale per stack for consistency with the counter model. Draw reduction via the `ModifyHandDraw` hook.
- **Files:** `Powers/ChimeraFamiliarPower.cs`; `cards.json`; regen `cards.json`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest (draw-hook + turn-start timing; behavior with multiple stacks).

### 28. Removed card: Cursed Bloodline
- **Done:** 2026-06-28
- **Changed:** Deleted `CursedBloodline` card + `CursedBloodlinePower` (only self-referenced; no external refs). Removed `.cs`/`.cs.uid` for both, loc keys from `cards.json` + `powers.json`. No art existed (was placeholder).
- **Files:** deleted `Cards/CursedBloodline.cs` (+uid), `Powers/CursedBloodlinePower.cs` (+uid); `cards.json`, `powers.json`; regen `cards.json`.
- **Verified:** build 0/0 (analyzer would fail on orphan loc — clean), regen OK.

---

### 27. New card: Prices Paid (+ new potion: Slicing Brew)
- **Done:** 2026-06-28
- **Changed:** New `PricesPaid` (Attack, Common, 1) — lose 3 HP (`HpLossVar`, Unblockable|Unpowered|Move), deal 6 damage, `PotionCmd.TryToProcure<SlicingBrew>`. New `SlicingBrew` potion (Token, CombatOnly, AnyEnemy): deals 3 damage 3 times (`DamageVar` + `RepeatVar`, loop of `CreatureCmd.Damage`). Added to `PotionTraits.Manual` = `Damage`.
- **Decisions:** Note omitted energy → 1 (common attack default). Note omitted upgrade → +3 damage. Potion rarity `Token` so it never drops randomly but `TryToProcure` still grants it (mirrors WickedBrew).
- **Files:** new `Cards/PricesPaid.cs`, `Potions/SlicingBrew.cs`; `Potions/Brewing/PotionTraits.cs`; `cards.json`, `potions.json`; `Docs/potion-brewing-system.md`; regen `Docs/card-data/cards.json`. Placeholder art `pricespaid.png`, `slicingbrew.png`.
- **Verified:** build 0/0, regen OK. ⚠️ **Needs in-game playtest** + art (`Images: Generate missing sizes` → `Godot: Import assets`).

### 26. New card: Broken Pact
- **Done:** 2026-06-28
- **Changed:** New `BrokenPact` (Skill, Rare, 2, Self, Exhaust) — `Familiars.RemoveRandom`; if a familiar was sacrificed, `CreatureCmd.Heal` for `Heal` (10, +3 on upgrade).
- **Decisions:** Heal only fires when a familiar is actually sacrificed (mirrors `RitualSacrifice` block-on-sacrifice gating).
- **Files:** new `Cards/BrokenPact.cs`; `cards.json`; regen `cards.json`. Placeholder art `brokenpact.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest + art.

### 25. New card: Bewitching Grin
- **Done:** 2026-06-28
- **Changed:** New `BewitchingGrin` (Attack, Common, 1, AnyEnemy) — deal 3 damage, apply 3 Hex to target. Upgrade +3 damage. Hex hover tip.
- **Files:** new `Cards/BewitchingGrin.cs`; `cards.json`; regen `cards.json`. Placeholder art `bewitchinggrin.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest + art.

### 24. New card: Soul Knot (+ Soul Knot power)
- **Done:** 2026-06-28
- **Changed:** New `SoulKnot` (Power, Rare, 3, Self) applies `SoulKnotPower` (Single buff). New `SoulKnotPower`: on owner's `AfterDamageReceived`, deals `result.UnblockedDamage` to all hittable enemies (raw `CreatureCmd.Damage`, enemies only → no recursion). Upgrade −1 energy (→2).
- **Decisions:** Mirrors damage taken (unblocked) onto enemies, modeled on `BindInBloodPower`'s `AfterDamageReceived`. Power doesn't stack (`Single`); card applies 1.
- **Files:** new `Cards/SoulKnot.cs`, `Powers/SoulKnotPower.cs`; `cards.json`, `powers.json`; regen `cards.json`. Placeholder art `soulknot.png`.
- **Verified:** build 0/0, regen OK. ⚠️ **Needs in-game playtest** — novel `AfterDamageReceived` reflect onto enemies. Placeholder art.

### 23. New card: Vexing Thwack
- **Done:** 2026-06-28
- **Changed:** New `VexingThwack` (Attack, Common, 3, AnyEnemy) — deal 10 damage 2 times (`DamageVar` + `RepeatVar`/`WithHitCount`), apply 2 Hex to target. Upgrade +2 Hex. Hex hover tip.
- **Files:** new `Cards/VexingThwack.cs`; `cards.json`; regen `cards.json`. Placeholder art `vexingthwack.png`.
- **Verified:** build 0/0, regen OK. ⚠️ Needs playtest + art.

### 15. New card: Cursed Bottles (+ Hex + Cursed Bottles powers)
- **Done:** 2026-06-27
- **Changed:** New `CursedBottles` (Power, Uncommon, 2) applies `CursedBottlesPower`: on each potion use, Hexes `Amount` random enemies (1, +1 on upgrade). New `HexPower` (enemy Counter debuff): on the hexed enemy's `AfterAttack`, splashes its attack's total damage to all OTHER enemies AND all players, then consumes one stack (raw `CreatureCmd.Damage`, no `AttackCommand` → no recursion).
- **Decisions:** Hex semantics per user — "when the ENEMY next attacks, it damages all enemies as well as all players." Splash damage = sum of `command.Results` `TotalDamage`; targets = `GetOpponentsOf ∪ GetTeammatesOf` minus the attacker. Upgrade = +1 Hex per potion (random picks may repeat).
- **Files:** new `Cards/CursedBottles.cs`, `Powers/CursedBottlesPower.cs`, `Powers/HexPower.cs`; `cards.json`, `powers.json`.
- **Verified:** build 0/0. ⚠️ **Needs in-game playtest** — novel `AfterAttack` splash on an enemy-side power, MP framing ("all players"). Placeholder art.

### 16. New card: Ambush!
- **Done:** 2026-06-27
- **Changed:** New `Ambush` (Attack, Rare, 3, AllEnemies, Exhaust): deal 20 to all enemies (upgrade +5), then "summon a random familiar" = apply a random `FamiliarPower` rolled from `ModelDb.AllPowers.OfType<FamiliarPower>()` via the non-generic `PowerCmd.Apply(power.ToMutable(), …)`.
- **Decisions:** No cosmetic pet (only Owl/Cat have one). `Ambush` does **not** implement `IFamiliarSummon` (it's an Attack, shouldn't pollute Embrace-the-Wilds / Broom-Strike which target familiar *Power* cards).
- **Files:** new `Cards/Ambush.cs`; `cards.json`.
- **Verified:** build 0/0. ⚠️ Playtest the random-summon path. Placeholder art.

### 17. New card: Pact of Fury (MP ally-buff)
- **Done:** 2026-06-27
- **Changed:** New `PactOfFury` (Skill, Uncommon, 1, MP-only, AllAllies): self gains 5 Weak; every *other* ally gains 2 Strength (upgrade →4). Mirrors `CircleOfRot`/`ShareTheBrew` teammate iteration, excluding self.
- **Decisions:** Renamed from the colliding "Pact of Agony" per user (solo keeps the name — item 19; MP → **Pact of Fury**).
- **Files:** new `Cards/PactOfFury.cs`; `cards.json`.
- **Verified:** build 0/0. ⚠️ **MP-only — needs co-op playtest.** Placeholder art.

### 18. New card: A Little Sip (+ power)
- **Done:** 2026-06-27
- **Changed:** New `ALittleSip` (Power, Rare, 2; upgrade cost →1) applies `ALittleSipPower` (Counter Buff): `AfterPotionUsed` → `CreatureCmd.Heal(Owner, Amount)` when the potion is yours. Heal = stack amount (1 per copy).
- **Files:** new `Cards/ALittleSip.cs`, `Powers/ALittleSipPower.cs`; `cards.json`, `powers.json`.
- **Verified:** build 0/0. ⚠️ runtime hook — playtest. Placeholder art.

### 19. New card: Pact of Agony (solo)
- **Done:** 2026-06-27
- **Changed:** New `PactOfAgony` (Skill, Uncommon, 0): gain 3 Vulnerable (self), all enemies lose 2 Strength (upgrade 5 / 3). `StrengthLoss` applied as negative `StrengthPower` to `HittableEnemies` (mirrors `Plague`).
- **Decisions:** Keeps the "Pact of Agony" name (MP version renamed to Pact of Fury). Rarity defaulted to Uncommon (note omitted it).
- **Files:** new `Cards/PactOfAgony.cs`; `cards.json`.
- **Verified:** build 0/0. Placeholder art.

### 20. New card: Favorite Spellbook
- **Done:** 2026-06-27
- **Changed:** New `FavoriteSpellbook` (Skill, Uncommon, 0, Exhaust): gain 2 Brambles, draw 1, gain 1 energy, create a Wicked Brew; upgrade removes Exhaust.
- **Decisions:** Rarity defaulted to Uncommon (note omitted it).
- **Files:** new `Cards/FavoriteSpellbook.cs`; `cards.json`.
- **Verified:** build 0/0. Placeholder art.

### 21. New card: Light the Candle (+ Vial of Smoke potion)
- **Done:** 2026-06-27
- **Changed:** New `LightTheCandle` (Skill, Uncommon, 1): upgrade 2 random hand cards (upgrade →4), then create a `VialOfSmoke`. New `VialOfSmoke` potion (Token rarity, Self): gain Block. In-hand upgrades go through `CardCmd.Upgrade` (so they also fire Bursting Roots / Twinroot). Added `PotionTraits.Manual[VialOfSmoke] = Block` + updated the brewing-doc table.
- **Decisions:** Card rarity defaulted to Uncommon. **Vial of Smoke Block = 1, taken literally from the note — almost certainly low (most potions give ~10); trivially tunable in `VialOfSmoke.cs`. Flagging for confirmation.**
- **Files:** new `Cards/LightTheCandle.cs`, `Potions/VialOfSmoke.cs`; `cards.json`, `potions.json`, `Potions/Brewing/PotionTraits.cs`, `Docs/potion-brewing-system.md`.
- **Verified:** build 0/0. ⚠️ Playtest. Placeholder art.

### 22. New card: Witchcraft
- **Done:** 2026-06-27
- **Changed:** New `Witchcraft` (Skill, Rare, X-cost, Exhaust): create X random combat potions (upgrade → X+1) via `PotionFactory.CreateRandomPotionInCombat(Owner, …)` + `PotionCmd.TryToProcure`. `ResolveEnergyXValue()` for X; `IsUpgraded` adds the +1.
- **Decisions:** Rarity defaulted to Rare (X-cost potion generation is strong). Potions are player-scoped (correct on-color pool).
- **Files:** new `Cards/Witchcraft.cs`; `cards.json`.
- **Verified:** build 0/0. Placeholder art.

### Kill Bitter Root (duplicate of Rotting Roots — user-directed)
- **Done:** 2026-06-27
- **Changed:** Removed the `BitterRoot` card + `BitterRootPower` (it had become functionally identical to Rotting Roots after item-13-era change) — `.cs`, `.cs.uid`, art, and localization in `cards.json` + `powers.json`. No code refs remained.
- **Verified:** build 0/0.

### 13. Familiar rework — per-turn card to hand (not shuffle-N)
- **Done:** 2026-06-27
- **Changed:** `FamiliarPower` now gives one producible card to your **hand at the start of each turn** (`AfterPlayerTurnStart`), replacing the on-summon "shuffle N tokens into draw". Added a generic `FamiliarPower<TCard>` base; the 7 single-type powers became one-liners (Owl→Wisdom, Cat→Ferocity, Crow→Scout, Rat→Plague, Porcupine→Quills, Sloth→Laze, Wolf→Gnash); `BearFamiliarPower`→random Hibernate/Mutilate; `ChimeraFamiliarPower`→random familiar card. Each summon card dropped its shuffle block + `CardsVar`; their upgrade now reduces cost by 1. All 9 summon-card descriptions rewritten.
- **Decisions:** into **hand** (your choice); **one card/turn per familiar type**, unscaled (your choice); generated cards un-upgraded; summon-card upgrade = cost −1 (the old count upgrade is gone). Familiar **count** still matters (Pillage/Stampede scale, Ritual Sacrifice consumes).
- **Files:** `Powers/FamiliarPower.cs` + the 9 `*FamiliarPower.cs`; the 9 `*Familiar.cs` summon cards; `cards.json`. (Agent-assisted mechanical pass.)
- **Verified:** build 0/0. ⚠️ **Needs in-game playtest** (turn-start generation timing, MP).

### 14. Serrated Bones → Fertilize (rename + new effect)
- **Done:** 2026-06-27
- **Changed:** Full rename `SerratedBones` → `Fertilize` (class, file, id `THEWICKEN-FERTILIZE`, localization, art `serrated_bones.png`→`fertilize.png`). New effect: **"Gain 6 Brambles. Upgrade {Cards} random card(s) in your hand."** (Brambles 6→7, Cards 1→2 on upgrade). The in-hand upgrades go through `CardCmd.Upgrade`, so they also trigger Bursting Roots / Twinroot.
- **Files:** `TheWickenCode/Cards/Fertilize.cs` (replaces `SerratedBones.cs`), `cards.json`, art renamed.
- **Verified:** build 0/0. ⚠️ Art is the old Serrated Bones image under the new name; old `.import` removed → run **Godot: Import assets** to regenerate before publish.

### Rotting Roots → potion trigger (user-directed mid-run change)
- **Done:** 2026-06-27
- **Changed:** `RottingRootsPower` now gains `Amount` Brambles **whenever you use a potion** (was: whenever you apply a Debuff). Uses `AfterPotionUsed` + `ThrowingPlayerChoiceContext` (the hook gives no context). `powers.json` synced to match (card text was already potion-based). ⚠️ **Now functionally identical to Bitter Root** — flag if you want them differentiated or one removed.
- **Files:** `TheWickenCode/Powers/RottingRootsPower.cs`, `powers.json`.
- **Verified:** build 0/0.

### 12. Stop the Wicked-Brew downgrade fallback on merge
- **Done:** 2026-06-27
- **Changed:** `BrewBook.Brew` now broadens its last-resort pool to any rarity ≥ the step-up rarity (excluding inputs) when the exact step-up rarity holds only the inputs — so a 2-potion brew yields a real higher potion instead of the Token Wicked Brew (downgrade feel). Per the "just the fallback" decision, the Rare ceiling is left as-is (the core step-up was already implemented in `NextRarity`).
- **Files:** `TheWickenCode/Potions/Brewing/BrewBook.cs`.
- **Verified:** build 0/0.

### 11. New Relic: Twinroot (duplicate card on upgrade)
- **Done:** 2026-06-27
- **Changed:** New `Twinroot` relic (Rare). On any card upgrade — in combat AND at rest-site/events — adds a copy of the upgraded card to your deck. Driven by the shared `CardUpgradeTracker` Harmony patch (synchronous `CreateDupe` + `Deck.AddInternal`).
- **Decisions:** Name "Twinroot" (mine — rename freely). Dup goes to the permanent **deck** in both cases (consistent; in-combat copies appear next combats). Rarity Rare (powerful effect) — tune as needed.
- **Files:** `TheWickenCode/Relics/Twinroot.cs` (new), `TheWickenCode/Patches/CardUpgradeTracker.cs` (shared), `relics.json`.
- **Verified:** build 0/0. ⚠️ **Needs in-game + MP playtest** — Harmony-runtime deck mutation; can't be statically verified. Art is placeholder until added (`relics/twinroot[_outline].png`, `big/twinroot.png`).

### 10. New card Rootcraft + power Bursting Roots (+ upgrade hook)
- **Done:** 2026-06-27
- **Changed:** New `CardUpgradeTracker` Harmony patch on `CardModel.UpgradeInternal` (the one chokepoint for in-combat AND rest-site upgrades). New `BurstingRootsPower` (Buff): when you upgrade a card **in hand during combat**, gain {Amount} Brambles — the sync patch enqueues the owed Brambles, the power drains+applies them async in `AfterCardPlayed`. New `Rootcraft` Power card (1, Uncommon) applies Bursting Roots; upgrade raises Brambles-per-upgrade 1→2.
- **Files:** `TheWickenCode/Patches/CardUpgradeTracker.cs`, `TheWickenCode/Powers/BurstingRootsPower.cs`, `TheWickenCode/Cards/Rootcraft.cs` (all new), `cards.json`, `powers.json`.
- **Verified:** build 0/0. ⚠️ **Needs in-game playtest** — Harmony-runtime reaction + the enqueue/flush timing. Art is placeholder (`card_portraits/rootcraft.png` + big, `powers/bursting_roots_power.png`).

### 7. Redesign Dance Around the Cauldron
- **Done:** 2026-06-27
- **Changed:** `DanceAroundTheCauldronPower` now makes a `WickedBrew` on **each Skill played this turn** (via `BeforeCardPlayed`), self-removes at turn end. Dropped the per-unspent-energy-at-turn-end behavior. The Dance card doesn't self-trigger (buff isn't on the creature yet at its own `BeforeCardPlayed`). Card unchanged (cost 1, upgrade →0).
- **Files:** `TheWickenCode/Powers/DanceAroundTheCauldronPower.cs`, `cards.json`, `powers.json`
- **Verified:** build 0/0.

### 6. Redesign Gathering Herbs
- **Done:** 2026-06-27
- **Changed:** New `NextPotionRarePower` (forces the next *created* potion to Rare). `GatherHerbs` now applies it instead of `NextPotionUpgradedPower`. The two rarity-rolling creators (`SomethingWicked`, `ToilAndTrouble`) call `MakeNextRare` before `UpgradeRarity`, so the buff is honored. `NextPotionUpgradedPower` kept (still referenced by those creators; just no longer granted by any card).
- **Files:** `TheWickenCode/Powers/NextPotionRarePower.cs` (new), `Cards/GatherHerbs.cs`, `Cards/SomethingWicked.cs`, `Cards/ToilAndTrouble.cs`, `cards.json`, `powers.json`
- **Verified:** build 0/0.

### 9. Kill the debuff sub-theme (surgical)
- **Done:** 2026-06-27
- **Changed:** Removed the two debuff-*payoff* cards — `Hexburst` and `RancidSmoke` (.cs, .cs.uid, art, localization). Kept plain debuff-appliers per the surgical decision.
- **Verified:** build 0/0 (no dangling refs).

### 5. Remove Witch's Curse
- **Done:** 2026-06-27
- **Changed:** Removed `WitchsCurse` card + `WitchsCursePower` (only WitchsCurse applied it — verified), plus art, uids, and localization in `cards.json` + `powers.json`.
- **Verified:** build 0/0.

### 4. Remove Blood Boiling
- **Done:** 2026-06-27
- **Changed:** Removed `BloodBoiling` card, art, uid, localization.
- **Verified:** build 0/0.

### 3. Remove Tiny Bottle
- **Done:** 2026-06-27
- **Changed:** Removed `TinyBottle` card, art, uid, localization.
- **Verified:** build 0/0.

### 8. Unstable Reaction → 20 damage per potion ("Exploding Brew")
- **Done:** 2026-06-27
- **Changed:** `UnstableReaction` per-potion `DamageVar` 10 → 20 (still discards all potions, then hits all enemies once per potion). Upgrade delta left at +3. Confirmed with user this is the card the "Exploding Brew" note meant.
- **Files:** `TheWickenCode/Cards/UnstableReaction.cs`
- **Verified:** dotnet build → 0/0.

### 2. Fix Extract Essence potion pool leak
- **Done:** 2026-06-27
- **Changed:** Off-color potions (e.g. a Defect potion) leaked because `EnemyEssence` rolled from `PotionCatalog.Query(Common)` = **all** registered pools. Added `PotionCatalog.WickenAndShared` (potions whose `Pool` is `WickenPotionPool` or base `SharedPotionPool` — mirrors `PotionFactory.GetPotionOptions`), and scoped `EnemyEssence.CombatCommons()` to it, filtered to Common rarity + `CanBeGeneratedInCombat`.
- **Files:** `TheWickenCode/Potions/Brewing/PotionCatalog.cs`, `TheWickenCode/Potions/Brewing/EnemyEssence.cs`
- **Verified:** dotnet build → 0/0.

### 1. Fix Find Familiar soft-lock
- **Done:** 2026-06-27
- **Changed:** Reworked `FindFamiliar.OnPlay` to gather `WickenFamiliarCard`s from draw+discard, **guard the empty case** (return → card just discards, never opens an empty selection screen = the soft-lock), and select via `CardSelectCmd.FromSimpleGrid`. Cost lowered to **0**. Upgrade now pulls an **extra** Familiar (`CardsVar` 1→2) instead of cutting cost. Localization updated with the `Plural` tag.
- **Decisions:** "Familiar card" = `WickenFamiliarCard` tokens (matches `PactOfBeasts` + the gold keyword), not `IFamiliarSummon` summon cards. "Your deck" in combat = draw+discard piles. The staging note's "Reagent" card doesn't exist; used `PactOfBeasts`' guarded gather pattern as the reference.
- **Files:** `TheWickenCode/Cards/FindFamiliar.cs`, `TheWicken/localization/eng/cards.json` (`THEWICKEN-FIND_FAMILIAR`)
- **Verified:** `dotnet build TheWicken.csproj` → 0 warnings, 0 errors.

<!-- Append completed items above this line. Template:

### <title>
- **Done:** <date>
- **Changed:** <one line>
- **Files:** <list>
- **Verified:** dotnet build OK
-->
