using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Ambush!: heavy AoE, then summon one random familiar (applies a random <see cref="FamiliarPower" />). Exhaust.</summary>
public sealed class Ambush : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(15m, ValueProp.Move)
    ];

    public Ambush()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // "Summon a random familiar" = apply a random familiar counter power, mirroring the summon cards'
        // GainFamiliar. No cosmetic pet (only Owl/Cat have one); purely the mechanical familiar.
        List<FamiliarPower> familiars = ModelDb.AllPowers.OfType<FamiliarPower>().ToList();
        if (familiars.Count > 0)
        {
            FamiliarPower pick = Owner.RunState.Rng.CombatCardGeneration.NextItem(familiars)!;
            await PowerCmd.Apply(choiceContext, pick.ToMutable(), Owner.Creature, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
