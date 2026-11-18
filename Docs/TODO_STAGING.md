# TODO — Staging / Inbox

Move Moonlight from Start of turn to End of Enemy Turn. 

## autoslay-loop notes
- (2026-11-07 soak) Silent shop hang, seed CVHQ4LH Act 1 F4: bot's `Buying item (cost: 177)` → `OnTryPurchaseWrapper` never returned, ShopRoomHandler timed out at 120s, no exception anywhere in godot.log. The item isn't logged. Likely a purchase that opened a screen the shop handler can't drive (relic with an on-pickup selection), or a Witch hook on card-added-to-deck / potion-procure that awaited forever. No Witch relic has an on-pickup selection. The replay diverged (no F4 shop) and completed. Logs: scratchpad autoslay-20261107-2015/013-CVHQ4LH.*.log.
- (2026-11-07 soak) NCard node-pool corruption, seed 3ZAUXXG Act 2 F4 (MYTES_NORMAL) onward: 24x `Can't add child '@Control@3514' to 'PlayContainer'/'CombatUi', already has a parent …` from CardPileCmd.CreateCardNodeAndUpdateVisuals / play-pile moves. One NCard instance is handed out by NodePool while still parented (a preview in CombatVfxContainer, then various NHandCardHolders) = it was freed to the pool while still in use. First hit is the first generated card (Myte's Toxic) after BOWLBUGS_WEAK ended mid-animation (bot burned Colorless Potion → Beat Down, Snecko Oil, Bottled Potential, Fysh Oil, then won with Strikes). No Witch frame; the mod never creates/frees NCards (MushroomedCards/FamiliarRarityPatch only restyle). Likely base-game teardown race at bot speed; 1 of 8 runs. Add to $noise if it recurs without a Witch frame. Log: scratchpad autoslay-20261107-2015/008-3ZAUXXG.godot.log.

# BENCHED - NEEDS FURTHER EVALUATION
- Need Unique VFX for Primal Form. 

