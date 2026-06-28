using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Rotting Roots: whenever the player uses a potion, gain <see cref="PowerModel.Amount" /> Brambles.
/// <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so the Brambles application uses a
/// <see cref="ThrowingPlayerChoiceContext" />.
/// </summary>
public sealed class RottingRootsPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await PowerCmd.Apply<BramblesPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null);
        }
    }
}
