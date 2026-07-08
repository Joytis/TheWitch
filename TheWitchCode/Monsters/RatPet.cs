using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.RatFamiliarPower" /> is active.
/// Placeholder sprite reuses the Rat Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class RatPet : WitchPet
{
    public override string TexturePath => "rat_familiar.png".CardImagePath();
}
