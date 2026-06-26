using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Wolf familiar token. Escalates: deals its base damage plus 5 for each Gnash already played this combat
/// (the "pack" grows as you play more). Bonus computed at play time from combat history.
/// </summary>
public sealed class Gnash : WickenFamiliarCard
{
    private const int PackBonusPerGnash = 5;

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
        int priorGnash = CombatHistoryQueries.CardsPlayedThisCombat<Gnash>(Owner.Creature);
        decimal damage = DynamicVars.Damage.BaseValue + PackBonusPerGnash * priorGnash;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
