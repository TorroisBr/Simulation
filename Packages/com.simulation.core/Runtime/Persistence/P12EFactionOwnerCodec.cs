using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for the two existing FactionStore owner sections.</summary>
internal static class P12EFactionOwnerCodec
{
    private const string PayloadContract = "p12a.faction/v1";
    private const string FactionPrefix = "faction/";
    private const string AffiliationPrefix = "affiliation/";

    internal static bool TryEncode(P12EFactionOwnerSnapshot snapshot, long capturedAbsoluteDay,
        out P12AOwnerSection factionsSection, out P12AOwnerSection affiliationsSection,
        out P12AResult result)
    {
        factionsSection = null;
        affiliationsSection = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, FactionStoreCensusProvider.FactionsSectionId);
        if (snapshot?.Factions?.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot?.Affiliations?.RecordCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded, FactionStoreCensusProvider.FactionsSectionId);
            return false;
        }
        try
        {
            if (!TryValidate(snapshot, capturedAbsoluteDay, requirePersonResolution: false,
                    requireCanonicalOrder: false, persons: null)) return false;

            List<P12EFactionSnapshotRecord> factions = new List<P12EFactionSnapshotRecord>(snapshot.Factions.Records);
            factions.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            List<P12AOwnerRow> factionRows = new List<P12AOwnerRow>(factions.Count);
            foreach (P12EFactionSnapshotRecord faction in factions)
                factionRows.Add(EncodeRow(FactionPrefix + faction.Id, writer =>
                {
                    P12AWire.WriteString(writer, faction.Id);
                    P12AWire.WriteString(writer, faction.DisplayName);
                    writer.Write(faction.CreatedAbsoluteDay);
                    writer.Write((ushort)faction.MembershipPolicy);
                    writer.Write(faction.ExpulsionAllowed ? (byte)1 : (byte)0);
                }, EstimateFactionPayloadLength(faction)));

            List<P12EFactionAffiliationSnapshotRecord> affiliations =
                new List<P12EFactionAffiliationSnapshotRecord>(snapshot.Affiliations.Records);
            affiliations.Sort((left, right) => StringComparer.Ordinal.Compare(left.AffiliationId, right.AffiliationId));
            List<P12AOwnerRow> affiliationRows = new List<P12AOwnerRow>(affiliations.Count);
            foreach (P12EFactionAffiliationSnapshotRecord affiliation in affiliations)
                affiliationRows.Add(EncodeRow(AffiliationPrefix + affiliation.AffiliationId, writer =>
                {
                    P12AWire.WriteString(writer, affiliation.AffiliationId);
                    P12AWire.WriteString(writer, affiliation.FactionId);
                    P12AWire.WriteString(writer, affiliation.PersonId);
                    writer.Write(affiliation.JoinedAbsoluteDay);
                    WriteNullableInt64(writer, affiliation.EndedAbsoluteDay);
                    WriteNullableInt32(writer, affiliation.EndReason);
                    writer.Write(affiliation.IsActive ? (byte)1 : (byte)0);
                }, EstimateAffiliationPayloadLength(affiliation)));

            factionsSection = new P12AOwnerSection(FactionStoreCensusProvider.FactionsSectionId,
                FactionStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(FactionStoreCensusProvider.FactionsSectionId),
                snapshot.Factions.Revision, factionRows);
            affiliationsSection = new P12AOwnerSection(FactionStoreCensusProvider.AffiliationsSectionId,
                FactionStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(FactionStoreCensusProvider.AffiliationsSectionId),
                snapshot.Affiliations.Revision, affiliationRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                FactionStoreCensusProvider.FactionsSectionId);
            factionsSection = null;
            affiliationsSection = null;
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection factionsSection, P12AOwnerSection affiliationsSection,
        long capturedAbsoluteDay, out P12EFactionOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, FactionStoreCensusProvider.FactionsSectionId);
        if (factionsSection?.Rows.Count > P12AContinuationCodec.MaxRows
            || affiliationsSection?.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded, FactionStoreCensusProvider.FactionsSectionId);
            return false;
        }
        if (capturedAbsoluteDay < 0L
            || !ValidSection(factionsSection, FactionStoreCensusProvider.FactionsSectionId)
            || !ValidSection(affiliationsSection, FactionStoreCensusProvider.AffiliationsSectionId)
            || factionsSection.Revision != affiliationsSection.Revision
            || !RevisionCanRepresentRows(factionsSection.Revision,
                factionsSection.Rows.Count, affiliationsSection.Rows.Count)) return false;

        try
        {
            List<P12EFactionSnapshotRecord> factions = new List<P12EFactionSnapshotRecord>(factionsSection.Rows.Count);
            string previousFactionId = null;
            foreach (P12AOwnerRow row in factionsSection.Rows)
            {
                if (!ValidRow(row) || !HasOwnerKeyValue(row.OwnerKey, FactionPrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadContract(reader) || !ReadRequiredString(reader, out string factionId)
                        || !P12AWire.ReadString(reader, out string displayName) || displayName == null) return false;
                    long createdDay = reader.ReadInt64();
                    FactionMembershipPolicy policy = (FactionMembershipPolicy)reader.ReadUInt16();
                    if (!TryReadBoolean(reader, out bool expulsionAllowed)
                        || !string.Equals(row.OwnerKey, FactionPrefix + factionId, StringComparison.Ordinal)
                        || stream.Position != stream.Length
                        || (previousFactionId != null && StringComparer.Ordinal.Compare(previousFactionId, factionId) >= 0))
                        return false;
                    factions.Add(new P12EFactionSnapshotRecord(factionId, displayName,
                        createdDay, (int)policy, expulsionAllowed));
                    previousFactionId = factionId;
                }
            }

            List<P12EFactionAffiliationSnapshotRecord> affiliations =
                new List<P12EFactionAffiliationSnapshotRecord>(affiliationsSection.Rows.Count);
            string previousAffiliationId = null;
            foreach (P12AOwnerRow row in affiliationsSection.Rows)
            {
                if (!ValidRow(row) || !HasOwnerKeyValue(row.OwnerKey, AffiliationPrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadContract(reader) || !ReadRequiredString(reader, out string affiliationId)
                        || !ReadRequiredString(reader, out string factionId)
                        || !ReadRequiredString(reader, out string personId)) return false;
                    long joinedDay = reader.ReadInt64();
                    if (!TryReadNullableInt64(reader, out long? endedDay)
                        || !TryReadNullableInt32(reader, out int? endReason)
                        || !TryReadBoolean(reader, out bool isActive)
                        || !string.Equals(row.OwnerKey, AffiliationPrefix + affiliationId, StringComparison.Ordinal)
                        || stream.Position != stream.Length
                        || (previousAffiliationId != null
                            && StringComparer.Ordinal.Compare(previousAffiliationId, affiliationId) >= 0)) return false;
                    affiliations.Add(new P12EFactionAffiliationSnapshotRecord(affiliationId, factionId,
                        personId, joinedDay, endedDay, endReason, isActive));
                    previousAffiliationId = affiliationId;
                }
            }

            P12EFactionOwnerSnapshot candidate = new P12EFactionOwnerSnapshot(
                new P12EFactionSnapshotSection<P12EFactionSnapshotRecord>(
                    FactionStoreCensusProvider.FactionsSectionId, P12EFactionOwnerSnapshot.CurrentSchemaVersion,
                    factions.Count, factionsSection.Revision, factions, row => row?.Copy()),
                new P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord>(
                    FactionStoreCensusProvider.AffiliationsSectionId, P12EFactionOwnerSnapshot.CurrentSchemaVersion,
                    affiliations.Count, affiliationsSection.Revision, affiliations, row => row?.Copy()));
            if (!TryValidate(candidate, capturedAbsoluteDay, requirePersonResolution: false,
                    requireCanonicalOrder: true, persons: null)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                FactionStoreCensusProvider.FactionsSectionId);
            return false;
        }
    }

    private static bool TryValidate(P12EFactionOwnerSnapshot snapshot, long currentDay,
        bool requirePersonResolution, bool requireCanonicalOrder, PersonStore persons)
    {
        if (snapshot == null || currentDay < 0L || (requirePersonResolution && persons == null)
            || !ValidSnapshotSection(snapshot.Factions, FactionStoreCensusProvider.FactionsSectionId)
            || !ValidSnapshotSection(snapshot.Affiliations, FactionStoreCensusProvider.AffiliationsSectionId)
            || snapshot.Factions.Revision < 0L || snapshot.Affiliations.Revision != snapshot.Factions.Revision
            || snapshot.Factions.RecordCount < 0 || snapshot.Affiliations.RecordCount < 0
            || snapshot.Factions.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot.Affiliations.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot.Factions.Records == null || snapshot.Affiliations.Records == null
            || snapshot.Factions.RecordCount != snapshot.Factions.Records.Count
            || snapshot.Affiliations.RecordCount != snapshot.Affiliations.Records.Count
            || !RevisionCanRepresentRows(snapshot.Factions.Revision,
                snapshot.Factions.RecordCount, snapshot.Affiliations.RecordCount)) return false;

        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        string previousFactionId = null;
        foreach (P12EFactionSnapshotRecord row in snapshot.Factions.Records)
        {
            if (row == null || !HasValue(row.Id) || row.DisplayName == null
                || !Enum.IsDefined(typeof(FactionMembershipPolicy), row.MembershipPolicy)
                || row.CreatedAbsoluteDay < 0L || row.CreatedAbsoluteDay > currentDay
                || !factionIds.Add(row.Id)
                || (requireCanonicalOrder && previousFactionId != null
                    && StringComparer.Ordinal.Compare(previousFactionId, row.Id) >= 0))
                return false;
            EnsurePayloadBound(EstimateFactionPayloadLength(row));
            previousFactionId = row.Id;
        }

        HashSet<string> affiliationIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> activeKeys = new HashSet<string>(StringComparer.Ordinal);
        string previousAffiliationId = null;
        foreach (P12EFactionAffiliationSnapshotRecord row in snapshot.Affiliations.Records)
        {
            if (row == null || !HasValue(row.AffiliationId) || !HasValue(row.FactionId) || !HasValue(row.PersonId)
                || !affiliationIds.Add(row.AffiliationId) || !factionIds.Contains(row.FactionId)
                || (requirePersonResolution && !persons.TryGet(new PersonId(row.PersonId), out _))
                || row.JoinedAbsoluteDay < 0L || row.JoinedAbsoluteDay > currentDay
                || (row.EndedAbsoluteDay.HasValue
                    && (row.EndedAbsoluteDay.Value < row.JoinedAbsoluteDay || row.EndedAbsoluteDay.Value > currentDay))
                || (row.EndReason.HasValue && (!row.EndedAbsoluteDay.HasValue
                    || !Enum.IsDefined(typeof(FactionAffiliationEndReason), row.EndReason.Value)))
                || row.IsActive != !row.EndedAbsoluteDay.HasValue
                || (requireCanonicalOrder && previousAffiliationId != null
                    && StringComparer.Ordinal.Compare(previousAffiliationId, row.AffiliationId) >= 0)) return false;
            P12EFactionSnapshotRecord faction = null;
            foreach (P12EFactionSnapshotRecord candidate in snapshot.Factions.Records)
                if (string.Equals(candidate.Id, row.FactionId, StringComparison.Ordinal))
                { faction = candidate; break; }
            if (faction == null || row.JoinedAbsoluteDay < faction.CreatedAbsoluteDay) return false;
            if (row.IsActive && !activeKeys.Add(row.FactionId + "\u001f" + row.PersonId)) return false;
            EnsurePayloadBound(EstimateAffiliationPayloadLength(row));
            previousAffiliationId = row.AffiliationId;
        }
        return true;
    }

    private static P12AOwnerRow EncodeRow(string ownerKey, Action<BinaryWriter> payloadWriter, long estimatedLength)
    {
        P12AWire.GetEncodedStringByteCount(ownerKey);
        EnsurePayloadBound(estimatedLength);
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            payloadWriter(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(ownerKey, 0L, stream.ToArray());
        }
    }

    private static bool ValidSection(P12AOwnerSection section, string id)
        => section != null && string.Equals(section.Id, id, StringComparison.Ordinal)
            && section.Major == FactionStoreCensusProvider.SchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && string.Equals(section.AliasGroup, P12ADailyV1Census.ExpectedAliasGroupForSection(id),
                StringComparison.Ordinal)
            && section.Revision >= 0L;

    private static bool ValidSnapshotSection<T>(P12EFactionSnapshotSection<T> section, string id)
        => section != null && section.SchemaVersion == P12EFactionOwnerSnapshot.CurrentSchemaVersion
            && string.Equals(section.SectionId, id, StringComparison.Ordinal);

    private static bool ValidRow(P12AOwnerRow row)
        => row != null && row.Revision == 0L && row.PayloadLength >= 0
            && row.PayloadLength <= P12AContinuationCodec.MaxSectionBytes;

    private static bool RevisionCanRepresentRows(long revision, int factionCount, int affiliationCount)
    {
        long retained = (long)factionCount + affiliationCount;
        return retained == 0L ? revision == 0L : retained <= revision;
    }

    private static bool ReadContract(BinaryReader reader)
        => P12AWire.ReadString(reader, out string contract)
            && string.Equals(contract, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte raw = reader.ReadByte();
        if (raw > 1) return false;
        value = raw == 1;
        return true;
    }

    private static void WriteNullableInt64(BinaryWriter writer, long? value)
    {
        writer.Write(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool TryReadNullableInt64(BinaryReader reader, out long? value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool hasValue)) return false;
        if (hasValue) value = reader.ReadInt64();
        return true;
    }

    private static void WriteNullableInt32(BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool TryReadNullableInt32(BinaryReader reader, out int? value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool hasValue)) return false;
        if (hasValue) value = reader.ReadInt32();
        return true;
    }

    private static bool HasOwnerKeyValue(string ownerKey, string prefix)
        => !string.IsNullOrWhiteSpace(ownerKey) && ownerKey.StartsWith(prefix, StringComparison.Ordinal)
            && ownerKey.Length > prefix.Length;

    private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);

    private static long EstimateFactionPayloadLength(P12EFactionSnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.Id)
            + P12AWire.GetEncodedStringFieldLength(row.DisplayName) + sizeof(long) + sizeof(ushort) + sizeof(byte);

    private static long EstimateAffiliationPayloadLength(P12EFactionAffiliationSnapshotRecord row)
        => P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(row.AffiliationId)
            + P12AWire.GetEncodedStringFieldLength(row.FactionId)
            + P12AWire.GetEncodedStringFieldLength(row.PersonId) + sizeof(long)
            + sizeof(byte) + (row.EndedAbsoluteDay.HasValue ? sizeof(long) : 0L)
            + sizeof(byte) + (row.EndReason.HasValue ? sizeof(int) : 0L) + sizeof(byte);

    private static void EnsurePayloadBound(long length)
    {
        if (length < 0L || length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
    }

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException;
}
