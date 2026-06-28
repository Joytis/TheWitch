using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.SlothFamiliarPower" /> is active.
/// Placeholder sprite reuses the Sloth Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class SlothPet : WickenPet
{
    public override string TexturePath => "sloth_familiar.png".CardImagePath();
}
