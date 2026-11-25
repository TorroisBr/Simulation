using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>Explicit schema-v1 wire codec for the four existing Institution/Office owner sections.</summary>
internal static class P12EInstitutionOfficeOwnerCodec
{
    private const string PayloadContract = "p12a.institution-office/v1";
    private const string InstitutionPrefix = "institution/";
    private const string OfficePrefix = "office/";
    private const string IncumbencyPrefix = "incumbency/";
    private const string TenurePrefix = "tenure/";

    internal static bool TryEncode(P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12AOwnerSection institutionsSection, out P12AOwnerSection officesSection,
        out P12AOwnerSection incumbenciesSection, out P12AOwnerSection tenuresSection,
        out P12AResult result)
    {
        institutionsSection = null;
        officesSection = null;
        incumbenciesSection = null;
        tenuresSection = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            InstitutionOfficeCensusProvider.InstitutionsSectionId);
        if (snapshot?.Institutions == null || snapshot.Offices == null
            || snapshot.Incumbencies == null || snapshot.Tenures == null) return false;

        if (snapshot.Institutions.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot.Offices.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot.Incumbencies.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot.Tenures.RecordCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                InstitutionOfficeCensusProvider.InstitutionsSectionId);
            return false;
        }
        if (!snapshot.TryValidateLocal(out _)
            || HasInvalidDisplayName(snapshot.Institutions.Records)
            || HasInvalidDisplayName(snapshot.Offices.Records)) return false;

        try
        {
            List<P12AOwnerRow> institutionRows = EncodeInstitutions(snapshot.Institutions.Records);
            List<P12AOwnerRow> officeRows = EncodeOffices(snapshot.Offices.Records);
            List<P12AOwnerRow> incumbencyRows = EncodeIncumbencies(snapshot.Incumbencies.Records);
            List<P12AOwnerRow> tenureRows = EncodeTenures(snapshot.Tenures.Records);

            institutionsSection = new P12AOwnerSection(InstitutionOfficeCensusProvider.InstitutionsSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(InstitutionOfficeCensusProvider.InstitutionsSectionId),
                snapshot.Institutions.Revision, institutionRows);
            officesSection = new P12AOwnerSection(InstitutionOfficeCensusProvider.OfficesSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(InstitutionOfficeCensusProvider.OfficesSectionId),
                snapshot.Offices.Revision, officeRows);
            incumbenciesSection = new P12AOwnerSection(InstitutionOfficeCensusProvider.IncumbenciesSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(InstitutionOfficeCensusProvider.IncumbenciesSectionId),
                snapshot.Incumbencies.Revision, incumbencyRows);
            tenuresSection = new P12AOwnerSection(InstitutionOfficeCensusProvider.TenuresSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(InstitutionOfficeCensusProvider.TenuresSectionId),
                snapshot.Tenures.Revision, tenureRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            institutionsSection = null;
            officesSection = null;
            incumbenciesSection = null;
            tenuresSection = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                InstitutionOfficeCensusProvider.InstitutionsSectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection institutionsSection, P12AOwnerSection officesSection,
        P12AOwnerSection incumbenciesSection, P12AOwnerSection tenuresSection,
        out P12EInstitutionOfficeOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            InstitutionOfficeCensusProvider.InstitutionsSectionId);
        if (!ValidSection(institutionsSection, InstitutionOfficeCensusProvider.InstitutionsSectionId)
            || !ValidSection(officesSection, InstitutionOfficeCensusProvider.OfficesSectionId)
            || !ValidSection(incumbenciesSection, InstitutionOfficeCensusProvider.IncumbenciesSectionId)
            || !ValidSection(tenuresSection, InstitutionOfficeCensusProvider.TenuresSectionId)) return false;

        if (institutionsSection.Rows.Count > P12AContinuationCodec.MaxRows
            || officesSection.Rows.Count > P12AContinuationCodec.MaxRows
            || incumbenciesSection.Rows.Count > P12AContinuationCodec.MaxRows
            || tenuresSection.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                InstitutionOfficeCensusProvider.InstitutionsSectionId);
            return false;
        }

        try
        {
            if (!TryDecodeInstitutions(institutionsSection, out List<P12EInstitutionSnapshotRecord> institutions)
                || !TryDecodeOffices(officesSection, out List<P12EOfficeSnapshotRecord> offices)
                || !TryDecodeIncumbencies(incumbenciesSection, out List<P12EOfficeIncumbencySnapshotRecord> incumbencies)
                || !TryDecodeTenures(tenuresSection, out List<P12EOfficeTenureSnapshotRecord> tenures)) return false;

            P12EInstitutionOfficeOwnerSnapshot candidate = new P12EInstitutionOfficeOwnerSnapshot(
                Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, institutionsSection.Revision,
                    institutions, row => row?.Copy()),
                Section(InstitutionOfficeCensusProvider.OfficesSectionId, officesSection.Revision,
                    offices, row => row?.Copy()),
                Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, incumbenciesSection.Revision,
                    incumbencies, row => row?.Copy()),
                Section(InstitutionOfficeCensusProvider.TenuresSectionId, tenuresSection.Revision,
                    tenures, row => row?.Copy()));
            if (!candidate.TryValidateLocal(out _)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                InstitutionOfficeCensusProvider.InstitutionsSectionId);
            return false;
        }
    }

    private static List<P12AOwnerRow> EncodeInstitutions(IReadOnlyList<P12EInstitutionSnapshotRecord> records)
    {
        List<P12EInstitutionSnapshotRecord> ordered = new List<P12EInstitutionSnapshotRecord>(records);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.InstitutionIdValue, right.InstitutionIdValue));
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(ordered.Count);
        foreach (P12EInstitutionSnapshotRecord row in ordered)
            rows.Add(EncodeRow(InstitutionPrefix + row.InstitutionIdValue, writer =>
            {
                P12AWire.WriteString(writer, row.InstitutionIdValue);
                P12AWire.WriteString(writer, row.DisplayName);
            }, EstimateInstitutionLength(row)));
        return rows;
    }

    private static List<P12AOwnerRow> EncodeOffices(IReadOnlyList<P12EOfficeSnapshotRecord> records)
    {
        List<P12EOfficeSnapshotRecord> ordered = new List<P12EOfficeSnapshotRecord>(records);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.OfficeIdValue, right.OfficeIdValue));
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(ordered.Count);
        foreach (P12EOfficeSnapshotRecord row in ordered)
            rows.Add(EncodeRow(OfficePrefix + row.OfficeIdValue, writer =>
            {
                P12AWire.WriteString(writer, row.OfficeIdValue);
                P12AWire.WriteString(writer, row.InstitutionIdValue);
                P12AWire.WriteString(writer, row.DisplayName);
            }, EstimateOfficeLength(row)));
        return rows;
    }

    private static List<P12AOwnerRow> EncodeIncumbencies(
        IReadOnlyList<P12EOfficeIncumbencySnapshotRecord> records)
    {
        List<P12EOfficeIncumbencySnapshotRecord> ordered = new List<P12EOfficeIncumbencySnapshotRecord>(records);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.OfficeIdValue, right.OfficeIdValue));
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(ordered.Count);
        foreach (P12EOfficeIncumbencySnapshotRecord row in ordered)
            rows.Add(EncodeRow(IncumbencyPrefix + row.OfficeIdValue, writer =>
            {
                P12AWire.WriteString(writer, row.OfficeIdValue);
                P12AWire.WriteString(writer, row.PersonIdValue);
                WriteNullableDay(writer, row.StartAbsoluteDay);
            }, EstimateIncumbencyLength(row)));
        return rows;
    }

    private static List<P12AOwnerRow> EncodeTenures(IReadOnlyList<P12EOfficeTenureSnapshotRecord> records)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(records.Count);
        for (int index = 0; index < records.Count; index++)
        {
            P12EOfficeTenureSnapshotRecord row = records[index];
            string ownerKey = TenurePrefix + index.ToString("D10", CultureInfo.InvariantCulture);
            rows.Add(EncodeRow(ownerKey, writer =>
            {
                P12AWire.WriteString(writer, row.OfficeIdValue);
                P12AWire.WriteString(writer, row.PersonIdValue);
                WriteNullableDay(writer, row.StartAbsoluteDay);
                WriteNullableDay(writer, row.EndAbsoluteDay);
                WriteNullableInt32(writer, row.EndReason);
                writer.Write(row.IsClosed);
            }, EstimateTenureLength(row)));
        }
        return rows;
    }

    private static P12AOwnerRow EncodeRow(string ownerKey, Action<BinaryWriter> writeValues, long estimatedLength)
    {
        P12AWire.GetEncodedStringByteCount(ownerKey);
        EnsurePayloadBound(estimatedLength);
        using (MemoryStream stream = new MemoryStream((int)estimatedLength))
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writeValues(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(ownerKey, 0L, stream.ToArray());
        }
    }

    private static bool TryDecodeInstitutions(P12AOwnerSection section,
        out List<P12EInstitutionSnapshotRecord> records)
    {
        records = new List<P12EInstitutionSnapshotRecord>(section.Rows.Count);
        string previous = null;
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (!TryReadRow(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
            using (reader)
            using (stream)
            {
                if (!ReadContract(reader) || !ReadRequiredString(reader, out string institutionId)
                    || !P12AWire.ReadString(reader, out string displayName)
                    || !KeyMatchesAndAdvances(ownerKey, InstitutionPrefix, institutionId, ref previous)
                    || stream.Position != stream.Length || displayName == null) return false;
                records.Add(new P12EInstitutionSnapshotRecord(institutionId, displayName));
            }
        }
        return true;
    }

    private static bool TryDecodeOffices(P12AOwnerSection section, out List<P12EOfficeSnapshotRecord> records)
    {
        records = new List<P12EOfficeSnapshotRecord>(section.Rows.Count);
        string previous = null;
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (!TryReadRow(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
            using (reader)
            using (stream)
            {
                if (!ReadContract(reader) || !ReadRequiredString(reader, out string officeId)
                    || !ReadRequiredString(reader, out string institutionId)
                    || !P12AWire.ReadString(reader, out string displayName)
                    || !KeyMatchesAndAdvances(ownerKey, OfficePrefix, officeId, ref previous)
                    || stream.Position != stream.Length || displayName == null) return false;
                records.Add(new P12EOfficeSnapshotRecord(officeId, institutionId, displayName));
            }
        }
        return true;
    }

    private static bool TryDecodeIncumbencies(P12AOwnerSection section,
        out List<P12EOfficeIncumbencySnapshotRecord> records)
    {
        records = new List<P12EOfficeIncumbencySnapshotRecord>(section.Rows.Count);
        string previous = null;
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (!TryReadRow(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
            using (reader)
            using (stream)
            {
                if (!ReadContract(reader) || !ReadRequiredString(reader, out string officeId)
                    || !ReadRequiredString(reader, out string personId)
                    || !TryReadNullableDay(reader, out long? startDay)
                    || !KeyMatchesAndAdvances(ownerKey, IncumbencyPrefix, officeId, ref previous)
                    || stream.Position != stream.Length) return false;
                records.Add(new P12EOfficeIncumbencySnapshotRecord(officeId, personId, startDay));
            }
        }
        return true;
    }

    private static bool TryDecodeTenures(P12AOwnerSection section, out List<P12EOfficeTenureSnapshotRecord> records)
    {
        records = new List<P12EOfficeTenureSnapshotRecord>(section.Rows.Count);
        for (int index = 0; index < section.Rows.Count; index++)
        {
            P12AOwnerRow row = section.Rows[index];
            string expectedKey = TenurePrefix + index.ToString("D10", CultureInfo.InvariantCulture);
            if (!TryReadRow(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
            using (reader)
            using (stream)
            {
                if (!string.Equals(ownerKey, expectedKey, StringComparison.Ordinal)
                    || !ReadContract(reader) || !ReadRequiredString(reader, out string officeId)
                    || !ReadRequiredString(reader, out string personId)
                    || !TryReadNullableDay(reader, out long? startDay)
                    || !TryReadNullableDay(reader, out long? endDay)
                    || !TryReadNullableInt32(reader, out int? endReason)
                    || !TryReadBoolean(reader, out bool isClosed)
                    || stream.Position != stream.Length) return false;
                records.Add(new P12EOfficeTenureSnapshotRecord(
                    officeId, personId, startDay, endDay, endReason, isClosed));
            }
        }
        return true;
    }

    private static bool TryReadRow(P12AOwnerRow row, out string ownerKey,
        out BinaryReader reader, out MemoryStream stream)
    {
        ownerKey = null;
        reader = null;
        stream = null;
        if (row == null || row.Revision != 0L || string.IsNullOrEmpty(row.OwnerKey)
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        stream = new MemoryStream(payload, false);
        reader = new BinaryReader(stream, P12AWire.Utf8, true);
        ownerKey = row.OwnerKey;
        return true;
    }

    private static bool ValidSection(P12AOwnerSection section, string id)
        => section != null && string.Equals(section.Id, id, StringComparison.Ordinal)
            && section.Major == InstitutionOfficeCensusProvider.SchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && string.Equals(section.AliasGroup, P12ADailyV1Census.ExpectedAliasGroupForSection(id),
                StringComparison.Ordinal)
            && section.Revision >= 0L;

    private static bool ReadContract(BinaryReader reader)
        => ReadRequiredString(reader, out string contract)
            && string.Equals(contract, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool KeyMatchesAndAdvances(string key, string prefix, string value, ref string previous)
    {
        string expected = prefix + value;
        if (!string.Equals(key, expected, StringComparison.Ordinal)
            || (previous != null && StringComparer.Ordinal.Compare(previous, key) >= 0)) return false;
        previous = key;
        return true;
    }

    private static void WriteNullableDay(BinaryWriter writer, long? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool TryReadNullableDay(BinaryReader reader, out long? value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool hasValue)) return false;
        if (!hasValue) return true;
        value = reader.ReadInt64();
        return true;
    }

    private static void WriteNullableInt32(BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool TryReadNullableInt32(BinaryReader reader, out int? value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool hasValue)) return false;
        if (!hasValue) return true;
        value = reader.ReadInt32();
        return true;
    }

    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte raw = reader.ReadByte();
        if (raw > 1) return false;
        value = raw == 1;
        return true;
    }

    private static bool HasInvalidDisplayName<T>(IReadOnlyList<T> values)
    {
        foreach (T value in values)
        {
            if (value is P12EInstitutionSnapshotRecord institution && institution.DisplayName == null) return true;
            if (value is P12EOfficeSnapshotRecord office && office.DisplayName == null) return true;
        }
        return false;
    }

    private static P12EInstitutionOfficeSnapshotSection<T> Section<T>(string id, long revision,
        IReadOnlyList<T> records, Func<T, T> copy)
        => new P12EInstitutionOfficeSnapshotSection<T>(id, P12EInstitutionOfficeOwnerSnapshot.CurrentSchemaVersion,
            records.Count, revision, records, copy);

    private static long EstimateInstitutionLength(P12EInstitutionSnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.InstitutionIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.DisplayName);

    private static long EstimateOfficeLength(P12EOfficeSnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.OfficeIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.InstitutionIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.DisplayName);

    private static long EstimateIncumbencyLength(P12EOfficeIncumbencySnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.OfficeIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.PersonIdValue)
            + 1L + (row.StartAbsoluteDay.HasValue ? sizeof(long) : 0L);

    private static long EstimateTenureLength(P12EOfficeTenureSnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.OfficeIdValue)
            + P12AWire.GetEncodedStringFieldLength(row.PersonIdValue)
            + 1L + (row.StartAbsoluteDay.HasValue ? sizeof(long) : 0L)
            + 1L + (row.EndAbsoluteDay.HasValue ? sizeof(long) : 0L)
            + 1L + (row.EndReason.HasValue ? sizeof(int) : 0L) + 1L;

    private static void EnsurePayloadBound(long length)
    {
        if (length < 0L || length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
    }

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException
            || exception is IOException || exception is ArgumentException
            || exception is DecoderFallbackException || exception is OverflowException;
}
