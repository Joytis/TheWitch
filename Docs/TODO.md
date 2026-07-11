# TODO

Work queue for TheWitch mod. Source of new items: [TODO_STAGING.md](TODO_STAGING.md) (raw notes; ingest from there, then clear).

## Loop protocol (read before working)

1. **Pick** the top-most item whose status is `TODO`. Skip `BLOCKED` and `IN PROGRESS`.
2. **Claim** it: change status to `IN PROGRESS (<who> — <date>)` and save this file *before* editing code. This prevents two agents touching the same item.
3. **Implement** to the item's Acceptance criteria. Touch only the files listed (or note new ones). Update matching localization JSON under `TheWitch/localization/eng/`.
4. **Verify**: `dotnet build` must succeed (it auto-copies into the game mods folder). No test suite — build is the gate.
5. **Finish**: remove the item from this file and append it to [DONE.md](DONE.md) with a one-line note on what changed + the commit/file list.
6. Loop.

**Conflict rule:** items that edit the same file must not run in parallel. Each item lists its files; an orchestrator dispatching agents must serialize overlapping file sets.

**Status legend:** `TODO` · `IN PROGRESS (who — date)` · `BLOCKED (reason)` · (done items live in DONE.md)

---

## Queue (top = next)

### 4. Neverending Potion — buff persists across combats (bug)
- **Status:** BLOCKED (needs repro — static analysis found no persistence path: `Player.AfterCombatEnd` → `RemoveAllPowersInternalExcept()` removes ALL powers, and each combat's power instance is a fresh `ToMutable()` clone with an empty bottled list. Need: which potion was bottled, single- or multiplayer, and what exactly "permanent add" looked like — power icon in next combat? potion back in belt? The item-5 hook fix may also have changed behavior; re-test after it.)
- **Type:** Bug
- **Files:** `TheWitchCode/Powers/NeverendingPotionPower.cs`, `TheWitchCode/Powers/CrystalBottlePower.cs`
- **Acceptance:** Root cause identified + fixed; playtest flag.

### 5. Neverending Potion — third unknown breakage
- **Status:** BLOCKED (no repro — user: "Something else also broke Neverending Potion — unknown repro")
- **Type:** Bug
- **Rule/Decision:** Awaiting repro details. Items 4/5 investigation may surface the cause — revisit after those land.
- **Files:** TBD
- **Acceptance:** TBD
