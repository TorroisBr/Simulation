using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codec for all five selected military owner rows.</summary>
internal static class P12EMilitaryOwnerCodec
{
    private const string PayloadContract = "p12a.military-owners/v1";
    private const byte ForceKind = 1;
    private const byte ContingentKind = 2;
    private const byte RelevantPersonKind = 3;
    private const byte ManpowerKind = 4;
    private const byte PositionKind = 5;
    private const string ForcePrefix = "force/";
    private const string ContingentPrefix = "contingent/";
    private const string RelevantPersonPrefix = "person-reference/";
    private const string ManpowerPrefix = "manpower/";
    private const string PositionPrefix = "position/";

    internal static bool TryEncode(P12EMilitaryOwnerSnapshot snapshot,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.ArmedForceForcesSectionId);
        if (snapshot == null || !snapshot.TryValidateDocument(out _)) return false;
        if (snapshot.ForceCount > P12AContinuationCodec.MaxRows
            || snapshot.ContingentCount > P12AContinuationCodec.MaxRows
            || snapshot.RelevantPersonCount > P12AContinuationCodec.MaxRows
            || snapshot.ManpowerStateCount > P12AContinuationCodec.MaxRows
            || snapshot.PositionCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded);
            return false;
        }

        try
        {
            List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(5);
            if (!TryEncodeForces(snapshot, out P12AOwnerSection forces, out result)
                || !TryEncodeContingents(snapshot, out P12AOwnerSection contingents, out result)
                || !TryEncodeRelevantPersons(snapshot, out P12AOwnerSection persons, out result)
                || !TryEncodeManpower(snapshot, out P12AOwnerSection manpower, out result)
                || !TryEncodePositions(snapshot, out P12AOwnerSection positions, out result))
                return false;
            encoded.Add(forces);
            encoded.Add(contingents);
            encoded.Add(persons);
            encoded.Add(manpower);
            encoded.Add(positions);
            sections = encoded.AsReadOnly();
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            sections = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
            return false;
        }
    }

    internal static bool TryDecode(IReadOnlyList<P12AOwnerSection> sections,
        out P12EMilitaryOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
        if (sections == null || sections.Count != 5) return false;
        Dictionary<string, P12AOwnerSection> byId = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id) || byId.ContainsKey(section.Id)) return false;
            byId.Add(section.Id, section);
        }

        try
        {
            if (!TryGet(byId, DailyV1SelectedOwnerSectionCensusContract.ArmedForceForcesSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, out P12AOwnerSection forceSection, out result)
                || !TryGet(byId, DailyV1SelectedOwnerSectionCensusContract.ArmedForceContingentsSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, out P12AOwnerSection contingentSection, out result)
                || !TryGet(byId, DailyV1SelectedOwnerSectionCensusContract.ArmedForceRelevantPeopleSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, out P12AOwnerSection personSection, out result)
                || !TryGet(byId, DailyV1SelectedOwnerSectionCensusContract.ContingentManpowerStatesSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, out P12AOwnerSection manpowerSection, out result)
                || !TryGet(byId, DailyV1SelectedOwnerSectionCensusContract.ArmedForceSpatialPositionsSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, out P12AOwnerSection positionSection, out result))
                return false;

            if (forceSection.Revision != contingentSection.Revision
                || forceSection.Revision != personSection.Revision)
            {
                result = Invalid(DailyV1SelectedOwnerSectionCensusContract.ArmedForceForcesSectionId);
                return false;
            }

            List<P12EMilitaryForceSnapshotRecord> forces = DecodeRows(forceSection, ForcePrefix, ForceKind,
                DecodeForce, out result);
            if (forces == null) return false;
            List<P12EMilitaryContingentSnapshotRecord> contingents = DecodeRows(contingentSection,
                ContingentPrefix, ContingentKind, DecodeContingent, out result);
            if (contingents == null) return false;
            List<P12EMilitaryRelevantPersonSnapshotRecord> persons = DecodeRows(personSection,
                RelevantPersonPrefix, RelevantPersonKind, DecodeRelevantPerson, out result);
            if (persons == null) return false;
            List<P12EManpowerStateSnapshotRecord> manpower = DecodeRows(manpowerSection,
                ManpowerPrefix, ManpowerKind, DecodeManpower, out result);
            if (manpower == null) return false;
            List<P12EForcePositionSnapshotRecord> positions = DecodeRows(positionSection,
                PositionPrefix, PositionKind, DecodePosition, out result);
            if (positions == null) return false;

            P12EMilitaryOwnerSnapshot candidate = new P12EMilitaryOwnerSnapshot(
                P12EMilitaryOwnerSnapshot.CurrentSchemaVersion,
                forceSection.Revision, manpowerSection.Revision, positionSection.Revision,
                forces.Count, contingents.Count, persons.Count, manpower.Count, positions.Count,
                forces, contingents, persons, manpower, positions);
            if (!candidate.TryValidateDocument(out _))
            {
                result = Invalid(DailyV1SelectedOwnerSectionCensusContract.ArmedForceForcesSectionId);
                return false;
            }
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
            return false;
        }
    }

    private static bool TryEncodeForces(P12EMilitaryOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Forces.Count);
        foreach (P12EMilitaryForceSnapshotRecord value in snapshot.Forces)
        {
            P12EMilitaryForceSnapshotRecord row = value;
            rows.Add(Encode(ForcePrefix + row.ForceIdValue, 0L, ForceKind, writer =>
            {
                P12AWire.WriteString(writer, row.ForceIdValue);
                P12AWire.WriteString(writer, row.DisplayName);
                writer.Write(row.CreatedAbsoluteDay);
                P12AWire.WriteString(writer, row.ParentForceIdValue);
                P12AWire.WriteString(writer, row.OperationalLocationReference);
                P12AWire.WriteString(writer, row.CommanderPersonIdValue);
                writer.Write((int)row.LifecycleState);
                WriteNullableLong(writer, row.TerminatedAbsoluteDay);
                writer.Write(row.IsDetached);
            }));
        }
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.ArmedForceForcesSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.ArmedForceRevision, rows, out section, out result);
    }

    private static bool TryEncodeContingents(P12EMilitaryOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Contingents.Count);
        foreach (P12EMilitaryContingentSnapshotRecord value in snapshot.Contingents)
        {
            P12EMilitaryContingentSnapshotRecord row = value;
            rows.Add(Encode(ContingentPrefix + row.ContingentIdValue, 0L, ContingentKind, writer =>
            {
                P12AWire.WriteString(writer, row.ContingentIdValue);
                P12AWire.WriteString(writer, row.ForceIdValue);
                writer.Write(row.Amount);
                P12AWire.WriteString(writer, row.OriginDomain);
                P12AWire.WriteString(writer, row.OriginValue);
                P12AWire.WriteString(writer, row.ServiceType);
                writer.Write(row.Characteristics.Count);
                foreach (P12EMilitaryCharacteristicSnapshot characteristic in row.Characteristics)
                { P12AWire.WriteString(writer, characteristic.Key); P12AWire.WriteString(writer, characteristic.Value); }
            }));
        }
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.ArmedForceContingentsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.ArmedForceRevision, rows, out section, out result);
    }

    private static bool TryEncodeRelevantPersons(P12EMilitaryOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.RelevantPersons.Count);
        foreach (P12EMilitaryRelevantPersonSnapshotRecord value in snapshot.RelevantPersons)
        {
            P12EMilitaryRelevantPersonSnapshotRecord row = value;
            rows.Add(Encode(RelevantPersonPrefix + row.ReferenceIdValue, 0L, RelevantPersonKind, writer =>
            {
                P12AWire.WriteString(writer, row.ReferenceIdValue);
                P12AWire.WriteString(writer, row.ForceIdValue);
                P12AWire.WriteString(writer, row.PersonIdValue);
                P12AWire.WriteString(writer, row.RoleKey);
            }));
        }
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.ArmedForceRelevantPeopleSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.ArmedForceRevision, rows, out section, out result);
    }

    private static bool TryEncodeManpower(P12EMilitaryOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.ManpowerStates.Count);
        foreach (P12EManpowerStateSnapshotRecord value in snapshot.ManpowerStates)
        {
            P12EManpowerStateSnapshotRecord row = value;
            rows.Add(Encode(ManpowerPrefix + row.ContingentIdValue, row.Revision, ManpowerKind, writer =>
            {
                P12AWire.WriteString(writer, row.ContingentIdValue);
                P12AWire.WriteString(writer, row.SourceIdValue);
                writer.Write(row.Cohorts.Count);
                foreach (P12EManpowerCohortSnapshotRecord cohort in row.Cohorts)
                {
                    writer.Write((int)cohort.InjuryState);
                    writer.Write((int)cohort.CustodyState);
                    P12AWire.WriteString(writer, cohort.CustodianForceIdValue);
                    writer.Write((int)cohort.AvailabilityState);
                    writer.Write(cohort.Amount);
                }
            }));
        }
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.ContingentManpowerStatesSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.ManpowerRevision, rows, out section, out result);
    }

    private static bool TryEncodePositions(P12EMilitaryOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Positions.Count);
        foreach (P12EForcePositionSnapshotRecord value in snapshot.Positions)
        {
            P12EForcePositionSnapshotRecord row = value;
            rows.Add(Encode(PositionPrefix + row.ForceIdValue, 0L, PositionKind, writer =>
            {
                P12AWire.WriteString(writer, row.ForceIdValue);
                WriteSpatialReference(writer, row.Reference);
            }));
        }
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.ArmedForceSpatialPositionsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.PositionRevision, rows, out section, out result);
    }

    private static P12EMilitaryForceSnapshotRecord DecodeForce(BinaryReader reader)
    {
        string id = RequiredString(reader);
        string name = P12AWire.ReadString(reader, out string n) ? n : throw new InvalidDataException();
        long created = reader.ReadInt64();
        string parent = NullableString(reader);
        string location = NullableString(reader);
        string commander = NullableString(reader);
        ArmedForceLifecycleState lifecycle = ReadEnum<ArmedForceLifecycleState>(reader);
        long? terminated = ReadNullableLong(reader);
        bool detached = ReadBoolean(reader);
        return new P12EMilitaryForceSnapshotRecord(id, name, created, parent, location, commander,
            lifecycle, terminated, detached);
    }

    private static P12EMilitaryContingentSnapshotRecord DecodeContingent(BinaryReader reader)
    {
        string id = RequiredString(reader), force = RequiredString(reader);
        long amount = reader.ReadInt64();
        string originDomain = RequiredString(reader), origin = RequiredString(reader), service = RequiredString(reader);
        int count = ReadCount(reader);
        List<P12EMilitaryCharacteristicSnapshot> characteristics = new List<P12EMilitaryCharacteristicSnapshot>(count);
        for (int i = 0; i < count; i++)
            characteristics.Add(new P12EMilitaryCharacteristicSnapshot(RequiredString(reader), RequiredString(reader)));
        return new P12EMilitaryContingentSnapshotRecord(id, force, amount, originDomain, origin, service, characteristics);
    }

    private static P12EMilitaryRelevantPersonSnapshotRecord DecodeRelevantPerson(BinaryReader reader) =>
        new P12EMilitaryRelevantPersonSnapshotRecord(RequiredString(reader), RequiredString(reader),
            RequiredString(reader), RequiredString(reader));

    private static P12EManpowerStateSnapshotRecord DecodeManpower(BinaryReader reader)
    {
        string contingent = RequiredString(reader), source = NullableString(reader);
        int count = ReadCount(reader);
        List<P12EManpowerCohortSnapshotRecord> cohorts = new List<P12EManpowerCohortSnapshotRecord>(count);
        for (int i = 0; i < count; i++)
        {
            ManpowerInjuryState injury = ReadEnum<ManpowerInjuryState>(reader);
            ManpowerCustodyState custody = ReadEnum<ManpowerCustodyState>(reader);
            string custodian = NullableString(reader);
            ManpowerAvailabilityState availability = ReadEnum<ManpowerAvailabilityState>(reader);
            long amount = reader.ReadInt64();
            cohorts.Add(new P12EManpowerCohortSnapshotRecord(injury, custody, custodian, availability, amount));
        }
        return new P12EManpowerStateSnapshotRecord(contingent, source, 0L, cohorts);
    }

    private static P12EForcePositionSnapshotRecord DecodePosition(BinaryReader reader)
    {
        string force = RequiredString(reader);
        SpatialReferenceKind kind = ReadEnum<SpatialReferenceKind>(reader);
        string hex = NullableString(reader), location = NullableString(reader), crossing = NullableString(reader);
        LocalTopologyOwnerKind? topologyKind = ReadNullableEnum<LocalTopologyOwnerKind>(reader);
        string topologyOwner = NullableString(reader), subLocation = NullableString(reader);
        return new P12EForcePositionSnapshotRecord(force,
            new P12ESpatialReferenceSnapshot(kind, hex, location, crossing, topologyKind, topologyOwner, subLocation));
    }

    private static List<T> DecodeRows<T>(P12AOwnerSection section, string keyPrefix, byte kind,
        Func<BinaryReader, T> decode, out P12AResult result)
    {
        result = Invalid(section?.Id);
        if (section == null || section.Rows.Count > P12AContinuationCodec.MaxRows) return null;
        List<T> values = new List<T>(section.Rows.Count);
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (row == null || row.Revision < 0L || row.Payload == null
                || !row.OwnerKey.StartsWith(keyPrefix, StringComparison.Ordinal)
                || row.OwnerKey.Length == keyPrefix.Length || !keys.Add(row.OwnerKey)) return null;
            byte[] payload = row.Payload;
            if (payload.Length > P12AContinuationCodec.MaxSectionBytes) return null;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (RequiredString(reader) != PayloadContract || reader.ReadByte() != kind) return null;
                T value = decode(reader);
                if (stream.Position != stream.Length || !OwnerKeyMatches(row.OwnerKey, keyPrefix, value)) return null;
                if (keyPrefix == ManpowerPrefix)
                {
                    P12EManpowerStateSnapshotRecord manpower = (P12EManpowerStateSnapshotRecord)(object)value;
                    if (row.Revision < 0L) return null;
                    value = (T)(object)new P12EManpowerStateSnapshotRecord(manpower.ContingentIdValue,
                        manpower.SourceIdValue, row.Revision, manpower.Cohorts);
                }
                else if (row.Revision != 0L) return null;
                values.Add(value);
            }
        }
        result = null;
        return values;
    }

    private static bool OwnerKeyMatches<T>(string key, string prefix, T value)
    {
        string id = value is P12EMilitaryForceSnapshotRecord force ? force.ForceIdValue
            : value is P12EMilitaryContingentSnapshotRecord contingent ? contingent.ContingentIdValue
            : value is P12EMilitaryRelevantPersonSnapshotRecord person ? person.ReferenceIdValue
            : value is P12EManpowerStateSnapshotRecord manpower ? manpower.ContingentIdValue
            : value is P12EForcePositionSnapshotRecord position ? position.ForceIdValue : null;
        return id != null && string.Equals(key, prefix + id, StringComparison.Ordinal);
    }

    private static P12AOwnerRow Encode(string key, long revision, byte kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writer.Write(kind);
            write(writer);
            writer.Flush();
            byte[] bytes = stream.ToArray();
            if (bytes.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(key, revision, bytes);
        }
    }

    private static bool BuildSection(string id, int schema, long revision, List<P12AOwnerRow> rows,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(id);
        if (revision < 0L || rows == null || rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = rows != null && rows.Count > P12AContinuationCodec.MaxRows
                ? new P12AResult(P12AResultCode.LimitExceeded, id) : Invalid(id);
            return false;
        }
        long total = 4L;
        foreach (P12AOwnerRow row in rows)
            total = checked(total + 4L + P12AWire.GetEncodedStringByteCount(row.OwnerKey)
                + 8L + 4L + row.PayloadLength);
        if (total > P12AContinuationCodec.MaxSectionBytes)
        { result = new P12AResult(P12AResultCode.LimitExceeded, id); return false; }
        section = new P12AOwnerSection(id, schema, 0, OwnerSectionRole.Required,
            P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision, rows);
        result = null;
        return true;
    }

    private static bool TryGet(Dictionary<string, P12AOwnerSection> sections, string id, int schema,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        if (!sections.TryGetValue(id, out section)) return false;
        if (section.Major != schema || section.Minor != 0)
        { result = new P12AResult(P12AResultCode.UnsupportedSchema, id); return false; }
        if (section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(id))
        { result = Invalid(id); return false; }
        result = null;
        return true;
    }

    private static void WriteSpatialReference(BinaryWriter writer, P12ESpatialReferenceSnapshot value)
    {
        if (value == null) throw new ArgumentException("Spatial reference required.");
        writer.Write((int)value.Kind);
        P12AWire.WriteString(writer, value.HexIdValue);
        P12AWire.WriteString(writer, value.LocationIdValue);
        P12AWire.WriteString(writer, value.CrossingIdValue);
        writer.Write(value.TopologyOwnerKind.HasValue);
        if (value.TopologyOwnerKind.HasValue) writer.Write((int)value.TopologyOwnerKind.Value);
        P12AWire.WriteString(writer, value.TopologyOwnerRuntimeId);
        P12AWire.WriteString(writer, value.SubLocationRuntimeId);
    }

    private static int ReadCount(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        return count;
    }

    private static string RequiredString(BinaryReader reader)
    {
        if (!P12AWire.ReadString(reader, out string value) || value == null) throw new InvalidDataException();
        return value;
    }

    private static string NullableString(BinaryReader reader)
    {
        if (!P12AWire.ReadString(reader, out string value)) throw new InvalidDataException();
        return value;
    }

    private static bool ReadBoolean(BinaryReader reader)
    {
        byte value = reader.ReadByte();
        if (value > 1) throw new InvalidDataException();
        return value == 1;
    }

    private static void WriteNullableLong(BinaryWriter writer, long? value)
    { writer.Write(value.HasValue); if (value.HasValue) writer.Write(value.Value); }

    private static long? ReadNullableLong(BinaryReader reader) => ReadBoolean(reader) ? reader.ReadInt64() : (long?)null;

    private static T ReadEnum<T>(BinaryReader reader) where T : struct, Enum
    {
        int value = reader.ReadInt32();
        if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException();
        return (T)Enum.ToObject(typeof(T), value);
    }

    private static T? ReadNullableEnum<T>(BinaryReader reader) where T : struct, Enum
    {
        if (!ReadBoolean(reader)) return null;
        return ReadEnum<T>(reader);
    }

    private static P12AResult Invalid(string id = null) => new P12AResult(P12AResultCode.InvalidOwnerValue, id);

    private static bool IsWireException(Exception exception) => exception is ArgumentException
        || exception is InvalidDataException || exception is EndOfStreamException || exception is IOException
        || exception is P12ALimitException || exception is OverflowException
        || exception is DecoderFallbackException;

}
