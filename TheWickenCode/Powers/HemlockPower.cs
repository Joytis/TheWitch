using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Hemlock (passive marker): while active, your <see cref="BramblesPower" /> retaliation also applies
/// 1 <see cref="HexPower" /> to whatever it damages. The effect itself lives in
/// <c>BramblesPower.BeforeDamageReceived</c>, which checks for this power on the bramble owner; this class
/// is just the toggle. Single stack — playing Hemlock again doesn't stack the Hex.
/// </summary>
public sealed class HemlockPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
