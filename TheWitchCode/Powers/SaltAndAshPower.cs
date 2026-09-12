using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Salt and Ash buff: whenever the owner plays a Skill this turn, deal <see cref="PowerModel.Amount" /> damage to
/// ALL enemies (unpowered, Hailstorm-style <c>CreatureCmd.Damage</c>). The Salt and Ash play that applied the
/// power does not trigger it. Base-game Rage shape: stacks additively, removed at end of the owner's turn.
/// </summary>
public sealed class SaltAndAshPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>The card play that applied this power; skipped so the source Skill doesn't trigger itself.</summary>
    private CardModel? _source;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _source ??= cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Skill)
        {
            return;
        }
        if (cardPlay.Card == _source)
        {
            _source = null;
            return;
        }
        Flash();
        SfxCmd.Play("event:/sfx/characters/attack_fire");
        foreach (Creature enemy in CombatState!.HittableEnemies)
        {
            WitchFx.PlayFlipbook("vfx/fire_impact/vfx_fire_burst_center_flipbook", enemy, null, 0.7f);
        }
        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
