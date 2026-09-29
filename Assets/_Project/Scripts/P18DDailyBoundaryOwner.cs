using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Builds immutable descriptors and prepares owner-owned commits for one family
/// of P18-D daily-boundary operations. Implementations must keep effect,
/// occurrence receipt, and returned outputs in the same domain-owner commit.
/// </summary>
public interface IP18DDailyBoundaryStepProvider
{
    bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure);

    bool OwnsStep(BoundaryContinuationStep step);

    bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure);
}

/// <summary>
/// Handoff for signals retained by committed P18-D daily owner steps. Implementations
/// must make retries idempotent by manifest/receipt identity because a handoff response
/// can be uncertain after the source accepts its signals.
/// </summary>
public interface IP18DDailyBoundarySignalSink
{
    bool TryHandoff(BoundaryContinuationManifest manifest, IReadOnlyList<string> signals,
        out TimelineFailure failure);
}

/// <summary>
/// P18-D's resumable manifest/cursor owner. It composes domain step providers,
/// while each provider remains the sole authority for its effect and occurrence
/// receipt. A host must hold its serialized runtime-advance ownership window
/// around activation, step dispatch, publication, and post-advance handoff.
/// </summary>
public sealed class P18DDailyBoundaryOwner : IResumableDayBoundaryOwner, IBoundarySourceSignalHandoff
{
    private const string SubphaseKind = "p18.daily-boundary";
    private const string SubphaseVersion = "1";

    private readonly string worldId;
    private readonly string profileId;
    private readonly string configurationIdentity;
    private readonly string contentIdentity;
    private readonly IReadOnlyList<IP18DDailyBoundaryStepProvider> providers;
    private readonly IP18DDailyBoundarySignalSink signalSink;
    private readonly Dictionary<string, BoundaryContinuationState> continuations =
        new Dictionary<string, BoundaryContinuationState>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> consumedOccurrences =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public P18DDailyBoundaryOwner(string worldId, string profileId,
        string configurationIdentity, string contentIdentity,
        IReadOnlyList<IP18DDailyBoundaryStepProvider> providers,
        IP18DDailyBoundarySignalSink signalSink = null)
    {
        if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required.", nameof(worldId));
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required.", nameof(profileId));
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        this.worldId = worldId;
        this.profileId = profileId;
        this.configurationIdentity = configurationIdentity ?? string.Empty;
        this.contentIdentity = contentIdentity ?? string.Empty;
        this.providers = new List<IP18DDailyBoundaryStepProvider>(providers).AsReadOnly();
        this.signalSink = signalSink;
        foreach (IP18DDailyBoundaryStepProvider provider in this.providers)
            if (provider == null) throw new ArgumentException("Boundary step providers cannot be null.", nameof(providers));
    }

    public bool TryPrepare(DailyBoundaryOperation operation, out IDayBoundaryCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryPrepareActivation(operation, out IBoundaryActivationCommit activation, out failure)) return false;
        prepared = new LegacyActivationCommit(activation);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryPrepareActivation(DailyBoundaryOperation operation,
        out IBoundaryActivationCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (operation == null || operation.WorldId != worldId || operation.ProfileId != profileId)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        string continuationId = SpatialStableKey.Encode(operation.OccurrenceId, SubphaseKind, SubphaseVersion);
        if (continuations.TryGetValue(continuationId, out BoundaryContinuationState existing))
        {
            prepared = new ActivationCommit(this, existing.Manifest, existing);
            failure = TimelineFailure.None;
            return true;
        }

        if (consumedOccurrences.TryGetValue(operation.OccurrenceId, out string consumedContinuation)
            && !string.Equals(consumedContinuation, continuationId, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        List<BoundaryContinuationStep> steps = new List<BoundaryContinuationStep>();
        foreach (IP18DDailyBoundaryStepProvider provider in providers)
        {
            if (!provider.TryCreateSteps(operation, steps.Count,
                out IReadOnlyList<BoundaryContinuationStep> created, out failure)
                || created == null)
            {
                if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
                return false;
            }
            foreach (BoundaryContinuationStep step in created)
            {
                if (step == null || step.Ordinal != steps.Count)
                {
                    failure = TimelineFailure.ContinuationFailed;
                    return false;
                }
                int owners = 0;
                foreach (IP18DDailyBoundaryStepProvider candidate in providers)
                    if (candidate.OwnsStep(step)) owners++;
                if (owners != 1)
                {
                    failure = TimelineFailure.ContinuationFailed;
                    return false;
                }
                steps.Add(step);
            }
        }
        if (steps.Count == 0)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        BoundaryContinuationManifest manifest;
        try
        {
            manifest = new BoundaryContinuationManifest(operation, SubphaseKind, SubphaseVersion,
                configurationIdentity, steps.AsReadOnly(), contentIdentity);
        }
        catch (ArgumentException)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        BoundaryContinuationState initial = new BoundaryContinuationState(manifest, 0, false,
            false, Array.Empty<BoundaryPublishedFact>(), Array.Empty<DueWorkReference>(),
            Array.Empty<string>(), false);
        prepared = new ActivationCommit(this, manifest, initial);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryResolveContinuation(string continuationId,
        out BoundaryContinuationState state, out TimelineFailure failure)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(continuationId)
            || !continuations.TryGetValue(continuationId, out state))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryPrepareStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetCurrentState(manifest, out BoundaryContinuationState state)
            || step == null || step.Ordinal != state.NextStepOrdinal
            || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        IP18DDailyBoundaryStepProvider owner = null;
        foreach (IP18DDailyBoundaryStepProvider candidate in providers)
        {
            if (!candidate.OwnsStep(step)) continue;
            if (owner != null)
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
            owner = candidate;
        }
        if (owner == null)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        if (!owner.TryPrepareStep(manifest, step,
            out IBoundaryContinuationStepCommit domainCommit, out failure) || domainCommit == null
            || domainCommit.RetainedTimelineFacts == null || domainCommit.RetainedSourceSignals == null)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (!TryAppend(state.RetainedTimelineFacts, domainCommit.RetainedTimelineFacts,
                out IReadOnlyList<DueWorkReference> nextFacts)
            || !TryAppend(state.RetainedSourceSignals, domainCommit.RetainedSourceSignals,
                out IReadOnlyList<string> nextSignals))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        HashSet<string> factIdentities = new HashSet<string>(StringComparer.Ordinal);
        foreach (DueWorkReference fact in nextFacts)
            if (!factIdentities.Add(Identity(fact)))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
        HashSet<string> signalIdentities = new HashSet<string>(StringComparer.Ordinal);
        foreach (string signal in nextSignals)
            if (string.IsNullOrWhiteSpace(signal) || !signalIdentities.Add(signal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
        bool complete = step.Ordinal + 1 == manifest.Steps.Count;
        BoundaryContinuationState next = new BoundaryContinuationState(manifest,
            step.Ordinal + 1, complete, false, Array.Empty<BoundaryPublishedFact>(),
            nextFacts, nextSignals, false);
        prepared = new StepCommit(this, state, next, domainCommit);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryPrepareTimelinePublication(BoundaryContinuationManifest manifest,
        out IBoundaryTimelinePublicationCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetCurrentState(manifest, out BoundaryContinuationState state) || !state.IsComplete)
        {
            failure = TimelineFailure.PublicationFailed;
            return false;
        }
        prepared = new TimelinePublicationCommit(this, state);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryHandoff(BoundaryContinuationManifest manifest,
        IReadOnlyList<string> retainedSignals, out TimelineFailure failure)
    {
        if (!TryGetCurrentState(manifest, out BoundaryContinuationState state)
            || !state.IsComplete || retainedSignals == null
            || !EqualStrings(state.RetainedSourceSignals, retainedSignals))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        if (state.SignalsHandedOff)
        {
            failure = TimelineFailure.None;
            return true;
        }
        if (retainedSignals.Count > 0 && signalSink == null)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        if (signalSink != null && !signalSink.TryHandoff(manifest, retainedSignals, out failure))
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        continuations[manifest.ContinuationId] = CopyState(state,
            state.TimelineFactsPublished, state.PublishedFacts, true);
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryGetCurrentState(BoundaryContinuationManifest manifest,
        out BoundaryContinuationState state)
    {
        state = null;
        return manifest != null && manifest.WorldId == worldId && manifest.ProfileId == profileId
            && continuations.TryGetValue(manifest.ContinuationId, out state)
            && state.Manifest.HasSameFrozenContent(manifest);
    }

    private static bool TryAppend<T>(IReadOnlyList<T> existing, IReadOnlyList<T> additions,
        out IReadOnlyList<T> combined)
    {
        combined = null;
        if (existing == null || additions == null) return false;
        List<T> copy = new List<T>(existing.Count + additions.Count);
        foreach (T value in existing)
        {
            if (value is null) return false;
            copy.Add(value);
        }
        foreach (T value in additions)
        {
            if (value == null) return false;
            copy.Add(value);
        }
        combined = copy.AsReadOnly();
        return true;
    }

    private static bool EqualStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal)) return false;
        return true;
    }

    private static BoundaryContinuationState CopyState(BoundaryContinuationState state,
        bool timelineFactsPublished, IReadOnlyList<BoundaryPublishedFact> publishedFacts,
        bool signalsHandedOff) => new BoundaryContinuationState(state.Manifest,
            state.NextStepOrdinal, state.IsComplete, timelineFactsPublished, publishedFacts,
            state.RetainedTimelineFacts, state.RetainedSourceSignals, signalsHandedOff);

    private sealed class ActivationCommit : IBoundaryActivationCommit
    {
        private readonly P18DDailyBoundaryOwner owner;
        private readonly BoundaryContinuationState state;
        public BoundaryContinuationManifest Manifest => state.Manifest;

        public ActivationCommit(P18DDailyBoundaryOwner owner, BoundaryContinuationManifest manifest,
            BoundaryContinuationState state)
        {
            this.owner = owner;
            if (!manifest.HasSameFrozenContent(state.Manifest))
                throw new ArgumentException("Activation manifest and state disagree.", nameof(state));
            this.state = state;
        }

        public bool TryCommit(out TimelineFailure failure)
        {
            if (owner.consumedOccurrences.TryGetValue(Manifest.BoundaryOccurrenceId, out string existingId))
            {
                if (existingId != Manifest.ContinuationId
                    || !owner.continuations.TryGetValue(existingId, out BoundaryContinuationState current)
                    || !current.Manifest.HasSameFrozenContent(Manifest))
                {
                    failure = TimelineFailure.ContinuationFailed;
                    return false;
                }
                failure = TimelineFailure.None;
                return true;
            }
            owner.consumedOccurrences.Add(Manifest.BoundaryOccurrenceId, Manifest.ContinuationId);
            owner.continuations.Add(Manifest.ContinuationId, state);
            failure = TimelineFailure.None;
            return true;
        }
    }

    private sealed class LegacyActivationCommit : IDayBoundaryCommit
    {
        private readonly IBoundaryActivationCommit activation;
        public LegacyActivationCommit(IBoundaryActivationCommit activation) { this.activation = activation; }
        public bool TryCommit(out TimelineFailure failure) => activation.TryCommit(out failure);
    }

    private sealed class StepCommit : IBoundaryContinuationStepCommit
    {
        private readonly P18DDailyBoundaryOwner owner;
        private readonly BoundaryContinuationState expected;
        private readonly BoundaryContinuationState replacement;
        private readonly IBoundaryContinuationStepCommit domainCommit;
        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => domainCommit.RetainedTimelineFacts;
        public IReadOnlyList<string> RetainedSourceSignals => domainCommit.RetainedSourceSignals;

        public StepCommit(P18DDailyBoundaryOwner owner, BoundaryContinuationState expected,
            BoundaryContinuationState replacement, IBoundaryContinuationStepCommit domainCommit)
        {
            this.owner = owner;
            this.expected = expected;
            this.replacement = replacement;
            this.domainCommit = domainCommit;
        }

        public bool TryCommit(out TimelineFailure failure)
        {
            if (!owner.continuations.TryGetValue(expected.Manifest.ContinuationId,
                    out BoundaryContinuationState current) || !ReferenceEquals(current, expected))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }
            if (!domainCommit.TryCommit(out failure)) return false;
            owner.continuations[expected.Manifest.ContinuationId] = replacement;
            failure = TimelineFailure.None;
            return true;
        }
    }

    private sealed class TimelinePublicationCommit : IBoundaryTimelinePublicationCommit
    {
        private readonly P18DDailyBoundaryOwner owner;
        private readonly BoundaryContinuationState expected;
        public TimelinePublicationCommit(P18DDailyBoundaryOwner owner, BoundaryContinuationState expected)
        { this.owner = owner; this.expected = expected; }

        public bool TryCommit(IReadOnlyList<BoundaryPublishedFact> facts, out TimelineFailure failure)
        {
            if (facts == null || !owner.continuations.TryGetValue(expected.Manifest.ContinuationId,
                    out BoundaryContinuationState current)
                || !ReferenceEquals(current, expected))
            {
                failure = TimelineFailure.PublicationFailed;
                return false;
            }
            if (expected.TimelineFactsPublished)
            {
                if (!EqualPublishedFacts(expected.PublishedFacts, facts))
                {
                    failure = TimelineFailure.PublicationFailed;
                    return false;
                }
                failure = TimelineFailure.None;
                return true;
            }
            if (facts.Count != expected.RetainedTimelineFacts.Count)
            {
                failure = TimelineFailure.PublicationFailed;
                return false;
            }
            HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
            HashSet<long> sequences = new HashSet<long>();
            for (int i = 0; i < facts.Count; i++)
            {
                BoundaryPublishedFact published = facts[i];
                DueWorkReference retained = expected.RetainedTimelineFacts[i];
                if (published == null || published.Fact == null || published.CausalSequence <= 0L
                    || !identities.Add(Identity(published.Fact)) || !sequences.Add(published.CausalSequence)
                    || !SameFact(retained, published.Fact))
                {
                    failure = TimelineFailure.PublicationFailed;
                    return false;
                }
            }
            BoundaryContinuationState replacement = CopyState(expected, true,
                new List<BoundaryPublishedFact>(facts).AsReadOnly(), false);
            owner.continuations[expected.Manifest.ContinuationId] = replacement;
            failure = TimelineFailure.None;
            return true;
        }
    }

    private static bool EqualPublishedFacts(IReadOnlyList<BoundaryPublishedFact> left,
        IReadOnlyList<BoundaryPublishedFact> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
            if (left[i] == null || right[i] == null || left[i].CausalSequence != right[i].CausalSequence
                || !SameFact(left[i].Fact, right[i].Fact)) return false;
        return true;
    }

    private static bool SameFact(DueWorkReference left, DueWorkReference right) => left != null && right != null
        && left.OwnerId == right.OwnerId && left.DueWorkId == right.DueWorkId
        && left.InstanceId == right.InstanceId && left.Revision == right.Revision
        && left.OccurrenceSequence == right.OccurrenceSequence && left.DueAt == right.DueAt
        && left.Kind == right.Kind;

    private static string Identity(DueWorkReference fact) => SpatialStableKey.Encode(
        fact.OwnerId, fact.DueWorkId, fact.InstanceId, fact.Revision.ToString(CultureInfo.InvariantCulture),
        fact.OccurrenceSequence.ToString(CultureInfo.InvariantCulture));
}
