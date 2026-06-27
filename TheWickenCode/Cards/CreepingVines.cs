using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Co-op (MP-only): spend X energy to fling 5 Brambles onto a random ally, X times — the brambles
/// scatter across the team. A random ally (yourself included) is rolled fresh for each hit.
/// </summary>
public sealed class CreepingVines : WickenCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(5m)
    ];

    public CreepingVines()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<Creature> allies = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();
        if (allies.Count == 0)
        {
            return;
        }
        decimal brambles = DynamicVars.Brambles().BaseValue;
        int times = ResolveEnergyXValue();
        for (int i = 0; i < times; i++)
        {
            Creature target = Owner.RunState.Rng.CombatTargets.NextItem(allies)!;
            await PowerCmd.Apply<BramblesPower>(choiceContext, target, brambles, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(2m);
}
