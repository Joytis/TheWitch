using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hex (enemy debuff): the hexed creature takes flat bonus damage on every hit of an incoming attack
/// (per stack), then loses 1 stack once per attack — a multi-hit attack gets the full bonus on each
/// hit but only burns one Hex. Defender-side mirror of the base-game Vigor shape
/// (ModifyDamageAdditive per hit + AfterAttack consume).
/// </summary>
public sealed class HexPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const decimal DamagePerStack = 3m;

    /// <summary>Hex signature on every application: occult gaze + doom sting on the hexed creature.</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.HexGaze(Owner);
        await Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0 || !props.IsPoweredAttack())
        {
            return 0m;
        }

        return DamagePerStack * Amount;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Amount <= 0 || !Owner.IsAlive || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }

        if (!command.Results.SelectMany(hit => hit).Any(result => result.Receiver == Owner))
        {
            return;
        }

        Flash();
        WitchFx.PurpleFlame(Owner);
        await PowerCmd.Decrement(this);
    }
}
