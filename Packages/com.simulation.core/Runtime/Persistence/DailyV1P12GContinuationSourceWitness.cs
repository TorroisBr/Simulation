using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

/// <summary>
/// Exact source boundary proof retained while P12-G captures and privately
/// stages a continuation. The original token/list identities are retained,
/// while values used across staging are privately copied.
/// </summary>
internal sealed class DailyV1P12GSourceBoundaryWitness
{
    private DailyV1P12GSourceBoundaryWitness(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long mutationEpoch,
        P12ADailyV1SectionManifest manifest)
    {
        Token = token;
        TokenOwnerSections = token.OwnerSections;
        TokenOwnerSectionsSnapshot = CopyOwnerSections(token.OwnerSections);
        CurrentOwnerSections = CopyOwnerSections(currentOwnerSections);
        MutationEpoch = mutationEpoch;
        Manifest = manifest;
    }

    internal DailyCaptureEligibilityToken Token { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> TokenOwnerSections { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> TokenOwnerSectionsSnapshot { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> CurrentOwnerSections { get; }
    internal long MutationEpoch { get; }
    internal P12ADailyV1SectionManifest Manifest { get; }

    internal static bool TryCreate(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long mutationEpoch,
        out DailyV1P12GSourceBoundaryWitness witness)
    {
        witness = null;
        if (token == null || token.OwnerSections == null
            || currentOwnerSections == null || mutationEpoch < 0L
            || (token.BoundaryProvenance != DailyCaptureBoundaryProvenance.CompletedAdvance
                && token.BoundaryProvenance != DailyCaptureBoundaryProvenance.RestoredContinuation)
            || token.CompletedCoreSequence <= 0L
            || mutationEpoch != token.MutationEpoch
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(token.OwnerSections)
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(currentOwnerSections)
            || !DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
                token.OwnerSections, currentOwnerSections)
            || !P12ADailyV1Census.TryCreate(token.OwnerSections,
                out P12ADailyV1Census census, out _)
            || census?.Manifest == null)
            return false;

        witness = new DailyV1P12GSourceBoundaryWitness(
            token, currentOwnerSections, mutationEpoch, census.Manifest);
        return true;
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CopyOwnerSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections)
    {
        OwnerSectionCensusSnapshot[] copy = new OwnerSectionCensusSnapshot[sections.Count];
        for (int index = 0; index < sections.Count; index++)
        {
            OwnerSectionCensusSnapshot source = sections[index];
            copy[index] = new OwnerSectionCensusSnapshot(source.SectionId,
                source.SchemaVersion, source.Role, source.OwnerInstanceIdentity,
                source.Cardinality, source.Revision);
        }
        return new ReadOnlyCollection<OwnerSectionCensusSnapshot>(copy);
    }

    internal bool Matches(DailyV1P12GSourceBoundaryWitness candidate) =>
        candidate != null
        && ReferenceEquals(Token, candidate.Token)
        && ReferenceEquals(TokenOwnerSections, candidate.TokenOwnerSections)
        && DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
            TokenOwnerSectionsSnapshot, candidate.TokenOwnerSectionsSnapshot)
        && MutationEpoch == candidate.MutationEpoch
        && DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
            CurrentOwnerSections, candidate.CurrentOwnerSections);
}

/// <summary>
/// Keeps the one Core ContinuationSave operation registered across the full
/// P12-G detached capture/staging protocol. Callers must revalidate the
/// witness after each boundary where staged work could have yielded or
/// invoked host code, then dispose on the owner thread.
/// </summary>
internal sealed class DailyV1P12GContinuationSaveOperation : IDisposable
{
    private readonly object disposalSyncRoot = new object();
    private DailyV1Application application;

    internal DailyV1P12GContinuationSaveOperation(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session,
        DailyV1P12GSourceBoundaryWitness sourceWitness)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        Session = session ?? throw new ArgumentNullException(nameof(session));
        SourceWitness = sourceWitness ?? throw new ArgumentNullException(nameof(sourceWitness));
    }

    internal DailyV1ActiveRuntimeSession Session { get; }
    internal DailyV1P12GSourceBoundaryWitness SourceWitness { get; }
    internal bool IsActive => Volatile.Read(ref application) != null;

    internal bool TryRevalidate(out DailyCaptureEligibilityFailure failure)
    {
        DailyV1Application owner = Volatile.Read(ref application);
        if (owner == null)
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }
        return owner.TryRevalidateContinuationSaveOperation(
            Session, SourceWitness, out failure);
    }

    internal bool TryPersistImage(
        P12AContinuationImage image,
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out P12AResult result)
    {
        DailyV1Application owner = Volatile.Read(ref application);
        if (owner == null)
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }
        return owner.TryPersistContinuationImage(
            this, image, persistentDataRoot, fileOperations, out result);
    }

    internal bool IsOwnedBy(DailyV1Application candidate) =>
        ReferenceEquals(Volatile.Read(ref application), candidate);

    public void Dispose()
    {
        lock (disposalSyncRoot)
        {
            DailyV1Application owner = application;
            if (owner == null) return;
            if (!Session.AdmissionContext.IsOwnedByCurrentThread())
                throw new InvalidOperationException(
                    "A P12-G continuation-save operation must be closed on its owner thread.");

            // Keep the handle live if closing the registered scope fails, so
            // callers can diagnose/retry instead of silently stranding the
            // application operation count.
            owner.EndContinuationSaveOperation();
            Volatile.Write(ref application, null);
        }
    }
}
