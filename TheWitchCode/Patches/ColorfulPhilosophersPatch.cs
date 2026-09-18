using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Adds the Witch as a color the Colorful Philosophers statues can argue for ("Obtain 3 Witch cards"), so
/// other characters can pick up Witch cards there like any base-game color. The base event keys its options
/// off <c>CardPoolModel.EnergyColorName</c>, which for a BaseLib pool is a model-id string, so the option
/// list is rebuilt here with our own loc key (<c>events.json</c>) instead of appending to the pool order.
/// Same rules as base: the player's own color is skipped, only unlocked characters count, and the list is
/// trimmed to 3 at random. Falls through to the base method whenever the Witch option wouldn't apply.
/// </summary>
[HarmonyPatch(typeof(ColorfulPhilosophers), "GenerateInitialOptions")]
public static class ColorfulPhilosophersPatch
{
    private const string _witchOptionKey = "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.WITCH";
    private const int _maxOptions = 3;

    private static readonly PropertyInfo _cardPoolColorOrder =
        AccessTools.Property(typeof(ColorfulPhilosophers), "CardPoolColorOrder");

    private static readonly MethodInfo _offerRewards =
        AccessTools.Method(typeof(ColorfulPhilosophers), "OfferRewards");

    private static bool Prefix(ColorfulPhilosophers __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner is not { } owner)
        {
            return true;
        }

        CardPoolModel witchPool = ModelDb.CardPool<WitchCardPool>();
        CardPoolModel ownPool = owner.Character.CardPool;
        List<CardPoolModel> unlocked = owner.UnlockState.CharacterCardPools.ToList();
        if (ownPool == witchPool || !unlocked.Contains(witchPool))
        {
            return true;
        }

        List<EventOption> options = [];
        var baseOrder = (IEnumerable<CardPoolModel>)_cardPoolColorOrder.GetValue(null)!;
        foreach (CardPoolModel pool in baseOrder)
        {
            if (ownPool != pool && unlocked.Contains(pool))
            {
                options.Add(new EventOption(__instance, () => Offer(__instance, pool),
                    "COLORFUL_PHILOSOPHERS.pages.INITIAL.options." + pool.EnergyColorName.ToUpperInvariant()));
            }
        }
        options.Add(new EventOption(__instance, () => Offer(__instance, witchPool), _witchOptionKey));

        while (options.Count > _maxOptions)
        {
            options.RemoveAt(__instance.Rng.NextInt(options.Count));
        }
        __result = options;
        return false;
    }

    private static Task Offer(ColorfulPhilosophers ev, CardPoolModel pool) =>
        (Task)_offerRewards.Invoke(ev, [pool])!;
}
