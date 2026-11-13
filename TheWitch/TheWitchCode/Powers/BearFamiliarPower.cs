using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Counter marking how many Bear familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class BearFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<BearPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Hibernate>()
            .Add<Mutilate>();
}
