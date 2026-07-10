using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// A Little Sip: whenever the player uses a potion, heal <see cref="PowerModel.Amount" /> HP and gain
/// 1 Strength. <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so the Strength apply uses
/// <c>ThrowingPlayerChoiceContext</c> (the base-game ReptileTrinket pattern).
/// </summary>
public sealed class ALittleSipPower : WitchPower
{
    private const decimal StrengthPerPotion = 1m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await CreatureCmd.Heal(Owner, Amount);
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, StrengthPerPotion, Owner, null);
        }
    }
}
