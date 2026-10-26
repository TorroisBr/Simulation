using System;

internal enum P12ECoreOwnerRestoreStep
{
    InstitutionOffice = 0,
    PropertyEstate = 1,
    Faction = 2,
    PoliticalClaim = 3
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

    internal P12ECoreOwnerRestoreStage(
        PersonStore persons,
        InstitutionStore institutions,
        OfficeStore offices,
        PropertyOwnershipStore propertyOwnership,
        EstateStore estates,
        FactionStore factions,
        PoliticalClaimStore politicalClaims)
    {
        Persons = persons;
        Institutions = institutions;
        Offices = offices;
        PropertyOwnership = propertyOwnership;
        Estates = estates;
        Factions = factions;
        PoliticalClaims = politicalClaims;
    }
}

/// <summary>Stages the dependency-ordered P12-E owner chain whose authorities are already in Core.</summary>
internal static class DailyV1P12ECoreOwnerRestoreStager
{
    internal static bool TryStage(
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        PersonStore persons,
        long capturedDay,
        Action<P12ECoreOwnerRestoreStep> stageObserver,
        out P12ECoreOwnerRestoreStage staged,
        out P12ECoreOwnerRestoreFailure failure)
    {
        staged = null;
        failure = P12ECoreOwnerRestoreFailure.InvalidInput;
        if (institutionOffice == null || propertyEstate == null || faction == null
            || politicalClaims == null || persons == null || capturedDay < 0L)
            return false;

        try
        {
            if (!institutionOffice.TryCreateStagedOwners(persons,
                    out InstitutionStore stagedInstitutions,
                    out OfficeStore stagedOffices,
                    out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12ECoreOwnerRestoreStep.InstitutionOffice);

            if (!propertyEstate.TryStage(persons, capturedDay,
                    out PropertyOwnershipStore stagedPropertyOwnership,
                    out EstateStore stagedEstates,
                    out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PropertyEstate);

            if (!faction.TryCreateStagedOwner(persons, capturedDay,
                    out FactionStore stagedFactions,
                    out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12ECoreOwnerRestoreStep.Faction);

            if (!politicalClaims.TryStage(persons, stagedPropertyOwnership,
                    stagedInstitutions, stagedOffices, capturedDay,
                    out PoliticalClaimStore stagedPoliticalClaims,
                    out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12ECoreOwnerRestoreStep.PoliticalClaim);

            if (stagedInstitutions == null || stagedOffices == null
                || stagedPropertyOwnership == null || stagedEstates == null
                || stagedFactions == null || stagedPoliticalClaims == null)
                return StageFailed(out failure);

            staged = new P12ECoreOwnerRestoreStage(persons, stagedInstitutions,
                stagedOffices, stagedPropertyOwnership, stagedEstates,
                stagedFactions, stagedPoliticalClaims);
            failure = P12ECoreOwnerRestoreFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is NullReferenceException)
        {
            staged = null;
            failure = P12ECoreOwnerRestoreFailure.StageFailed;
            return false;
        }
    }

    private static bool StageFailed(out P12ECoreOwnerRestoreFailure failure)
    {
        failure = P12ECoreOwnerRestoreFailure.StageFailed;
        return false;
    }
}