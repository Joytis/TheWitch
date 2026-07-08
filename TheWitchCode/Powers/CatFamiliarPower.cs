using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Counter marking how many Cat familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class CatFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<CatPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Ferocity>()
            .Add<Nimble>();
}
