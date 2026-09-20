using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheAugur.TheAugurCode.Character;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Relics;

[Pool(typeof(AugurRelicPool))]
public abstract class AugurRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".RelicAtlasPath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres".RelicOutlineAtlasPath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
}
