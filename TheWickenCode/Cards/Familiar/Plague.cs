using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Rat familiar token: draw, then everyone (you + all enemies) loses Strength.</summary>
public sealed class Plague : WickenFamiliarCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1),
        new DynamicVar("StrengthLoss", 1m),
        new DynamicVar("EnemyStrengthLoss", 2m),
    ];

    public Plague()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        int loss = DynamicVars["StrengthLoss"].IntValue;
        int enemyLoss = DynamicVars["EnemyStrengthLoss"].IntValue;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, -loss, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, CombatState!.HittableEnemies, -enemyLoss, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["EnemyStrengthLoss"].UpgradeValueBy(1m);
}
