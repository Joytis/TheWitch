using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Rotting Roots: whenever the player applies a Debuff to an enemy, gain <see cref="PowerModel.Amount" />
/// Brambles. Listens to the global <c>AfterPowerAmountChanged</c> hook (delivered to every combat-hook
/// model) and filters to debuffs the player inflicted on someone else.
/// </summary>
public sealed class RottingRootsPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (applier == Owner && power.Owner != Owner && power.Type == PowerType.Debuff && amount > 0m)
        {
            Flash();
            await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}
