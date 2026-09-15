---
name: autoslay-loop
description: Run the game's AutoSlay bot headless on the Witch in a timed loop — launch a run, skim its logs for exceptions/stalls, fix confirmed bugs, re-run the same seed to verify, repeat until the time budget is spent. Use when the user says "/autoslay-loop", "run autoslay for N hours", "soak test the mod", "let the bot find bugs", or wants unattended bug-hunting.
---

# AutoSlay loop

Unattended soak test. The game's own `AutoSlayer` (`gamedata/src/Core/AutoSlay/`) plays a full singleplayer run — random-ish card picks, every room type, up to floor 49 — and `WitchDebug` forces it onto the Witch. This skill runs it repeatedly, skims each run for errors, and fixes what is provably broken.

**Argument:** a time budget — `/autoslay-loop 2h`, `90m`. Default `1h`. The deadline caps *starting* new runs; a run in flight finishes (≤ 25 min, game-enforced `AutoSlayConfig.runTimeout`).

## Tools

- `tools/autoslay-run.ps1 -OutDir <dir> -Index <n> [-Seed <s>] [-Build publish]` — one headless run. Writes `<n>-<seed>.autoslay.log`, `.godot.log`, `.summary.json` and prints a one-line verdict. Exit 0 = run completed with zero unfiltered `[ERROR]` lines.
- `summary.json` fields: `seed`, `exitCode`, `completed`, `lastFloor`, `minutes`, `watchdogHits`, `failure` (the `[AutoSlay] Run failed` block, or null), `lastActions[]` (the bot's last 10 log lines before the failure — what was being played/used), `errors[]` (`count`, `message`, `frame` — deduped by message + first stack frame).
- A **silent hang** (`Operation timed out after 300s` / watchdog, no exception anywhere in `.godot.log`) means an awaited command never completed — an action-queue stall. Find the last `Playing X` in `lastActions`; the culprit is that card's `OnPlay` chain or a hook it fired. If two runs don't reproduce it, stage it with seed + log path rather than guessing.
- Reproduce: same command with `-Seed <seed>`. The run seed + bot RNG derive from it, but replays are **not** identical (observed: same seed, different Act 2 route) — a fix changes RNG consumption, and the bot's timing varies. "Verified" means the specific error is absent from the re-run, not that the run played out the same way.

## Protocol

**0. Setup (once).** Pick `OutDir` = `<scratchpad>/autoslay-<yyyyMMdd-HHmm>`. `dotnet publish` first (`-Build publish` on run #1) — the bot needs the `.pck` or resource loads fail. Compute the deadline. Start a ledger in your head: runs, seeds, errors seen / fixed / deferred.

**1. Run.** Launch `autoslay-run.ps1` via the shell tool with `run_in_background: true` (a run can outlast the 10-min foreground cap). Do nothing else game-related while it runs — **runs are serial**: every instance rotates the shared `%APPDATA%\SlayTheSpire2\logs\godot.log`, so parallel instances clobber each other. While waiting, you may read code for a pending investigation, but never `dotnet build`/`publish` mid-run (build = deploy; it swaps the `.dll` under the running game).

**2. Skim.** Read `summary.json`. Triage each `errors[]` entry and any `failure`:
- **Mod bug** — stack frame in `TheWitch.*`, or a base-game frame reached from Witch content (a `FamiliarPower` hook, a Witch card's `OnPlay`, a Witch relic hook). → fix.
- **Bot limitation** — `failure` is a `WaitHelper` timeout / watchdog dump on a screen the base handlers don't know (e.g. a custom Witch overlay, a selection screen the bot can't drive). Not a mod crash. → note it; if it's a *Witch* screen that stalls the bot, that is worth fixing in the mod side (the bot must be able to finish a run) — ask the user first if the fix means changing game-facing behavior.
- **Base-game noise** — errors with no Witch frame and not caused by Witch content. → add the pattern to `$noise` in `autoslay-run.ps1` only if it recurs across seeds and is clearly not ours; otherwise just record it.
- **Localization formatting errors** (`Localization formatting error!`) → mod bug; the key is named in the message. Fix the loc JSON / var.

**3. Fix — only what you can prove.** CLAUDE.md rules apply in full: trace the actual path against `gamedata/`, produce the concrete failing case (the log line + seed *is* one), never call design intent a bug. Behavior that looks odd but throws nothing is **not** in scope — drop a one-line note into `Docs/TODO_STAGING.md` under a `## autoslay-loop notes` heading (never touch `BENCHED`) and move on. Same for anything needing a design call: stage it, don't ask (the user is not watching).
- Touch only what the error needs. Keep loc JSON in sync. Card mechanic change → `node Docs/card-data/regen.js`.
- `dotnet publish` (not just build — a `.pck`-side fix like loc text needs the export).
- **Verify with the same seed**: `autoslay-run.ps1 -Seed <seed>`. Fixed = that error is gone from the new summary (the run may still die later for an unrelated reason — that's a new item). Not fixed → one more attempt, then stage it and revert if the change is dubious.
- Commit nothing; leave the working tree for the user to review. Do note each fix in the ledger with the file.

**4. Loop.** New random seed, `Index + 1`, back to step 1 until the deadline passes. Between runs, if the user has queued nothing else, the budget is the only stop condition — a stretch of clean runs is a *result*, not a reason to quit early.

**5. Report.** Final message = the ledger, nothing else:
- runs launched, completed, failed (with seeds + last floor for the failures)
- errors found → fixed (file + one line each) / deferred to staging / noise-filtered
- anything the user must act on (a staged design question, a fix you're unsure of, a bot limitation on a Witch screen)
- `OutDir` path so the logs can be inspected

## Gotchas

- **`--headless` still runs the whole UI tree** (dummy display server). Screen handlers work; only hover/focus checks are bypassed. A run is 5–25 min real time.
- **Singleplayer only.** `AutoSlayer.PlayMainMenuAsync` clicks `SingleplayerButton`; there is no multiplayer path in the bot. Lockstep MP bugs are out of scope here (use `launch-witch.ps1 -Players N` by hand).
- The bot sets FastMode, disables FTUE tips, and reveals epochs on the *real* prefs save — it's the same profile the user plays on. Don't run this while the user has the game open.
- Exit-time `RID allocations`/`resources still in use` spam is engine teardown, already filtered.
- A run that hits `lastFloor` = null with `failure` about the main menu usually means the `.pck` is stale/missing (`Build publish`) or the game is already running.
- Do not edit `AutoSlayConfig` timeouts or the base bot — it's game code (`sts2.dll`). Bot-driving fixes belong in `TheWitchCode/Debug/WitchDebug.cs` via Harmony, as the character-select redirect does.
