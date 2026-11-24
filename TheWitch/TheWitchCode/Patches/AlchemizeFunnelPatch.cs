using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Separatory Funnel for the base-game card Alchemize, the one base-game in-combat "create a random potion"
/// effect. It rolls through <c>PotionFactory.CreateRandomPotionInCombat</c> (synchronous), not
/// <see cref="PotionCatalog.Pick" />, so the funnel never saw it. With the funnel, OnPlay is replaced: roll
/// <see cref="SeparatoryFunnel.Choices" /> distinct potions with the same factory call (the rest blacklisted,
/// so the base-game pool and rarity weights are kept), choose one, procure it. Without the funnel the
/// original runs untouched.
/// </summary>
[HarmonyPatch(typeof(Alchemize), "OnPlay")]
public static class AlchemizeFunnelPatch
{
    public static bool Prefix(Alchemize __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (__instance.Owner?.GetRelic<SeparatoryFunnel>() is not { } funnel)
        {
            return true;
        }
        __result = Play(__instance, funnel, choiceContext);
        return false;
    }

    private static async Task Play(Alchemize card, SeparatoryFunnel funnel, PlayerChoiceContext choiceContext)
    {
        Player owner = card.Owner;
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        List<PotionModel> options = PotionCatalog.RollFunnelOptions(rolled =>
            PotionFactory.CreateRandomPotionInCombat(owner, owner.RunState.Rng.CombatPotionGeneration, rolled));

        PotionModel? chosen = await PotionCatalog.FunnelChoice(funnel, options, choiceContext, owner);
        if (chosen != null)
        {
            await PotionCmd.TryToProcure(chosen.ToMutable(), owner);
        }
    }
}
