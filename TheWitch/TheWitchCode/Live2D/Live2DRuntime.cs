using Godot;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// PROTOTYPE. Live2D support via our fork of the GDCubism GDExtension
/// (github.com/MizunagiKB/gd_cubism, built by <c>tools/live2d/setup-live2d.ps1</c>).
///
/// The fork prefixes every class (<c>GDCubismUserModel</c> -> <see cref="ModelClass"/>) and loads its
/// shaders from <see cref="ShaderResDir"/>, so another mod shipping stock GDCubism can't collide
/// with it: Godot keeps whichever extension registers a class name first, and a later pck
/// overrides earlier files at the same res:// path.
///
/// The extension is a native dll, which cannot live inside our .pck, so it sits on disk next to
/// TheWitch.dll (copied by the csproj on build, shipped by bundle-workshop.ps1):
/// <code>
///   mods/TheWitch/live2d/witch_cubism.gdextension
///   mods/TheWitch/live2d/bin/libwitch_cubism.windows.release.x86_64.dll
///   mods/TheWitch/live2d/bin/libwitch_cubism.linux.release.x86_64.so
///   mods/TheWitch/live2d/bin/libwitch_cubism.macos.release.framework   (universal: arm64 + x86_64)
/// </code>
/// The extension is never registered with the game's project; it is loaded on first use with
/// <see cref="GDExtensionManager.LoadExtension"/> and its nodes are made via <see cref="ClassDB"/>.
/// Models live in the pck under <see cref="ModelResDir"/> (GDCubism reads them through FileAccess):
/// loose model files can't go under mods/, the game's ModManager parses every *.json there as a
/// mod manifest.
///
/// Required: the Witch has no static sprite, so an unsupported platform or a missing
/// extension/library/model leaves her invisible, logged as an error.
/// </summary>
internal static class Live2DRuntime
{
    /// <summary>The fork's class-name prefix (stock GDCubism uses <c>GDCubism</c>).</summary>
    public const string ClassPrefix = "WitchCubism";
    public const string ModelClass = $"{ClassPrefix}UserModel";

    private static bool? _loaded;

    public const string ModelResDir = $"{MainFile.ResPath}/live2d";

    /// <summary>Where the fork loads its shaders from; setup-live2d.ps1 patches this path in.</summary>
    public const string ShaderResDir = $"{ModelResDir}/shader";

    public static string Live2DDir =>
        Path.Combine(Path.GetDirectoryName(typeof(MainFile).Assembly.Location)!, "live2d").Replace('\\', '/');

    /// <summary>Loads the GDCubism extension once; false (logged) when it is missing or fails.</summary>
    public static bool EnsureLoaded()
    {
        if (_loaded.HasValue)
        {
            return _loaded.Value;
        }

        // Libraries are built for these (tools/live2d/setup-live2d.ps1, live2d-build workflow);
        // elsewhere LoadExtension would only log an engine error.
        if (!IsSupportedPlatform())
        {
            MainFile.Logger.Error($"[live2d] no Live2D build for {OS.GetName()} {Engine.GetArchitectureName()}; the Witch will be invisible");
            _loaded = false;
            return false;
        }

        string extensionPath = $"{Live2DDir}/witch_cubism.gdextension";
        if (!File.Exists(extensionPath))
        {
            MainFile.Logger.Error($"[live2d] {extensionPath} not found; the Witch will be invisible");
            _loaded = false;
            return false;
        }

        GDExtensionManager.LoadStatus status = GDExtensionManager.LoadExtension(extensionPath);
        _loaded = (status is GDExtensionManager.LoadStatus.Ok or GDExtensionManager.LoadStatus.AlreadyLoaded)
                  && ClassDB.ClassExists(ModelClass);
        MainFile.Logger.Info($"[live2d] load {extensionPath}: {status}, {ModelClass} registered: {ClassDB.ClassExists(ModelClass)}");
        return _loaded.Value;
    }

    private static bool IsSupportedPlatform() => (OS.GetName(), Engine.GetArchitectureName()) switch
    {
        ("Windows", "x86_64") or ("Linux", "x86_64") => true,
        ("macOS", "arm64" or "x86_64") => true,
        _ => false,
    };

    /// <summary>
    /// The <c>*.model3.json</c> in <c>res://TheWitch/live2d/&lt;modelName&gt;/</c> (committed like other
    /// art), or null (logged as an error).
    /// </summary>
    public static string? FindModelJson(string modelName)
    {
        string dir = $"{ModelResDir}/{modelName}";
        string? file = DirAccess.DirExistsAbsolute(dir)
            ? DirAccess.GetFilesAt(dir).FirstOrDefault(f => f.EndsWith(".model3.json"))
            : null;
        if (file != null)
        {
            return $"{dir}/{file}";
        }
        MainFile.Logger.Error($"[live2d] no *.model3.json under {dir}");
        return null;
    }
}
