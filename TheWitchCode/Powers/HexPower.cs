using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hex (enemy debuff): each stack is a marker that explodes for flat damage back at whoever lands an
/// attack on the hexed creature — every stack fires independently per hit, so a heavily-hexed enemy
/// punishes every attacker (the witch herself, an ally, or anyone else hitting it) on every swing.
/// Stacks persist (no decrement, no consumption on trigger) — mirrors the base-game Flame Barrier shape.
/// </summary>
public sealed class HexPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const decimal DamageAmount = 10m;

    /// <summary>Hex signature on every application: occult gaze + doom sting on the hexed creature.</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.HexGaze(Owner);
        await Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer is not { IsAlive: true } || Amount <= 0 || !props.IsPoweredAttack())
        {
            return;
        }

        Flash();
        WitchFx.PurpleFlame(dealer);
        await CreatureCmd.Damage(choiceContext, [dealer], DamageAmount * Amount, ValueProp.Unpowered, Owner, null);
    }
}
