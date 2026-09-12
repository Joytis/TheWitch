using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Wild Growth: at the start of each of the next <see cref="PowerModel.Amount" /> turns, gain
/// <c>Energy</c> and <c>Brambles</c> (fixed values in <see cref="DynamicVars" />; the card upgrade adds turns, so
/// stacks from normal and upgraded plays are interchangeable). Mirrors the base-game LightningRodPower: fires in AfterEnergyReset so the gain is
/// not wiped by the reset, and decrements per turn.
/// </summary>
public sealed class WildGrowthPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new PowerVar<BramblesPower>(5m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, player);
        await PowerCmd.Apply<BramblesPower>(
            new ThrowingPlayerChoiceContext(), Owner, DynamicVars.Brambles().BaseValue, Owner, null);
        await PowerCmd.Decrement(this);
    }
}
