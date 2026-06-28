using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWicken.TheWickenCode.Patches;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Bursting Roots (granted by Rootcraft): whenever the player upgrades a card in their hand, gain
/// <see cref="MegaCrit.Sts2.Core.Models.PowerModel.Amount" /> Brambles. The upgrade is detected synchronously by
/// <see cref="CardUpgradeTracker" /> (no async context there), which enqueues the owed Brambles; this power drains
/// and applies them in <c>AfterCardPlayed</c> — the first async beat after the card that caused the upgrade.
/// </summary>
public sealed class BurstingRootsPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int owed = CardUpgradeTracker.TakePendingBrambles(Owner);
        if (owed > 0)
        {
            Flash();
            await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, owed, Owner, null);
        }
    }
}
