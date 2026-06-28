using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.PorcupineFamiliarPower" /> is active.
/// Placeholder sprite reuses the Porcupine Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class PorcupinePet : WickenPet
{
    public override string TexturePath => "porcupine_familiar.png".CardImagePath();
}
