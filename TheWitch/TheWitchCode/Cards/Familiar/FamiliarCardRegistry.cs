using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Registry of every familiar token-card (all <see cref="WitchFamiliarCard" /> subtypes). Backs the
/// "random familiar card" effects — Animal Handling.
/// Canonical models come from <see cref="ModelDb.AllCards" />; the <c>WitchFamiliarCardPool</c> is
/// shared (<c>IsShared =&gt; true</c>), so all familiar cards are present there.
/// </summary>
public static class FamiliarCardRegistry
{
    /// <summary>Canonical models for every registered familiar token-card.</summary>
    public static IReadOnlyList<WitchFamiliarCard> AllCanonical =>
        ModelDb.AllCards.OfType<WitchFamiliarCard>().ToList();

    /// <summary>
    /// Canonical models for every familiar *summon* card — the <see cref="IFamiliarSummon" /> Power cards
    /// (Owl, Cat, Rat, Bear, Crow, Wolf, …). Backs Buddy in a Bottle.
    /// Filtered by the run's player count (like <c>CardFactory</c>), so MP-only summons such as
    /// Plague Tide never roll in singleplayer.
    /// </summary>
    public static IReadOnlyList<CardModel> AllSummonCanonical(IRunState runState)
    {
        bool multiplayer = runState.Players.Count > 1;
        return ModelDb.AllCards
            .OfType<IFamiliarSummon>()
            .Cast<CardModel>()
            .Where(c => multiplayer
                ? c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
            .ToList();
    }

    /// <summary>
    /// Create <paramref name="amount" /> real familiar cards of type <typeparamref name="T" /> for
    /// <paramref name="owner" /> (upgraded if <paramref name="isUpgraded" />).
    /// </summary>
    public static IEnumerable<T> CreateFamiliarCards<T>(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
        where T : WitchFamiliarCard
    {
        ArgumentNullException.ThrowIfNull(combatState, "combatState");
        List<T> list = new List<T>();
        for (int i = 0; i < amount; i++)
        {
            var newCard = combatState.CreateCard<T>(owner);
            list.Add(newCard);
            if (isUpgraded)
            {
                CardCmd.Upgrade(newCard);
            }
        }
        return list;
    }

    /// <summary>
    /// Create <paramref name="amount" /> real familiar cards for <paramref name="owner" />, each a
    /// random familiar type rolled from <paramref name="rng" />, weighted by
    /// <see cref="WitchFamiliarCard.RandomRollWeight" /> (upgraded if <paramref name="isUpgraded" />).
    /// </summary>
    public static List<CardModel> CreateRandom(Player owner, int amount, ICombatState combatState, Rng rng, bool isUpgraded)
    {
        ArgumentNullException.ThrowIfNull(combatState, "combatState");
        IReadOnlyList<WitchFamiliarCard> pool = AllCanonical;
        int totalWeight = pool.Sum(c => Math.Max(0, c.RandomRollWeight));
        var result = new List<CardModel>(amount);
        for (int i = 0; i < amount; i++)
        {
            WitchFamiliarCard canonical = PickWeighted(pool, totalWeight, rng);
            CardModel card = combatState.CreateCard(canonical, owner);
            if (isUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            result.Add(card);
        }
        return result;
    }

    private static WitchFamiliarCard PickWeighted(IReadOnlyList<WitchFamiliarCard> pool, int totalWeight, Rng rng)
    {
        int roll = rng.NextInt(totalWeight);
        foreach (WitchFamiliarCard card in pool)
        {
            roll -= Math.Max(0, card.RandomRollWeight);
            if (roll < 0)
            {
                return card;
            }
        }
        throw new InvalidOperationException("Familiar random-roll pool is empty or has no positive weights.");
    }
}
