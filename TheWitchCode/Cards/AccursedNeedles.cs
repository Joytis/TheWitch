using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Accursed Needles: each play digs the needles deeper — applies base Hex + 1 per prior play this combat.
/// Live Hex number via the Barrage CalculatedVar shape; the play counter is a combat-scoped mutable-instance
/// field (UpMySleeve pattern).
/// </summary>
public sealed class AccursedNeedles : WitchCard
{
    private const string _calculatedHexKey = "CalculatedHex";

    private int _timesPlayedThisCombat;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move),
        new CalculationBaseVar(2m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHexKey)
            .WithMultiplier((card, _) => ((AccursedNeedles)card)._timesPlayedThisCombat)
    ];

    public AccursedNeedles()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        decimal hex = ((CalculatedVar)DynamicVars[_calculatedHexKey]).Calculate(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, hex, Owner.Creature, this);
        }

        AssertMutable();
        _timesPlayedThisCombat++;
    }

    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(1m);
}
