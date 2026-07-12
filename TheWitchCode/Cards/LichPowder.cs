using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using HexPower = TheWitch.TheWitchCode.Powers.HexPower;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Lich Powder: become untouchable at the cost of a curse — gain Intangible, gain Hex.</summary>
public sealed class LichPowder : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<IntangiblePower>(1m),
        new PowerVar<HexPower>(3m),
    ];

    public LichPowder()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<HexPower>(choiceContext, Owner.Creature, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);
    }

    // Upgrade softens the drawback: gain 2 Hex instead of 3.
    protected override void OnUpgrade() => DynamicVars["HexPower"].UpgradeValueBy(-1m);
}
