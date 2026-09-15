# TODO — Staging / Inbox

## autoslay-loop notes
- AutoSlay seed 55QGFPI run 3 (2026-09-13): silent action-queue hang in Act 2 F3 (EXOSKELETONS_WEAK) right after `Playing THEWITCH-STONY_BREW` (9th Stony Brew of the run, Weak Potion + Moonbeam played just before; no Separatory Funnel; no exception logged). 300s room timeout. Not reproduced in runs 1-2. Logs: %TEMP%utoslay-test-55QGFPI.*
- AutoSlay seed 55QGFPI run 2: NRE in `PetVisuals.Populate` on the run's second Crow summon (Act 2 F7, first Crow at F5 was fine; Moth/Rat/Wolf never fail). No Godot load error logged. Now null-guarded with a diagnostic `pet visuals for ...` log line in `WitchPetVisualsPatch` — watch for that line in future runs to learn which of config/scene comes back null.

# BENCHED - NEEDS FURTHER EVALUATION
- Broken Pact: COMPLETE REWORK. BUNJI BLAST 
- Need Unique VFX for Primal Form. 
