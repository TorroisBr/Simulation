using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codecs for the Conflict, War, and Battle owner snapshots.</summary>
internal static class P12EPersistentConflictWarBattleOwnerCodec
{
    private const string PayloadContract = "p12a.persistent-conflict-war-battle/v1";
    private const byte ConflictKind = 1;
    private const byte WarKind = 2;
    private const byte BattleKind = 3;
    private const string ConflictPrefix = "conflict/";
    private const string WarPrefix = "war/";
    private const string BattlePrefix = "battle/";

    internal static bool TryEncode(PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars, PersistentBattleOwnerSnapshot battles,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = Invalid(PersistentConflictCensusProvider.SectionId);
        if (!TryEncodeConflicts(conflicts, out P12AOwnerSection conflictSection, out result)
            || !TryEncodeWars(wars, out P12AOwnerSection warSection, out result)
            || !TryEncodeBattles(battles, out P12AOwnerSection battleSection, out result)) return false;
        sections = Array.AsReadOnly(new[] { conflictSection, warSection, battleSection });
        result = null;
        return true;
    }

    internal static bool TryDecode(IReadOnlyList<P12AOwnerSection> sections,
        out PersistentConflictOwnerSnapshot conflicts, out PersistentWarOwnerSnapshot wars,
        out PersistentBattleOwnerSnapshot battles, out P12AResult result)
    {
        conflicts = null;
        wars = null;
        battles = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
        if (sections == null || sections.Count != 3) return false;
        Dictionary<string, P12AOwnerSection> map = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id) || map.ContainsKey(section.Id)) return false;
            map.Add(section.Id, section);
        }
        if (!TryGetSection(map, PersistentConflictCensusProvider.SectionId,
                PersistentConflictCensusProvider.SchemaVersion, out P12AOwnerSection conflictSection, out result)
            || !TryGetSection(map, PersistentWarCensusProvider.SectionId,
                PersistentWarCensusProvider.SchemaVersion, out P12AOwnerSection warSection, out result)
            || !TryGetSection(map, PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion, out P12AOwnerSection battleSection, out result))
            return false;
        try
        {
            List<PersistentConflictOwnerSnapshotRecord> conflictRows = DecodeRows(conflictSection,
                ConflictPrefix, ConflictKind, ReadConflict, row => row.ConflictIdValue, out result);
            if (conflictRows == null) return false;
            List<PersistentWarOwnerSnapshotRecord> warRows = DecodeRows(warSection,
                WarPrefix, WarKind, ReadWar, row => row.WarIdValue, out result);
            if (warRows == null) return false;
            List<PersistentBattleOwnerSnapshotRecord> battleRows = DecodeRows(battleSection,
                BattlePrefix, BattleKind, ReadBattle, row => row.BattleIdValue, out result);
            if (battleRows == null) return false;

            PersistentConflictOwnerSnapshot conflictSnapshot = new PersistentConflictOwnerSnapshot(
                PersistentConflictOwnerSnapshot.CurrentSchemaVersion, conflictRows.Count,
                conflictSection.Revision, conflictRows);
            PersistentWarOwnerSnapshot warSnapshot = new PersistentWarOwnerSnapshot(
                PersistentWarOwnerSnapshot.CurrentSchemaVersion, warRows.Count,
                warSection.Revision, warRows);
            PersistentBattleOwnerSnapshot battleSnapshot = new PersistentBattleOwnerSnapshot(
                PersistentBattleOwnerSnapshot.CurrentSchemaVersion, battleRows.Count,
                battleSection.Revision, battleRows);
            conflicts = conflictSnapshot;
            wars = warSnapshot;
            battles = battleSnapshot;
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

    private static bool TryEncodeConflicts(PersistentConflictOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(PersistentConflictCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != PersistentConflictOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0L || snapshot.RecordCount < 0 || snapshot.Records == null
            || snapshot.RecordCount != snapshot.Records.Count || snapshot.RecordCount > P12AContinuationCodec.MaxRows)
            return false;
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.RecordCount);
        string previous = null;
        try
        {
            foreach (PersistentConflictOwnerSnapshotRecord row in snapshot.Records)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.ConflictIdValue)
                    || !OrderedAfter(previous, row.ConflictIdValue) || !ValidConflict(row)) return false;
                previous = row.ConflictIdValue;
                rows.Add(WriteRow(ConflictPrefix + row.ConflictIdValue, ConflictKind, writer => WriteConflict(writer, row)));
            }
            return BuildSection(PersistentConflictCensusProvider.SectionId,
                PersistentConflictCensusProvider.SchemaVersion, snapshot.Revision, rows, out section, out result);
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PersistentConflictCensusProvider.SectionId);
            return false;
        }
    }

    private static bool TryEncodeWars(PersistentWarOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(PersistentWarCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != PersistentWarOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0L || snapshot.RecordCount < 0 || snapshot.Records == null
            || snapshot.RecordCount != snapshot.Records.Count || snapshot.RecordCount > P12AContinuationCodec.MaxRows)
            return false;
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.RecordCount);
        string previous = null;
        try
        {
            foreach (PersistentWarOwnerSnapshotRecord row in snapshot.Records)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.WarIdValue)
                    || !OrderedAfter(previous, row.WarIdValue) || !ValidWar(row)) return false;
                previous = row.WarIdValue;
                rows.Add(WriteRow(WarPrefix + row.WarIdValue, WarKind, writer => WriteWar(writer, row)));
            }
            return BuildSection(PersistentWarCensusProvider.SectionId,
                PersistentWarCensusProvider.SchemaVersion, snapshot.Revision, rows, out section, out result);
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PersistentWarCensusProvider.SectionId);
            return false;
        }
    }

    private static bool TryEncodeBattles(PersistentBattleOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(PersistentBattleCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != PersistentBattleOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0L || snapshot.RecordCount < 0 || snapshot.Records == null
            || snapshot.RecordCount != snapshot.Records.Count || snapshot.RecordCount > P12AContinuationCodec.MaxRows)
            return false;
        List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.RecordCount);
        string previous = null;
        try
        {
            foreach (PersistentBattleOwnerSnapshotRecord row in snapshot.Records)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.BattleIdValue)
                    || !OrderedAfter(previous, row.BattleIdValue) || !ValidBattle(row)) return false;
                previous = row.BattleIdValue;
                rows.Add(WriteRow(BattlePrefix + row.BattleIdValue, BattleKind, writer => WriteBattle(writer, row)));
            }
            return BuildSection(PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion, snapshot.Revision, rows, out section, out result);
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PersistentBattleCensusProvider.SectionId);
            return false;
        }
    }

    private static void WriteConflict(BinaryWriter writer, PersistentConflictOwnerSnapshotRecord row)
    {
        P12AWire.WriteString(writer, row.ConflictIdValue);
        writer.Write(row.CreatedAbsoluteDay);
        writer.Write((int)row.LifecycleState);
        WriteNullableLong(writer, row.EndedAbsoluteDay);
        writer.Write(row.Sides.Count);
        foreach (PersistentConflictOwnerSideSnapshot side in row.Sides)
        {
            P12AWire.WriteString(writer, side.ConflictIdValue);
            P12AWire.WriteString(writer, side.SideIdValue);
            P12AWire.WriteString(writer, side.DisplayName);
        }
        writer.Write(row.ParticipantBindings.Count);
        foreach (PersistentConflictOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            P12AWire.WriteString(writer, binding.BindingIdValue);
            P12AWire.WriteString(writer, binding.ConflictIdValue);
            P12AWire.WriteString(writer, binding.SideIdValue);
            P12AWire.WriteString(writer, binding.ArmedForceIdValue);
        }
    }

    private static void WriteWar(BinaryWriter writer, PersistentWarOwnerSnapshotRecord row)
    {
        P12AWire.WriteString(writer, row.WarIdValue);
        writer.Write(row.CreatedAbsoluteDay);
        writer.Write((int)row.LifecycleState);
        WriteNullableLong(writer, row.EndedAbsoluteDay);
        P12AWire.WriteString(writer, row.ConflictIdValue);
        writer.Write(row.Sides.Count);
        foreach (PersistentWarOwnerSideSnapshot side in row.Sides)
        {
            P12AWire.WriteString(writer, side.WarIdValue);
            P12AWire.WriteString(writer, side.SideIdValue);
            P12AWire.WriteString(writer, side.DisplayName);
        }
        writer.Write(row.ParticipantBindings.Count);
        foreach (PersistentWarOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            P12AWire.WriteString(writer, binding.BindingIdValue);
            P12AWire.WriteString(writer, binding.WarIdValue);
            P12AWire.WriteString(writer, binding.SideIdValue);
            P12AWire.WriteString(writer, binding.ArmedForceIdValue);
        }
    }

    private static void WriteBattle(BinaryWriter writer, PersistentBattleOwnerSnapshotRecord row)
    {
        P12AWire.WriteString(writer, row.BattleIdValue);
        writer.Write(row.CreatedAbsoluteDay);
        WriteNullableLong(writer, row.StartedAbsoluteDay);
        writer.Write((int)row.LifecycleState);
        P12AWire.WriteString(writer, row.ConflictIdValue);
        P12AWire.WriteString(writer, row.WarIdValue);
        writer.Write(row.LocationReference != null);
        if (row.LocationReference != null) WriteSpatial(writer, row.LocationReference);
        writer.Write(row.Sides.Count);
        foreach (PersistentBattleOwnerSideSnapshot side in row.Sides)
        {
            P12AWire.WriteString(writer, side.BattleIdValue);
            P12AWire.WriteString(writer, side.SideIdValue);
            P12AWire.WriteString(writer, side.DisplayName);
        }
        writer.Write(row.ParticipantBindings.Count);
        foreach (PersistentBattleOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            P12AWire.WriteString(writer, binding.BindingIdValue);
            P12AWire.WriteString(writer, binding.BattleIdValue);
            P12AWire.WriteString(writer, binding.SideIdValue);
            P12AWire.WriteString(writer, binding.ArmedForceIdValue);
        }
        writer.Write(row.TerminalOutcome != null);
        if (row.TerminalOutcome != null) WriteOutcome(writer, row.TerminalOutcome);
    }

    private static PersistentConflictOwnerSnapshotRecord ReadConflict(BinaryReader reader)
    {
        string id = RequiredString(reader);
        long created = reader.ReadInt64();
        ConflictLifecycleState lifecycle = ReadEnum<ConflictLifecycleState>(reader);
        long? ended = ReadNullableLong(reader);
        int sideCount = ReadCount(reader);
        List<PersistentConflictOwnerSideSnapshot> sides = new List<PersistentConflictOwnerSideSnapshot>(sideCount);
        for (int i = 0; i < sideCount; i++)
            sides.Add(new PersistentConflictOwnerSideSnapshot(RequiredString(reader), RequiredString(reader), NullableString(reader)));
        int bindingCount = ReadCount(reader);
        List<PersistentConflictOwnerBindingSnapshot> bindings = new List<PersistentConflictOwnerBindingSnapshot>(bindingCount);
        for (int i = 0; i < bindingCount; i++)
            bindings.Add(new PersistentConflictOwnerBindingSnapshot(RequiredString(reader), RequiredString(reader),
                RequiredString(reader), RequiredString(reader)));
        return new PersistentConflictOwnerSnapshotRecord(id, created, lifecycle, ended, sides, bindings);
    }

    private static PersistentWarOwnerSnapshotRecord ReadWar(BinaryReader reader)
    {
        string id = RequiredString(reader);
        long created = reader.ReadInt64();
        WarLifecycleState lifecycle = ReadEnum<WarLifecycleState>(reader);
        long? ended = ReadNullableLong(reader);
        string conflict = NullableString(reader);
        int sideCount = ReadCount(reader);
        List<PersistentWarOwnerSideSnapshot> sides = new List<PersistentWarOwnerSideSnapshot>(sideCount);
        for (int i = 0; i < sideCount; i++)
            sides.Add(new PersistentWarOwnerSideSnapshot(RequiredString(reader), RequiredString(reader), NullableString(reader)));
        int bindingCount = ReadCount(reader);
        List<PersistentWarOwnerBindingSnapshot> bindings = new List<PersistentWarOwnerBindingSnapshot>(bindingCount);
        for (int i = 0; i < bindingCount; i++)
            bindings.Add(new PersistentWarOwnerBindingSnapshot(RequiredString(reader), RequiredString(reader),
                RequiredString(reader), RequiredString(reader)));
        return new PersistentWarOwnerSnapshotRecord(id, created, lifecycle, ended, conflict, sides, bindings);
    }

    private static PersistentBattleOwnerSnapshotRecord ReadBattle(BinaryReader reader)
    {
        string id = RequiredString(reader);
        long created = reader.ReadInt64();
        long? started = ReadNullableLong(reader);
        BattleLifecycleState lifecycle = ReadEnum<BattleLifecycleState>(reader);
        string conflict = NullableString(reader), war = NullableString(reader);
        PersistentBattleOwnerSpatialReferenceSnapshot location = ReadBoolean(reader) ? ReadSpatial(reader) : null;
        int sideCount = ReadCount(reader);
        List<PersistentBattleOwnerSideSnapshot> sides = new List<PersistentBattleOwnerSideSnapshot>(sideCount);
        for (int i = 0; i < sideCount; i++)
            sides.Add(new PersistentBattleOwnerSideSnapshot(RequiredString(reader), RequiredString(reader), NullableString(reader)));
        int bindingCount = ReadCount(reader);
        List<PersistentBattleOwnerBindingSnapshot> bindings = new List<PersistentBattleOwnerBindingSnapshot>(bindingCount);
        for (int i = 0; i < bindingCount; i++)
            bindings.Add(new PersistentBattleOwnerBindingSnapshot(RequiredString(reader), RequiredString(reader),
                RequiredString(reader), RequiredString(reader)));
        PersistentBattleOwnerOutcomeSnapshot outcome = ReadBoolean(reader) ? ReadOutcome(reader) : null;
        return new PersistentBattleOwnerSnapshotRecord(id, created, started, lifecycle, conflict, war,
            location, sides, bindings, outcome);
    }

    private static void WriteSpatial(BinaryWriter writer, PersistentBattleOwnerSpatialReferenceSnapshot value)
    {
        writer.Write((int)value.Kind);
        P12AWire.WriteString(writer, value.HexIdValue);
        P12AWire.WriteString(writer, value.LocationIdValue);
        P12AWire.WriteString(writer, value.CrossingIdValue);
        writer.Write(value.TopologyOwnerKind.HasValue);
        if (value.TopologyOwnerKind.HasValue) writer.Write((int)value.TopologyOwnerKind.Value);
        P12AWire.WriteString(writer, value.TopologyOwnerRuntimeId);
        P12AWire.WriteString(writer, value.SubLocationRuntimeId);
    }

    private static PersistentBattleOwnerSpatialReferenceSnapshot ReadSpatial(BinaryReader reader)
    {
        SpatialReferenceKind kind = ReadEnum<SpatialReferenceKind>(reader);
        string hex = NullableString(reader), location = NullableString(reader), crossing = NullableString(reader);
        LocalTopologyOwnerKind? ownerKind = ReadBoolean(reader)
            ? (LocalTopologyOwnerKind?)ReadEnum<LocalTopologyOwnerKind>(reader) : null;
        string ownerId = NullableString(reader), subLocation = NullableString(reader);
        return new PersistentBattleOwnerSpatialReferenceSnapshot(kind, hex, location, crossing,
            ownerKind, ownerId, subLocation);
    }

    private static void WriteOutcome(BinaryWriter writer, PersistentBattleOwnerOutcomeSnapshot value)
    {
        P12AWire.WriteString(writer, value.BattleIdValue);
        writer.Write((int)value.OutcomeType);
        P12AWire.WriteString(writer, value.WinningBattleSideIdValue);
        writer.Write(value.ResolvedAbsoluteDay);
        writer.Write(value.Provenance != null);
        if (value.Provenance == null) return;
        P12AWire.WriteString(writer, value.Provenance.PolicyFingerprint);
        P12AWire.WriteString(writer, value.Provenance.NumericExecutionProfileKey);
        P12AWire.WriteString(writer, value.Provenance.ProjectionVersion);
        P12AWire.WriteString(writer, value.Provenance.CausalResolutionFingerprint);
        P12AWire.WriteString(writer, value.Provenance.SourceContextFingerprint);
        P12AWire.WriteString(writer, value.Provenance.CapabilityRuleKey);
        P12AWire.WriteString(writer, value.Provenance.RandomAuthorityRuleKey);
        P12AWire.WriteString(writer, value.Provenance.ResolverSettingsIdentity);
        P12AWire.WriteString(writer, value.Provenance.D6B2PolicyFingerprint);
        P12AWire.WriteString(writer, value.Provenance.D6B2PlanSchemaVersion);
        P12AWire.WriteString(writer, value.Provenance.D6B2CoverageVersion);
        P12AWire.WriteString(writer, value.Provenance.D6B2PlanFingerprint);
    }

    private static PersistentBattleOwnerOutcomeSnapshot ReadOutcome(BinaryReader reader)
    {
        string battle = RequiredString(reader);
        BattleOutcomeType outcome = ReadEnum<BattleOutcomeType>(reader);
        string winner = NullableString(reader);
        long day = reader.ReadInt64();
        PersistentBattleOwnerProvenanceSnapshot provenance = null;
        if (ReadBoolean(reader))
            provenance = new PersistentBattleOwnerProvenanceSnapshot(
                NullableString(reader), NullableString(reader), NullableString(reader), NullableString(reader),
                NullableString(reader), NullableString(reader), NullableString(reader), NullableString(reader),
                NullableString(reader), NullableString(reader), NullableString(reader), NullableString(reader));
        return new PersistentBattleOwnerOutcomeSnapshot(battle, outcome, winner, day, provenance);
    }

    private static List<T> DecodeRows<T>(P12AOwnerSection section, string prefix, byte kind,
        Func<BinaryReader, T> decode, Func<T, string> id, out P12AResult result)
    {
        result = Invalid(section?.Id);
        if (section.Rows.Count > P12AContinuationCodec.MaxRows) return null;
        List<T> values = new List<T>(section.Rows.Count);
        string previous = null;
        HashSet<string> ownerKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (row == null || row.Revision != 0L || row.Payload == null
                || !row.OwnerKey.StartsWith(prefix, StringComparison.Ordinal)
                || row.OwnerKey.Length == prefix.Length || !ownerKeys.Add(row.OwnerKey)) return null;
            byte[] payload = row.Payload;
            if (payload.Length > P12AContinuationCodec.MaxSectionBytes) return null;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (RequiredString(reader) != PayloadContract || reader.ReadByte() != kind) return null;
                T value = decode(reader);
                string stableId = id(value);
                if (stream.Position != stream.Length || string.IsNullOrWhiteSpace(stableId)
                    || !string.Equals(row.OwnerKey, prefix + stableId, StringComparison.Ordinal)
                    || !OrderedAfter(previous, stableId)) return null;
                previous = stableId;
                values.Add(value);
            }
        }
        result = null;
        return values;
    }

    private static bool ValidConflict(PersistentConflictOwnerSnapshotRecord row)
    {
        if (row.CreatedAbsoluteDay < 0L || !Enum.IsDefined(typeof(ConflictLifecycleState), row.LifecycleState)
            || (row.EndedAbsoluteDay.HasValue && (row.EndedAbsoluteDay.Value < row.CreatedAbsoluteDay
                || row.EndedAbsoluteDay.Value < 0L)) || row.Sides == null || row.ParticipantBindings == null)
            return false;
        foreach (PersistentConflictOwnerSideSnapshot side in row.Sides)
            if (side == null || side.ConflictIdValue != row.ConflictIdValue || string.IsNullOrWhiteSpace(side.SideIdValue)) return false;
        foreach (PersistentConflictOwnerBindingSnapshot binding in row.ParticipantBindings)
            if (binding == null || binding.ConflictIdValue != row.ConflictIdValue
                || string.IsNullOrWhiteSpace(binding.BindingIdValue) || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue)) return false;
        return true;
    }

    private static bool ValidWar(PersistentWarOwnerSnapshotRecord row)
    {
        if (row.CreatedAbsoluteDay < 0L || !Enum.IsDefined(typeof(WarLifecycleState), row.LifecycleState)
            || (row.EndedAbsoluteDay.HasValue && (row.EndedAbsoluteDay.Value < row.CreatedAbsoluteDay
                || row.EndedAbsoluteDay.Value < 0L)) || row.Sides == null || row.ParticipantBindings == null)
            return false;
        foreach (PersistentWarOwnerSideSnapshot side in row.Sides)
            if (side == null || side.WarIdValue != row.WarIdValue || string.IsNullOrWhiteSpace(side.SideIdValue)) return false;
        foreach (PersistentWarOwnerBindingSnapshot binding in row.ParticipantBindings)
            if (binding == null || binding.WarIdValue != row.WarIdValue
                || string.IsNullOrWhiteSpace(binding.BindingIdValue) || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue)) return false;
        return true;
    }

    private static bool ValidBattle(PersistentBattleOwnerSnapshotRecord row)
    {
        if (row.CreatedAbsoluteDay < 0L || !Enum.IsDefined(typeof(BattleLifecycleState), row.LifecycleState)
            || (row.StartedAbsoluteDay.HasValue && (row.StartedAbsoluteDay.Value < row.CreatedAbsoluteDay
                || row.StartedAbsoluteDay.Value < 0L)) || row.Sides == null || row.ParticipantBindings == null)
            return false;
        foreach (PersistentBattleOwnerSideSnapshot side in row.Sides)
            if (side == null || side.BattleIdValue != row.BattleIdValue || string.IsNullOrWhiteSpace(side.SideIdValue)) return false;
        foreach (PersistentBattleOwnerBindingSnapshot binding in row.ParticipantBindings)
            if (binding == null || binding.BattleIdValue != row.BattleIdValue
                || string.IsNullOrWhiteSpace(binding.BindingIdValue) || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue)) return false;
        if (row.LocationReference != null && !Enum.IsDefined(typeof(SpatialReferenceKind), row.LocationReference.Kind)) return false;
        if (row.TerminalOutcome != null && (row.TerminalOutcome.BattleIdValue != row.BattleIdValue
            || !Enum.IsDefined(typeof(BattleOutcomeType), row.TerminalOutcome.OutcomeType)
            || row.TerminalOutcome.ResolvedAbsoluteDay < 0L)) return false;
        return true;
    }

    private static bool BuildSection(string id, int schema, long revision,
        List<P12AOwnerRow> rows, out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(id);
        if (revision < 0L || rows.Count > P12AContinuationCodec.MaxRows)
        { result = new P12AResult(rows.Count > P12AContinuationCodec.MaxRows
            ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, id); return false; }
        long length = 4L;
        foreach (P12AOwnerRow row in rows)
            length = checked(length + P12AWire.GetEncodedStringFieldLength(row.OwnerKey) + 16L + row.PayloadLength);
        if (length > P12AContinuationCodec.MaxSectionBytes)
        { result = new P12AResult(P12AResultCode.LimitExceeded, id); return false; }
        section = new P12AOwnerSection(id, schema, 0, OwnerSectionRole.Required,
            P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision, rows);
        result = null;
        return true;
    }

    private static bool TryGetSection(Dictionary<string, P12AOwnerSection> map, string id, int schema,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        if (!map.TryGetValue(id, out section)) return false;
        if (section.Major != schema || section.Minor != 0)
        { result = new P12AResult(P12AResultCode.UnsupportedSchema, id); return false; }
        if (section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(id))
        { result = Invalid(id); return false; }
        result = null;
        return true;
    }

    private static P12AOwnerRow WriteRow(string key, byte kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writer.Write(kind);
            write(writer);
            writer.Flush();
            byte[] payload = stream.ToArray();
            if (payload.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(key, 0L, payload);
        }
    }

    private static void WriteNullableLong(BinaryWriter writer, long? value)
    { writer.Write(value.HasValue); if (value.HasValue) writer.Write(value.Value); }
    private static long? ReadNullableLong(BinaryReader reader) => ReadBoolean(reader) ? reader.ReadInt64() : (long?)null;
    private static bool ReadBoolean(BinaryReader reader)
    { byte value = reader.ReadByte(); if (value > 1) throw new InvalidDataException(); return value == 1; }
    private static int ReadCount(BinaryReader reader)
    { int value = reader.ReadInt32(); if (value < 0 || value > P12AContinuationCodec.MaxRows) throw new P12ALimitException(); return value; }
    private static string RequiredString(BinaryReader reader)
    { if (!P12AWire.ReadString(reader, out string value) || value == null) throw new InvalidDataException(); return value; }
    private static string NullableString(BinaryReader reader)
    { if (!P12AWire.ReadString(reader, out string value)) throw new InvalidDataException(); return value; }
    private static T ReadEnum<T>(BinaryReader reader) where T : struct, Enum
    { int value = reader.ReadInt32(); if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException(); return (T)Enum.ToObject(typeof(T), value); }
    private static bool OrderedAfter(string previous, string current) => previous == null
        || StringComparer.Ordinal.Compare(previous, current) < 0;
    private static P12AResult Invalid(string id = null) => new P12AResult(P12AResultCode.InvalidOwnerValue, id);
    private static bool IsWireException(Exception exception) => exception is ArgumentException
        || exception is InvalidDataException || exception is EndOfStreamException || exception is IOException
        || exception is P12ALimitException || exception is OverflowException || exception is DecoderFallbackException;
}
