using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Co-op (MP-only): pass a round of brews — you and every ally each gain a random potion.</summary>
public sealed class ShareTheBrew : WitchCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public ShareTheBrew()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        IEnumerable<Creature> allies = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);
        foreach (Creature ally in allies)
        {
            var player = ally.Player!;
            await PotionCmd.TryToProcure(
                PotionFactory.CreateRandomPotionInCombat(player, player.RunState.Rng.CombatPotionGeneration).ToMutable(),
                player);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
