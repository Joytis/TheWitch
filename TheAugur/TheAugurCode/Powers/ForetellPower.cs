using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TheAugur.TheAugurCode.Cards;
using TheAugur.TheAugurCode.Nodes;

namespace TheAugur.TheAugurCode.Powers;

/// <summary>
/// The Augur's Portent tracker: every foretold card waits here with its remaining turn count.
/// <c>Amount</c> = number of waiting Portents (the power's stack counter), so the icon doubles as a
/// count and the power auto-removes when the last Portent resolves. At the start of each of the
/// owner's play phases every Portent ticks down and those at 0 are auto-played for free (random
/// target for targeted cards), in ascending remaining-turn order.
///
/// The over-the-head rift display (<see cref="NForetellManager"/>) is spawned on apply and freed on
/// remove; it re-reads <see cref="Portents"/> whenever <see cref="Changed"/> fires.
///
/// Plain list state is fine here: run saves hold no combat state and MP is lockstep (see CLAUDE.md).
/// </summary>
public sealed class ForetellPower : AugurPower
{
    public sealed class Portent(CardModel card, int turnsLeft)
    {
        public CardModel Card { get; } = card;
        public int TurnsLeft { get; set; } = turnsLeft;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Not readonly: DeepCloneFields gives every mutable clone its own list (MemberwiseClone would share it).
    private List<Portent> _portents = new();
    private NForetellManager? _display;

    public IReadOnlyList<Portent> Portents => _portents;

    /// <summary>Fires after any change to the Portent list (add, tick, resolve).</summary>
    public event Action? Changed;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        _portents.Select(p => HoverTipFactory.FromCard(p.Card)).ToList();

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _portents = new List<Portent>();
        _display = null;
    }

    public void Add(CardModel card, int turns)
    {
        MainFile.Logger.Info($"[foretell] {card.Id.Entry} set aside for {turns} turn(s)");
        _portents.Add(new Portent(card, Math.Max(1, turns)));
        _portents.Sort((a, b) => a.TurnsLeft.CompareTo(b.TurnsLeft));
        Changed?.Invoke();
    }

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || _portents.Count == 0)
        {
            return;
        }
        foreach (Portent portent in _portents)
        {
            portent.TurnsLeft--;
        }
        List<Portent> ready = _portents.Where(p => p.TurnsLeft <= 0).ToList();
        _portents.RemoveAll(p => p.TurnsLeft <= 0);
        Changed?.Invoke();

        foreach (Portent portent in ready)
        {
            MainFile.Logger.Info($"[foretell] resolving {portent.Card.Id.Entry} ({_portents.Count} still waiting)");
            if (portent.Card is ForetellCard foretold)
            {
                foretold.ResolvingForetell = true;
            }
            // Free auto-play; AnyEnemy cards get a random hittable enemy. Pile == null is handled
            // (the card is put in the Play pile first) and the card ends in its normal result pile.
            await CardCmd.AutoPlay(choiceContext, portent.Card, null);
            // Decrement -> ModifyAmount; hitting 0 auto-removes the power (and the display).
            await PowerCmd.Decrement(this);
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SpawnDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        _display?.Dismiss();
        _display = null;
        return Task.CompletedTask;
    }

    private void SpawnDisplay()
    {
        if (_display != null)
        {
            return;
        }
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null)
        {
            return; // headless / test mode / no room: state still works without visuals
        }
        _display = NForetellManager.Create(this, creatureNode);
        creatureNode.AddChildSafely(_display);
    }
}
