using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using TheWitch.TheWitchCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Relics;
using TheWitch.TheWitchCode.Config;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Players;
using TheWitch.TheWitchCode.Potions;
using MegaCrit.Sts2.Core.Commands;
using BaseLib.Patches.UI;

namespace TheWitch.TheWitchCode.Character;

public class Witch : PlaceholderCharacterModel
{
    public const string CharacterId = "Witch";
    
    // Colors
    public static readonly Color Color = new("5E2626");
    public static readonly Color DarkColor = new("3D1714FF");
    public override Color NameColor => Color;
    public override Color MapDrawingColor => new("632626"); // Color darkened 30% for map-pen legibility
    public override Color RemoteTargetingLineColor => Color;
    public override Color RemoteTargetingLineOutline => DarkColor;
    public override Color EnergyLabelOutlineColor => DarkColor;

    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

    public static bool Turbo => WitchConfig.EffectiveTurboWitchery;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<Harvest>(),
        ModelDb.Card<Incant>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<LargePockets>()
    ];
    
    // Preload the non-globally-preloaded vfx scenes used by WitchFx signatures (powers have no
    // ExtraRunAssetPaths hook, so power-spawned vfx like the Bramble slice must preload here).
    protected override IEnumerable<string> ExtraAssetPaths => [
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NPowerUpVfx.AssetPaths,
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NThinSliceVfx.AssetPaths,
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NGroundFireVfx.AssetPaths,
        // MoonlightPower re-fires the beam every turn, so it can't rely on the card's own assets.
        Vfx.NMoonbeamVfx.scenePath,
        .. Potions.NUnstablePotionVfx.AssetPaths,
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<WitchCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<WitchRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<WitchPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public static async Task<PotionProcureResult> ProducePotion<T>(Player player, PotionMode mode = PotionMode.Stable)
        where T : PotionModel
    {
        return await ProducePotion(ModelDb.Potion<T>(), player, mode);
    }

    public enum PotionMode
    {
        Stable,
        Unstable
    }

    /// <summary>
    /// Grants a potion, optionally marked Unstable. Takes the CANONICAL model — the mutable clone
    /// is made here, and passing an already-mutable one throws in AssertCanonical.
    /// </summary>
    public static async Task<PotionProcureResult> ProducePotion(PotionModel potion, Player player, PotionMode mode = PotionMode.Stable, int slotIndex = -1)
    {
        PotionProcureResult result = await PotionCmd.TryToProcure(potion.ToMutable(), player);
        if (result.success && mode == PotionMode.Unstable)
        {
            UnstablePotions.Mark(result.potion);
        }
        return result;
    }

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

    public override string CustomEnergyCounterPath => "witch_energy_counter.tscn".CharacterScenePath();
    public override string CustomVisualPath => "witch_visuals.tscn".CharacterScenePath();
    public override string CustomCharacterSelectBg => "char_select_bg_witch.tscn".CharacterScenePath();
    public override string CustomTrailPath => "card_trail_witch.tscn".CharacterScenePath();
    public override string CustomRestSiteAnimPath => "witch_rest_site.tscn".CharacterScenePath();
    // SDF wipe played by NTransition.FadeOut when embarking from character select.
    public override string CustomCharacterSelectTransitionPath => "witch_transition_mat.tres".ShaderPath();
    public override string CustomMerchantAnimPath => "witch_merchant.tscn".CharacterScenePath();

    // Audio — Ironclad stand-ins until the Witch gets her own FMOD events.
    public override string CustomAttackSfx => "event:/sfx/characters/ironclad/ironclad_attack";
    public override string CustomCastSfx => "event:/sfx/characters/ironclad/ironclad_cast";
    public override string CustomDeathSfx => "event:/sfx/characters/ironclad/ironclad_die";

    // Arms (events / rock-paper-scissors)
    public override string CustomArmPointingTexturePath => "multiplayer_hand_witch_point.png".CharacterUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_witch_rock.png".CharacterUiPath();
    public override string CustomArmPaperTexturePath => "multiplayer_hand_witch_paper.png".CharacterUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_witch_scissors.png".CharacterUiPath();
    public override string CharacterSelectSfx => "witch_screen_selection.wav".CharacterSfxPath();

    // Witch-flavored Yummy Cookie: BaseLib swaps the base relic's art only while a mutable instance is
    // owned by a Witch. Source art at relics/yummy_cookie.png (256) — packed + outlined like any relic.
    public override RelicIconData CustomYummyCookie => new(
        "yummy_cookie.png".BigRelicImagePath(),
        "yummy_cookie.tres".RelicAtlasPath(),
        "yummy_cookie_outline.tres".RelicOutlineAtlasPath()
    );
    
    // Anims / visuals
    // The game calls this once per combat creature node, with her Spine body: the stock animator
    // (idle_loop / attack / cast / hurt / die on track 0) plus her blink on track 1
    // and her idle tail fwip on track 2.
    // Same state graph as CharacterModel.GenerateAnimator, except a loop the rig doesn't have
    // (low_health_loop, relaxed_loop) falls back to idle_loop. Stock, at <= 25% HP every return to
    // idle targets low_health_loop; with it missing nothing is queued after cast/hurt, so the body
    // freezes on that animation's last frame until the next trigger ("idle stops sometimes").
    public override CreatureAnimator GenerateAnimator(MegaSprite controller, MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        string LoopOrIdle(string name) => controller.HasAnimation(name) ? name : AnimState.idleAnim;

        AnimState idle = new(AnimState.idleAnim, isLooping: true);
        AnimState lowHealthIdle = new(LoopOrIdle(AnimState.lowHealthIdleAnim), isLooping: true);
        AnimState relaxed = new(LoopOrIdle("relaxed_loop"), isLooping: true);
        AnimState dead = new(AnimState.dieAnim);
        CreatureAnimator animator = new(IsLowHealth(creature) ? lowHealthIdle : idle, controller);
        animator.AddAnyState("Idle", idle, () => !IsLowHealth(creature));
        animator.AddAnyState("Idle", lowHealthIdle, () => IsLowHealth(creature));
        animator.AddAnyState("Relaxed", relaxed);
        animator.AddAnyState("Dead", dead);
        foreach ((AnimState state, string trigger) in AnimationStates)
        {
            state.AddNextState(idle, () => !IsLowHealth(creature));
            state.AddNextState(lowHealthIdle, () => IsLowHealth(creature));
            animator.AddAnyState(trigger, state);
        }

        WitchBlink.Attach(controller);
        WitchTailFwip.Attach(controller);
        return animator;
    }

    /*
    // Anims / visuals
    // public override float DeathAnimTime => ...;
    // public override NCreatureVisuals CreateCustomVisuals() => ...;
    // public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) => ...;
    // public override void RegisterSceneConversions() => ...;


    // Currently provided by PlaceholderCharacterModel — must supply once off the placeholder base:
    // public override string CharacterTransitionSfx => ...;
    // public override List<string> GetArchitectAttackVfx() => ...;
    */
}