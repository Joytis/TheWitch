using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Owl familiar token: memorize a card — copy a card in your hand (base-game Dual Wield pattern:
/// <c>CreateClone</c>, added through the generated-card funnel so creation payoffs fire). Unupgraded
/// the copy target is RANDOM; upgraded you choose it.
/// </summary>
public sealed class Knowledge : WitchFamiliarCard
{
    public Knowledge()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Knowledge can't copy Knowledge (by type, not instance — two in hand copying each other is the same infinite loop).
        CardModel? selection;
        if (IsUpgraded)
        {
            selection = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: Owner,
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: c => c is not Knowledge,
                source: this)).FirstOrDefault();
        }
        else
        {
            List<CardModel> candidates = PileType.Hand.GetPile(Owner).Cards
                .Where(c => c is not Knowledge)
                .ToList();
            selection = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        }

        if (selection == null)
        {
            return;
        }

        WitchFx.EnchantShimmer();
        await CardPileCmd.AddGeneratedCardToCombat(selection.CreateClone(), PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        // Upgrade changes behavior only (random pick -> player choice); no numbers to bump.
    }
}
