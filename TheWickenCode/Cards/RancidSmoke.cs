using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Belch out the rot: copy every Debuff currently on the Wicken onto ALL enemies (you keep yours).
/// Mirrors the base-game <c>Misery</c> debuff-spread, but the source is your own powers, not a target's.
/// Clones each debuff per application so the player's instances are never detached
/// (<see cref="PowerModel.ClonePreservingMutability" />); stacks onto enemies that already have it.
/// </summary>
public sealed class RancidSmoke : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public RancidSmoke()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        List<PowerModel> debuffs = Owner.Creature.Powers
            .Where(p => p.TypeForCurrentAmount == PowerType.Debuff)
            .Select(p => (PowerModel)p.ClonePreservingMutability())
            .ToList();
        if (debuffs.Count == 0)
        {
            return;
        }
        foreach (Creature enemy in CombatState!.HittableEnemies)
        {
            foreach (PowerModel debuff in debuffs)
            {
                PowerModel? existing = PowerCmd.FindExistingInstanceForStacking(debuff, enemy, Owner.Creature);
                if (existing != null)
                {
                    await PowerCmd.ModifyAmount(choiceContext, existing, debuff.Amount, Owner.Creature, this);
                }
                else
                {
                    PowerModel fresh = (PowerModel)debuff.ClonePreservingMutability();
                    if (fresh is ITemporaryPower temporary)
                    {
                        temporary.IgnoreNextInstance();
                    }
                    await PowerCmd.Apply(choiceContext, fresh, enemy, debuff.Amount, Owner.Creature, this);
                }
            }
        }
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
