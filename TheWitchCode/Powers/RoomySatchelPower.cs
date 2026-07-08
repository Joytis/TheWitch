using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Tracks the extra potion slots granted by <see cref="Cards.RoomySatchel" /> for the current combat only.
/// The slots are real while you fight; at combat end this power reverts them (see <see cref="AfterCombatEnd" />),
/// discarding any potion that overflows the slots you keep. <see cref="Amount" /> is the number of bonus slots.
/// </summary>
public sealed class RoomySatchelPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player is { } player)
        {
            await Revert(player);
        }
    }

    /// <summary>
    /// Combat-end teardown removes powers via <c>RemoveAllPowersInternalExcept</c>, which skips
    /// <c>AfterRemoved</c> — so this only fires for a genuine mid-combat removal (buff strip), where the
    /// slots would otherwise leak into the rest of the run. The two revert paths are mutually exclusive:
    /// once removed, this power no longer receives <see cref="AfterCombatEnd" />.
    /// </summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (!oldOwner.IsDead && oldOwner.Player is { } player)
        {
            await Revert(player);
        }
    }

    private async Task Revert(Player player)
    {
        // Player.SetMaxPotionCountInternal migrates a doomed slot's potion to `_potionSlots.IndexOf(null)`
        // WITHOUT checking for -1 — on a full belt that indexes _potionSlots[-1] and throws. The base game
        // never shrinks the belt so the bug is latent there; we must discard the overflow ourselves (top
        // slot down) before shrinking, so migration always has an empty kept slot to land in.
        int kept = Math.Max(0, player.PotionSlots.Count - Amount);
        int potionCount = player.Potions.Count();
        for (int slot = player.PotionSlots.Count - 1; slot >= 0 && potionCount > kept; slot--)
        {
            if (player.PotionSlots[slot] is { } overflow)
            {
                await PotionCmd.Discard(overflow);
                potionCount--;
            }
        }

        await PlayerCmd.LoseMaxPotionCount(Amount, player);
    }
}
