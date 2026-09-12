using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Counter marking how many Moth familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class MothFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<MothPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<StarDust>()
            .Add<Gossamer>();
}
