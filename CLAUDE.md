# CLAUDE.md


Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## What this is

A **Slay the Spire 2 character mod** ("TheWicken") built from the Alchyr Sts2 BaseLib template. It is a Godot 4.5 / C# (net9.0) project that compiles to a `.dll` + `.pck` loaded by the game at runtime via Harmony patching. There is no standalone runnable app — output is consumed by Slay the Spire 2.

## Build & run

```bash
dotnet build          # compiles, then auto-copies .dll/.pdb/.json into the game's mods/ folder
dotnet publish        # build + invokes headless Godot to export the .pck (full asset packaging)
```

- **Build = deploy.** The `CopyToModsFolderOnBuild` target copies outputs straight into `<Sts2Path>/mods/TheWicken/`. To test in-game: build, then launch Slay the Spire 2.
- The game dir is auto-discovered from the Steam registry/library by [Sts2PathDiscovery.props](Sts2PathDiscovery.props). Override via a `local.props` or `/p:Sts2Path=...` if discovery fails.
- `dotnet publish` requires a real Godot mono executable; path is set in [Directory.Build.props](Directory.Build.props) (`GodotPath`). **Must be Godot 4.5.x** — the game refuses `.pck` files exported by a newer Godot.
- Build references `sts2.dll` and `0Harmony.dll` from the installed game; building fails with a clear error if the game isn't found.
- No test suite. Validation is manual in-game.

## Architecture

The mod assembly is **not** the game's assembly. Game types live under `MegaCrit.Sts2.Core.*`; the BaseLib helper layer under `BaseLib.*`. All mod code is in [TheWickenCode/](TheWickenCode/); all assets/data in [TheWicken/](TheWicken/).

**Entry point:** [MainFile.cs](TheWickenCode/MainFile.cs) is marked `[ModInitializer]`. Its only job is `harmony.PatchAll()` — all integration with the game happens through BaseLib's model registration + Harmony patches, not a main loop.

**Content model pattern.** Each content type has an abstract mod-base class that wires up asset paths, then concrete content subclasses it:
- [Cards/WickenCard.cs](TheWickenCode/Cards/WickenCard.cs) → `CustomCardModel`
- [Powers/WickenPower.cs](TheWickenCode/Powers/WickenPower.cs) → `CustomPowerModel`
- [Relics/WickenRelic.cs](TheWickenCode/Relics/WickenRelic.cs) → `CustomRelicModel`
- [Potions/WickenPotion.cs](TheWickenCode/Potions/WickenPotion.cs) → `CustomPotionModel`

Create new content by subclassing the relevant base in its folder (the BaseLib `ModAnalyzers` and IDE templates assist). Models are looked up at runtime via `ModelDb.Card<T>()`, `ModelDb.Relic<T>()`, etc.

**Pools.** [Character/](TheWickenCode/Character/) defines the character and its `CardPool`/`RelicPool`/`PotionPool`. Content is bound to a pool with the `[Pool(typeof(...))]` attribute on the base class — so individual cards/relics inherit pool membership and don't declare it themselves. [Character/Wicken.cs](TheWickenCode/Character/Wicken.cs) (`PlaceholderCharacterModel`) defines starting HP/deck/relics and references the pools; it currently uses base-game Ironclad placeholders.

**Asset path convention (important).** Content classes derive their image path from `Id.Entry` — the model id, lowercased with the mod prefix stripped (`RemovePrefix().ToLowerInvariant()`). So a card with id `THEWICKEN-Foo` loads `TheWicken/images/card_portraits/foo.png` (and `big/foo.png` for full art). Path helpers live in [Extensions/StringExtensions.cs](TheWickenCode/Extensions/StringExtensions.cs); they fall back to a default placeholder image and log when a file is missing. Add art at the matching path rather than overriding the path methods.

**Localization** is JSON under [TheWicken/localization/eng/](TheWicken/localization/) (`cards.json`, `powers.json`, `relics.json`, `characters.json`, keywords, hover tips). Keys follow `THEWICKEN-<ENTRY>.<field>`. These files are fed to the BaseLib analyzer (`AdditionalFiles` in the csproj) so missing/extra localization is caught at build time.

## Content creation

The mod's `Custom{Card,Relic,Potion,Power}Model` bases are thin wrappers over the game's `MegaCrit.Sts2.Core.Models.{CardModel,RelicModel,PotionModel,PowerModel}`. All content shares one root, `AbstractModel`. Two facts drive everything:

- **Registration is reflection-based.** `ModelDb` discovers content via `ReflectionHelper.GetSubtypesInMods<AbstractModel>()` and instantiates with a parameterless ctor. Subclassing a model base + binding it to a pool (`[Pool]`) is the entire "registration" — there is no manual register call.
- **Behavior is hook overrides, not a loop.** `AbstractModel` exposes ~100 async `Task` event hooks (`BeforeCombatStart`, `AfterDamageGiven`, `AfterCardPlayed`, `AfterPlayerTurnStart`, …) plus ~40 synchronous `Modify*` hooks (`ModifyDamageAdditive`, `ModifyEnergyGain`, …). Relics/powers react by overriding the relevant hook. `ShouldReceiveCombatHooks` gates delivery (relics/potions always; cards only while in a combat pile).

**What you override per type:**
- **Card** → ctor `base(energyCost, type, rarity, targetType)`, `CanonicalVars` (the numbers), `OnPlay(ctx, cardPlay)` (the effect), `OnUpgrade()`. Optional: `CanonicalKeywords`, `CanonicalTags`, `MaxUpgradeLevel`, `HasEnergyCostX`, `CanonicalStarCost`.
- **Relic** → abstract `Rarity` + hook overrides. Instant pickup effects use `HasUponPickupEffect` + `AfterObtained()`. Counter relics use `ShowCounter`/`DisplayAmount`.
- **Potion** → abstract `Rarity`/`Usage`/`TargetType` + `OnUse(ctx, target)`. Use `PotionRarity.Token` (or `Event`) for a potion that should only be granted by a card/relic/effect and never appear as a random drop — see the pool-gating gotcha below.

**Stats live in `DynamicVars`.** `CanonicalVars` returns typed vars (`DamageVar`, `BlockVar`, `PowerVar<T>`, `CardsVar`, …) keyed by name; `DynamicVars.Damage` etc. resolve by that key (`PowerVar<VulnerablePower>` → `DynamicVars.Vulnerable`). The same vars are the tokens referenced in the localization JSON, and `UpgradeValueBy` drives green upgrade previews. `ValueProp` (`Move`/`Unpowered`) controls power scaling of the displayed number.

**Effects go through `Cmd` families, never raw state writes:** `DamageCmd.Attack(n).FromCard(this).Targeting(t).Execute(ctx)`, `PowerCmd.Apply<TPower>(...)`, `CreatureCmd.GainBlock(...)`, `CardCmd`/`CardPileCmd`/`CardSelectCmd`. State mutation is guarded by `AssertMutable()` (canonical instances are immutable; real instances are mutable clones).

**Decompiled game source is the reference.** A full decompile lives at `gamedata/` (**gitignored** — local-only, not committed; it's proprietary game code). The ~400 base-game content classes under `gamedata/src/Core/Models/{Cards,Relics,Potions,Powers}/` are the authoritative examples. Workflow: find the closest base-game class, copy its pattern into a `CustomXModel` subclass here, swap the pool + localization keys. Do **not** paste verbatim decompiled code into tracked files.

## Potion traits & brewing

Potions are a core character identity, so there's a dedicated system to **query potions by what they do** and **brew** two into a higher-rarity one — see [Docs/potion-brewing-system.md](Docs/potion-brewing-system.md) for the full design record. Code: [TheWickenCode/Potions/Brewing/](TheWickenCode/Potions/Brewing/).

- **`PotionTrait`** is a `[Flags]` effect taxonomy (Damage/Block/Buff/Debuff/Poison/Heal/Energy/Draw/CardGen/…) with `Offensive`/`Defensive`/`Utility` masks. `PotionTraits.Of(potion)` classifies *any* potion (base-game or modded) — **manual table first, inference fallback**: the hand-curated `PotionTraits.Manual` table (keyed by model `Type`) is the authoritative source for the potion loot table; any potion **not** in it falls back to auto-derivation from its typed `DynamicVars` + `TargetType`. **Every potion the mod ships must have a `Manual` entry**; when you add or re-tag one, edit `Manual` **and** update the doc's table. Values are best-guesses meant to be balance-tuned.
- **`PotionCatalog`** queries `ModelDb.AllPotions` by trait/rarity/usage/orientation; **`BrewBook.Brew(a, b, rng)`** combines two potions (output rarity = highest + 1; trait **union** → shared-orientation → rarity-only fallback) and returns a real existing potion. Both return *canonical* models — `.ToMutable()` + `PotionCmd.TryToProcure` to grant.
- **Dev console** (mods register commands just by subclassing `AbstractConsoleCmd` — picked up via `ReflectionHelper.GetSubtypesInMods`; debug-console only): `potiontraits` dumps every potion's classification; `mergepotions` brews the first two belt potions (one potion → `WickedBrew`). Code in [TheWickenCode/DevConsole/](TheWickenCode/DevConsole/).

## Conventions & gotchas

- `Nullable` is enabled and `<TreatWarningsAsErrors>` is not, but the BaseLib analyzers surface mod-specific mistakes — read analyzer warnings.
- The csproj **excludes** `TheWicken/**`, `materials/`, `shaders/`, `images/` from compilation — that tree is Godot assets, not C#. Don't put `.cs` there.
- `TheWicken.json` is the mod manifest (id, version, `min_game_version`, BaseLib dependency). The build auto-syncs the BaseLib `min_version` in this file to the actually-restored package version (`UpdateDependencyVersions` target) — don't hand-edit that field.
- The mod id `"TheWicken"` (in [MainFile.cs](TheWickenCode/MainFile.cs)) is the resource path root (`res://TheWicken`) and Harmony id; keep it in sync with the manifest and folder name.
- `git` is configured to normalize all text to LF ([.gitattributes](.gitattributes)).
- **Pool membership ≠ random availability for potions.** `[Pool]` on the base registers content; for potions, *random* drop/shop availability is gated separately by **rarity**. `PotionFactory.CreateRandomPotion` only ever rolls `Common`/`Uncommon`/`Rare` (combat drops *and* merchant stock both route through it), so a potion with rarity `PotionRarity.Token` or `Event` is never generated randomly while remaining registered — so `PotionCmd.TryToProcure<T>()` from a card/relic still grants it. This is how you make a **card-only payload potion** (base-game example: `PotionShapedRock` = `Token`; `FoulPotion`/`GlowwaterPotion` = `Event`). Don't try to exclude it by removing `[Pool]` — that unregisters it and breaks `TryToProcure`.
- **A custom card pool must reach `ModelDb.AllCardPools`, or its cards crash on render with `InvalidOperationException: You monster!`.** `AllCardPools = AllCharacterCardPools.Concat(AllSharedCardPools)`. A pool only enters `AllCharacterCardPools` if some `CharacterModel.CardPool` returns it (one pool per character), and only enters `AllSharedCardPools` if it overrides `IsShared => true` (which calls `ModelDbSharedCardPoolsPatch.Register` from the `CustomCardPoolModel` ctor). A pool that is *neither* referenced by a character *nor* shared is invisible. `CardModel.Pool` then finds no pool, falls through to the `MockCardPool` branch, and `MockCardPool.GenerateAllCards()` calls `NeverEverCallThisOutsideOfTests_ClearOwner()` → **"You monster!"** — and it fires on *render/hover*, not on play (e.g. a `HoverTipFactory.FromCard<T>` preview). **Secondary/utility pools that aren't a character's main `CardPool` (familiar, spawned, token-payload cards) must be `IsShared => true`** — mirrors base-game shared pools `ColorlessCardPool`/`StatusCardPool`/`TokenCardPool`. Token rarity still keeps such cards out of random rewards. (Fixed: [Character/WickenFamiliarCardPool.cs](TheWickenCode/Character/WickenFamiliarCardPool.cs) for `Wisdom`, a `Token` familiar previewed by `OwlFamiliar`.)
