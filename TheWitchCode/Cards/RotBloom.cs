using System;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Rot Bloom: heavy hit, then every debuff on the target blooms — each debuff power's stacks are
/// duplicated (applied again at its current amount, Rend's debuff filter: temporary powers excluded).
/// </summary>
public sealed class RotBloom : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(15m, ValueProp.Move)
    ];

    public RotBloom()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_thrash", null, "heavy_attack.mp3")
            .Execute(choiceContext);

        if (!cardPlay.Target.IsAlive)
        {
            return;
        }

        // Snapshot first — applying while iterating would mutate the power list.
        var debuffs = cardPlay.Target.Powers
            .Where(p => p.TypeForCurrentAmount == PowerType.Debuff && p is not ITemporaryPower && p.Amount > 0)
            .Select(p => (Power: p, p.Amount))
            .ToList();
        if (debuffs.Count == 0)
        {
            return;
        }

        WitchFx.GreenGas(cardPlay.Target);
        foreach ((PowerModel power, decimal amount) in debuffs)
        {
            await PowerCmd.Apply(choiceContext, power, cardPlay.Target, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
