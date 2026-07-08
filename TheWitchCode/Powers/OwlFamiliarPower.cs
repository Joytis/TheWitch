using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Counter marking how many Owl familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class OwlFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<OwlPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Wisdom>()
            .Add<Knowledge>();
}
