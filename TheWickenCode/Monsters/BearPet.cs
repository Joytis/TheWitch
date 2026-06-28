using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.BearFamiliarPower" /> is active.
/// Placeholder sprite reuses the Bear Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class BearPet : WickenPet
{
    public override string TexturePath => "bear_familiar.png".CardImagePath();
}
