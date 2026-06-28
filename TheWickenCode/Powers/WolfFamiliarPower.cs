using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Wolf familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class WolfFamiliarPower : FamiliarPower<Gnash>
{
    protected override WickenPet Pet => ModelDb.Monster<WolfPet>();
}
