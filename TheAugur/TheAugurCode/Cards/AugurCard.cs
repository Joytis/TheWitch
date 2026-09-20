using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using TheAugur.TheAugurCode.Character;
using TheAugur.TheAugurCode.Extensions;

namespace TheAugur.TheAugurCode.Cards;

/// <summary>
/// Base class for every Augur card: binds the character pool and resolves the packed portrait slice
/// (tools/pack-atlases.py --character Augur) from the card's model id.
/// </summary>
[Pool(typeof(AugurCardPool))]
public abstract class AugurCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".CardAtlasPath();
    public override string PortraitPath => CustomPortraitPath;
    public override string BetaPortraitPath => CustomPortraitPath;
}
