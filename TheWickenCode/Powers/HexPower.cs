using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Hex (enemy debuff): when the hexed attacker <em>next attacks</em>, its attack damage rebounds onto the
/// attacker itself and all of its allies (in addition to the attack's normal targets). One stack is consumed
/// per attack. The splash uses raw <c>CreatureCmd.Damage</c> (not an <c>AttackCommand</c>), so it does not
/// recurse back into <see cref="AfterAttack" />.
/// </summary>
public sealed class HexPower : WickenPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner || Amount <= 0 || Owner.CombatState is not { } combat)
        {
            return;
        }

        decimal damage = command.Results.SelectMany(hit => hit).Sum(d => d.TotalDamage);

        // Consume one stack before splashing.
        await PowerCmd.Decrement(this);
        if (damage <= 0m)
        {
            return;
        }

        Flash();
        List<Creature> targets = combat.GetTeammatesOf(Owner)
            .Append(Owner)
            .Where(c => c != null && c.IsAlive)
            .Distinct()
            .ToList();
        if (targets.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, targets, damage, ValueProp.Unpowered, Owner, null);
        }
    }
}
