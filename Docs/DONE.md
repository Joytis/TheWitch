# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

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
