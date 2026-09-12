using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Wild Growth: for the next N turns, gain Energy and Brambles at turn start (via
/// <see cref="WildGrowthPower" />). Upgrade adds a turn. The per-turn payload is fixed on the power so
/// mixed normal/upgraded plays just add turns.</summary>
public sealed class WildGrowth : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WildGrowthPower>(2m),
        new EnergyVar(1),
        new PowerVar<BramblesPower>(4m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    public WildGrowth()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<WildGrowthPower>(
            choiceContext, Owner.Creature, DynamicVars["WildGrowthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["WildGrowthPower"].UpgradeValueBy(1m);
}
