using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Owl familiar token: forge your hand. Upgrade one card in your hand (Upgraded: all of them) — an in-combat
/// upgrade, so it lasts only for the rest of this fight. Mirrors the base-game Armaments pattern.
/// </summary>
public sealed class Knowledge : WickenFamiliarCard
{
    public Knowledge()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            WickenFx.EnchantShimmer();
            foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable))
            {
                CardCmd.Upgrade(card);
            }
            return;
        }

        CardModel? card2 = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
        if (card2 != null)
        {
            WickenFx.EnchantShimmer();
            CardCmd.Upgrade(card2);
        }
    }
}
