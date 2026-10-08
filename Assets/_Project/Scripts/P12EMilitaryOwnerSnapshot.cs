using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EMilitaryOwnerSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidOwnerComposition,
    InvalidOwnerValues,
    InvalidOwnerSectionVector,
    UnsupportedSourceBinding,
    LocalTopologyComposed,
    P16ExtensionState,
    P17ProvenanceState,
    InvalidSnapshot,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    InvalidRelation,
    InvalidSpatialReference,
    StageFailed
}

internal sealed class P12EMilitaryOwnerSnapshotFailure
{
    internal static readonly P12EMilitaryOwnerSnapshotFailure None =
        new P12EMilitaryOwnerSnapshotFailure(P12EMilitaryOwnerSnapshotFailureCode.None, string.Empty);

    internal P12EMilitaryOwnerSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EMilitaryOwnerSnapshotFailure(
        P12EMilitaryOwnerSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EMilitaryOwnerSnapshotFailure Create(
        P12EMilitaryOwnerSnapshotFailureCode code,
        string message) => code == P12EMilitaryOwnerSnapshotFailureCode.None
            ? None
            : new P12EMilitaryOwnerSnapshotFailure(code, message);
}

/// <summary>
/// Detached schema-v1 value export for the selected Daily-v1 ArmedForce,
/// contingent-manpower, and baseline-position owners. The ephemeral P12-B
/// token and owner identities are checked on capture but never retained.
/// </summary>
internal sealed class P12EMilitaryOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long ArmedForceRevision { get; }
    internal long ManpowerRevision { get; }
    internal long PositionRevision { get; }
    internal int ForceCount { get; }
    internal int ContingentCount { get; }
    internal int RelevantPersonCount { get; }
    internal int ManpowerStateCount { get; }
    internal int PositionCount { get; }
    internal IReadOnlyList<P12EMilitaryForceSnapshotRecord> Forces { get; }
    internal IReadOnlyList<P12EMilitaryContingentSnapshotRecord> Contingents { get; }
    internal IReadOnlyList<P12EMilitaryRelevantPersonSnapshotRecord> RelevantPersons { get; }
    internal IReadOnlyList<P12EManpowerStateSnapshotRecord> ManpowerStates { get; }
    internal IReadOnlyList<P12EForcePositionSnapshotRecord> Positions { get; }

    internal P12EMilitaryOwnerSnapshot(
        int schemaVersion,
        long armedForceRevision,
        long manpowerRevision,
        long positionRevision,
        int forceCount,
        int contingentCount,
        int relevantPersonCount,
        int manpowerStateCount,
        int positionCount,
        IEnumerable<P12EMilitaryForceSnapshotRecord> forces,
        IEnumerable<P12EMilitaryContingentSnapshotRecord> contingents,
        IEnumerable<P12EMilitaryRelevantPersonSnapshotRecord> relevantPersons,
        IEnumerable<P12EManpowerStateSnapshotRecord> manpowerStates,
        IEnumerable<P12EForcePositionSnapshotRecord> positions)
    {
        SchemaVersion = schemaVersion;
        ArmedForceRevision = armedForceRevision;
        ManpowerRevision = manpowerRevision;
        PositionRevision = positionRevision;
        ForceCount = forceCount;
        ContingentCount = contingentCount;
        RelevantPersonCount = relevantPersonCount;
        ManpowerStateCount = manpowerStateCount;
        PositionCount = positionCount;
        Forces = CopyRows(forces, row => row?.Copy());
        Contingents = CopyRows(contingents, row => row?.Copy());
        RelevantPersons = CopyRows(relevantPersons, row => row?.Copy());
        ManpowerStates = CopyRows(manpowerStates, row => row?.Copy());
        Positions = CopyRows(positions, row => row?.Copy());
    }

    internal static bool TryCapture(
        ArmedForceStore armedForceOwner,
        ContingentManpowerStateStore manpowerOwner,
        ArmedForceSpatialStateStore positionOwner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EMilitaryOwnerSnapshot snapshot,
        out P12EMilitaryOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EMilitaryOwnerSnapshotFailure.Create(
            P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerComposition,
            "The selected Daily-v1 military owners are invalid.");
        if (armedForceOwner == null || manpowerOwner == null || positionOwner == null
            || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidCaptureContext,
                "Capture requires the exact owner-section vector carried by the P12-B token.", out failure);

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedProfile,
                "Military owner export requires a successful Daily-v1 completed-boundary token.", out failure);

        if (!ReferenceEquals(manpowerOwner.ArmedForceStore, armedForceOwner)
            || !ReferenceEquals(positionOwner.ArmedForceStore, armedForceOwner))
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerComposition,
                "The three owners must be installed against the exact same ArmedForceStore.", out failure);
        if (positionOwner.LocalTopologyStore != null)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.LocalTopologyComposed,
                "Daily-v1 LocalTopology is NOT_COMPOSED; an injected topology owner rejects capture.", out failure);
        if (positionOwner.HasP17AProvenanceState)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.P17ProvenanceState,
                "Daily-v1 does not capture P17-A provenance state.", out failure);
        if (positionOwner.HasP16ExtensionState)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.P16ExtensionState,
                "Daily-v1 does not capture P16 profile, carried supply, or crossing receipt state.", out failure);
        if (manpowerOwner.SourceProvider != null)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding,
                "The exact census-bound Daily-v1 manpower owner must have a null SourceProvider.", out failure);

        ArmedForceInvariantReport forceReport = armedForceOwner.ValidateInvariants();
        ContingentManpowerInvariantReport manpowerReport = manpowerOwner.ValidateInvariants();
        ArmedForceSpatialInvariantReport positionReport = positionOwner.ValidateInvariants();
        if (!forceReport.IsValid || !manpowerReport.IsValid || !positionReport.IsValid)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                "A military owner failed its existing invariant report.", out failure);

        IReadOnlyList<ArmedForceRecord> sourceForces = armedForceOwner.Forces;
        IReadOnlyList<ContingentRecord> sourceContingents = armedForceOwner.Contingents;
        IReadOnlyList<ArmedForcePersonReference> sourceRelevantPersons = armedForceOwner.RelevantPersons;
        IReadOnlyList<ContingentManpowerState> sourceManpowerStates = manpowerOwner.States;
        IReadOnlyList<ArmedForceSpatialPosition> sourcePositions = positionOwner.Positions;
        int forceCount = armedForceOwner.Count;
        int contingentCount = armedForceOwner.ContingentCount;
        int relevantPersonCount = armedForceOwner.RelevantPersonCount;
        int manpowerStateCount = sourceManpowerStates?.Count ?? -1;
        int positionCount = positionOwner.Count;
        long armedForceRevision = armedForceOwner.Revision;
        long manpowerRevision = manpowerOwner.Revision;
        long positionRevision = positionOwner.Revision;

        if (sourceForces == null || sourceContingents == null || sourceRelevantPersons == null
            || sourceManpowerStates == null || sourcePositions == null
            || forceCount < 0 || contingentCount < 0 || relevantPersonCount < 0
            || manpowerStateCount < 0 || positionCount < 0
            || armedForceRevision < 0L || manpowerRevision < 0L || positionRevision < 0L
            || sourceForces.Count != forceCount || sourceContingents.Count != contingentCount
            || sourceRelevantPersons.Count != relevantPersonCount
            || sourcePositions.Count != positionCount)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                "Military owner counts, revisions, and defensive views are inconsistent.", out failure);

        if (!MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.ForcesSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, forceCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.ContingentsSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, contingentCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, relevantPersonCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ContingentManpowerCensusProvider.SectionId,
                ContingentManpowerCensusProvider.SchemaVersion, manpowerOwner, manpowerStateCount, manpowerRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceSpatialCensusProvider.SectionId,
                ArmedForceSpatialCensusProvider.SchemaVersion, positionOwner, positionCount, positionRevision))
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B token vector must contain exactly one matching required witness for each of the five existing sections.", out failure);

        List<P12EMilitaryForceSnapshotRecord> forces = new List<P12EMilitaryForceSnapshotRecord>(forceCount);
        foreach (ArmedForceRecord source in sourceForces)
        {
            if (source == null || source.Id == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                    "A force row cannot be copied as detached values.", out failure);
            forces.Add(new P12EMilitaryForceSnapshotRecord(
                source.Id.Value,
                source.DisplayName,
                source.CreatedAbsoluteDay,
                source.ParentForceId?.Value,
                source.OperationalLocationReference,
                source.CommanderPersonId?.Value,
                source.LifecycleState,
                source.TerminatedAbsoluteDay,
                source.IsDetached));
        }

        List<P12EMilitaryContingentSnapshotRecord> contingents =
            new List<P12EMilitaryContingentSnapshotRecord>(contingentCount);
        foreach (ContingentRecord source in sourceContingents)
        {
            if (source == null || source.Id == null || source.ForceId == null || source.Origin == null
                || source.Characteristics == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                    "A contingent row cannot be copied as detached values.", out failure);
            List<P12EMilitaryCharacteristicSnapshot> characteristics =
                new List<P12EMilitaryCharacteristicSnapshot>(source.Characteristics.Count);
            foreach (ArmedForceCharacteristic characteristic in source.Characteristics)
            {
                if (characteristic == null)
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                        "A contingent characteristic cannot be null.", out failure);
                characteristics.Add(new P12EMilitaryCharacteristicSnapshot(characteristic.Key, characteristic.Value));
            }
            contingents.Add(new P12EMilitaryContingentSnapshotRecord(
                source.Id.Value,
                source.ForceId.Value,
                source.Amount,
                source.Origin.Domain,
                source.Origin.Value,
                source.ServiceType,
                characteristics));
        }

        List<P12EMilitaryRelevantPersonSnapshotRecord> relevantPersons =
            new List<P12EMilitaryRelevantPersonSnapshotRecord>(relevantPersonCount);
        foreach (ArmedForcePersonReference source in sourceRelevantPersons)
        {
            if (source == null || source.Id == null || source.ForceId == null || source.PersonId == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                    "A relevant Person row cannot be copied as detached values.", out failure);
            relevantPersons.Add(new P12EMilitaryRelevantPersonSnapshotRecord(
                source.Id.Value, source.ForceId.Value, source.PersonId.Value, source.RoleKey));
        }

        List<P12EManpowerStateSnapshotRecord> manpowerStates =
            new List<P12EManpowerStateSnapshotRecord>(manpowerStateCount);
        foreach (ContingentManpowerState source in sourceManpowerStates)
        {
            if (source == null || source.ContingentId == null || source.Cohorts == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                    "A manpower row cannot be copied as detached values.", out failure);
            if (source.SourceId != null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding,
                    "A source-bound manpower row cannot be captured by the selected provider-free Daily-v1 profile.", out failure);
            List<P12EManpowerCohortSnapshotRecord> cohorts =
                new List<P12EManpowerCohortSnapshotRecord>(source.Cohorts.Count);
            foreach (ContingentManpowerCohort cohort in source.Cohorts)
            {
                if (cohort == null)
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerValues,
                        "A manpower cohort cannot be null.", out failure);
                cohorts.Add(new P12EManpowerCohortSnapshotRecord(
                    cohort.InjuryState,
                    cohort.CustodyState,
                    cohort.CustodianForceId?.Value,
                    cohort.AvailabilityState,
                    cohort.Amount));
            }
            manpowerStates.Add(new P12EManpowerStateSnapshotRecord(
                source.ContingentId.Value, null, source.Revision, cohorts));
        }

        List<P12EForcePositionSnapshotRecord> positions =
            new List<P12EForcePositionSnapshotRecord>(positionCount);
        foreach (ArmedForceSpatialPosition source in sourcePositions)
        {
            if (source?.ForceId == null || source.Position == null
                || !TryCopySpatialReference(source.Position, out P12ESpatialReferenceSnapshot copiedReference))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference,
                    "A position must be a supported baseline typed reference with no topology fields.", out failure);
            positions.Add(new P12EForcePositionSnapshotRecord(source.ForceId.Value, copiedReference));
        }

        if (armedForceOwner.Revision != armedForceRevision
            || armedForceOwner.Count != forceCount
            || armedForceOwner.ContingentCount != contingentCount
            || armedForceOwner.RelevantPersonCount != relevantPersonCount
            || manpowerOwner.Revision != manpowerRevision
            || manpowerOwner.States.Count != manpowerStateCount
            || manpowerOwner.SourceProvider != null
            || positionOwner.Revision != positionRevision
            || positionOwner.Count != positionCount
            || positionOwner.LocalTopologyStore != null
            || positionOwner.HasP16ExtensionState
            || positionOwner.HasP17AProvenanceState
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.ForcesSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, forceCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.ContingentsSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, contingentCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, armedForceOwner, relevantPersonCount, armedForceRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ContingentManpowerCensusProvider.SectionId,
                ContingentManpowerCensusProvider.SchemaVersion, manpowerOwner, manpowerStateCount, manpowerRevision)
            || !MatchesOwnerSection(sharedOwnerSectionVector, ArmedForceSpatialCensusProvider.SectionId,
                ArmedForceSpatialCensusProvider.SchemaVersion, positionOwner, positionCount, positionRevision))
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "A military owner changed while its detached values were copied.", out failure);

        snapshot = new P12EMilitaryOwnerSnapshot(
            CurrentSchemaVersion,
            armedForceRevision,
            manpowerRevision,
            positionRevision,
            forceCount,
            contingentCount,
            relevantPersonCount,
            manpowerStateCount,
            positionCount,
            forces,
            contingents,
            relevantPersons,
            manpowerStates,
            positions);
        failure = P12EMilitaryOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        PersonStore stagedPersons,
        SpatialAuthorityStore stagedSpatialAuthority,
        LocalTopologyStore stagedLocalTopology,
        out ArmedForceStore stagedArmedForce,
        out ContingentManpowerStateStore stagedManpower,
        out ArmedForceSpatialStateStore stagedPositions,
        out P12EMilitaryOwnerSnapshotFailure failure)
    {
        stagedArmedForce = null;
        stagedManpower = null;
        stagedPositions = null;
        failure = P12EMilitaryOwnerSnapshotFailure.Create(
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot,
            "The detached military snapshot is invalid.");
        if (stagedPersons == null || stagedSpatialAuthority == null)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerComposition,
                "Staging requires the exact staged PersonStore and P8 spatial authority.", out failure);
        if (stagedLocalTopology != null)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.LocalTopologyComposed,
                "Daily-v1 LocalTopology remains NOT_COMPOSED.", out failure);
        if (!TryValidateDocument(out failure)) return false;
        if (!TryValidateStagingDependencies(stagedPersons, stagedSpatialAuthority, out failure)) return false;

        try
        {
            List<ArmedForceRecord> forceRecords = new List<ArmedForceRecord>(Forces.Count);
            foreach (P12EMilitaryForceSnapshotRecord row in Forces)
                forceRecords.Add(new ArmedForceRecord(
                    new ArmedForceId(row.ForceIdValue),
                    row.DisplayName,
                    row.CreatedAbsoluteDay,
                    row.ParentForceIdValue == null ? null : new ArmedForceId(row.ParentForceIdValue),
                    row.OperationalLocationReference,
                    row.CommanderPersonIdValue == null ? null : new PersonId(row.CommanderPersonIdValue),
                    row.LifecycleState,
                    row.TerminatedAbsoluteDay,
                    row.IsDetached));

            List<ContingentRecord> contingentRecords = new List<ContingentRecord>(Contingents.Count);
            foreach (P12EMilitaryContingentSnapshotRecord row in Contingents)
            {
                List<ArmedForceCharacteristic> characteristics =
                    new List<ArmedForceCharacteristic>(row.Characteristics.Count);
                foreach (P12EMilitaryCharacteristicSnapshot characteristic in row.Characteristics)
                    characteristics.Add(new ArmedForceCharacteristic(characteristic.Key, characteristic.Value));
                contingentRecords.Add(new ContingentRecord(
                    new ContingentId(row.ContingentIdValue),
                    new ArmedForceId(row.ForceIdValue),
                    row.Amount,
                    new ContingentOriginReference(row.OriginDomain, row.OriginValue),
                    row.ServiceType,
                    characteristics));
            }

            List<ArmedForcePersonReference> relevantPersonRecords =
                new List<ArmedForcePersonReference>(RelevantPersons.Count);
            foreach (P12EMilitaryRelevantPersonSnapshotRecord row in RelevantPersons)
                relevantPersonRecords.Add(new ArmedForcePersonReference(
                    new ArmedForceId(row.ForceIdValue),
                    new PersonId(row.PersonIdValue),
                    row.RoleKey,
                    new ArmedForcePersonReferenceId(row.ReferenceIdValue)));

            if (!ArmedForceStore.TryCreateFromP12EOwnerSnapshot(
                    stagedPersons,
                    forceRecords,
                    contingentRecords,
                    relevantPersonRecords,
                    ArmedForceRevision,
                    out ArmedForceStore forceCandidate,
                    out string forceDiagnostic))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.StageFailed,
                    forceDiagnostic, out failure);

            List<ContingentManpowerState> manpowerStates =
                new List<ContingentManpowerState>(ManpowerStates.Count);
            foreach (P12EManpowerStateSnapshotRecord row in ManpowerStates)
            {
                List<ContingentManpowerCohort> cohorts =
                    new List<ContingentManpowerCohort>(row.Cohorts.Count);
                foreach (P12EManpowerCohortSnapshotRecord cohort in row.Cohorts)
                    cohorts.Add(new ContingentManpowerCohort(
                        cohort.InjuryState,
                        cohort.CustodyState,
                        cohort.CustodianForceIdValue == null
                            ? null
                            : new ArmedForceId(cohort.CustodianForceIdValue),
                        cohort.AvailabilityState,
                        cohort.Amount));
                manpowerStates.Add(new ContingentManpowerState(
                    new ContingentId(row.ContingentIdValue),
                    null,
                    cohorts,
                    row.Revision));
            }

            if (!ContingentManpowerStateStore.TryCreateFromP12EOwnerSnapshot(
                    forceCandidate,
                    manpowerStates,
                    ManpowerRevision,
                    out ContingentManpowerStateStore manpowerCandidate,
                    out string manpowerDiagnostic))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.StageFailed,
                    manpowerDiagnostic, out failure);
            if (manpowerCandidate.SourceProvider != null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding,
                    "The private Daily-v1 manpower owner must retain an exact null SourceProvider.", out failure);

            List<ArmedForceSpatialPosition> positions =
                new List<ArmedForceSpatialPosition>(Positions.Count);
            foreach (P12EForcePositionSnapshotRecord row in Positions)
            {
                if (!TryBuildSpatialReference(row.Reference, out SpatialReference reference))
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference,
                        "A staged position contains a topology, unsupported, or malformed typed reference.", out failure);
                positions.Add(new ArmedForceSpatialPosition(new ArmedForceId(row.ForceIdValue), reference));
            }

            if (!ArmedForceSpatialStateStore.TryCreateFromP12EOwnerSnapshot(
                    forceCandidate,
                    stagedSpatialAuthority,
                    null,
                    positions,
                    PositionRevision,
                    out ArmedForceSpatialStateStore positionCandidate,
                    out string positionDiagnostic))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.StageFailed,
                    positionDiagnostic, out failure);

            stagedArmedForce = forceCandidate;
            stagedManpower = manpowerCandidate;
            stagedPositions = positionCandidate;
            failure = P12EMilitaryOwnerSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.StageFailed,
                "Private military owner staging rejected malformed values: " + exception.Message, out failure);
        }
    }

    private bool TryValidateDocument(out P12EMilitaryOwnerSnapshotFailure failure)
    {
        failure = P12EMilitaryOwnerSnapshotFailure.Create(
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot,
            "The military owner snapshot is malformed.");
        if (SchemaVersion != CurrentSchemaVersion)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSchema,
                "The military owner snapshot schema is unsupported.", out failure);
        if (ArmedForceRevision < 0L || ManpowerRevision < 0L || PositionRevision < 0L)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRevision,
                "Military owner revisions cannot be negative.", out failure);
        if (ForceCount < 0 || ContingentCount < 0 || RelevantPersonCount < 0
            || ManpowerStateCount < 0 || PositionCount < 0
            || Forces == null || Contingents == null || RelevantPersons == null
            || ManpowerStates == null || Positions == null
            || ForceCount != Forces.Count || ContingentCount != Contingents.Count
            || RelevantPersonCount != RelevantPersons.Count
            || ManpowerStateCount != ManpowerStates.Count || PositionCount != Positions.Count)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidCardinality,
                "Every military owner row count must match its complete detached row set.", out failure);

        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, P12EMilitaryForceSnapshotRecord> forceRowsById =
            new Dictionary<string, P12EMilitaryForceSnapshotRecord>(StringComparer.Ordinal);
        string previous = null;
        foreach (P12EMilitaryForceSnapshotRecord row in Forces)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ForceIdValue)
                || !forceIds.Add(row.ForceIdValue)
                || (previous != null && StringComparer.Ordinal.Compare(previous, row.ForceIdValue) >= 0)
                || row.DisplayName == null
                || row.CreatedAbsoluteDay < 0L
                || !Enum.IsDefined(typeof(ArmedForceLifecycleState), row.LifecycleState)
                || (row.TerminatedAbsoluteDay.HasValue
                    && (row.TerminatedAbsoluteDay.Value < 0L
                        || row.TerminatedAbsoluteDay.Value < row.CreatedAbsoluteDay))
                || (row.LifecycleState == ArmedForceLifecycleState.Active && row.TerminatedAbsoluteDay.HasValue)
                || (row.LifecycleState == ArmedForceLifecycleState.Terminated && !row.TerminatedAbsoluteDay.HasValue)
                || (row.IsDetached && string.IsNullOrWhiteSpace(row.ParentForceIdValue)))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity,
                    "Force rows must be unique, ordinally ordered, and locally valid.", out failure);
            previous = row.ForceIdValue;
            forceRowsById.Add(row.ForceIdValue, row);
        }
        foreach (P12EMilitaryForceSnapshotRecord row in Forces)
        {
            if (row.ParentForceIdValue != null
                && (string.IsNullOrWhiteSpace(row.ParentForceIdValue)
                    || string.Equals(row.ParentForceIdValue, row.ForceIdValue, StringComparison.Ordinal)
                    || !forceRowsById.TryGetValue(row.ParentForceIdValue, out P12EMilitaryForceSnapshotRecord parent)
                    || (row.LifecycleState == ArmedForceLifecycleState.Active
                        && parent.LifecycleState != ArmedForceLifecycleState.Active)))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "Force parent links must resolve and preserve active-parent and no-self-parent rules.", out failure);

            if (row.IsDetached && row.ParentForceIdValue == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A detached force must retain its structural parent.", out failure);

            HashSet<string> ancestors = new HashSet<string>(StringComparer.Ordinal);
            string ancestorId = row.ForceIdValue;
            while (ancestorId != null)
            {
                if (!ancestors.Add(ancestorId))
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                        "The force hierarchy contains a parent cycle.", out failure);
                ancestorId = forceRowsById.TryGetValue(ancestorId, out P12EMilitaryForceSnapshotRecord current)
                    ? current.ParentForceIdValue
                    : null;
            }
        }

        HashSet<string> contingentIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, P12EMilitaryContingentSnapshotRecord> contingentRowsById =
            new Dictionary<string, P12EMilitaryContingentSnapshotRecord>(StringComparer.Ordinal);
        previous = null;
        foreach (P12EMilitaryContingentSnapshotRecord row in Contingents)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ContingentIdValue)
                || string.IsNullOrWhiteSpace(row.ForceIdValue)
                || !contingentIds.Add(row.ContingentIdValue)
                || (previous != null && StringComparer.Ordinal.Compare(previous, row.ContingentIdValue) >= 0)
                || row.Amount < 0L || string.IsNullOrWhiteSpace(row.OriginDomain)
                || string.IsNullOrWhiteSpace(row.OriginValue) || string.IsNullOrWhiteSpace(row.ServiceType)
                || row.Characteristics == null)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity,
                    "Contingent rows require unique ordered identity and complete origin/service values.", out failure);
            previous = row.ContingentIdValue;
            if (!forceRowsById.ContainsKey(row.ForceIdValue))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A contingent must reference a registered force.", out failure);
            contingentRowsById.Add(row.ContingentIdValue, row);
            string priorCharacteristicKey = null;
            foreach (P12EMilitaryCharacteristicSnapshot characteristic in row.Characteristics)
            {
                if (characteristic == null || string.IsNullOrWhiteSpace(characteristic.Key)
                    || (priorCharacteristicKey != null
                        && StringComparer.Ordinal.Compare(priorCharacteristicKey, characteristic.Key) >= 0))
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot,
                        "Contingent characteristics must be unique and in ordinal key order.", out failure);
                priorCharacteristicKey = characteristic.Key;
            }
        }

        HashSet<string> referenceIds = new HashSet<string>(StringComparer.Ordinal);
        P12EMilitaryRelevantPersonSnapshotRecord previousRelevantPerson = null;
        foreach (P12EMilitaryRelevantPersonSnapshotRecord row in RelevantPersons)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ReferenceIdValue)
                || string.IsNullOrWhiteSpace(row.ForceIdValue)
                || string.IsNullOrWhiteSpace(row.PersonIdValue)
                || string.IsNullOrWhiteSpace(row.RoleKey)
                || !referenceIds.Add(row.ReferenceIdValue)
                || (previousRelevantPerson != null && CompareRelevantPersonRows(previousRelevantPerson, row) >= 0)
                || !forceRowsById.ContainsKey(row.ForceIdValue))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity,
                    "Relevant Person rows require unique stable IDs, canonical order, and registered force links.", out failure);
            previousRelevantPerson = row;
        }

        HashSet<string> stateIds = new HashSet<string>(StringComparer.Ordinal);
        previous = null;
        foreach (P12EManpowerStateSnapshotRecord row in ManpowerStates)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ContingentIdValue)
                || !stateIds.Add(row.ContingentIdValue)
                || (previous != null && StringComparer.Ordinal.Compare(previous, row.ContingentIdValue) >= 0)
                || row.Revision < 0L || row.SourceIdValue != null || row.Cohorts == null)
                return Fail(row != null && row.SourceIdValue != null
                        ? P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding
                        : P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity,
                    "Daily-v1 manpower rows must be ordered, source-null, and have nonnegative revisions.", out failure);
            previous = row.ContingentIdValue;

            P12EManpowerCohortSnapshotRecord priorCohort = null;
            long living = 0L;
            foreach (P12EManpowerCohortSnapshotRecord cohort in row.Cohorts)
            {
                if (cohort == null || !Enum.IsDefined(typeof(ManpowerInjuryState), cohort.InjuryState)
                    || !Enum.IsDefined(typeof(ManpowerCustodyState), cohort.CustodyState)
                    || !Enum.IsDefined(typeof(ManpowerAvailabilityState), cohort.AvailabilityState)
                    || cohort.Amount <= 0L
                    || (cohort.CustodyState == ManpowerCustodyState.Captured
                        ? string.IsNullOrWhiteSpace(cohort.CustodianForceIdValue)
                            || cohort.AvailabilityState != ManpowerAvailabilityState.Unavailable
                        : cohort.CustodianForceIdValue != null))
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                        "Manpower cohorts must preserve positive valid amounts and current custody rules.", out failure);
                if (priorCohort != null && CompareCohortRows(priorCohort, cohort) >= 0)
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot,
                        "Manpower cohorts must be unique and in the current canonical order.", out failure);
                priorCohort = cohort;
                try { living = checked(living + cohort.Amount); }
                catch (OverflowException)
                {
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                        "A manpower living roster total exceeds Int64.", out failure);
                }
            }
            if (!contingentIds.Contains(row.ContingentIdValue))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A manpower state has no corresponding managed contingent.", out failure);
            if (!contingentRowsById.TryGetValue(row.ContingentIdValue, out P12EMilitaryContingentSnapshotRecord contingent)
                || contingent.Amount != living)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "Every manpower living roster must equal its exact contingent Amount mirror.", out failure);
            if (forceRowsById.TryGetValue(contingent.ForceIdValue, out P12EMilitaryForceSnapshotRecord owningForce)
                && owningForce.LifecycleState == ArmedForceLifecycleState.Terminated
                && living > 0L)
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A terminated force cannot retain direct living manpower.", out failure);
            foreach (P12EManpowerCohortSnapshotRecord cohort in row.Cohorts)
            {
                if (cohort.CustodianForceIdValue != null
                    && (!forceRowsById.TryGetValue(cohort.CustodianForceIdValue, out P12EMilitaryForceSnapshotRecord custodian)
                        || custodian.LifecycleState != ArmedForceLifecycleState.Active))
                    return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                        "Captured manpower requires a registered active custodian force.", out failure);
            }
        }
        if (stateIds.Count != contingentIds.Count)
            return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                "Every managed contingent requires exactly one manpower state.", out failure);

        HashSet<string> positionedForceIds = new HashSet<string>(StringComparer.Ordinal);
        previous = null;
        foreach (P12EForcePositionSnapshotRecord row in Positions)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ForceIdValue)
                || row.Reference == null || !positionedForceIds.Add(row.ForceIdValue)
                || (previous != null && StringComparer.Ordinal.Compare(previous, row.ForceIdValue) >= 0)
                || !forceIds.Contains(row.ForceIdValue)
                || !forceRowsById.TryGetValue(row.ForceIdValue, out P12EMilitaryForceSnapshotRecord positionedForce)
                || !IsBaselineSpatialReference(row.Reference))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference,
                    "Positions must be unique, ordered, supported typed references for registered forces.", out failure);
            previous = row.ForceIdValue;
        }

        failure = P12EMilitaryOwnerSnapshotFailure.None;
        return true;
    }

    private bool TryValidateStagingDependencies(
        PersonStore stagedPersons,
        SpatialAuthorityStore stagedSpatialAuthority,
        out P12EMilitaryOwnerSnapshotFailure failure)
    {
        foreach (P12EMilitaryForceSnapshotRecord force in Forces)
        {
            if (force.CommanderPersonIdValue == null) continue;
            PersonId commander;
            try { commander = new PersonId(force.CommanderPersonIdValue); }
            catch (ArgumentException)
            {
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A commander PersonId is malformed.", out failure);
            }
            if (!stagedPersons.TryGet(commander, out _))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A commander PersonId is absent from the staged PersonStore.", out failure);
        }

        foreach (P12EMilitaryRelevantPersonSnapshotRecord reference in RelevantPersons)
        {
            PersonId person;
            try { person = new PersonId(reference.PersonIdValue); }
            catch (ArgumentException)
            {
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A relevant PersonId is malformed.", out failure);
            }
            if (!stagedPersons.TryGet(person, out _))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation,
                    "A relevant PersonId is absent from the staged PersonStore.", out failure);
        }

        foreach (P12EForcePositionSnapshotRecord row in Positions)
        {
            if (!TryBuildSpatialReference(row.Reference, out SpatialReference position)
                || !stagedSpatialAuthority.TryResolve(
                    position,
                    null,
                    out _,
                    out _))
                return Fail(P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference,
                    "A typed force position does not resolve against the staged P8 authority with topology absent.", out failure);
        }

        failure = P12EMilitaryOwnerSnapshotFailure.None;
        return true;
    }

    private static int CompareRelevantPersonRows(
        P12EMilitaryRelevantPersonSnapshotRecord left,
        P12EMilitaryRelevantPersonSnapshotRecord right)
    {
        if (left == null || right == null) return -1;
        int comparison = StringComparer.Ordinal.Compare(left.ForceIdValue, right.ForceIdValue);
        if (comparison != 0) return comparison;
        comparison = StringComparer.Ordinal.Compare(left.RoleKey, right.RoleKey);
        if (comparison != 0) return comparison;
        comparison = StringComparer.Ordinal.Compare(left.PersonIdValue, right.PersonIdValue);
        if (comparison != 0) return comparison;
        return StringComparer.Ordinal.Compare(left.ReferenceIdValue, right.ReferenceIdValue);
    }

    private static int CompareCohortRows(
        P12EManpowerCohortSnapshotRecord left,
        P12EManpowerCohortSnapshotRecord right)
    {
        int comparison = left.InjuryState.CompareTo(right.InjuryState);
        if (comparison != 0) return comparison;
        comparison = left.CustodyState.CompareTo(right.CustodyState);
        if (comparison != 0) return comparison;
        comparison = StringComparer.Ordinal.Compare(left.CustodianForceIdValue, right.CustodianForceIdValue);
        return comparison != 0 ? comparison : left.AvailabilityState.CompareTo(right.AvailabilityState);
    }

    private static bool TryCopySpatialReference(
        SpatialReference source,
        out P12ESpatialReferenceSnapshot copied)
    {
        copied = null;
        if (source == null || !IsBaselineSpatialReference(source)) return false;
        copied = new P12ESpatialReferenceSnapshot(
            source.Kind,
            source.HexId?.Value,
            source.LocationId?.Value,
            source.CrossingId?.Value,
            source.TopologyOwnerKind,
            source.TopologyOwnerRuntimeId,
            source.SubLocationRuntimeId);
        return true;
    }

    private static bool TryBuildSpatialReference(
        P12ESpatialReferenceSnapshot source,
        out SpatialReference position)
    {
        position = null;
        if (!IsBaselineSpatialReference(source)) return false;
        switch (source.Kind)
        {
            case SpatialReferenceKind.Hex:
                position = SpatialReference.ForHex(new HexId(source.HexIdValue));
                return true;
            case SpatialReferenceKind.Location:
                position = SpatialReference.ForLocation(new LocationId(source.LocationIdValue));
                return true;
            case SpatialReferenceKind.Crossing:
                position = SpatialReference.ForCrossing(new CrossingId(source.CrossingIdValue));
                return true;
            default:
                return false;
        }
    }

    private static bool IsBaselineSpatialReference(P12ESpatialReferenceSnapshot source)
    {
        if (source == null || source.TopologyOwnerKind.HasValue
            || source.TopologyOwnerRuntimeId != null || source.SubLocationRuntimeId != null)
            return false;
        switch (source.Kind)
        {
            case SpatialReferenceKind.Hex:
                return !string.IsNullOrWhiteSpace(source.HexIdValue)
                    && source.LocationIdValue == null && source.CrossingIdValue == null;
            case SpatialReferenceKind.Location:
                return !string.IsNullOrWhiteSpace(source.LocationIdValue)
                    && source.HexIdValue == null && source.CrossingIdValue == null;
            case SpatialReferenceKind.Crossing:
                return !string.IsNullOrWhiteSpace(source.CrossingIdValue)
                    && source.HexIdValue == null && source.LocationIdValue == null;
            default:
                return false;
        }
    }

    private static bool IsBaselineSpatialReference(SpatialReference source)
    {
        if (source == null || source.TopologyOwnerKind.HasValue
            || source.TopologyOwnerRuntimeId != null || source.SubLocationRuntimeId != null)
            return false;
        switch (source.Kind)
        {
            case SpatialReferenceKind.Hex:
                return source.HexId != null && source.LocationId == null && source.CrossingId == null;
            case SpatialReferenceKind.Location:
                return source.LocationId != null && source.HexId == null && source.CrossingId == null;
            case SpatialReferenceKind.Crossing:
                return source.CrossingId != null && source.HexId == null && source.LocationId == null;
            default:
                return false;
        }
    }

    private static bool MatchesOwnerSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        int count = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            count++;
            if (section.SchemaVersion != schemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != cardinality
                || section.Revision != revision)
                return false;
        }
        return count == 1;
    }

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in rows) copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }

    private static bool Fail(
        P12EMilitaryOwnerSnapshotFailureCode code,
        string message,
        out P12EMilitaryOwnerSnapshotFailure failure)
    {
        failure = P12EMilitaryOwnerSnapshotFailure.Create(code, message);
        return false;
    }
}

internal sealed class P12EMilitaryForceSnapshotRecord
{
    internal string ForceIdValue { get; }
    internal string DisplayName { get; }
    internal long CreatedAbsoluteDay { get; }
    internal string ParentForceIdValue { get; }
    internal string OperationalLocationReference { get; }
    internal string CommanderPersonIdValue { get; }
    internal ArmedForceLifecycleState LifecycleState { get; }
    internal long? TerminatedAbsoluteDay { get; }
    internal bool IsDetached { get; }

    internal P12EMilitaryForceSnapshotRecord(
        string forceIdValue,
        string displayName,
        long createdAbsoluteDay,
        string parentForceIdValue,
        string operationalLocationReference,
        string commanderPersonIdValue,
        ArmedForceLifecycleState lifecycleState,
        long? terminatedAbsoluteDay,
        bool isDetached)
    {
        ForceIdValue = forceIdValue;
        DisplayName = displayName;
        CreatedAbsoluteDay = createdAbsoluteDay;
        ParentForceIdValue = parentForceIdValue;
        OperationalLocationReference = operationalLocationReference;
        CommanderPersonIdValue = commanderPersonIdValue;
        LifecycleState = lifecycleState;
        TerminatedAbsoluteDay = terminatedAbsoluteDay;
        IsDetached = isDetached;
    }

    internal P12EMilitaryForceSnapshotRecord Copy() => new P12EMilitaryForceSnapshotRecord(
        ForceIdValue, DisplayName, CreatedAbsoluteDay, ParentForceIdValue,
        OperationalLocationReference, CommanderPersonIdValue, LifecycleState,
        TerminatedAbsoluteDay, IsDetached);
}

internal sealed class P12EMilitaryContingentSnapshotRecord
{
    internal string ContingentIdValue { get; }
    internal string ForceIdValue { get; }
    internal long Amount { get; }
    internal string OriginDomain { get; }
    internal string OriginValue { get; }
    internal string ServiceType { get; }
    internal IReadOnlyList<P12EMilitaryCharacteristicSnapshot> Characteristics { get; }

    internal P12EMilitaryContingentSnapshotRecord(
        string contingentIdValue,
        string forceIdValue,
        long amount,
        string originDomain,
        string originValue,
        string serviceType,
        IEnumerable<P12EMilitaryCharacteristicSnapshot> characteristics)
    {
        ContingentIdValue = contingentIdValue;
        ForceIdValue = forceIdValue;
        Amount = amount;
        OriginDomain = originDomain;
        OriginValue = originValue;
        ServiceType = serviceType;
        Characteristics = CopyRows(characteristics, row => row?.Copy());
    }

    internal P12EMilitaryContingentSnapshotRecord Copy() => new P12EMilitaryContingentSnapshotRecord(
        ContingentIdValue, ForceIdValue, Amount, OriginDomain, OriginValue, ServiceType, Characteristics);

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in rows) copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class P12EMilitaryCharacteristicSnapshot
{
    internal string Key { get; }
    internal string Value { get; }
    internal P12EMilitaryCharacteristicSnapshot(string key, string value) { Key = key; Value = value; }
    internal P12EMilitaryCharacteristicSnapshot Copy() => new P12EMilitaryCharacteristicSnapshot(Key, Value);
}

internal sealed class P12EMilitaryRelevantPersonSnapshotRecord
{
    internal string ReferenceIdValue { get; }
    internal string ForceIdValue { get; }
    internal string PersonIdValue { get; }
    internal string RoleKey { get; }
    internal P12EMilitaryRelevantPersonSnapshotRecord(
        string referenceIdValue, string forceIdValue, string personIdValue, string roleKey)
    {
        ReferenceIdValue = referenceIdValue;
        ForceIdValue = forceIdValue;
        PersonIdValue = personIdValue;
        RoleKey = roleKey;
    }
    internal P12EMilitaryRelevantPersonSnapshotRecord Copy() =>
        new P12EMilitaryRelevantPersonSnapshotRecord(ReferenceIdValue, ForceIdValue, PersonIdValue, RoleKey);
}

internal sealed class P12EManpowerStateSnapshotRecord
{
    internal string ContingentIdValue { get; }
    internal string SourceIdValue { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12EManpowerCohortSnapshotRecord> Cohorts { get; }
    internal P12EManpowerStateSnapshotRecord(
        string contingentIdValue,
        string sourceIdValue,
        long revision,
        IEnumerable<P12EManpowerCohortSnapshotRecord> cohorts)
    {
        ContingentIdValue = contingentIdValue;
        SourceIdValue = sourceIdValue;
        Revision = revision;
        Cohorts = CopyRows(cohorts, row => row?.Copy());
    }
    internal P12EManpowerStateSnapshotRecord Copy() =>
        new P12EManpowerStateSnapshotRecord(ContingentIdValue, SourceIdValue, Revision, Cohorts);
    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in rows) copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class P12EManpowerCohortSnapshotRecord
{
    internal ManpowerInjuryState InjuryState { get; }
    internal ManpowerCustodyState CustodyState { get; }
    internal string CustodianForceIdValue { get; }
    internal ManpowerAvailabilityState AvailabilityState { get; }
    internal long Amount { get; }
    internal P12EManpowerCohortSnapshotRecord(
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        string custodianForceIdValue,
        ManpowerAvailabilityState availabilityState,
        long amount)
    {
        InjuryState = injuryState;
        CustodyState = custodyState;
        CustodianForceIdValue = custodianForceIdValue;
        AvailabilityState = availabilityState;
        Amount = amount;
    }
    internal P12EManpowerCohortSnapshotRecord Copy() => new P12EManpowerCohortSnapshotRecord(
        InjuryState, CustodyState, CustodianForceIdValue, AvailabilityState, Amount);
}

internal sealed class P12EForcePositionSnapshotRecord
{
    internal string ForceIdValue { get; }
    internal P12ESpatialReferenceSnapshot Reference { get; }
    internal P12EForcePositionSnapshotRecord(string forceIdValue, P12ESpatialReferenceSnapshot reference)
    {
        ForceIdValue = forceIdValue;
        Reference = reference?.Copy();
    }
    internal P12EForcePositionSnapshotRecord Copy() => new P12EForcePositionSnapshotRecord(ForceIdValue, Reference);
}

internal sealed class P12ESpatialReferenceSnapshot
{
    internal SpatialReferenceKind Kind { get; }
    internal string HexIdValue { get; }
    internal string LocationIdValue { get; }
    internal string CrossingIdValue { get; }
    internal LocalTopologyOwnerKind? TopologyOwnerKind { get; }
    internal string TopologyOwnerRuntimeId { get; }
    internal string SubLocationRuntimeId { get; }
    internal P12ESpatialReferenceSnapshot(
        SpatialReferenceKind kind,
        string hexIdValue,
        string locationIdValue,
        string crossingIdValue,
        LocalTopologyOwnerKind? topologyOwnerKind,
        string topologyOwnerRuntimeId,
        string subLocationRuntimeId)
    {
        Kind = kind;
        HexIdValue = hexIdValue;
        LocationIdValue = locationIdValue;
        CrossingIdValue = crossingIdValue;
        TopologyOwnerKind = topologyOwnerKind;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SubLocationRuntimeId = subLocationRuntimeId;
    }
    internal P12ESpatialReferenceSnapshot Copy() => new P12ESpatialReferenceSnapshot(
        Kind, HexIdValue, LocationIdValue, CrossingIdValue,
        TopologyOwnerKind, TopologyOwnerRuntimeId, SubLocationRuntimeId);
}
