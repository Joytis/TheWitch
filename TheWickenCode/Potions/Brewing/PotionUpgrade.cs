using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>
/// "Upgrade a random potion": pick a random potion in the player's belt and replace it with a higher-rarity one
/// that shares its traits. Implemented on top of <see cref="BrewBook" /> by brewing the potion with itself — that
/// steps the rarity up one tier (capped at Rare), seeks the same traits, and excludes the input, so the result is
/// always a strictly different, higher-or-equal-rarity potion. Used by the Brew card.
/// </summary>
public static class PotionUpgrade
{
    public static async Task UpgradeRandomPotion(Player player, Rng rng)
    {
        var potions = player.Potions.ToList();
        if (potions.Count == 0)
        {
            return;
        }

        PotionModel target = rng.NextItem(potions)!;
        BrewResult upgrade = BrewBook.Brew(target, target, rng);
        if (!upgrade.Success)
        {
            return; // nothing higher to upgrade into — leave the potion as-is.
        }

        await PotionCmd.Discard(target);
        await PotionCmd.TryToProcure(upgrade.Potion!.ToMutable(), player);
    }
}
