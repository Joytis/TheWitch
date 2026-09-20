using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Powers;

public abstract class AugurPower : CustomPowerModel
{
    //Renders the power_atlas slice packed from TheAugur/images/powers/your_power.png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PowerAtlasPath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }
}
