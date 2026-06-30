using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// The potion belt UI (<see cref="NPotionContainer" />) only ever reacts to <c>MaxPotionCountChanged</c> by
/// GROWING — its <c>GrowPotionHolders</c> handler adds holder nodes but never removes them, because the base
/// game never shrinks max potion count mid-run. Roomy Satchel does: it grants slots for one combat and reverts
/// them in <c>AfterCombatEnd</c>, so the model contracts but the empty holder nodes linger on screen.
///
/// This postfix runs after <c>GrowPotionHolders</c> and, when the new count is smaller than the holders shown,
/// frees the surplus holders so the belt visually contracts. The model has already migrated/discarded any
/// potions out of the doomed slots before firing the event, so the trimmed holders are empty.
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

        instance.Method("UpdateNavigation").GetValue();
    }
}
