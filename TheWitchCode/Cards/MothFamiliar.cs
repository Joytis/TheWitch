using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Moth Familiar: the common-rarity familiar — drawn to the moonlight. Alternates Star Dust
/// (Moonlight) and Gossamer (Block) tokens each turn.</summary>
public sealed class MothFamiliar : WitchCard, IFamiliarSummon
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MothFamiliarPower>(),
        HoverTipFactory.FromPower<MoonlightPower>(),
        HoverTipFactory.FromCard<StarDust>(IsUpgraded),
        HoverTipFactory.FromCard<Gossamer>(IsUpgraded),
    ];

    public MothFamiliar()
        : base(2, CardType.Power, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<MothFamiliarPower>(choiceContext);
    }
}
