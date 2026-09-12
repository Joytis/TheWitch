using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Moth familiar token: a pinch of star dust — apply Moonlight to an enemy.</summary>
public sealed class StarDust : WitchFamiliarCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MoonlightPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<MoonlightPower>(3m)
    ];

    public StarDust()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext, cardPlay.Target, DynamicVars.Moonlight().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Moonlight().UpgradeValueBy(1m);
}
