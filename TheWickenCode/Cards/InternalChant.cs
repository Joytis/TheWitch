using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Internal Chant: gain Block, then gain Vigor equal to the total number of debuffs across every creature in
/// combat (the player and all enemies). Debuff counting mirrors the base-game <c>Rend</c> filter
/// (<c>PowerType.Debuff</c>, excluding temporary powers).
/// </summary>
public sealed class InternalChant : WickenCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<VigorPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move)
    ];

    public InternalChant()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        int debuffs = CombatState!.Creatures.Sum(c => c.Powers.Count(ShouldCountPower));
        if (debuffs > 0)
        {
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, debuffs, Owner.Creature, this);
        }
    }

    private static bool ShouldCountPower(PowerModel power) =>
        power.TypeForCurrentAmount == PowerType.Debuff && power is not ITemporaryPower;
}
