using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Registry + Harmony patches behind the Mushroom Extract potion. A "mushroomed" card shows random-letter
/// gibberish for its name and description and uses the <c>mystery.png</c> art — only that specific card
/// instance, only visually (it stays fully playable). The gibberish is generated once per card and stored,
/// so it's stable for the rest of the combat; every mark is dropped when the combat ends.
///
/// The disguise is enforced in overlapping layers, so that losing any one of them does not show the real card:
/// <list type="number">
/// <item><b>Model getters</b> (Title / description / hover tips / Portrait, Harmony) — covers every consumer that
/// is not an <see cref="NCard" />. NOT sufficient for the card face: these patches can be absent in a player's
/// game (another mod's broken patch on the same getter makes ours fail to apply; a later postfix can overwrite
/// the result), and <c>Title</c> is virtual, so a card that overrides it skips the patch. When the Portrait
/// patch is missing, the game writes the real art on every card refresh (measured: thousands of times).</item>
/// <item><b>Model path</b> (<see cref="PortraitPathFor" />, no Harmony) — the mod's own card bases report the
/// mystery slice as their portrait path while mushroomed.</item>
/// <item><b>View</b> (<see cref="MushroomedCardViewPatch" />) — after ANY <see cref="NCard" /> method that writes
/// the portrait or text nodes, the face is forced back. The targets are discovered from the game's IL, so a
/// writer added by a game update (or one that exists on only one game branch) is covered without touching this
/// file.</item>
/// <item><b>Clones</b> (<see cref="MushroomedClonePatch" />) — the game shows <c>MutableClone()</c>s of a card in
/// the inspect screen, upgrade previews and card hover tips; a clone inherits its source's disguise.</item>
/// </list>
///
/// Card cost-0 is handled separately by the potion via <c>CardModel.SetToFreeThisCombat()</c>; this file only
/// owns the cosmetic override.
/// </summary>
public static class MushroomedCards
{
    public sealed record Gibberish(string Title, string Description);

    private static readonly ConditionalWeakTable<CardModel, Gibberish> Store = new();

    private static readonly AccessTools.FieldRef<CardModel, DynamicVarSet?> DynamicVarsField =
        AccessTools.FieldRefAccess<CardModel, DynamicVarSet?>("_dynamicVars");

    /// <summary>False whenever <see cref="Store" /> is empty — keeps the clone hook free outside a mushroomed combat.</summary>
    private static volatile bool _any;

    private static readonly string[] Syllables =
    [
        "bo", "gru", "mish", "fa", "lor", "nak", "wim", "ble", "zu", "thra", "glor", "pfo", "oo", "ka", "tz",
        "ip", "mun", "dle", "sev", "qua", "rin", "tho", "blu", "grik", "sna", "fenn", "wob", "plo", "dox", "yib",
    ];

    static MushroomedCards()
    {
        // The disguise lasts "for the rest of combat". A clone can outlive the combat's own card instances,
        // so expire the marks explicitly instead of relying on the weak table alone.
        CombatManager.Instance.CombatSetUp += _ => Clear();
        CombatManager.Instance.CombatEnded += _ => Clear();
    }

    /// <summary>Tag <paramref name="card" /> as mushroomed, generating + storing its gibberish text once.</summary>
    public static void Mark(CardModel card, Rng rng)
    {
        if (Store.TryGetValue(card, out _))
        {
            return;
        }
        Store.Add(card, Generate(rng));
        _any = true;
        RefreshView(card);
    }

    /// <summary>Remove the disguise from <paramref name="card" /> (it was marked but never ended up mushroomed).</summary>
    public static void Unmark(CardModel card)
    {
        if (Store.Remove(card))
        {
            RefreshView(card);
        }
    }

    public static bool TryGet(CardModel card, out Gibberish gibberish) => Store.TryGetValue(card, out gibberish!);

    /// <summary>
    /// Called on a fresh clone BEFORE <c>CardModel.DeepCloneFields</c> runs: at that point the clone is still a
    /// memberwise copy, so it shares its source's <see cref="DynamicVarSet" /> reference — the only way to tell
    /// which card it was cloned from. If that source is mushroomed, the clone becomes mushroomed too.
    /// </summary>
    internal static void InheritFromCloneSource(CardModel clone)
    {
        if (!_any || DynamicVarsField(clone) is not { } vars)
        {
            return;
        }
        foreach ((CardModel card, Gibberish gibberish) in Store)
        {
            if (!ReferenceEquals(card, clone) && ReferenceEquals(DynamicVarsField(card), vars))
            {
                Store.AddOrUpdate(clone, gibberish);
                return;
            }
        }
    }

    private static void Clear()
    {
        Store.Clear();
        _any = false;
    }

    /// <summary>Redraw a card that is already on the table, so a (un)mark made after its view exists shows at once.</summary>
    private static void RefreshView(CardModel card)
    {
        if (NCard.FindOnTable(card) is { } view && view.IsNodeReady())
        {
            view.UpdateVisuals(view.DisplayingPile, CardPreviewMode.Normal);
        }
    }

    private static Texture2D? _mystery;

    /// <summary>Atlas slice of the shared "mystery" art (falls back to the placeholder slice if missing).</summary>
    public static string MysteryPortraitPath => "mystery.tres".CardAtlasPath();

    /// <summary>The shared "mystery" portrait used for every mushroomed card.</summary>
    public static Texture2D? MysteryPortrait =>
        _mystery ??= ResourceLoader.Load<Texture2D>(MysteryPortraitPath, null, ResourceLoader.CacheMode.Reuse);

    /// <summary>
    /// The portrait path a card should report: the mystery slice while it is mushroomed, else <paramref name="realPath" />.
    /// The mod's own card bases route their path overrides through this, so for Witch cards the disguised art
    /// comes from the model itself and does not depend on any Harmony patch being applied or honoured.
    /// </summary>
    public static string PortraitPathFor(CardModel card, string realPath) =>
        Store.TryGetValue(card, out _) ? MysteryPortraitPath : realPath;

    private static Gibberish Generate(Rng rng)
    {
        int titleWords = 1 + rng.NextInt(2); // 1–2 words
        string title = string.Join(' ', Range(titleWords, () => Word(rng, 2 + rng.NextInt(3))));

        int sentences = 2 + rng.NextInt(3); // 2–4 "sentences"
        string description = string.Join(' ', Range(sentences, () =>
        {
            int words = 3 + rng.NextInt(4); // 3–6 words
            return string.Join(' ', Range(words, () => Word(rng, 1 + rng.NextInt(3)))) + ".";
        }));

        return new Gibberish(title, description);
    }

    private static IEnumerable<string> Range(int count, Func<string> make)
    {
        for (int i = 0; i < count; i++)
        {
            yield return make();
        }
    }

    private static string Word(Rng rng, int syllables)
    {
        string word = string.Concat(Range(syllables, () => Syllables[rng.NextInt(Syllables.Length)]));
        return char.ToUpperInvariant(word[0]) + word[1..];
    }
}

[HarmonyPatch(typeof(CardModel), "get_Title")]
internal static class MushroomedTitlePatch
{
    [HarmonyPriority(Priority.Last)] // after any other mod's postfix, so nobody re-writes the result behind us
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Title;
        }
    }
}

/// <summary>
/// Targets the PRIVATE 3-arg <c>GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)</c> — the real
/// body that every description path (hand, hover previews, upgrade preview) funnels into. The public 2-arg overload
/// is a one-line forwarder that the JIT inlines into <c>NCard</c> once it goes hot (tier-1 rejit), which silently
/// bypasses a patch placed on it — the "Mushroom Extract sometimes shows the real text" bug.
/// </summary>
[HarmonyPatch]
internal static class MushroomedDescriptionPatch
{
    private static MethodBase TargetMethod()
    {
        Type previewType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType")
            ?? throw new MissingMemberException("CardModel.DescriptionPreviewType not found");
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [typeof(PileType), previewType, typeof(Creature)])
            ?? throw new MissingMethodException("CardModel.GetDescriptionForPile(PileType, DescriptionPreviewType, Creature) not found");
    }

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Description;
        }
    }
}

[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class MushroomedHoverTipsPatch
{
    [HarmonyPriority(Priority.Last)] // must also run after ArtistCreditPatches appends its "Art by" tip
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        // Keyword/status hover tips would leak the real card's identity through the gibberish.
        if (MushroomedCards.TryGet(__instance, out _))
        {
            __result = [];
        }
    }
}

[HarmonyPatch(typeof(CardModel), "get_Portrait")]
internal static class MushroomedPortraitPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (MushroomedCards.TryGet(__instance, out _) && MushroomedCards.MysteryPortrait is { } mystery)
        {
            __result = mystery;
        }
    }
}

/// <summary>
/// The single place the card face is enforced. Postfixes every <see cref="NCard" /> instance method that writes the
/// portrait or the title / description labels and puts the disguise back, so it no longer matters which game
/// method wrote the real face or whether a model getter patch was honoured.
///
/// What this replaces: the art was forced only after <c>Reload</c>, but <c>UpdateVisuals</c> re-reads
/// <c>Model.Portrait</c> (through <c>UpdatePortrait</c>) on every refresh, so real art written there was never
/// corrected — gibberish text over real art, the reported bug; and <c>SetPortraitTexture</c> (inspect screen,
/// newer game versions) was not covered at all.
/// </summary>
internal static class MushroomedCardViewPatch
{
    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitField =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");
    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortraitField =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientPortrait");
    private static readonly AccessTools.FieldRef<NCard, MegaLabel> TitleLabelField =
        AccessTools.FieldRefAccess<NCard, MegaLabel>("_titleLabel");
    private static readonly AccessTools.FieldRef<NCard, MegaRichTextLabel> DescriptionLabelField =
        AccessTools.FieldRefAccess<NCard, MegaRichTextLabel>("_descriptionLabel");

    /// <summary>The nodes that make up the card's identity.</summary>
    private static readonly HashSet<string> FaceFields = ["_portrait", "_ancientPortrait", "_titleLabel", "_descriptionLabel"];

    /// <summary>Calls that put content into those nodes.</summary>
    private static readonly HashSet<string> FaceWriters = ["set_Texture", "SetTextAutoSize", "set_Text"];

    /// <summary>Pool teardown clears the nodes on purpose; the next <c>Reload</c> re-enforces.</summary>
    private static readonly HashSet<string> Excluded = ["OnFreedToPool", "OnReturnedFromPool"];

    /// <summary>Method names already reported by <see cref="Enforce" /> (one log line each).</summary>
    private static readonly HashSet<string> ReportedCorrections = [];

    /// <summary>
    /// Applied by hand from <c>MainFile</c> instead of through <c>[HarmonyPatch]</c>: each target is patched on its
    /// own, so one target that cannot be patched (another mod's broken patch on the same method) does not cost the
    /// others — the entry points and the individual writers overlap on purpose.
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        HarmonyMethod postfix = new(AccessTools.Method(typeof(MushroomedCardViewPatch), nameof(AfterFaceWritten)))
        {
            priority = Priority.Last, // after every other mod's postfix on the same method
        };
        List<string> patched = [];
        foreach (MethodBase target in FindTargets())
        {
            try
            {
                harmony.Patch(target, postfix: postfix);
                patched.Add(target.Name);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Mushroom Extract: could not patch NCard.{target.Name}; the card disguise may leak there: {e}");
            }
        }
        MainFile.Logger.Info($"Mushroom Extract: enforcing the card disguise after NCard.{{{string.Join(", ", patched.Order())}}}");
    }

    private static HashSet<MethodBase> FindTargets()
    {
        HashSet<MethodBase> targets = [];

        // The two entry points that rebuild the whole face are always patched, so they still cover a private
        // helper whose own postfix is lost (inlined into them, or unpatchable).
        MethodBase?[] entryPoints =
        [
            AccessTools.Method(typeof(NCard), "Reload"),
            AccessTools.Method(typeof(NCard), nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)]),
        ];
        foreach (MethodBase? entryPoint in entryPoints)
        {
            if (entryPoint != null)
            {
                targets.Add(entryPoint);
            }
            else
            {
                MainFile.Logger.Error("Mushroom Extract: an NCard entry point (Reload / UpdateVisuals) is missing; the card disguise may leak.");
            }
        }

        // Every other writer is found by reading the game's IL: any NCard method that loads a face node and
        // calls a texture / text setter.
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(NCard)))
        {
            if (!method.IsStatic && !method.IsAbstract && !method.IsGenericMethodDefinition
                && !Excluded.Contains(method.Name) && WritesFace(method))
            {
                targets.Add(method);
            }
        }
        return targets;
    }

    private static bool WritesFace(MethodInfo method)
    {
        try
        {
            if (method.GetMethodBody() == null)
            {
                return false;
            }
            bool loadsFaceNode = false;
            bool callsWriter = false;
            foreach ((OpCode opcode, object operand) in PatchProcessor.ReadMethodBody(method))
            {
                if (opcode == OpCodes.Ldfld && operand is FieldInfo field && field.DeclaringType == typeof(NCard) && FaceFields.Contains(field.Name))
                {
                    loadsFaceNode = true;
                }
                else if (operand is MethodInfo called && FaceWriters.Contains(called.Name))
                {
                    callsWriter = true;
                }
            }
            return loadsFaceNode && callsWriter;
        }
        catch (Exception e)
        {
            MainFile.Logger.Info($"Mushroom Extract: could not read NCard.{method.Name} ({e.GetType().Name}); not treated as a face writer.");
            return false;
        }
    }

    // Deliberately not named "Postfix": Harmony's class processor would then try to auto-patch this class.
    private static void AfterFaceWritten(NCard __instance, MethodBase __originalMethod) => Enforce(__instance, __originalMethod.Name);

    /// <summary>Idempotent: put the mystery art + gibberish text on <paramref name="view" /> if its model is mushroomed.</summary>
    private static void Enforce(NCard view, string after)
    {
        // The game's own writers early-return when the node isn't ready (Model can be assigned before _Ready
        // runs, e.g. by CardCmd.Preview spawns) — the face nodes are still null then.
        if (!view.IsNodeReady()
            || view.Model is not { } model
            || !MushroomedCards.TryGet(model, out MushroomedCards.Gibberish g))
        {
            return;
        }

        if (MushroomedCards.MysteryPortrait is { } mystery)
        {
            TextureRect portrait = PortraitField(view);
            TextureRect ancientPortrait = AncientPortraitField(view);
            // The game writes _ancientPortrait for Ancient cards and _portrait for the rest; check the visible one.
            Texture2D? shown = (model.Rarity == CardRarity.Ancient ? ancientPortrait : portrait).Texture;
            if (shown != null && shown != mystery && ReportedCorrections.Add(after))
            {
                // Means the model-level Portrait patch did not take effect for this card (or the path never used
                // it, like the inspect screen's BigPortrait). Not seen in a clean install; logged once per method
                // so a player's log shows that it happened and where.
                MainFile.Logger.Info($"Mushroom Extract: the game wrote real art onto a disguised card in NCard.{after}; replaced with the mystery art.");
            }
            portrait.Texture = mystery;
            ancientPortrait.Texture = mystery;
        }

        // Unknown / locked cards (compendium) already show their own placeholder text.
        if (view.Visibility != ModelVisibility.Visible)
        {
            return;
        }
        MegaLabel title = TitleLabelField(view);
        if (title.Text != g.Title)
        {
            title.SetTextAutoSize(g.Title);
        }
        MegaRichTextLabel description = DescriptionLabelField(view);
        string text = "[center]" + g.Description + "[/center]";
        if (description.Text != text)
        {
            description.SetTextAutoSize(text);
        }
    }
}

/// <summary>
/// Makes a clone of a mushroomed card mushroomed. <c>AbstractModel.MutableClone()</c> is only a few instructions
/// long (inlinable, so not a dependable patch target); <c>CardModel.DeepCloneFields</c> is the large method it
/// calls on the clone, and a prefix there still sees the clone's memberwise-copied fields — see
/// <see cref="MushroomedCards.InheritFromCloneSource" />. Without this the inspect screen, the upgrade preview and
/// "copy this card" effects showed the real card.
/// </summary>
[HarmonyPatch(typeof(CardModel), "DeepCloneFields")]
internal static class MushroomedClonePatch
{
    private static void Prefix(CardModel __instance) => MushroomedCards.InheritFromCloneSource(__instance);
}
