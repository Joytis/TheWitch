using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Temporary Strength loss applied to enemies by <see cref="NeedleWhip" /> — they lose Strength this turn
/// equal to the Brambles spent. Mirrors the base game's <c>DarkShacklesPower</c> (a
/// <see cref="TemporaryStrengthPower" /> with <c>IsPositive = false</c>); uses the shared temporary-strength
/// localization, so it needs no powers.json entry of its own.
/// </summary>
public sealed class NeedleWhipStrengthPower : TemporaryStrengthPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<NeedleWhip>();

    protected override bool IsPositive => false;
}
