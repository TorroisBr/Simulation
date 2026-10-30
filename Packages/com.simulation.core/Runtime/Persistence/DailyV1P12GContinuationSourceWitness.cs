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
    private DailyV1P12GCoreBoundCityOwnerSources coreCityOwnerSources;
    private DailyV1P12GCoreBoundDemographicOwnerSources coreDemographicOwnerSources;
    private DailyV1P12GCoreBoundSpatialOwnerSources coreSpatialOwnerSources;
    private DailyV1P12GCoreBoundNpcOwnerSources coreNpcOwnerSources;
    private DailyV1P12GCoreBoundCausalRootSources coreCausalRootSources;
    private DailyV1P12GCoreBoundOwnerSources coreBoundP12FOwnerSources;
    private DailyV1RuntimeIdentityIndex coreP12CRuntimeIdentityOwner;
    private P12CRuntimeIdentityOwnerSnapshot coreP12CRuntimeIdentitySnapshot;
    private DailyV1P12ECoreOwnerSourceSnapshots coreP12EOwnerSources;

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
    internal DailyV1P12GCoreBoundCityOwnerSources CoreCityOwnerSources =>
        Volatile.Read(ref coreCityOwnerSources);
    internal DailyV1P12GCoreBoundDemographicOwnerSources CoreDemographicOwnerSources =>
        Volatile.Read(ref coreDemographicOwnerSources);
    internal DailyV1P12GCoreBoundSpatialOwnerSources CoreSpatialOwnerSources =>
        Volatile.Read(ref coreSpatialOwnerSources);
    internal DailyV1P12GCoreBoundNpcOwnerSources CoreNpcOwnerSources =>
        Volatile.Read(ref coreNpcOwnerSources);
    internal DailyV1P12GCoreBoundCausalRootSources CoreCausalRootSources =>
        Volatile.Read(ref coreCausalRootSources);
    internal DailyV1P12ECoreOwnerSourceSnapshots CoreP12EOwnerSources =>
        Volatile.Read(ref coreP12EOwnerSources);
    internal bool IsActive => Volatile.Read(ref application) != null;

    internal bool TryBindCoreCityOwnerSources(
        DailyV1P12GCoreBoundCityOwnerSources sources)
    {
        if (!IsActive || sources == null || !sources.IsFor(this)) return false;
        return Interlocked.CompareExchange(ref coreCityOwnerSources, sources, null) == null;
    }

    internal bool TryBindCoreDemographicOwnerSources(
        DailyV1P12GCoreBoundDemographicOwnerSources sources)
    {
        if (!IsActive || sources == null || !sources.IsFor(this)) return false;
        return Interlocked.CompareExchange(ref coreDemographicOwnerSources, sources, null) == null;
    }

    internal bool TryBindCoreSpatialOwnerSources(
        DailyV1P12GCoreBoundSpatialOwnerSources sources)
    {
        if (!IsActive || sources == null || !sources.IsFor(this)) return false;
        return Interlocked.CompareExchange(ref coreSpatialOwnerSources, sources, null) == null;
    }

    internal bool TryBindCoreNpcOwnerSources(
        DailyV1P12GCoreBoundNpcOwnerSources sources)
    {
        if (!IsActive || sources == null || !sources.IsFor(this)) return false;
        return Interlocked.CompareExchange(ref coreNpcOwnerSources, sources, null) == null;
    }

    internal bool TryBindCoreCausalRootSources(
        DailyV1P12GCoreBoundCausalRootSources sources)
    {
        if (!IsActive || sources == null || !sources.IsFor(this)) return false;
        return Interlocked.CompareExchange(ref coreCausalRootSources, sources, null) == null;
    }

    internal bool TryCaptureCoreP12EOwnerSources(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        out DailyV1P12ECoreOwnerSourceSnapshots sources,
        out DailyV1P12ECoreOwnerSourceCaptureFailure failure,
        out string diagnostic)
    {
        sources = Volatile.Read(ref coreP12EOwnerSources);
        failure = DailyV1P12ECoreOwnerSourceCaptureFailure.InvalidCaptureContext;
        diagnostic = null;
        if (!IsActive || !ReferenceEquals(token, SourceWitness.Token)
            || !ReferenceEquals(ownerSectionVector, SourceWitness.TokenOwnerSections)
            || !TryRevalidate(out _))
        {
            sources = null;
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.StaleBoundary;
            diagnostic = "The exact active P12-E continuation-save boundary is required.";
            return false;
        }

        if (sources != null)
        {
            if (!sources.IsFor(this))
            {
                sources = null;
                diagnostic = "The cached P12-E owner snapshots belong to another save boundary.";
                return false;
            }

            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.None;
            return true;
        }

        if (!DailyV1P12ECoreOwnerSourceSnapshots.TryCapture(
                token, ownerSectionVector, this, null,
                out DailyV1P12ECoreOwnerSourceSnapshots candidate,
                out failure, out diagnostic)
            || candidate == null)
            return false;

        if (Interlocked.CompareExchange(ref coreP12EOwnerSources, candidate, null) == null)
        {
            sources = candidate;
        }
        else
        {
            sources = Volatile.Read(ref coreP12EOwnerSources);
            if (sources == null || !sources.IsFor(this))
            {
                sources = null;
                failure = DailyV1P12ECoreOwnerSourceCaptureFailure.InvalidCaptureContext;
                diagnostic = "The P12-E owner snapshots could not bind to this save boundary.";
                return false;
            }
        }

        if (!TryRevalidate(out _))
        {
            sources = null;
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.StaleBoundary;
            diagnostic = "The P12-E save boundary changed while owner snapshots were captured.";
            return false;
        }

        failure = DailyV1P12ECoreOwnerSourceCaptureFailure.None;
        diagnostic = null;
        return true;
    }

    internal bool TryEncodeCoreP12EOwnerSections(
        long capturedAbsoluteDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (!TryCaptureCoreP12EOwnerSources(
                SourceWitness.Token, SourceWitness.TokenOwnerSections,
                out DailyV1P12ECoreOwnerSourceSnapshots sources,
                out DailyV1P12ECoreOwnerSourceCaptureFailure captureFailure,
                out _)
            || sources == null || !sources.IsFor(this))
        {
            result = new P12AResult(captureFailure
                    == DailyV1P12ECoreOwnerSourceCaptureFailure.StaleBoundary
                ? P12AResultCode.CaptureRejected
                : P12AResultCode.OwnerGraphRejected,
                "p12e.core-owner-sources." + captureFailure);
            return false;
        }

        if (!DailyV1P12ECoreOwnerSectionEncoder.TryEncode(
                sources, capturedAbsoluteDay, out sections, out result))
            return false;

        if (!TryRevalidate(out _))
        {
            sections = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        return true;
    }

    internal bool TryEncodeCoreP12CRuntimeIdentitySections(
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (!IsActive || !TryRevalidate(out _)) return false;

        DailyV1SelectedRuntimeOwnerGraph graph = Session.OwnerGraph;
        DailyV1RuntimeIdentityIndex identityOwner = graph?.RuntimeIdentityIndex;
        if (identityOwner == null || !MatchesRuntimeIdentityCensus(
                identityOwner, SourceWitness.TokenOwnerSections))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12c.runtime-identity-owner-binding");
            return false;
        }

        DailyV1RuntimeIdentityIndex boundOwner = Volatile.Read(ref coreP12CRuntimeIdentityOwner);
        if (boundOwner == null)
        {
            DailyV1RuntimeIdentityIndex prior = Interlocked.CompareExchange(
                ref coreP12CRuntimeIdentityOwner, identityOwner, null);
            boundOwner = prior ?? identityOwner;
        }
        if (!ReferenceEquals(boundOwner, identityOwner))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12c.runtime-identity-owner-binding");
            return false;
        }

        P12CRuntimeIdentityOwnerSnapshot snapshot =
            Volatile.Read(ref coreP12CRuntimeIdentitySnapshot);
        if (snapshot == null)
        {
            P12CRuntimeIdentityOwnerSnapshot candidate = new P12CRuntimeIdentityOwnerSnapshot(
                P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion,
                identityOwner.CensusRevision,
                identityOwner.CaptureRegisteredValues());
            P12CRuntimeIdentityOwnerSnapshot prior = Interlocked.CompareExchange(
                ref coreP12CRuntimeIdentitySnapshot, candidate, null);
            snapshot = prior ?? candidate;
        }

        if (snapshot == null || snapshot.Revision != identityOwner.CensusRevision
            || !MatchesRuntimeIdentityCensus(identityOwner, SourceWitness.TokenOwnerSections)
            || !P12ARuntimeIdentityOwnerCodec.TryEncode(snapshot, out sections, out result))
        {
            sections = null;
            if (result == null)
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12c.runtime-identity-census");
            return false;
        }

        if (!TryRevalidate(out _)
            || !MatchesRuntimeIdentityCensus(identityOwner, SourceWitness.TokenOwnerSections))
        {
            sections = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        result = null;
        return true;
    }

    private static bool MatchesRuntimeIdentityCensus(
        DailyV1RuntimeIdentityIndex identityOwner,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
    {
        if (identityOwner == null || ownerSections == null) return false;
        return MatchesRuntimeIdentityFamily(identityOwner, ownerSections,
                DailyV1RuntimeIdentityKind.Npc)
            && MatchesRuntimeIdentityFamily(identityOwner, ownerSections,
                DailyV1RuntimeIdentityKind.City)
            && MatchesRuntimeIdentityFamily(identityOwner, ownerSections,
                DailyV1RuntimeIdentityKind.Location)
            && MatchesRuntimeIdentityFamily(identityOwner, ownerSections,
                DailyV1RuntimeIdentityKind.Route);
    }

    private static bool MatchesRuntimeIdentityFamily(
        DailyV1RuntimeIdentityIndex identityOwner,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        DailyV1RuntimeIdentityKind kind)
    {
        OwnerSectionCensusWitness expected = identityOwner.GetOwnerSectionCensusWitness(kind);
        OwnerSectionCensusSnapshot match = null;
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in ownerSections)
        {
            if (!string.Equals(section?.SectionId, expected.SectionId, StringComparison.Ordinal))
                continue;
            match = section;
            matches++;
        }

        return matches == 1
            && match.SchemaVersion == expected.SchemaVersion
            && match.Role == OwnerSectionRole.Required
            && ReferenceEquals(match.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
            && match.Cardinality == expected.Cardinality
            && match.Revision == expected.Revision
            && match.Revision == identityOwner.CensusRevision;
    }

    internal bool TryCaptureCoreBoundP12FOwnerSources(
        out DailyV1P12GCoreBoundOwnerSources sources,
        out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
        out string diagnostic)
    {
        sources = Volatile.Read(ref coreBoundP12FOwnerSources);
        failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        if (!IsActive || !TryRevalidate(out _))
        {
            sources = null;
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
            diagnostic = "A current active Core continuation-save witness is required.";
            return false;
        }

        if (sources != null)
        {
            if (!sources.IsFor(this))
            {
                sources = null;
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCensusMismatch;
                diagnostic = "The bound P12-F source snapshots belong to another save operation.";
                return false;
            }
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
            return true;
        }

        if (!DailyV1P12GCoreBoundOwnerSources.TryCapture(
                this, out DailyV1P12GCoreBoundOwnerSources candidate,
                out failure, out diagnostic)
            || candidate == null)
            return false;

        if (Interlocked.CompareExchange(ref coreBoundP12FOwnerSources,
                candidate, null) == null)
        {
            sources = candidate;
        }
        else
        {
            sources = Volatile.Read(ref coreBoundP12FOwnerSources);
            if (sources == null || !sources.IsFor(this))
            {
                sources = null;
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCensusMismatch;
                diagnostic = "The P12-F source snapshots could not bind to the active save operation.";
                return false;
            }
        }

        if (!TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
        {
            sources = null;
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
            diagnostic = "The Core save witness changed while P12-F sources were bound: "
                + staleFailure + ".";
            return false;
        }

        failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
        diagnostic = null;
        return true;
    }

    internal bool TryEncodeCoreP12FOwnerSections(
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (!TryCaptureCoreBoundP12FOwnerSources(
                out DailyV1P12GCoreBoundOwnerSources sources,
                out DailyV1P12GCoreBoundOwnerSourceCaptureFailure captureFailure,
                out _)
            || sources == null || !sources.IsFor(this))
        {
            result = new P12AResult(captureFailure
                == DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                ? P12AResultCode.CaptureRejected
                : P12AResultCode.OwnerGraphRejected,
                "p12f.core-owner-sources." + captureFailure);
            return false;
        }

        if (!DailyV1P12FCoreOwnerSectionEncoder.TryEncode(
                sources, out sections, out result))
            return false;

        if (!TryRevalidate(out _))
        {
            sections = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        return true;
    }

    internal bool TryEncodeCoreP12DOwnerSections(
        out IReadOnlyList<P12AOwnerSection> sections,
        out bool includesNpcOwnerSections,
        out P12AResult result)
    {
        sections = null;
        includesNpcOwnerSections = false;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (!IsActive || !TryRevalidate(out _)) return false;

        DailyV1P12GCoreBoundCityOwnerSources cities = CoreCityOwnerSources;
        DailyV1P12GCoreBoundDemographicOwnerSources demographics = CoreDemographicOwnerSources;
        DailyV1P12GCoreBoundSpatialOwnerSources spatial = CoreSpatialOwnerSources;
        DailyV1P12GCoreBoundNpcOwnerSources npcs = CoreNpcOwnerSources;
        if (cities == null || !cities.IsFor(this)
            || demographics == null || !demographics.IsFor(this)
            || spatial == null || !spatial.IsFor(this)
            || (npcs != null && !npcs.IsFor(this)))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12d.core-owner-sources");
            return false;
        }

        if (!DailyV1P12DCoreOwnerSectionEncoder.TryEncode(
                cities, demographics, spatial, npcs,
                SourceWitness.TokenOwnerSections, out sections, out result))
            return false;

        if (!TryRevalidate(out _))
        {
            sections = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        includesNpcOwnerSections = npcs != null;
        return true;
    }

    internal bool TryCaptureCoreCausalRootSources(
        out DailyV1P12GCoreBoundCausalRootSources sources,
        out DailyV1P12GCoreBoundCausalRootCaptureFailure failure,
        out string diagnostic)
    {
        sources = Volatile.Read(ref coreCausalRootSources);
        failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        if (!IsActive || !TryRevalidate(out _))
        {
            sources = null;
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.StaleSourceBoundary;
            diagnostic = "A current active Core continuation-save witness is required.";
            return false;
        }

        if (sources != null)
        {
            if (!sources.IsFor(this))
            {
                sources = null;
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootBindingMismatch;
                diagnostic = "The bound P12-C roots belong to another save operation.";
                return false;
            }
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.None;
            return true;
        }

        if (!DailyV1P12GCoreBoundCausalRootSources.TryCapture(
                this, out DailyV1P12GCoreBoundCausalRootSources candidate,
                out failure, out diagnostic)
            || candidate == null)
            return false;

        if (!TryBindCoreCausalRootSources(candidate))
        {
            sources = Volatile.Read(ref coreCausalRootSources);
            if (sources == null || !sources.IsFor(this))
            {
                sources = null;
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootBindingMismatch;
                diagnostic = "The P12-C roots could not bind to the active save operation.";
                return false;
            }
        }
        else
        {
            sources = candidate;
        }

        failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.None;
        diagnostic = null;
        return true;
    }

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

    internal bool TryComposeImage(
        P12ACompatibilityDescriptor unityCompatibility,
        P12ACompatibilityDescriptorV2 dotNetCompatibility,
        long politicalWorldRevision,
        IReadOnlyList<P12AOwnerSection> encodedOwnerSections,
        out P12AContinuationImage image,
        out P12ADailyV1SectionManifest manifest,
        out P12AResult result)
    {
        DailyV1Application owner = Volatile.Read(ref application);
        if (owner == null)
        {
            image = null;
            manifest = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        return owner.TryComposeContinuationImage(
            this, unityCompatibility, dotNetCompatibility, politicalWorldRevision,
            encodedOwnerSections,
            out image, out manifest, out result);
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
