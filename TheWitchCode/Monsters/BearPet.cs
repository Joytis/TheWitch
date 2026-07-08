using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.BearFamiliarPower" /> is active.
/// Placeholder sprite reuses the Bear Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class BearPet : WitchPet
{
    public override string TexturePath => "bear_familiar.png".CardImagePath();
}
