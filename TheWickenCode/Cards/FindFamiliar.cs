using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Tutor: choose a familiar summon Power card (an <see cref="IFamiliarSummon" />) from your draw or discard
/// pile and add it to your hand. Free to play. If you have no Familiar powers, the card does nothing and just
/// discards — it never opens an empty selection screen, which is what previously soft-locked the game.
/// </summary>
public sealed class FindFamiliar : WickenCard
{
    public FindFamiliar()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // "Your deck" during combat = draw + discard piles.
        List<CardModel> familiars = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(c => c is IFamiliarSummon)
            .ToList();

        // No familiar to find -> do nothing (card discards normally). Guard the selector so it never opens empty.
        if (familiars.Count == 0)
        {
            return;
        }

        List<CardModel> chosen = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            familiars,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue))).ToList();
        if (chosen.Count > 0)
        {
            await CardPileCmd.Add(chosen, PileType.Hand);
        }
    }

    // Already free; upgrade lets you pull an extra Familiar instead of cutting cost.
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
