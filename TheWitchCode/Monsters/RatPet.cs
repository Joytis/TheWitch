using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.RatFamiliarPower" /> is active.
/// Sprite ships under images/pets/; falls back to the card art if the PNG is missing.
/// </summary>
public sealed class RatPet : WitchPet
{
    public override string TexturePath => "rat_familiar.png".PetImagePath();
}
