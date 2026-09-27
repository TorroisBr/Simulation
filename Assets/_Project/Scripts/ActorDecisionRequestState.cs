using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

public enum ActorDecisionRequestReceiptKind
{
    RequestBound = 1, TriggerObserved = 2, Deferred = 3, RetryableProposal = 4, TerminalReconciled = 5
}

public enum ActorDecisionDeferralReason
{
    DecisionBoundaryAlreadyUsed = 1, TemporarilyUnavailable = 2, InputNotDue = 3
}

/// <summary>Stable causal receipt owned by P18-C. It stores correlations, never command payload or availability truth.</summary>
public sealed class ActorDecisionRequestReceipt
{
    public long ReceiptSequence { get; }
    public long BoundarySequence { get; }
    public ActorDecisionRequestReceiptKind Kind { get; }
    public string OperationId { get; }
    public string RequestId { get; }
    public string InputId { get; }
    public string TriggerId { get; }
    public PersonId Actor { get; }
    public LogicalTick Instant { get; }
    public string BoundaryId { get; }
    public string ProfileId { get; }
    public long BoundaryRevision { get; }
    public long SourceSequence { get; }
    public ActorDecisionDeferralReason? DeferralReason { get; }
    public string ProposalId { get; }
    public string Outcome { get; }

    internal ActorDecisionRequestReceipt(long receiptSequence, long boundarySequence, ActorDecisionRequestReceiptKind kind,
        string operationId, string requestId, string inputId, string triggerId, PersonId actor, LogicalTick instant,
        string boundaryId, long boundaryRevision, long sourceSequence, ActorDecisionDeferralReason? deferralReason,
        string proposalId, string outcome, string profileId = null)
    {
        ReceiptSequence = receiptSequence; BoundarySequence = boundarySequence; Kind = kind; OperationId = operationId;
        RequestId = requestId; InputId = inputId; TriggerId = triggerId; Actor = actor; Instant = instant;
        BoundaryId = boundaryId; BoundaryRevision = boundaryRevision; SourceSequence = sourceSequence;
        DeferralReason = deferralReason; ProposalId = proposalId; Outcome = outcome; ProfileId = profileId;
    }
    internal ActorDecisionRequestReceipt Copy() => new ActorDecisionRequestReceipt(ReceiptSequence, BoundarySequence, Kind,
        OperationId, RequestId, InputId, TriggerId, Actor, Instant, BoundaryId, BoundaryRevision, SourceSequence,
        DeferralReason, ProposalId, Outcome, ProfileId);
}

/// <summary>Append-only P18-C request correlation log with a C-owned decision sequence allocator.</summary>
public sealed class ActorDecisionRequestState
{
    private readonly List<ActorDecisionRequestReceipt> receipts = new List<ActorDecisionRequestReceipt>();
    private readonly Dictionary<string, ActorDecisionRequestReceipt> byOperation = new Dictionary<string, ActorDecisionRequestReceipt>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> requestByInput = new Dictionary<string, string>(StringComparer.Ordinal);
    private long nextBoundarySequence = 1L;

    public long NextBoundarySequence => nextBoundarySequence;
    public int Count => receipts.Count;
    public IReadOnlyList<ActorDecisionRequestReceipt> Snapshot()
    {
        return new ReadOnlyCollection<ActorDecisionRequestReceipt>(receipts.Select(x => x.Copy()).ToList());
    }

    public bool TryBindInput(string operationId, string inputId, PersonId actor, LogicalTick targetInstant,
        string profileId, long p11InputSequence, string committedReceiptId, long receiptRevision,
        out ActorDecisionRequestReceipt receipt)
    {
        if (string.IsNullOrWhiteSpace(operationId) || string.IsNullOrWhiteSpace(inputId) || string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(committedReceiptId)
            || receiptRevision < 0L || p11InputSequence <= 0L || actor == null) { receipt = null; return false; }
        if (byOperation.TryGetValue(operationId, out ActorDecisionRequestReceipt prior))
        {
            if (prior.Kind == ActorDecisionRequestReceiptKind.RequestBound && prior.InputId == inputId && prior.Actor.Equals(actor)
                && prior.Instant == targetInstant && prior.BoundaryId == inputId && prior.ProfileId == profileId && prior.BoundaryRevision == receiptRevision
                && prior.SourceSequence == p11InputSequence && prior.Outcome == committedReceiptId)
            { receipt = prior.Copy(); return true; }
            receipt = null; return false;
        }
        long sequence = nextBoundarySequence;
        string requestId = new ActorDecisionRequest(actor, targetInstant, inputId, sequence, receiptRevision).Id;
        return Append(operationId, ActorDecisionRequestReceiptKind.RequestBound, inputId, requestId, actor, targetInstant,
            inputId, receiptRevision, p11InputSequence, null, null, committedReceiptId, out receipt, committedReceiptId, profileId);
    }

    public bool TryObserveTrigger(string operationId, string requestId, string triggerId, PersonId actor,
        LogicalTick instant, string boundaryId, long boundaryRevision, long lifecycleSourceSequence,
        string pendingInputId, out ActorDecisionRequestReceipt receipt)
    {
        if (string.IsNullOrWhiteSpace(operationId) || string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(pendingInputId)
            || actor == null || string.IsNullOrWhiteSpace(triggerId) || string.IsNullOrWhiteSpace(boundaryId)
            || boundaryRevision < 0L || lifecycleSourceSequence <= 0L) { receipt = null; return false; }
        foreach (ActorDecisionRequestReceipt committed in receipts)
        {
            if (committed.Kind != ActorDecisionRequestReceiptKind.TriggerObserved || committed.TriggerId != triggerId
                || !committed.Actor.Equals(actor)) continue;
            if (committed.RequestId == requestId && committed.InputId == pendingInputId && committed.Actor.Equals(actor)
                && committed.Instant == instant && committed.BoundaryId == boundaryId && committed.BoundaryRevision == boundaryRevision
                && committed.SourceSequence == lifecycleSourceSequence) { receipt = committed.Copy(); return true; }
            receipt = null; return false;
        }
        string derivedRequestId = new ActorDecisionRequest(actor, instant, boundaryId, nextBoundarySequence, boundaryRevision).Id;
        if (byOperation.TryGetValue(operationId, out ActorDecisionRequestReceipt prior))
        {
            if (prior.Kind == ActorDecisionRequestReceiptKind.TriggerObserved && prior.RequestId == requestId
                && prior.TriggerId == triggerId && prior.InputId == pendingInputId && prior.Actor.Equals(actor)
                && prior.Instant == instant && prior.BoundaryId == boundaryId && prior.BoundaryRevision == boundaryRevision
                && prior.SourceSequence == lifecycleSourceSequence) { receipt = prior.Copy(); return true; }
            receipt = null; return false;
        }
        if (!string.Equals(requestId, derivedRequestId, StringComparison.Ordinal)) { receipt = null; return false; }
        return Append(operationId, ActorDecisionRequestReceiptKind.TriggerObserved, pendingInputId, requestId, actor,
            instant, boundaryId, boundaryRevision, lifecycleSourceSequence, null, null, null, out receipt, triggerId);
    }

    public bool TryDefer(string operationId, string requestId, string inputId, PersonId actor, LogicalTick instant,
        string boundaryId, long boundaryRevision, long sourceSequence, ActorDecisionDeferralReason reason,
        out ActorDecisionRequestReceipt receipt)
    {
        if (!Enum.IsDefined(typeof(ActorDecisionDeferralReason), reason)) { receipt = null; return false; }
        return Append(operationId, ActorDecisionRequestReceiptKind.Deferred, inputId, requestId, actor, instant,
            boundaryId, boundaryRevision, sourceSequence, reason, null, null, out receipt);
    }

    public bool TryRecordRetryableProposal(string operationId, string requestId, string inputId, PersonId actor,
        LogicalTick instant, string proposalId, string disposition, out ActorDecisionRequestReceipt receipt)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(disposition)) { receipt = null; return false; }
        return Append(operationId, ActorDecisionRequestReceiptKind.RetryableProposal, inputId, requestId, actor,
            instant, requestId, 0L, 0L, null, proposalId, disposition, out receipt);
    }

    public bool TryReconcileTerminal(string operationId, string requestId, string inputId, PersonId actor,
        LogicalTick instant, string proposalId, string terminalOutcome, out ActorDecisionRequestReceipt receipt)
    {
        if (string.IsNullOrWhiteSpace(proposalId) || string.IsNullOrWhiteSpace(terminalOutcome)) { receipt = null; return false; }
        return Append(operationId, ActorDecisionRequestReceiptKind.TerminalReconciled, inputId, requestId, actor,
            instant, requestId, 0L, 0L, null, proposalId, terminalOutcome, out receipt);
    }

    private bool Append(string operationId, ActorDecisionRequestReceiptKind kind, string inputId, string requestId,
        PersonId actor, LogicalTick instant, string boundaryId, long revision, long sourceSequence,
        ActorDecisionDeferralReason? reason, string proposalId, string outcome, out ActorDecisionRequestReceipt receipt,
        string triggerId = null, string profileId = null)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationId) || actor == null || string.IsNullOrWhiteSpace(inputId)
            || string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(boundaryId) || revision < 0L || sourceSequence < 0L)
            return false;
        var candidate = new ActorDecisionRequestReceipt(0L, 0L, kind, operationId, requestId, inputId, triggerId,
            actor, instant, boundaryId, revision, sourceSequence, reason, proposalId, outcome, profileId);
        if (byOperation.TryGetValue(operationId, out ActorDecisionRequestReceipt existing))
        {
            if (SameContent(existing, candidate)) { receipt = existing.Copy(); return true; }
            return false;
        }
        if (kind == ActorDecisionRequestReceiptKind.TriggerObserved)
        {
            bool hasAcceptedBinding = receipts.Any(prior => prior.Kind == ActorDecisionRequestReceiptKind.RequestBound
                && prior.InputId == inputId && prior.Actor.Equals(actor) && prior.Instant.Value <= instant.Value);
            if (!hasAcceptedBinding) return false;
            foreach (ActorDecisionRequestReceipt prior in receipts)
            {
                if (prior.Kind != kind || prior.TriggerId != triggerId || !prior.Actor.Equals(actor)) continue;
                if (SameTrigger(prior, candidate)) { receipt = prior.Copy(); return true; }
                return false;
            }
        }
        if (kind == ActorDecisionRequestReceiptKind.Deferred || kind == ActorDecisionRequestReceiptKind.RetryableProposal
            || kind == ActorDecisionRequestReceiptKind.TerminalReconciled)
        {
            if (!HasExactRequestChain(requestId, inputId, actor, instant)) return false;
        }
        if (kind != ActorDecisionRequestReceiptKind.TerminalReconciled && receipts.Any(prior =>
            prior.Kind == ActorDecisionRequestReceiptKind.TerminalReconciled && prior.RequestId == requestId && prior.InputId == inputId))
            return false;
        if (kind == ActorDecisionRequestReceiptKind.Deferred)
        {
            foreach (ActorDecisionRequestReceipt prior in receipts)
            {
                if (prior.RequestId != requestId || prior.InputId != inputId) continue;
                if (prior.Kind == ActorDecisionRequestReceiptKind.RetryableProposal) return false;
                if (prior.Kind == kind && prior.Instant == instant && prior.BoundaryId == boundaryId) return false;
            }
        }
        if (kind == ActorDecisionRequestReceiptKind.TerminalReconciled)
        {
            foreach (ActorDecisionRequestReceipt prior in receipts)
            {
                if (prior.Kind != kind || prior.InputId != inputId || prior.RequestId != requestId) continue;
                if (prior.ProposalId == proposalId && prior.Outcome == outcome && prior.Actor.Equals(actor) && prior.Instant == instant)
                { receipt = prior.Copy(); return true; }
                return false;
            }
            foreach (ActorDecisionRequestReceipt prior in receipts)
                if (prior.Kind == ActorDecisionRequestReceiptKind.RetryableProposal && prior.RequestId == requestId
                    && prior.InputId == inputId && prior.ProposalId != proposalId) return false;
        }
        if (kind == ActorDecisionRequestReceiptKind.RetryableProposal)
            foreach (ActorDecisionRequestReceipt prior in receipts)
                if (prior.Kind == kind && prior.RequestId == requestId && prior.InputId == inputId && prior.ProposalId != proposalId) return false;
        long boundarySequence = kind == ActorDecisionRequestReceiptKind.RequestBound || kind == ActorDecisionRequestReceiptKind.TriggerObserved
            ? nextBoundarySequence : FindRequestSequence(requestId);
        if (boundarySequence <= 0L) return false;
        if (kind == ActorDecisionRequestReceiptKind.RequestBound && requestByInput.ContainsKey(inputId)) return false;
        if (receipts.Count == int.MaxValue || (kind == ActorDecisionRequestReceiptKind.RequestBound || kind == ActorDecisionRequestReceiptKind.TriggerObserved)
            && nextBoundarySequence == long.MaxValue) return false;
        receipt = new ActorDecisionRequestReceipt(receipts.Count + 1L, boundarySequence, kind, operationId, requestId,
            inputId, triggerId, actor, instant, boundaryId, revision, sourceSequence, reason, proposalId, outcome, profileId);
        receipts.Add(receipt); byOperation.Add(operationId, receipt);
        if (kind == ActorDecisionRequestReceiptKind.RequestBound) { requestByInput.Add(inputId, requestId); nextBoundarySequence++; }
        else if (kind == ActorDecisionRequestReceiptKind.TriggerObserved) nextBoundarySequence++;
        return true;
    }

    private long FindRequestSequence(string requestId)
    {
        for (int i = receipts.Count - 1; i >= 0; i--) if (receipts[i].RequestId == requestId) return receipts[i].BoundarySequence;
        return 0L;
    }

    private bool HasExactRequestChain(string requestId, string inputId, PersonId actor, LogicalTick instant)
    {
        foreach (ActorDecisionRequestReceipt prior in receipts)
            if ((prior.Kind == ActorDecisionRequestReceiptKind.RequestBound || prior.Kind == ActorDecisionRequestReceiptKind.TriggerObserved)
                && prior.RequestId == requestId && prior.InputId == inputId && prior.Actor.Equals(actor) && prior.Instant == instant)
                return true;
        return false;
    }

    private static bool SameContent(ActorDecisionRequestReceipt a, ActorDecisionRequestReceipt b) => a.Kind == b.Kind
        && a.InputId == b.InputId && a.RequestId == b.RequestId && a.TriggerId == b.TriggerId && Equals(a.Actor, b.Actor)
        && a.Instant == b.Instant && a.BoundaryId == b.BoundaryId && a.BoundaryRevision == b.BoundaryRevision
        && a.ProfileId == b.ProfileId
        && a.SourceSequence == b.SourceSequence && a.DeferralReason == b.DeferralReason && a.ProposalId == b.ProposalId && a.Outcome == b.Outcome;

    private static bool SameTrigger(ActorDecisionRequestReceipt a, ActorDecisionRequestReceipt b) => a.RequestId == b.RequestId
        && a.InputId == b.InputId && Equals(a.Actor, b.Actor) && a.Instant == b.Instant && a.BoundaryId == b.BoundaryId
        && a.BoundaryRevision == b.BoundaryRevision && a.SourceSequence == b.SourceSequence;

    public ActorDecisionRequestState Clone()
    {
        ActorDecisionRequestState clone = new ActorDecisionRequestState { nextBoundarySequence = nextBoundarySequence };
        foreach (ActorDecisionRequestReceipt item in receipts)
        {
            ActorDecisionRequestReceipt copy = item.Copy(); clone.receipts.Add(copy); clone.byOperation.Add(copy.OperationId, copy);
            if (copy.Kind == ActorDecisionRequestReceiptKind.RequestBound) clone.requestByInput.Add(copy.InputId, copy.RequestId);
        }
        return clone;
    }

    /// <summary>Reconstructs authoritative receipts; all indexes are rebuilt from the ordered snapshot.</summary>
    public static bool TryRestore(IEnumerable<ActorDecisionRequestReceipt> snapshot, long nextSequence,
        out ActorDecisionRequestState restored)
    {
        restored = null;
        if (snapshot == null || nextSequence <= 0L) return false;
        ActorDecisionRequestState candidate = new ActorDecisionRequestState { nextBoundarySequence = nextSequence };
        long expectedReceipt = 1L;
        foreach (ActorDecisionRequestReceipt source in snapshot)
        {
            if (source == null || source.ReceiptSequence != expectedReceipt++ || string.IsNullOrWhiteSpace(source.OperationId)
                || source.Actor == null || string.IsNullOrWhiteSpace(source.RequestId) || string.IsNullOrWhiteSpace(source.InputId)
                || string.IsNullOrWhiteSpace(source.BoundaryId) || source.BoundarySequence <= 0L
                || source.BoundarySequence >= nextSequence || candidate.byOperation.ContainsKey(source.OperationId)) return false;
            ActorDecisionRequestReceipt copy = source.Copy();
            if (copy.Kind == ActorDecisionRequestReceiptKind.RequestBound && candidate.requestByInput.ContainsKey(copy.InputId)) return false;
            candidate.receipts.Add(copy); candidate.byOperation.Add(copy.OperationId, copy);
            if (copy.Kind == ActorDecisionRequestReceiptKind.RequestBound) candidate.requestByInput.Add(copy.InputId, copy.RequestId);
        }
        if (candidate.ValidateInvariants().Count != 0) return false;
        restored = candidate; return true;
    }

    public IReadOnlyList<string> ValidateInvariants()
    {
        List<string> issues = new List<string>(); long lastReceipt = 0L, lastBoundary = 0L; HashSet<string> operations = new HashSet<string>(StringComparer.Ordinal);
        foreach (ActorDecisionRequestReceipt item in receipts)
        {
            if (item == null || item.ReceiptSequence != ++lastReceipt) { issues.Add("P18-C receipt sequence is not contiguous."); continue; }
            if (!operations.Add(item.OperationId) || !byOperation.TryGetValue(item.OperationId, out ActorDecisionRequestReceipt indexed) || !ReferenceEquals(item, indexed))
                issues.Add("P18-C operation index is inconsistent.");
            if (item.BoundarySequence <= 0L || item.BoundarySequence > nextBoundarySequence) issues.Add("P18-C boundary sequence is invalid.");
            if (!Enum.IsDefined(typeof(ActorDecisionRequestReceiptKind), item.Kind)
                || item.Kind == ActorDecisionRequestReceiptKind.RequestBound && (string.IsNullOrWhiteSpace(item.ProfileId) || item.SourceSequence <= 0L)
                || item.Kind == ActorDecisionRequestReceiptKind.TriggerObserved && (string.IsNullOrWhiteSpace(item.TriggerId) || item.SourceSequence <= 0L)
                || item.Kind == ActorDecisionRequestReceiptKind.Deferred && (!item.DeferralReason.HasValue
                    || !Enum.IsDefined(typeof(ActorDecisionDeferralReason), item.DeferralReason.Value))
                || item.Kind == ActorDecisionRequestReceiptKind.RetryableProposal && (string.IsNullOrWhiteSpace(item.ProposalId) || string.IsNullOrWhiteSpace(item.Outcome))
                || item.Kind == ActorDecisionRequestReceiptKind.TerminalReconciled && (string.IsNullOrWhiteSpace(item.ProposalId) || string.IsNullOrWhiteSpace(item.Outcome)))
                issues.Add("P18-C receipt values are incomplete.");
            if (!ValidateCausalLink(receipts, (int)item.ReceiptSequence - 1))
                issues.Add("P18-C receipt has an orphan or mismatched causal link.");
            if (item.Kind == ActorDecisionRequestReceiptKind.RequestBound
                && (!requestByInput.TryGetValue(item.InputId, out string boundRequestId) || boundRequestId != item.RequestId))
                issues.Add("P18-C input binding index does not match its request receipt.");
            if (item.Kind == ActorDecisionRequestReceiptKind.RequestBound || item.Kind == ActorDecisionRequestReceiptKind.TriggerObserved)
            { if (item.BoundarySequence <= lastBoundary) issues.Add("P18-C boundary sequences must increase in receipt order."); lastBoundary = item.BoundarySequence; }
        }
        if (nextBoundarySequence <= lastBoundary || requestByInput.Count != receipts.Count(x => x.Kind == ActorDecisionRequestReceiptKind.RequestBound))
            issues.Add("P18-C next sequence or input binding index is inconsistent.");
        return new ReadOnlyCollection<string>(issues);
    }

    private static bool ValidateCausalLink(IReadOnlyList<ActorDecisionRequestReceipt> ordered, int index)
    {
        ActorDecisionRequestReceipt item = ordered[index];
        if (item.Kind == ActorDecisionRequestReceiptKind.RequestBound)
            return item.BoundarySequence > 0L && item.RequestId == new ActorDecisionRequest(item.Actor, item.Instant,
                item.BoundaryId, item.BoundarySequence, item.BoundaryRevision).Id
                && !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.RequestBound && x.InputId == item.InputId);
        if (item.Kind == ActorDecisionRequestReceiptKind.TriggerObserved)
        {
            if (ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.TriggerObserved
                && x.TriggerId == item.TriggerId && x.Actor.Equals(item.Actor))) return false;
            bool bound = ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.RequestBound
                && x.InputId == item.InputId && x.Actor.Equals(item.Actor) && x.Instant.Value <= item.Instant.Value);
            return bound && item.BoundarySequence > 0L && item.RequestId == new ActorDecisionRequest(item.Actor, item.Instant,
                item.BoundaryId, item.BoundarySequence, item.BoundaryRevision).Id;
        }
        ActorDecisionRequestReceipt root = ordered.Take(index).LastOrDefault(x =>
            (x.Kind == ActorDecisionRequestReceiptKind.RequestBound || x.Kind == ActorDecisionRequestReceiptKind.TriggerObserved)
            && x.RequestId == item.RequestId && x.InputId == item.InputId && x.Actor.Equals(item.Actor) && x.Instant == item.Instant);
        if (root == null || root.BoundarySequence != item.BoundarySequence) return false;
        if (item.Kind != ActorDecisionRequestReceiptKind.TerminalReconciled && ordered.Take(index).Any(x =>
            x.Kind == ActorDecisionRequestReceiptKind.TerminalReconciled && x.RequestId == item.RequestId && x.InputId == item.InputId)) return false;
        if (item.Kind == ActorDecisionRequestReceiptKind.RetryableProposal)
        {
            if (string.IsNullOrWhiteSpace(item.ProposalId)) return false;
            return !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.RetryableProposal
                && x.RequestId == item.RequestId && x.InputId == item.InputId && x.ProposalId != item.ProposalId);
        }
        if (item.Kind == ActorDecisionRequestReceiptKind.TerminalReconciled)
        {
            if (string.IsNullOrWhiteSpace(item.ProposalId) || string.IsNullOrWhiteSpace(item.Outcome)) return false;
            return !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.TerminalReconciled
                && x.RequestId == item.RequestId && x.InputId == item.InputId)
                && !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.RetryableProposal
                    && x.RequestId == item.RequestId && x.InputId == item.InputId && x.ProposalId != item.ProposalId);
        }
        if (item.Kind == ActorDecisionRequestReceiptKind.Deferred)
            return item.DeferralReason.HasValue && !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.Deferred
                && x.RequestId == item.RequestId && x.InputId == item.InputId && x.Instant == item.Instant && x.BoundaryId == item.BoundaryId)
                && !ordered.Take(index).Any(x => x.Kind == ActorDecisionRequestReceiptKind.RetryableProposal
                    && x.RequestId == item.RequestId && x.InputId == item.InputId);
        return false;
    }
}
