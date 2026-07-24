using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Hurl the whole belt: one 10-damage hit per potion you've brewed this combat. The hit count renders live
/// on the card face via the base-game Barrage pattern (CalculatedVar of base 0 + extra 1 × potions created).
/// Counting uses the native AfterPotionProcured hook (delivered while this card is in any combat pile), so
/// only SUCCESSFUL procures count — a belt-full brew adds no hit. Each copy tracks its own counter.
/// </summary>
public sealed class BottleBarrage : WitchCard
{
    private const string _calculatedHitsKey = "CalculatedHits";

    private int _potionsCreated;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHitsKey)
            .WithMultiplier((card, _) => ((BottleBarrage)card)._potionsCreated)
    ];

    public override Task BeforeCombatStart()
    {
        _potionsCreated = 0;
        return base.BeforeCombatStart();
    }

    public override Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner)
        {
            _potionsCreated++;
        }
        return base.AfterPotionProcured(potion);
    }

    public BottleBarrage()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int potions = (int)((CalculatedVar)DynamicVars[_calculatedHitsKey]).Calculate(cardPlay.Target);
        if (potions <= 0)
        {
            return;
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(potions)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_rock_shatter", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
