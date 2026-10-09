using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12FActorChoiceSnapshotFailure
{
    None,
    InvalidBoundary,
    MissingOrMismatchedOwnerSection,
    UnsupportedHistory,
    PersonBindingMismatch,
    StagingFailed
}

/// <summary>
/// Detached terminal P11 ActorChoice values for the bounded P12-F Daily-v1
/// continuation. This value contains no source store or owner identity.
/// </summary>
internal sealed class P12FActorChoiceSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    private readonly IReadOnlyList<ActorChoiceInput> inputs;

    private P12FActorChoiceSnapshot(
        int schemaVersion,
        IEnumerable<ActorChoiceInput> inputs,
        long nextInputSequence,
        long censusRevision)
    {
        SchemaVersion = schemaVersion;
        List<ActorChoiceInput> copy = new List<ActorChoiceInput>();
        foreach (ActorChoiceInput input in inputs) copy.Add(input.Copy());
        this.inputs = new ReadOnlyCollection<ActorChoiceInput>(copy);
        NextInputSequence = nextInputSequence;
        CensusRevision = censusRevision;
    }

    internal int SchemaVersion { get; }
    internal IReadOnlyList<ActorChoiceInput> Inputs => inputs;
    internal long NextInputSequence { get; }
    internal long CensusRevision { get; }

    internal static bool TryCapture(
        ActorChoiceStore source,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactVector,
        out P12FActorChoiceSnapshot snapshot,
        out P12FActorChoiceSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12FActorChoiceSnapshotFailure.InvalidBoundary;
        if (source == null || token == null || exactVector == null
            || !ReferenceEquals(token.OwnerSections, exactVector)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L)
            return false;

        if (!TryGetSection(exactVector, out OwnerSectionCensusSnapshot section)
            || section.Role != OwnerSectionRole.Required)
        {
            failure = P12FActorChoiceSnapshotFailure.MissingOrMismatchedOwnerSection;
            return false;
        }

        ActorChoiceP11CensusProvider provider = new ActorChoiceP11CensusProvider(source);
        if (!Matches(provider.GetCurrentCensus(), section, source))
        {
            failure = P12FActorChoiceSnapshotFailure.MissingOrMismatchedOwnerSection;
            return false;
        }
        if (!source.ValidateInvariants().IsValid)
        {
            failure = P12FActorChoiceSnapshotFailure.UnsupportedHistory;
            return false;
        }

        IReadOnlyList<ActorChoiceInput> inputs = source.Inputs;
        if (source.TemporalInputCount != 0 || inputs.Count != source.P11InputCount)
        {
            failure = P12FActorChoiceSnapshotFailure.UnsupportedHistory;
            return false;
        }
        foreach (ActorChoiceInput input in inputs)
        {
            if (input == null || input.TemporalCapture != null || input.TemporalDispositions.Count != 0
                || input.Status == ActorChoiceInputStatus.Pending
                || input.Status == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)
            {
                failure = P12FActorChoiceSnapshotFailure.UnsupportedHistory;
                return false;
            }
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
            {
                if (disposition == null || disposition.Kind == ActorChoiceDispositionKind.Deferred)
                {
                    failure = P12FActorChoiceSnapshotFailure.UnsupportedHistory;
                    return false;
                }
            }
        }

        long nextSequence = source.NextInputSequence;
        long revision = source.CensusRevision;
        P12FActorChoiceSnapshot candidate = new P12FActorChoiceSnapshot(
            CurrentSchemaVersion,
            inputs,
            nextSequence,
            revision);
        if (!Matches(provider.GetCurrentCensus(), section, source)
            || section.Revision != revision)
        {
            failure = P12FActorChoiceSnapshotFailure.MissingOrMismatchedOwnerSection;
            return false;
        }
        snapshot = candidate;
        failure = P12FActorChoiceSnapshotFailure.None;
        return true;
    }

    internal static bool TryStage(
        P12FActorChoiceSnapshot snapshot,
        PersonStore stagedPersonStore,
        out ActorChoiceStore stagedStore,
        out P12FActorChoiceSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FActorChoiceSnapshotFailure.StagingFailed;
        if (snapshot == null || stagedPersonStore == null
            || snapshot.SchemaVersion != CurrentSchemaVersion
            || snapshot.inputs == null)
            return false;

        foreach (ActorChoiceInput input in snapshot.inputs)
        {
            if (input == null || input.PersonId == null
                || !stagedPersonStore.TryGet(input.PersonId, out _))
            {
                failure = P12FActorChoiceSnapshotFailure.PersonBindingMismatch;
                return false;
            }
        }
        if (!ActorChoiceStore.TryCreateP12FStaged(
                stagedPersonStore,
                snapshot.inputs,
                snapshot.NextInputSequence,
                snapshot.CensusRevision,
                out stagedStore,
                out _))
        {
            failure = P12FActorChoiceSnapshotFailure.StagingFailed;
            return false;
        }
        failure = P12FActorChoiceSnapshotFailure.None;
        return true;
    }

    private static bool TryGetSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        out OwnerSectionCensusSnapshot section)
    {
        section = null;
        foreach (OwnerSectionCensusSnapshot candidate in vector)
        {
            if (candidate == null
                || !string.Equals(candidate.SectionId, ActorChoiceP11CensusProvider.SectionId, StringComparison.Ordinal))
                continue;
            if (section != null) return false;
            section = candidate;
        }
        return section != null
            && section.SchemaVersion == ActorChoiceP11CensusProvider.SchemaVersion;
    }

    private static bool Matches(
        OwnerSectionCensusWitness current,
        OwnerSectionCensusSnapshot expected,
        ActorChoiceStore source) => current != null && expected != null
        && string.Equals(current.SectionId, expected.SectionId, StringComparison.Ordinal)
        && current.SchemaVersion == expected.SchemaVersion
        && ReferenceEquals(current.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
        && ReferenceEquals(current.OwnerInstanceIdentity, source.CensusOwnerIdentity)
        && current.Cardinality == expected.Cardinality
        && current.Cardinality == source.P11InputCount
        && current.Revision == expected.Revision
        && current.Revision == source.CensusRevision;
}
