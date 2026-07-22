using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Extensions;

// Typed accessors for the Witch's own power vars, mirroring the base game's DynamicVarSet
// properties (DynamicVars.Vulnerable, .Weak, ...). Methods, not properties — DynamicVarSet
// is a game class and C# has no extension properties.
public static class DynamicVarSetExtensions
{
    public static PowerVar<BramblesPower> Brambles(this DynamicVarSet set) => (PowerVar<BramblesPower>)set[nameof(BramblesPower)];

    public static PowerVar<HexPower> Hex(this DynamicVarSet set) => (PowerVar<HexPower>)set[nameof(HexPower)];
}
