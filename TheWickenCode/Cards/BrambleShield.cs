using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

public sealed class BrambleShield : WickenCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar("PerBramble", 2m)
    ];

    public BrambleShield()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int made = CombatHistoryQueries.BramblesCreatedThisTurn(Owner.Creature, CombatState);
        decimal total = DynamicVars.Block.BaseValue + DynamicVars["PerBramble"].IntValue * made;
        await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["PerBramble"].UpgradeValueBy(1m);
}
