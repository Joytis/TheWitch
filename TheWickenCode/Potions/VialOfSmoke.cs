using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// Vial of Smoke: a card-only (Token rarity) defensive potion that grants Block. Created by Light the Candle.
/// </summary>
public sealed class VialOfSmoke : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Unpowered)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }
}
