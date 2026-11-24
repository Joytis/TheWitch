using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Relics;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Co-op (MP-only): pass a round of brews — you and every ally each gain a random Unstable potion.</summary>
public sealed class ShareTheBrew : WitchCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ UnstablePotions.UnstableHoverTip ];

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
            // Each player's brew runs under its own branching context (base-game Huddle Up shape): a Separatory
            // Funnel pick by an ally moves onto that ally's own action instead of blocking the caster, and two
            // funnel-holding allies never share one branch.
#if STS2_BETA
            BranchingPlayerChoiceContext branch = new(this, LocalContext.NetId!.Value, choiceContext);
#else
            BranchingPlayerChoiceContext branch = new(this, LocalContext.NetId!.Value, GameActionType.Combat, choiceContext);
#endif
            await branch.AssignTaskAndWaitForPauseOrCompletion(Brew(branch, ally.Player!));
        }
    }

    private static async Task Brew(PlayerChoiceContext context, Player player)
    {
        Rng rng = player.RunState.Rng.CombatPotionGeneration;
        PotionModel? potion;
        if (player.GetRelic<SeparatoryFunnel>() is { } funnel)
        {
            List<PotionModel> options = PotionCatalog.RollFunnelOptions(
                rolled => PotionFactory.CreateRandomPotionInCombat(player, rng, rolled), PotionTraits.IsHealing);
            potion = await PotionCatalog.FunnelChoice(funnel, options, context, player);
        }
        else
        {
            // The factory rolls each player's own color pool (right for MP), so healing potions are filtered
            // by bounded re-roll rather than a pool query — combat-created potions must never heal.
            potion = PotionFactory.CreateRandomPotionInCombat(player, rng);
            for (int i = 0; i < 20 && PotionTraits.IsHealing(potion); i++)
            {
                potion = PotionFactory.CreateRandomPotionInCombat(player, rng);
            }
        }
        if (potion != null)
        {
            await Witch.ProducePotion(potion, player, Witch.PotionMode.Unstable);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
