using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

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
}
