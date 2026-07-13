using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Wax and Wane's delayed curse: at the start of the owner's next turn, apply <see cref="PowerModel.Amount" />
/// Hex to ALL enemies, then remove itself (the base-game <c>BlockNextTurnPower</c> one-shot shape).
/// </summary>
public sealed class WaxAndWanePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count > 0)
        {
            await PowerCmd.Apply<HexPower>(choiceContext, enemies, Amount, Owner, null);
        }
        await PowerCmd.Remove(this);
    }
}
