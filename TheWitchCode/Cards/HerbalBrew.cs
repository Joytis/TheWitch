using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class HerbalBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Utility;

    // First pass: every utility potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<BlessingOfTheForge>(),
        ModelDb.Potion<BottledPotential>(),
        ModelDb.Potion<Clarity>(),
        ModelDb.Potion<ColorlessPotion>(),
        ModelDb.Potion<CureAll>(),
        ModelDb.Potion<DistilledChaos>(),
        ModelDb.Potion<DropletOfPrecognition>(),
        ModelDb.Potion<Duplicator>(),
        ModelDb.Potion<EnergyPotion>(),
        ModelDb.Potion<EntropicBrew>(),
        ModelDb.Potion<GamblersBrew>(),
        ModelDb.Potion<LiquidMemories>(),
        ModelDb.Potion<OrobicAcid>(),
        ModelDb.Potion<PowerPotion>(),
        ModelDb.Potion<RadiantTincture>(),
        ModelDb.Potion<SneckoOil>(),
        ModelDb.Potion<StableSerum>(),
        ModelDb.Potion<SwiftPotion>(),
        ModelDb.Potion<TouchOfInsanity>(),
        ModelDb.Potion<BuddyInABottle>(),
        ModelDb.Potion<MushroomExtract>(),
    ];

    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [];
}
