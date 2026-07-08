using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Extract Life: an Attack that also enchants a random card in your hand with Replay and Exhaust — one big
/// extra payoff this combat, then gone. Replay enchant mirrors the base-game <c>HiddenGem</c> pattern
/// (<c>BaseReplayCount += Replay</c>, then <c>CardCmd.Preview</c>); Exhaust is added via <c>AddKeyword</c>.
/// </summary>
public sealed class ExtractLife : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move),
        new IntVar("Replay", 2m)
    ];

    public ExtractLife()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);

        List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();
        if (hand.Count == 0)
        {
            return;
        }
        CardModel? chosen = Owner.RunState.Rng.CombatCardSelection.NextItem(hand);
        if (chosen != null)
        {
            chosen.BaseReplayCount += DynamicVars["Replay"].IntValue;
            chosen.AddKeyword(CardKeyword.Exhaust);
            CardCmd.Preview(chosen);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Replay"].UpgradeValueBy(1m);
}
