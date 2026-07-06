using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Pact of Beasts: bleed for the pack — round up EVERY familiar card (summon Powers *and* generated token
/// cards) from your draw and discard piles into your hand.
/// </summary>
public sealed class PactOfBeasts : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(3m)
    ];

    public PactOfBeasts()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);

        List<CardModel> familiars = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(c => c is IFamiliarSummon or WickenFamiliarCard)
            .ToList();
        if (familiars.Count > 0)
        {
            await CardPileCmd.Add(familiars, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
