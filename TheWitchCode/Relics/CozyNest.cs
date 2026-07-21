using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Cozy Nest (shop): at the start of combat, summon an Owl Familiar — one stack of
/// <see cref="OwlFamiliarPower" />, same as playing an unupgraded Owl Familiar (the power
/// spawns the pet and produces the turn-start token cards).
/// </summary>
public sealed class CozyNest : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<OwlFamiliarPower>(),
        HoverTipFactory.FromCard<Wisdom>(),
        HoverTipFactory.FromCard<Knowledge>(),
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<OwlFamiliarPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
    }
}
