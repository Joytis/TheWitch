using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Wormy: a playable nuisance status (from Wormy Apple). Pay 1 energy to clear one, but it bites — lose 1 life
/// and gain 1 Weak. Retain keeps it clinging to your hand; Exhaust removes it once played. Token rarity keeps
/// it out of random rewards.
/// </summary>
public sealed class Wormy : WickenCard
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(1m),
        new PowerVar<WeakPower>(1m)
    ];

    public Wormy()
        : base(1, CardType.Status, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }
}
