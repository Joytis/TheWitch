using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Light the Candle: upgrade random cards in your hand (those upgrades also fire Bursting Roots / Twinroot),
/// then create a Vial of Smoke.
/// </summary>
public sealed class LightTheCandle : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<VialOfSmoke>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public LightTheCandle()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<CardModel> upgradable = PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        int count = DynamicVars.Cards.IntValue;
        for (int i = 0; i < count && upgradable.Count > 0; i++)
        {
            CardModel pick = Owner.RunState.Rng.CombatCardGeneration.NextItem(upgradable);
            upgradable.Remove(pick);
            CardCmd.Upgrade(pick);
        }

        await PotionCmd.TryToProcure<VialOfSmoke>(Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}
