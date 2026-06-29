using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Counter marking how many Rat familiars the player currently has. Each turn it rolls a token from its loot
/// table (Plague or Nibble, equal weight). See <see cref="FamiliarPower" />.
/// </summary>
public sealed class RatFamiliarPower : LootTableFamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<RatPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Plague>()
            .Add<Nibble>();
}
