using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

public sealed class SomethingWicked : WickenCard
{
    public SomethingWicked()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var rarity = IsUpgraded ? PotionRarity.Uncommon : PotionRarity.Common;
        var potion = PotionCatalog.Random(
            PotionCatalog.Query(PotionTrait.Offensive, matchAll: false, rarity: rarity),
            Owner.RunState.Rng.CombatPotionGeneration);

        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
}
