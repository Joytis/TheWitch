using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Cursed Bottles: whenever the player uses a potion, apply <see cref="HexPower" /> to <see cref="PowerModel.Amount" />
/// random enemies. <c>AfterPotionUsed</c> gives no PlayerChoiceContext, so a <see cref="ThrowingPlayerChoiceContext" />
/// is used for the Hex application.
/// </summary>
public sealed class CursedBottlesPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner.Player || Owner.CombatState is not { } combat)
        {
            return;
        }

        List<Creature> enemies = combat.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        for (int i = 0; i < Amount; i++)
        {
            Creature pick = Owner.Player!.RunState.Rng.CombatTargets.NextItem(enemies)!;
            await PowerCmd.Apply<HexPower>(ctx, pick, 1m, Owner, null);
        }
    }
}
