using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Tutor: choose a familiar summon Power card (an <see cref="IFamiliarSummon" />) from your draw pile and add
/// it to your hand. Free to play. Mirrors the base-game Droplet of Precognition / Seeker Strike shape:
/// <c>CardSelectCmd.FromCombatPile</c> over the draw pile with a <c>SelectionScreenPrompt</c>. That prompt is a
/// REQUIRED loc key (<c>.selectionScreenPrompt</c>) — the getter throws without it, which is what previously
/// killed the play action mid-animation and left the card hanging center screen.
/// </summary>
public sealed class FindFamiliar : WitchCard
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

        IEnumerable<CardModel> chosen = await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue),
            IsFamiliar);

        await CardPileCmd.Add(chosen, PileType.Hand);
    }

    // Already free; upgrade lets you pull an extra Familiar instead of cutting cost.
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
