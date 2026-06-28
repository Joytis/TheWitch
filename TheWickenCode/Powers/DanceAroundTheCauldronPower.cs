using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Dance Around the Cauldron: for the rest of this turn, each Skill the player plays makes a
/// <see cref="WickedBrew" />. Removes itself at the end of the turn. The Dance card that applies this buff does
/// NOT count itself — the buff isn't on the creature yet when its own <c>BeforeCardPlayed</c> fires.
/// </summary>
public sealed class DanceAroundTheCauldronPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Card.Type != CardType.Skill
            || cardPlay.Card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return;
        }
        if (Owner.Player is not { } player)
        {
            return;
        }

        Flash();
        await PotionCmd.TryToProcure<WickedBrew>(player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
