using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// A Little Sip: whenever the player uses a potion, draw <see cref="PowerModel.Amount" /> cards (one stack per
/// copy played). <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so the draw uses
/// <c>ThrowingPlayerChoiceContext</c> (the base-game ReptileTrinket pattern).
/// </summary>
public sealed class ALittleSipPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, potion.Owner);
        }
    }
}
