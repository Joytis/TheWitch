using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Sprouts: for the rest of this turn, each card the player plays grows <see cref="PowerModel.Amount" />
/// Brambles. The Sprouts card that applies this does NOT count itself (source-card skip, Cauldron Dance
/// pattern); removes itself at the end of the turn.
/// </summary>
public sealed class SproutsPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private CardModel? _sourceCard;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _sourceCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || ReferenceEquals(cardPlay.Card, _sourceCard))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
