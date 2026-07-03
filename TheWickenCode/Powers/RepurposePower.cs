using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Repurpose: at the start of the owner's turn, move a random card from the Discard Pile into the hand,
/// then discard a card of the player's choice. Passive toggle (Single stack).
/// </summary>
public sealed class RepurposePower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        var discardCards = PileType.Discard.GetPile(player).Cards.ToList();
        if (discardCards.Count == 0)
        {
            return;
        }

        Flash();
        CardModel pulled = player.RunState.Rng.CombatCardGeneration.NextItem(discardCards)!;
        await CardPileCmd.Add(pulled, PileType.Hand);

        CardModel? toDiscard = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, player, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this)).FirstOrDefault();
        if (toDiscard != null)
        {
            await CardCmd.Discard(choiceContext, toDiscard);
        }
    }
}
