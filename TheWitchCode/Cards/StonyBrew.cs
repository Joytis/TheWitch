using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class StonyBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Defensive;

    // First pass: every defensive potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<BlockPotion>(),
        ModelDb.Potion<DexterityPotion>(),
        ModelDb.Potion<Fortifier>(),
        ModelDb.Potion<HeartOfIron>(),
        ModelDb.Potion<LiquidBronze>(),
        ModelDb.Potion<LuckyTonic>(),
        ModelDb.Potion<ShipInABottle>(),
        ModelDb.Potion<SkillPotion>(),
        ModelDb.Potion<SpeedPotion>(),
    ];

    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [];
}
