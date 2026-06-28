using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Witchcraft: spend X energy to brew X random potions. Exhaust.</summary>
public sealed class Witchcraft : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    public Witchcraft()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int count = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < count; i++)
        {
            await PotionCmd.TryToProcure(
                PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
                Owner);
        }
    }
}
