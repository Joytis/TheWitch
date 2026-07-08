using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>How the brew chose its output potion. Useful for tuning / dev console output.</summary>
public enum BrewMatchKind
{
    /// <summary>No candidate found at all (shouldn't happen if any randomizable potion exists at the rarity).</summary>
    None,
    /// <summary>A potion sharing an input's orientation was found (primary path).</summary>
    Orientation,
    /// <summary>Last resort: any potion of the output rarity.</summary>
    RarityOnly,
}

/// <summary>Outcome of a brew. <see cref="Potion" /> is a canonical model; mutate + procure to actually grant it.</summary>
public readonly struct BrewResult
{
    public readonly PotionModel? Potion;
    public readonly PotionRarity Rarity;
    public readonly PotionOrientation Orientation;
    public readonly BrewMatchKind MatchKind;

    public BrewResult(PotionModel? potion, PotionRarity rarity, PotionOrientation orientation, BrewMatchKind matchKind)
    {
        Potion = potion;
        Rarity = rarity;
        Orientation = orientation;
        MatchKind = matchKind;
    }

    public bool Success => Potion != null;
}

/// <summary>
/// Brewing rules. Recipes are RULE-based, not keyed to specific potion ids, so the space stays additive:
///   * Inputs: exactly 2 potions (an "upgrade" passes the same potion twice).
///   * Output rarity: the higher input rarity, bumped up one tier (capped at Rare).
///   * Output orientation: a potion sharing an input's <see cref="PotionOrientation" /> (Offensive/Defensive/
///     Utility) is sought first; if nothing matches, fall back to any potion of the output rarity.
///   * Output potion: a real existing potion drawn from <see cref="PotionCatalog" /> (no bespoke fused potions).
/// We match on orientation ONLY — fine-grained effect tags were collapsed away because they made the candidate
/// pool too narrow (e.g. a Heal potion only ever upgraded into the single Uncommon healer).
/// </summary>
public static class BrewBook
{
    /// <summary>Combine two potions into a higher-rarity result. Returns <see cref="BrewResult.Success" /> = false if
    /// no candidate exists (e.g. no randomizable potion of the output rarity is registered).</summary>
    public static BrewResult Brew(PotionModel a, PotionModel b, Rng rng)
    {
        PotionRarity outRarity = NextRarity(a.Rarity, b.Rarity);

        HashSet<PotionOrientation> allowed = new()
        {
            PotionTraits.OrientationOf(a),
            PotionTraits.OrientationOf(b),
        };
        allowed.Remove(PotionOrientation.Neutral);

        // Pool of legitimate, distinct-from-inputs candidates at the target rarity.
        List<PotionModel> pool = PotionCatalog
            .Query(rarity: outRarity, randomizableOnly: true)
            .Where(p => !IsInput(p, a, b))
            .ToList();

        // 1. Primary: a potion sharing an input's orientation.
        List<PotionModel> candidates = allowed.Count > 0
            ? pool.Where(p => allowed.Contains(PotionTraits.OrientationOf(p))).ToList()
            : new List<PotionModel>();
        BrewMatchKind kind = BrewMatchKind.Orientation;

        // 2. Fallback: anything of the right rarity.
        if (candidates.Count == 0)
        {
            candidates = pool;
            kind = BrewMatchKind.RarityOnly;
        }

        // 3. The exact step-up rarity has nothing but the inputs: broaden UP to ANY higher-or-equal rarity, so a
        //    brew still produces a real potion rather than failing. Never broadens downward.
        if (candidates.Count == 0)
        {
            candidates = PotionCatalog.Query(randomizableOnly: true)
                .Where(p => Rank(p.Rarity) >= Rank(outRarity) && !IsInput(p, a, b))
                .ToList();
            kind = BrewMatchKind.RarityOnly;
        }

        PotionModel? pick = PotionCatalog.Random(candidates, rng);
        PotionOrientation outOrientation = pick != null ? PotionTraits.OrientationOf(pick) : PotionOrientation.Neutral;
        return new BrewResult(pick, outRarity, outOrientation, pick == null ? BrewMatchKind.None : kind);
    }

    /// <summary>The higher of two rarities bumped one tier, capped at Rare. Always yields at least Uncommon.</summary>
    public static PotionRarity NextRarity(PotionRarity a, PotionRarity b)
    {
        int top = Math.Max(Rank(a), Rank(b));
        int next = Math.Min(top + 1, 3);
        return next switch
        {
            >= 3 => PotionRarity.Rare,
            _ => PotionRarity.Uncommon,
        };
    }

    // Common=1, Uncommon=2, Rare=3. None/Event/Token treated as Common-tier so they still brew sensibly.
    private static int Rank(PotionRarity rarity) => rarity switch
    {
        PotionRarity.Uncommon => 2,
        PotionRarity.Rare => 3,
        _ => 1,
    };

    private static bool IsInput(PotionModel candidate, PotionModel a, PotionModel b)
    {
        Type t = candidate.GetType();
        return t == a.GetType() || t == b.GetType();
    }
}
