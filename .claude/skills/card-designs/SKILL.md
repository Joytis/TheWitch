---
name: card-designs
description: View or regenerate the Witch card design reference (Docs/card-designs.html + Docs/card-data/cards.json). Use when the user wants to see the card list/makeup, open the card design page, regenerate the card data after editing cards, mark a card TESTED, or asks where the card docs live.
---

# Card designs reference

Living design documentation for every Witch card. Single source of truth is **`Docs/card-data/cards.json`**; an interactive page renders it with copy-to-console buttons and a TESTED toggle.

## Files (all under `Docs/`)
- **`card-data/cards.json`** — generated data, one object per card (`name`, `entry`, `file`, `cost`, `type`, `rarity`, `target`, `text`, `numbers`, `upgrade`, `note`, `tested`). **Source of truth.**
- **`card-data/regen.js`** — rebuilds `cards.json` from the card `.cs` files + localization. **Preserves `tested` and curated `note` per card.**
- **`card-data/server.js`** — zero-dep Node server (port 7820); persists TESTED toggles back to `cards.json`.
- **`card-designs.html`** — the page: search, rarity filter, sort, makeup counts, per-row `card THEWITCH-<ENTRY>` copy button, live TESTED toggle.
- **`card-designs.cmd`** — double-click launcher (starts server + opens browser).

## View the page
```bash
node Docs/card-data/server.js     # then open http://localhost:7820
```
or double-click `Docs/card-designs.cmd`. Copy buttons spawn a card in-game via the dev console: `card THEWITCH-<ENTRY>`.

## Regenerate after editing cards
After adding/removing a card or changing a card's cost/type/rarity/target/numbers/upgrade/text:
```bash
node Docs/card-data/regen.js          # rewrite cards.json + print a report
node Docs/card-data/regen.js --check  # no write; exit 1 if drift (CI / pre-commit)
```
The report lists **added / removed / changed** cards. When a card's mechanical fingerprint changes, `regen.js` **auto-clears that card's `TESTED` flag** — this is the "clear TESTED on design change" rule, automated. So the normal loop after editing a card is just: edit the `.cs`, run `regen.js`.

## The TESTED contract
- `tested: false` = not yet verified in-game at current design. `true` = verified.
- Only a human flips a card to `true` (via the page toggle, or hand-editing `cards.json`).
- Any design change must reset it to `false` — `regen.js` does this automatically; if you hand-edit a card's numbers in `cards.json`, set `tested:false` yourself.

## Big-art tracking (server only)
The page shows each card's **big portrait** (`TheWitch/images/card_portraits/big/<entry>.png`) as a thumbnail plus a 3-state badge, computed **live** by the server from image hashes:
- **No Art** — file missing, OR equals a known `card.png`, OR is a **duplicate shared by >1 card** (any shared image is a placeholder, not finished art — this catches every generic placeholder variant automatically, not just `card.png`).
- **Placeholder** — a real, distinct image (unique to this card), not yet flagged.
- **Final** — distinct unique image AND the card is flagged (the `★ final` button; persists as `artFinal` in `cards.json`).

Drop a new png into `big/` and reload — state updates with no regen. `regen.js` preserves the `artFinal` flag. **Export no-art names** button copies the newline-separated png filenames still lacking art (every "No Art" card), for handing to an art tool.

## Notes
- `entry` is SCREAMING_SNAKE derived from the class name; the console command needs the `THEWITCH-` mod prefix (base-game cards like `STRIKE`/`DEFEND` collide otherwise).
- New cards land with `tested:false`, `artFinal:false`, and a `note` derived from the class `<summary>`; edit the note in `cards.json` to curate it (regen preserves curated notes).
