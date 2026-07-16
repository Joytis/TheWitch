using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Propagation (was Lich Powder): the growth spreads — choose up to three cards; each gains Replay 1
/// (game-native <see cref="CardModel.BaseReplayCount" />, the Transfigure/Hidden Gem mechanic).
/// </summary>
public sealed class Propagation : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    public Propagation()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        IEnumerable<CardModel> chosen = await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue), null, this);
        foreach (CardModel card in chosen)
        {
            card.BaseReplayCount++;
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
