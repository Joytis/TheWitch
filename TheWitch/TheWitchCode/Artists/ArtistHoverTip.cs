using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Artists;

/// <summary>
/// Title-only "Art by …" tip. Not a base-game <see cref="MegaCrit.Sts2.Core.HoverTips.HoverTip" />, so
/// <see cref="ArtistCreditPatches" /> must pull it out before <c>NHoverTipSet.Init</c> (which casts every
/// non-HoverTip to CardHoverTip) and render it itself.
/// </summary>
public sealed class ArtistHoverTip(LocString title) : IHoverTip
{
    public LocString Title => title;
    public string Id => $"ArtistHoverTip:{title.GetFormattedText()}";
    public bool IsSmart => false;
    public bool IsDebuff => false;
    public bool IsInstanced => false;
    public AbstractModel? CanonicalModel => null;
}
