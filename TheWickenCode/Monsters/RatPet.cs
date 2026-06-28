using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.RatFamiliarPower" /> is active.
/// Placeholder sprite reuses the Rat Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class RatPet : WickenPet
{
    public override string TexturePath => "rat_familiar.png".CardImagePath();
}
