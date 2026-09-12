using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Smolder: at the start of each of the next <see cref="PowerModel.Amount" /> turns, create an Unstable
/// Ember Jar. Lightning Rod shape — fires in AfterEnergyReset and decrements per turn.
/// </summary>
public sealed class SmolderPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
        HoverTipFactory.FromPotion<EmberJar>(),
    ];

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }
        Flash();
        await Witch.ProducePotion<EmberJar>(player, Witch.PotionMode.Unstable);
        await PowerCmd.Decrement(this);
    }
}
