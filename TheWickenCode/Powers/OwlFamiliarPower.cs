using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Owl familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class OwlFamiliarPower : FamiliarPower<Wisdom>
{
    protected override WickenPet Pet => ModelDb.Monster<OwlPet>();
}
