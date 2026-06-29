using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Owl familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class OwlFamiliarPower : LootTableFamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<OwlPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Wisdom>()
            .Add<Knowledge>();
}
