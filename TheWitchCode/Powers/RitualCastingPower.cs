using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Ritual Casting (was Thirst): for every 3 cards the owner draws, apply <see cref="PowerModel.Amount" />
/// Hex to ALL enemies. The draw counter is plain instance state — safe in SP (no mid-combat restore) and
/// MP lockstep; only a mid-combat MP rejoin resets it (accepted trade-off, same as FamiliarPower).
/// </summary>
public sealed class RitualCastingPower : WitchPower
{
    private const int DrawsPerTrigger = 3;

    private int _drawsSinceTrigger;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        _drawsSinceTrigger++;
        if (_drawsSinceTrigger < DrawsPerTrigger)
        {
            return;
        }
        _drawsSinceTrigger = 0;

        Flash();
        await PowerCmd.Apply<HexPower>(choiceContext, combat.HittableEnemies, Amount, Owner, null);
    }
}
