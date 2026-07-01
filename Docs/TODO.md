# TODO

Work queue for TheWicken mod. Source of new items: [TODO_STAGING.md](TODO_STAGING.md) (raw notes; ingest from there, then clear).

## Loop protocol (read before working)

1. **Pick** the top-most item whose status is `TODO`. Skip `BLOCKED` and `IN PROGRESS`.
2. **Claim** it: change status to `IN PROGRESS (<who> — <date>)` and save this file *before* editing code. This prevents two agents touching the same item.
3. **Implement** to the item's Acceptance criteria. Touch only the files listed (or note new ones). Update matching localization JSON under `TheWicken/localization/eng/`.
4. **Verify**: `dotnet build` must succeed (it auto-copies into the game mods folder). No test suite — build is the gate.
5. **Finish**: remove the item from this file and append it to [DONE.md](DONE.md) with a one-line note on what changed + the commit/file list.
6. Loop.

**Conflict rule:** items that edit the same file must not run in parallel. Each item lists its files; an orchestrator dispatching agents must serialize overlapping file sets.

**Status legend:** `TODO` · `IN PROGRESS (who — date)` · `BLOCKED (reason)` · (done items live in DONE.md)

---

## Queue (top = next)

### 79. Bug: Familiar pets show HP on hover in multiplayer
- **Status:** BLOCKED (base-game bug, waiting upstream — 2026-06-30)
- **Type:** Bug fix (MP-only) — **not our bug**
- **Investigation (confirmed via decompile):** Root cause is in the base game, not the mod. `NCreature.OnFocus` (`gamedata/src/Core/Nodes/Combat/NCreature.cs:348-361`) is fired by the Hitbox mouse-enter signal; when `_isRemotePlayerOrPet` is true (i.e. hovering a teammate's own player-creature OR a pet owned by a teammate — `Entity.PetOwner != null && !LocalContext.IsMe(Entity.PetOwner)`), it **unconditionally** calls `_stateDisplay.AnimateIn(HealthBarAnimMode.FromHidden)`, with no check against `Entity.Monster.IsHealthBarVisible`. Every *other* gate in the file (`_Ready()` line 195, the local-hover `else` branch at line 364) correctly respects `IsHealthBarVisible`; `OnFocus`'s remote branch is the one place that doesn't. This affects **any** pet with `IsHealthBarVisible => false` viewed by a teammate — confirmed it'd equally affect vanilla `Byrdpip`, not just our `WickenPet`. Ruled out a mod-side cause first: `Entity.Monster` population is synchronous (ctor-only, no setter) and MP is deterministic lockstep (every peer re-executes the same `PlayerCmd.AddPet`/hook locally) — no race, no remote-deserialization path exists.
- **Decision:** User chose to leave it — this is the base game's bug, wait for an upstream fix rather than Harmony-patch around it. Re-open only if the user changes their mind or an upstream patch lands (recheck `NCreature.OnFocus` in a future `gamedata` decompile refresh).
- **Files:** none (no code changed).
