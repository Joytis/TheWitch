using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// The Oxidizers replay: after a potion fully resolves via <see cref="PotionModel.OnUseWrapper" />,
/// consume one <see cref="OxidizersPower" /> stack and run the potion's protected <c>OnUse</c> again
/// with the wrapper's REAL <see cref="PlayerChoiceContext" /> — so potions that prompt a player choice
/// (Colorless Potion's choose-a-card screen, Attack/Skill/Power Potion, Gambler's Brew, …) replay
/// correctly instead of throwing. Postfix chains onto the wrapper task, so the replay still runs
/// inside the owning UsePotionAction.
///
/// Direct <c>OnUse</c> (not <c>OnUseWrapper</c>) keeps the replay from recursing and from re-firing
/// <c>RemoveBeforeUse</c>/history/<c>AfterPotionUsed</c>; it skips the throw VFX, matching the old
/// behavior.
/// </summary>
[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
public static class OxidizersReplayPatch
{
    private static readonly MethodInfo OnUseMethod = AccessTools.Method(typeof(PotionModel), "OnUse");

    private static void Postfix(PotionModel __instance, PlayerChoiceContext choiceContext, Creature? target, ref Task __result)
    {
        __result = ReplayAfter(__result, __instance, choiceContext, target);
    }

    private static async Task ReplayAfter(Task original, PotionModel potion, PlayerChoiceContext choiceContext, Creature? target)
    {
        await original;

        OxidizersPower? power = potion.Owner?.Creature?.Powers
            .OfType<OxidizersPower>()
            .FirstOrDefault(p => p.Amount > 0);
        if (power == null)
        {
            return;
        }

        await PowerCmd.Decrement(power);
        power.FlashForReplay();
        choiceContext.PushModel(potion);
        CombatManager.Instance.BeginCardOrPotionEffect(potion.Owner!);
        try
        {
            await (Task)OnUseMethod.Invoke(potion, [choiceContext, target])!;
        }
        finally
        {
            CombatManager.Instance.EndCardOrPotionEffect(potion.Owner!);
            choiceContext.PopModel(potion);
        }
    }
}
