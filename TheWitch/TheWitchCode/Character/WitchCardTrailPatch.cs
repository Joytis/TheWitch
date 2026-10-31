using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Fallback builder for the Witch's card trail. card_trail_witch.tscn attaches the game's
/// NCardTrailVfx/NCardTrail scripts directly (res://src/... resolves through the gamedata src/
/// junction at export, the same way Downfall's trail scenes do), so it normally instantiates as
/// the real thing and this prefix only wires _nodeToFollow. If an export ever strips those script
/// references again, the scene comes in as plain nodes and Instantiate&lt;NCardTrailVfx&gt; would
/// throw — then this rebuilds the root as an NCardTrailVfx and swaps every Line2D for an NCardTrail.
/// (The scripts must stay on the scene: RitsuLib's Priority.First prefix on Create claims every
/// existing trail scene and skips ours, and its converter does not turn Line2D into NCardTrail.)
/// </summary>
[HarmonyPatch(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create))]
public static class WitchCardTrailPatch
{
    private static readonly string TrailPath = "card_trail_witch.tscn".CharacterScenePath();

    private static readonly FieldInfo? NodeToFollowField =
        AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow");

    private static bool Prefix(Control card, string characterTrailPath, ref NCardTrailVfx? __result)
    {
        if (characterTrailPath != TrailPath || NodeToFollowField == null)
        {
            return true;
        }

        if (TestMode.IsOn)
        {
            __result = null;
            return false;
        }

        try
        {
            Node source = PreloadManager.Cache.GetScene(characterTrailPath)
                .Instantiate(PackedScene.GenEditState.Disabled);

            // Normal case: the scene's scripts survived the export, so it is already the
            // real thing — just wire it up. Otherwise convert the plain nodes.
            if (source is not NCardTrailVfx vfx)
            {
                vfx = new NCardTrailVfx();
                CopyStorageProperties(source, vfx);
                foreach (Node child in source.GetChildren())
                {
                    source.RemoveChild(child);
                    vfx.AddChild(child);
                }
                source.QueueFree();
                // Owners still point at the freed source scene root; ReplaceBy copies them
                // onto the new nodes and Godot rejects a non-ancestor owner (error spam).
                ClearOwners(vfx);
                ConvertLinesToTrails(vfx);
            }

            NodeToFollowField.SetValue(vfx, card);
            __result = vfx;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Witch card trail construction failed: {e.Message}");
            __result = null; // callers already handle the TestMode null
        }

        return false;
    }

    private static void ClearOwners(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            child.Owner = null;
            ClearOwners(child);
        }
    }

    /// <summary>Replaces every plain Line2D in the subtree with an NCardTrail clone.</summary>
    private static void ConvertLinesToTrails(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Line2D line and not NCardTrail)
            {
                var trail = new NCardTrail();
                CopyStorageProperties(line, trail);
                line.ReplaceBy(trail);
                line.QueueFree();
            }
            else
            {
                ConvertLinesToTrails(child);
            }
        }
    }

    /// <summary>
    /// Copies every serialized (storage) property except the script binding — the generic
    /// equivalent of BaseLib's per-type property copies, so scene edits (widths, gradients,
    /// new emitters) never require touching this patch.
    /// </summary>
    private static void CopyStorageProperties(GodotObject from, GodotObject to)
    {
        foreach (var prop in from.GetPropertyList())
        {
            var usage = (PropertyUsageFlags)prop["usage"].As<long>();
            if (!usage.HasFlag(PropertyUsageFlags.Storage))
            {
                continue;
            }

            string name = prop["name"].As<string>();
            if (name == "script")
            {
                continue;
            }

            to.Set(name, from.Get(name));
        }
    }
}
