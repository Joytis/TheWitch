using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Mulch: compost the whole discard pile — EXHAUST it for Brambles and Block per card.</summary>
public sealed class Mulch : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(3m),
        new BlockVar(3m, ValueProp.Move)
    ];

    public Mulch()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> discard = PileType.Discard.GetPile(Owner).Cards.ToList();
        int count = discard.Count;
        if (count == 0)
        {
            return;
        }

        foreach (CardModel card in discard)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue * count, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue * count, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Brambles().UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
