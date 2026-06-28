using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Round up every familiar summon Power card (an <see cref="IFamiliarSummon" />) from your draw and discard piles into your hand.</summary>
public sealed class PactOfBeasts : WickenCard
{
    public PactOfBeasts()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> familiars = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(c => c is IFamiliarSummon)
            .ToList();
        if (familiars.Count > 0)
        {
            await CardPileCmd.Add(familiars, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
