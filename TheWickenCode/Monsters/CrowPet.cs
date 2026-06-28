using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.CrowFamiliarPower" /> is active.
/// Placeholder sprite reuses the Crow Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class CrowPet : WickenPet
{
    public override string TexturePath => "crow_familiar.png".CardImagePath();
}
