using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Oxidizers buff (counter): the next potion the player plays this turn is played again — its effect
/// re-executes right after the first resolution. One stack consumed per potion; expires at end of turn
/// (the base-game Burst pattern). The replay invokes the protected <c>PotionModel.OnUse</c> directly via
/// reflection rather than <c>OnUseWrapper</c>: the wrapper would call <c>RemoveBeforeUse</c> on the
/// already-removed potion (throws) and re-fire <c>AfterPotionUsed</c> (infinite replay). Direct
/// <c>OnUse</c> skips the throw VFX and history entry; a <see cref="ThrowingPlayerChoiceContext" /> is safe
/// because no potion's <c>OnUse</c> prompts a player choice (none in the base game or this mod).
/// </summary>
public sealed class OxidizersPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private static readonly MethodInfo OnUseMethod = AccessTools.Method(typeof(PotionModel), "OnUse");

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (Amount <= 0 || potion.Owner != Owner.Player)
        {
            return;
        }

        await PowerCmd.Decrement(this);
        Flash();
        CombatManager.Instance.BeginCardOrPotionEffect(potion.Owner);
        try
        {
            await (Task)OnUseMethod.Invoke(potion, [new ThrowingPlayerChoiceContext(), target])!;
        }
        finally
        {
            CombatManager.Instance.EndCardOrPotionEffect(potion.Owner);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
