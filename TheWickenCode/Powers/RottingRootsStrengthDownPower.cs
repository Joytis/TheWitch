using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Temporary Strength down applied by Rotting Roots — the target loses Strength for the rest of the turn and
/// regains it at turn end. Subclasses the base-game <see cref="TemporaryStrengthPower" /> (the DarkShackles
/// pattern); Title/Description/icon come from that base + <see cref="OriginModel" />, so this needs no mod
/// localization of its own.
/// </summary>
public sealed class RottingRootsStrengthDownPower : TemporaryStrengthPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<RottingRoots>();

    protected override bool IsPositive => false;
}
