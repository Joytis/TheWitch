using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Slicing Brew: a card-only payload potion (created by Prices Paid). Deals 3 damage 3 times to one enemy.
/// Token rarity keeps it out of the random drop/shop pool while staying registered for
/// <c>PotionCmd.TryToProcure&lt;SlicingBrew&gt;</c>.
/// </summary>
public sealed class SlicingBrew : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Unpowered),
        new RepeatVar(3)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        DamageVar damage = DynamicVars.Damage;
        int hits = DynamicVars["Repeat"].IntValue;
        for (int i = 0; i < hits; i++)
        {
            await CreatureCmd.Damage(choiceContext, target, damage.BaseValue, damage.Props, Owner.Creature, null);
        }
    }
}
