using MegaCrit.Sts2.Core.Entities.Relics;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Separatory Funnel: whenever an effect would create a RANDOM potion, choose 1 of <see cref="Choices" />
/// random potions from that pool instead. Passive — <see cref="Potions.Brewing.PotionCatalog.Pick" /> is the
/// chokepoint every in-combat potion-creation effect routes through; it checks for this relic, rolls the
/// options and opens the potion-select overlay.
/// </summary>
public sealed class SeparatoryFunnel : WitchRelic
{
    public const int Choices = 3;

    public override RelicRarity Rarity => RelicRarity.Rare;
}
