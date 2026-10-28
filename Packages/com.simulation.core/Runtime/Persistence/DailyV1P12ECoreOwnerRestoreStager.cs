using System;
using System.Collections.Generic;

internal enum P12ECoreOwnerRestoreStep
{
    InstitutionOffice = 0,
    PropertyEstate = 1,
    Faction = 2,
    PoliticalClaim = 3,
    PoliticalSupport = 4,
    PoliticalDecision = 5,
    Military = 6,
    Conflict = 7,
    War = 8,
    Battle = 9,
    JusticeRecords = 10
}

internal enum P12ECoreOwnerRestoreFailure
{
    None = 0,
    InvalidInput,
    StageFailed
}

/// <summary>Private staged owner roots available to the Core-selected P12-E restore chain.</summary>
internal sealed class P12ECoreOwnerRestoreStage
{
    internal PersonStore Persons { get; }
    internal InstitutionStore Institutions { get; }
    internal OfficeStore Offices { get; }
    internal PropertyOwnershipStore PropertyOwnership { get; }
    internal EstateStore Estates { get; }
    internal FactionStore Factions { get; }
    internal PoliticalClaimStore PoliticalClaims { get; }
    internal PoliticalSupportStore PoliticalSupport { get; }
    internal PoliticalDecisionStore PoliticalDecisions { get; }
    internal ArmedForceStore ArmedForces { get; }
    internal ContingentManpowerStateStore Manpower { get; }
    internal ArmedForceSpatialStateStore Positions { get; }
    internal PersistentConflictStore Conflicts { get; }
    internal PersistentWarStore Wars { get; }
    internal PersistentBattleStore Battles { get; }
    internal DailyV1JusticeRecordsOwner JusticeRecords { get; }

    internal P12ECoreOwnerRestoreStage(
        PersonStore persons,
        InstitutionStore institutions,
        OfficeStore offices,
        PropertyOwnershipStore propertyOwnership,
        EstateStore estates,
        FactionStore factions,
        PoliticalClaimStore politicalClaims,
        PoliticalSupportStore politicalSupport,
        PoliticalDecisionStore politicalDecisions,
        ArmedForceStore armedForces,
        ContingentManpowerStateStore manpower,
        ArmedForceSpatialStateStore positions,
        PersistentConflictStore conflicts,
        PersistentWarStore wars,
        PersistentBattleStore battles,
        DailyV1JusticeRecordsOwner justiceRecords)
    {
        Persons = persons;
        Institutions = institutions;
        Offices = offices;
        PropertyOwnership = propertyOwnership;
        Estates = estates;
        Factions = factions;
        PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport;
        PoliticalDecisions = politicalDecisions;
        ArmedForces = armedForces;
        Manpower = manpower;
        Positions = positions;
        Conflicts = conflicts;
        Wars = wars;
        Battles = battles;
        JusticeRecords = justiceRecords;
    }
}

/// <summary>
/// A private, ordered builder for the P12-E owner chain whose authoritative owners and snapshot staging are in Core.
/// No staged reference is returned until every requested owner stage succeeds.
/// </summary>
internal sealed class DailyV1P12ECoreOwnerRestoreBuilder
{
    private readonly PersonStore persons;
    private readonly SpatialAuthorityStore spatialAuthority;
    private readonly long capturedDay;
    private int nextStep;
    private bool failed;
    private InstitutionStore institutions;
    private OfficeStore offices;
    private PropertyOwnershipStore propertyOwnership;
    private EstateStore estates;
    private FactionStore factions;
    private PoliticalClaimStore politicalClaims;
    private PoliticalSupportStore politicalSupport;
    private PoliticalDecisionStore politicalDecisions;
    private ArmedForceStore armedForces;
    private ContingentManpowerStateStore manpower;
    private ArmedForceSpatialStateStore positions;
    private PersistentConflictStore conflicts;
    private PersistentWarStore wars;
    private PersistentBattleStore battles;
    private DailyV1JusticeRecordsOwner justiceRecords;

    private DailyV1P12ECoreOwnerRestoreBuilder(
        PersonStore persons, SpatialAuthorityStore spatialAuthority, long capturedDay)
    {
        this.persons = persons;
        this.spatialAuthority = spatialAuthority;
        this.capturedDay = capturedDay;
    }

    internal static bool TryCreate(
        PersonStore persons,
        SpatialAuthorityStore spatialAuthority,
        long capturedDay,
        out DailyV1P12ECoreOwnerRestoreBuilder builder,
        out P12ECoreOwnerRestoreFailure failure)
    {
        builder = null;
        if (persons == null || spatialAuthority == null || capturedDay < 0L)
        {
            failure = P12ECoreOwnerRestoreFailure.InvalidInput;
            return false;
        }
        builder = new DailyV1P12ECoreOwnerRestoreBuilder(persons, spatialAuthority, capturedDay);
        failure = P12ECoreOwnerRestoreFailure.None;
        return true;
    }

    internal bool TryStageInstitutionOffice(
        P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.InstitutionOffice, out failure)) return false;
        try
        {
            if (snapshot == null || !snapshot.TryCreateStagedOwners(persons,
                    out InstitutionStore stagedInstitutions,
                    out OfficeStore stagedOffices,
                    out _))
                return MarkFailed(out failure);
            institutions = stagedInstitutions;
            offices = stagedOffices;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStagePropertyEstate(
        PropertyEstateOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.PropertyEstate, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null
                || !snapshot.TryStage(persons, capturedDay,
                    out PropertyOwnershipStore stagedPropertyOwnership,
                    out EstateStore stagedEstates,
                    out _))
                return MarkFailed(out failure);
            propertyOwnership = stagedPropertyOwnership;
            estates = stagedEstates;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageFaction(
        P12EFactionOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.Faction, out failure)) return false;
        try
        {
            if (snapshot == null
                || !snapshot.TryCreateStagedOwner(persons, capturedDay,
                    out FactionStore stagedFactions,
                    out _))
                return MarkFailed(out failure);
            factions = stagedFactions;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStagePoliticalClaims(
        P12EPoliticalClaimOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.PoliticalClaim, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null || propertyOwnership == null
                || institutions == null || offices == null
                || !snapshot.TryStage(persons, propertyOwnership, institutions, offices,
                    capturedDay, out PoliticalClaimStore stagedPoliticalClaims, out _))
                return MarkFailed(out failure);
            politicalClaims = stagedPoliticalClaims;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStagePoliticalSupport(
        P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.PoliticalSupport, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null || factions == null || politicalClaims == null
                || !snapshot.TryStage(persons, factions, politicalClaims,
                    out PoliticalSupportStore stagedPoliticalSupport, out _))
                return MarkFailed(out failure);
            politicalSupport = stagedPoliticalSupport;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStagePoliticalDecisions(
        P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.PoliticalDecision, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null || institutions == null || offices == null
                || politicalClaims == null
                || !snapshot.TryStage(persons, institutions, offices, politicalClaims,
                    out PoliticalDecisionStore stagedPoliticalDecisions, out _))
                return MarkFailed(out failure);
            politicalDecisions = stagedPoliticalDecisions;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageMilitary(
        P12EMilitaryOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.Military, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null || spatialAuthority == null
                || !snapshot.TryStage(persons, spatialAuthority, null,
                    out ArmedForceStore stagedArmedForces,
                    out ContingentManpowerStateStore stagedManpower,
                    out ArmedForceSpatialStateStore stagedPositions, out _))
                return MarkFailed(out failure);
            armedForces = stagedArmedForces;
            manpower = stagedManpower;
            positions = stagedPositions;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageConflicts(
        PersistentConflictOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.Conflict, out failure)) return false;
        try
        {
            if (snapshot == null || armedForces == null
                || !snapshot.TryStage(armedForces, out PersistentConflictStore stagedConflicts, out _))
                return MarkFailed(out failure);
            conflicts = stagedConflicts;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageWars(
        PersistentWarOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.War, out failure)) return false;
        try
        {
            if (snapshot == null || armedForces == null || conflicts == null
                || !snapshot.TryStage(armedForces, conflicts, out PersistentWarStore stagedWars, out _))
                return MarkFailed(out failure);
            wars = stagedWars;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageBattles(
        PersistentBattleOwnerSnapshot snapshot,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.Battle, out failure)) return false;
        try
        {
            if (snapshot == null || armedForces == null || conflicts == null || wars == null
                || spatialAuthority == null
                || !snapshot.TryStage(armedForces, conflicts, wars, spatialAuthority, null,
                    out PersistentBattleStore stagedBattles, out _))
                return MarkFailed(out failure);
            battles = stagedBattles;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryStageJusticeRecords(
        P12EJusticeRecordsOwnerSnapshot snapshot,
        IReadOnlyList<DailyV1CityRuntimeOwners> stagedCities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcs,
        out P12ECoreOwnerRestoreFailure failure)
    {
        if (!CanStage(P12ECoreOwnerRestoreStep.JusticeRecords, out failure)) return false;
        try
        {
            if (snapshot == null || persons == null
                || snapshot.CapturedAbsoluteDay != capturedDay
                || !snapshot.TryStageCoreOwner(persons, stagedCities, stagedNpcs,
                    out DailyV1JusticeRecordsOwner stagedJustice, out _))
                return MarkFailed(out failure);
            justiceRecords = stagedJustice;
            nextStep++;
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (IsStageException(exception))
        {
            return MarkFailed(out failure);
        }
    }

    internal bool TryBuild(
        out P12ECoreOwnerRestoreStage staged,
        out P12ECoreOwnerRestoreFailure failure)
    {
        staged = null;
        if (failed || nextStep != 11 || persons == null || spatialAuthority == null
            || institutions == null || offices == null || propertyOwnership == null || estates == null
            || factions == null || politicalClaims == null || politicalSupport == null
            || politicalDecisions == null || armedForces == null || manpower == null || positions == null
            || conflicts == null || wars == null || battles == null || justiceRecords == null)
        {
            failure = P12ECoreOwnerRestoreFailure.StageFailed;
            return false;
        }
        staged = new P12ECoreOwnerRestoreStage(persons, institutions, offices,
            propertyOwnership, estates, factions, politicalClaims, politicalSupport,
            politicalDecisions, armedForces, manpower, positions, conflicts, wars, battles, justiceRecords);
        failure = P12ECoreOwnerRestoreFailure.None;
        return true;
    }

    private bool CanStage(P12ECoreOwnerRestoreStep expected, out P12ECoreOwnerRestoreFailure failure)
    {
        if (!failed && nextStep == (int)expected)
        {
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        failed = true;
        failure = P12ECoreOwnerRestoreFailure.StageFailed;
        return false;
    }

    private bool MarkFailed(out P12ECoreOwnerRestoreFailure failure)
    {
        failed = true;
        failure = P12ECoreOwnerRestoreFailure.StageFailed;
        return false;
    }

    private static bool IsStageException(Exception exception) =>
        exception is ArgumentException
        || exception is InvalidOperationException
        || exception is OverflowException
        || exception is NullReferenceException;
}

/// <summary>Stages the dependency-ordered P12-E owner chain whose authorities are already in Core.</summary>
internal static class DailyV1P12ECoreOwnerRestoreStager
{
    internal static bool TryStage(
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles,
        P12EJusticeRecordsOwnerSnapshot justiceRecords,
        PersonStore persons,
        SpatialAuthorityStore spatialAuthority,
        IReadOnlyList<DailyV1CityRuntimeOwners> stagedCities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcs,
        long capturedDay,
        Action<P12ECoreOwnerRestoreStep> stageObserver,
        out P12ECoreOwnerRestoreStage staged,
        out P12ECoreOwnerRestoreFailure failure)
    {
        staged = null;
        if (!DailyV1P12ECoreOwnerRestoreBuilder.TryCreate(
                persons, spatialAuthority, capturedDay,
                out DailyV1P12ECoreOwnerRestoreBuilder builder, out failure))
            return false;

        if (!builder.TryStageInstitutionOffice(institutionOffice, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.InstitutionOffice);
        if (!builder.TryStagePropertyEstate(propertyEstate, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PropertyEstate);
        if (!builder.TryStageFaction(faction, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.Faction);
        if (!builder.TryStagePoliticalClaims(politicalClaims, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PoliticalClaim);
        if (!builder.TryStagePoliticalSupport(politicalSupport, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PoliticalSupport);
        if (!builder.TryStagePoliticalDecisions(politicalDecisions, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PoliticalDecision);
        if (!builder.TryStageMilitary(military, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.Military);
        if (!builder.TryStageConflicts(conflicts, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.Conflict);
        if (!builder.TryStageWars(wars, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.War);
        if (!builder.TryStageBattles(battles, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.Battle);
        if (!builder.TryStageJusticeRecords(justiceRecords, stagedCities, stagedNpcs, out failure)) return false;
        stageObserver?.Invoke(P12ECoreOwnerRestoreStep.JusticeRecords);

        return builder.TryBuild(out staged, out failure);
    }
}