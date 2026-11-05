using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Queen of the Rats: <see cref="Rats" /> deal <see cref="PowerModel.Amount" /> additional damage (base-game Accuracy
/// shape, keyed on the Rats card type instead of the Shiv tag). Applied by the Rat familiar's <see cref="Swarm" />.
/// </summary>
public sealed class QueenOfTheRatsPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (Owner != dealer || !props.IsPoweredAttack() || card is not Rats)
        {
            return 0m;
        }

        return Amount;
    }
}
