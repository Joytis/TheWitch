using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class NeedleWhip : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    public NeedleWhip()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int brambles = Owner.Creature.GetPowerAmount<BramblesPower>();
        if (brambles <= 0)
        {
            return;
        }

        await PowerCmd.Remove<BramblesPower>(Owner.Creature);
        await PowerCmd.Apply<NeedleWhipStrengthPower>(choiceContext, CombatState!.HittableEnemies, brambles, Owner.Creature, this);
    }
}
