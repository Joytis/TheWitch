using BaseLib.Config;

namespace TheAugur.TheAugurCode.Config;

/// <summary>Mod settings shown in the game's mod config screen (BaseLib SimpleModConfig; loc keys in settings_ui.json).</summary>
internal class AugurConfig : SimpleModConfig
{
    /// <summary>Upload anonymous Augur run results (see Common/Data/RunAnalytics.cs). Default on; the game's
    /// own "Upload Data" setting still gates the upload upstream.</summary>
    [ConfigHoverTip]
    public static bool AnalyticsEnabled { get; set; } = true;
}
