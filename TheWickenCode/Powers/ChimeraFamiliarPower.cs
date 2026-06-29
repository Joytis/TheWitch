using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Cards;
using TheWicken.TheWickenCode.Monsters;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Counter marking how many Chimera familiars the player currently has. Unlike other familiars, Chimera
/// trades hand size for volume: each stack draws 2 fewer cards each turn (<see cref="ModifyHandDraw" />)
/// but creates 3 random familiar token-cards at the start of your turn (<see cref="AfterPlayerTurnStart" />).
/// See <see cref="FamiliarPower" />.
/// </summary>
public sealed class ChimeraFamiliarPower : FamiliarPower
{
    private const int CardsPerStack = 3;
    private const int DrawReductionPerStack = 2;

    protected override WickenPet Pet => ModelDb.Monster<ChimeraPet>();

    // Unused: the turn-start payoff is overridden below to create several cards per stack.
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        FamiliarCardRegistry.CreateRandom(owner, 1, combat, rng, GrantsUpgradedCards).First();

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player.Creature == Owner ? count - DrawReductionPerStack * Amount : count;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        Rng rng = player.RunState.Rng.CombatCardGeneration;
        List<CardModel> cards = FamiliarCardRegistry.CreateRandom(
            player, CardsPerStack * Amount, combat, rng, GrantsUpgradedCards);
        foreach (CardModel card in cards)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }
}
