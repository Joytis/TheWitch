using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Crow familiar token: pluck a shiny — fetch a chosen card from the Draw Pile. Exhausts.</summary>
public sealed class Shiny : WitchFamiliarCard
{
    public Shiny()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (chosen != null)
        {
            await CardPileCmd.Add(chosen, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
