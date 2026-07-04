using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Infuse: the Ancient (transcended) form of Brew — choose and Exhaust cards from your hand, stirring each
/// into The Cauldron as if it were a potion (+2 Strength / +3 Heal apiece). Granted by the Archaic Tooth
/// transcendence map (see <see cref="Patches.AncientTranscendencePatch" />).
/// </summary>
public sealed class Infuse : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPotion<TheCauldron>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public Infuse()
        : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var chosen = await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Hand.GetPile(Owner), Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, DynamicVars.Cards.IntValue));

        int stirred = 0;
        foreach (CardModel card in chosen)
        {
            await CardCmd.Exhaust(choiceContext, card);
            stirred++;
        }

        if (stirred > 0)
        {
            (await TheCauldron.EnsureInBelt(Owner))?.Stir(stirred);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
