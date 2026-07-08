using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Wicker Form's replacement effect: while <see cref="WickerFormPower" /> is on the creator, any GENERATED
/// card becomes a <see cref="Rake" />. <c>AddGeneratedCardsToCombat</c> is the single funnel every
/// generated-card path goes through (the singular overload wraps it), so swapping the incoming list here
/// covers familiars, potion-payload cards, Shiv-likes — everything. Cards that are already Rakes pass
/// through untouched (also prevents re-entry when the potion patch below generates its Rake).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
public static class WickerFormCardReplacementPatch
{
    private static void Prefix(ref IEnumerable<CardModel> cards, Player? creator)
    {
        if (creator?.Creature?.GetPower<WickerFormPower>() == null || creator.Creature.CombatState is not { } combat)
        {
            return;
        }
        cards = cards.Select(c => c is Rake || c.Owner != creator ? c : combat.CreateCard<Rake>(creator)).ToList();
    }
}

/// <summary>
/// Wicker Form's potion side: an in-combat potion procure for the power's owner is cancelled and a Rake is
/// generated into the hand instead. Out of combat the power can't exist (combat-scoped), so rewards/shops
/// are untouched.
/// </summary>
[HarmonyPatch(typeof(PotionCmd), nameof(PotionCmd.TryToProcure), typeof(PotionModel), typeof(Player), typeof(int))]
public static class WickerFormPotionReplacementPatch
{
    private static bool Prefix(PotionModel potion, Player player, ref Task<PotionProcureResult> __result)
    {
        if (player.Creature?.GetPower<WickerFormPower>() == null || player.Creature.CombatState == null)
        {
            return true;
        }
        __result = ReplaceWithRake(potion, player);
        return false;
    }

    private static async Task<PotionProcureResult> ReplaceWithRake(PotionModel potion, Player player)
    {
        Rake rake = player.Creature.CombatState!.CreateCard<Rake>(player);
        var generated = await CardPileCmd.AddGeneratedCardToCombat(rake, PileType.Hand, player);
        CardCmd.PreviewCardPileAdd([generated]);
        return new PotionProcureResult
        {
            potion = potion,
            success = false,
            failureReason = PotionProcureFailureReason.NotAllowed,
        };
    }
}
