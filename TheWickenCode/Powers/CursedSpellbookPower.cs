using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Cursed Spellbook engine: each turn you draw one fewer card but gain <see cref="PowerModel.Amount" /> extra
/// Energy. Single-stack — replaying the book refreshes rather than compounding the draw penalty (an upgraded
/// copy just raises the Energy). Draw hook mirrors <see cref="EmbraceTheWildsPower.ModifyHandDraw" />; energy
/// hook mirrors the base-game <c>NoEnergyGainPower.ModifyEnergyGain</c>.
/// </summary>
public sealed class CursedSpellbookPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyEnergyGain(Player player, decimal amount) =>
        player == Owner.Player ? amount + Amount : amount;

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player.Creature == Owner ? count - 1 : count;
}
