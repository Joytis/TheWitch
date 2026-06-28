using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Crow familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class CrowFamiliarPower : LootTableFamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<CrowPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<ScoutWeakness>(2)
            .Add<ClawEyes>(2)
            .Add<Shiny>(1);
}
