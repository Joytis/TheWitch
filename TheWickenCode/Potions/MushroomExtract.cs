using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Patches;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// Mushroom Extract: discard your hand and draw 6 cards. Those 6 cards are "mushroomed" for the rest of the
/// combat — they cost 0 (<see cref="CardModel.SetToFreeThisCombat" />) and render with random gibberish
/// names/descriptions + mystery art (see <see cref="MushroomedCards" />). They're your real cards, so the
/// chaos sticks for the fight.
/// </summary>
public sealed class MushroomExtract : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count > 0)
        {
            await CardCmd.Discard(choiceContext, hand);
        }

        List<CardModel> drawn = (await CardPileCmd.Draw(choiceContext, 6m, Owner)).ToList();
        Rng rng = Owner.RunState.Rng.CombatCardGeneration;
        foreach (CardModel card in drawn)
        {
            MushroomedCards.Mark(card, rng);
            card.SetToFreeThisCombat();
            CardCmd.Preview(card);
        }
    }
}
