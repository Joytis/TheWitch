using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Tutor: pull a chosen familiar token-card out of your draw pile into your hand.</summary>
public sealed class FindFamiliar : WickenCard
{
    public FindFamiliar()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? chosen = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner,
            filter: (CardModel c) => c is WickenFamiliarCard)).FirstOrDefault();
        if (chosen != null)
        {
            await CardPileCmd.Add(chosen, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
