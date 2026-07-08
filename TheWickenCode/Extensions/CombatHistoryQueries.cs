using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Powers;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Extensions;

/// <summary>
/// Read-only counters derived from the game's <see cref="CombatHistory" /> (every combat event is logged
/// with an <c>Actor</c> and a <c>HappenedThisTurn</c> check). This avoids needing hidden "tracker" powers
/// for "X this turn / this combat" card scaling — just query the log.
///
/// Pass the playing card's <c>Owner.Creature</c> as <paramref name="player" /> and its <c>CombatState</c>
/// as <paramref name="state" />. All methods return 0 when there is no live combat.
/// </summary>
public static class CombatHistoryQueries
{
    private static CombatHistory? History => CombatManager.Instance?.History;

    /// <summary>Potions this player has used so far during the current turn.</summary>
    public static int PotionsUsedThisTurn(Creature player, ICombatState? state) =>
        History?.Entries.OfType<PotionUsedEntry>()
            .Count(e => e.Actor == player && e.HappenedThisTurn(state)) ?? 0;

    /// <summary>
    /// Total Brambles this player has GAINED during the current turn (positive applications only — losing
    /// brambles to retaliation does not subtract). Used by Bramble Shield ("per bramble created this turn").
    /// </summary>
    public static int BramblesCreatedThisTurn(Creature player, ICombatState? state) =>
        (int)(History?.Entries.OfType<PowerReceivedEntry>()
            .Where(e => e.Actor == player && e.Power is BramblesPower && e.Amount > 0m && e.HappenedThisTurn(state))
            .Sum(e => e.Amount) ?? 0m);

    /// <summary>
    /// How many cards of type <typeparamref name="T" /> this player has finished playing this combat. Counts
    /// completed plays only, so reading it during a card's own <c>OnPlay</c> excludes the current play
    /// (e.g. Gnash escalating "+5 for each Gnash played this combat" → 0, 1, 2, … on successive plays).
    /// </summary>
    public static int CardsPlayedThisCombat<T>(Creature player) where T : CardModel =>
        History?.CardPlaysFinished.Count(e => e.CardPlay.Card is T && e.CardPlay.Card.Owner.Creature == player) ?? 0;

    /// <summary>
    /// How many Rat familiar token cards (anything marked <see cref="IRatCard" /> — Scavenge, Plague) this
    /// player has finished playing this combat. Excludes the in-progress play, so a card read during its own
    /// <c>OnPlay</c> counts only the rats before it.
    /// </summary>
    public static int RatCardsPlayedThisCombat(Creature player) =>
        History?.CardPlaysFinished.Count(e => e.CardPlay.Card is IRatCard && e.CardPlay.Card.Owner.Creature == player) ?? 0;
}
