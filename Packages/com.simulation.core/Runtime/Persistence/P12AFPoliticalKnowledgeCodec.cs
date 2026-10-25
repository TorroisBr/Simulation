using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit v1 wire codec for all current Political Knowledge owner values.</summary>
internal static class P12AFPoliticalKnowledgeCodec
{
    private const string PayloadContract = "p12a.political-knowledge/v1";
    private const string OwnerPrefix = "holder/";

    internal static bool TryEncode(P12FPoliticalKnowledgeOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalKnowledgeStoreCensusProvider.SectionId);
        if (snapshot == null || snapshot.Revision < 0L || snapshot.Cardinality < 0
            || snapshot.Cardinality > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.Cardinality
            || (snapshot.Cardinality == 0 && snapshot.Revision != 0L)) return false;

        try
        {
            IReadOnlyList<PoliticalKnowledgeRuntime> runtimes = snapshot.CopyDetachedRuntimes();
            if (runtimes == null || runtimes.Count != snapshot.Cardinality) return false;
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(runtimes.Count);
            HashSet<string> holders = new HashSet<string>(StringComparer.Ordinal);
            string priorHolder = null;
            foreach (PoliticalKnowledgeRuntime runtime in runtimes)
            {
                if (runtime == null || runtime.Holder == null) return false;
                string stableId = runtime.Holder.StableId;
                if (string.IsNullOrWhiteSpace(stableId) || !holders.Add(stableId)
                    || (priorHolder != null && StringComparer.Ordinal.Compare(priorHolder, stableId) >= 0))
                    return false;
                priorHolder = stableId;

                IReadOnlyList<PoliticalKnowledgeObservation> observations = runtime.Observations;
                if (observations == null || observations.Count > P12AContinuationCodec.MaxRows) return false;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    WriteEnum(writer, runtime.Holder.Kind);
                    P12AWire.WriteString(writer, runtime.Holder.RawStableId);
                    writer.Write(observations.Count);
                    PoliticalKnowledgeObservation prior = null;
                    foreach (PoliticalKnowledgeObservation observation in observations)
                    {
                        if (!IsSupported(observation) || (prior != null && Compare(prior, observation) >= 0))
                            return false;
                        WriteObservation(writer, observation);
                        prior = observation;
                    }
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            PoliticalKnowledgeStoreCensusProvider.SectionId, OwnerPrefix + stableId);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(OwnerPrefix + stableId, 0L, stream.ToArray()));
                }
            }

            section = new P12AOwnerSection(PoliticalKnowledgeStoreCensusProvider.SectionId,
                PoliticalKnowledgeStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null,
                snapshot.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalKnowledgeStoreCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12FPoliticalKnowledgeOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalKnowledgeStoreCensusProvider.SectionId);
        if (section == null || section.Id != PoliticalKnowledgeStoreCensusProvider.SectionId
            || section.Major != PoliticalKnowledgeStoreCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count > P12AContinuationCodec.MaxRows
            || section.Revision < section.Rows.Count
            || (section.Rows.Count == 0 && section.Revision != 0L)) return false;

        try
        {
            List<PoliticalKnowledgeRuntime> runtimes = new List<PoliticalKnowledgeRuntime>(section.Rows.Count);
            HashSet<string> holders = new HashSet<string>(StringComparer.Ordinal);
            string priorStableId = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != 0L || row.Payload == null
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || !TryReadRuntime(row.Payload, out PoliticalKnowledgeRuntime runtime)
                    || runtime == null || runtime.Holder == null)
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        PoliticalKnowledgeStoreCensusProvider.SectionId, row?.OwnerKey);
                    return false;
                }
                string stableId = runtime.Holder.StableId;
                if (row.OwnerKey != OwnerPrefix + stableId || !holders.Add(stableId)
                    || (priorStableId != null && StringComparer.Ordinal.Compare(priorStableId, stableId) >= 0))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        PoliticalKnowledgeStoreCensusProvider.SectionId, row.OwnerKey);
                    return false;
                }
                priorStableId = stableId;
                runtimes.Add(runtime);
            }

            snapshot = new P12FPoliticalKnowledgeOwnerSnapshot(runtimes, section.Revision);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            snapshot = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalKnowledgeStoreCensusProvider.SectionId);
            return false;
        }
    }

    private static bool TryReadRuntime(byte[] payload, out PoliticalKnowledgeRuntime runtime)
    {
        runtime = null;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !ReadEnum(reader, out PoliticalKnowledgeHolderKind holderKind)
                || !ReadRequiredString(reader, out string holderId)) return false;
            PoliticalKnowledgeHolder holder = CreateHolder(holderKind, holderId);
            if (holder == null || !ReadCount(reader, P12AContinuationCodec.MaxRows, out int count)) return false;
            PoliticalKnowledgeRuntime candidate = new PoliticalKnowledgeRuntime(holder);
            PoliticalKnowledgeObservation prior = null;
            for (int i = 0; i < count; i++)
            {
                if (!TryReadObservation(reader, out PoliticalKnowledgeObservation observation)
                    || !IsSupported(observation) || (prior != null && Compare(prior, observation) >= 0)
                    || !Record(candidate, observation)) return false;
                prior = observation;
            }
            if (stream.Position != stream.Length || candidate.Observations.Count != count) return false;
            runtime = candidate;
            return true;
        }
    }

    private static bool TryReadObservation(BinaryReader reader, out PoliticalKnowledgeObservation observation)
    {
        observation = null;
        if (!ReadEnum(reader, out PoliticalKnowledgeFactKind factKind)) return false;
        long observedDay = reader.ReadInt64();
        long receivedDay = reader.ReadInt64();
        if (!TryReadProvenance(reader, out PoliticalKnowledgeProvenance provenance)) return false;

        switch (factKind)
        {
            case PoliticalKnowledgeFactKind.PoliticalClaim:
                if (!ReadRequiredString(reader, out string claimId)
                    || !ReadBoolean(reader, out bool exists)
                    || !ReadRequiredString(reader, out string claimantId)
                    || !ReadEnum(reader, out PoliticalClaimType claimType)
                    || !ReadEnum(reader, out PoliticalClaimTargetKind targetKind)
                    || !ReadRequiredString(reader, out string targetId)
                    || !ReadEnum(reader, out PoliticalClaimBasis basis)) return false;
                long createdDay = reader.ReadInt64();
                if (!ReadEnum(reader, out PoliticalClaimStatus status)
                    || !ReadNullableLong(reader, out long? resolutionDay)
                    || !ReadEnum(reader, out PoliticalClaimRecognitionState recognition)
                    || !ReadNullableString(reader, out string recognizingInstitution)
                    || !ReadNullableLong(reader, out long? recognitionDay)) return false;
                observation = new PoliticalClaimKnowledgeObservation(new PoliticalClaimId(claimId), exists,
                    new PersonId(claimantId), claimType, CreateTarget(targetKind, targetId), basis,
                    createdDay, status, resolutionDay, recognition,
                    recognizingInstitution == null ? null : new InstitutionId(recognizingInstitution),
                    recognitionDay, observedDay, receivedDay, provenance);
                return true;

            case PoliticalKnowledgeFactKind.Faction:
                if (!ReadRequiredString(reader, out string factionId) || !ReadBoolean(reader, out bool factionExists)) return false;
                observation = new FactionKnowledgeObservation(new FactionId(factionId), factionExists,
                    observedDay, receivedDay, provenance);
                return true;

            case PoliticalKnowledgeFactKind.FactionAffiliation:
                if (!ReadRequiredString(reader, out string affiliationFaction)
                    || !ReadRequiredString(reader, out string affiliationPerson)
                    || !ReadBoolean(reader, out bool active)) return false;
                observation = new FactionAffiliationKnowledgeObservation(new FactionId(affiliationFaction),
                    new PersonId(affiliationPerson), active, observedDay, receivedDay, provenance);
                return true;

            case PoliticalKnowledgeFactKind.OfficeVacancy:
                if (!ReadRequiredString(reader, out string officeId)
                    || !ReadRequiredString(reader, out string officeInstitution)
                    || !ReadBoolean(reader, out bool vacant)
                    || !ReadBoolean(reader, out bool recognizedVacant)) return false;
                observation = new OfficeVacancyKnowledgeObservation(new OfficeId(officeId),
                    new InstitutionId(officeInstitution), vacant, observedDay, receivedDay,
                    provenance, recognizedVacant);
                return true;

            case PoliticalKnowledgeFactKind.PersonDeath:
                if (!ReadRequiredString(reader, out string personId)
                    || !ReadBoolean(reader, out bool isDead)
                    || !ReadNullableLong(reader, out long? deathDay)) return false;
                observation = new PersonDeathKnowledgeObservation(new PersonId(personId), isDead,
                    deathDay, observedDay, receivedDay, provenance);
                return true;

            default:
                return false;
        }
    }

    private static void WriteObservation(BinaryWriter writer, PoliticalKnowledgeObservation observation)
    {
        WriteEnum(writer, observation.FactKind);
        writer.Write(observation.ObservedAbsoluteDay);
        writer.Write(observation.ReceivedAbsoluteDay);
        WriteProvenance(writer, observation.Provenance);

        if (observation is PoliticalClaimKnowledgeObservation claim)
        {
            P12AWire.WriteString(writer, claim.ClaimId.Value);
            WriteBoolean(writer, claim.Exists);
            P12AWire.WriteString(writer, claim.ClaimantPersonId.Value);
            WriteEnum(writer, claim.ClaimType);
            WriteEnum(writer, claim.Target.Kind);
            P12AWire.WriteString(writer, claim.Target.TargetId);
            WriteEnum(writer, claim.Basis);
            writer.Write(claim.CreatedAbsoluteDay);
            WriteEnum(writer, claim.Status);
            WriteNullableLong(writer, claim.ResolutionAbsoluteDay);
            WriteEnum(writer, claim.RecognitionState);
            P12AWire.WriteString(writer, claim.RecognizingInstitutionId?.Value);
            WriteNullableLong(writer, claim.RecognitionAbsoluteDay);
            return;
        }
        if (observation is FactionKnowledgeObservation faction)
        {
            P12AWire.WriteString(writer, faction.FactionId.Value);
            WriteBoolean(writer, faction.Exists);
            return;
        }
        if (observation is FactionAffiliationKnowledgeObservation affiliation)
        {
            P12AWire.WriteString(writer, affiliation.FactionId.Value);
            P12AWire.WriteString(writer, affiliation.PersonId.Value);
            WriteBoolean(writer, affiliation.IsActive);
            return;
        }
        if (observation is OfficeVacancyKnowledgeObservation office)
        {
            P12AWire.WriteString(writer, office.OfficeId.Value);
            P12AWire.WriteString(writer, office.InstitutionId.Value);
            WriteBoolean(writer, office.IsVacant);
            WriteBoolean(writer, office.IsRecognizedVacant);
            return;
        }
        if (observation is PersonDeathKnowledgeObservation death)
        {
            P12AWire.WriteString(writer, death.PersonId.Value);
            WriteBoolean(writer, death.IsDead);
            WriteNullableLong(writer, death.DeathAbsoluteDay);
            return;
        }
        throw new ArgumentException("The Political Knowledge observation type is unsupported.", nameof(observation));
    }

    private static bool TryReadProvenance(BinaryReader reader, out PoliticalKnowledgeProvenance provenance)
    {
        provenance = null;
        if (!ReadEnum(reader, out PoliticalKnowledgeSource source)
            || !ReadNullableString(reader, out string sourceReference)
            || !ReadNullableString(reader, out string sourcePerson)
            || !ReadNullableString(reader, out string sourceInstitution)) return false;
        provenance = new PoliticalKnowledgeProvenance(source, sourceReference,
            sourcePerson == null ? null : new PersonId(sourcePerson),
            sourceInstitution == null ? null : new InstitutionId(sourceInstitution));
        return true;
    }

    private static void WriteProvenance(BinaryWriter writer, PoliticalKnowledgeProvenance provenance)
    {
        if (provenance == null) throw new ArgumentException("Political Knowledge provenance is required.");
        WriteEnum(writer, provenance.Source);
        P12AWire.WriteString(writer, provenance.SourceReference);
        P12AWire.WriteString(writer, provenance.SourcePersonId?.Value);
        P12AWire.WriteString(writer, provenance.SourceInstitutionId?.Value);
    }

    private static PoliticalKnowledgeHolder CreateHolder(PoliticalKnowledgeHolderKind kind, string id)
    {
        switch (kind)
        {
            case PoliticalKnowledgeHolderKind.Person: return PoliticalKnowledgeHolder.ForPerson(new PersonId(id));
            case PoliticalKnowledgeHolderKind.Institution: return PoliticalKnowledgeHolder.ForInstitution(new InstitutionId(id));
            case PoliticalKnowledgeHolderKind.Faction: return PoliticalKnowledgeHolder.ForFaction(new FactionId(id));
            default: return null;
        }
    }

    private static PoliticalClaimTarget CreateTarget(PoliticalClaimTargetKind kind, string id)
    {
        switch (kind)
        {
            case PoliticalClaimTargetKind.Office: return PoliticalClaimTarget.ForOffice(new OfficeId(id));
            case PoliticalClaimTargetKind.Property: return PoliticalClaimTarget.ForProperty(new PropertyId(id));
            case PoliticalClaimTargetKind.Institution: return PoliticalClaimTarget.ForInstitution(new InstitutionId(id));
            case PoliticalClaimTargetKind.Person: return PoliticalClaimTarget.ForPerson(new PersonId(id));
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static bool Record(PoliticalKnowledgeRuntime runtime, PoliticalKnowledgeObservation observation)
    {
        if (observation is PoliticalClaimKnowledgeObservation claim) return runtime.RecordClaimObservation(claim);
        if (observation is FactionKnowledgeObservation faction) return runtime.RecordFactionObservation(faction);
        if (observation is FactionAffiliationKnowledgeObservation affiliation)
            return runtime.RecordFactionAffiliationObservation(affiliation);
        if (observation is OfficeVacancyKnowledgeObservation office) return runtime.RecordOfficeVacancyObservation(office);
        if (observation is PersonDeathKnowledgeObservation death) return runtime.RecordPersonDeathObservation(death);
        return false;
    }

    private static bool IsSupported(PoliticalKnowledgeObservation observation)
    {
        if (observation == null || observation.Provenance == null
            || !Enum.IsDefined(typeof(PoliticalKnowledgeFactKind), observation.FactKind)
            || !Enum.IsDefined(typeof(PoliticalKnowledgeSource), observation.Provenance.Source)) return false;
        return (observation.FactKind == PoliticalKnowledgeFactKind.PoliticalClaim && observation is PoliticalClaimKnowledgeObservation)
            || (observation.FactKind == PoliticalKnowledgeFactKind.Faction && observation is FactionKnowledgeObservation)
            || (observation.FactKind == PoliticalKnowledgeFactKind.FactionAffiliation && observation is FactionAffiliationKnowledgeObservation)
            || (observation.FactKind == PoliticalKnowledgeFactKind.OfficeVacancy && observation is OfficeVacancyKnowledgeObservation)
            || (observation.FactKind == PoliticalKnowledgeFactKind.PersonDeath && observation is PersonDeathKnowledgeObservation);
    }

    private static bool IsWireException(Exception exception) => exception is P12ALimitException
        || exception is IOException || exception is ArgumentException || exception is DecoderFallbackException
        || exception is OverflowException;

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool ReadNullableString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value);

    private static bool ReadCount(BinaryReader reader, int maximum, out int count)
    {
        count = reader.ReadInt32();
        return count >= 0 && count <= maximum;
    }

    private static void WriteBoolean(BinaryWriter writer, bool value) => writer.Write((byte)(value ? 1 : 0));

    private static bool ReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte raw = reader.ReadByte();
        if (raw > 1) return false;
        value = raw == 1;
        return true;
    }

    private static void WriteNullableLong(BinaryWriter writer, long? value)
    {
        writer.Write((byte)(value.HasValue ? 1 : 0));
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool ReadNullableLong(BinaryReader reader, out long? value)
    {
        value = null;
        byte present = reader.ReadByte();
        if (present > 1) return false;
        if (present == 1) value = reader.ReadInt64();
        return true;
    }

    private static bool ReadEnum<T>(BinaryReader reader, out T value) where T : struct, Enum
    {
        value = default;
        ushort raw = reader.ReadUInt16();
        T decoded = (T)Enum.ToObject(typeof(T), raw);
        if (!Enum.IsDefined(typeof(T), decoded)) return false;
        value = decoded;
        return true;
    }

    private static void WriteEnum<T>(BinaryWriter writer, T value) where T : struct, Enum
        => writer.Write(Convert.ToUInt16(value, System.Globalization.CultureInfo.InvariantCulture));

    private static int Compare(PoliticalKnowledgeObservation left, PoliticalKnowledgeObservation right)
    {
        int kind = ((int)left.FactKind).CompareTo((int)right.FactKind);
        if (kind != 0) return kind;
        int identity = StringComparer.Ordinal.Compare(left.IdentityKey, right.IdentityKey);
        return identity != 0 ? identity : StringComparer.Ordinal.Compare(left.SnapshotSortKey, right.SnapshotSortKey);
    }
}
