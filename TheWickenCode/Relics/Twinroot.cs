using MegaCrit.Sts2.Core.Entities.Relics;

namespace TheWicken.TheWickenCode.Relics;

/// <summary>
/// Twinroot: whenever you upgrade a card, add a copy of it to your deck — in and out of combat. The duplication is
/// driven by <see cref="Patches.CardUpgradeTracker" />, which checks for this relic at the upgrade chokepoint, so
/// this class is just the registered marker + rarity.
/// </summary>
public sealed class Twinroot : WickenRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}
