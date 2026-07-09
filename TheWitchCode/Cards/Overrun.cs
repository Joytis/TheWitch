using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Overrun: strike one enemy, then the whole menagerie tramples — each familiar hits ALL enemies.</summary>
public sealed class Overrun : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("FamiliarDamage", 3m)
    ];

    public Overrun()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite")
            .Execute(choiceContext);

        int familiars = Familiars.Count(Owner.Creature);
        if (familiars <= 0)
        {
            return;
        }
        await DamageCmd.Attack(DynamicVars["FamiliarDamage"].BaseValue)
            .WithHitCount(familiars)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_thrash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars["FamiliarDamage"].UpgradeValueBy(3m);
}
