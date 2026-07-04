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

### 122. Build/portability hardening (deferred from 2026-07-03 structure audit)
- **Status:** TODO
- **What:** Batch of build-infrastructure fixes flagged in the adversarial structure review; deliberately deferred (solo-dev machine works today, these bite contributors/other machines):
  1. Wire the documented `local.props` override: `<Import Project="local.props" Condition="Exists('local.props')"/>` at top of `Directory.Build.props`, add `local.props` to `.gitignore`. (Docs promise this override; nothing imports it.)
  2. Move the machine-specific `GodotPath` (`Directory.Build.props:5`, hardcoded `C:/megadot/...`) into `local.props`; the `NeedGodotForPublish` guard already errors cleanly when unset.
  3. Pin floating `Version="*"` for BaseLib/ModAnalyzers (`TheWicken.csproj:36,38`) to a range (e.g. `3.3.*`) — floating versions make builds non-reproducible and auto-bump the manifest `min_version`, forcing players to update BaseLib needlessly.
  4. Harden the `UpdateDependencyVersions` manifest rewrite (`TheWicken.csproj:89-97`): `WriteLinesToFile` splits content on `;` (a semicolon in any manifest field shatters the JSON) and writes a BOM; also the success message references undefined `$(BaseLibVersion)` (should be `$(ActiveBaseLibVersion)`).
  5. Steam discovery (`Sts2PathDiscovery.props`) misses secondary Steam libraries — parse `libraryfolders.vdf` (or rely on the item-1 escape hatch and document it).
  6. Manifest `version` frozen at `v0.0.0` (`TheWicken.json:6`) — bump per release or derive from csproj.
  7. `.gitattributes`: add explicit binary markers (`*.png binary`, `*.pck binary`, `*.dll binary`) — `text=auto` heuristics alone can LF-corrupt a binary that looks texty.
- **Files:** `Directory.Build.props`, `TheWicken.csproj`, `Sts2PathDiscovery.props`, `TheWicken.json`, `.gitignore`, `.gitattributes`
- **Acceptance:** `dotnet build` green on this machine; `local.props` override honored when present; build with a deliberately semicolon'd manifest description survives round-trip.


