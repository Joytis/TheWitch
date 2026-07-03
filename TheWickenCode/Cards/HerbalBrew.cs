using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions.Brewing;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class HerbalBrew : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public HerbalBrew()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var rarity = await NextPotionRarePower.MakeNextRare(Owner, PotionRarity.Common);
        rarity = await NextPotionUpgradedPower.UpgradeRarity(Owner, rarity);
        var potion = PotionCatalog.Random(
            PotionCatalog.Query(orientation: PotionOrientation.Utility, rarity: rarity),
            Owner.RunState.Rng.CombatPotionGeneration);

        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
