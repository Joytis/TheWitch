using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Cloak of Moonlight: whenever the owner creates a card (<c>AfterCardGeneratedForCombat</c> — the generated
/// path, which familiar tokens, brews-as-cards, etc. all use) or a potion (<c>AfterPotionProcured</c>), apply
/// <see cref="PowerModel.Amount" /> <see cref="MoonlightPower" /> to a random enemy. Same use/create hook pair
/// as Volatile Vapors.
/// </summary>
public sealed class CloakOfMoonlightPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MoonlightPower>(),
    ];

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner.Player)
        {
            await ApplyMoonlightToRandomEnemy();
        }
    }

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner.Player)
        {
            await ApplyMoonlightToRandomEnemy();
        }
    }

    private async Task ApplyMoonlightToRandomEnemy()
    {
        if (Owner.CombatState is not { } combat || Owner.Player is not { } player || Amount <= 0)
        {
            return;
        }
        List<Creature> targets = combat.HittableEnemies.ToList();
        if (targets.Count == 0)
        {
            return;
        }
        if (player.RunState.Rng.CombatTargets.NextItem(targets) is not { } target)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<MoonlightPower>(new ThrowingPlayerChoiceContext(), target, Amount, Owner, null);
    }
}
