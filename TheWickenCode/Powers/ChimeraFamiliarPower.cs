using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>Counter marking how many Chimera familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class ChimeraFamiliarPower : FamiliarPower
{
    protected override WickenPet Pet => ModelDb.Monster<ChimeraPet>();

    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        FamiliarCardRegistry.CreateRandom(owner, 1, combat, rng, false).First();
}
