using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Soul Knot: whenever the witch applies a debuff to an enemy, that same debuff (same amount) also lands on
/// every OTHER enemy — spreading your curses across the whole room. Implemented off
/// <see cref="AbstractModel.AfterPowerAmountChanged" /> (the base-game Outbreak pattern): the hook fires on the
/// player's powers for every power change, so we filter to "a debuff I just applied to an enemy" and copy it.
/// A <see cref="_spreading" /> re-entrancy guard stops the copies from re-triggering the spread (infinite loop),
/// and we only copy onto enemies that don't already have that debuff so an already-AoE card doesn't compound.
/// </summary>
public sealed class SoulKnotPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private bool _spreading;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_spreading || applier != Owner || amount <= 0m || power.Type != PowerType.Debuff)
        {
            return;
        }

        Creature landed = power.Owner;
        if (landed == null || landed.Side == Owner.Side || Owner.CombatState is not { } combat)
        {
            return;
        }

        List<Creature> others = combat.HittableEnemies
            .Where(e => e != landed && e.IsAlive && e.GetPower(power.Id) == null)
            .ToList();
        if (others.Count == 0)
        {
            return;
        }

        _spreading = true;
        try
        {
            Flash();
            foreach (Creature enemy in others)
            {
                PowerModel copy = (PowerModel)ModelDb.DebugPower(power.GetType()).ToMutable();
                await PowerCmd.Apply(choiceContext, copy, enemy, amount, Owner, cardSource, silent: true);
            }
        }
        finally
        {
            _spreading = false;
        }
    }
}
