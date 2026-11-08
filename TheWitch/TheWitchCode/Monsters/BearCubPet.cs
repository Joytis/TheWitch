namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Cosmetic pet shown per stack of <see cref="Powers.BearCubPower" />. Placeholder visual: the Bear pet's sprite at a
/// fraction of the size (see data/pets/bear_cub.tres), parked at the bear_cub_pos marker next to the Bear.
/// </summary>
public sealed class BearCubPet : WitchPet
{
    public override string PetFileName => "bear_cub";
}
