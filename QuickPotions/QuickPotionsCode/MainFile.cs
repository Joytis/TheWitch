using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Bird.QuickPotions;

/// <summary>
/// Entry point of the QuickPotions mod: right-click quick use for every character's potions.
/// The behavior lives in the shared <see cref="Bird.Common.QoL.PotionQuickUse"/> (TheWitch ships
/// the same file gated to the Witch; with both installed they dedupe per click).
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "QuickPotions";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        if (Bird.Common.QoL.PotionQuickUse.Install(harmony, _ => true, Logger))
        {
            Logger.Info("Right-click potion quick use installed.");
        }
    }
}
