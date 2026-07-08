using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Registry of every familiar token-card (all <see cref="WitchFamiliarCard" /> subtypes). Backs the
/// "random familiar card" effects — Woe and Whimsy, Embrace the Wilds.
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
    /// (Owl, Cat, Rat, Bear, Crow, Wolf). Backs Embrace the Wilds.
    /// </summary>
    public static IReadOnlyList<CardModel> AllSummonCanonical =>
        ModelDb.AllCards.OfType<IFamiliarSummon>().Cast<CardModel>().ToList();

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
    /// random familiar type rolled from <paramref name="rng" /> (upgraded if <paramref name="isUpgraded" />).
    /// </summary>
    public static List<CardModel> CreateRandom(Player owner, int amount, ICombatState combatState, Rng rng, bool isUpgraded)
    {
        ArgumentNullException.ThrowIfNull(combatState, "combatState");
        IReadOnlyList<WitchFamiliarCard> pool = AllCanonical;
        var result = new List<CardModel>(amount);
        for (int i = 0; i < amount; i++)
        {
            WitchFamiliarCard canonical = rng.NextItem(pool)!;
            CardModel card = combatState.CreateCard(canonical, owner);
            if (isUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            result.Add(card);
        }
        return result;
    }
}
