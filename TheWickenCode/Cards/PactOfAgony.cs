using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Pact of Agony: take on Vulnerable yourself to sap the Strength of all enemies.</summary>
public sealed class PactOfAgony : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(3m),
        new DynamicVar("StrengthLoss", 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public PactOfAgony()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
        int loss = DynamicVars["StrengthLoss"].IntValue;
        await PowerCmd.Apply<StrengthPower>(choiceContext, CombatState!.HittableEnemies, -loss, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(2m);
        DynamicVars["StrengthLoss"].UpgradeValueBy(1m);
    }
}
