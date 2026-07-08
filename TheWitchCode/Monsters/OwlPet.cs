using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet spawned when <see cref="Cards.OwlFamiliar" /> is played.
/// Placeholder sprite reuses the Owl Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class OwlPet : WitchPet
{
    public override string TexturePath => "owl_familiar.png".CardImagePath();
}
