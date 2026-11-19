using System.Globalization;
using System.Linq;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Everything a <see cref="BottledMessage" /> remembers about its card, as ONE value: captured once
/// (<see cref="Capture" />), saved and net-synced as a unit (<see cref="RegisterSaves" />), and turned back into a
/// card by one method (<see cref="CreateCard" />) that both the potion's use and its hover preview go through.
/// <para>
/// It is plain data on purpose. Every combat start the game rebuilds the OTHER players' potions from their
/// serialized form (<c>Player.SyncWithSerializedPlayer</c>) while the owner keeps the in-memory potion, and a run
/// save does the same on reload. A potion holding anything that is not in here (a live card, a model reference)
/// would give different cards on different clients; a potion holding only this cannot.
/// </para>
/// <para>
/// Two layers. <see cref="Card" /> is the game's own save form of a card (id, upgrades, enchantment, saved
/// properties, other mods' saved card values), written and read by the game's own <c>ToSerializable</c> /
/// <c>FromSerializable</c>. The rest is what the card had gained in combat on top of that, which the game never
/// saves: each is stored as the DIFFERENCE between the bottled card and a card rebuilt from <see cref="Card" />
/// alone, so new sources of Replay / keywords / grown numbers are covered without naming them here.
/// </para>
/// </summary>
public sealed class BottledCard
{
    private const string SaveId = "thewitch_bottled_card";

    /// <summary>Saves from v0.14.5 and earlier stored only the card, under this id. Read, never written.</summary>
    private const string LegacySaveId = "thewitch_bottled_message";

    // Public get/set properties: this is the JSON save contract (see RegisterSaves). None may be null.

    /// <summary>The game's save form of the card.</summary>
    public SerializableCard Card { get; set; } = new();

    /// <summary>
    /// The card's cost with its own modifiers (Snecko roll, "costs 0 this combat", …), or -1 for none. Comes back
    /// as a reduce-only "until played" cost, so a cost INCREASE is never carried over.
    /// </summary>
    public int Cost { get; set; } = -1;

    /// <summary>Replay gained in combat (Toil and Trouble, Extract Life, …).</summary>
    public int Replay { get; set; }

    /// <summary>Keywords the card gained in combat (Extract Life's Exhaust, …), by name.</summary>
    public List<string> KeywordsAdded { get; set; } = [];

    /// <summary>Keywords the card lost in combat, by name.</summary>
    public List<string> KeywordsRemoved { get; set; } = [];

    /// <summary>Numbers that grew in combat (Rampage's damage, …): var name -> change to its base value.</summary>
    public Dictionary<string, string> Vars { get; set; } = [];

    /// <summary>False when the card no longer exists (removed by a mod update): the bottle is then empty.</summary>
    public bool IsKnown => Card.Id is { } id && ModelDb.GetByIdOrNull<CardModel>(id) != null;

    public static BottledCard Capture(CardModel card)
    {
        SerializableCard saved = card.ToSerializable();
        saved.FloorAddedToDeck = null; // what comes out is a generated combat card, not the deck's copy

        // What the saved part alone rebuilds. Everything below is measured against it.
        CardModel rebuilt = CardModel.FromSerializable(saved);

        BottledCard bottled = new()
        {
            Card = saved,
            Replay = card.BaseReplayCount - rebuilt.BaseReplayCount,
            // Local modifiers only: global hook effects re-evaluate by themselves in the next combat.
            Cost = !card.EnergyCost.CostsX && card.EnergyCost.HasLocalModifiers
                ? card.EnergyCost.GetWithModifiers(CostModifiers.Local)
                : -1,
        };

        // Local keywords only, for the same reason (and this-turn-only Retain / Sly are not keywords at all).
        IReadOnlySet<CardKeyword> now = card.GetKeywordsWithSources(KeywordSources.Local);
        IReadOnlySet<CardKeyword> before = rebuilt.GetKeywordsWithSources(KeywordSources.Local);
        bottled.KeywordsAdded = now.Except(before).Select(k => k.ToString()).Order(StringComparer.Ordinal).ToList();
        bottled.KeywordsRemoved = before.Except(now).Select(k => k.ToString()).Order(StringComparer.Ordinal).ToList();

        foreach ((string name, DynamicVar value) in card.DynamicVars)
        {
            if (rebuilt.DynamicVars.TryGetValue(name, out DynamicVar? baseline) && value.BaseValue != baseline.BaseValue)
            {
                bottled.Vars[name] = (value.BaseValue - baseline.BaseValue).ToString(CultureInfo.InvariantCulture);
            }
        }
        return bottled;
    }

    /// <summary>
    /// The bottled card as a new card: in <paramref name="combat" /> and owned by <paramref name="owner" />, or
    /// (both null) a loose one for the hover preview. Null when the card no longer exists.
    /// </summary>
    public CardModel? CreateCard(ICombatState? combat = null, Player? owner = null)
    {
        if (!IsKnown)
        {
            return null;
        }

        // The game's own load path for a saved card, then RunState.LoadCard's "add to the scope" step.
        CardModel card = CardModel.FromSerializable(Card);
        if (combat != null && owner != null)
        {
            combat.AddCard(card, owner);
        }

        foreach (string keyword in KeywordsAdded)
        {
            if (Enum.TryParse(keyword, out CardKeyword parsed))
            {
                card.AddKeyword(parsed);
            }
        }
        foreach (string keyword in KeywordsRemoved)
        {
            if (Enum.TryParse(keyword, out CardKeyword parsed))
            {
                card.RemoveKeyword(parsed);
            }
        }
        if (Replay != 0)
        {
            card.BaseReplayCount = Math.Max(0, card.BaseReplayCount + Replay);
        }
        foreach ((string name, string change) in Vars)
        {
            if (card.DynamicVars.TryGetValue(name, out DynamicVar? value)
                && decimal.TryParse(change, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal delta))
            {
                value.BaseValue += delta;
            }
        }
        if (Cost >= 0 && !card.EnergyCost.CostsX)
        {
            card.EnergyCost.SetUntilPlayed(Cost, reduceOnly: true);
        }
        return card;
    }

    private void Serialize(PacketWriter writer)
    {
        Card.Serialize(writer);
        writer.WriteInt(Cost);
        writer.WriteInt(Replay);
        WriteStrings(writer, KeywordsAdded);
        WriteStrings(writer, KeywordsRemoved);
        // Ordered: every client must write the same bytes for the same card.
        WriteStrings(writer, Vars.OrderBy(v => v.Key, StringComparer.Ordinal).SelectMany(v => new[] { v.Key, v.Value }).ToList());
    }

    private static BottledCard Deserialize(PacketReader reader)
    {
        BottledCard bottled = new();
        bottled.Card.Deserialize(reader);
        bottled.Cost = reader.ReadInt();
        bottled.Replay = reader.ReadInt();
        bottled.KeywordsAdded = ReadStrings(reader);
        bottled.KeywordsRemoved = ReadStrings(reader);
        List<string> vars = ReadStrings(reader);
        for (int i = 0; i + 1 < vars.Count; i += 2)
        {
            bottled.Vars[vars[i]] = vars[i + 1];
        }
        return bottled;
    }

    private static void WriteStrings(PacketWriter writer, List<string> values)
    {
        writer.WriteInt(values.Count);
        foreach (string value in values)
        {
            writer.WriteString(value);
        }
    }

    private static List<string> ReadStrings(PacketReader reader)
    {
        int count = reader.ReadInt();
        List<string> values = new(count);
        for (int i = 0; i < count; i++)
        {
            values.Add(reader.ReadString());
        }
        return values;
    }

    /// <summary>
    /// Bakes the bottled card into the run save and into the potion's net serialization, attached to the potion's
    /// own <c>SerializablePotion</c> entry (BaseLib ExtendedSaveTypes), so every Bottled Message on the belt carries
    /// its own. Called once from the mod initializer.
    /// </summary>
    internal static void RegisterSaves()
    {
        ExtendedSaveTypes.RegisterDictionarySaveType<string, string>();
        ExtendedSaveTypes.RegisterObjectSaveType<BottledCard>(
            ExtendedSaveTypes.PropertyFunc<BottledCard, SerializableCard>(nameof(Card)),
            ExtendedSaveTypes.PropertyFunc<BottledCard, int>(nameof(Cost)),
            ExtendedSaveTypes.PropertyFunc<BottledCard, int>(nameof(Replay)),
            ExtendedSaveTypes.PropertyFunc<BottledCard, List<string>>(nameof(KeywordsAdded)),
            ExtendedSaveTypes.PropertyFunc<BottledCard, List<string>>(nameof(KeywordsRemoved)),
            ExtendedSaveTypes.PropertyFunc<BottledCard, Dictionary<string, string>>(nameof(Vars)));

        ExtendedSaveTypes.RegisterSavedValue<BottledMessage, BottledCard>(
            SaveId,
            potion => potion.Bottled,
            (potion, bottled) => potion.Bottled = bottled,
            (bottled, writer) => bottled.Serialize(writer),
            Deserialize);

        // Old saves: a run in progress keeps the card in its bottle (without its cost discount, which those
        // saves held as a separate value).
        ExtendedSaveTypes.RegisterSavedValue<BottledMessage, SerializableCard>(
            LegacySaveId,
            _ => null,
            (potion, card) =>
            {
                if (card != null && !potion.HoldsCard)
                {
                    potion.Bottled = new BottledCard { Card = card };
                }
            },
            (card, writer) => card.Serialize(writer),
            reader =>
            {
                SerializableCard card = new();
                card.Deserialize(reader);
                return card;
            });
    }
}
