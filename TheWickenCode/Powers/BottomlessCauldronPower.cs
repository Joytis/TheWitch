using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Bottomless Cauldron: whenever the player uses a potion, create a <see cref="WickedBrew" />. Passive
/// toggle (Single stack). Uses the context-free <c>PotionCmd.TryToProcure&lt;T&gt;(Player)</c> overload.
/// </summary>
public sealed class BottomlessCauldronPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await PotionCmd.TryToProcure<WickedBrew>(Owner.Player);
        }
    }
}
