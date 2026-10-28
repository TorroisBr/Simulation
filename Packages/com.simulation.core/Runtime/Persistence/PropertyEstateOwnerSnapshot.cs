using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum PropertyEstateOwnerSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidOwnerGroup,
    InvalidOwnerSectionVector,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    DuplicateIdentity,
    InvalidReference,
    InvalidDay,
    InvalidOrdering,
    StageFailed
}

internal sealed class PropertyEstateOwnerSnapshotFailure
{
    internal static readonly PropertyEstateOwnerSnapshotFailure None =
        new PropertyEstateOwnerSnapshotFailure(PropertyEstateOwnerSnapshotFailureCode.None, string.Empty);

    internal PropertyEstateOwnerSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private PropertyEstateOwnerSnapshotFailure(PropertyEstateOwnerSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static PropertyEstateOwnerSnapshotFailure Create(
        PropertyEstateOwnerSnapshotFailureCode code,
        string message)
    {
        return code == PropertyEstateOwnerSnapshotFailureCode.None
            ? None
            : new PropertyEstateOwnerSnapshotFailure(code, message);
    }
}

/// <summary>
/// Detached Daily-v1 export of the independent Property ownership, complete
/// transfer-history, and Estate records sections. Capture owner identities and
/// the P12-B token are checked but never retained as serialized values.
/// </summary>
internal sealed class PropertyEstateOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int OwnershipSchemaVersion { get; }
    internal int OwnershipRecordCount { get; }
    internal long PropertyRevision { get; }
    internal IReadOnlyList<PropertyEstateOwnershipRow> OwnershipRows { get; }
    internal int TransferHistorySchemaVersion { get; }
    internal int TransferHistoryRecordCount { get; }
    internal long TransferHistoryRevision { get; }
    internal IReadOnlyList<PropertyEstateTransferHistoryRow> TransferHistoryRows { get; }
    internal int EstateSchemaVersion { get; }
    internal int EstateRecordCount { get; }
    internal long EstateRevision { get; }
    internal IReadOnlyList<PropertyEstateEstateRow> EstateRows { get; }

    internal PropertyEstateOwnerSnapshot(
        int ownershipSchemaVersion,
        int ownershipRecordCount,
        long propertyRevision,
        IEnumerable<PropertyEstateOwnershipRow> ownershipRows,
        int transferHistorySchemaVersion,
        int transferHistoryRecordCount,
        long transferHistoryRevision,
        IEnumerable<PropertyEstateTransferHistoryRow> transferHistoryRows,
        int estateSchemaVersion,
        int estateRecordCount,
        long estateRevision,
        IEnumerable<PropertyEstateEstateRow> estateRows)
    {
        OwnershipSchemaVersion = ownershipSchemaVersion;
        OwnershipRecordCount = ownershipRecordCount;
        PropertyRevision = propertyRevision;
        OwnershipRows = CopyRows(ownershipRows);
        TransferHistorySchemaVersion = transferHistorySchemaVersion;
        TransferHistoryRecordCount = transferHistoryRecordCount;
        TransferHistoryRevision = transferHistoryRevision;
        TransferHistoryRows = CopyRows(transferHistoryRows);
        EstateSchemaVersion = estateSchemaVersion;
        EstateRecordCount = estateRecordCount;
        EstateRevision = estateRevision;
        EstateRows = CopyRows(estateRows);
    }

    internal static bool TryCapture(
        PropertyOwnershipStore propertyOwner,
        EstateStore estateOwner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out PropertyEstateOwnerSnapshot snapshot,
        out PropertyEstateOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = PropertyEstateOwnerSnapshotFailure.Create(
            PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerGroup,
            "The Property/Estate owner group is invalid.");
        if (propertyOwner == null
            || estateOwner == null
            || token == null
            || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidCaptureContext,
                "Capture requires both installed owners and the exact owner-section vector carried by the P12-B token.");
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.UnsupportedProfile,
                "Property/Estate export requires a successful Daily-v1 completed-boundary token.");
            return false;
        }

        PersonStore sharedPersonStore = propertyOwner.PersonStoreForWorldBoundary;
        if (sharedPersonStore == null
            || !ReferenceEquals(sharedPersonStore, estateOwner.PersonStoreForWorldBoundary))
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerGroup,
                "Both owners must be bound to the same world PersonStore.");
            return false;
        }

        IReadOnlyList<PropertyOwnershipRecord> ownership = propertyOwner.OwnershipRecords;
        IReadOnlyList<PropertyOwnershipTransferHistoryRecord> history = propertyOwner.TransferHistory;
        IReadOnlyList<EstateRecord> estates = estateOwner.Estates;
        int ownershipCount = propertyOwner.Count;
        int historyCount = history != null ? history.Count : -1;
        int estateCount = estateOwner.Count;
        long propertyRevision = propertyOwner.Revision;
        long estateRevision = estateOwner.Revision;
        if (ownership == null || history == null || estates == null
            || ownershipCount < 0 || historyCount < 0 || estateCount < 0
            || propertyRevision < 0L || estateRevision < 0L
            || ownership.Count != ownershipCount
            || history.Count != historyCount
            || estates.Count != estateCount
            || propertyRevision != (long)ownershipCount + historyCount
            || estateRevision != estateCount)
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidCardinality,
                "Owner row counts and revisions do not match the current Property/Estate owner contracts.");
            return false;
        }

        if (!MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.SchemaVersion,
                propertyOwner,
                ownershipCount,
                propertyRevision)
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                PropertyOwnershipCensusProvider.TransferHistorySectionId,
                PropertyOwnershipCensusProvider.SchemaVersion,
                propertyOwner,
                historyCount,
                propertyRevision)
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                EstateCensusProvider.SectionId,
                EstateCensusProvider.SchemaVersion,
                estateOwner,
                estateCount,
                estateRevision))
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B vector must contain exactly matching Required witnesses for all three owner sections.");
            return false;
        }

        List<PropertyEstateOwnershipRow> ownershipRows = new List<PropertyEstateOwnershipRow>(ownershipCount);
        for (int i = 0; i < ownership.Count; i++)
        {
            PropertyOwnershipRecord row = ownership[i];
            if (row == null || row.PropertyId == null || row.OwnerPersonId == null)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "A Property ownership row has a missing identity.");
                return false;
            }
            ownershipRows.Add(new PropertyEstateOwnershipRow(row.PropertyId.Value, row.OwnerPersonId.Value));
        }

        List<PropertyEstateTransferHistoryRow> historyRows =
            new List<PropertyEstateTransferHistoryRow>(historyCount);
        for (int i = 0; i < history.Count; i++)
        {
            PropertyOwnershipTransferHistoryRecord row = history[i];
            if (row == null || row.PropertyId == null
                || row.PreviousOwnerPersonId == null || row.NewOwnerPersonId == null)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "A Property transfer-history row has a missing identity.");
                return false;
            }
            historyRows.Add(new PropertyEstateTransferHistoryRow(
                row.PropertyId.Value,
                row.PreviousOwnerPersonId.Value,
                row.NewOwnerPersonId.Value,
                row.TransferAbsoluteDay));
        }

        List<PropertyEstateEstateRow> estateRows = new List<PropertyEstateEstateRow>(estateCount);
        for (int i = 0; i < estates.Count; i++)
        {
            EstateRecord row = estates[i];
            if (row == null || row.EstateId == null || row.DeceasedPersonId == null)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "An Estate row has a missing identity.");
                return false;
            }
            estateRows.Add(new PropertyEstateEstateRow(
                row.EstateId.Value,
                row.DeceasedPersonId.Value,
                row.OpenedAbsoluteDay));
        }

        if (propertyOwner.Count != ownershipCount
            || propertyOwner.Revision != propertyRevision
            || propertyOwner.TransferHistory.Count != historyCount
            || estateOwner.Count != estateCount
            || estateOwner.Revision != estateRevision
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.SchemaVersion,
                propertyOwner,
                ownershipCount,
                propertyRevision)
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                PropertyOwnershipCensusProvider.TransferHistorySectionId,
                PropertyOwnershipCensusProvider.SchemaVersion,
                propertyOwner,
                historyCount,
                propertyRevision)
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector,
                EstateCensusProvider.SectionId,
                EstateCensusProvider.SchemaVersion,
                estateOwner,
                estateCount,
                estateRevision))
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "An owner changed while detached Property/Estate values were being copied.");
            return false;
        }

        snapshot = new PropertyEstateOwnerSnapshot(
            CurrentSchemaVersion, ownershipCount, propertyRevision, ownershipRows,
            CurrentSchemaVersion, historyCount, propertyRevision, historyRows,
            CurrentSchemaVersion, estateCount, estateRevision, estateRows);
        failure = PropertyEstateOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        PersonStore stagedPersonStore,
        long savedAbsoluteDay,
        out PropertyOwnershipStore stagedPropertyOwner,
        out EstateStore stagedEstateOwner,
        out PropertyEstateOwnerSnapshotFailure failure)
    {
        stagedPropertyOwner = null;
        stagedEstateOwner = null;
        failure = PropertyEstateOwnerSnapshotFailure.Create(
            PropertyEstateOwnerSnapshotFailureCode.StageFailed,
            "The Property/Estate owner pair could not be staged.");

        if (OwnershipSchemaVersion != CurrentSchemaVersion
            || TransferHistorySchemaVersion != CurrentSchemaVersion
            || EstateSchemaVersion != CurrentSchemaVersion)
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.UnsupportedSchema,
                "All three required Property/Estate sections must use schema v1.");
            return false;
        }
        if (stagedPersonStore == null || savedAbsoluteDay < 0L
            || OwnershipRecordCount < 0 || TransferHistoryRecordCount < 0 || EstateRecordCount < 0
            || PropertyRevision < 0L || TransferHistoryRevision < 0L || EstateRevision < 0L
            || OwnershipRows == null || TransferHistoryRows == null || EstateRows == null
            || OwnershipRecordCount != OwnershipRows.Count
            || TransferHistoryRecordCount != TransferHistoryRows.Count
            || EstateRecordCount != EstateRows.Count
            || PropertyRevision != TransferHistoryRevision
            || PropertyRevision != (long)OwnershipRecordCount + TransferHistoryRecordCount
            || EstateRevision != EstateRecordCount)
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.InvalidCardinality,
                "Section row counts and owner-local revisions are inconsistent.");
            return false;
        }

        List<PropertyOwnershipRecord> ownership = new List<PropertyOwnershipRecord>(OwnershipRows.Count);
        HashSet<string> propertyIds = new HashSet<string>(StringComparer.Ordinal);
        string previousPropertyId = null;
        foreach (PropertyEstateOwnershipRow row in OwnershipRows)
        {
            if (row == null || !HasValue(row.PropertyIdValue) || !HasValue(row.OwnerPersonIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "Every ownership row requires nonempty PropertyId and owner PersonId values.");
                return false;
            }
            if (!propertyIds.Add(row.PropertyIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.DuplicateIdentity,
                    "PropertyId values must be unique in the ownership section.");
                return false;
            }
            if (previousPropertyId != null && string.CompareOrdinal(previousPropertyId, row.PropertyIdValue) >= 0)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidOrdering,
                    "Ownership rows must be in strict ordinal PropertyId order.");
                return false;
            }
            previousPropertyId = row.PropertyIdValue;
            if (!stagedPersonStore.TryGet(new PersonId(row.OwnerPersonIdValue), out _))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidReference,
                    "Every current property owner must resolve to the staged P12-D Person root.");
                return false;
            }
            ownership.Add(new PropertyOwnershipRecord(new PropertyId(row.PropertyIdValue), new PersonId(row.OwnerPersonIdValue)));
        }

        List<PropertyEstateTransferHistoryRow> historyRows =
            new List<PropertyEstateTransferHistoryRow>(TransferHistoryRows);
        // Validate every row before comparing adjacent entries. The DTO is an
        // untrusted detached value and may contain null rows; ordering must
        // never dereference malformed input before returning a typed failure.
        foreach (PropertyEstateTransferHistoryRow row in historyRows)
        {
            if (row == null || !HasValue(row.PropertyIdValue)
                || !HasValue(row.PreviousOwnerPersonIdValue) || !HasValue(row.NewOwnerPersonIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "Every transfer-history row requires PropertyId and both PersonId values.");
                return false;
            }
        }
        for (int i = 1; i < historyRows.Count; i++)
        {
            if (CompareHistory(historyRows[i - 1], historyRows[i]) > 0)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidOrdering,
                    "Transfer-history rows must preserve deterministic owner ordering.");
                return false;
            }
        }

        List<PropertyOwnershipTransferHistoryRecord> history =
            new List<PropertyOwnershipTransferHistoryRecord>(historyRows.Count);
        foreach (PropertyEstateTransferHistoryRow row in historyRows)
        {
            if (row == null || !HasValue(row.PropertyIdValue)
                || !HasValue(row.PreviousOwnerPersonIdValue) || !HasValue(row.NewOwnerPersonIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "Every transfer-history row requires PropertyId and both PersonId values.");
                return false;
            }
            if (!propertyIds.Contains(row.PropertyIdValue)
                || !stagedPersonStore.TryGet(new PersonId(row.PreviousOwnerPersonIdValue), out _)
                || !stagedPersonStore.TryGet(new PersonId(row.NewOwnerPersonIdValue), out _))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidReference,
                    "Transfer history must reference a captured property and Persons in the same staged root.");
                return false;
            }
            if (row.TransferAbsoluteDay < 0L || row.TransferAbsoluteDay > savedAbsoluteDay)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidDay,
                    "Transfer days must be nonnegative and no later than the saved world day.");
                return false;
            }
            history.Add(new PropertyOwnershipTransferHistoryRecord(
                new PropertyId(row.PropertyIdValue),
                new PersonId(row.PreviousOwnerPersonIdValue),
                new PersonId(row.NewOwnerPersonIdValue),
                row.TransferAbsoluteDay));
        }

        List<EstateRecord> estates = new List<EstateRecord>(EstateRows.Count);
        HashSet<string> estateIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> deceasedPersonIds = new HashSet<string>(StringComparer.Ordinal);
        string previousEstateId = null;
        foreach (PropertyEstateEstateRow row in EstateRows)
        {
            if (row == null || !HasValue(row.EstateIdValue) || !HasValue(row.DeceasedPersonIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidIdentity,
                    "Every Estate row requires nonempty EstateId and deceased PersonId values.");
                return false;
            }
            if (!estateIds.Add(row.EstateIdValue) || !deceasedPersonIds.Add(row.DeceasedPersonIdValue))
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.DuplicateIdentity,
                    "EstateId and deceased PersonId values must each be unique.");
                return false;
            }
            if (previousEstateId != null && string.CompareOrdinal(previousEstateId, row.EstateIdValue) >= 0)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidOrdering,
                    "Estate rows must be in strict ordinal EstateId order.");
                return false;
            }
            previousEstateId = row.EstateIdValue;
            if (row.OpenedAbsoluteDay < 0L || row.OpenedAbsoluteDay > savedAbsoluteDay)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidDay,
                    "Estate opening days must be within the saved world timeline.");
                return false;
            }
            if (!stagedPersonStore.TryGet(new PersonId(row.DeceasedPersonIdValue), out PersonRuntime deceased)
                || !deceased.DeathAbsoluteDay.HasValue
                || row.OpenedAbsoluteDay < deceased.DeathAbsoluteDay.Value)
            {
                failure = PropertyEstateOwnerSnapshotFailure.Create(
                    PropertyEstateOwnerSnapshotFailureCode.InvalidReference,
                    "Each Estate must reference one staged deceased Person and open no earlier than death.");
                return false;
            }
            estates.Add(new EstateRecord(
                new EstateId(row.EstateIdValue),
                new PersonId(row.DeceasedPersonIdValue),
                row.OpenedAbsoluteDay));
        }

        if (!PropertyOwnershipStore.TryCreateFromOwnerSnapshot(
                stagedPersonStore, ownership, history, PropertyRevision, out PropertyOwnershipStore propertyCandidate)
            || !EstateStore.TryCreateFromOwnerSnapshot(
                stagedPersonStore, estates, EstateRevision, savedAbsoluteDay, out EstateStore estateCandidate))
        {
            failure = PropertyEstateOwnerSnapshotFailure.Create(
                PropertyEstateOwnerSnapshotFailureCode.StageFailed,
                "A private owner factory rejected the already-validated Property/Estate pair.");
            return false;
        }

        stagedPropertyOwner = propertyCandidate;
        stagedEstateOwner = estateCandidate;
        failure = PropertyEstateOwnerSnapshotFailure.None;
        return true;
    }

    private static bool MatchesUniqueRequiredSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            matches++;
            if (section.SchemaVersion != schemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != cardinality
                || section.Revision != revision)
                return false;
        }
        return matches == 1;
    }

    private static int CompareHistory(PropertyEstateTransferHistoryRow left, PropertyEstateTransferHistoryRow right)
    {
        int property = string.CompareOrdinal(left.PropertyIdValue, right.PropertyIdValue);
        if (property != 0) return property;
        int day = left.TransferAbsoluteDay.CompareTo(right.TransferAbsoluteDay);
        if (day != 0) return day;
        int previous = string.CompareOrdinal(left.PreviousOwnerPersonIdValue, right.PreviousOwnerPersonIdValue);
        return previous != 0 ? previous : string.CompareOrdinal(left.NewOwnerPersonIdValue, right.NewOwnerPersonIdValue);
    }

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows) where T : class
    {
        if (rows == null) return null;
        List<T> copy = new List<T>();
        foreach (T row in rows)
        {
            if (row == null) copy.Add(null);
            else if (row is PropertyEstateOwnershipRow ownership) copy.Add((T)(object)ownership.Copy());
            else if (row is PropertyEstateTransferHistoryRow history) copy.Add((T)(object)history.Copy());
            else if (row is PropertyEstateEstateRow estate) copy.Add((T)(object)estate.Copy());
            else copy.Add(row);
        }
        return new ReadOnlyCollection<T>(copy);
    }

    private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);
}

internal sealed class PropertyEstateOwnershipRow
{
    internal string PropertyIdValue { get; }
    internal string OwnerPersonIdValue { get; }
    internal PropertyEstateOwnershipRow(string propertyIdValue, string ownerPersonIdValue)
    { PropertyIdValue = propertyIdValue; OwnerPersonIdValue = ownerPersonIdValue; }
    internal PropertyEstateOwnershipRow Copy() => new PropertyEstateOwnershipRow(PropertyIdValue, OwnerPersonIdValue);
}

internal sealed class PropertyEstateTransferHistoryRow
{
    internal string PropertyIdValue { get; }
    internal string PreviousOwnerPersonIdValue { get; }
    internal string NewOwnerPersonIdValue { get; }
    internal long TransferAbsoluteDay { get; }
    internal PropertyEstateTransferHistoryRow(string propertyIdValue, string previousOwnerPersonIdValue,
        string newOwnerPersonIdValue, long transferAbsoluteDay)
    { PropertyIdValue = propertyIdValue; PreviousOwnerPersonIdValue = previousOwnerPersonIdValue;
      NewOwnerPersonIdValue = newOwnerPersonIdValue; TransferAbsoluteDay = transferAbsoluteDay; }
    internal PropertyEstateTransferHistoryRow Copy() => new PropertyEstateTransferHistoryRow(
        PropertyIdValue, PreviousOwnerPersonIdValue, NewOwnerPersonIdValue, TransferAbsoluteDay);
}

internal sealed class PropertyEstateEstateRow
{
    internal string EstateIdValue { get; }
    internal string DeceasedPersonIdValue { get; }
    internal long OpenedAbsoluteDay { get; }
    internal PropertyEstateEstateRow(string estateIdValue, string deceasedPersonIdValue, long openedAbsoluteDay)
    { EstateIdValue = estateIdValue; DeceasedPersonIdValue = deceasedPersonIdValue; OpenedAbsoluteDay = openedAbsoluteDay; }
    internal PropertyEstateEstateRow Copy() => new PropertyEstateEstateRow(
        EstateIdValue, DeceasedPersonIdValue, OpenedAbsoluteDay);
}
