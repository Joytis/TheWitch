---
name: todo-loop
description: Ingest raw notes from Docs/TODO_STAGING.md into a formatted Docs/TODO.md, then work the queue one item at a time — claim the top item, implement it, verify with a build, and move it to Docs/DONE.md. Use when the user wants to process the TODO backlog, "run the todo loop", ingest staging notes, or grind through pending tasks for this mod.
---

# TODO loop

A repeatable workflow for turning rough design notes into implemented, build-verified changes for TheWitch mod. Three docs under `Docs/`:

- **`TODO_STAGING.md`** — raw inbox. The user drops half-formed notes here.
- **`TODO.md`** — the formatted, prioritized work queue (this protocol lives in its header too).
- **`DONE.md`** — completed items, newest first, with what changed + design calls + verification.

## Step 1 — Ingest staging → TODO

For each raw note in `TODO_STAGING.md`:
1. Turn it into a numbered `### N. <title>` item in `TODO.md` with: **Status** (`TODO` / `BLOCKED (reason)`), **Type**, a one-line **Rule/Decision**, the **Files** it'll touch (map design-doc names to real files first — search `TheWitchCode/` + `TheWitch/localization/eng/`), and crisp **Acceptance** criteria so the item is self-contained.
2. **Delete the ingested line from `TODO_STAGING.md`** (the user relies on this — a cleared note = ingested).
3. If a note is **truncated or ambiguous**, file it as `BLOCKED` and surface the gap — do not guess at design intent.

## Step 2 — Resolve genuine blockers

Before coding, use `AskUserQuestion` for decisions that are truly the user's and change what you build (which card a vague name maps to, scope of a sweeping cut, an exploratory design direction). Give a recommended option first. Don't ask about things with an obvious default or that the code answers — pick it, note it, proceed.

## Step 3 — Work the loop

1. **Pick** the top-most `TODO` item (skip `BLOCKED` / `IN PROGRESS`).
2. **Claim** it: set Status to `IN PROGRESS (<who> — <date>)` and save `TODO.md` *before* editing code (prevents two agents colliding on one item).
3. **Implement** to the Acceptance criteria. Touch only that item's files. Update matching localization JSON.
4. **Verify**: `dotnet build "<repo-root>/TheWitch.csproj"` must succeed — **0 errors** is the gate (build = deploy; there's no test suite). Run it from the **repo root**, never from `gamedata/` (that's the decompiled game's own `sts2.csproj`).
5. **Finish**: remove the item from `TODO.md`; append it to `DONE.md` with a one-line summary, the design decisions you made, the files, and `build 0/0`.
6. Loop. Stop only at a real blocker, a build failure, or an empty queue. Honor any pacing the user sets ("grind to next blocker" = don't checkpoint until blocked).

**Batching:** cheap same-type items (content cuts, one-line stat changes) may share ONE build gate — claim each item first, implement the batch, build once, then write each item's own DONE entry. Never batch across types or past anything risky.

**Staging is live:** the user may drop new notes into `TODO_STAGING.md` while the loop runs — a note can even revise an item you just finished. Re-check staging after each build gate and before declaring the queue empty; ingest (or apply directly if it supersedes work still fresh in context) and clear the line.

## Conventions & gotchas

- **Parallelize safely.** Independent, file-isolated work (research; a mechanical multi-file refactor where each agent owns distinct `.cs` files) → spawn agents. But **shared JSON** (`cards.json`/`powers.json`/`relics.json`) is a single conflict surface — serialize those edits yourself; never let parallel agents write the same JSON file. Agents should not run `dotnet build` (shared output) — build once yourself after.
- **Localization plurals:** use the `Plural` tag `{Var:plural:singular|plural}` (and `{Var:diff()}` for the number + upgrade preview), never "card(s)".
- **Removing content:** delete the `.cs` (+ `.cs.uid`), the localization keys (the analyzer flags orphans → build fails), and the art (`.png` + `.png.import`); grep for dangling references first (hover tips, `[Pool]` lists, powers applied by other cards). Registration is reflection-based, so deleting the file deregisters it. **Also check what the cut content *grants*:** a cut card can orphan a payload potion/power that only it procured (Concoct → Villainous Brew) — surface the orphan and ask (`AskUserQuestion`) whether to cascade the cut; deleting user-unnamed files unprompted gets denied.
- **Renaming a card:** full rename = class + file + id-derived localization keys + art path (`Id.Entry`-derived). Rename the `.png`, delete the stale `.import`, and tell the user to run the **Godot: Import assets** task.
- **New content** can ship with placeholder art (paths fall back + log); flag the missing art and the `Images: Generate missing sizes` → `Godot: Import assets` follow-up.
- **Can't verify runtime.** Harmony patches, combat hooks, and MP behavior compile-check only — explicitly flag these for an in-game playtest in the DONE entry.
- Decompiled game source under `gamedata/src/` is the authoritative API reference — find the closest base-game example and copy its pattern.
