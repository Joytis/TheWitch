using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Ritual Casting: whenever the owner plays a card that costs 2 or more (its cost when played,
/// <see cref="ResourceInfo.EnergyValue" /> — so auto-plays of big cards count too), <see cref="PowerModel.Amount" />
/// distinct random cards in their hand become free to play this turn (one per stack). Only cards that would
/// actually benefit are eligible: Unplayable cards (curses/statuses) and cards already costing 0 (incl. X)
/// are skipped.
/// </summary>
public sealed class RitualCastingPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Player is not { } player
            || cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Resources.EnergyValue < 2)
        {
            return Task.CompletedTask;
        }

        List<CardModel> hand = PileType.Hand.GetPile(player).Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable)
                && c.EnergyCost.GetWithModifiers(CostModifiers.All) > 0)
            .ToList();
        if (hand.Count == 0)
        {
            return Task.CompletedTask;
        }

        hand.UnstableShuffle(player.RunState.Rng.CombatCardSelection);
        Flash();
        foreach (CardModel pick in hand.Take((int)Amount))
        {
            pick.SetToFreeThisTurn();
            CardCmd.Preview(pick);
        }
        return Task.CompletedTask;
    }
}
