using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

public sealed class HerbalRemedy : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1),
        new EnergyVar(1)
    ];

    public HerbalRemedy()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        foreach (PotionModel potion in Owner.Potions.ToList())
        {
            await PotionCmd.Discard(potion);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
