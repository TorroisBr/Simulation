using System;

internal enum P12ECoreOwnerRestoreStep
{
    InstitutionOffice = 0,
    PropertyEstate = 1,
    Faction = 2,
    PoliticalClaim = 3,
    PoliticalSupport = 4
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

    internal P12ECoreOwnerRestoreStage(
        PersonStore persons,
        InstitutionStore institutions,
        OfficeStore offices,
        PropertyOwnershipStore propertyOwnership,
        EstateStore estates,
        FactionStore factions,
        PoliticalClaimStore politicalClaims,
        PoliticalSupportStore politicalSupport)
    {
        Persons = persons;
        Institutions = institutions;
        Offices = offices;
        PropertyOwnership = propertyOwnership;
        Estates = estates;
        Factions = factions;
        PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport;
    }
}

/// <summary>
/// A private, ordered builder for the P12-E owner chain whose authoritative owners and snapshot staging are in Core.
/// No staged reference is returned until every requested owner stage succeeds.
/// </summary>
internal sealed class DailyV1P12ECoreOwnerRestoreBuilder
{
    private readonly PersonStore persons;
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

    private DailyV1P12ECoreOwnerRestoreBuilder(PersonStore persons, long capturedDay)
    {
        this.persons = persons;
        this.capturedDay = capturedDay;
    }

    internal static bool TryCreate(
        PersonStore persons,
        long capturedDay,
        out DailyV1P12ECoreOwnerRestoreBuilder builder,
        out P12ECoreOwnerRestoreFailure failure)
    {
        builder = null;
        if (persons == null || capturedDay < 0L)
        {
            failure = P12ECoreOwnerRestoreFailure.InvalidInput;
            return false;
        }
        builder = new DailyV1P12ECoreOwnerRestoreBuilder(persons, capturedDay);
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

    internal bool TryBuild(
        out P12ECoreOwnerRestoreStage staged,
        out P12ECoreOwnerRestoreFailure failure)
    {
        staged = null;
        if (failed || nextStep != 5 || persons == null || institutions == null || offices == null
            || propertyOwnership == null || estates == null || factions == null || politicalClaims == null
            || politicalSupport == null)
        {
            failure = P12ECoreOwnerRestoreFailure.StageFailed;
            return false;
        }
        staged = new P12ECoreOwnerRestoreStage(persons, institutions, offices,
            propertyOwnership, estates, factions, politicalClaims, politicalSupport);
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
        PersonStore persons,
        long capturedDay,
        Action<P12ECoreOwnerRestoreStep> stageObserver,
        out P12ECoreOwnerRestoreStage staged,
        out P12ECoreOwnerRestoreFailure failure)
    {
        staged = null;
        if (!DailyV1P12ECoreOwnerRestoreBuilder.TryCreate(persons, capturedDay,
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

        return builder.TryBuild(out staged, out failure);
    }
}