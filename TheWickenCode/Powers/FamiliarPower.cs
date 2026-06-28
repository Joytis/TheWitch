using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WickenCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Payoff: at the START of the owner's turn, the familiar gives you ONE random card it can produce, added to
/// your hand (see <see cref="CreateTurnStartCard" />). This replaces the old "shuffle N token cards into your
/// deck on summon" — ongoing, immediate value, and sacrificing the power (<c>PowerCmd.Decrement</c> to zero,
/// which auto-removes the <see cref="PowerStackType.Counter" />) actually costs you that recurring card.
/// </summary>
public abstract class FamiliarPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Create one (un-upgraded) real combat card this familiar can produce, chosen at random if it has several.</summary>
    protected abstract CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }

        Flash();
        CardModel card = CreateTurnStartCard(player, combat, player.RunState.Rng.CombatCardGeneration);
        await CardPileCmd.Add(card, PileType.Hand);
    }
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
        WickenCard.CreateFamiliarCards<TCard>(owner, 1, combat, false).First();
}
