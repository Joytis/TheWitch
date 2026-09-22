using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.MothFamiliarPower" /> is active.
/// Sprite ships under images/pets/; falls back to the card art if the PNG is missing.
/// </summary>
public sealed class MothPet : WitchPet
{
    public override string PetFileName => "moth_familiar";
}
