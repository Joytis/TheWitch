using Godot;

namespace TheWitch.TheWitchCode.Live2D;

/// <summary>
/// TEMPORARY. Blend modes the artist painted (Photoshop Multiply / Linear Dodge layers in the PSDs)
/// that came out of the 2026-11-02 Cubism export as Normal: Cubism 5.3 blend modes are dropped when
/// exporting for an older SDK. Until the models are re-exported with legacy blend modes ("Multiply
/// (Before 5.3)" etc., see Docs/live2d.md), swap those parts' shader here.
///
/// Keyed by model folder, then ArtMesh id. Ids were matched to PSD layers by bounding box; a new drop
/// can renumber ids, so a stale entry is logged as a warning. Delete an entry (or this file) once the
/// export carries the blend mode itself: parts that are no longer Normal are left alone.
/// </summary>
internal static class Live2DBlendOverrides
{
    private const string Multiply = "mul";
    private const string Additive = "add";

    private static readonly Dictionary<string, Dictionary<string, string>> Overrides = new()
    {
        ["witch"] = new()
        {
            ["Shadow"] = Multiply,     // PSD "Shadow"
            ["ArtMesh8"] = Multiply,   // PSD "Hair shadow" (clipped to Head)
            ["ArtMesh0"] = Additive,   // PSD "Gem glow"
            ["ArtMesh"] = Additive,    // PSD "Eye glow"
        },
        ["witch_shop"] = new()
        {
            ["Shadow"] = Multiply,     // PSD "Shadow"
            ["ArtMesh5"] = Multiply,   // PSD "Hair shadow" (clipped to Head)
            ["ArtMesh"] = Additive,    // PSD "Gem glow"
        },
        ["witch_rest"] = new()
        {
            ["Shadow"] = Multiply,     // PSD "Shadow"
            ["Shade"] = Multiply,      // PSD "Shade" (clipped to Character)
            ["ArtMesh"] = Additive,    // PSD "Fire glow 1"
            ["ArtMesh0"] = Additive,   // PSD "Fire glow hat"
            ["ArtMesh24"] = Additive,  // PSD "Fire glow ears"
            ["ArtMesh25"] = Additive,  // PSD "Fire glow chest"
            ["ArtMesh26"] = Additive,  // PSD "Fire glow body"
            ["ArtMesh27"] = Additive,  // PSD "Fire glow back leg"
            ["ArtMesh28"] = Additive,  // PSD "Fire glow front leg"
            ["ArtMesh29"] = Additive,  // PSD "Fire glow hair back"
            ["ArtMesh30"] = Additive,  // PSD "Fire glow snoot"
            ["ArtMesh31"] = Additive,  // PSD "Fire glow front hair"
        },
    };

    /// <summary>
    /// Swaps the shader of each overridden part that is still Normal (2d_cubism_*_mix*) for the
    /// matching Multiply/Additive variant (masked and inverted-mask variants keep their kind). Call
    /// once the model has built its parts (its first update). GDCubism only sets shader parameters
    /// per frame, never the shader, so the swap sticks.
    /// </summary>
    public static void Apply(Node2D model, string modelName)
    {
        if (!Overrides.TryGetValue(modelName, out Dictionary<string, string>? parts))
        {
            return;
        }

        var found = new HashSet<string>();
        foreach (MeshInstance2D part in model.FindChildren("*", nameof(MeshInstance2D), recursive: false, owned: false).OfType<MeshInstance2D>())
        {
            if (!parts.TryGetValue(part.Name, out string? blend)
                || part.Material is not ShaderMaterial { Shader: { } shader } material)
            {
                continue;
            }
            found.Add(part.Name);
            string file = shader.ResourcePath.GetFile();
            if (!file.Contains("_mix"))
            {
                continue; // the export already carries a blend mode; leave it
            }
            material.Shader = GD.Load<Shader>($"{Live2DRuntime.ShaderResDir}/{file.Replace("_mix", "_" + blend)}");
        }

        string[] stale = parts.Keys.Where(p => !found.Contains(p)).ToArray();
        if (stale.Length > 0)
        {
            MainFile.Logger.Warn($"[live2d] {modelName}: blend overrides for missing parts {string.Join(", ", stale)} (ids changed? re-match them to the PSD)");
        }
    }
}
