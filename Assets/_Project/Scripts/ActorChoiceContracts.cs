using System;
using System.Collections.Generic;

/// <summary>Stable identity for one captured actor choice.</summary>
public sealed class ActorChoiceInputId : IEquatable<ActorChoiceInputId>
{
    private readonly string value;

    public string Value => value;

    public ActorChoiceInputId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Actor choice input identity is required.", nameof(value));
        }

        this.value = value;
    }

    public bool Equals(ActorChoiceInputId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ActorChoiceInputId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(ActorChoiceInputId left, ActorChoiceInputId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ActorChoiceInputId left, ActorChoiceInputId right) => (left == right) == false;
}

public enum ActorChoiceInputStatus
{
    Pending,
    ConsumedAwaitingTerminalAttempt,
    Rejected,
    AttemptReturned,
    AttemptThrew
}

public enum ActorChoiceDispositionKind
{
    Deferred,
    Rejected,
    DispatchStarted,
    AttemptReturned,
    AttemptThrew
}

public enum ActorChoiceDeferralReason
{
    Traveling,
    ExpeditionParticipant,
    ReservedExpeditionActivity,
    ScheduledDirective
}

/// <summary>Domain reason that a choice was rejected before dispatch.</summary>
public enum ActorChoiceFailure
{
    ActorUnavailable,
    ActionUnavailable
}

public enum ActorChoiceAttemptOutcome
{
    Succeeded,
    Failed,
    ReturnedNoResult,
    Threw
}

public enum ActorChoiceStoreFailureCode
{
    None,
    RuntimeFaulted,
    InvalidInput,
    DuplicateWorldCommandId,
    InputNotFound,
    InvalidLifecycleTransition,
    InvalidBoundary,
    InvalidReason,
    SequenceExhausted
}

/// <summary>
/// Immutable logical boundary record. A disposition contains stable values only;
/// it never retains action, NPC, market, or Unity runtime objects.
/// </summary>
public sealed class ActorChoiceDisposition
{
    public long TransitionOrdinal { get; }
    public ActorChoiceDispositionKind Kind { get; }
    public long AbsoluteDay { get; }
    public int ActorTurnRosterOrdinal { get; }
    public ActorChoiceDeferralReason? DeferralReason { get; }
    public ActorChoiceFailure? Failure { get; }
    public string DecisionRecordId { get; }
    public ActorChoiceAttemptOutcome? AttemptOutcome { get; }
    public NpcActionResultType? ReturnedResultStatus { get; }

    internal ActorChoiceDisposition(
        long transitionOrdinal,
        ActorChoiceDispositionKind kind,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        ActorChoiceDeferralReason? deferralReason = null,
        ActorChoiceFailure? failure = null,
        string decisionRecordId = null,
        ActorChoiceAttemptOutcome? attemptOutcome = null,
        NpcActionResultType? returnedResultStatus = null)
    {
        TransitionOrdinal = transitionOrdinal;
        Kind = kind;
        AbsoluteDay = absoluteDay;
        ActorTurnRosterOrdinal = actorTurnRosterOrdinal;
        DeferralReason = deferralReason;
        Failure = failure;
        DecisionRecordId = string.IsNullOrWhiteSpace(decisionRecordId) ? null : decisionRecordId;
        AttemptOutcome = attemptOutcome;
        ReturnedResultStatus = returnedResultStatus;
    }

    internal ActorChoiceDisposition Copy()
    {
        return new ActorChoiceDisposition(
            TransitionOrdinal,
            Kind,
            AbsoluteDay,
            ActorTurnRosterOrdinal,
            DeferralReason,
            Failure,
            DecisionRecordId,
            AttemptOutcome,
            ReturnedResultStatus);
    }
}

/// <summary>
/// Immutable captured input and the immutable disposition history known at the
/// time this value was read. Later store transitions replace the value.
/// </summary>
public sealed class ActorChoiceInput
{
    private readonly IReadOnlyList<ActorChoiceDisposition> dispositions;

    public ActorChoiceInputId InputId { get; }
    public string WorldCommandId { get; }
    public long InputSequence { get; }
    public PersonId PersonId { get; }
    public string ActionDefinitionId { get; }
    public WorldCommandOrigin Origin { get; }
    public WorldCommandAuthorityMode Authority { get; }
    public long CapturedAbsoluteDay { get; }
    public ActorChoiceInputStatus Status { get; }
    public IReadOnlyList<ActorChoiceDisposition> Dispositions => dispositions;

    internal ActorChoiceInput(
        ActorChoiceInputId inputId,
        string worldCommandId,
        long inputSequence,
        PersonId personId,
        string actionDefinitionId,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        long capturedAbsoluteDay,
        ActorChoiceInputStatus status,
        IEnumerable<ActorChoiceDisposition> dispositions)
    {
        InputId = inputId;
        WorldCommandId = worldCommandId;
        InputSequence = inputSequence;
        PersonId = personId;
        ActionDefinitionId = actionDefinitionId;
        Origin = origin;
        Authority = authority;
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Status = status;

        List<ActorChoiceDisposition> copy = new List<ActorChoiceDisposition>();
        if (dispositions != null)
        {
            foreach (ActorChoiceDisposition disposition in dispositions)
            {
                copy.Add(disposition.Copy());
            }
        }

        this.dispositions = copy.AsReadOnly();
    }

    internal ActorChoiceInput WithDisposition(
        ActorChoiceInputStatus status,
        ActorChoiceDisposition disposition)
    {
        List<ActorChoiceDisposition> next = new List<ActorChoiceDisposition>(dispositions.Count + 1);
        foreach (ActorChoiceDisposition existing in dispositions)
        {
            next.Add(existing);
        }

        next.Add(disposition);
        return new ActorChoiceInput(
            InputId,
            WorldCommandId,
            InputSequence,
            PersonId,
            ActionDefinitionId,
            Origin,
            Authority,
            CapturedAbsoluteDay,
            status,
            next);
    }

    internal ActorChoiceInput Copy()
    {
        return new ActorChoiceInput(
            InputId,
            WorldCommandId,
            InputSequence,
            PersonId,
            ActionDefinitionId,
            Origin,
            Authority,
            CapturedAbsoluteDay,
            Status,
            dispositions);
    }
}

public sealed class ActorChoiceInvariantReport
{
    private readonly IReadOnlyList<string> issues;

    public bool IsValid => issues.Count == 0;
    public IReadOnlyList<string> Issues => issues;

    internal ActorChoiceInvariantReport(IEnumerable<string> issues)
    {
        List<string> copy = issues == null ? new List<string>() : new List<string>(issues);
        this.issues = copy.AsReadOnly();
    }
}
