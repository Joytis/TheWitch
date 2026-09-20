# TODO — Staging / Inbox

- Thank you so much!! I'll have to test that out. The upgraded version hits ALL enemies, but it doesn't quite make sense that the base version wouldn't stack. I'll note that in TODO. Thank you!!
- CoM stacks are a bit awkward - maybe have it stack on card play instead? 
- Wild Growth Rework? 

## autoslay-loop notes
- (2026-09-14 soak) Distill bot-stall fix (PotionSelectCmd skips the overlay under AutoSlayer.IsActive) is verified by code path only: the seed KRHHUZG replay diverged and never played Distill with 2+ potions. Re-hit it manually with `launch-witch.ps1 -Solo -AutoSlay` if paranoid.
- (2026-09-14 soak) Base-game noise seen once (seed KRHHUZG, Act 2): `Attempted to play animation on creature Cubex Construct but its creature node doesn't exist!` from CubexConstruct.RepeaterBlastMove. No Witch frame; add to autoslay-run.ps1 $noise if it recurs.

- (2026-09-14 soak) Native crash (exit 0xC0000005, no managed exception) while the game built the VisualOnly combat room for PUNCH_OFF_EVENT_ENCOUNTER, seed T49XS51 Act 1 F11. Same-seed replay diverged (never revisited the event) and cleared. Log: scratchpad autoslay-20260914-2234/005-T49XS51.godot.log. Watch for recurrence; the Witch's only patches on that path (WitchPetVisualsPatch / WitchPetClusterPatch) early-return for non-pets.
- (2026-09-14 soak) Bot limitation, not a mod bug: when the Witch dies to a boss (seed KNDESJR, The Insatiable) the base AutoSlayer waits for a rewards screen and times out. No Witch fix possible.
- (user report) Token-rarity Witch potions (Ominous Flask, Noxious Brew, Ember Jar, Prickly Vial, Puff/Vial of Smoke, Bottled Message, Potion-Shaped Pebble) can be granted by 4 base events that pick uniformly from Character.PotionPool + SharedPotionPool with no rarity filter: TheLegendsWereTrue, BattlewornDummy, EndlessConveyor, Wellspring. Base game avoids this via separate TokenPotionPool/EventPotionPool. Recommended fix: WitchTokenPotionPool + rebind the Token potions. Awaiting design call.

# BENCHED - NEEDS FURTHER EVALUATION
- Broken Pact: COMPLETE REWORK. BUNJI BLAST 
- Need Unique VFX for Primal Form. 
