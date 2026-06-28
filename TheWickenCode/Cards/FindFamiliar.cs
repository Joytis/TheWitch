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
/// pile and add it to your hand. Free to play. Uses <c>CardSelectCmd.FromCombatPile</c> (the base-game tutor
/// pattern — see Dredge) rather than <c>FromSimpleGrid</c>: the simple grid is for brand-new/reward cards and
/// soft-locks when handed cards that already live in a combat pile. The selector is single-pile, so we point it
/// at whichever pile actually holds familiars; with none, it returns empty and the card just discards.
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
        static bool IsFamiliar(CardModel c) => c is IFamiliarSummon;

        // The selector takes one combat pile; familiar summon (Power) cards live in the Draw pile in practice,
        // but fall back to Discard so the tutor still works if one ended up there.
        CardPile draw = PileType.Draw.GetPile(Owner);
        CardPile pile = draw.Cards.Any(IsFamiliar) ? draw : PileType.Discard.GetPile(Owner);

        IEnumerable<CardModel> chosen = await CardSelectCmd.FromCombatPile(
            choiceContext,
            pile,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue),
            IsFamiliar);

        await CardPileCmd.Add(chosen, PileType.Hand);
    }

    // Already free; upgrade lets you pull an extra Familiar instead of cutting cost.
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
