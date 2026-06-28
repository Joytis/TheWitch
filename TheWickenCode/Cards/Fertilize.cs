using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Fertilize (renamed from Serrated Bones): gain Brambles and upgrade random card(s) in your hand. The in-hand
/// upgrades go through <c>CardCmd.Upgrade</c>, so they also trigger Bursting Roots / Twinroot if present.
/// </summary>
public sealed class Fertilize : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(6m),
        new CardsVar(1)
    ];

    public Fertilize()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);

        List<CardModel> upgradable = PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        int count = DynamicVars.Cards.IntValue;
        for (int i = 0; i < count && upgradable.Count > 0; i++)
        {
            CardModel? pick = Owner.RunState.Rng.CombatCardGeneration.NextItem(upgradable);
            if(pick != null)
            {
                upgradable.Remove(pick);
                CardCmd.Upgrade(pick);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Brambles().UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
