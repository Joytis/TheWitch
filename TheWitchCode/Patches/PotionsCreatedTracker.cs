using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Per-combat counter of potions procured ("created"). The game logs no "potion procured" history entry
/// (see <c>CombatHistoryQueries</c> for the history-backed counters), so this Harmony-patches the single
/// procurement chokepoint — <see cref="PotionCmd.TryToProcure(PotionModel, Player, int)" /> (the generic
/// overload routes through it). Counts are keyed by the live <see cref="ICombatState" /> via a weak table,
/// so they reset every combat automatically and are collected when the combat ends.
///
/// Read by <c>BottleBarrage</c>. Counts procure *calls* (a belt-full failure still counts) — an acceptable
/// approximation of "potions created this combat". Combat-keyed, so in multiplayer it is the team total.
/// </summary>
[HarmonyPatch(typeof(PotionCmd), nameof(PotionCmd.TryToProcure), new[] { typeof(PotionModel), typeof(Player), typeof(int) })]
public static class PotionsCreatedTracker
{
    private static readonly ConditionalWeakTable<ICombatState, StrongBox<int>> Counts = new();

    private static void Prefix(PotionModel potion, Player player)
    {
        ICombatState? combat = player?.Creature?.CombatState;
        if (combat == null)
        {
            return;
        }
        Counts.GetValue(combat, _ => new StrongBox<int>(0)).Value++;

        // Potion-creation signature: brew puff on the Witch (gated so other characters keep vanilla feel).
        // Energy Potions puff yellow (Hasty Brew's "fast mana" read); everything else keeps the house green.
        if (player!.Character is Character.Witch)
        {
            Color? tint = potion is EnergyPotion ? new Color("ffd75e") : null;
            WitchFx.BrewPuff(player.Creature!, tint);
        }
    }

    /// <summary>Potions procured so far in <paramref name="combat" /> (0 if none / no live combat).</summary>
    public static int CountFor(ICombatState? combat) =>
        combat != null && Counts.TryGetValue(combat, out StrongBox<int>? box) ? box.Value : 0;
}
