using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// The potion belt UI (<see cref="NPotionContainer" />) only ever reacts to <c>MaxPotionCountChanged</c> by
/// GROWING — its <c>GrowPotionHolders</c> handler adds holder nodes but never removes them, because the base
/// game never shrinks max potion count mid-run. Roomy Satchel does: it grants slots for one combat and reverts
/// them in <c>AfterCombatEnd</c>, so the model contracts but the empty holder nodes linger on screen.
///
/// This postfix runs after <c>GrowPotionHolders</c> and, when the new count is smaller than the holders shown,
/// frees the surplus holders so the belt visually contracts.
///
/// Crucially, the shrink is NOT just cosmetic cleanup of empty holders: when the model contracts,
/// <c>Player.SetMaxPotionCountInternal</c> silently migrates potions from doomed slots into earlier empty
/// slots — no event fires, so the UI node for a migrated potion is still parented to a doomed holder.
/// Freeing that holder would free the potion's node with it, leaving a potion that exists in the model but
/// is invisible and unusable on screen. So before trimming, any potion node stranded in a doomed holder is
/// rebuilt in the holder matching its actual model slot.
/// </summary>
[HarmonyPatch(typeof(NPotionContainer), "GrowPotionHolders")]
public static class PotionBeltShrinkPatch
{
    private static void Postfix(NPotionContainer __instance, int newMaxPotionSlots)
    {
        Traverse instance = Traverse.Create(__instance);
        var holders = instance.Field("_holders").GetValue<List<NPotionHolder>>();
        if (holders == null || holders.Count <= newMaxPotionSlots)
        {
            return;
        }

        for (int i = holders.Count - 1; i >= newMaxPotionSlots; i--)
        {
            holders[i].QueueFree();
            holders.RemoveAt(i);
        }

        // Re-seat potions the model silently migrated out of the trimmed slots: any belt potion whose
        // holder doesn't display it gets a fresh node in the holder at its real slot index.
        var player = instance.Field("_player").GetValue<Player>();
        if (player != null)
        {
            for (int slot = 0; slot < holders.Count; slot++)
            {
                PotionModel? potion = slot < player.PotionSlots.Count ? player.PotionSlots[slot] : null;
                if (potion != null && holders[slot].Potion?.Model != potion)
                {
                    NPotion? node = NPotion.Create(potion);
                    if (node != null)
                    {
                        holders[slot].AddPotion(node);
                    }
                }
            }
        }

        instance.Method("UpdateNavigation").GetValue();
    }
}
