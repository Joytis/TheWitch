using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Porcupine familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class PorcupineFamiliarPower : LootTableFamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<PorcupinePet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Quills>()
            .Add<Bristle>();
}
