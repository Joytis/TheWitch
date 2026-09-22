using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Embrace the Wilds downside (counter): the player draws fewer cards at the start of each turn — the price
/// paid for the burst of familiars the card summons. Persists for the combat; <see cref="Amount" /> is the
/// number of cards reduced.
/// </summary>
public sealed class EmbraceTheWildsPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player.Creature == Owner ? count - Amount : count;
}
