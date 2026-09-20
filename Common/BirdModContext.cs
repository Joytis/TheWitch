using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Bird.Common;

/// <summary>
/// Everything the shared <c>Common/</c> code needs to know about the character mod it is compiled
/// into. <c>Common/**</c> is source-included by every character csproj (no shared dll, no extra
/// Workshop dependency), so each mod builds its own copy of these classes and hands them this
/// context from its <c>MainFile.Initialize</c>.
/// </summary>
public sealed class BirdModContext
{
    /// <summary>Manifest id / assembly name / res:// root, e.g. "TheWitch".</summary>
    public required string ModId { get; init; }

    /// <summary>Localization key prefix, e.g. "THEWITCH" (keys look like THEWITCH-WORKSHOP_UPDATE.header).</summary>
    public required string LocPrefix { get; init; }

    public required Logger Logger { get; init; }

    /// <summary>The character's key for <c>-bird-character</c>, e.g. "Witch" (the CharacterId constant).</summary>
    public required string CharacterKey { get; init; }

    /// <summary>The character this mod adds. Resolved lazily: ModelDb is not populated at mod init.</summary>
    public required Func<CharacterModel> Character { get; init; }

    /// <summary>The mod's own "upload analytics" user toggle; null = always on.</summary>
    public Func<bool>? AnalyticsEnabled { get; init; }
}
