using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.CrowFamiliarPower" /> is active.
/// Sprite ships under images/pets/; falls back to the card art if the PNG is missing.
/// </summary>
public sealed class CrowPet : WitchPet
{
    public override string PetFileName => "crow_familiar";
}
