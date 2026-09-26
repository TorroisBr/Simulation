using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

/// <summary>
/// Owns captured actor choices and their one-shot lifecycle. Choices are values
/// keyed by stable PersonId; execution objects remain owned by the runtime.
/// </summary>
public sealed class ActorChoiceStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly List<ActorChoiceInput> inputs = new List<ActorChoiceInput>();
    private readonly Dictionary<string, int> indexByInputId = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly HashSet<string> worldCommandIds = new HashSet<string>(StringComparer.Ordinal);
    private long nextInputSequence = 1L;

    internal PersonStore PersonStore => personStore;

    public ActorChoiceStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    public int Count => inputs.Count;

    public IReadOnlyList<ActorChoiceInput> Inputs
    {
        get
        {
            List<ActorChoiceInput> snapshot = new List<ActorChoiceInput>(inputs.Count);
            foreach (ActorChoiceInput input in inputs)
            {
                snapshot.Add(input.Copy());
            }

            return new ReadOnlyCollection<ActorChoiceInput>(snapshot);
        }
    }

    public IReadOnlyList<ActorChoiceInput> PendingInputs
    {
        get
        {
            List<ActorChoiceInput> snapshot = new List<ActorChoiceInput>();
            foreach (ActorChoiceInput input in inputs)
            {
                if (input.Status == ActorChoiceInputStatus.Pending)
                {
                    snapshot.Add(input.Copy());
                }
            }

            return new ReadOnlyCollection<ActorChoiceInput>(snapshot);
        }
    }

    /// <summary>
    /// Captures a trusted request as stable values. This does not check grants,
    /// ownership, or any security principal; current domain eligibility is
    /// revalidated by the runtime when the actor's turn arrives.
    /// </summary>
    public bool TryCapture(
        string worldCommandId,
        PersonId personId,
        string actionDefinitionId,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        long capturedAbsoluteDay,
        out ActorChoiceInput input,
        out ActorChoiceStoreFailureCode failure)
    {
        input = null;
        if (!mutationGuardBinding.CanMutate)
        {
            failure = ActorChoiceStoreFailureCode.RuntimeFaulted;
            return false;
        }

        if (string.IsNullOrWhiteSpace(worldCommandId)
            || personId == null
            || string.IsNullOrWhiteSpace(actionDefinitionId)
            || !Enum.IsDefined(typeof(WorldCommandOrigin), origin)
            || !Enum.IsDefined(typeof(WorldCommandAuthorityMode), authority)
            || capturedAbsoluteDay < 0L)
        {
            failure = ActorChoiceStoreFailureCode.InvalidInput;
            return false;
        }

        if (worldCommandIds.Contains(worldCommandId))
        {
            failure = ActorChoiceStoreFailureCode.DuplicateWorldCommandId;
            return false;
        }

        if (nextInputSequence == long.MaxValue)
        {
            failure = ActorChoiceStoreFailureCode.SequenceExhausted;
            return false;
        }

        long inputSequence = nextInputSequence;
        ActorChoiceInputId inputId = new ActorChoiceInputId(
            "actor-choice-" + inputSequence.ToString("D6", CultureInfo.InvariantCulture));
        ActorChoiceInput captured = new ActorChoiceInput(
            inputId,
            worldCommandId,
            inputSequence,
            personId,
            actionDefinitionId,
            origin,
            authority,
            capturedAbsoluteDay,
            ActorChoiceInputStatus.Pending,
            Array.Empty<ActorChoiceDisposition>());

        inputs.Add(captured);
        indexByInputId.Add(inputId.Value, inputs.Count - 1);
        worldCommandIds.Add(worldCommandId);
        nextInputSequence++;
        input = captured.Copy();
        failure = ActorChoiceStoreFailureCode.None;
        return true;
    }

    public bool TryGet(ActorChoiceInputId inputId, out ActorChoiceInput input)
    {
        input = null;
        if (inputId == null || !indexByInputId.TryGetValue(inputId.Value, out int index))
        {
            return false;
        }

        input = inputs[index].Copy();
        return true;
    }

    /// <summary>Returns the earliest pending choice for this actor.</summary>
    public bool TryGetNextPendingForActor(PersonId personId, out ActorChoiceInput input)
    {
        input = null;
        if (personId == null)
        {
            return false;
        }

        foreach (ActorChoiceInput candidate in inputs)
        {
            if (candidate.Status == ActorChoiceInputStatus.Pending
                && candidate.PersonId.Equals(personId))
            {
                input = candidate.Copy();
                return true;
            }
        }

        return false;
    }

    public bool TryDefer(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        ActorChoiceDeferralReason reason,
        out ActorChoiceStoreFailureCode failure)
    {
        if (!CanBeginTransition(inputId, absoluteDay, actorTurnRosterOrdinal, out ActorChoiceInput current, out failure))
        {
            return false;
        }

        if (current.Status != ActorChoiceInputStatus.Pending)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            return false;
        }

        if (!Enum.IsDefined(typeof(ActorChoiceDeferralReason), reason))
        {
            failure = ActorChoiceStoreFailureCode.InvalidReason;
            return false;
        }

        return Append(
            current,
            ActorChoiceInputStatus.Pending,
            new ActorChoiceDisposition(
                NextTransitionOrdinal(current),
                ActorChoiceDispositionKind.Deferred,
                absoluteDay,
                actorTurnRosterOrdinal,
                deferralReason: reason),
            out failure);
    }

    public bool TryReject(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        ActorChoiceFailure reason,
        out ActorChoiceStoreFailureCode failure)
    {
        if (!CanBeginTransition(inputId, absoluteDay, actorTurnRosterOrdinal, out ActorChoiceInput current, out failure))
        {
            return false;
        }

        if (current.Status != ActorChoiceInputStatus.Pending)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            return false;
        }

        if (!Enum.IsDefined(typeof(ActorChoiceFailure), reason))
        {
            failure = ActorChoiceStoreFailureCode.InvalidReason;
            return false;
        }

        return Append(
            current,
            ActorChoiceInputStatus.Rejected,
            new ActorChoiceDisposition(
                NextTransitionOrdinal(current),
                ActorChoiceDispositionKind.Rejected,
                absoluteDay,
                actorTurnRosterOrdinal,
                failure: reason),
            out failure);
    }

    public bool TryMarkDispatchStarted(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        string decisionRecordId,
        out ActorChoiceStoreFailureCode failure)
    {
        if (!CanBeginTransition(inputId, absoluteDay, actorTurnRosterOrdinal, out ActorChoiceInput current, out failure))
        {
            return false;
        }

        if (current.Status != ActorChoiceInputStatus.Pending)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            return false;
        }

        return Append(
            current,
            ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt,
            new ActorChoiceDisposition(
                NextTransitionOrdinal(current),
                ActorChoiceDispositionKind.DispatchStarted,
                absoluteDay,
                actorTurnRosterOrdinal,
                decisionRecordId: decisionRecordId),
            out failure);
    }

    /// <summary>
    /// Closes a started dispatch using only the returned action result status.
    /// A null result is recorded distinctly and has no result status.
    /// </summary>
    public bool TryRecordAttemptReturned(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        NpcActionResult result,
        out ActorChoiceStoreFailureCode failure)
    {
        if (!CanBeginTransition(inputId, absoluteDay, actorTurnRosterOrdinal, out ActorChoiceInput current, out failure))
        {
            return false;
        }

        if (current.Status != ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            return false;
        }

        NpcActionResultType? resultStatus = result == null ? (NpcActionResultType?)null : result.ResultType;
        if (resultStatus.HasValue && !Enum.IsDefined(typeof(NpcActionResultType), resultStatus.Value))
        {
            failure = ActorChoiceStoreFailureCode.InvalidInput;
            return false;
        }

        ActorChoiceAttemptOutcome outcome = !resultStatus.HasValue
            ? ActorChoiceAttemptOutcome.ReturnedNoResult
            : resultStatus.Value == NpcActionResultType.Success
                ? ActorChoiceAttemptOutcome.Succeeded
                : ActorChoiceAttemptOutcome.Failed;
        return Append(
            current,
            ActorChoiceInputStatus.AttemptReturned,
            new ActorChoiceDisposition(
                NextTransitionOrdinal(current),
                ActorChoiceDispositionKind.AttemptReturned,
                absoluteDay,
                actorTurnRosterOrdinal,
                attemptOutcome: outcome,
                returnedResultStatus: resultStatus),
            out failure);
    }

    /// <summary>
    /// Records the terminal throw after dispatch. The runtime caller then
    /// rethrows the original exception; the store never retries the input.
    /// </summary>
    public bool TryRecordAttemptThrew(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        out ActorChoiceStoreFailureCode failure)
    {
        if (!CanBeginTransition(inputId, absoluteDay, actorTurnRosterOrdinal, out ActorChoiceInput current, out failure))
        {
            return false;
        }

        if (current.Status != ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            return false;
        }

        return Append(
            current,
            ActorChoiceInputStatus.AttemptThrew,
            new ActorChoiceDisposition(
                NextTransitionOrdinal(current),
                ActorChoiceDispositionKind.AttemptThrew,
                absoluteDay,
                actorTurnRosterOrdinal,
                attemptOutcome: ActorChoiceAttemptOutcome.Threw),
            out failure);
    }

    public ActorChoiceInvariantReport ValidateInvariants()
    {
        List<string> issues = new List<string>();
        HashSet<string> seenInputIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> seenCommandIds = new HashSet<string>(StringComparer.Ordinal);
        long previousSequence = 0L;

        for (int i = 0; i < inputs.Count; i++)
        {
            ActorChoiceInput input = inputs[i];
            if (input == null)
            {
                issues.Add("Actor choice store contains a null input.");
                continue;
            }

            if (input.InputId == null || string.IsNullOrWhiteSpace(input.InputId.Value)
                || !seenInputIds.Add(input.InputId.Value))
            {
                issues.Add("Actor choice input identity is missing or duplicated.");
            }

            if (string.IsNullOrWhiteSpace(input.WorldCommandId)
                || !seenCommandIds.Add(input.WorldCommandId))
            {
                issues.Add("Actor choice WorldCommandId is missing or duplicated.");
            }

            if (input.InputSequence <= previousSequence || input.InputSequence <= 0L)
            {
                issues.Add("Actor choice input sequences must be positive and strictly increasing.");
            }

            previousSequence = input.InputSequence;
            if (input.PersonId == null || string.IsNullOrWhiteSpace(input.ActionDefinitionId)
                || input.CapturedAbsoluteDay < 0L
                || !Enum.IsDefined(typeof(WorldCommandOrigin), input.Origin)
                || !Enum.IsDefined(typeof(WorldCommandAuthorityMode), input.Authority))
            {
                issues.Add("Actor choice capture values are invalid.");
            }

            ValidateInputLifecycle(input, issues);
            if (input.InputId != null
                && (!indexByInputId.TryGetValue(input.InputId.Value, out int indexedAt) || indexedAt != i))
            {
                issues.Add("Actor choice identity index does not match its record order.");
            }
        }

        if (nextInputSequence <= previousSequence)
        {
            issues.Add("Actor choice next input sequence must exceed every captured sequence.");
        }

        if (indexByInputId.Count != inputs.Count)
        {
            issues.Add("Actor choice identity index size does not match its receipts.");
        }

        if (seenCommandIds.Count != worldCommandIds.Count)
        {
            issues.Add("Actor choice command correlation index does not match its receipts.");
        }
        else
        {
            foreach (string commandId in seenCommandIds)
            {
                if (!worldCommandIds.Contains(commandId))
                {
                    issues.Add("Actor choice command correlation index is missing a receipt.");
                }
            }
        }

        return new ActorChoiceInvariantReport(issues);
    }

    internal ActorChoiceStore Clone(
        PersonStore targetPersonStore,
        AuthoritativeMutationGuard targetMutationGuard = null)
    {
        if (targetPersonStore == null) throw new ArgumentNullException(nameof(targetPersonStore));

        ActorChoiceStore copy = new ActorChoiceStore(targetPersonStore)
        {
            nextInputSequence = nextInputSequence
        };
        foreach (ActorChoiceInput input in inputs)
        {
            ActorChoiceInput clonedInput = input.Copy();
            copy.indexByInputId.Add(clonedInput.InputId.Value, copy.inputs.Count);
            copy.inputs.Add(clonedInput);
            copy.worldCommandIds.Add(clonedInput.WorldCommandId);
        }

        if (targetMutationGuard != null && !copy.TryBindMutationGuard(targetMutationGuard))
        {
            throw new InvalidOperationException("Actor choice clone could not bind to the target runtime mutation guard.");
        }

        return copy;
    }

    private bool CanBeginTransition(
        ActorChoiceInputId inputId,
        long absoluteDay,
        int actorTurnRosterOrdinal,
        out ActorChoiceInput current,
        out ActorChoiceStoreFailureCode failure)
    {
        current = null;
        if (!mutationGuardBinding.CanMutate)
        {
            failure = ActorChoiceStoreFailureCode.RuntimeFaulted;
            return false;
        }

        if (absoluteDay < 0L || actorTurnRosterOrdinal < 0)
        {
            failure = ActorChoiceStoreFailureCode.InvalidBoundary;
            return false;
        }

        if (inputId == null || !indexByInputId.TryGetValue(inputId.Value, out int index))
        {
            failure = ActorChoiceStoreFailureCode.InputNotFound;
            return false;
        }

        current = inputs[index];
        if (absoluteDay < current.CapturedAbsoluteDay)
        {
            failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
            current = null;
            return false;
        }

        if (current.Dispositions.Count > 0)
        {
            ActorChoiceDisposition previous = current.Dispositions[current.Dispositions.Count - 1];
            if (absoluteDay < previous.AbsoluteDay
                || (absoluteDay == previous.AbsoluteDay
                    && actorTurnRosterOrdinal < previous.ActorTurnRosterOrdinal))
            {
                failure = ActorChoiceStoreFailureCode.InvalidLifecycleTransition;
                current = null;
                return false;
            }
        }

        failure = ActorChoiceStoreFailureCode.None;
        return true;
    }

    private bool Append(
        ActorChoiceInput current,
        ActorChoiceInputStatus nextStatus,
        ActorChoiceDisposition disposition,
        out ActorChoiceStoreFailureCode failure)
    {
        if (current.Dispositions.Count == int.MaxValue)
        {
            failure = ActorChoiceStoreFailureCode.SequenceExhausted;
            return false;
        }

        string id = current.InputId.Value;
        int index = indexByInputId[id];
        inputs[index] = current.WithDisposition(nextStatus, disposition);
        failure = ActorChoiceStoreFailureCode.None;
        return true;
    }

    private static long NextTransitionOrdinal(ActorChoiceInput input)
    {
        return (long)input.Dispositions.Count + 1L;
    }

    private static void ValidateInputLifecycle(ActorChoiceInput input, List<string> issues)
    {
        bool dispatchStarted = false;
        bool terminal = false;
        ActorChoiceInputStatus derivedStatus = ActorChoiceInputStatus.Pending;
        long expectedOrdinal = 1L;

        if (input.Dispositions == null)
        {
            issues.Add("Actor choice disposition history is missing.");
            return;
        }

        foreach (ActorChoiceDisposition disposition in input.Dispositions)
        {
            if (disposition == null)
            {
                issues.Add("Actor choice disposition history contains a null entry.");
                continue;
            }

            if (disposition.TransitionOrdinal != expectedOrdinal++)
            {
                issues.Add("Actor choice disposition ordinals must be contiguous and ordered.");
            }

            if (disposition.AbsoluteDay < 0L || disposition.ActorTurnRosterOrdinal < 0)
            {
                issues.Add("Actor choice disposition boundary values must be nonnegative.");
            }

            if (disposition.AbsoluteDay < input.CapturedAbsoluteDay)
            {
                issues.Add("Actor choice disposition cannot predate its capture day.");
            }

            if (terminal)
            {
                issues.Add("Actor choice disposition follows a terminal outcome.");
                continue;
            }

            switch (disposition.Kind)
            {
                case ActorChoiceDispositionKind.Deferred:
                    if (dispatchStarted || disposition.DeferralReason == null
                        || !Enum.IsDefined(typeof(ActorChoiceDeferralReason), disposition.DeferralReason.Value)
                        || disposition.Failure.HasValue || disposition.AttemptOutcome.HasValue
                        || disposition.ReturnedResultStatus.HasValue)
                    {
                        issues.Add("Actor choice deferral is not valid in this lifecycle state.");
                    }
                    derivedStatus = ActorChoiceInputStatus.Pending;
                    break;

                case ActorChoiceDispositionKind.Rejected:
                    if (dispatchStarted || disposition.Failure == null
                        || !Enum.IsDefined(typeof(ActorChoiceFailure), disposition.Failure.Value)
                        || disposition.DeferralReason.HasValue || disposition.AttemptOutcome.HasValue
                        || disposition.ReturnedResultStatus.HasValue)
                    {
                        issues.Add("Actor choice rejection is not valid in this lifecycle state.");
                    }
                    derivedStatus = ActorChoiceInputStatus.Rejected;
                    terminal = true;
                    break;

                case ActorChoiceDispositionKind.DispatchStarted:
                    if (dispatchStarted || disposition.DeferralReason.HasValue || disposition.Failure.HasValue
                        || disposition.AttemptOutcome.HasValue || disposition.ReturnedResultStatus.HasValue)
                    {
                        issues.Add("Actor choice dispatch marker is duplicated or carries an outcome.");
                    }
                    dispatchStarted = true;
                    derivedStatus = ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt;
                    break;

                case ActorChoiceDispositionKind.AttemptReturned:
                    if (!dispatchStarted || !disposition.AttemptOutcome.HasValue
                        || (disposition.AttemptOutcome.Value != ActorChoiceAttemptOutcome.Succeeded
                            && disposition.AttemptOutcome.Value != ActorChoiceAttemptOutcome.Failed
                            && disposition.AttemptOutcome.Value != ActorChoiceAttemptOutcome.ReturnedNoResult)
                        || disposition.DeferralReason.HasValue || disposition.Failure.HasValue
                        || HasResultStatusMismatch(disposition))
                    {
                        issues.Add("Actor choice returned attempt must follow dispatch and match its result status.");
                    }
                    derivedStatus = ActorChoiceInputStatus.AttemptReturned;
                    terminal = true;
                    break;

                case ActorChoiceDispositionKind.AttemptThrew:
                    if (!dispatchStarted
                        || disposition.AttemptOutcome != ActorChoiceAttemptOutcome.Threw
                        || disposition.ReturnedResultStatus.HasValue
                        || disposition.DeferralReason.HasValue || disposition.Failure.HasValue)
                    {
                        issues.Add("Actor choice thrown attempt must follow dispatch and have no returned result status.");
                    }
                    derivedStatus = ActorChoiceInputStatus.AttemptThrew;
                    terminal = true;
                    break;

                default:
                    issues.Add("Actor choice disposition kind is invalid.");
                    break;
            }
        }

        if (derivedStatus != input.Status)
        {
            issues.Add("Actor choice status does not match its disposition history.");
        }

        if (input.Status == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt && !dispatchStarted)
        {
            issues.Add("Consumed actor choice must have exactly one dispatch marker.");
        }

        if ((input.Status == ActorChoiceInputStatus.AttemptReturned
                || input.Status == ActorChoiceInputStatus.AttemptThrew)
            && !dispatchStarted)
        {
            issues.Add("Terminal actor choice attempt must have a prior dispatch marker.");
        }
    }

    private static bool HasResultStatusMismatch(ActorChoiceDisposition disposition)
    {
        if (disposition.AttemptOutcome == ActorChoiceAttemptOutcome.Succeeded)
        {
            return disposition.ReturnedResultStatus != NpcActionResultType.Success;
        }

        if (disposition.AttemptOutcome == ActorChoiceAttemptOutcome.Failed)
        {
            return disposition.ReturnedResultStatus != NpcActionResultType.Failure;
        }

        return disposition.AttemptOutcome == ActorChoiceAttemptOutcome.ReturnedNoResult
            && disposition.ReturnedResultStatus.HasValue;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }
}
