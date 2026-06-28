using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Crow familiar token: apply Weak to an enemy. Exhausts.</summary>
public sealed class ClawEyes : WickenFamiliarCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WeakPower>(1m),
    ];

    public ClawEyes()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}
