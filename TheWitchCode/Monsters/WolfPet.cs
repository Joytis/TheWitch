using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.WolfFamiliarPower" /> is active.
/// Placeholder sprite reuses the Wolf Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class WolfPet : WitchPet
{
    public override string TexturePath => "wolf_familiar.png".CardImagePath();
}
