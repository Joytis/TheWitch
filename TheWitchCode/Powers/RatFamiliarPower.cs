using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Counter marking how many Rat familiars the player currently has. Each turn it rolls a token from its loot
/// table (Plague or Scavengers, equal weight). See <see cref="FamiliarPower" />.
/// </summary>
public sealed class RatFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<RatPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Plague>()
            .Add<Scavengers>();
}
