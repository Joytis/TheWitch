using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Patches;

/// <summary>
/// Registry + Harmony patches behind the Mushroom Extract potion. A "mushroomed" card shows random-letter
/// gibberish for its name and description and uses the <c>mystery.png</c> art — only that specific card
/// instance, only visually (it stays fully playable). The gibberish is generated once per card and stored,
/// so it's stable for the rest of the combat. The store is keyed by card instance via a
/// <see cref="ConditionalWeakTable{TKey,TValue}" />, so it's collected automatically when the combat's card
/// clones are discarded — no explicit combat-end cleanup needed.
///
/// Card cost-0 is handled separately by the potion via <c>CardModel.SetToFreeThisCombat()</c>; this file only
/// owns the cosmetic override.
/// </summary>
public static class MushroomedCards
{
    public sealed record Gibberish(string Title, string Description);

    private static readonly ConditionalWeakTable<CardModel, Gibberish> Store = new();

    private static readonly string[] Syllables =
    [
        "bo", "gru", "mish", "fa", "lor", "nak", "wim", "ble", "zu", "thra", "glor", "pfo", "oo", "ka", "tz",
        "ip", "mun", "dle", "sev", "qua", "rin", "tho", "blu", "grik", "sna", "fenn", "wob", "plo", "dox", "yib",
    ];

    /// <summary>Tag <paramref name="card" /> as mushroomed, generating + storing its gibberish text once.</summary>
    public static void Mark(CardModel card, Rng rng)
    {
        if (Store.TryGetValue(card, out _))
        {
            return;
        }
        Store.Add(card, Generate(rng));
    }

    public static bool TryGet(CardModel card, out Gibberish gibberish) => Store.TryGetValue(card, out gibberish!);

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
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Title;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), new[] { typeof(PileType), typeof(Creature) })]
internal static class MushroomedDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Description;
        }
    }
}

[HarmonyPatch(typeof(CardModel), "get_Portrait")]
internal static class MushroomedPortraitPatch
{
    private static Texture2D? _mystery;

    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (!MushroomedCards.TryGet(__instance, out _))
        {
            return;
        }
        _mystery ??= ResourceLoader.Load<Texture2D>("mystery.png".CardImagePath(), null, ResourceLoader.CacheMode.Reuse);
        if (_mystery != null)
        {
            __result = _mystery;
        }
    }
}
