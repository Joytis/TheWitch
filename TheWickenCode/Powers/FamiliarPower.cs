using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WickenCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Payoff: at the START of the owner's turn, the familiar adds one random card it can produce to your hand
/// PER STACK (see <see cref="CreateTurnStartCard" /> — each stack rolls independently). This replaces the old
/// "shuffle N token cards into your deck on summon" — ongoing, immediate value, and sacrificing the power
/// (<c>PowerCmd.Decrement</c> to zero, which auto-removes the <see cref="PowerStackType.Counter" />) actually
/// costs you those recurring cards.
/// </summary>
public abstract class FamiliarPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// When true, the token cards this familiar produces come out Upgraded. Set by <c>WickenCard.GainFamiliar</c>
    /// when an upgraded familiar summon card is played — sticky once any upgraded summon has applied this power.
    /// </summary>
    public bool GrantsUpgradedCards { get; set; }

    /// <summary>Create one real combat card this familiar can produce (Upgraded per <see cref="GrantsUpgradedCards" />), chosen at random if it has several.</summary>
    protected abstract CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng);

    /// <summary>
    /// Canonical cosmetic pet shown at the player's feet while this familiar has stacks. Spawned when the
    /// familiar is first summoned and removed when its last stack is lost (see <see cref="AfterApplied" /> /
    /// <see cref="AfterRemoved" />). Every familiar declares one.
    /// </summary>
    protected abstract WickenPet Pet { get; }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        Rng rng = player.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < Amount; i++)
        {
            CardModel card = CreateTurnStartCard(player, combat, rng);
            // Use the "generated" path (not a plain Add) so the card counts as created — records combat
            // history and fires AfterCardGeneratedForCombat, which card-creation payoffs like Cloak of Moonlight listen to.
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }

    /// <summary>Spawn the cosmetic pet when the familiar is summoned (idempotent: one pet per type, per combat).</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is not { } player || player.PlayerCombatState is null || Owner.CombatState is not { } combat)
        {
            return;
        }

        if (FindPet(player) != null)
        {
            return;
        }

        Creature pet = combat.CreateCreature((MonsterModel)Pet.ToMutable(), Owner.Side, null);
        await PlayerCmd.AddPet(pet, player);
    }

    /// <summary>Despawn the cosmetic pet when the familiar's last stack is lost.</summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player is not { PlayerCombatState: not null } player)
        {
            return;
        }

        if (FindPet(player) is { } pet)
        {
            await CreatureCmd.Kill(pet, force: true);
        }
    }

    private Creature? FindPet(Player player) =>
        player.PlayerCombatState!.Pets.FirstOrDefault(p => p.Monster?.GetType() == Pet.GetType());
}

/// <summary>
/// Convenience base for the common case: a familiar that always produces the same single token card
/// <typeparamref name="TCard" />. Single-type familiar powers just declare <c>: FamiliarPower&lt;TCard&gt;</c>.
/// Familiars that produce several different cards (Bear, Chimera) extend <see cref="FamiliarPower" /> directly
/// and override <see cref="CreateTurnStartCard" />.
/// </summary>
public abstract class FamiliarPower<TCard> : FamiliarPower where TCard : WickenFamiliarCard
{
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        WickenCard.CreateFamiliarCards<TCard>(owner, 1, combat, GrantsUpgradedCards).First();
}

/// <summary>
/// A weighted "loot table" of familiar token-cards. Each <c>Add&lt;TCard&gt;(weight)</c> registers a card type
/// the familiar can produce; <see cref="Roll" /> picks one with probability proportional to its weight
/// (default weight 1 = uniform). Built once per power via <see cref="LootTableFamiliarPower.BuildLootTable" />.
/// </summary>
public sealed class FamiliarLootTable
{
    private readonly List<(int Weight, Func<Player, ICombatState, bool, CardModel> Create)> _entries = [];
    private int _totalWeight;

    /// <summary>Register a card type the familiar can roll, with optional relative <paramref name="weight" /> (default 1).</summary>
    public FamiliarLootTable Add<TCard>(int weight = 1) where TCard : WickenFamiliarCard
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Loot-table weight must be positive.");
        }

        _entries.Add((weight, (owner, combat, upgraded) => WickenCard.CreateFamiliarCards<TCard>(owner, 1, combat, upgraded).First()));
        _totalWeight += weight;
        return this;
    }

    /// <summary>Roll one real combat card from the table, weighted by each entry's weight (Upgraded if <paramref name="upgraded" />).</summary>
    public CardModel Roll(Player owner, ICombatState combat, Rng rng, bool upgraded)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Familiar loot table is empty.");
        }

        int roll = rng.NextInt(_totalWeight);
        foreach ((int weight, Func<Player, ICombatState, bool, CardModel> create) in _entries)
        {
            if (roll < weight)
            {
                return create(owner, combat, upgraded);
            }

            roll -= weight;
        }

        return _entries[^1].Create(owner, combat, upgraded); // unreachable; satisfies the compiler
    }
}

/// <summary>
/// Convenience base for a familiar that can produce one of several token-cards (a "loot table"), e.g. Bear.
/// Declare the cards (and optional weights) by overriding <see cref="BuildLootTable" />; the table is built
/// once and rolled at the start of each owner turn. Single-card familiars should use <see cref="FamiliarPower{TCard}" />.
/// </summary>
public abstract class LootTableFamiliarPower : FamiliarPower
{
    private FamiliarLootTable? _lootTable;

    /// <summary>Declare the cards this familiar can produce. Called once, lazily.</summary>
    protected abstract FamiliarLootTable BuildLootTable();

    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        (_lootTable ??= BuildLootTable()).Roll(owner, combat, rng, GrantsUpgradedCards);
}
