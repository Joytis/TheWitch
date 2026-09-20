using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Bird.BirdDebug;

/// <summary>
/// Entry point of the dev-only BirdDebug mod. It owns every "-bird-*" launch flag (see
/// <see cref="BirdDebug"/>) for whichever character mod <c>-bird-character=&lt;key&gt;</c> names,
/// so the character mods ship none of the debug harness. No assets, no pck.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "BirdDebug";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        BirdDebug.ApplyPatches(harmony);
    }
}
