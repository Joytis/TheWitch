using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>Wormy Apple: a big heal that comes with a catch — 3 Wormy status cards wriggle into your hand.</summary>
public sealed class WormyApple : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Wormy>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heal", 15m),
        new CardsVar(3)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].IntValue);

        int wormyCount = DynamicVars.Cards.IntValue;
        var wormies = new List<CardModel>(wormyCount);
        for (int i = 0; i < wormyCount; i++)
        {
            wormies.Add(Owner.Creature.CombatState!.CreateCard<Wormy>(Owner));
        }
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(wormies, PileType.Hand, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }
}
