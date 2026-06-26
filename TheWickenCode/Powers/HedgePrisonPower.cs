using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// While active, the player's Brambles never decrement when they retaliate — they are permanent for the
/// rest of combat. Read by <c>BramblesPower.BeforeDamageReceived</c>. Applied by the Hedge Prison card.
/// </summary>
public sealed class HedgePrisonPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
