using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Bear familiar token: den up — block now, wake up stronger next turn with a new Cub (read by Forage).</summary>
public sealed class Hibernate : WitchFamiliarCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<BearCubPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(15m, ValueProp.Move),
        new PowerVar<StrengthNextTurnPower>(2m),
        new PowerVar<BearCubPower>(1m)
    ];

    public override CardModel Summon => ModelDb.Card<BearFamiliar>();

    public Hibernate()
        : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<StrengthNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["StrengthNextTurnPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BearCubNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["BearCubPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["StrengthNextTurnPower"].UpgradeValueBy(1m);
    }
}
