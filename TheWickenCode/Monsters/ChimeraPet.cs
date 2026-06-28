using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Cosmetic pet shown while a <see cref="Powers.ChimeraFamiliarPower" /> is active.
/// Placeholder sprite reuses the Chimera Familiar card art; swap for a clean sprite PNG later.
/// </summary>
public sealed class ChimeraPet : WickenPet
{
    public override string TexturePath => "chimera_familiar.png".CardImagePath();
}
