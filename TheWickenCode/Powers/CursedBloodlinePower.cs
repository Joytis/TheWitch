using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Cursed Bloodline: whenever the player loses life (takes unblocked damage), gain
/// <see cref="PowerModel.Amount" /> Brambles. Flat amount per life-loss event.
/// </summary>
public sealed class CursedBloodlinePower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0)
        {
            Flash();
            await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}
