using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Bewitching Grin: a cheap one-shot skill that saps every enemy with Weak.</summary>
public sealed class BewitchingGrin : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WeakPower>(2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BewitchingGrin()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, CombatState!.HittableEnemies, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["WeakPower"].UpgradeValueBy(1m);
}
