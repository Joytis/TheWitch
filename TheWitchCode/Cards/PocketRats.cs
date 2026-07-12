using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Pocket Rats: dump a handful of one-shot Rats straight into your hand. Exhausts.</summary>
public sealed class PocketRats : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public PocketRats()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        // Upgrade adds +1 Rat but never upgrades the Rats themselves.
        List<Rats> cards = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, DynamicVars.Cards.IntValue, CombatState, isUpgraded: false).ToList();
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
