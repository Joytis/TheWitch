using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet spawned when <see cref="Cards.CatFamiliar" /> is played.
/// Placeholder sprite reuses the Cat Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class CatPet : WickenPet
{
    public override string TexturePath => "cat_familiar.png".CardImagePath();
}
