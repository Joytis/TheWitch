using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace TheAugur.TheAugurCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "TheAugur"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Register this assembly's [ScriptPath] classes with Godot so scenes/resources in our .pck
        // can bind mod C# scripts by res:// path.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Shared Common/ services (source-included): run analytics + Workshop self-update.
        Bird.Common.BirdModContext ctx = new()
        {
            ModId = ModId,
            LocPrefix = "THEAUGUR",
            Logger = Logger,
            CharacterKey = Character.Augur.CharacterId,
            Character = () => MegaCrit.Sts2.Core.Models.ModelDb.Character<Character.Augur>(),
        };
        Bird.Common.Data.RunAnalytics.Initialize(ctx);
        Bird.Common.Steam.WorkshopSelfUpdate.Initialize(ctx);

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
