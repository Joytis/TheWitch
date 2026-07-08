using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wormy: a playable nuisance status (from Wormy Apple). Pay 1 energy to clear one, but it bites — lose 1 life
/// and gain 1 Weak. Retain keeps it clinging to your hand; Exhaust removes it once played. Status rarity keeps
/// it out of random rewards (like base-game Wound).
/// </summary>
public sealed class Wormy : WitchCard
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
        : base(1, CardType.Status, CardRarity.Status, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Base-game wormy squirm on the player (globally preloaded).
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NWormyImpactVfx.Create(Owner.Creature));
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }
}
