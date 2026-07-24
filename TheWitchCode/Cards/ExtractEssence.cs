using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// ExtractEssence: deal damage; on unblocked damage, create a random potion from a HARD-CODED
/// loot table (Herbal Brew pattern — not a live catalog query), so the pool is tuned per card.
/// </summary>
public sealed class ExtractEssence : WitchCard
{
    // First pass: every Common the card could previously roll (shared pool + Witch pool).
    // Trim/add freely — this list IS the card's roll pool.
    private static IEnumerable<PotionModel> LootTable => [
        // Common
        ModelDb.Potion<AttackPotion>(),
        ModelDb.Potion<BlockPotion>(),
        ModelDb.Potion<ColorlessPotion>(),
        ModelDb.Potion<DexterityPotion>(),
        ModelDb.Potion<EnergyPotion>(),
        ModelDb.Potion<ExplosiveAmpoule>(),
        ModelDb.Potion<FirePotion>(),
        ModelDb.Potion<FlexPotion>(),
        ModelDb.Potion<PowerPotion>(),
        ModelDb.Potion<SkillPotion>(),
        ModelDb.Potion<SpeedPotion>(),
        ModelDb.Potion<StrengthPotion>(),
        ModelDb.Potion<SwiftPotion>(),
        ModelDb.Potion<VulnerablePotion>(),
        ModelDb.Potion<WeakPotion>(),
        ModelDb.Potion<Fertilizer>(),
        ModelDb.Potion<CursedBottle>(),

        // Uncommon
        ModelDb.Potion<FyshOil>(),
        ModelDb.Potion<HeartOfIron>(),
        ModelDb.Potion<LiquidMemories>(),
        ModelDb.Potion<ShipInABottle>(),
        ModelDb.Potion<ShacklingPotion>(),
        ModelDb.Potion<CureAll>(),

        // Rare
        ModelDb.Potion<OrobicAcid>(),
        ModelDb.Potion<BuddyInABottle>(),
        ModelDb.Potion<Duplicator>(),
        ModelDb.Potion<LiquidBronze>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move)
    ];

    public ExtractEssence()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int unblocked = attack.Results
            .SelectMany(hit => hit)
            .Where(d => d.Receiver == target)
            .Sum(d => d.UnblockedDamage);

        if (unblocked <= 0)
        {
            return;
        }

        PotionModel? potion = PotionCatalog.Random(LootTable, Owner.RunState.Rng.CombatPotionGeneration);
        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
