using System;
using System.Collections.Generic;

public enum OwnerSectionRole
{
    Required,
    ExplicitlyEmpty,
    Excluded
}

/// <summary>Explicit non-P12 domain composition used only by bounded proving profiles.</summary>
public enum SimulationRuntimeCompositionProfile
{
    Standard = 0,
    P15AProvingStructure = 1,
    P16AOneHopMilitary = 2,
    P17AWithdrawalWar = 3
}

internal enum DailyCaptureEligibilityFailure
{
    None = 0,
    UnsupportedProfile = 1,
    WrongOwnerThread = 2,
    RuntimeFaulted = 3,
    OperationInProgress = 4,
    NoCompletedBoundary = 5,
    StaleToken = 6,
    OwnerCoverageIncomplete = 7,
    WorldNotPublished = 8
}

internal enum DailyCaptureBoundaryProvenance
{
    CompletedAdvance = 1,
    RestoredContinuation = 2
}

/// <summary>
/// Ephemeral, in-memory proof that one live Daily-v1 runtime has an admitted
/// completed or restored continuation boundary and remains unchanged at that
/// boundary. This is not serialized authority or a domain-state export.
/// </summary>
internal sealed class DailyCaptureEligibilityToken
{
    internal DailyCaptureEligibilityToken(
        object runtimeInstanceIdentity,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long completedCoreSequence,
        DailyCaptureBoundaryProvenance boundaryProvenance,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
    {
        RuntimeInstanceIdentity = runtimeInstanceIdentity ?? throw new ArgumentNullException(nameof(runtimeInstanceIdentity));
        AdmissionContext = admissionContext ?? throw new ArgumentNullException(nameof(admissionContext));
        ConfigurationIdentity = configurationIdentity ?? throw new ArgumentNullException(nameof(configurationIdentity));
        CalendarIdentity = calendarIdentity ?? throw new ArgumentNullException(nameof(calendarIdentity));
        CompositionProfile = compositionProfile;
        WorldId = worldId ?? throw new ArgumentNullException(nameof(worldId));
        AbsoluteDay = absoluteDay;
        CompletedCoreSequence = completedCoreSequence;
        BoundaryProvenance = boundaryProvenance;
        MutationEpoch = mutationEpoch;
        OwnerSections = ownerSections ?? throw new ArgumentNullException(nameof(ownerSections));
    }

    internal DailyCaptureEligibilityToken(
        object runtimeInstanceIdentity,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long completedCoreSequence,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
        : this(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            completedCoreSequence,
            DailyCaptureBoundaryProvenance.CompletedAdvance,
            mutationEpoch,
            ownerSections)
    {
    }

    internal object RuntimeInstanceIdentity { get; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; }
    internal EffectiveSimulationConfiguration ConfigurationIdentity { get; }
    internal SimulationCalendar CalendarIdentity { get; }
    internal SimulationRuntimeCompositionProfile CompositionProfile { get; }
    internal WorldId WorldId { get; }
    internal long AbsoluteDay { get; }
    internal long CompletedCoreSequence { get; }
    internal DailyCaptureBoundaryProvenance BoundaryProvenance { get; }
    internal long MutationEpoch { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> OwnerSections { get; }
}

/// <summary>Copied live owner evidence captured only at a quiescent boundary.</summary>
internal sealed class OwnerSectionCensusSnapshot
{
    internal OwnerSectionCensusSnapshot(
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role,
        object ownerInstanceIdentity,
        int cardinality,
        long revision)
    {
        SectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
        SchemaVersion = schemaVersion;
        Role = role;
        OwnerInstanceIdentity = ownerInstanceIdentity ?? throw new ArgumentNullException(nameof(ownerInstanceIdentity));
        Cardinality = cardinality;
        Revision = revision;
    }

    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal OwnerSectionRole Role { get; }
    internal object OwnerInstanceIdentity { get; }
    internal int Cardinality { get; }
    internal long Revision { get; }
}
