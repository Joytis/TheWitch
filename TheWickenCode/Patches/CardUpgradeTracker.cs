using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Relics;

namespace TheWicken.TheWickenCode.Patches;

/// <summary>
/// Reacts to card upgrades. <see cref="CardModel.UpgradeInternal" /> is the single chokepoint — it runs for
/// in-combat hand upgrades AND rest-site/event upgrades — but it is synchronous, so we can only do synchronous
/// work here:
///   * <see cref="Twinroot" /> relic: duplicates the (now-upgraded) card into the owner's deck. The dup +
///     <c>Deck.AddInternal</c> are both synchronous, so they happen right here, in and out of combat.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
public static class CardUpgradeTracker
{
    private static void Postfix(CardModel __instance)
    {
        Player? owner = __instance.Owner;
        if (owner == null)
        {
            return;
        }

        // Twinroot: a permanent copy of the upgraded card. Runs in and out of combat.
        if (owner.GetRelic<Twinroot>() != null)
        {
            owner.Deck.AddInternal(__instance.CreateDupe(), -1, false);
        }
    }
}
