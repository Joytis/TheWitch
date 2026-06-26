using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

public sealed class BloodBoiling : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(10m)
    ];

    public BloodBoiling()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        var potion = PotionCatalog.Random(
            PotionCatalog.Query(rarity: PotionRarity.Rare),
            Owner.RunState.Rng.CombatPotionGeneration);

        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
