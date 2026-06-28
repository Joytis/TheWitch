using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

public sealed class BottleWall : WickenCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move),
        new DynamicVar("PerPotion", 6m)
    ];

    public BottleWall()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int used = CombatHistoryQueries.PotionsUsedThisTurn(Owner.Creature, CombatState);
        decimal total = DynamicVars.Block.BaseValue + DynamicVars["PerPotion"].IntValue * used;
        await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["PerPotion"].UpgradeValueBy(2m);
    }
}
