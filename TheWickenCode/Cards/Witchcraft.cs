using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Extensions;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Witchcraft (renamed from Cackle): pour the whole belt into the Cauldron. Discards every potion except
/// The Cauldron (creating one if needed), then feeds the discarded count into
/// <see cref="TheCauldron.PourPotions" /> — per potion: +2 Strength and +3 heal on use; 2+ poured also
/// unlocks Energy, 3+ a debuff cleanse, 4+ Intangible.
/// </summary>
public sealed class Witchcraft : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<TheCauldron>(),
    ];

    public Witchcraft()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<PotionModel> poured = Owner.Potions.Where(p => p is not TheCauldron).ToList();
        foreach (PotionModel potion in poured)
        {
            await PotionCmd.Discard(potion);
        }

        TheCauldron? cauldron = Owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        if (cauldron == null)
        {
            await PotionCmd.TryToProcure<TheCauldron>(Owner);
            cauldron = Owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        }

        if (cauldron != null && poured.Count > 0)
        {
            WickenFx.Splash(Owner.Creature, new Godot.Color("ac54b3")); // cauldron pour: purple splash
            cauldron.PourPotions(poured.Count);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
