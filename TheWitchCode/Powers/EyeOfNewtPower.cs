using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Eye of Newt: the owner's potions deal +100% damage per stack (multiplier = 1 + Amount — 1 stack
/// doubles, 2 stacks triple, additive not exponential). Potion damage carries no potion identity into the damage pipeline, so
/// <c>BeforePotionUsed</c> / <c>AfterPotionUsed</c> bracket the use with a transient flag and
/// <c>ModifyDamageMultiplicative</c> amplifies the owner's damage only while a potion is resolving.
/// Plain instance field is safe: single-player has no mid-combat restore and MP is deterministic lockstep.
/// </summary>
public sealed class EyeOfNewtPower : WitchPower
{
    private bool _potionInUse;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            _potionInUse = true;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        _potionInUse = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_potionInUse || dealer != Owner)
        {
            return 1m;
        }
        Flash();
        return 1m + Amount;
    }
}
