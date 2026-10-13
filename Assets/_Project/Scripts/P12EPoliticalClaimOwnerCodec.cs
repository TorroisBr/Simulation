using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for the two existing PoliticalClaimStore sections.</summary>
internal static class P12EPoliticalClaimOwnerCodec
{
    private const string PayloadContract = "p12a.political-claims/v1";
    private const string ClaimOwnerPrefix = "claim/";
    private const string RecognitionOwnerPrefix = "recognition/";

    internal static bool TryEncode(P12EPoliticalClaimOwnerSnapshot snapshot, long capturedAbsoluteDay,
        out P12AOwnerSection claimsSection, out P12AOwnerSection recognitionsSection,
        out P12AResult result)
    {
        claimsSection = null;
        recognitionsSection = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalClaimStoreCensusProvider.ClaimsSectionId);
        if (snapshot?.Claims?.RecordCount > P12AContinuationCodec.MaxRows
            || snapshot?.Recognitions?.RecordCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                PoliticalClaimStoreCensusProvider.ClaimsSectionId);
            return false;
        }
        try
        {
            if (!TryValidateValues(snapshot, capturedAbsoluteDay)) return false;
            List<P12AOwnerRow> claimRows = new List<P12AOwnerRow>(snapshot.Claims.RecordCount);
            foreach (P12EPoliticalClaimRow claim in snapshot.Claims.Records)
            {
                EnsurePayloadBound(EstimateClaimPayloadLength(claim));
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    P12AWire.WriteString(writer, claim.ClaimId);
                    P12AWire.WriteString(writer, claim.ClaimantPersonId);
                    writer.Write((ushort)claim.ClaimType);
                    writer.Write((ushort)claim.TargetKind);
                    P12AWire.WriteString(writer, claim.TargetId);
                    writer.Write((ushort)claim.Basis);
                    P12AWire.WriteString(writer, claim.BasisDescription);
                    writer.Write(claim.CreatedAbsoluteDay);
                    writer.Write((ushort)claim.Status);
                    WriteNullableInt64(writer, claim.ResolutionAbsoluteDay);
                    WriteStrings(writer, claim.EvidenceReferences);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                            ClaimOwnerPrefix + claim.ClaimId);
                        return false;
                    }
                    claimRows.Add(CreateOwnerRow(ClaimOwnerPrefix + claim.ClaimId, stream.ToArray()));
                }
            }

            List<P12AOwnerRow> recognitionRows = new List<P12AOwnerRow>(snapshot.Recognitions.RecordCount);
            foreach (P12EPoliticalClaimRecognitionRow recognition in snapshot.Recognitions.Records)
            {
                EnsurePayloadBound(EstimateRecognitionPayloadLength(recognition));
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    P12AWire.WriteString(writer, recognition.ClaimId);
                    P12AWire.WriteString(writer, recognition.InstitutionId);
                    P12AWire.WriteString(writer, recognition.RecognitionId);
                    writer.Write((ushort)recognition.State);
                    writer.Write(recognition.RecognitionAbsoluteDay);
                    P12AWire.WriteString(writer, recognition.Reason);
                    writer.Write(recognition.History.Count);
                    foreach (P12EPoliticalClaimRecognitionHistoryRow history in recognition.History)
                    {
                        writer.Write((ushort)history.State);
                        writer.Write(history.Day);
                        P12AWire.WriteString(writer, history.Reason);
                    }
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
                            RecognitionOwnerPrefix + recognition.RecognitionId);
                        return false;
                    }
                    recognitionRows.Add(CreateOwnerRow(
                        RecognitionOwnerPrefix + recognition.RecognitionId, stream.ToArray()));
                }
            }

            claimsSection = new P12AOwnerSection(PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                PoliticalClaimStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(PoliticalClaimStoreCensusProvider.ClaimsSectionId),
                snapshot.Claims.Revision, claimRows);
            recognitionsSection = new P12AOwnerSection(PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
                PoliticalClaimStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(PoliticalClaimStoreCensusProvider.RecognitionsSectionId),
                snapshot.Recognitions.Revision, recognitionRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalClaimStoreCensusProvider.ClaimsSectionId);
            claimsSection = null;
            recognitionsSection = null;
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection claimsSection, P12AOwnerSection recognitionsSection,
        long capturedAbsoluteDay, out P12EPoliticalClaimOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalClaimStoreCensusProvider.ClaimsSectionId);
        if (claimsSection?.Rows.Count > P12AContinuationCodec.MaxRows
            || recognitionsSection?.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                PoliticalClaimStoreCensusProvider.ClaimsSectionId);
            return false;
        }
        if (!ValidSection(claimsSection, PoliticalClaimStoreCensusProvider.ClaimsSectionId)
            || !ValidSection(recognitionsSection, PoliticalClaimStoreCensusProvider.RecognitionsSectionId)
            || capturedAbsoluteDay < 0L || claimsSection.Revision != recognitionsSection.Revision
            || !RevisionCanRepresentRows(claimsSection.Revision,
                claimsSection.Rows.Count, recognitionsSection.Rows.Count))
            return false;

        try
        {
            List<P12EPoliticalClaimRow> claims = new List<P12EPoliticalClaimRow>(claimsSection.Rows.Count);
            foreach (P12AOwnerRow row in claimsSection.Rows)
            {
                if (row == null || row.Revision != 0L || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || !OwnerKeyHasValue(row.OwnerKey, ClaimOwnerPrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadRequiredString(reader, out string contract) || contract != PayloadContract
                        || !ReadRequiredString(reader, out string claimId)
                        || !ReadRequiredString(reader, out string claimantId)) return false;
                    PoliticalClaimType claimType = (PoliticalClaimType)reader.ReadUInt16();
                    PoliticalClaimTargetKind targetKind = (PoliticalClaimTargetKind)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalClaimType), claimType)
                        || !Enum.IsDefined(typeof(PoliticalClaimTargetKind), targetKind)
                        || !ReadRequiredString(reader, out string targetId)) return false;
                    PoliticalClaimBasis basis = (PoliticalClaimBasis)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalClaimBasis), basis)
                        || !P12AWire.ReadString(reader, out string description) || description == null) return false;
                    long createdDay = reader.ReadInt64();
                    PoliticalClaimStatus status = (PoliticalClaimStatus)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalClaimStatus), status)
                        || !TryReadNullableInt64(reader, out long? resolutionDay)
                        || !TryReadStrings(reader, out List<string> evidence)
                        || stream.Position != stream.Length
                        || !string.Equals(row.OwnerKey, ClaimOwnerPrefix + claimId, StringComparison.Ordinal))
                        return false;
                    claims.Add(new P12EPoliticalClaimRow(claimId, claimantId, (int)claimType,
                        (int)targetKind, targetId, (int)basis, description, createdDay,
                        (int)status, resolutionDay, evidence));
                }
            }

            List<P12EPoliticalClaimRecognitionRow> recognitions =
                new List<P12EPoliticalClaimRecognitionRow>(recognitionsSection.Rows.Count);
            foreach (P12AOwnerRow row in recognitionsSection.Rows)
            {
                if (row == null || row.Revision != 0L || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || !OwnerKeyHasValue(row.OwnerKey, RecognitionOwnerPrefix)) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadRequiredString(reader, out string contract) || contract != PayloadContract
                        || !ReadRequiredString(reader, out string claimId)
                        || !ReadRequiredString(reader, out string institutionId)
                        || !ReadRequiredString(reader, out string recognitionId)) return false;
                    PoliticalClaimRecognitionState state = (PoliticalClaimRecognitionState)reader.ReadUInt16();
                    long day = reader.ReadInt64();
                    if (!Enum.IsDefined(typeof(PoliticalClaimRecognitionState), state)
                        || !P12AWire.ReadString(reader, out string reason) || reason == null) return false;
                    int historyCount = ReadCount(reader);
                    List<P12EPoliticalClaimRecognitionHistoryRow> history =
                        new List<P12EPoliticalClaimRecognitionHistoryRow>(historyCount);
                    for (int i = 0; i < historyCount; i++)
                    {
                        PoliticalClaimRecognitionState historyState = (PoliticalClaimRecognitionState)reader.ReadUInt16();
                        long historyDay = reader.ReadInt64();
                        if (!Enum.IsDefined(typeof(PoliticalClaimRecognitionState), historyState)
                            || !P12AWire.ReadString(reader, out string historyReason) || historyReason == null)
                            return false;
                        history.Add(new P12EPoliticalClaimRecognitionHistoryRow(
                            (int)historyState, historyDay, historyReason));
                    }
                    if (stream.Position != stream.Length
                        || !string.Equals(row.OwnerKey, RecognitionOwnerPrefix + recognitionId, StringComparison.Ordinal))
                        return false;
                    recognitions.Add(new P12EPoliticalClaimRecognitionRow(
                        claimId, institutionId, recognitionId, (int)state, day, reason, history));
                }
            }

            P12EPoliticalClaimOwnerSnapshot candidate = new P12EPoliticalClaimOwnerSnapshot(
                new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                    PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                    P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion,
                    claims.Count, claimsSection.Revision, claims, row => row?.Copy()),
                new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                    PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
                    P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion,
                    recognitions.Count, recognitionsSection.Revision, recognitions, row => row?.Copy()));
            if (!TryValidateValues(candidate, capturedAbsoluteDay)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalClaimStoreCensusProvider.ClaimsSectionId);
            return false;
        }
    }

    private static bool TryValidateValues(P12EPoliticalClaimOwnerSnapshot snapshot, long capturedAbsoluteDay)
    {
        if (snapshot == null || capturedAbsoluteDay < 0L || snapshot.Claims == null || snapshot.Recognitions == null)
            return false;
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> claims = snapshot.Claims;
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> recognitions = snapshot.Recognitions;
        if (!ValidSnapshotSection(claims, PoliticalClaimStoreCensusProvider.ClaimsSectionId)
            || !ValidSnapshotSection(recognitions, PoliticalClaimStoreCensusProvider.RecognitionsSectionId)
            || claims.Revision < 0L || claims.Revision != recognitions.Revision
            || claims.RecordCount < 0 || recognitions.RecordCount < 0
            || claims.RecordCount > P12AContinuationCodec.MaxRows
            || recognitions.RecordCount > P12AContinuationCodec.MaxRows
            || !RevisionCanRepresentRows(claims.Revision, claims.RecordCount, recognitions.RecordCount)
            || claims.Records == null || recognitions.Records == null
            || claims.Records.Count != claims.RecordCount || recognitions.Records.Count != recognitions.RecordCount)
            return false;

        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        string previousClaimId = null;
        foreach (P12EPoliticalClaimRow claim in claims.Records)
        {
            if (claim == null || string.IsNullOrWhiteSpace(claim.ClaimId)
                || string.IsNullOrWhiteSpace(claim.ClaimantPersonId)
                || string.IsNullOrWhiteSpace(claim.TargetId) || claim.BasisDescription == null
                || !Enum.IsDefined(typeof(PoliticalClaimType), claim.ClaimType)
                || !Enum.IsDefined(typeof(PoliticalClaimTargetKind), claim.TargetKind)
                || !Enum.IsDefined(typeof(PoliticalClaimBasis), claim.Basis)
                || !Enum.IsDefined(typeof(PoliticalClaimStatus), claim.Status)
                || !PoliticalClaimRecord.IsTargetCompatible((PoliticalClaimType)claim.ClaimType,
                    (PoliticalClaimTargetKind)claim.TargetKind)
                || claim.CreatedAbsoluteDay < 0L || claim.CreatedAbsoluteDay > capturedAbsoluteDay
                || (claim.Status == (int)PoliticalClaimStatus.Active && claim.ResolutionAbsoluteDay.HasValue)
                || (claim.Status != (int)PoliticalClaimStatus.Active
                    && (!claim.ResolutionAbsoluteDay.HasValue
                        || claim.ResolutionAbsoluteDay.Value < claim.CreatedAbsoluteDay
                        || claim.ResolutionAbsoluteDay.Value > capturedAbsoluteDay))
                || !claimIds.Add(claim.ClaimId)
                || (previousClaimId != null && StringComparer.Ordinal.Compare(previousClaimId, claim.ClaimId) >= 0)
                || claim.EvidenceReferences == null)
                return false;
            if (claim.EvidenceReferences.Count > P12AContinuationCodec.MaxRows)
                throw new P12ALimitException();
            EnsurePayloadBound(EstimateClaimPayloadLength(claim));
            previousClaimId = claim.ClaimId;
            string previousEvidence = null;
            foreach (string evidence in claim.EvidenceReferences)
            {
                if (string.IsNullOrWhiteSpace(evidence)
                    || (previousEvidence != null && StringComparer.Ordinal.Compare(previousEvidence, evidence) >= 0))
                    return false;
                previousEvidence = evidence;
            }
        }

        HashSet<string> recognitionIds = new HashSet<string>(StringComparer.Ordinal);
        string previousRecognitionClaim = null;
        string previousRecognitionInstitution = null;
        foreach (P12EPoliticalClaimRecognitionRow recognition in recognitions.Records)
        {
            if (recognition == null || string.IsNullOrWhiteSpace(recognition.ClaimId)
                || string.IsNullOrWhiteSpace(recognition.InstitutionId)
                || !Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognition.State)
                || recognition.Reason == null || recognition.History == null || recognition.History.Count == 0
                || !claimIds.Contains(recognition.ClaimId)) return false;
            if (recognition.History.Count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
            string expectedId = PoliticalClaimRecognitionRecord.BuildRecognitionId(
                new PoliticalClaimId(recognition.ClaimId), new InstitutionId(recognition.InstitutionId));
            if (!string.Equals(recognition.RecognitionId, expectedId, StringComparison.Ordinal)
                || !recognitionIds.Add(expectedId)
                || (previousRecognitionClaim != null
                    && (StringComparer.Ordinal.Compare(previousRecognitionClaim, recognition.ClaimId) > 0
                        || (StringComparer.Ordinal.Equals(previousRecognitionClaim, recognition.ClaimId)
                            && StringComparer.Ordinal.Compare(previousRecognitionInstitution,
                                recognition.InstitutionId) >= 0)))) return false;

            P12EPoliticalClaimRow matchingClaim = null;
            foreach (P12EPoliticalClaimRow claim in claims.Records)
                if (string.Equals(claim.ClaimId, recognition.ClaimId, StringComparison.Ordinal))
                { matchingClaim = claim; break; }
            if (matchingClaim == null || recognition.RecognitionAbsoluteDay < matchingClaim.CreatedAbsoluteDay
                || recognition.RecognitionAbsoluteDay > capturedAbsoluteDay) return false;

            long lastDay = -1L;
            foreach (P12EPoliticalClaimRecognitionHistoryRow history in recognition.History)
            {
                if (history == null || !Enum.IsDefined(typeof(PoliticalClaimRecognitionState), history.State)
                    || history.Day < matchingClaim.CreatedAbsoluteDay || history.Day > capturedAbsoluteDay
                    || history.Day < lastDay || history.Reason == null) return false;
                lastDay = history.Day;
            }
            EnsurePayloadBound(EstimateRecognitionPayloadLength(recognition));
            P12EPoliticalClaimRecognitionHistoryRow terminal = recognition.History[recognition.History.Count - 1];
            if (terminal.State != recognition.State || terminal.Day != recognition.RecognitionAbsoluteDay
                || !string.Equals(terminal.Reason, recognition.Reason, StringComparison.Ordinal)) return false;
            previousRecognitionClaim = recognition.ClaimId;
            previousRecognitionInstitution = recognition.InstitutionId;
        }
        return true;
    }

    private static bool ValidSnapshotSection<T>(P12EPoliticalClaimSnapshotSection<T> section, string id)
        => section != null && string.Equals(section.SectionId, id, StringComparison.Ordinal)
            && section.SchemaVersion == P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion;

    private static bool ValidSection(P12AOwnerSection section, string id)
        => section != null && string.Equals(section.Id, id, StringComparison.Ordinal)
            && section.Major == PoliticalClaimStoreCensusProvider.SchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && string.Equals(section.AliasGroup, P12ADailyV1Census.ExpectedAliasGroupForSection(id),
                StringComparison.Ordinal)
            && section.Revision >= 0L;

    private static P12AOwnerRow CreateOwnerRow(string ownerKey, byte[] payload)
    {
        P12AWire.GetEncodedStringByteCount(ownerKey);
        return new P12AOwnerRow(ownerKey, 0L, payload);
    }

    private static bool RevisionCanRepresentRows(long revision, int claimCount, int recognitionCount)
    {
        long retainedRows = (long)claimCount + recognitionCount;
        return retainedRows == 0L ? revision == 0L : retainedRows <= revision;
    }

    private static bool OwnerKeyHasValue(string ownerKey, string prefix)
        => !string.IsNullOrWhiteSpace(ownerKey) && ownerKey.StartsWith(prefix, StringComparison.Ordinal)
            && ownerKey.Length > prefix.Length;

    private static long EstimateClaimPayloadLength(P12EPoliticalClaimRow claim)
    {
        long length = P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(claim.ClaimId)
            + P12AWire.GetEncodedStringFieldLength(claim.ClaimantPersonId)
            + sizeof(ushort) * 2L + P12AWire.GetEncodedStringFieldLength(claim.TargetId)
            + sizeof(ushort) + P12AWire.GetEncodedStringFieldLength(claim.BasisDescription)
            + sizeof(long) + sizeof(ushort) + 1L + (claim.ResolutionAbsoluteDay.HasValue ? sizeof(long) : 0L)
            + sizeof(int);
        foreach (string evidence in claim.EvidenceReferences)
            length = checked(length + P12AWire.GetEncodedStringFieldLength(evidence));
        return length;
    }

    private static long EstimateRecognitionPayloadLength(P12EPoliticalClaimRecognitionRow recognition)
    {
        long length = P12AWire.GetEncodedStringFieldLength(PayloadContract)
            + P12AWire.GetEncodedStringFieldLength(recognition.ClaimId)
            + P12AWire.GetEncodedStringFieldLength(recognition.InstitutionId)
            + P12AWire.GetEncodedStringFieldLength(recognition.RecognitionId)
            + sizeof(ushort) + sizeof(long)
            + P12AWire.GetEncodedStringFieldLength(recognition.Reason)
            + sizeof(int);
        foreach (P12EPoliticalClaimRecognitionHistoryRow history in recognition.History)
            length = checked(length + sizeof(ushort) + sizeof(long)
                + P12AWire.GetEncodedStringFieldLength(history.Reason));
        return length;
    }

    private static void EnsurePayloadBound(long length)
    {
        if (length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
    }

    private static void WriteStrings(BinaryWriter writer, IReadOnlyList<string> values)
    {
        if (values == null || values.Count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        writer.Write(values.Count);
        foreach (string value in values) P12AWire.WriteString(writer, value);
    }

    private static bool TryReadStrings(BinaryReader reader, out List<string> values)
    {
        values = null;
        int count = ReadCount(reader);
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            if (!ReadRequiredString(reader, out string value)) return false;
            result.Add(value);
        }
        values = result;
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
        byte present = reader.ReadByte();
        if (present == 0) return true;
        if (present != 1) return false;
        value = reader.ReadInt64();
        return true;
    }

    private static int ReadCount(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        return count;
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException
            || exception is IOException || exception is ArgumentException
            || exception is DecoderFallbackException || exception is OverflowException;
}
