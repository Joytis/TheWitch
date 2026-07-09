using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using HexPower = TheWitch.TheWitchCode.Powers.HexPower;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Read the Bones: curse the enemy now, divine your next draw — apply Hex, then draw extra cards next turn
/// (base-game <see cref="DrawCardsNextTurnPower" />).
/// </summary>
public sealed class ReadTheBones : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m),
        new CardsVar(2)
    ];

    public ReadTheBones()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, VfxCmd.gazePath); // divination read
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["HexPower"].UpgradeValueBy(1m);
}

