using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Bear familiar token (the turn after Hibernate): the Cubs roll around in the blackberries — Brambles per Cub.
/// Live total via the base-game Compile Driver shape (<see cref="CalculatedVar" />), with the per-Cub amount being the
/// displayed/upgradable <c>BramblesPower</c> var instead of a separate <c>CalculationExtra</c>.
/// </summary>
public sealed class Forage : WitchFamiliarCard
{
    private const string CalculatedBramblesKey = "CalculatedBrambles";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromPower<BearCubPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(3m),
        new CalculationBaseVar(0m),
        new PerCubBramblesVar(CalculatedBramblesKey)
            .WithMultiplier((card, _) => card.Owner?.Creature?.GetPowerAmount<BearCubPower>() ?? 0),
    ];

    /// <summary><see cref="CalculatedVar" /> whose per-unit extra is the card's <c>BramblesPower</c> var.</summary>
    private sealed class PerCubBramblesVar(string name) : CalculatedVar(name)
    {
        protected override DynamicVar GetExtraVar() => ((CardModel)_owner!).DynamicVars.Brambles();
    }

    public override CardModel Summon => ModelDb.Card<BearFamiliar>();

    public Forage()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal brambles = ((CalculatedVar)DynamicVars[CalculatedBramblesKey]).Calculate(null);
        if (brambles > 0)
        {
            await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, brambles, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(1m);
}
