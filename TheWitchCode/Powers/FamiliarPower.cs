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
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WitchCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Payoff: just BEFORE the owner's turn hand-draw, the familiar adds one random card it can produce to your
/// hand PER STACK (see <see cref="CreateTurnStartCard" /> — each stack rolls independently), so tokens sit in
/// front of the drawn cards. This replaces the old
/// "shuffle N token cards into your deck on summon" — ongoing, immediate value, and sacrificing the power
/// (<c>PowerCmd.Decrement</c> to zero, which auto-removes the <see cref="PowerStackType.Counter" />) actually
/// costs you those recurring cards.
/// </summary>
public abstract class FamiliarPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// When true, the token cards this familiar produces come out Upgraded. Set by <c>WitchCard.GainFamiliar</c>
    /// when an upgraded familiar summon card is played — sticky once any upgraded summon has applied this power.
    /// </summary>
    public bool GrantsUpgradedCards { get; set; }

    /// <summary>Create one real combat card this familiar can produce (Upgraded per <see cref="GrantsUpgradedCards" />), chosen at random if it has several.</summary>
    protected abstract CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng);

    /// <summary>
    /// Canonical cosmetic pet shown at the player's feet — ONE PET PER STACK, so the board shows how many of
    /// which familiar are out without reading the buff bar. The pet count is synced to <see cref="Amount" />
    /// on every stack change (see <see cref="AfterPowerAmountChanged" />); same-type pets cluster together via
    /// <c>WitchPetClusterPatch</c>. Every familiar declares one.
    /// </summary>
    protected abstract WitchPet Pet { get; }

    // BeforeHandDraw (base-game SentryModePower pattern) so tokens enter the hand BEFORE the turn's
    // draw — they sit in front of the drawn cards. Top guards the retained-cards case.
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        Flash();
        Rng rng = player.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < Amount; i++)
        {
            CardModel card = CreateTurnStartCard(player, combatState, rng);
            // Use the "generated" path (not a plain Add) so the card counts as created — records combat
            // history and fires AfterCardGeneratedForCombat, which card-creation payoffs like Cloak of Moonlight listen to.
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
        }
    }

    /// <summary>Summon signature on every familiar gained (assets preloaded via Witch.ExtraAssetPaths). Pet spawning lives in <see cref="AfterPowerAmountChanged" />, which also fires for the initial application.</summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.SummonFlourish(Owner);
        return Task.CompletedTask;
    }

    /// <summary>Keep the cosmetic pet count in lockstep with the stack count (one pet per stack).</summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            await SyncPets();
        }
    }

    /// <summary>Despawn any remaining pets when the power is removed outright (decrement-to-zero already synced to 0 via <see cref="AfterPowerAmountChanged" />).</summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player is not { PlayerCombatState: not null } player)
        {
            return;
        }

        foreach (Creature pet in FindPets(player))
        {
            await CreatureCmd.Kill(pet, force: true);
        }
    }

    private async Task SyncPets()
    {
        if (Owner.Player is not { PlayerCombatState: not null } player || Owner.CombatState is not { } combat)
        {
            return;
        }

        List<Creature> pets = FindPets(player);
        int want = Math.Max(0, Amount);
        for (int i = pets.Count; i < want; i++)
        {
            Creature pet = combat.CreateCreature((MonsterModel)Pet.ToMutable(), Owner.Side, null);
            await PlayerCmd.AddPet(pet, player);
        }
        for (int i = pets.Count - 1; i >= want; i--)
        {
            await CreatureCmd.Kill(pets[i], force: true);
        }
    }

    private List<Creature> FindPets(Player player) =>
        player.PlayerCombatState!.Pets.Where(p => p.Monster?.GetType() == Pet.GetType()).ToList();
}

/// <summary>
/// Convenience base for the common case: a familiar that always produces the same single token card
/// <typeparamref name="TCard" />. Single-type familiar powers just declare <c>: FamiliarPower&lt;TCard&gt;</c>.
/// Familiars that produce several different cards (Bear) extend <see cref="FamiliarPower" /> directly
/// and override <see cref="CreateTurnStartCard" />.
/// </summary>
public abstract class FamiliarPower<TCard> : FamiliarPower where TCard : WitchFamiliarCard
{
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, GrantsUpgradedCards).First();
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
    public FamiliarLootTable Add<TCard>(int weight = 1) where TCard : WitchFamiliarCard
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Loot-table weight must be positive.");
        }

        _entries.Add((weight, (owner, combat, upgraded) => FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, upgraded).First()));
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
