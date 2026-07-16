using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Broken Pact: end a familiar's service and take its strength for your own.</summary>
public sealed class BrokenPact : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(8m)
    ];

    public BrokenPact()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        bool sacrificed = await Familiars.RemoveRandom(Owner.Creature, Owner.RunState.Rng.CombatTargets);
        if (sacrificed)
        {
            VfxCmd.PlayOnCreatureCenter(Owner.Creature, "vfx/vfx_spooky_scream");
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(2m);
}
