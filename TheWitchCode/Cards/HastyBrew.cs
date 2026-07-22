using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Hasty Brew: fast mana — brew an Energy Potion. Exhausts; upgrade removes Exhaust.</summary>
public sealed class HastyBrew : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<EnergyPotion>(),
    ];

    public HastyBrew()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PotionCmd.TryToProcure<EnergyPotion>(Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
