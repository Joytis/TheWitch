using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Tracks the extra potion slots granted by <see cref="Cards.RoomySatchel" /> for the current combat only.
/// The slots are real while you fight; at combat end this power reverts them (see <see cref="AfterCombatEnd" />),
/// discarding any potion that overflows the slots you keep. <see cref="Amount" /> is the number of bonus slots.
/// </summary>
public sealed class RoomySatchelPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player is { } player)
        {
            await PlayerCmd.LoseMaxPotionCount(Amount, player);
        }
    }
}
