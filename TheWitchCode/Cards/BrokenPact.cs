using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Broken Pact: end a familiar's service to mend your own wounds.</summary>
public sealed class BrokenPact : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 10m)
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
            await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].IntValue);
            VfxCmd.PlayOnCreatureCenter(Owner.Creature, VfxCmd.healPath);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(3m);
}
