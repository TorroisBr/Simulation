using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codecs for the three CrimeSocialAppraisal owner sections.</summary>
internal static class P12ECrimeSocialAppraisalOwnerCodec
{
    private const string PayloadContract = "p12a.crime-social-appraisal/v1";
    private const string OutcomePrefix = "outcome/";
    private const string KnowledgePrefix = "knowledge/";
    private const string ReactionPrefix = "reaction/";
    private const byte OutcomeKind = 1;
    private const byte KnowledgeKind = 2;
    private const byte ReactionKind = 3;

    internal static bool TryEncodeOutcomes(P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        string id = P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId;
        result = Invalid(id);
        P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> values = snapshot?.Outcomes;
        if (!ValidSection(values, id, snapshot?.CapturedAbsoluteDay ?? -1L, out result)) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(values.RecordCount);
            string previous = null;
            foreach (P12ECrimeOutcomeSnapshotRow value in values.Records)
            {
                if (!ValidOutcome(value, snapshot.CapturedAbsoluteDay)
                    || (previous != null && StringComparer.Ordinal.Compare(previous, value.OutcomeId) >= 0))
                    return false;
                previous = value.OutcomeId;
                rows.Add(EncodeOutcome(value));
            }
            section = CreateSection(id, values.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            section = null;
            return false;
        }
    }

    internal static bool TryDecodeOutcomes(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> values, out P12AResult result)
    {
        values = null;
        string id = P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId;
        result = Invalid(id);
        if (!ValidWireSection(section, id, capturedAbsoluteDay, out result)) return false;
        try
        {
            List<P12ECrimeOutcomeSnapshotRow> records = new List<P12ECrimeOutcomeSnapshotRow>(section.Rows.Count);
            string previous = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (!TryDecodeOutcome(row, records.Count, capturedAbsoluteDay,
                        out P12ECrimeOutcomeSnapshotRow record)
                    || (previous != null && StringComparer.Ordinal.Compare(previous, record.OutcomeId) >= 0)) return false;
                previous = record.OutcomeId;
                records.Add(record);
            }
            values = new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(id,
                P12CrimeSocialAppraisalCensusProvider.SchemaVersion, records.Count, section.Revision, records);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            values = null;
            return false;
        }
    }

    internal static bool TryEncodeKnowledge(P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        string id = P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId;
        result = Invalid(id);
        P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> values = snapshot?.Knowledge;
        if (!ValidSection(values, id, snapshot?.CapturedAbsoluteDay ?? -1L, out result)) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(values.RecordCount);
            string previous = null;
            foreach (P12ECrimeKnowledgeSnapshotRow value in values.Records)
            {
                string key = value == null ? null : KnowledgeKey(value.EvaluatorPersonId, value.OutcomeId);
                if (!ValidKnowledge(value, snapshot.CapturedAbsoluteDay)
                    || (previous != null && StringComparer.Ordinal.Compare(previous, key) >= 0)) return false;
                previous = key;
                rows.Add(EncodeKnowledge(value, key));
            }
            section = CreateSection(id, values.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            section = null;
            return false;
        }
    }

    internal static bool TryDecodeKnowledge(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> values, out P12AResult result)
    {
        values = null;
        string id = P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId;
        result = Invalid(id);
        if (!ValidWireSection(section, id, capturedAbsoluteDay, out result)) return false;
        try
        {
            List<P12ECrimeKnowledgeSnapshotRow> records = new List<P12ECrimeKnowledgeSnapshotRow>(section.Rows.Count);
            string previous = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (!TryDecodeKnowledge(row, records.Count, capturedAbsoluteDay,
                        out P12ECrimeKnowledgeSnapshotRow record)) return false;
                string key = KnowledgeKey(record.EvaluatorPersonId, record.OutcomeId);
                if (previous != null && StringComparer.Ordinal.Compare(previous, key) >= 0) return false;
                previous = key;
                records.Add(record);
            }
            values = new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(id,
                P12CrimeSocialAppraisalCensusProvider.SchemaVersion, records.Count, section.Revision, records);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            values = null;
            return false;
        }
    }

    internal static bool TryEncodeReactions(P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        string id = P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId;
        result = Invalid(id);
        P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> values = snapshot?.Reactions;
        if (!ValidSection(values, id, snapshot?.CapturedAbsoluteDay ?? -1L, out result)) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(values.RecordCount);
            string previous = null;
            foreach (P12ESocialReactionSnapshotRow value in values.Records)
            {
                if (!ValidReaction(value, snapshot.CapturedAbsoluteDay)
                    || (previous != null && StringComparer.Ordinal.Compare(previous, value.ReactionId) >= 0)) return false;
                previous = value.ReactionId;
                rows.Add(EncodeReaction(value));
            }
            section = CreateSection(id, values.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            section = null;
            return false;
        }
    }

    internal static bool TryDecodeReactions(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> values, out P12AResult result)
    {
        values = null;
        string id = P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId;
        result = Invalid(id);
        if (!ValidWireSection(section, id, capturedAbsoluteDay, out result)) return false;
        try
        {
            List<P12ESocialReactionSnapshotRow> records = new List<P12ESocialReactionSnapshotRow>(section.Rows.Count);
            string previous = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (!TryDecodeReaction(row, records.Count, capturedAbsoluteDay,
                        out P12ESocialReactionSnapshotRow record)
                    || (previous != null && StringComparer.Ordinal.Compare(previous, record.ReactionId) >= 0)) return false;
                previous = record.ReactionId;
                records.Add(record);
            }
            values = new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(id,
                P12CrimeSocialAppraisalCensusProvider.SchemaVersion, records.Count, section.Revision, records);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = WireFailure(id, exception);
            values = null;
            return false;
        }
    }

    private static P12AOwnerRow EncodeOutcome(P12ECrimeOutcomeSnapshotRow row)
        => EncodeRow(OutcomePrefix + row.OutcomeId, OutcomeKind, writer =>
        {
            P12AWire.WriteString(writer, row.OutcomeId);
            P12AWire.WriteString(writer, row.PerpetratorPersonId);
            P12AWire.WriteString(writer, row.VictimPersonId);
            writer.Write(row.LossAmount);
            writer.Write(row.OccurredAbsoluteDay);
            P12AWire.WriteString(writer, row.OccurrenceKey);
            P12AWire.WriteString(writer, row.OriginDecisionId);
        });

    private static P12AOwnerRow EncodeKnowledge(P12ECrimeKnowledgeSnapshotRow row, string key)
        => EncodeRow(KnowledgePrefix + key, KnowledgeKind, writer =>
        {
            P12AWire.WriteString(writer, row.EvaluatorPersonId);
            P12AWire.WriteString(writer, row.OutcomeId);
            writer.Write((ushort)row.Role);
            writer.Write(row.KnowsLoss ? (byte)1 : (byte)0);
            writer.Write((ushort)row.AttributionKind);
            P12AWire.WriteString(writer, row.AttributionPersonId);
            P12AWire.WriteString(writer, row.AttributionInstitutionId);
            P12AWire.WriteString(writer, row.KnownInvestigatorPersonId);
            P12AWire.WriteString(writer, row.KnownInvestigatorInstitutionId);
            writer.Write((ushort)row.CognitiveBasisKind);
            P12AWire.WriteString(writer, row.CognitiveBasisReference);
            P12AWire.WriteString(writer, row.CognitiveBasisPersonId);
            P12AWire.WriteString(writer, row.CognitiveBasisInstitutionId);
            writer.Write(row.ObservedAbsoluteDay);
        });

    private static P12AOwnerRow EncodeReaction(P12ESocialReactionSnapshotRow row)
        => EncodeRow(ReactionPrefix + row.ReactionId, ReactionKind, writer =>
        {
            P12AWire.WriteString(writer, row.ReactionId);
            P12AWire.WriteString(writer, row.EvaluatorPersonId);
            P12AWire.WriteString(writer, row.SourceDomain);
            P12AWire.WriteString(writer, row.SourceStableId);
            writer.Write((ushort)row.TargetKind);
            P12AWire.WriteString(writer, row.TargetStableId);
            writer.Write((ushort)row.AttributionKind);
            P12AWire.WriteString(writer, row.AttributionPersonId);
            P12AWire.WriteString(writer, row.AttributionInstitutionId);
            writer.Write((ushort)row.Valence);
            writer.Write((ushort)row.Salience);
            writer.Write((ushort)row.CognitiveBasisKind);
            P12AWire.WriteString(writer, row.CognitiveBasisReference);
            P12AWire.WriteString(writer, row.CognitiveBasisPersonId);
            P12AWire.WriteString(writer, row.CognitiveBasisInstitutionId);
            writer.Write(row.CreatedAbsoluteDay);
            P12AWire.WriteString(writer, row.SupersedesReactionId);
        });

    private static P12AOwnerRow EncodeRow(string ownerKey, byte kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writer.Write(kind);
            write(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(ownerKey, 0L, stream.ToArray());
        }
    }

    private static bool TryDecodeOutcome(P12AOwnerRow row, int ordinal, long capturedDay,
        out P12ECrimeOutcomeSnapshotRow value)
    {
        value = null;
        if (!TryOpen(row, OutcomePrefix, OutcomeKind, out string key, out BinaryReader reader, out MemoryStream stream)) return false;
        using (reader)
        {
            if (!P12AWire.ReadString(reader, out string id) || !ReadRequiredString(reader, out string perpetrator)
                || !ReadRequiredString(reader, out string victim)) return false;
            int loss = reader.ReadInt32();
            long day = reader.ReadInt64();
            if (!ReadRequiredString(reader, out string occurrenceKey)
                || !P12AWire.ReadString(reader, out string originDecisionId) || stream.Position != stream.Length) return false;
            P12ECrimeOutcomeSnapshotRow candidate = new P12ECrimeOutcomeSnapshotRow(
                id, perpetrator, victim, loss, day, occurrenceKey, originDecisionId);
            if (!string.Equals(key, OutcomePrefix + id, StringComparison.Ordinal)
                || !ValidOutcome(candidate, capturedDay)) return false;
            value = candidate;
            return true;
        }
    }

    private static bool TryDecodeKnowledge(P12AOwnerRow row, int ordinal, long capturedDay,
        out P12ECrimeKnowledgeSnapshotRow value)
    {
        value = null;
        if (!TryOpen(row, KnowledgePrefix, KnowledgeKind, out string key, out BinaryReader reader, out MemoryStream stream)) return false;
        using (reader)
        {
            if (!ReadRequiredString(reader, out string evaluator) || !ReadRequiredString(reader, out string outcome)) return false;
            int role = reader.ReadUInt16();
            if (!TryReadBoolean(reader, out bool knowsLoss)) return false;
            int attributionKind = reader.ReadUInt16();
            if (!P12AWire.ReadString(reader, out string attributionPerson)
                || !P12AWire.ReadString(reader, out string attributionInstitution)
                || !P12AWire.ReadString(reader, out string investigatorPerson)
                || !P12AWire.ReadString(reader, out string investigatorInstitution)) return false;
            int basisKind = reader.ReadUInt16();
            if (!P12AWire.ReadString(reader, out string basisReference)
                || !P12AWire.ReadString(reader, out string basisPerson)
                || !P12AWire.ReadString(reader, out string basisInstitution)) return false;
            long observedDay = reader.ReadInt64();
            P12ECrimeKnowledgeSnapshotRow candidate = new P12ECrimeKnowledgeSnapshotRow(
                evaluator, outcome, role, knowsLoss, attributionKind, attributionPerson,
                attributionInstitution, investigatorPerson, investigatorInstitution,
                basisKind, basisReference, basisPerson, basisInstitution, observedDay);
            string expectedKey = KnowledgeKey(evaluator, outcome);
            if (stream.Position != stream.Length || !string.Equals(key, KnowledgePrefix + expectedKey, StringComparison.Ordinal)
                || !ValidKnowledge(candidate, capturedDay)) return false;
            value = candidate;
            return true;
        }
    }

    private static bool TryDecodeReaction(P12AOwnerRow row, int ordinal, long capturedDay,
        out P12ESocialReactionSnapshotRow value)
    {
        value = null;
        if (!TryOpen(row, ReactionPrefix, ReactionKind, out string key, out BinaryReader reader, out MemoryStream stream)) return false;
        using (reader)
        {
            if (!ReadRequiredString(reader, out string id) || !ReadRequiredString(reader, out string evaluator)
                || !ReadRequiredString(reader, out string sourceDomain) || !ReadRequiredString(reader, out string sourceId)) return false;
            int targetKind = reader.ReadUInt16();
            if (!ReadRequiredString(reader, out string targetId)) return false;
            int attributionKind = reader.ReadUInt16();
            if (!P12AWire.ReadString(reader, out string attributionPerson)
                || !P12AWire.ReadString(reader, out string attributionInstitution)) return false;
            int valence = reader.ReadUInt16();
            int salience = reader.ReadUInt16();
            int basisKind = reader.ReadUInt16();
            if (!P12AWire.ReadString(reader, out string basisReference)
                || !P12AWire.ReadString(reader, out string basisPerson)
                || !P12AWire.ReadString(reader, out string basisInstitution)) return false;
            long createdDay = reader.ReadInt64();
            if (!P12AWire.ReadString(reader, out string supersedes) || stream.Position != stream.Length) return false;
            P12ESocialReactionSnapshotRow candidate = new P12ESocialReactionSnapshotRow(
                id, evaluator, sourceDomain, sourceId, targetKind, targetId, attributionKind,
                attributionPerson, attributionInstitution, valence, salience, basisKind,
                basisReference, basisPerson, basisInstitution, createdDay, supersedes);
            if (!string.Equals(key, ReactionPrefix + id, StringComparison.Ordinal)
                || !ValidReaction(candidate, capturedDay)) return false;
            value = candidate;
            return true;
        }
    }

    private static bool TryOpen(P12AOwnerRow row, string prefix, byte kind,
        out string ownerKey, out BinaryReader reader, out MemoryStream stream)
    {
        ownerKey = null; reader = null; stream = null;
        if (row == null || row.Revision != 0L || string.IsNullOrWhiteSpace(row.OwnerKey)
            || !row.OwnerKey.StartsWith(prefix, StringComparison.Ordinal)
            || row.OwnerKey.Length == prefix.Length || row.PayloadLength < 0
            || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
        P12AWire.GetEncodedStringByteCount(row.OwnerKey);
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        stream = new MemoryStream(payload, false);
        reader = new BinaryReader(stream, P12AWire.Utf8, true);
        ownerKey = row.OwnerKey;
        if (!ReadRequiredString(reader, out string contract)
            || !string.Equals(contract, PayloadContract, StringComparison.Ordinal)
            || reader.ReadByte() != kind)
        {
            reader.Dispose();
            stream.Dispose();
            reader = null;
            stream = null;
            ownerKey = null;
            return false;
        }
        return true;
    }

    private static bool ValidOutcome(P12ECrimeOutcomeSnapshotRow row, long capturedDay)
    {
        if (row == null || !NonEmpty(row.OutcomeId) || !NonEmpty(row.PerpetratorPersonId)
            || !NonEmpty(row.VictimPersonId) || row.LossAmount <= 0 || !NonEmpty(row.OccurrenceKey)
            || row.OccurredAbsoluteDay < 0L || row.OccurredAbsoluteDay > capturedDay) return false;
        return string.Equals(row.OutcomeId, TheftOutcomeId.Create(new PersonId(row.PerpetratorPersonId),
            new PersonId(row.VictimPersonId), row.OccurredAbsoluteDay, row.OccurrenceKey).Value, StringComparison.Ordinal);
    }

    private static bool ValidKnowledge(P12ECrimeKnowledgeSnapshotRow row, long capturedDay)
    {
        if (row == null || !NonEmpty(row.EvaluatorPersonId) || !NonEmpty(row.OutcomeId)
            || !Enum.IsDefined(typeof(CrimeKnowledgeRole), row.Role)
            || !Enum.IsDefined(typeof(SocialPerceivedAttributionKind), row.AttributionKind)
            || !Enum.IsDefined(typeof(SocialCognitiveBasisKind), row.CognitiveBasisKind)
            || (row.KnownInvestigatorPersonId != null && row.KnownInvestigatorInstitutionId != null)
            || (row.CognitiveBasisPersonId != null && row.CognitiveBasisInstitutionId != null)
            || !OptionalId(row.KnownInvestigatorPersonId) || !OptionalId(row.KnownInvestigatorInstitutionId)
            || !OptionalId(row.CognitiveBasisPersonId) || !OptionalId(row.CognitiveBasisInstitutionId)
            || row.ObservedAbsoluteDay < 0L || row.ObservedAbsoluteDay > capturedDay) return false;
        if (!ValidAttribution(row.AttributionKind, row.AttributionPersonId, row.AttributionInstitutionId)) return false;
        if (!row.KnowsLoss && row.AttributionKind != (int)SocialPerceivedAttributionKind.NotApplicable) return false;
        return true;
    }

    private static bool ValidReaction(P12ESocialReactionSnapshotRow row, long capturedDay)
    {
        if (row == null || !NonEmpty(row.ReactionId) || !NonEmpty(row.EvaluatorPersonId)
            || !NonEmpty(row.SourceDomain) || !NonEmpty(row.SourceStableId)
            || !Enum.IsDefined(typeof(SocialReactionTargetKind), row.TargetKind) || !NonEmpty(row.TargetStableId)
            || !Enum.IsDefined(typeof(SocialReactionValence), row.Valence)
            || !Enum.IsDefined(typeof(SocialReactionSalience), row.Salience)
            || !Enum.IsDefined(typeof(SocialCognitiveBasisKind), row.CognitiveBasisKind)
            || (row.CognitiveBasisPersonId != null && row.CognitiveBasisInstitutionId != null)
            || !OptionalId(row.CognitiveBasisPersonId) || !OptionalId(row.CognitiveBasisInstitutionId)
            || row.CreatedAbsoluteDay < 0L || row.CreatedAbsoluteDay > capturedDay
            || !ValidAttribution(row.AttributionKind, row.AttributionPersonId, row.AttributionInstitutionId)) return false;
        SocialReactionTarget target = SocialReactionTarget.ForStableId((SocialReactionTargetKind)row.TargetKind, row.TargetStableId);
        SocialPerceivedAttribution attribution = CreateAttribution(row.AttributionKind, row.AttributionPersonId, row.AttributionInstitutionId);
        SocialCognitiveBasis basis = new SocialCognitiveBasis((SocialCognitiveBasisKind)row.CognitiveBasisKind,
            row.CognitiveBasisReference,
            row.CognitiveBasisPersonId == null ? null : new PersonId(row.CognitiveBasisPersonId),
            row.CognitiveBasisInstitutionId == null ? null : new InstitutionId(row.CognitiveBasisInstitutionId));
        SocialReactionId supersedes = row.SupersedesReactionId == null ? null : new SocialReactionId(row.SupersedesReactionId);
        string expected = SocialReactionId.Create(new PersonId(row.EvaluatorPersonId),
            new SocialSourceReference(row.SourceDomain, row.SourceStableId), target, attribution, basis,
            (SocialReactionValence)row.Valence, (SocialReactionSalience)row.Salience,
            row.CreatedAbsoluteDay, supersedes).Value;
        return string.Equals(row.ReactionId, expected, StringComparison.Ordinal);
    }

    private static bool ValidAttribution(int kind, string personId, string institutionId)
    {
        if (!Enum.IsDefined(typeof(SocialPerceivedAttributionKind), kind)) return false;
        switch ((SocialPerceivedAttributionKind)kind)
        {
            case SocialPerceivedAttributionKind.NotApplicable:
            case SocialPerceivedAttributionKind.Unknown:
                return personId == null && institutionId == null;
            case SocialPerceivedAttributionKind.BelievedPerson:
                return NonEmpty(personId) && institutionId == null;
            case SocialPerceivedAttributionKind.BelievedInstitution:
                return personId == null && NonEmpty(institutionId);
            default: return false;
        }
    }

    private static SocialPerceivedAttribution CreateAttribution(int kind, string personId, string institutionId)
    {
        switch ((SocialPerceivedAttributionKind)kind)
        {
            case SocialPerceivedAttributionKind.NotApplicable: return SocialPerceivedAttribution.NotApplicable();
            case SocialPerceivedAttributionKind.Unknown: return SocialPerceivedAttribution.Unknown();
            case SocialPerceivedAttributionKind.BelievedPerson: return SocialPerceivedAttribution.BelievedPerson(new PersonId(personId));
            case SocialPerceivedAttributionKind.BelievedInstitution: return SocialPerceivedAttribution.BelievedInstitution(new InstitutionId(institutionId));
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static bool ValidSection<TRow>(P12ECrimeOwnerSnapshotSection<TRow> values, string id,
        long capturedDay, out P12AResult result) where TRow : class
    {
        result = Invalid(id);
        if (capturedDay < 0L || values == null || values.Revision < 0L
            || !string.Equals(values.SectionId, id, StringComparison.Ordinal)
            || values.RecordCount < 0 || values.RecordCount > P12AContinuationCodec.MaxRows
            || values.Records == null || values.Records.Count != values.RecordCount)
        {
            if (values != null && values.RecordCount > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, id);
            return false;
        }
        if (values.SchemaVersion != P12CrimeSocialAppraisalCensusProvider.SchemaVersion)
        {
            result = new P12AResult(P12AResultCode.UnsupportedSchema, id);
            return false;
        }
        result = null;
        return true;
    }

    private static bool ValidWireSection(P12AOwnerSection section, string id, long capturedDay,
        out P12AResult result)
    {
        result = Invalid(id);
        if (section?.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded, id);
            return false;
        }
        if (capturedDay < 0L || section == null || section.Id != id
            || section.Role != OwnerSectionRole.Required
            || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(id)
            || section.Revision < 0L) return false;
        if (section.Major != P12CrimeSocialAppraisalCensusProvider.SchemaVersion || section.Minor != 0)
        {
            result = new P12AResult(P12AResultCode.UnsupportedSchema, id);
            return false;
        }
        if (!SectionLengthWithinLimit(section.Rows, out bool exceeded))
        {
            if (exceeded) result = new P12AResult(P12AResultCode.LimitExceeded, id);
            return false;
        }
        result = null;
        return true;
    }

    private static P12AOwnerSection CreateSection(string id, long revision, IEnumerable<P12AOwnerRow> rows)
    {
        List<P12AOwnerRow> materialized = new List<P12AOwnerRow>(rows);
        if (!SectionLengthWithinLimit(materialized, out bool exceeded) && exceeded)
            throw new P12ALimitException();
        return new P12AOwnerSection(id, P12CrimeSocialAppraisalCensusProvider.SchemaVersion, 0,
            OwnerSectionRole.Required, P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision, materialized);
    }

    private static bool SectionLengthWithinLimit(IEnumerable<P12AOwnerRow> rows, out bool exceeded)
    {
        exceeded = false;
        try
        {
            long total = sizeof(int);
            foreach (P12AOwnerRow row in rows)
            {
                byte[] payload = row?.Payload;
                if (row == null || string.IsNullOrWhiteSpace(row.OwnerKey) || row.Revision < 0L
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || payload == null || payload.Length != row.PayloadLength)
                    return false;
                total = checked(total + sizeof(int) + P12AWire.GetEncodedStringByteCount(row.OwnerKey)
                    + sizeof(long) + sizeof(int) + row.PayloadLength);
                if (total > P12AContinuationCodec.MaxSectionBytes)
                {
                    exceeded = true;
                    return false;
                }
            }
            return true;
        }
        catch (OverflowException)
        {
            exceeded = true;
            return false;
        }
        catch (P12ALimitException)
        {
            exceeded = true;
            return false;
        }
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;
    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte encoded = reader.ReadByte();
        if (encoded > 1) return false;
        value = encoded == 1;
        return true;
    }
    private static string KnowledgeKey(string evaluator, string outcome)
        => evaluator.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + evaluator
            + outcome.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + outcome;
    private static bool NonEmpty(string value) => !string.IsNullOrWhiteSpace(value);
    private static bool OptionalId(string value) => value == null || NonEmpty(value);
    private static P12AResult Invalid(string id) => new P12AResult(P12AResultCode.InvalidOwnerValue, id);
    private static P12AResult WireFailure(string id, Exception exception)
        => new P12AResult(exception is P12ALimitException || exception is OverflowException
            ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, id);
    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is InvalidOperationException
            || exception is DecoderFallbackException || exception is OverflowException
            || exception is NullReferenceException;
}
