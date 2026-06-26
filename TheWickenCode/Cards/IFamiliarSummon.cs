namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Marks a card that summons a familiar (i.e. applies a <see cref="Powers.FamiliarPower" /> via
/// <c>GainFamiliar</c>). Lets other effects identify familiar-summon cards without hard-coding the list —
/// e.g. Broom Strike ("the next familiar power you play is free") and Pact of Beasts.
/// </summary>
public interface IFamiliarSummon
{
}
