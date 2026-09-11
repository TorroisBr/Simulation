using System;
using System.Collections.Generic;

public enum NpcChronicleEntryType
{
    Decision,
    DomainEvent
}

public enum NpcChronicleRelation
{
    SelfDecision,
    SelfAction,
    AffectedOther,
    AffectedByOther,
    Participant
}

public sealed class NpcChronicleEntry
{
    public long AbsoluteDay { get; }
    public long RecordSequence { get; }
    public NpcChronicleEntryType EntryType { get; }
    public NpcChronicleRelation Relation { get; }
    public NpcDecisionRecord Decision { get; }
    public DomainEvent DomainEvent { get; }

    private NpcChronicleEntry(
        long absoluteDay,
        long recordSequence,
        NpcChronicleEntryType entryType,
        NpcChronicleRelation relation,
        NpcDecisionRecord decision,
        DomainEvent domainEvent)
    {
        AbsoluteDay = absoluteDay;
        RecordSequence = recordSequence;
        EntryType = entryType;
        Relation = relation;
        Decision = decision;
        DomainEvent = domainEvent;
    }

    public static NpcChronicleEntry FromDecision(NpcDecisionRecord decision)
    {
        if (decision == null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        return new NpcChronicleEntry(
            decision.AbsoluteDay,
            decision.RecordSequence,
            NpcChronicleEntryType.Decision,
            NpcChronicleRelation.SelfDecision,
            decision,
            null);
    }

    public static NpcChronicleEntry FromDomainEvent(DomainEvent domainEvent, NpcChronicleRelation relation)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        return new NpcChronicleEntry(
            domainEvent.AbsoluteDay,
            domainEvent.RecordSequence,
            NpcChronicleEntryType.DomainEvent,
            relation,
            null,
            domainEvent);
    }
}

public sealed class NpcChronicleService
{
    private readonly NpcDecisionStore decisionStore;
    private readonly DomainEventStore domainEventStore;

    public NpcChronicleService(NpcDecisionStore decisionStore, DomainEventStore domainEventStore)
    {
        this.decisionStore = decisionStore ?? throw new ArgumentNullException(nameof(decisionStore));
        this.domainEventStore = domainEventStore ?? throw new ArgumentNullException(nameof(domainEventStore));
    }

    public IReadOnlyList<NpcChronicleEntry> GetChronicle(string npcRuntimeId)
    {
        List<NpcChronicleEntry> entries = new List<NpcChronicleEntry>();

        foreach (NpcDecisionRecord decision in decisionStore.GetDecisionsForActor(npcRuntimeId))
        {
            entries.Add(NpcChronicleEntry.FromDecision(decision));
        }

        foreach (DomainEvent domainEvent in domainEventStore.GetEventsForParticipant(npcRuntimeId))
        {
            entries.Add(NpcChronicleEntry.FromDomainEvent(domainEvent, GetRelation(domainEvent, npcRuntimeId)));
        }

        entries.Sort((left, right) => left.RecordSequence.CompareTo(right.RecordSequence));
        return entries.AsReadOnly();
    }

    private static NpcChronicleRelation GetRelation(DomainEvent domainEvent, string npcRuntimeId)
    {
        bool isActor = false;
        bool isTarget = false;
        bool hasOtherTarget = false;

        foreach (DomainEventParticipant participant in domainEvent.GetParticipants())
        {
            if (participant.Role == DomainEventParticipantRole.Actor
                && string.Equals(participant.RuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
            {
                isActor = true;
            }

            if (participant.Role == DomainEventParticipantRole.Target)
            {
                if (string.Equals(participant.RuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
                {
                    isTarget = true;
                }
                else
                {
                    hasOtherTarget = true;
                }
            }
        }

        if (isTarget == true && isActor == false)
        {
            return NpcChronicleRelation.AffectedByOther;
        }

        if (isActor == true)
        {
            return hasOtherTarget == true
                ? NpcChronicleRelation.AffectedOther
                : NpcChronicleRelation.SelfAction;
        }

        return NpcChronicleRelation.Participant;
    }
}

public sealed class NpcChronicleFormatter
{
    private readonly Func<string, string> resolveNpcName;
    private readonly Func<string, string> resolveLocationName;
    private readonly Func<string, string> resolveItemName;
    private readonly Func<string, string> resolveActionName;

    public NpcChronicleFormatter(
        Func<string, string> resolveNpcName,
        Func<string, string> resolveLocationName,
        Func<string, string> resolveItemName,
        Func<string, string> resolveActionName)
    {
        this.resolveNpcName = resolveNpcName;
        this.resolveLocationName = resolveLocationName;
        this.resolveItemName = resolveItemName;
        this.resolveActionName = resolveActionName;
    }

    public string Format(NpcChronicleEntry entry, string perspectiveNpcRuntimeId)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        return entry.EntryType == NpcChronicleEntryType.Decision
            ? FormatDecision(entry.Decision)
            : FormatEvent(entry.DomainEvent, perspectiveNpcRuntimeId);
    }

    private string FormatDecision(NpcDecisionRecord decision)
    {
        if (decision == null)
        {
            return string.Empty;
        }

        string actor = Resolve(resolveNpcName, decision.ActorRuntimeId);
        CommercialDecisionEvidence evidence = decision.CommercialEvidence;

        if (evidence != null)
        {
            string item = Resolve(resolveItemName, evidence.ItemDefinitionId);
            string destination = Resolve(resolveLocationName, evidence.TradeDestinationLocationRuntimeId);

            if (decision.DecisionType == NpcDecisionType.TradePurchase)
            {
                return $"{actor} decidiu comprar {evidence.ExpectedQuantity} de {item} para negociar em {destination}; lucro liquido esperado: {evidence.ExpectedNetProfit:0.##}.";
            }

            if (decision.DecisionType == NpcDecisionType.TradeReposition)
            {
                string tradeOrigin = Resolve(resolveLocationName, evidence.TradeOriginLocationRuntimeId);
                return $"{actor} decidiu se reposicionar em {tradeOrigin} para negociar {item} em {destination}; lucro liquido esperado: {evidence.ExpectedNetProfit:0.##}.";
            }

            if (decision.DecisionType == NpcDecisionType.TradeRedirect)
            {
                return $"{actor} decidiu redirecionar o plano de {item} para {destination}; lucro liquido esperado: {evidence.ExpectedNetProfit:0.##}.";
            }

            if (decision.DecisionType == NpcDecisionType.TradeSale)
            {
                return $"{actor} decidiu vender {evidence.ExpectedQuantity} de {item} em {destination} por {evidence.ExpectedSaleUnitPrice:0.##} cada.";
            }
        }

        string action = Resolve(resolveActionName, decision.ActionDefinitionId);
        string target = decision.TargetRuntimeId != null
            ? " para " + Resolve(resolveNpcName, decision.TargetRuntimeId)
            : decision.TargetLocationRuntimeId != null
                ? " para " + Resolve(resolveLocationName, decision.TargetLocationRuntimeId)
                : string.Empty;
        return $"{actor} decidiu realizar {action}{target}.";
    }

    private string FormatEvent(DomainEvent domainEvent, string perspectiveNpcRuntimeId)
    {
        if (domainEvent is NpcTravelStartedEvent travelStarted)
        {
            string actor = Resolve(resolveNpcName, travelStarted.ActorRuntimeId);
            string destination = Resolve(resolveLocationName, travelStarted.DestinationLocationRuntimeId);
            return $"{actor} iniciou viagem para {destination}.";
        }

        if (domainEvent is NpcArrivedEvent arrived)
        {
            string actor = Resolve(resolveNpcName, arrived.ActorRuntimeId);
            string destination = Resolve(resolveLocationName, arrived.DestinationLocationRuntimeId);
            return $"{actor} chegou em {destination}.";
        }

        if (domainEvent is NpcArrestedEvent arrested)
        {
            string actor = Resolve(resolveNpcName, arrested.ActorRuntimeId);
            string target = Resolve(resolveNpcName, arrested.TargetRuntimeId);
            string location = Resolve(resolveLocationName, arrested.LocationRuntimeId);

            return string.Equals(perspectiveNpcRuntimeId, arrested.TargetRuntimeId, StringComparison.Ordinal) == true
                ? $"{target} foi preso por {actor} em {location}."
                : $"{actor} prendeu {target} em {location}.";
        }

        if (domainEvent is NpcEscapedEvent escaped)
        {
            string actor = Resolve(resolveNpcName, escaped.ActorRuntimeId);
            string location = Resolve(resolveLocationName, escaped.LocationRuntimeId);
            return $"{actor} escapou da prisao em {location}.";
        }

        return domainEvent != null ? domainEvent.EventId : string.Empty;
    }

    private static string Resolve(Func<string, string> resolver, string stableId)
    {
        if (string.IsNullOrWhiteSpace(stableId) == true)
        {
            return "<unknown>";
        }

        string displayName = resolver != null ? resolver(stableId) : null;
        return string.IsNullOrWhiteSpace(displayName) == true ? stableId : displayName;
    }
}
