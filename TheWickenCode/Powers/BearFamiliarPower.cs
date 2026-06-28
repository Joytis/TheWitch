using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Bear familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class BearFamiliarPower : FamiliarPower
{
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        rng.NextFloat() < 0.5f
            ? WickenCard.CreateFamiliarCards<Hibernate>(owner, 1, combat, false).First()
            : WickenCard.CreateFamiliarCards<Mutilate>(owner, 1, combat, false).First();
}
