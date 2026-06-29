# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

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
