using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Gather Herbs buff (counter): the next potion the Wicken <em>creates</em> is rolled one rarity higher.
/// One stack is consumed per upgraded creation. Honored only by the Wicken's own rarity-rolling potion
/// creators (the Brew trio via <c>OrientationBrewCard</c>) through <see cref="UpgradeRarity" /> — deliberately
/// NOT a global procurement hook, so relic/event potions are untouched. Fixed-output creators (Witchcraft =
/// specific potion) don't call it, so the buff waits for a potion it can actually upgrade.
/// </summary>
public sealed class NextPotionUpgradedPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// If <paramref name="player" /> has this buff, consumes one stack and returns <paramref name="rarity" />
    /// bumped one tier (capped at Rare). Otherwise returns it unchanged. Call when rolling a created potion's rarity.
    /// </summary>
    public static async Task<PotionRarity> UpgradeRarity(Player player, PotionRarity rarity)
    {
        NextPotionUpgradedPower? power = player.Creature.GetPower<NextPotionUpgradedPower>();
        if (power == null)
        {
            return rarity;
        }
        await PowerCmd.Decrement(power);
        return rarity switch
        {
            PotionRarity.Common => PotionRarity.Uncommon,
            PotionRarity.Uncommon => PotionRarity.Rare,
            _ => rarity
        };
    }
}
