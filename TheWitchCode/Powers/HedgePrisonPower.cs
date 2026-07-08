using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// While active, the player's Brambles never decrement when they retaliate — they are permanent for the
/// rest of combat. Read by <c>BramblesPower.BeforeDamageReceived</c>. Applied by the Hedge Prison card.
/// </summary>
public sealed class HedgePrisonPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
