using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Broom Strike: swat with the broom, then sweep a random Skill out of the draw pile and play it
/// (the base-game BeatDown/Catastrophe auto-play shape: filter, shuffle-pick, <c>CardCmd.AutoPlay</c>
/// with a random target for enemy-targeted picks).
/// </summary>
public sealed class BroomStrike : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move)
    ];

    public BroomStrike()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        CardModel? skill = PileType.Draw.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Skill && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Shuffle)
            .FirstOrDefault();
        if (skill == null)
        {
            return;
        }

        if (skill.TargetType == TargetType.AnyEnemy)
        {
            Creature? target = Owner.RunState.Rng.CombatTargets.NextItem(CombatState!.HittableEnemies);
            if (target != null)
            {
                await CardCmd.AutoPlay(choiceContext, skill, target);
            }
        }
        else
        {
            await CardCmd.AutoPlay(choiceContext, skill, null);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
