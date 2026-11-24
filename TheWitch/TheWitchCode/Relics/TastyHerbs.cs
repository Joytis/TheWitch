using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Tasty Herbs: every 4th potion you use in combat is used an additional time (counter relic, Morganne shape;
/// the count persists across combats). The extra use goes through the out-of-belt path
/// (<see cref="PotionAutoPlay.ReplayOutOfBelt" />): by the time AfterPotionUsed fires the potion has already
/// been removed from the belt, so <c>OnUseWrapper</c>'s RemoveBeforeUse throws "Tried to remove potion
/// you don't have" — and that exception escaping UsePotionAction froze the action queue for the rest
/// of the combat (AutoSlay seed 55QGFPI, Act 2 boss). The replay fires no use-hooks, so it never counts
/// toward the next 4.
/// </summary>
public sealed class TastyHerbs : WitchRelic
{
    private const int Interval = 4;

    private int _potionsUsed;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    public override int DisplayAmount => PotionsUsed % Interval;

    [SavedProperty]
    public int PotionsUsed
    {
        get => _potionsUsed;
        set
        {
            AssertMutable();
            _potionsUsed = value;
            Status = _potionsUsed % Interval == Interval - 1 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner
            || !CombatManager.Instance.IsInProgress
            || Owner.Creature.CombatState is not { } combat)
        {
            return;
        }

        PotionsUsed++;
        if (PotionsUsed % Interval != 0)
        {
            return;
        }

        // The first use may have killed a single-enemy potion's target — re-roll onto a living enemy.
        if (potion.TargetType == TargetType.AnyEnemy && target is not { IsAlive: true })
        {
            target = PotionAutoPlay.ResolveTarget(potion, Owner, Owner.Creature, combat);
            if (target == null)
            {
                return;
            }
        }

        Flash();
        await PotionAutoPlay.ReplayOutOfBelt(this, potion, target, Owner, combat);
    }
}
