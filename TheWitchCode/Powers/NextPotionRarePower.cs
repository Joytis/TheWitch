using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Gather Herbs buff (counter): the next potion the Witch <em>creates</em> comes out Rare. One stack is consumed
/// per forced creation. Honored only by the Witch's own rarity-rolling potion creators (the Brew trio via
/// <c>OrientationBrewCard</c>) through <see cref="TryConsume" /> — deliberately NOT a global procurement hook,
/// so relic/event potions are untouched. Fixed-output creators (Witchcraft = specific potion) don't call it, so the
/// buff waits for a potion it can actually upgrade.
/// </summary>
public sealed class NextPotionRarePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// If <paramref name="player" /> has this buff, consumes one stack and returns true — the caller
    /// must then produce a Rare potion. Only call when a Rare result is actually possible (the brew
    /// cards check their loot table for Rare entries first, so the buff never fizzles on a table
    /// that can't honor it).
    /// </summary>
    public static async Task<bool> TryConsume(Player player)
    {
        NextPotionRarePower? power = player.Creature.GetPower<NextPotionRarePower>();
        if (power == null)
        {
            return false;
        }
        await PowerCmd.Decrement(power);
        return true;
    }
}
