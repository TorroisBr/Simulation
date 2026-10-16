using System.Collections.Generic;

/// <summary>
/// Narrow Unity Runtime and P12 access to the authoritative institution and
/// office owners. This is not a general Store mutation API.
/// </summary>
public static class InstitutionOfficeOwnerBridge
{
    public static bool CanBindMutationStatus(
        InstitutionStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.CanBindMutationStatus(status);
    }

    public static bool TryBindMutationStatus(
        InstitutionStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.TryBindMutationStatus(status);
    }

    public static bool CanBindMutationStatus(
        OfficeStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.CanBindMutationStatus(status);
    }

    public static bool TryBindMutationStatus(
        OfficeStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.TryBindMutationStatus(status);
    }

    public static InstitutionStore GetInstitutionOwner(OfficeStore owner)
    {
        return owner.InstitutionStoreForWorldBoundary;
    }

    public static bool BelongsTo(OfficeStore owner, InstitutionStore institutionStore)
    {
        return owner != null
            && System.Object.ReferenceEquals(owner.InstitutionStoreForWorldBoundary, institutionStore);
    }

    public static IReadOnlyList<OfficeTenureRecord> GetTenureHistoryInMutationOrder(OfficeStore owner)
    {
        return owner.TenureHistoryInMutationOrder;
    }

    public static OfficeTenureRecord CreateTenureRecord(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        long? endAbsoluteDay,
        InstitutionalVacancyRecognitionReason? endReason,
        bool isClosed)
    {
        return new OfficeTenureRecord(
            officeId,
            incumbent,
            startAbsoluteDay,
            endAbsoluteDay,
            endReason,
            isClosed);
    }

    public static bool TryAddHistoricalTenureForRuntimeOfficeClone(
        OfficeStore replayCandidate,
        OfficeTenureRecord record,
        out InstitutionFoundationFailure failure)
    {
        return replayCandidate.TryAddHistoricalTenure(record, out failure);
    }

    public static bool TryGetLatestClosedTenure(
        OfficeStore source,
        OfficeId officeId,
        out OfficeTenureRecord tenure)
    {
        return source.TryGetLatestClosedTenure(officeId, out tenure);
    }

    public static bool TryCreateStagedOwners(
        IReadOnlyList<InstitutionRecord> institutionRecords,
        long institutionRevision,
        IReadOnlyList<OfficeRecord> officeRecords,
        IReadOnlyList<OfficeIncumbency> officeIncumbencies,
        IReadOnlyList<OfficeTenureRecord> tenureHistoryInSourceMutationOrder,
        long officeRevision,
        out InstitutionStore institutionStore,
        out OfficeStore officeStore)
    {
        institutionStore = null;
        officeStore = null;
        if (!InstitutionStore.TryCreateFromP12EOwnerSnapshot(
            institutionRecords,
            institutionRevision,
            out InstitutionStore stagedInstitutions))
        {
            return false;
        }

        if (!OfficeStore.TryCreateFromP12EOwnerSnapshot(
            stagedInstitutions,
            officeRecords,
            officeIncumbencies,
            tenureHistoryInSourceMutationOrder,
            officeRevision,
            out OfficeStore stagedOffices))
        {
            return false;
        }

        institutionStore = stagedInstitutions;
        officeStore = stagedOffices;
        return true;
    }

    public static bool TryCloneForRestoredContinuation(
        OfficeStore source,
        InstitutionStore targetInstitution,
        out OfficeStore clone)
    {
        clone = null;
        if (source == null || targetInstitution == null)
        {
            return false;
        }

        return OfficeStore.TryCreateFromP12EOwnerSnapshot(
            targetInstitution,
            source.Offices,
            source.Incumbencies,
            source.TenureHistoryInMutationOrder,
            source.Revision,
            out clone);
    }
}
