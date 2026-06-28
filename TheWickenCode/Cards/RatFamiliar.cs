using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class RatFamiliar : WickenCard, IFamiliarSummon
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Plague>(false),
    ];

    public RatFamiliar()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<RatFamiliarPower>(choiceContext);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
