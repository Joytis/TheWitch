using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using TheWitch.TheWitchCode.Config;

namespace TheWitch.TheWitchCode.Artists;

/// <summary>
/// In-game artist credits, after Downfall's AddArtistHoverTipPatch: every credited Witch card gets an
/// "Art by …" tip (title only, tinted the rose brown of the card frame, drawn above the other text tips).
/// Gated by <see cref="WitchConfig.ArtistCredits" />; skipped while a card shows the packed placeholder or beta art,
/// since <see cref="IArtCredited.ArtBy" /> credits the final art.
/// </summary>
public static class ArtistCreditPatches
{
    private static readonly AccessTools.FieldRef<NHoverTipSet, VFlowContainer> _textContainerRef =
        AccessTools.FieldRefAccess<NHoverTipSet, VFlowContainer>("_textHoverTipContainer");

    // hsv.gdshader params that rotate the base tip's slate blue onto the rose brown of the Witch card frame
    // (card_frame_red at WitchCardPool H 0 / S 0.24), lifted slightly so it sits closer to the frame's brightness.
    private static ShaderMaterial? _bgMaterial;

    private static ShaderMaterial BgMaterial
    {
        get
        {
            if (_bgMaterial != null) return _bgMaterial;
            _bgMaterial = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader") };
            _bgMaterial.SetShaderParameter("h", 0.455f);
            _bgMaterial.SetShaderParameter("s", 0.9f);
            _bgMaterial.SetShaderParameter("v", 1.3f);
            return _bgMaterial;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    private static class CardHoverTipsPatch
    {
        private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!WitchConfig.ArtistCredits || __instance is not IArtCredited { ArtBy: { } artist }) return;

            // CardAtlasPath resolves to beta/ (Beta Images config) or the packed placeholder when final art is absent.
            string portrait = __instance.PortraitPath;
            if (portrait.Contains("/beta/") || portrait.EndsWith("card_atlas.sprites/card.tres")) return;

            __result = __result.Append(artist.HoverTip);
        }
    }

    [HarmonyPatch(typeof(NHoverTipSet), "Init")]
    private static class RenderPatch
    {
        // Init casts every non-HoverTip to CardHoverTip, so credits must leave the list before it runs.
        private static void Prefix(NHoverTipSet __instance, ref IEnumerable<IHoverTip> hoverTips)
        {
            List<IHoverTip> list = hoverTips.ToList();
            List<ArtistHoverTip> credits = list.OfType<ArtistHoverTip>().DistinctBy(tip => tip.Id).ToList();
            if (credits.Count == 0) return;

            hoverTips = list.Where(tip => tip is not ArtistHoverTip).ToList();

            VFlowContainer container = _textContainerRef(__instance);
            foreach (ArtistHoverTip tip in credits)
            {
                var control = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip.tscn").Instantiate<Control>();
                container.AddChildSafely(control);
                control.GetNode<MegaLabel>("%Title").SetTextAutoSize(tip.Title.GetFormattedText());
                control.GetNode<MegaRichTextLabel>("%Description").Text = "";
                control.GetNode<TextureRect>("%Icon").Texture = null;
                control.GetNode<CanvasItem>("%Bg").Material = BgMaterial;
                control.ResetSize();

                if (NGame.Instance != null &&
                    container.Size.Y + control.Size.Y + 5f < NGame.Instance.GetViewportRect().Size.Y - 50f)
                    container.Size = new Vector2(360f, container.Size.Y + control.Size.Y + 5f);
                else
                    container.Alignment = FlowContainer.AlignmentMode.Center;
            }
        }
    }
}
