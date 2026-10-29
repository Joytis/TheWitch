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
/// </code>
/// The extension is never registered with the game's project; it is loaded on first use with
/// <see cref="GDExtensionManager.LoadExtension"/> and its nodes are made via <see cref="ClassDB"/>.
/// Models live in the pck under <see cref="ModelResDir"/> (GDCubism reads them through FileAccess):
/// loose model files can't go under mods/, the game's ModManager parses every *.json there as a
/// mod manifest.
///
/// On whenever the extension and a model are present (Windows x64 only, the one platform built);
/// otherwise the static sprite stays.
/// </summary>
internal static class Live2DRuntime
{
    public const string ModelName = "witch";

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

        // Only a Windows x64 library is built; elsewhere LoadExtension would just log an engine error.
        if (OS.GetName() != "Windows" || Engine.GetArchitectureName() != "x86_64")
        {
            MainFile.Logger.Info($"[live2d] no build for {OS.GetName()} {Engine.GetArchitectureName()}; using the static sprite");
            _loaded = false;
            return false;
        }

        string extensionPath = $"{Live2DDir}/witch_cubism.gdextension";
        if (!File.Exists(extensionPath))
        {
            MainFile.Logger.Info($"[live2d] {extensionPath} not found; using the static sprite");
            _loaded = false;
            return false;
        }

        GDExtensionManager.LoadStatus status = GDExtensionManager.LoadExtension(extensionPath);
        _loaded = (status is GDExtensionManager.LoadStatus.Ok or GDExtensionManager.LoadStatus.AlreadyLoaded)
                  && ClassDB.ClassExists(ModelClass);
        MainFile.Logger.Info($"[live2d] load {extensionPath}: {status}, {ModelClass} registered: {ClassDB.ClassExists(ModelClass)}");
        return _loaded.Value;
    }

    /// <summary>
    /// The <c>*.model3.json</c> in <c>res://TheWitch/live2d/witch/</c> (the real model, committed like
    /// other art), else in the first folder under <c>.../live2d/samples/</c> (SDK samples, local-only),
    /// or null.
    /// </summary>
    public static string? FindModelJson()
    {
        string samplesDir = $"{ModelResDir}/samples";
        IEnumerable<string> samples = DirAccess.DirExistsAbsolute(samplesDir)
            ? DirAccess.GetDirectoriesAt(samplesDir).Order().Select(d => $"{samplesDir}/{d}")
            : [];
        foreach (string dir in samples.Prepend($"{ModelResDir}/{ModelName}"))
        {
            if (!DirAccess.DirExistsAbsolute(dir))
            {
                continue;
            }
            string? file = DirAccess.GetFilesAt(dir).FirstOrDefault(f => f.EndsWith(".model3.json"));
            if (file != null)
            {
                return $"{dir}/{file}";
            }
        }
        MainFile.Logger.Warn($"[live2d] no *.model3.json under {ModelResDir}/{ModelName} or {samplesDir}");
        return null;
    }
}
