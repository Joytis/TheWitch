using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Ritual Casting: whenever the owner ends their turn without playing any cards, their next
/// <see cref="PowerModel.Amount" /> Skills are free (base-game <see cref="FreeSkillPower" />).
/// The played-this-turn flag is plain instance state — safe in SP (no mid-combat restore) and
/// MP lockstep; only a mid-combat MP rejoin resets it (accepted trade-off, same as FamiliarPower).
/// </summary>
public sealed class RitualCastingPower : WitchPower
{
    private bool _playedCardThisTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner)
        {
            _playedCardThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        bool played = _playedCardThisTurn;
        _playedCardThisTurn = false;
        if (played)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<FreeSkillPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
