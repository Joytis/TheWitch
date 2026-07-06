using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Consume Youth: a heavy hit that doubles against a healthy (above half HP) target. Uses the Soul Storm
/// <see cref="CalculatedDamageVar" /> shape (base + extra × multiplier, multiplier = 1 while the target is
/// above half HP) so the card face previews the doubled number live when targeting a healthy enemy.
/// </summary>
public sealed class ConsumeYouth : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(15m),
        new ExtraDamageVar(15m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier(static (_, target) =>
                target is { } t && t.CurrentHp * 2 > t.MaxHp ? 1m : 0m),
    ];

    public ConsumeYouth()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // Both halves scale so the doubled hit stays exactly 2× (26 → 52 upgraded).
    protected override void OnUpgrade()
    {
        DynamicVars["CalculationBase"].UpgradeValueBy(6m);
        DynamicVars.ExtraDamage.UpgradeValueBy(6m);
    }
}
