using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TheWitch.TheWitchCode.Extensions;

/// <summary>
/// Shared Witch sfx/vfx mechanic signatures. See Docs/sfx-vfx-catalog.md for the
/// base-game asset catalog and Docs/sfx-vfx-proposal.md for the house palette.
/// </summary>
public static class WitchFx
{
    public const string SummonSfx = "event:/sfx/characters/necrobinder/necrobinder_summon";
    public const string HexSfx = "doom_apply.mp3";

    /// <summary>Poison-green tint shared by brew/bramble effects (base-game Noxious Fumes green).</summary>
    public static readonly Color WitchGreen = new("83eb85");

    private static void Attach(Node2D? vfx) => NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);

    /// <summary>Familiar summon signature: ghostly aura + summon sting. NPowerUpVfx self-attaches.
    /// Cards using this must include NPowerUpVfx.AssetPaths in ExtraRunAssetPaths.</summary>
    public static void SummonFlourish(Creature owner)
    {
        NPowerUpVfx.CreateGhostly(owner);
        SfxCmd.Play(SummonSfx);
    }

    /// <summary>Potion-creation signature: fiery smoke puff on the brewer (creation, not flatulence).
    /// Cards using this must include NFireSmokePuffVfx.AssetPaths in ExtraRunAssetPaths.
    /// Optional tint recolors the cloud material the same way the base game's Purple
    /// smoke-puff treatment does (duplicated material, narrowed hue variation).</summary>
    public static void BrewPuff(Creature owner, Color? tint = null)
    {
        NFireSmokePuffVfx? vfx = NFireSmokePuffVfx.Create(owner);
        if (vfx != null && tint is Color color)
        {
            var clouds = vfx.GetNode<GpuParticles2D>("Clouds");
            var mat = (ParticleProcessMaterial)clouds.ProcessMaterial.Duplicate();
            mat.HueVariationMin = -0.02f;
            mat.HueVariationMax = 0.02f;
            mat.Color = color;
            clouds.ProcessMaterial = mat;
        }
        Attach(vfx);
    }

    /// <summary>Bramble-gain signature: green spore burst on the gainer (globally preloaded).</summary>
    public static void SporePuff(Creature owner) => Attach(NSporeImpactVfx.Create(owner, WitchGreen));

    /// <summary>Bramble retaliation/thorn hit: swamp-green thin slice on the target.
    /// NThinSliceVfx is NOT globally preloaded — every Bramble-granting card registers it.</summary>
    public static void BrambleSlice(Creature target) => Attach(NThinSliceVfx.Create(target, VfxColor.Swamp));

    /// <summary>Hex signature: occult gaze + doom sting on the target (globally preloaded).</summary>
    public static void HexGaze(Creature target)
    {
        VfxCmd.PlayOnCreatureCenter(target, VfxCmd.gazePath);
        NDebugAudioManager.Instance?.Play(HexSfx);
    }

    /// <summary>Green gas burst (globally preloaded) — plague/rot effects.</summary>
    public static void GreenGas(Creature target) => Attach(NGaseousImpactVfx.Create(target, WitchGreen));

    /// <summary>Sparkle sting for in-combat card/potion upgrades (base NCardUpgradeVfx is silent).</summary>
    public static void EnchantShimmer() => SfxCmd.Play("event:/sfx/ui/enchant_shimmer");

    /// <summary>Potion-splash visual with custom tint.</summary>
    public static void Splash(Creature target, Color tint) => NCombatRoom.Instance?.PlaySplashVfx(target, tint);
}
