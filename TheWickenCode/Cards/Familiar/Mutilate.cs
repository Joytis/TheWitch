using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Bear familiar token: heavy hit that ignores Block. No "ignore block" exists on the attack builder, so
/// this uses <c>CreatureCmd.Damage</c> with <see cref="ValueProp.Unblockable" /> (still powered via Move).
/// </summary>
public sealed class Mutilate : WickenFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(22m, ValueProp.Move)
    ];

    public Mutilate()
        : base(2, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Move | ValueProp.Unblockable, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);
}
