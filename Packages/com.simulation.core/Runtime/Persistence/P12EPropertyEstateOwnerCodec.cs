using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codec for the existing Property and Estate owner sections.</summary>
internal static class P12EPropertyEstateOwnerCodec
{
    private const string PayloadContract = "p12a.property-estate/v1";
    private const string OwnershipPrefix = "property/";
    private const string TransferPrefix = "transfer/";
    private const string EstatePrefix = "estate/";

    internal static bool TryEncode(PropertyEstateOwnerSnapshot snapshot, long capturedAbsoluteDay,
        out P12AOwnerSection ownershipSection, out P12AOwnerSection transferHistorySection,
        out P12AOwnerSection estateSection, out P12AResult result)
    {
        ownershipSection = null;
        transferHistorySection = null;
        estateSection = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PropertyOwnershipCensusProvider.OwnershipSectionId);
        if (snapshot == null || capturedAbsoluteDay < 0L
            || snapshot.OwnershipRecordCount > P12AContinuationCodec.MaxRows
            || snapshot.TransferHistoryRecordCount > P12AContinuationCodec.MaxRows
            || snapshot.EstateRecordCount > P12AContinuationCodec.MaxRows)
        {
            if (snapshot != null && (snapshot.OwnershipRecordCount > P12AContinuationCodec.MaxRows
                || snapshot.TransferHistoryRecordCount > P12AContinuationCodec.MaxRows
                || snapshot.EstateRecordCount > P12AContinuationCodec.MaxRows))
                result = new P12AResult(P12AResultCode.LimitExceeded,
                    PropertyOwnershipCensusProvider.OwnershipSectionId);
            return false;
        }

        try
        {
            if (!TryValidateValues(snapshot, capturedAbsoluteDay, requireCanonicalOrder: false)) return false;

            List<PropertyEstateOwnershipRow> ownership = new List<PropertyEstateOwnershipRow>(snapshot.OwnershipRows);
            ownership.Sort((left, right) => StringComparer.Ordinal.Compare(left.PropertyIdValue, right.PropertyIdValue));
            List<P12AOwnerRow> ownershipRows = new List<P12AOwnerRow>(ownership.Count);
            foreach (PropertyEstateOwnershipRow row in ownership)
                ownershipRows.Add(EncodeRow(OwnershipPrefix + row.PropertyIdValue, writer =>
                {
                    P12AWire.WriteString(writer, row.PropertyIdValue);
                    P12AWire.WriteString(writer, row.OwnerPersonIdValue);
                }, EstimateOwnershipPayloadLength(row)));

            List<PropertyEstateTransferHistoryRow> transferHistory =
                new List<PropertyEstateTransferHistoryRow>(snapshot.TransferHistoryRows);
            transferHistory.Sort(CompareTransferHistory);
            List<P12AOwnerRow> transferHistoryRows = new List<P12AOwnerRow>(transferHistory.Count);
            PropertyEstateTransferHistoryRow priorHistory = null;
            int duplicateOccurrence = 0;
            for (int index = 0; index < transferHistory.Count; index++)
            {
                PropertyEstateTransferHistoryRow row = transferHistory[index];
                duplicateOccurrence = priorHistory != null && CompareTransferHistory(priorHistory, row) == 0
                    ? checked(duplicateOccurrence + 1) : 0;
                string ownerKey = TransferOwnerKey(row, duplicateOccurrence);
                transferHistoryRows.Add(EncodeRow(ownerKey, writer =>
                {
                    P12AWire.WriteString(writer, row.PropertyIdValue);
                    P12AWire.WriteString(writer, row.PreviousOwnerPersonIdValue);
                    P12AWire.WriteString(writer, row.NewOwnerPersonIdValue);
                    writer.Write(row.TransferAbsoluteDay);
                }, EstimateTransferPayloadLength(row)));
                priorHistory = row;
            }

            List<PropertyEstateEstateRow> estates = new List<PropertyEstateEstateRow>(snapshot.EstateRows);
            estates.Sort((left, right) => StringComparer.Ordinal.Compare(left.EstateIdValue, right.EstateIdValue));
            List<P12AOwnerRow> estateRows = new List<P12AOwnerRow>(estates.Count);
            foreach (PropertyEstateEstateRow row in estates)
                estateRows.Add(EncodeRow(EstatePrefix + row.EstateIdValue, writer =>
                {
                    P12AWire.WriteString(writer, row.EstateIdValue);
                    P12AWire.WriteString(writer, row.DeceasedPersonIdValue);
                    writer.Write(row.OpenedAbsoluteDay);
                }, EstimateEstatePayloadLength(row)));

            ownershipSection = new P12AOwnerSection(PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(PropertyOwnershipCensusProvider.OwnershipSectionId),
                snapshot.PropertyRevision, ownershipRows);
            transferHistorySection = new P12AOwnerSection(PropertyOwnershipCensusProvider.TransferHistorySectionId,
                PropertyOwnershipCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(PropertyOwnershipCensusProvider.TransferHistorySectionId),
                snapshot.PropertyRevision, transferHistoryRows);
            estateSection = new P12AOwnerSection(EstateCensusProvider.SectionId,
                EstateCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(EstateCensusProvider.SectionId),
                snapshot.EstateRevision, estateRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PropertyOwnershipCensusProvider.OwnershipSectionId);
            ownershipSection = null;
            transferHistorySection = null;
            estateSection = null;
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection ownershipSection,
        P12AOwnerSection transferHistorySection, P12AOwnerSection estateSection,
        long capturedAbsoluteDay, out PropertyEstateOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PropertyOwnershipCensusProvider.OwnershipSectionId);
        if (ownershipSection?.Rows.Count > P12AContinuationCodec.MaxRows
            || transferHistorySection?.Rows.Count > P12AContinuationCodec.MaxRows
            || estateSection?.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                PropertyOwnershipCensusProvider.OwnershipSectionId);
            return false;
        }
        if (capturedAbsoluteDay < 0L
            || !ValidSection(ownershipSection, PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.SchemaVersion)
            || !ValidSection(transferHistorySection, PropertyOwnershipCensusProvider.TransferHistorySectionId,
                PropertyOwnershipCensusProvider.SchemaVersion)
            || !ValidSection(estateSection, EstateCensusProvider.SectionId, EstateCensusProvider.SchemaVersion)
            || ownershipSection.Revision != transferHistorySection.Revision
            || !RevisionMatchesRows(ownershipSection.Revision,
                ownershipSection.Rows.Count, transferHistorySection.Rows.Count)
            || !RevisionMatchesRows(estateSection.Revision, estateSection.Rows.Count)) return false;

        try
        {
            List<PropertyEstateOwnershipRow> ownership = new List<PropertyEstateOwnershipRow>(ownershipSection.Rows.Count);
            foreach (P12AOwnerRow row in ownershipSection.Rows)
            {
                if (!ValidRow(row) || !HasOwnerKeyValue(row.OwnerKey, OwnershipPrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadContract(reader) || !ReadRequiredString(reader, out string propertyId)
                        || !ReadRequiredString(reader, out string ownerPersonId)
                        || !string.Equals(row.OwnerKey, OwnershipPrefix + propertyId, StringComparison.Ordinal)
                        || stream.Position != stream.Length) return false;
                    ownership.Add(new PropertyEstateOwnershipRow(propertyId, ownerPersonId));
                }
            }

            List<PropertyEstateTransferHistoryRow> history =
                new List<PropertyEstateTransferHistoryRow>(transferHistorySection.Rows.Count);
            Dictionary<string, int> duplicateOccurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < transferHistorySection.Rows.Count; index++)
            {
                P12AOwnerRow row = transferHistorySection.Rows[index];
                if (!ValidRow(row)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadContract(reader) || !ReadRequiredString(reader, out string propertyId)
                        || !ReadRequiredString(reader, out string previousOwnerPersonId)
                        || !ReadRequiredString(reader, out string newOwnerPersonId)) return false;
                    long transferAbsoluteDay = reader.ReadInt64();
                    if (stream.Position != stream.Length) return false;
                    PropertyEstateTransferHistoryRow decoded = new PropertyEstateTransferHistoryRow(propertyId,
                        previousOwnerPersonId, newOwnerPersonId, transferAbsoluteDay);
                    string duplicateIdentity = TransferDuplicateIdentity(decoded);
                    duplicateOccurrences.TryGetValue(duplicateIdentity, out int occurrence);
                    if (!string.Equals(row.OwnerKey, TransferOwnerKey(decoded, occurrence), StringComparison.Ordinal))
                        return false;
                    duplicateOccurrences[duplicateIdentity] = checked(occurrence + 1);
                    history.Add(decoded);
                }
            }

            List<PropertyEstateEstateRow> estates = new List<PropertyEstateEstateRow>(estateSection.Rows.Count);
            foreach (P12AOwnerRow row in estateSection.Rows)
            {
                if (!ValidRow(row) || !HasOwnerKeyValue(row.OwnerKey, EstatePrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadContract(reader) || !ReadRequiredString(reader, out string estateId)
                        || !ReadRequiredString(reader, out string deceasedPersonId)) return false;
                    long openedAbsoluteDay = reader.ReadInt64();
                    if (!string.Equals(row.OwnerKey, EstatePrefix + estateId, StringComparison.Ordinal)
                        || stream.Position != stream.Length) return false;
                    estates.Add(new PropertyEstateEstateRow(estateId, deceasedPersonId, openedAbsoluteDay));
                }
            }

            PropertyEstateOwnerSnapshot candidate = new PropertyEstateOwnerSnapshot(
                PropertyEstateOwnerSnapshot.CurrentSchemaVersion, ownership.Count, ownershipSection.Revision, ownership,
                PropertyEstateOwnerSnapshot.CurrentSchemaVersion, history.Count, transferHistorySection.Revision, history,
                PropertyEstateOwnerSnapshot.CurrentSchemaVersion, estates.Count, estateSection.Revision, estates);
            if (!TryValidateValues(candidate, capturedAbsoluteDay, requireCanonicalOrder: true)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PropertyOwnershipCensusProvider.OwnershipSectionId);
            return false;
        }
    }

    private static bool TryValidateValues(PropertyEstateOwnerSnapshot snapshot, long capturedAbsoluteDay,
        bool requireCanonicalOrder)
    {
        if (snapshot == null || capturedAbsoluteDay < 0L
            || snapshot.OwnershipSchemaVersion != PropertyEstateOwnerSnapshot.CurrentSchemaVersion
            || snapshot.TransferHistorySchemaVersion != PropertyEstateOwnerSnapshot.CurrentSchemaVersion
            || snapshot.EstateSchemaVersion != PropertyEstateOwnerSnapshot.CurrentSchemaVersion
            || snapshot.OwnershipRecordCount < 0 || snapshot.TransferHistoryRecordCount < 0
            || snapshot.EstateRecordCount < 0
            || snapshot.OwnershipRecordCount > P12AContinuationCodec.MaxRows
            || snapshot.TransferHistoryRecordCount > P12AContinuationCodec.MaxRows
            || snapshot.EstateRecordCount > P12AContinuationCodec.MaxRows
            || snapshot.PropertyRevision < 0L || snapshot.PropertyRevision != snapshot.TransferHistoryRevision
            || snapshot.EstateRevision < 0L || snapshot.OwnershipRows == null
            || snapshot.TransferHistoryRows == null || snapshot.EstateRows == null
            || snapshot.OwnershipRows.Count != snapshot.OwnershipRecordCount
            || snapshot.TransferHistoryRows.Count != snapshot.TransferHistoryRecordCount
            || snapshot.EstateRows.Count != snapshot.EstateRecordCount
            || !RevisionMatchesRows(snapshot.PropertyRevision,
                snapshot.OwnershipRecordCount, snapshot.TransferHistoryRecordCount)
            || !RevisionMatchesRows(snapshot.EstateRevision, snapshot.EstateRecordCount)) return false;

        HashSet<string> propertyIds = new HashSet<string>(StringComparer.Ordinal);
        string previousPropertyId = null;
        foreach (PropertyEstateOwnershipRow row in snapshot.OwnershipRows)
        {
            if (row == null || !HasValue(row.PropertyIdValue) || !HasValue(row.OwnerPersonIdValue)
                || !propertyIds.Add(row.PropertyIdValue)) return false;
            EnsurePayloadBound(EstimateOwnershipPayloadLength(row));
            if (requireCanonicalOrder && previousPropertyId != null
                && StringComparer.Ordinal.Compare(previousPropertyId, row.PropertyIdValue) >= 0) return false;
            previousPropertyId = row.PropertyIdValue;
        }

        PropertyEstateTransferHistoryRow previousHistory = null;
        foreach (PropertyEstateTransferHistoryRow row in snapshot.TransferHistoryRows)
        {
            if (row == null || !HasValue(row.PropertyIdValue)
                || !HasValue(row.PreviousOwnerPersonIdValue) || !HasValue(row.NewOwnerPersonIdValue)
                || !propertyIds.Contains(row.PropertyIdValue)
                || row.TransferAbsoluteDay < 0L || row.TransferAbsoluteDay > capturedAbsoluteDay) return false;
            EnsurePayloadBound(EstimateTransferPayloadLength(row));
            if (requireCanonicalOrder && previousHistory != null
                && CompareTransferHistory(previousHistory, row) > 0) return false;
            previousHistory = row;
        }

        HashSet<string> estateIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> deceasedPersonIds = new HashSet<string>(StringComparer.Ordinal);
        string previousEstateId = null;
        foreach (PropertyEstateEstateRow row in snapshot.EstateRows)
        {
            if (row == null || !HasValue(row.EstateIdValue) || !HasValue(row.DeceasedPersonIdValue)
                || !estateIds.Add(row.EstateIdValue) || !deceasedPersonIds.Add(row.DeceasedPersonIdValue)
                || row.OpenedAbsoluteDay < 0L || row.OpenedAbsoluteDay > capturedAbsoluteDay) return false;
            EnsurePayloadBound(EstimateEstatePayloadLength(row));
            if (requireCanonicalOrder && previousEstateId != null
                && StringComparer.Ordinal.Compare(previousEstateId, row.EstateIdValue) >= 0) return false;
            previousEstateId = row.EstateIdValue;
        }
        return true;
    }

    private static P12AOwnerRow EncodeRow(string ownerKey, Action<BinaryWriter> writePayload, long estimatedLength)
    {
        P12AWire.GetEncodedStringByteCount(ownerKey);
        EnsurePayloadBound(estimatedLength);
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writePayload(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(ownerKey, 0L, stream.ToArray());
        }
    }

    private static bool ValidSection(P12AOwnerSection section, string sectionId, int schemaVersion)
        => section != null && string.Equals(section.Id, sectionId, StringComparison.Ordinal)
            && section.Major == schemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && string.Equals(section.AliasGroup, P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId),
                StringComparison.Ordinal)
            && section.Revision >= 0L;

    private static bool ValidRow(P12AOwnerRow row)
        => row != null && row.Revision == 0L && row.PayloadLength >= 0
            && row.PayloadLength <= P12AContinuationCodec.MaxSectionBytes;

    private static bool RevisionMatchesRows(long revision, int firstCount, int secondCount = 0)
        => revision == (long)firstCount + secondCount;

    private static bool ReadContract(BinaryReader reader)
        => ReadRequiredString(reader, out string contract) && string.Equals(contract, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool HasOwnerKeyValue(string ownerKey, string prefix)
        => !string.IsNullOrWhiteSpace(ownerKey) && ownerKey.StartsWith(prefix, StringComparison.Ordinal)
            && ownerKey.Length > prefix.Length;

    private static string TransferOwnerKey(PropertyEstateTransferHistoryRow row, int duplicateOccurrence)
        => TransferPrefix + row.PropertyIdValue.Length.ToString(CultureInfo.InvariantCulture) + ":"
            + row.PropertyIdValue + "/" + row.TransferAbsoluteDay.ToString(CultureInfo.InvariantCulture) + "/"
            + row.PreviousOwnerPersonIdValue.Length.ToString(CultureInfo.InvariantCulture) + ":"
            + row.PreviousOwnerPersonIdValue + "/" + row.NewOwnerPersonIdValue.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + row.NewOwnerPersonIdValue + "/" + duplicateOccurrence.ToString("D10", CultureInfo.InvariantCulture);

    private static string TransferDuplicateIdentity(PropertyEstateTransferHistoryRow row)
        => row.PropertyIdValue.Length.ToString(CultureInfo.InvariantCulture) + ":" + row.PropertyIdValue + "/"
            + row.TransferAbsoluteDay.ToString(CultureInfo.InvariantCulture) + "/"
            + row.PreviousOwnerPersonIdValue.Length.ToString(CultureInfo.InvariantCulture) + ":"
            + row.PreviousOwnerPersonIdValue + "/" + row.NewOwnerPersonIdValue.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + row.NewOwnerPersonIdValue;

    private static int CompareTransferHistory(PropertyEstateTransferHistoryRow left,
        PropertyEstateTransferHistoryRow right)
    {
        int property = StringComparer.Ordinal.Compare(left.PropertyIdValue, right.PropertyIdValue);
        if (property != 0) return property;
        int day = left.TransferAbsoluteDay.CompareTo(right.TransferAbsoluteDay);
        if (day != 0) return day;
        int previous = StringComparer.Ordinal.Compare(left.PreviousOwnerPersonIdValue, right.PreviousOwnerPersonIdValue);
        return previous != 0 ? previous
            : StringComparer.Ordinal.Compare(left.NewOwnerPersonIdValue, right.NewOwnerPersonIdValue);
    }

    private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);

    private static long EstimateOwnershipPayloadLength(PropertyEstateOwnershipRow row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.PropertyIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.OwnerPersonIdValue);

    private static long EstimateTransferPayloadLength(PropertyEstateTransferHistoryRow row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.PropertyIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.PreviousOwnerPersonIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.NewOwnerPersonIdValue) + sizeof(long);

    private static long EstimateEstatePayloadLength(PropertyEstateEstateRow row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.EstateIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.DeceasedPersonIdValue) + sizeof(long);

    private static void EnsurePayloadBound(long length)
    {
        if (length < 0L || length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
    }

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException;
}
