using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// The player's next Power card costs 0 to play, but only THIS TURN — the discount is lost at the end of
/// the owner's turn if unused. Applied by Broom Strike. Mirrors the base game's <c>FreePowerPower</c> pattern:
/// discount via <c>TryModifyEnergyCostInCombatLate</c>, consume in <c>BeforeCardPlayed</c> (which runs before
/// the applying card's own play, so it never self-consumes), and self-remove in <c>AfterPlayerTurnEnd</c>.
/// </summary>
public sealed class NextPowerFreePower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || card.Type != CardType.Power)
        {
            return false;
        }
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card.Type == CardType.Power
            && cardPlay.Card.Pile?.Type is PileType.Hand or PileType.Play)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
