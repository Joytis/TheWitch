using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Brew: upgrade a random potion in the player's belt to a higher-rarity one sharing its traits
/// (see <see cref="PotionUpgrade" />). Does nothing if the belt is empty.
/// </summary>
public sealed class Brew : WickenCard
{
    public Brew()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PotionUpgrade.UpgradeRandomPotion(Owner, Owner.RunState.Rng.CombatPotionGeneration);
    }

    protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
