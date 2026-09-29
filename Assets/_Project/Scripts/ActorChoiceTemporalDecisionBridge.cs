using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Exact retained P11 input paired with the P18-C request that authorizes its
/// current decision-boundary attempt. The original C sequence is retained as
/// the FIFO key even when a later trigger creates a newer request.
/// </summary>
public sealed class ActorChoiceTemporalDecisionRequest
{
    public ActorChoiceInput Input { get; }
    public ActorDecisionRequest Request { get; }
    public long OriginatingBoundarySequence { get; }
    public string RetryableProposalId { get; }

    internal ActorChoiceTemporalDecisionRequest(ActorChoiceInput input, ActorDecisionRequest request,
        long originatingBoundarySequence, string retryableProposalId)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        if (originatingBoundarySequence <= 0L) throw new ArgumentOutOfRangeException(nameof(originatingBoundarySequence));
        OriginatingBoundarySequence = originatingBoundarySequence;
        RetryableProposalId = retryableProposalId;
    }
}

/// <summary>One committed activity-participant trigger supplied after timeline dispatch.</summary>
public sealed class ActorChoiceTemporalTrigger
{
    public PersonId Actor { get; }
    public string TriggerId { get; }
    public string BoundaryId { get; }
    public long BoundaryRevision { get; }
    public long SourceSequence { get; }
    public LogicalTick Instant { get; }

    public ActorChoiceTemporalTrigger(PersonId actor, string triggerId, string boundaryId,
        long boundaryRevision, long sourceSequence, LogicalTick instant)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        if (string.IsNullOrWhiteSpace(triggerId)) throw new ArgumentException("Trigger identity is required.", nameof(triggerId));
        if (string.IsNullOrWhiteSpace(boundaryId)) throw new ArgumentException("Boundary identity is required.", nameof(boundaryId));
        if (boundaryRevision < 0L) throw new ArgumentOutOfRangeException(nameof(boundaryRevision));
        if (sourceSequence <= 0L) throw new ArgumentOutOfRangeException(nameof(sourceSequence));
        TriggerId = triggerId;
        BoundaryId = boundaryId;
        BoundaryRevision = boundaryRevision;
        SourceSequence = sourceSequence;
        Instant = instant;
    }
}

/// <summary>
/// Binds committed temporal P11 inputs to P18-C request receipts and selects
/// one due retained choice per actor after a successful outer timeline advance.
/// This bridge does not dispatch from a timeline owner callback and does not
/// execute or revalidate the domain action.
/// </summary>
public sealed class ActorChoiceTemporalDecisionBridge
{
    private readonly ActorChoiceStore actorChoices;
    private readonly ActorDecisionRequestState requests;
    private readonly string profileId;

    public ActorChoiceTemporalDecisionBridge(ActorChoiceStore actorChoices,
        ActorDecisionRequestState requests, string profileId)
    {
        this.actorChoices = actorChoices ?? throw new ArgumentNullException(nameof(actorChoices));
        this.requests = requests ?? throw new ArgumentNullException(nameof(requests));
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required.", nameof(profileId));
        this.profileId = profileId;
    }

    /// <summary>
    /// Call only after an outer timeline advance succeeds at <paramref name="instant"/>.
    /// Newly committed inputs must target this exact yield; a missed earlier
    /// handoff is reported instead of executing an input retroactively.
    /// </summary>
    public bool AfterSuccessfulAdvance(LogicalTick instant,
        out IReadOnlyList<ActorChoiceTemporalDecisionRequest> admitted, out string failure)
    {
        return AfterSuccessfulAdvance(instant, Array.Empty<ActorChoiceTemporalTrigger>(), out admitted, out failure);
    }

    public bool AfterSuccessfulAdvance(LogicalTick instant,
        IReadOnlyList<ActorChoiceTemporalTrigger> committedTriggers,
        out IReadOnlyList<ActorChoiceTemporalDecisionRequest> admitted, out string failure)
    {
        admitted = Array.Empty<ActorChoiceTemporalDecisionRequest>();
        failure = null;

        IReadOnlyList<ActorChoiceInput> allInputs = actorChoices.Inputs;
        IReadOnlyList<ActorDecisionRequestReceipt> initialReceipts = requests.Snapshot();
        Dictionary<string, RequestHistory> histories = BuildHistories(initialReceipts);
        IReadOnlyList<ActorChoiceInput> pending = allInputs.Where(input => input.TemporalCapture != null
            && string.Equals(input.TemporalCapture.ProfileId, profileId, StringComparison.Ordinal)
            && (input.Status == ActorChoiceInputStatus.Pending
                || input.Status == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt
                    && histories.TryGetValue(input.InputId.Value, out RequestHistory history)
                    && history.LastDisposition?.Kind == ActorDecisionRequestReceiptKind.RetryableProposal)).ToArray();

        List<ActorChoiceInput> newlyCommitted = new List<ActorChoiceInput>();
        foreach (ActorChoiceInput input in pending)
        {
            if (histories.ContainsKey(input.InputId.Value)) continue;
            if (input.TemporalCapture.TargetInstant.Value < instant.Value)
            {
                failure = "A committed P11 input missed its exact P18-D post-advance handoff; refusing retroactive admission.";
                return false;
            }
            if (input.TemporalCapture.TargetInstant == instant) newlyCommitted.Add(input);
        }

        foreach (ActorChoiceInput input in pending)
        {
            if (!histories.TryGetValue(input.InputId.Value, out RequestHistory history)
                || history.CurrentRequest == null || history.LastDisposition != null
                    && history.LastDisposition.Kind != ActorDecisionRequestReceiptKind.RetryableProposal) continue;
            if (history.CurrentRequest.Instant.Value < instant.Value
                && history.LastDisposition?.Kind != ActorDecisionRequestReceiptKind.RetryableProposal)
            {
                failure = "A due P11 request missed its exact P18-D decision boundary; refusing retroactive admission.";
                return false;
            }
        }

        // A newly accepted input is a meaningful later trigger for the oldest
        // previously deferred pending input of the same actor. Allocate that
        // fresh C sequence before binding the new input, preserving FIFO.
        foreach (IGrouping<string, ActorChoiceInput> actorInputs in newlyCommitted
            .GroupBy(input => input.PersonId.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            ActorChoiceInput trigger = actorInputs.OrderBy(input => input.TemporalCapture.AcceptedInput.Sequence)
                .ThenBy(input => input.InputSequence).First();
            RequestHistory deferred = pending
                .Where(input => string.Equals(input.PersonId.Value, actorInputs.Key, StringComparison.Ordinal)
                    && input.TemporalCapture.TargetInstant.Value <= instant.Value)
                .Select(input => histories.TryGetValue(input.InputId.Value, out RequestHistory found)
                    ? new { Input = input, History = found } : null)
                .Where(item => item != null && item.History.CurrentRequest != null
                    && item.History.LastDisposition != null
                    && item.History.LastDisposition.Kind == ActorDecisionRequestReceiptKind.Deferred
                    && item.History.LastDisposition.Instant.Value < instant.Value)
                .OrderBy(item => item.History.OriginatingSequence)
                .ThenBy(item => item.Input.InputSequence)
                .Select(item => item.History).FirstOrDefault();
            if (deferred == null) continue;

            if (!TryObserveMeaningfulTrigger(deferred.CurrentRequest.Actor,
                trigger.TemporalCapture.AcceptedInput.InputId, instant,
                trigger.TemporalCapture.AcceptedInput.InputId, 0L,
                trigger.TemporalCapture.AcceptedInput.Sequence, out _, out failure)) return false;
        }

        if (committedTriggers != null)
        {
            foreach (ActorChoiceTemporalTrigger trigger in committedTriggers
                .Where(item => item != null)
                .OrderBy(item => item.SourceSequence)
                .ThenBy(item => item.Actor.Value, StringComparer.Ordinal)
                .ThenBy(item => item.TriggerId, StringComparer.Ordinal))
            {
                if (trigger.Instant != instant)
                {
                    failure = "A committed activity trigger does not belong to the completed timeline instant.";
                    return false;
                }

                histories = BuildHistories(requests.Snapshot());
                bool hasOlderDeferredInput = pending.Any(input => input.PersonId.Equals(trigger.Actor)
                    && input.TemporalCapture.TargetInstant.Value <= instant.Value
                    && histories.TryGetValue(input.InputId.Value, out RequestHistory history)
                    && history.CurrentRequest != null
                    && history.LastDisposition != null
                    && history.LastDisposition.Kind == ActorDecisionRequestReceiptKind.Deferred
                    && history.LastDisposition.Instant.Value < instant.Value);
                if (!hasOlderDeferredInput) continue;

                if (!TryObserveMeaningfulTrigger(trigger.Actor, trigger.TriggerId,
                    trigger.Instant, trigger.BoundaryId, trigger.BoundaryRevision,
                    trigger.SourceSequence, out _, out failure)) return false;
            }
        }

        foreach (ActorChoiceInput input in newlyCommitted.OrderBy(item => item.TemporalCapture.AcceptedInput.Sequence)
            .ThenBy(item => item.InputSequence))
        {
            TimelineInputReference accepted = input.TemporalCapture.AcceptedInput;
            string operationId = OperationId("bind", input.InputId.Value);
            if (!requests.TryBindInput(operationId, input.InputId.Value, input.PersonId,
                input.TemporalCapture.TargetInstant, profileId, input.InputSequence,
                accepted.InputId, 0L, out _))
            {
                failure = "P18-C could not bind the exact committed P11 temporal input.";
                return false;
            }
        }

        IReadOnlyList<ActorDecisionRequestReceipt> receipts = requests.Snapshot();
        histories = BuildHistories(receipts);
        List<ActorChoiceTemporalDecisionRequest> due = new List<ActorChoiceTemporalDecisionRequest>();
        foreach (ActorChoiceInput input in pending)
        {
            if (input.TemporalCapture.TargetInstant.Value > instant.Value) continue;
            if (!histories.TryGetValue(input.InputId.Value, out RequestHistory history)
                || history.CurrentRequest == null || history.OriginatingSequence <= 0L
                || history.LastDisposition != null
                    && history.LastDisposition.Kind != ActorDecisionRequestReceiptKind.RetryableProposal) continue;
            if (history.CurrentRequest.Instant.Value > instant.Value) continue;

            if (!TryCreateAdmittedRequest(input, history, out ActorChoiceTemporalDecisionRequest entry))
            {
                failure = "P18-C request receipt did not reconstruct its exact request identity.";
                return false;
            }
            due.Add(entry);
        }

        List<ActorChoiceTemporalDecisionRequest> selected = new List<ActorChoiceTemporalDecisionRequest>();
        foreach (IGrouping<string, ActorChoiceTemporalDecisionRequest> actorGroup in due
            .GroupBy(entry => entry.Input.PersonId.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            List<ActorChoiceTemporalDecisionRequest> ordered = actorGroup
                .OrderBy(entry => entry.OriginatingBoundarySequence)
                .ThenBy(entry => entry.Input.InputSequence)
                .ToList();
            selected.Add(ordered[0]);
            for (int i = 1; i < ordered.Count; i++)
            {
                ActorChoiceTemporalDecisionRequest deferred = ordered[i];
                string boundaryId = DecisionBoundaryId(instant);
                if (!requests.TryDefer(OperationId("defer", deferred.Request.Id,
                        instant.Value.ToString(CultureInfo.InvariantCulture)), deferred.Request.Id,
                    deferred.Input.InputId.Value, deferred.Input.PersonId, instant, boundaryId, 0L,
                    deferred.Request.BoundarySequence, ActorDecisionDeferralReason.DecisionBoundaryAlreadyUsed,
                    out _))
                {
                    failure = "P18-C could not record same-boundary ActorChoice deferral.";
                    return false;
                }
            }
        }

        admitted = Array.AsReadOnly(selected
            .OrderBy(entry => entry.Request.BoundarySequence)
            .ThenBy(entry => entry.Input.PersonId.Value, StringComparer.Ordinal)
            .ToArray());
        return true;
    }

    /// <summary>
    /// Reissues one deferred retained input after a later meaningful trigger.
    /// The caller supplies the trigger's stable source identity and sequence;
    /// P18-C allocates the new decision request sequence.
    /// </summary>
    public bool TryObserveMeaningfulTrigger(PersonId actor, string triggerId, LogicalTick instant,
        string boundaryId, long boundaryRevision, long sourceSequence,
        out ActorChoiceTemporalDecisionRequest admitted, out string failure)
    {
        admitted = null;
        failure = null;
        if (actor == null || string.IsNullOrWhiteSpace(triggerId) || string.IsNullOrWhiteSpace(boundaryId)
            || boundaryRevision < 0L || sourceSequence <= 0L)
        {
            failure = "P18-D trigger identity is incomplete.";
            return false;
        }

        IReadOnlyList<ActorDecisionRequestReceipt> receipts = requests.Snapshot();
        foreach (ActorDecisionRequestReceipt existing in receipts)
        {
            if (existing.Kind != ActorDecisionRequestReceiptKind.TriggerObserved
                || existing.TriggerId != triggerId || !existing.Actor.Equals(actor)) continue;
            if (existing.Instant != instant || existing.BoundaryId != boundaryId
                || existing.BoundaryRevision != boundaryRevision || existing.SourceSequence != sourceSequence
                || !actorChoices.TryGet(new ActorChoiceInputId(existing.InputId), out ActorChoiceInput priorInput)
                || priorInput.TemporalCapture == null
                || !string.Equals(priorInput.TemporalCapture.ProfileId, profileId, StringComparison.Ordinal)
                || !TryGetHistory(existing.InputId, receipts, out RequestHistory priorHistory)
                || priorHistory.CurrentRequest == null || priorHistory.CurrentRequest.RequestId != existing.RequestId)
            {
                failure = "P18-D trigger identity conflicts with its retained P18-C receipt.";
                return false;
            }

            // A replay of a trigger is idempotent, but it must not re-admit a
            // request that was already deferred at this boundary or reached a
            // terminal outcome. The caller can safely retry only the exact
            // request whose owner receipt remains retryable.
            bool retryableDispatch = priorInput.Status == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt
                && priorHistory.LastDisposition?.Kind == ActorDecisionRequestReceiptKind.RetryableProposal;
            if (priorInput.Status != ActorChoiceInputStatus.Pending && !retryableDispatch
                || priorHistory.LastDisposition != null
                    && priorHistory.LastDisposition.Kind != ActorDecisionRequestReceiptKind.RetryableProposal)
                return true;
            admitted = CreateAdmittedRequest(priorInput, priorHistory);
            return true;
        }

        List<ActorChoiceInput> candidates = actorChoices.PendingInputs
            .Where(input => input.TemporalCapture != null
                && string.Equals(input.TemporalCapture.ProfileId, profileId, StringComparison.Ordinal)
                && input.PersonId.Equals(actor)
                && input.TemporalCapture.TargetInstant.Value <= instant.Value)
            .ToList();
        RequestHistory selected = null;
        ActorChoiceInput selectedInput = null;
        foreach (ActorChoiceInput input in candidates)
        {
            if (!TryGetHistory(input.InputId.Value, receipts, out RequestHistory history)
                || history.CurrentRequest == null || history.LastDisposition == null
                || history.LastDisposition.Kind != ActorDecisionRequestReceiptKind.Deferred
                || history.LastDisposition.Instant.Value >= instant.Value) continue;
            if (selected == null || history.OriginatingSequence < selected.OriginatingSequence
                || history.OriginatingSequence == selected.OriginatingSequence && input.InputSequence < selectedInput.InputSequence)
            { selected = history; selectedInput = input; }
        }
        if (selected == null)
        {
            failure = "No older deferred ActorChoice input is eligible for this later trigger.";
            return false;
        }

        long nextSequence = requests.NextBoundarySequence;
        string derivedRequestId = new ActorDecisionRequest(actor, instant, boundaryId, nextSequence, boundaryRevision).Id;
        if (!requests.TryObserveTrigger(OperationId("trigger", selectedInput.InputId.Value, triggerId),
            derivedRequestId, triggerId, actor, instant, boundaryId, boundaryRevision, sourceSequence,
            selectedInput.InputId.Value, out ActorDecisionRequestReceipt observed))
        {
            failure = "P18-C could not observe the later ActorChoice trigger.";
            return false;
        }
        if (!TryGetHistory(selectedInput.InputId.Value, requests.Snapshot(), out RequestHistory updated)
            || updated.CurrentRequest == null || updated.CurrentRequest.RequestId != observed.RequestId
            || !TryCreateAdmittedRequest(selectedInput, updated, out admitted))
        {
            failure = "P18-C later-trigger receipt did not reconstruct its exact request.";
            return false;
        }
        return true;
    }

    private bool TryGetPendingInput(string inputId, out ActorChoiceInput input)
    {
        input = actorChoices.PendingInputs.FirstOrDefault(candidate => candidate.InputId.Value == inputId
            && candidate.TemporalCapture != null
            && string.Equals(candidate.TemporalCapture.ProfileId, profileId, StringComparison.Ordinal));
        return input != null;
    }

    private bool TryGetHistory(string inputId, IReadOnlyList<ActorDecisionRequestReceipt> receipts,
        out RequestHistory history)
    {
        history = BuildHistories(receipts).TryGetValue(inputId, out RequestHistory found) ? found : null;
        return history != null;
    }

    private static Dictionary<string, RequestHistory> BuildHistories(
        IReadOnlyList<ActorDecisionRequestReceipt> receipts)
    {
        Dictionary<string, RequestHistory> result = new Dictionary<string, RequestHistory>(StringComparer.Ordinal);
        foreach (ActorDecisionRequestReceipt receipt in receipts)
        {
            if (receipt.Kind == ActorDecisionRequestReceiptKind.RequestBound)
            {
                if (!result.TryGetValue(receipt.InputId, out RequestHistory history))
                    result.Add(receipt.InputId, history = new RequestHistory());
                history.OriginatingSequence = receipt.BoundarySequence;
                history.OriginatingRequest = receipt;
                history.CurrentRequest = receipt;
                history.LastDisposition = null;
            }
            else if (receipt.Kind == ActorDecisionRequestReceiptKind.TriggerObserved)
            {
                if (!result.TryGetValue(receipt.InputId, out RequestHistory history))
                    result.Add(receipt.InputId, history = new RequestHistory());
                history.CurrentRequest = receipt;
                history.LastDisposition = null;
            }
            else if (result.TryGetValue(receipt.InputId, out RequestHistory history)
                && history.CurrentRequest != null && history.CurrentRequest.RequestId == receipt.RequestId)
            {
                history.LastDisposition = receipt;
            }
        }
        return result;
    }

    private bool TryCreateAdmittedRequest(ActorChoiceInput input, RequestHistory history,
        out ActorChoiceTemporalDecisionRequest admitted)
    {
        admitted = null;
        if (input?.PersonId == null || input.TemporalCapture == null || history?.CurrentRequest == null
            || history.OriginatingRequest == null || history.OriginatingSequence <= 0L
            || !string.Equals(input.TemporalCapture.ProfileId, profileId, StringComparison.Ordinal)) return false;
        ActorDecisionRequestReceipt origin = history.OriginatingRequest;
        if (origin.Kind != ActorDecisionRequestReceiptKind.RequestBound
            || origin.InputId != input.InputId.Value || !origin.Actor.Equals(input.PersonId)
            || origin.Instant != input.TemporalCapture.TargetInstant
            || origin.BoundaryId != input.InputId.Value || origin.BoundaryRevision != 0L
            || origin.SourceSequence != input.InputSequence || origin.ProfileId != profileId
            || origin.Outcome != input.TemporalCapture.AcceptedInput.InputId
            || origin.BoundarySequence != history.OriginatingSequence) return false;
        ActorDecisionRequestReceipt receipt = history.CurrentRequest;
        if (receipt.InputId != input.InputId.Value || !receipt.Actor.Equals(input.PersonId)
            || receipt.BoundarySequence < history.OriginatingSequence
            || receipt.Instant.Value < input.TemporalCapture.TargetInstant.Value
            || (receipt.Kind != ActorDecisionRequestReceiptKind.RequestBound
                && receipt.Kind != ActorDecisionRequestReceiptKind.TriggerObserved)) return false;
        ActorDecisionRequest request;
        try { request = new ActorDecisionRequest(receipt.Actor, receipt.Instant, receipt.BoundaryId,
            receipt.BoundarySequence, receipt.BoundaryRevision); }
        catch (ArgumentException) { return false; }
        if (!string.Equals(request.Id, receipt.RequestId, StringComparison.Ordinal)) return false;
        string retryable = history.LastDisposition?.Kind == ActorDecisionRequestReceiptKind.RetryableProposal
            ? history.LastDisposition.ProposalId : null;
        admitted = new ActorChoiceTemporalDecisionRequest(input, request, history.OriginatingSequence, retryable);
        return true;
    }

    private ActorChoiceTemporalDecisionRequest CreateAdmittedRequest(ActorChoiceInput input, RequestHistory history)
    {
        if (!TryCreateAdmittedRequest(input, history, out ActorChoiceTemporalDecisionRequest admitted))
            throw new InvalidOperationException("A committed P18-C receipt has an invalid ActorDecisionRequest identity.");
        return admitted;
    }

    private string OperationId(params string[] parts) => Encode("p18d-actor-choice", profileId, parts);

    private string DecisionBoundaryId(LogicalTick instant) => Encode("p18d-decision-boundary", profileId,
        new[] { instant.Value.ToString(CultureInfo.InvariantCulture) });

    private static string Encode(string prefix, string profile, IEnumerable<string> parts)
    {
        List<string> fields = new List<string> { prefix, profile };
        fields.AddRange(parts);
        return string.Join("", fields.Select(value => (value ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture)
            + ":" + (value ?? string.Empty)));
    }

    private sealed class RequestHistory
    {
        public long OriginatingSequence;
        public ActorDecisionRequestReceipt OriginatingRequest;
        public ActorDecisionRequestReceipt CurrentRequest;
        public ActorDecisionRequestReceipt LastDisposition;
    }
}
