using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// While active, the player's Brambles retaliation deals <see cref="PowerModel.Amount" /> extra damage per
/// trigger. Read by <c>BramblesPower.BeforeDamageReceived</c>. Applied by the Vicious Barbs card.
/// </summary>
public sealed class ViciousBarbsPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
