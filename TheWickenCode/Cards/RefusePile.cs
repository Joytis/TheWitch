using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Refuse Pile: block up and seed your piles with Rats — two into the draw pile, two into the discard.</summary>
public sealed class RefusePile : WickenCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(IsUpgraded),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(11m, ValueProp.Move),
        new CardsVar(2)
    ];

    public RefusePile()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        int each = DynamicVars.Cards.IntValue;
        await AddRats(each, PileType.Draw);
        await AddRats(each, PileType.Discard);
    }

    private async Task AddRats(int count, PileType pile)
    {
        foreach (Rats rat in FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, count, CombatState, IsUpgraded))
        {
            await CardPileCmd.AddGeneratedCardToCombat(rat, pile, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
