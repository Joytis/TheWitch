using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// A Little Sip: whenever the player uses a potion, heal <see cref="PowerModel.Amount" /> HP.
/// <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so no choice-driven effects are used here.
/// </summary>
public sealed class ALittleSipPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await CreatureCmd.Heal(Owner, Amount);
        }
    }
}
