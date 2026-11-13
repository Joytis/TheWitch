using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Throttles Moth Familiar in the merchant's Power slot. Moth is the Witch's only Common Power, and every
/// shop rolls one Power slot at shop odds (Common 54%, 58.5% on Scarcity), so unpatched it filled that slot
/// in more than half of all shops. Moth stays Common (card rewards unaffected); here, when the Power slot is
/// rolled and Moth is a candidate, it is kept only with probability
/// <c>TargetShopChance / (current Common share of a shop roll)</c> and otherwise filtered out of the options,
/// so a Common roll bumps up to an Uncommon Power via the base game's <c>GetNextAllowedRarity</c>. Net: Moth
/// appears in ~<see cref="TargetShopChance"/> of shops at any ascension / pity offset.
/// Uses the seeded <c>PlayerRng.Shops</c> stream, so it stays deterministic in multiplayer.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant),
    [typeof(Player), typeof(IEnumerable<CardModel>), typeof(IEnumerable<CardModel>), typeof(CardType)])]
public static class MothShopThrottlePatch
{
    /// <summary>Fraction of shops whose Power slot should offer Moth Familiar.</summary>
    private const float TargetShopChance = 0.10f;

    /// <summary>Base-game shop Uncommon odds (private <c>CardRarityOdds.GetBaseOdds</c>, a flat 0.37).</summary>
    private const float ShopUncommonOdds = 0.37f;

    private static void Prefix(Player player, ref IEnumerable<CardModel> options, CardType type)
    {
        if (type != CardType.Power)
        {
            return;
        }

        CardModel[] optionsArr = options.ToArray();
        options = optionsArr;
        if (!optionsArr.Any(c => c is MothFamiliar))
        {
            return;
        }

        if (player.PlayerRng.Shops.NextFloat() >= KeepChance(player))
        {
            options = optionsArr.Where(c => c is not MothFamiliar).ToArray();
        }
    }

    /// <summary>
    /// Mirrors <c>CardRarityOdds.RollWithoutChangingFutureOdds(Shop)</c>: the Common share of a shop roll is
    /// <c>1 - (ShopRareOdds + pity offset) - ShopUncommonOdds</c>. The pity offset starts at -0.05 and grows
    /// with every non-rare card reward, so the Common share drifts (~41-59%) over a run; dividing by the live
    /// value keeps Moth's shop rate at <see cref="TargetShopChance"/> throughout.
    /// </summary>
    private static float KeepChance(Player player)
    {
        float rareShare = CardRarityOdds.ShopRareOdds + player.PlayerOdds.CardRarity.CurrentValue;
        float commonShare = 1f - rareShare - ShopUncommonOdds;
        return commonShare <= 0f ? 1f : Math.Clamp(TargetShopChance / commonShare, 0f, 1f);
    }
}
