using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hex (enemy debuff): at the end of the hexed enemy's turn, drain 1 Strength from it and give that Strength to
/// the witch who cast the Hex (<see cref="Applier" />), then remove one stack. A decrementing, consistent power
/// drain — Hex is the thematic means of stealing Strength over time; Strength does the payoff work.
/// </summary>
public sealed class HexPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Hex signature on every application: occult gaze + doom sting on the hexed creature.</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.HexGaze(Owner);
        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Amount <= 0 || !participants.Contains(Owner))
        {
            return;
        }

        Flash();

        // Enemy loses 1 Strength (can go negative), the witch gains it.
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -1m, Owner, null);
        if (Applier is { IsAlive: true } witch)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, witch, 1m, witch, null);
        }

        await PowerCmd.Decrement(this);
    }
}
