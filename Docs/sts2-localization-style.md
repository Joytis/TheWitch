# Slay the Spire 2 — Localization Text Formatting Standards

Reverse-engineered from the base game's English localization (`gamedata/localization/eng/`, ~600 cards + powers/relics/potions). This is the house style our `TheWicken/localization/eng/` strings should match so they read as native game text.

---

## 1. Markup tags

Text is rendered with BBCode-style tags. Two kinds: **color** and **animation/layout**. Always close every tag (`[gold]...[/gold]`).

### Color tags — semantic, not decorative

| Tag | Meaning | Usage frequency | Example |
|-----|---------|-----------------|---------|
| `[gold]` | **Game terms / keywords.** Any in-game proper noun: status names, keywords, card types, pile names, currencies, mechanic names. | dominant (~2200) | `gain {AfterimagePower:diff()} [gold]Block[/gold]` |
| `[blue]` | **Dynamic numeric values** in power/potion/relic descriptions (the live number). Also character names in flavor prose. | ~1200 | `gain [blue]{Amount}[/blue] [gold]Dexterity[/gold]` |
| `[green]` | **Beneficial HP / Max HP values**, heal amounts, "Event" card type, unlock prereqs. | ~128 | `Heal for [green]{HealPercent}%[/green] of your Max HP` |
| `[red]` | Warnings, hidden/placeholder flavor, danger flavor. | ~584 | `[red]Details for this relic will be revealed in the future...[/red]` |
| `[orange]` | **Quest cards**, place names (Preon, Scrapyard), royal/title flavor. | ~75 | `[orange]Quest[/orange]` |
| `[purple]` | **Curse cards**, the **Enchant** keyword, dark flavor. | ~138 | `[purple]Curse[/purple]` |
| `[aqua]` | Special named entities/companions (Flaw, Orobas). | ~45 | `restore your [aqua]companion[/aqua]` |
| `[pink]` | Necrobinder character name in prereq/unlock text. | ~16 | `the [pink]Necrobinder[/pink]` |

**The two rules that matter most:**
1. **Every game keyword gets `[gold]`** — in *both* `.description` and `.smartDescription`, and even in fallback/static text. Base game never leaves `Block`, `Weak`, `Energy`, `Hand`, etc. uncolored.
2. **Live numbers get `[blue]`** in powers/potions/relics. (Cards are the exception — see §3, they use `:diff()` which colors itself.)

### Animation / layout tags (flavor & UI only — not gameplay text)

`[b]`/`[i]` bold/italic · `[center]` · `[font_size]` · animated: `[sine]`, `[jitter]`, `[shake]`, `[thinky_dots]`, `[rainbow]` (used for character banter, epoch lore, monster dialogue — never in card/power rules text). `[b]X[/b]` is also used for a literal "X" variable amount (e.g. "Gain Brambles `[b]X[/b]` times").

---

## 2. Token / variable interpolation

Variables are `{Name}` with optional `:function(args)` suffixes.

| Token form | Effect |
|------------|--------|
| `{Var:diff()}` | **The canonical, upgrade-aware value.** Renders the number and auto-colors the green upgrade delta. The standard for *card* description numbers. |
| `{Var}` | Raw value, no upgrade coloring (use inside `[blue]...[/blue]` for powers/potions). |
| `{Var:plural:singular\|plural}` | Picks the word by the value. `{Cards:plural:card\|cards}`. The `{}` inside a branch reprints the number: `{Repeat:plural:next turn\|for the next [blue]{}[/blue] turns}`. |
| `{Energy:energyIcons()}` | Renders energy-orb icon(s). For an inline cost prefix: `0{energyPrefix:energyIcons(1)}` = a "0-cost" marker. |
| `{Var:starIcons()}` | Star-cost icons. |
| `{IfUpgraded:show:A\|B}` | `A` when the card is upgraded, else `B`. Used for branching rules text. |
| `{Cond:choose(KeyA\|KeyB):textA\|textB\|...}` | Multi-branch select on an enum/condition. |
| `{Cond:cond:...}` | Conditional inclusion. |

**Upgradeable created/added card names** use the gold-`+` convention, never `[green]`:
```
[gold]{IfUpgraded:show:Name+|Name}[/gold]
```
The trailing `+` *is* the upgrade marker, and the name stays `[gold]`. Pair with `HoverTipFactory.FromCard<T>(IsUpgraded)` on the source card.

---

## 3. Cards — description style

- **One effect per line, separated by `\n`.** ~90% of base cards (547/606) put each sentence on its own line. `"Draw {Cards:diff()} cards.\nDiscard 1 card."` — **not** `". "` joins. Single-effect cards stay one line.
- **Numbers via `:diff()`**, bare (no manual color): `Deal {Damage:diff()} damage.` `:diff()` handles the green upgrade preview itself.
- **Keywords in `[gold]`**, the value before them bare: `Gain {Block:diff()} [gold]Block[/gold].`
- Each sentence ends with a period (`PERIOD` key `= "."`).
- `HP` / `Max HP` are written plainly (capitalized), **not** goldened. HP *loss* is plain (`Lose {HpLoss:diff()} HP.`); Max HP *gains* and heals take `[green]` on the number.
- Currency is `[gold]Gold[/gold]` (capital G).
- Card types referenced as keywords are goldened: `[gold]Attack[/gold]`, `[gold]Skill[/gold]`, `[gold]Power[/gold]`. (Base occasionally leaves an indefinite "a random Attack" ungilded mid-sentence, but the keyword form is gold.)
- Pile/zone names are **Title Case + gold**: `[gold]Hand[/gold]`, `[gold]Draw Pile[/gold]`, `[gold]Discard Pile[/gold]`, `[gold]Deck[/gold]`.
- **Adding a card** to a pile uses "into": `Add ... into your [gold]Hand[/gold].` Location uses "in": `a card in your [gold]Hand[/gold]`.

## 4. Powers — description style

Powers carry **two** strings:
- `.description` — the generic/static form. **Still fully formatted**: keywords `[gold]`, and a representative number hardcoded in `[blue]` (`Whenever you play a card, gain [blue]1[/blue] [gold]Block[/gold].`). Do not write a plain, un-tagged sentence here.
- `.smartDescription` — the live instance form, value via `[blue]{Amount}[/blue]`: `Whenever you play a card, gain [blue]{Amount}[/blue] [gold]Block[/gold].`

`{Amount}` is the power's stack value; pluralize with `{Amount:plural:...}`. Powers use `[blue]{Amount}[/blue]`, **not** `:diff()`.

## 5. Potions / Relics — description style

- **Potions** mirror powers: live value in `[blue]{Var}[/blue]`, keywords in `[gold]`. `Gain [blue]{Block}[/blue] [gold]Block[/gold].`
- **Relics**: `.description` (value `[blue]{Var}[/blue]` + gold keywords), optional `.eventDescription` (the shorter event-screen variant), optional `.flavor` (prose; may be plain or `[red]` placeholder). Instant-pickup relics open with "Upon pickup, ...".
- "It's free to play this turn." — contraction, exactly this phrasing for free-this-turn cards.

## 6. Capitalization & punctuation quick rules

- Keyword/status/pile/type names: **Title Case** (`Discard Pile`, `Max HP`, `Wicked Brew`).
- Emphatic "ALL" is **all-caps** (`ALL enemies`).
- Every rules sentence ends in a period.
- No double spaces.
- American spelling.

## 7. Checklist when writing a string

1. Each distinct effect on its own `\n` line (cards).
2. Every game keyword wrapped in `[gold]`, Title Case.
3. Card numbers via `{Var:diff()}`; power/potion/relic numbers via `[blue]{Var}[/blue]`.
4. HP/Max HP plain; heals & Max HP gains `[green]`; currency `[gold]Gold[/gold]`.
5. Piles Title-Cased + gold; "into" to add, "in" for location.
6. Power `.description` keeps gold keywords + a `[blue]` sample number.
7. Upgradeable created-card names: `[gold]{IfUpgraded:show:Name+|Name}[/gold]`.
8. Sentence ends with a period; no double spaces; "ALL" caps.
