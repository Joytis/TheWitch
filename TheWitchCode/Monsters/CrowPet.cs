using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.CrowFamiliarPower" /> is active.
/// Placeholder sprite reuses the Crow Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class CrowPet : WitchPet
{
    public override string TexturePath => "crow_familiar.png".CardImagePath();
}
