using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>
/// "Distill a random potion": pick a random potion in the player's belt and replace it with a random RARE
/// potion of the same orientation. Non-Rare potions are prioritized as inputs (a Rare is only re-rolled when
/// nothing lesser is left); the input itself is excluded from the roll, and healing potions
/// (<see cref="PotionTraits.IsHealing" />) never come out. If the orientation has no eligible Rare, any
/// eligible Rare is used instead (mirrors the BrewBook same-orientation fallback). Used by the Distill card.
/// </summary>
public static class PotionUpgrade
{
    /// <summary>
    /// Distill <paramref name="count" /> random belt potions to Rare. The belt is snapshotted and shuffled once
    /// up front (then non-Rares sorted first), so the same potion is never picked twice and freshly created
    /// results are never re-distilled in the same call.
    /// </summary>
    public static async Task UpgradeRandomPotions(Player player, Rng rng, int count = 1)
    {
        var potions = player.Potions.ToList();
        potions.UnstableShuffle(rng);
        var ordered = potions.OrderBy(p => p.Rarity == PotionRarity.Rare ? 1 : 0).ToList();

        foreach (PotionModel target in ordered.Take(count))
        {
            PotionOrientation orientation = PotionTraits.OrientationOf(target);
            PotionModel? result = PotionCatalog.Random(
                PotionCatalog.Query(orientation: orientation, rarity: PotionRarity.Rare)
                    .Where(p => p.GetType() != target.GetType()),
                rng);
            result ??= PotionCatalog.Random(
                PotionCatalog.Query(rarity: PotionRarity.Rare)
                    .Where(p => p.GetType() != target.GetType()),
                rng);
            if (result == null)
            {
                continue; // nothing eligible to distill into — leave the potion as-is.
            }

            await PotionCmd.Discard(target);
            await PotionCmd.TryToProcure(result.ToMutable(), player);
        }
    }
}
