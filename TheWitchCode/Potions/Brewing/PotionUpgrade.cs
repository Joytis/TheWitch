using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>
/// "Upgrade a random potion": pick a random potion in the player's belt and replace it with a higher-rarity one
/// that shares its traits. Implemented on top of <see cref="BrewBook" /> by brewing the potion with itself — that
/// steps the rarity up one tier (capped at Rare), seeks the same traits, and excludes the input, so the result is
/// always a strictly different, higher-or-equal-rarity potion. Used by the Brew card.
/// </summary>
public static class PotionUpgrade
{
    /// <summary>
    /// Upgrade <paramref name="count" /> random belt potions. The belt is snapshotted and shuffled once up
    /// front, then the first <paramref name="count" /> distinct potions are upgraded — so the same potion is
    /// never picked twice and freshly created upgrades are never re-upgraded in the same call.
    /// </summary>
    public static async Task UpgradeRandomPotions(Player player, Rng rng, int count = 1)
    {
        var potions = player.Potions.ToList();
        potions.UnstableShuffle(rng);

        foreach (PotionModel target in potions.Take(count))
        {
            BrewResult upgrade = BrewBook.Brew(target, target, rng);
            if (!upgrade.Success)
            {
                continue; // nothing higher to upgrade into — leave the potion as-is.
            }

            await PotionCmd.Discard(target);
            await PotionCmd.TryToProcure(upgrade.Potion!.ToMutable(), player);
        }
    }
}
