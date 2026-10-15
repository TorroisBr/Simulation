using System.Collections.Generic;

/// <summary>
/// Narrow Unity composition and persistence access to the authoritative population owner.
/// This bridge is not a general mutation or mod API.
/// </summary>
public static class PopulationOwnerBridge
{
    /// <summary>
    /// Reconstructs the immutable transition value validated by the retained
    /// P12 persistence codec. This does not apply the transition to an owner.
    /// </summary>
    public static SettlementPopulationTransition CreateDecodedTransition(
        string settlementRuntimeId,
        long expectedRevision,
        int populationBefore,
        PopulationChangeSet changes,
        long netChange,
        int populationAfter)
    {
        return new SettlementPopulationTransition(
            settlementRuntimeId,
            expectedRevision,
            populationBefore,
            changes,
            netChange,
            populationAfter);
    }

    public static bool CanBindMutationStatus(
        SettlementPopulationRuntime owner,
        IAuthoritativeMutationStatus status)
    {
        return owner.CanBindMutationStatus(status);
    }

    public static bool TryBindMutationStatus(
        SettlementPopulationRuntime owner,
        IAuthoritativeMutationStatus status)
    {
        return owner.TryBindMutationStatus(status);
    }

    public static bool IsP12PopulationBound(SettlementPopulationRuntime owner)
    {
        return owner.IsP12PopulationBound;
    }

    public static bool CanAdvanceP12AggregateRevision(SettlementPopulationRuntime owner)
    {
        return owner.CanAdvanceP12AggregateRevision;
    }

    public static void BindP12PopulationMutationBoundary(
        SettlementPopulationRuntime owner,
        System.Func<SettlementPopulationRuntime, bool, bool, bool> admission,
        System.Action<SettlementPopulationRuntime, bool, bool> committed)
    {
        owner.BindP12PopulationMutationBoundary(admission, committed);
    }

    public static void GetOperationReceiptCensus(
        SettlementPopulationRuntime owner,
        out int cardinality,
        out long receiptRevision)
    {
        owner.GetOperationReceiptCensus(out cardinality, out receiptRevision);
    }

    public static bool TryCaptureOperationReceiptSnapshot(
        SettlementPopulationRuntime owner,
        out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
        out long receiptRevision)
    {
        return owner.TryCaptureOperationReceiptSnapshot(out receipts, out receiptRevision);
    }

    public static bool TryCreateFromOwnerSnapshot(
        string settlementRuntimeId,
        int population,
        long revision,
        IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
        long receiptRevision,
        out SettlementPopulationRuntime stagedOwner)
    {
        return SettlementPopulationRuntime.TryCreateFromOwnerSnapshot(
            settlementRuntimeId,
            population,
            revision,
            receipts,
            receiptRevision,
            out stagedOwner);
    }

    public static PopulationOperationReceiptResolution ResolveOperationReceipt(
        SettlementPopulationRuntime owner,
        string identity,
        string fingerprint,
        SettlementPopulationTransition transition = null)
    {
        return owner.ResolveOperationReceipt(identity, fingerprint, transition);
    }

    public static bool TryApplyTransitionWithReceipt(
        SettlementPopulationRuntime owner,
        string identity,
        string fingerprint,
        SettlementPopulationTransition transition,
        out bool newlyApplied,
        out PopulationTransitionFailure failure)
    {
        return owner.TryApplyTransitionWithReceipt(
            identity,
            fingerprint,
            transition,
            out newlyApplied,
            out failure);
    }

    public static bool TryApplyPairedMigration(
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int destinationPopulationBefore,
        out PopulationTransitionFailure failure)
    {
        return SettlementPopulationRuntime.TryApplyPairedMigration(
            origin,
            destination,
            originExpectedRevision,
            destinationExpectedRevision,
            originPopulationBefore,
            destinationPopulationBefore,
            out failure);
    }

    public static bool TryApplyAggregateWithReceipt(
        SettlementPopulationRuntime owner,
        int representedResidentFloor,
        AggregateDemographyTransition transition,
        string operationIdentity,
        out bool newlyApplied,
        out AggregateDemographyFailure failure)
    {
        return AggregateDemographySystem.TryApplyWithReceipt(
            owner,
            representedResidentFloor,
            transition,
            operationIdentity,
            out newlyApplied,
            out failure);
    }

    public static PopulationRollbackPoint CaptureRollbackPoint(SettlementPopulationRuntime owner)
    {
        return new PopulationRollbackPoint(owner, owner.CurrentPopulation, owner.Revision);
    }

    public static bool TryRestoreRollbackPoint(
        SettlementPopulationRuntime owner,
        PopulationRollbackPoint point)
    {
        if (owner == null || point == null || !ReferenceEquals(owner, point.Owner))
        {
            return false;
        }

        owner.RestoreSnapshot(point.Population, point.Revision);
        return owner.CurrentPopulation == point.Population
            && owner.Revision == point.Revision;
    }
}
