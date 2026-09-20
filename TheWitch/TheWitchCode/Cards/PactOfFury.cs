using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Co-op (MP-only): take 5 Weak onto yourself so every other ally gains Strength.</summary>
public sealed class PactOfFury : WitchCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WeakPower>(5m),
        new PowerVar<StrengthPower>(4m)
    ];

    public PactOfFury()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, DynamicVars.Weak.BaseValue, Owner.Creature, this);

        decimal strength = DynamicVars.Strength.BaseValue;
        IEnumerable<Creature> otherAllies = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature);
        foreach (Creature ally in otherAllies)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, ally, strength, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Strength.UpgradeValueBy(2m);
}
