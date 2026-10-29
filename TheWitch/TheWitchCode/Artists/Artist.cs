using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace TheWitch.TheWitchCode.Artists;

/// <summary>
/// Artist credit for card art. Cards are tagged via <see cref="IArtCredited.ArtBy" />
/// (e.g. <c>Artist.Joytis</c>); Kitsu is the base-class default. Shown in game as an
/// "Art by …" hover tip on the card (<see cref="ArtistCreditPatches" />, Downfall's shape), gated by the
/// Artist Credits config. <see cref="Credit" /> is the in-game credit text (defaults to <see cref="Name" />);
/// <see cref="Url" /> is the artist's link for the Workshop credits.
/// </summary>
public sealed record Artist(string Name, string? Handle = null, string? Url = null)
{
    public string Credit => Handle ?? Name;

    public IHoverTip HoverTip
    {
        get
        {
            var title = new LocString("static_hover_tips", "THEWITCH-ART_BY.title");
            title.Add("name", Credit);
            return new ArtistHoverTip(title);
        }
    }

    public static readonly Artist Kitsu = new("Kitsu");
    public static readonly Artist JakeRuesch = new("Jake Ruesch");
    public static readonly Artist Phenelia = new("Phenelia");
    public static readonly Artist Joytis = new("Joytis");
    public static readonly Artist BobbyHessler = new("Bobby Hessler");
    public static readonly Artist KaiThePhaux = new("KaiThePhaux");
    public static readonly Artist Vuwa = new("Vuwa", "@via.lacroix", "https://vuwa.art/");
}

/// <summary>A card base that carries an artist credit (<see cref="Cards.WitchCard" />, <see cref="Cards.WitchFamiliarCard" />).</summary>
public interface IArtCredited
{
    /// <summary>Artist credit for this card's art; null for uncredited.</summary>
    Artist? ArtBy { get; }
}
