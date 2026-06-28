using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Bear familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class BearFamiliarPower : LootTableFamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<BearPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Hibernate>()
            .Add<Mutilate>();
}
