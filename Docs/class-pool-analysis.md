# Base-Game Class Pool Analysis

Reference study of how Slay the Spire 2's base characters are built, done to inform Wicken
design. For each class we categorized every card on two axes and looked at the resulting
shape. The point is not the raw counts — it's the **patterns** that separate a real mechanical
*pillar* from a splashed keyword, and the healthy ratios a pillar should hit.

## Where the data lives

- **Interactive view:** [card-designs.html](card-designs.html) — tabs for Wicken / Silent /
  Necrobinder / Ironclad, each with filter chips and a live Mechanic × Role matrix.
- **Data:** `Docs/card-data/{silent,necrobinder,ironclad}.json` (base-game, read-only) and
  `cards.json` (Wicken). Each card carries `mechanics[]` and `role[]` tags.
- **Regen:** `node Docs/card-data/gen-basegame.js [class]` rebuilds the base-game files from the
  decompiled `gamedata/` source, **preserving the curated tags** (like `regen.js` preserves
  `tested`). gamedata/ is gitignored, so the committed JSON is the snapshot.

## Method — the two axes

**Axis 1 — Mechanic (pillar):** which of the class's core themes a card touches. Determined by
reading the card's actual effect (OnPlay / powers / hooks), not its name. `None` = no pillar.

**Axis 2 — Role:** what the card *does* for that mechanic.
- **Generator** — produces the resource (gain Strength, summon a minion, apply Poison, make a potion).
- **Payoff** — scales off / consumes it (damage per minion, double the debuff, spend the resource).
- **Enabler** — synergy glue: tutor, retain, discount, draw, energy that feeds the mechanic.
- **Standalone** — generic stats with no pillar interaction (plain damage/block/rider debuff).
- **Token** — (Wicken only) a generated payload card, e.g. familiar tokens.

A card can carry several of each. In the matrices below, a multi-tag card counts in every cell
it spans, so rows/columns can exceed the card total.

---

## The three big lessons

### 1. A mechanic is a *pillar* only if it has a payoff cluster

The single sharpest test. Applying something is not a theme — **exploiting** it is. The clearest
proof is the same keyword landing differently in different classes:

| Keyword | Class | Appliers | Payoffs | Verdict |
|---|---|---|---|---|
| **Vulnerable** | Ironclad | 8 | 5 (Bully, Dismantle, Cruelty, Dominate, +amp) | **Pillar** |
| **Ethereal** | Necrobinder | 2 | 4 (Pagestorm, SpiritOfAsh, PullFromBelow, Banshee's Cry) | **Pillar** |
| **Weak** | Ironclad | many | 1 (a single conditional) | Not a theme |
| **Weak** | Silent | 6 | 1 (Tracking) | Not a theme |
| **Sly** | Silent | 2 grant | 0 (rides the Discard pillar) | Not a theme |

Rule of thumb for promoting a candidate to a pillar: **~3–4 dedicated payoff cards** plus
appliers. Below that it's a rider, not an archetype.

### 2. Generator-heavy is fine *if the resource acts on its own*; spend-it resources need ~1:1

Two distinct resource shapes, and they want different gen:payoff ratios:

- **Self-acting** (ticks/scales passively): **Poison** (ticks every turn), **Strength**
  (multiplies every attack). These run heavily generator-weighted and that's *correct* — Silent
  Poison is 9:3, Ironclad Strength is 8:2, both top-tier archetypes. The resource is its own payoff.
- **Spend-it** (inert until cashed out): **Osty** (summon → attack/sacrifice), **Exhaust**
  (exhaust → trigger), **Doom** (apply → detonate). These need a real payoff cluster, ~1:1.
  Necrobinder Osty is 11 gen : 13 payoff; Ironclad Exhaust is 11 : 10. Balanced both ways.

The diagnostic question for any new mechanic: *does it do something each turn on its own, or
sit there until a payoff spends it?* That answer sizes how many payoff cards you owe it.
The cautionary counter-example is Necrobinder **Soul** (8 gen : 1 payoff) — a hoarded token
resource with almost nothing to spend it on; it feels thin precisely because it's spend-it but
was built like a self-acting one.

### 3. Base classes are ~half "neutral" filler — that flexibility is deliberate

Share of each pool that touches **no** pillar (`None`):

| Class | None-share |
|---|---|
| Silent | 63% |
| Ironclad | 46% |
| Necrobinder | 41% |
| **Wicken (for contrast)** | **16%** |

Base classes devote roughly half their cards to generic block / draw / vanilla attacks /
splashed debuffs. That neutral mass is what makes a deck *draftable* — flexible glue that works
in any build. Wicken is ~3× more mechanic-saturated: tight identity, but thin on the connective
tissue that smooths draws. Not necessarily wrong, but a deliberate trade to be aware of.

### Payoff-light themes can still be cheap to add — the "Ethereal model"

Necrobinder's Ethereal is the inverse of every other pillar: **payoffs (4) outnumber generators
(2).** It works because the deck's *incidental* Ethereal cards (Ethereal as a stat-balancing
downside on plain cards) become free fuel for the payoffs. Lesson: you can bolt on a pillar with
payoffs **only**, harvesting keyword cards already in the pool, without building a new generator
suite. The cheapest way to widen.

---

## Ironclad — 87 cards · Strength · Exhaust · Self-Harm · Vulnerable

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Strength | 8 | 2 | 0 | 0 | 8 |
| Exhaust | 11 | 10 | 5 | 0 | 19 |
| Self-Harm | 10 | 4 | 1 | 0 | 12 |
| Vulnerable | 8 | 5 | 1 | 0 | 12 |
| None | 0 | 3 | 15 | 22 | 40 |

- **Exhaust** is the textbook balanced engine — biggest pillar (19), near-1:1 gen:payoff, plus
  enablers. Exhaust-for-value (Cinder, True Grit, Burning Pact) ↔ exhaust-payoff (Feel No Pain,
  Dark Embrace, Fiend Fire, Ashen Strike).
- **Strength** — pure self-acting; 8 generators, almost no payoffs needed (Inflame, Demon Form,
  Limit Break-style). Multiplies every attack on its own.
- **Self-Harm** — HP as a cost (Bloodletting, Offering, Hemokinesis) with real payoffs that
  reward losing HP (Rupture gains Strength, Spite, Tear Asunder).
- **Vulnerable** — a *debuff* pillar: appliers (Bash, Thunderclap, Break) feeding a payoff cluster
  (Bully scales per Vulnerable, Dismantle double-hits, Cruelty +25%, Dominate → Strength). The
  proof that an apply-then-exploit debuff can be a full archetype.

## Silent — 88 cards · Poison · Shivs · Discard

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Poison | 9 | 3 | 1 | 0 | 12 |
| Shivs | 10 | 2 | 0 | 0 | 12 |
| Discard | 9 | 1 | 7 | 0 | 11 |
| None | 0 | 1 | 11 | 43 | 55 |

- **Poison** — self-acting damage-over-time; generator-heavy is correct (Deadly Poison, Snakebite,
  Noxious Fumes) with a few exploiters (Mirage = Block per enemy Poison, Accelerant, Bubble Bubble).
- **Shivs** — generated 0-cost knife tokens (Blade Dance, Infinite Blades, Phantom Blades); the
  Shivs themselves are the payoff, so few dedicated payoff cards (Accuracy buffs them).
- **Discard** — note the **7 enablers**: Discard is as much a card-selection engine as a payoff
  theme (Survivor, Acrobatics, Tools of the Trade discard *for value*); only Memento Mori truly
  scales off it.
- **63% None** — the most neutral-heavy pool; lots of flexible block/draw/0-cost glue.
- **Rejected candidates** (checked, not pillars): **Sly** (rides Discard — a Sly card auto-plays
  when discarded; no dedicated payoffs), **Weak** (1 payoff: Tracking), **card-draw** (3 scattered
  payoffs — Murder/Speedster/Corrosive Wave — but no shared engine; a *minor* sub-theme).

## Necrobinder — 88 cards · Osty (summons) · Soul · Doom · Ethereal

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Osty | 11 | 13 | 6 | 0 | 24 |
| Soul | 8 | 1 | 1 | 0 | 9 |
| Doom | 8 | 5 | 3 | 0 | 13 |
| Ethereal | 2 | 4 | 1 | 0 | 7 |
| None | 0 | 1 | 6 | 29 | 36 |

- **Osty** — the summoner identity and the **benchmark for a minion archetype**: 11 gen : 13
  payoff. Summon (Bodyguard, Reanimate, Legion of Bone) ↔ command/sacrifice (Unleash, Sacrifice,
  Bone Shards, Rattle scales per Osty-attack). This is what a *spend-it* pillar should look like.
- **Doom** — apply-and-detonate debuff (Blight Strike, Negative Pulse apply; Countdown, Death's
  Door, Time's Up pay off). Balanced ~8:5.
- **Soul** — a generated token-card resource (Grave Warden, Reave, Capture Spirit make Souls;
  Soul Storm cashes them). 8:1 — thin, the spend-it-built-like-self-acting cautionary case.
- **Ethereal** — the payoff-heavy "harvest the incidentals" pillar (see lesson above).

---

## How this maps onto Wicken (the reason for the study)

Wicken pillars: **Brambles · Potions · Familiars** (86 cards, only 16% None).

- **Familiars** (14 gen : 5 payoff, +12 token cards) is a summon archetype — its benchmark is
  Necrobinder's Osty at ~1:1. The gap is payoff cards (scale-per-familiar, sacrifice-for-value),
  not more summons.
- **Brambles** (13 gen : 4 payoff) is the open question: it depends whether Brambles is
  *self-acting* (then generator-heavy is fine, à la Poison/Strength) or *spend-it* (then it owes
  payoffs, à la Osty/Exhaust). Decide which, then size accordingly.
- **Potions** (14 : 8 : 6) is the healthiest pillar — generators, payoffs, and enablers all
  present, the only one that looks like a finished archetype.
- Wicken's **16% neutral** vs. the base ~41–63% means far less draftable glue — a deliberate
  saturation trade.

The "payoff cluster" test (lesson 1) is the gate for any proposed 4th Wicken mechanic: it's only
a pillar if 3–4 cards *exploit* it, not just apply it.
