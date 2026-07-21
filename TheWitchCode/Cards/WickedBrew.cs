using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class WickedBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Offensive;

    // First pass: every offensive potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<AttackPotion>(),
        ModelDb.Potion<BeetleJuice>(),
        ModelDb.Potion<ExplosiveAmpoule>(),
        ModelDb.Potion<FirePotion>(),
        ModelDb.Potion<FlexPotion>(),
        ModelDb.Potion<FyshOil>(),
        ModelDb.Potion<GigantificationPotion>(),
        ModelDb.Potion<MazalethsGift>(),
        ModelDb.Potion<PotionOfBinding>(),
        ModelDb.Potion<PowderedDemise>(),
        ModelDb.Potion<ShacklingPotion>(),
        ModelDb.Potion<StrengthPotion>(),
        ModelDb.Potion<VulnerablePotion>(),
        ModelDb.Potion<WeakPotion>(),
        ModelDb.Potion<CursedBottle>(),
        ModelDb.Potion<Fertilizer>(),
    ];

    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [];
}
