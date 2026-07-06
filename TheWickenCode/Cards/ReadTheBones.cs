using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Read the Bones (was Steal Bones): divine the enemy's next move — if it intends to attack, draw cards;
/// otherwise strike it. Intent check + gold glow mirror the base-game Go for the Eyes.
/// </summary>
public sealed class ReadTheBones : WickenCard
{
    protected override bool ShouldGlowGoldInternal =>
        CombatState != null && (Pile?.Type != PileType.Hand
            || CombatState.HittableEnemies.Any(e => e.Monster?.IntendsToAttack ?? false));

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new DamageVar(9m, ValueProp.Move)
    ];

    public ReadTheBones()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        if (cardPlay.Target.Monster?.IntendsToAttack ?? false)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
        else
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
