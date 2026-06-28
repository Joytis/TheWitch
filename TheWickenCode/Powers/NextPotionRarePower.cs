using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Gather Herbs buff (counter): the next potion the Wicken <em>creates</em> comes out Rare. One stack is consumed
/// per forced creation. Honored only by the Wicken's own rarity-rolling potion creators (Something Wicked, Toil
/// and Trouble) via <see cref="MakeNextRare" /> — deliberately NOT a global procurement hook, so relic/event
/// potions are untouched. Fixed-output creators (Cackle/Brew = specific potions) don't call it, so the buff
/// waits for a potion it can actually upgrade. Takes precedence over <see cref="NextPotionUpgradedPower" />.
/// </summary>
public sealed class NextPotionRarePower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// If <paramref name="player" /> has this buff, consumes one stack and returns <see cref="PotionRarity.Rare" />.
    /// Otherwise returns <paramref name="rarity" /> unchanged. Call when rolling a created potion's rarity.
    /// </summary>
    public static async Task<PotionRarity> MakeNextRare(Player player, PotionRarity rarity)
    {
        NextPotionRarePower? power = player.Creature.GetPower<NextPotionRarePower>();
        if (power == null)
        {
            return rarity;
        }
        await PowerCmd.Decrement(power);
        return PotionRarity.Rare;
    }
}
