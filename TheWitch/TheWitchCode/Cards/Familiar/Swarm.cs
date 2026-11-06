using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Rat familiar token: flood the piles — Rats shuffled into BOTH the draw and discard piles
/// (Refuse Pile pattern) — and crown the Queen of the Rats: every Rats card deals more damage (<see cref="QueenOfTheRatsPower" />).
/// </summary>
public sealed class Swarm : WitchFamiliarCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(IsUpgraded),
        HoverTipFactory.FromPower<QueenOfTheRatsPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new PowerVar<QueenOfTheRatsPower>(1m),
    ];

    public override CardModel Summon => ModelDb.Card<RatFamiliar>();

    public Swarm()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int perPile = DynamicVars.Cards.IntValue;
        var drawRats = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, perPile, CombatState, IsUpgraded);
        var generatedDraw = await CardPileCmd.AddGeneratedCardsToCombat(drawRats, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generatedDraw);

        var discardRats = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, perPile, CombatState, IsUpgraded);
        var generatedDiscard = await CardPileCmd.AddGeneratedCardsToCombat(discardRats, PileType.Discard, Owner);
        CardCmd.PreviewCardPileAdd(generatedDiscard);

        await PowerCmd.Apply<QueenOfTheRatsPower>(choiceContext, Owner.Creature, DynamicVars["QueenOfTheRatsPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
