using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>Wormy Apple: a big heal that comes with a catch — 3 Wormy status cards wriggle into your hand.</summary>
public sealed class WormyApple : WickenPotion
{
    private const int HealAmount = 15;
    private const int WormyCount = 3;

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CreatureCmd.Heal(Owner.Creature, HealAmount);

        var wormies = new List<CardModel>(WormyCount);
        for (int i = 0; i < WormyCount; i++)
        {
            wormies.Add(Owner.Creature.CombatState!.CreateCard<Wormy>(Owner));
        }
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(wormies, PileType.Hand, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }
}
