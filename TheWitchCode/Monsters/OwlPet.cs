using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet spawned when <see cref="Cards.OwlFamiliar" /> is played.
/// Sprite ships under images/pets/; falls back to the card art if the PNG is missing.
/// </summary>
public sealed class OwlPet : WitchPet
{
    public override string TexturePath => "owl_familiar.png".PetImagePath();
}
