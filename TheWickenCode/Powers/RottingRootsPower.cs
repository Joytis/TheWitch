using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Rotting Roots: at the start of the owner's turn, every enemy loses <see cref="PowerModel.Amount" /> HP
/// (unblockable, non-attack rot) and the witch heals 1 HP. A slow, inevitable decay engine.
/// </summary>
public sealed class RottingRootsPower : WickenPower
{
    private const int HealPerTurn = 1;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, enemies, Amount, ValueProp.Unblockable | ValueProp.Unpowered, Owner, null);
        await CreatureCmd.Heal(Owner, HealPerTurn);
    }
}
