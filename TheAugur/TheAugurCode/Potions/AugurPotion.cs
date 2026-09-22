using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheAugur.TheAugurCode.Character;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Potions;

[Pool(typeof(AugurPotionPool))]
public abstract class AugurPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PotionAtlasPath();
    public override string? CustomPackedOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres".PotionOutlineAtlasPath();
}
