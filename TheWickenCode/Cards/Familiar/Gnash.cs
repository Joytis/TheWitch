using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Wolf familiar token. Escalates: each time any Gnash is played, every Gnash card permanently gains
/// +5 damage (the "pack" grows). Mirrors the base-game Maul pattern — the bonus is baked into each card's
/// displayed <see cref="DamageVar" /> so the number visibly rises, and is restored on downgrade.
/// </summary>
public sealed class Gnash : WickenFamiliarCard
{
    private const int PackBonusPerGnash = 5;

    private decimal _extraDamageFromPlays;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move)
    ];

    public Gnash()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        foreach (Gnash gnash in Owner.PlayerCombatState!.AllCards.OfType<Gnash>())
        {
            gnash.BuffFromGnashPlay(PackBonusPerGnash);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += _extraDamageFromPlays;
    }

    private void BuffFromGnashPlay(decimal extraDamage)
    {
        DynamicVars.Damage.BaseValue += extraDamage;
        _extraDamageFromPlays += extraDamage;
    }
}
