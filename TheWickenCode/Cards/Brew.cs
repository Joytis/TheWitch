using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Brew (starter): apply Weak, then stir a RANDOM belt potion into The Cauldron (+2 Strength / +3 Heal to
/// its stats; the potion is consumed). Creates The Cauldron only when there is a potion to stir — no empty
/// Cauldron clogging a belt slot. With Extract Essence this is the starter generator→payoff potion loop.
/// </summary>
public sealed class Brew : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPotion<TheCauldron>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WeakPower>(1m)
    ];

    public Brew()
        : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);

        List<PotionModel> stirrable = Owner.Potions.Where(p => p is not TheCauldron).ToList();
        PotionModel? stirred = Owner.RunState.Rng.CombatPotionGeneration.NextItem(stirrable);
        if (stirred == null)
        {
            return;
        }
        await PotionCmd.Discard(stirred);

        TheCauldron? cauldron = Owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        if (cauldron == null)
        {
            await PotionCmd.TryToProcure<TheCauldron>(Owner);
            cauldron = Owner.Potions.OfType<TheCauldron>().FirstOrDefault();
        }
        cauldron?.Stir();
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(2m);
}
