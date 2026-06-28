using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Soul Knot: whenever an attack lands on the owner — whether it eats block or HP — every enemy takes that
/// much damage too (see <see cref="DamageResult.TotalDamage" />). The splash uses raw <c>CreatureCmd.Damage</c>
/// against enemies only, so it never recurses back into <see cref="AfterDamageReceived" /> on the owner.
/// </summary>
public sealed class SoulKnotPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.TotalDamage <= 0 || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        List<Creature> enemies = combat.GetOpponentsOf(Owner)
            .Where(c => c != null && c.IsAlive)
            .ToList();
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, result.TotalDamage, ValueProp.Unpowered, Owner, null);
        }
    }
}
