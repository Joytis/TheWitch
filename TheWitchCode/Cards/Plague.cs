using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Plague: hex the whole board, and the rats come back — every Rats card in the Exhaust pile returns
/// to your hand (a move of existing cards, so plain <c>CardPileCmd.Add</c>, capped by hand space).
/// </summary>
public sealed class Plague : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m)
    ];

    public Plague()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HexPower>(choiceContext, CombatState!.HittableEnemies, DynamicVars.Hex().BaseValue, Owner.Creature, this);

        int space = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        List<CardModel> rats = PileType.Exhaust.GetPile(Owner).Cards
            .Where(c => c is IRatCard)
            .Take(Math.Max(0, space))
            .ToList();
        if (rats.Count > 0)
        {
            await CardPileCmd.Add(rats, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Hex().UpgradeValueBy(1m);
}
