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

        Rng rng = Owner.RunState.Rng.CombatCardGeneration;
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        for (int i = 0; i < 6; i++)
        {
            // Mark the next card BEFORE drawing it, so its gibberish name/desc + mystery art are already
            // applied during the draw flight animation — the player never sees the real card face.
            await CardPileCmd.ShuffleIfNecessary(choiceContext, Owner);
            CardModel? next = drawPile.Cards.FirstOrDefault();
            if (next == null)
            {
                break;
            }
            MushroomedCards.Mark(next, rng);

            CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
            if (drawn == null)
            {
                break;
            }
            drawn.SetToFreeThisCombat();
            CardCmd.Preview(drawn);
        }
    }
}
