using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for the append-only PoliticalDecision owner.</summary>
internal static class P12EPoliticalDecisionOwnerCodec
{
    private const string PayloadContract = "p12a.political-decisions/v1";
    private const string OwnerPrefix = "decision/";

    internal static bool TryEncode(P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalDecisionStoreCensusProvider.SectionId);
        if (snapshot == null || snapshot.Decisions == null) return false;
        if (snapshot.Decisions.RecordCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                PoliticalDecisionStoreCensusProvider.SectionId);
            return false;
        }
        if (!snapshot.TryValidate(out _)) return false;

        try
        {
            P12EPoliticalDecisionSnapshotSection decisions = snapshot.Decisions;
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(decisions.RecordCount);
            foreach (P12EPoliticalDecisionSnapshotRow decision in decisions.Records)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    WriteDecision(writer, decision);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            PoliticalDecisionStoreCensusProvider.SectionId,
                            OwnerPrefix + decision.DecisionId);
                        return false;
                    }
                    // The append-only owner exposes one section revision, not per-record revisions.
                    rows.Add(new P12AOwnerRow(OwnerPrefix + decision.DecisionId, 0L, stream.ToArray()));
                }
            }

            section = new P12AOwnerSection(PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                null, decisions.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalDecisionStoreCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12EPoliticalDecisionOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalDecisionStoreCensusProvider.SectionId);
        if (section == null || capturedAbsoluteDay < 0L
            || section.Id != PoliticalDecisionStoreCensusProvider.SectionId
            || section.Major != PoliticalDecisionStoreCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count > P12AContinuationCodec.MaxRows
            || section.Revision != section.Rows.Count) return false;

        try
        {
            List<P12EPoliticalDecisionSnapshotRow> decisions = new List<P12EPoliticalDecisionSnapshotRow>(section.Rows.Count);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != 0L || string.IsNullOrEmpty(row.OwnerKey)
                    || !row.OwnerKey.StartsWith(OwnerPrefix, StringComparison.Ordinal)
                    || row.OwnerKey.Length == OwnerPrefix.Length || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    return false;

                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadRequiredString(reader, out string contract) || contract != PayloadContract
                        || !TryReadDecision(reader, out P12EPoliticalDecisionSnapshotRow decision)
                        || stream.Position != stream.Length
                        || !string.Equals(row.OwnerKey, OwnerPrefix + decision.DecisionId, StringComparison.Ordinal))
                        return false;
                    decisions.Add(decision);
                }
            }

            P12EPoliticalDecisionSnapshotSection values = new P12EPoliticalDecisionSnapshotSection(
                PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion,
                decisions.Count,
                section.Revision,
                decisions);
            P12EPoliticalDecisionOwnerSnapshot candidate = new P12EPoliticalDecisionOwnerSnapshot(
                capturedAbsoluteDay, values);
            if (!candidate.TryValidate(out _)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalDecisionStoreCensusProvider.SectionId);
            return false;
        }
    }

    private static void WriteDecision(BinaryWriter writer, P12EPoliticalDecisionSnapshotRow row)
    {
        P12AWire.WriteString(writer, row.DecisionId);
        writer.Write((ushort)row.DeciderKind);
        P12AWire.WriteString(writer, row.DeciderId);
        writer.Write((ushort)row.DecisionKind);
        WriteStrings(writer, row.CandidatePersonIds);
        writer.Write((ushort)row.OutcomeKind);
        P12AWire.WriteString(writer, row.SelectedCandidatePersonId);
        P12AWire.WriteString(writer, row.ReferencedClaimId);
        writer.Write(row.ObservedAbsoluteDay);
        writer.Write(row.DecisionAbsoluteDay);
        WriteStrings(writer, row.EvidenceReferences);
        WriteStrings(writer, row.KnowledgeReferences);
        writer.Write(row.ExpectedWorldRevision);
        writer.Write(row.ExpectedKnowledgeRevision);
        P12AWire.WriteString(writer, row.OfficeId);
        P12AWire.WriteString(writer, row.RecognizingInstitutionId);
    }

    private static bool TryReadDecision(BinaryReader reader, out P12EPoliticalDecisionSnapshotRow row)
    {
        row = null;
        if (!ReadRequiredString(reader, out string decisionId)) return false;
        PoliticalKnowledgeHolderKind deciderKind = (PoliticalKnowledgeHolderKind)reader.ReadUInt16();
        if (!Enum.IsDefined(typeof(PoliticalKnowledgeHolderKind), deciderKind)
            || !ReadRequiredString(reader, out string deciderId)) return false;
        PoliticalDecisionKind decisionKind = (PoliticalDecisionKind)reader.ReadUInt16();
        if (!Enum.IsDefined(typeof(PoliticalDecisionKind), decisionKind)
            || !TryReadStrings(reader, out List<string> candidates)) return false;
        PoliticalDecisionOutcomeKind outcomeKind = (PoliticalDecisionOutcomeKind)reader.ReadUInt16();
        if (!Enum.IsDefined(typeof(PoliticalDecisionOutcomeKind), outcomeKind)
            || !P12AWire.ReadString(reader, out string selectedCandidate)
            || !P12AWire.ReadString(reader, out string referencedClaim)) return false;
        long observedDay = reader.ReadInt64();
        long decisionDay = reader.ReadInt64();
        if (!TryReadStrings(reader, out List<string> evidence)
            || !TryReadStrings(reader, out List<string> knowledge)) return false;
        long expectedWorldRevision = reader.ReadInt64();
        long expectedKnowledgeRevision = reader.ReadInt64();
        if (!P12AWire.ReadString(reader, out string officeId)
            || !P12AWire.ReadString(reader, out string recognizingInstitutionId)) return false;

        row = new P12EPoliticalDecisionSnapshotRow(decisionId, (int)deciderKind, deciderId,
            (int)decisionKind, candidates, (int)outcomeKind, selectedCandidate, referencedClaim,
            observedDay, decisionDay, evidence, knowledge, expectedWorldRevision,
            expectedKnowledgeRevision, officeId, recognizingInstitutionId);
        return true;
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
        int count = reader.ReadInt32();
        if (count < 0 || count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        List<string> result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            if (!ReadRequiredString(reader, out string value)) return false;
            result.Add(value);
        }
        values = result;
        return true;
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException
            || exception is IOException || exception is ArgumentException
            || exception is DecoderFallbackException || exception is OverflowException;
}
