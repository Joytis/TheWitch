using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Rotting Roots: a slow rot — every turn all enemies wither and the witch mends a little.</summary>
public sealed class RottingRoots : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<RottingRootsPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<RottingRootsPower>(5m)
    ];

    public RottingRoots()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<RottingRootsPower>(choiceContext, Owner.Creature, DynamicVars["RottingRootsPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["RottingRootsPower"].UpgradeValueBy(2m);
}
