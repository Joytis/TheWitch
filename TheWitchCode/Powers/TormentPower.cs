using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Torment (self buff, this turn only): whenever you play a potion, apply <see cref="PowerModel.Amount" />
/// Hex to a random enemy (Juggernaut random-target pattern). Removed at the end of your turn.
/// </summary>
public sealed class TormentPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner?.Creature != Owner || Amount <= 0)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = CombatState.HittableEnemies;
        if (enemies.Count == 0 || Owner.Player == null)
        {
            return;
        }

        Creature? hexed = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (hexed == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HexPower>(new ThrowingPlayerChoiceContext(), hexed, Amount, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
