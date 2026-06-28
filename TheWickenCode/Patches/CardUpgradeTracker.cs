using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Powers;
using TheWicken.TheWickenCode.Relics;

namespace TheWicken.TheWickenCode.Patches;

/// <summary>
/// Reacts to card upgrades. <see cref="CardModel.UpgradeInternal" /> is the single chokepoint — it runs for
/// in-combat hand upgrades AND rest-site/event upgrades — but it is synchronous, so we can only do synchronous
/// work here:
///   * <see cref="Twinroot" /> relic: duplicates the (now-upgraded) card into the owner's deck. The dup +
///     <c>Deck.AddInternal</c> are both synchronous, so they happen right here, in and out of combat.
///   * <see cref="BurstingRootsPower" />: there is no async context here to apply a power, so we just *enqueue*
///     the owed Brambles (per-creature) when an in-hand card is upgraded in combat. The power drains and applies
///     them in its async <c>AfterCardPlayed</c> hook — the first async beat after the card that did the upgrade.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
public static class CardUpgradeTracker
{
    private static readonly ConditionalWeakTable<Creature, StrongBox<int>> PendingBrambles = new();

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

        // Bursting Roots: only when a card is upgraded while in hand during combat.
        Creature? creature = owner.Creature;
        if (creature?.CombatState != null
            && __instance.Pile?.Type == PileType.Hand
            && creature.GetPower<BurstingRootsPower>() is { } power)
        {
            PendingBrambles.GetValue(creature, _ => new StrongBox<int>(0)).Value += power.Amount;
        }
    }

    /// <summary>Returns and clears the Brambles owed to <paramref name="creature" /> from in-hand upgrades.</summary>
    public static int TakePendingBrambles(Creature creature)
    {
        if (PendingBrambles.TryGetValue(creature, out StrongBox<int>? box))
        {
            int owed = box.Value;
            box.Value = 0;
            return owed;
        }
        return 0;
    }
}
