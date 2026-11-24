using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Separatory Funnel for the base-game potion Entropic Brew (fill every empty slot of the target player's belt
/// with random potions). When the TARGET player holds the funnel, each slot is a separate pick of
/// <see cref="SeparatoryFunnel.Choices" /> from the same factory roll (out-of-combat pool, as the original).
/// The choice belongs to the target: thrown at an ally, OnUseWrapper's BranchingPlayerChoiceContext moves the
/// choices onto the ally's own action, the base-game path for a potion that makes another player choose.
/// Without the funnel the original runs untouched.
/// </summary>
[HarmonyPatch(typeof(EntropicBrew), "OnUse")]
public static class EntropicBrewFunnelPatch
{
    public static bool Prefix(PlayerChoiceContext choiceContext, Creature? target, ref Task __result)
    {
        if (target?.Player is not { } player || player.GetRelic<SeparatoryFunnel>() is not { } funnel)
        {
            return true;
        }
        __result = Fill(player, funnel, choiceContext);
        return false;
    }

    private static async Task Fill(Player player, SeparatoryFunnel funnel, PlayerChoiceContext choiceContext)
    {
        while (player.HasOpenPotionSlots)
        {
            // Same pool + rng as the original OnUse on each game branch.
#if STS2_BETA
            Rng rng = player.PlayerRng.NonCombatGeneration;
#else
            Rng rng = player.RunState.Rng.CombatPotionGeneration;
#endif
            List<PotionModel> options = PotionCatalog.RollFunnelOptions(rolled =>
                PotionFactory.CreateRandomPotionOutOfCombat(player, rng, rolled));
            PotionModel? chosen = await PotionCatalog.FunnelChoice(funnel, options, choiceContext, player);
            if (chosen == null || !(await PotionCmd.TryToProcure(chosen.ToMutable(), player)).success)
            {
                break;
            }
        }
    }
}
