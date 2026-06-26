using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// The Cauldron: the big card-only payoff potion (created by Cackle). "A little of everything" — AoE
/// damage, block, Brambles, and AoE Weak. Token rarity so it never rolls as a random drop.
/// </summary>
public sealed class TheCauldron : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Unpowered),
        new BlockVar(10m, ValueProp.Unpowered),
        new PowerVar<BramblesPower>(10m),
        new PowerVar<WeakPower>(2m)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        ICombatState? combat = Owner.Creature.CombatState;
        if (combat == null)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = combat.HittableEnemies;
        await CreatureCmd.Damage(choiceContext, enemies, DynamicVars.Damage.BaseValue, DynamicVars.Damage.Props, Owner.Creature, null);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<WeakPower>(choiceContext, enemies, DynamicVars.Weak.BaseValue, Owner.Creature, null);
    }
}
