using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Co-op (MP-only): siphon energy out of a chosen ally and bottle it — you gain one random potion
/// per energy drained (capped at the ally's remaining energy). The ally loses the energy; you keep
/// the brew. MP-only.
/// </summary>
public sealed class TinyBottle : WickenCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        EnergyHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2)
    ];

    public TinyBottle()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var ally = cardPlay.Target.Player!;
        int available = ally.PlayerCombatState?.Energy ?? 0;
        int drained = Math.Min(DynamicVars.Energy.IntValue, available);
        if (drained <= 0)
        {
            return;
        }
        await PlayerCmd.LoseEnergy(drained, ally);
        for (int i = 0; i < drained; i++)
        {
            await PotionCmd.TryToProcure(
                PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
                Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
