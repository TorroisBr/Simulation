using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Explicit Daily-v1 encoding for the fixed PersonStore membership and
/// materialization-binding census sections. The two sections together encode
/// the exact detached PersonStore owner snapshot; no runtime objects escape.
/// </summary>
internal static class P12APersonStoreOwnerCodec
{
    private const string MembershipContract = "p12a.person-membership/v1";
    private const string BindingContract = "p12a.person-materialization-binding/v1";
    private const string PersonOwnerPrefix = "person/";

    internal static bool TryEncode(PersonStoreOwnerSnapshot snapshot,
        out P12AOwnerSection membership, out P12AOwnerSection bindings, out P12AResult result)
    {
        membership = null;
        bindings = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        if (!TryValidateSnapshot(snapshot, out bool validationLimitExceeded))
        {
            if (validationLimitExceeded)
                result = new P12AResult(P12AResultCode.LimitExceeded,
                    DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
            return false;
        }

        try
        {
            List<IndexedPerson> people = new List<IndexedPerson>(snapshot.Rows.Count);
            for (int i = 0; i < snapshot.Rows.Count; i++)
                people.Add(new IndexedPerson(i, snapshot.Rows[i]));
            people.Sort((left, right) => StringComparer.Ordinal.Compare(
                OwnerKey(left.Row.PersonIdValue), OwnerKey(right.Row.PersonIdValue)));

            List<P12AOwnerRow> membershipRows = new List<P12AOwnerRow>(people.Count);
            List<P12AOwnerRow> bindingRows = new List<P12AOwnerRow>(snapshot.BindingCount);
            foreach (IndexedPerson person in people)
            {
                string ownerKey = OwnerKey(person.Row.PersonIdValue);
                P12AWire.GetEncodedStringByteCount(ownerKey);
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, MembershipContract);
                    writer.Write(person.Ordinal);
                    writer.Write(snapshot.MembershipCount);
                    writer.Write(snapshot.BindingCount);
                    P12AWire.WriteString(writer, person.Row.PersonIdValue);
                    WriteNullableInt64(writer, person.Row.BirthAbsoluteDay);
                    WriteNullableInt64(writer, person.Row.DeathAbsoluteDay);
                    P12AWire.WriteString(writer, person.Row.ResidenceSettlementRuntimeId);
                    writer.Write(person.Row.LifeResidenceRevision);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId, ownerKey);
                        return false;
                    }
                    // PersonStore exposes insertion order. The ordinal preserves it
                    // while the wire rows use deterministic stable-key ordering.
                    membershipRows.Add(new P12AOwnerRow(ownerKey, 0L, stream.ToArray()));
                }

                if (person.Row.MaterializedNpcRuntimeId != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                    {
                        P12AWire.WriteString(writer, BindingContract);
                        writer.Write(snapshot.MembershipCount);
                        writer.Write(snapshot.BindingCount);
                        P12AWire.WriteString(writer, person.Row.PersonIdValue);
                        P12AWire.WriteString(writer, person.Row.MaterializedNpcRuntimeId);
                        writer.Flush();
                        if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                        {
                            result = new P12AResult(P12AResultCode.LimitExceeded,
                                DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId, ownerKey);
                            return false;
                        }
                        bindingRows.Add(new P12AOwnerRow(ownerKey, 0L, stream.ToArray()));
                    }
                }
            }

            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
                DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
            membership = new P12AOwnerSection(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0, OwnerSectionRole.Required,
                alias, snapshot.Revision, membershipRows);
            bindings = new P12AOwnerSection(DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0, OwnerSectionRole.Required,
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(
                    DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId), snapshot.Revision, bindingRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            membership = null;
            bindings = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection membership, P12AOwnerSection bindings,
        out PersonStoreOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            membership?.Id ?? DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId);
        if (!IsSection(membership, DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion)
            || !IsSection(bindings, DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion)
            || membership.Revision != bindings.Revision)
            return false;

        try
        {
            List<MembershipRow> rows = new List<MembershipRow>(membership.Rows.Count);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> ordinals = new HashSet<int>();
            int? declaredMembershipCount = null;
            int? declaredBindingCount = null;
            string priorKey = null;
            foreach (P12AOwnerRow row in membership.Rows)
            {
                if (!TryReadMembershipRow(row, out MembershipRow value)
                    || row.OwnerKey != OwnerKey(value.PersonId)
                    || (priorKey != null && StringComparer.Ordinal.Compare(priorKey, row.OwnerKey) >= 0)
                    || !ids.Add(value.PersonId) || !ordinals.Add(value.Ordinal)
                    || (declaredMembershipCount.HasValue
                        && declaredMembershipCount.Value != value.DeclaredMembershipCount)
                    || (declaredBindingCount.HasValue
                        && declaredBindingCount.Value != value.DeclaredBindingCount))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId, row?.OwnerKey);
                    return false;
                }
                declaredMembershipCount = value.DeclaredMembershipCount;
                declaredBindingCount = value.DeclaredBindingCount;
                priorKey = row.OwnerKey;
                rows.Add(value);
            }
            if (rows.Count > 0 && declaredMembershipCount.Value != rows.Count) return false;
            if (ordinals.Count != rows.Count) return false;
            for (int i = 0; i < rows.Count; i++)
                if (!ordinals.Contains(i)) return false;

            Dictionary<string, string> npcByPerson = new Dictionary<string, string>(StringComparer.Ordinal);
            HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
            int? bindingMembershipCount = null;
            int? bindingRowCount = null;
            priorKey = null;
            foreach (P12AOwnerRow row in bindings.Rows)
            {
                if (!TryReadBindingRow(row, out int declaredPersonCount, out int declaredBindings,
                        out string personId, out string npcRuntimeId)
                    || row.OwnerKey != OwnerKey(personId)
                    || (priorKey != null && StringComparer.Ordinal.Compare(priorKey, row.OwnerKey) >= 0)
                    || !ids.Contains(personId) || !npcByPerson.TryAdd(personId, npcRuntimeId)
                    || !npcIds.Add(npcRuntimeId)
                    || (bindingMembershipCount.HasValue && bindingMembershipCount.Value != declaredPersonCount)
                    || (bindingRowCount.HasValue && bindingRowCount.Value != declaredBindings))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId, row?.OwnerKey);
                    return false;
                }
                bindingMembershipCount = declaredPersonCount;
                bindingRowCount = declaredBindings;
                priorKey = row.OwnerKey;
            }
            if (rows.Count > 0 && (declaredBindingCount.Value != npcByPerson.Count
                || bindingMembershipCount.HasValue && bindingMembershipCount.Value != rows.Count
                || bindingRowCount.HasValue && bindingRowCount.Value != npcByPerson.Count)) return false;

            List<PersonStoreOwnerSnapshotRow> personRows = new List<PersonStoreOwnerSnapshotRow>(rows.Count);
            foreach (MembershipRow row in rows.OrderBy(value => value.Ordinal))
            {
                npcByPerson.TryGetValue(row.PersonId, out string npcRuntimeId);
                personRows.Add(new PersonStoreOwnerSnapshotRow(row.PersonId,
                    row.BirthAbsoluteDay, row.DeathAbsoluteDay, row.ResidenceSettlementRuntimeId,
                    npcRuntimeId, row.LifeResidenceRevision));
            }

            PersonStoreOwnerSnapshot candidate = new PersonStoreOwnerSnapshot(
                PersonStoreOwnerSnapshot.CurrentSchemaId,
                PersonStoreOwnerSnapshot.CurrentSchemaVersion,
                membership.Revision, rows.Count, npcByPerson.Count, personRows);
            if (!TryValidateSnapshot(candidate, out _) || bindings.Rows.Count != candidate.BindingCount)
                return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            snapshot = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                membership.Id);
            return false;
        }
    }

    private static bool TryValidateSnapshot(PersonStoreOwnerSnapshot snapshot, out bool limitExceeded)
    {
        limitExceeded = false;
        if (snapshot == null || snapshot.SchemaId != PersonStoreOwnerSnapshot.CurrentSchemaId
            || snapshot.SchemaVersion != PersonStoreOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.Rows == null
            || snapshot.Rows.Count > P12AContinuationCodec.MaxRows
            || snapshot.MembershipCount != snapshot.Rows.Count
            || snapshot.BindingCount < 0 || snapshot.BindingCount > snapshot.MembershipCount
            || snapshot.Revision < (long)snapshot.MembershipCount + snapshot.BindingCount - 1L)
            return false;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        int bindingCount = 0;
        try
        {
            P12AWire.GetEncodedStringByteCount(
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId));
            foreach (PersonStoreOwnerSnapshotRow row in snapshot.Rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.PersonIdValue)
                    || row.LifeResidenceRevision < 0
                    || (row.BirthAbsoluteDay.HasValue && row.BirthAbsoluteDay.Value < 0)
                    || (row.DeathAbsoluteDay.HasValue && row.DeathAbsoluteDay.Value < 0)
                    || (row.BirthAbsoluteDay.HasValue && row.DeathAbsoluteDay.HasValue
                        && row.DeathAbsoluteDay.Value < row.BirthAbsoluteDay.Value)
                    || (row.ResidenceSettlementRuntimeId != null
                        && string.IsNullOrWhiteSpace(row.ResidenceSettlementRuntimeId))
                    || (row.MaterializedNpcRuntimeId != null
                        && string.IsNullOrWhiteSpace(row.MaterializedNpcRuntimeId))
                    || !ids.Add(row.PersonIdValue))
                    return false;

                P12AWire.GetEncodedStringByteCount(OwnerKey(row.PersonIdValue));
                P12AWire.GetEncodedStringByteCount(row.PersonIdValue);
                P12AWire.GetEncodedStringByteCount(row.ResidenceSettlementRuntimeId);
                if (row.MaterializedNpcRuntimeId != null)
                {
                    bindingCount++;
                    P12AWire.GetEncodedStringByteCount(row.MaterializedNpcRuntimeId);
                    if (!npcIds.Add(row.MaterializedNpcRuntimeId)) return false;
                }
            }
            P12AWire.GetEncodedStringByteCount(
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId));
        }
        catch (Exception exception) when (exception is P12ALimitException
            || exception is EncoderFallbackException || exception is ArgumentException)
        {
            limitExceeded = exception is P12ALimitException;
            return false;
        }
        return bindingCount == snapshot.BindingCount;
    }

    private static bool IsSection(P12AOwnerSection section, string id, int schemaVersion)
        => section != null && section.Id == id && section.Major == schemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && section.AliasGroup == DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(id)
            && section.Revision >= 0 && section.Rows.Count <= P12AContinuationCodec.MaxRows
            && section.Rows.All(row => row != null && row.Revision == 0L
                && row.PayloadLength >= 0 && row.PayloadLength <= P12AContinuationCodec.MaxSectionBytes);

    private static bool TryReadMembershipRow(P12AOwnerRow row, out MembershipRow value)
    {
        value = null;
        if (!TryOpen(row, out MemoryStream stream, out BinaryReader reader)) return false;
        using (stream)
        using (reader)
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != MembershipContract) return false;
            int ordinal = reader.ReadInt32();
            int declaredMembershipCount = reader.ReadInt32();
            int declaredBindingCount = reader.ReadInt32();
            if (ordinal < 0 || declaredMembershipCount < 0 || declaredBindingCount < 0
                || declaredBindingCount > declaredMembershipCount
                || !ReadRequiredString(reader, out string personId)
                || !ReadNullableInt64(reader, out long? birth)
                || !ReadNullableInt64(reader, out long? death)
                || !P12AWire.ReadString(reader, out string residence)
                || !ReadNonNegativeInt64(reader, out long localRevision)
                || stream.Position != stream.Length)
                return false;
            value = new MembershipRow(ordinal, declaredMembershipCount, declaredBindingCount,
                personId, birth, death, residence, localRevision);
            return IsValidMembershipValue(value);
        }
    }

    private static bool TryReadBindingRow(P12AOwnerRow row, out int declaredPersonCount,
        out int declaredBindings, out string personId, out string npcRuntimeId)
    {
        declaredPersonCount = -1;
        declaredBindings = -1;
        personId = null;
        npcRuntimeId = null;
        if (!TryOpen(row, out MemoryStream stream, out BinaryReader reader)) return false;
        using (stream)
        using (reader)
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != BindingContract)
                return false;
            declaredPersonCount = reader.ReadInt32();
            declaredBindings = reader.ReadInt32();
            if (declaredPersonCount < 0 || declaredBindings < 0
                || declaredBindings > declaredPersonCount
                || !ReadRequiredString(reader, out personId)
                || !ReadRequiredString(reader, out npcRuntimeId)
                || stream.Position != stream.Length)
                return false;
            P12AWire.GetEncodedStringByteCount(OwnerKey(personId));
            return true;
        }
    }

    private static bool TryOpen(P12AOwnerRow row, out MemoryStream stream, out BinaryReader reader)
    {
        stream = null;
        reader = null;
        if (row == null || row.Revision != 0L || row.Payload == null
            || row.PayloadLength != row.Payload.Length
            || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
            return false;
        P12AWire.GetEncodedStringByteCount(row.OwnerKey);
        stream = new MemoryStream(row.Payload, false);
        reader = new BinaryReader(stream, P12AWire.Utf8, true);
        return true;
    }

    private static bool IsValidMembershipValue(MembershipRow row)
        => row != null && !string.IsNullOrWhiteSpace(row.PersonId)
            && row.LifeResidenceRevision >= 0
            && (!row.BirthAbsoluteDay.HasValue || row.BirthAbsoluteDay.Value >= 0)
            && (!row.DeathAbsoluteDay.HasValue || row.DeathAbsoluteDay.Value >= 0)
            && (!row.BirthAbsoluteDay.HasValue || !row.DeathAbsoluteDay.HasValue
                || row.DeathAbsoluteDay.Value >= row.BirthAbsoluteDay.Value)
            && (row.ResidenceSettlementRuntimeId == null
                || !string.IsNullOrWhiteSpace(row.ResidenceSettlementRuntimeId));

    private static string OwnerKey(string personId)
    {
        if (personId == null) throw new ArgumentNullException(nameof(personId));
        return PersonOwnerPrefix + P12AWire.Utf8.GetByteCount(personId).ToString(
            System.Globalization.CultureInfo.InvariantCulture) + ":" + personId;
    }

    private static void WriteNullableInt64(BinaryWriter writer, long? value)
    {
        writer.Write(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool ReadNullableInt64(BinaryReader reader, out long? value)
    {
        value = null;
        byte present = reader.ReadByte();
        if (present == 0) return true;
        if (present != 1) return false;
        value = reader.ReadInt64();
        return true;
    }

    private static bool ReadNonNegativeInt64(BinaryReader reader, out long value)
    {
        value = reader.ReadInt64();
        return value >= 0;
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
    {
        return P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;

    private sealed class IndexedPerson
    {
        internal IndexedPerson(int ordinal, PersonStoreOwnerSnapshotRow row)
        { Ordinal = ordinal; Row = row; }
        internal int Ordinal { get; }
        internal PersonStoreOwnerSnapshotRow Row { get; }
    }

    private sealed class MembershipRow
    {
        internal MembershipRow(int ordinal, int declaredMembershipCount, int declaredBindingCount,
            string personId, long? birthAbsoluteDay,
            long? deathAbsoluteDay, string residenceSettlementRuntimeId, long lifeResidenceRevision)
        {
            Ordinal = ordinal;
            DeclaredMembershipCount = declaredMembershipCount;
            DeclaredBindingCount = declaredBindingCount;
            PersonId = personId;
            BirthAbsoluteDay = birthAbsoluteDay;
            DeathAbsoluteDay = deathAbsoluteDay;
            ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
            LifeResidenceRevision = lifeResidenceRevision;
        }
        internal int Ordinal { get; }
        internal int DeclaredMembershipCount { get; }
        internal int DeclaredBindingCount { get; }
        internal string PersonId { get; }
        internal long? BirthAbsoluteDay { get; }
        internal long? DeathAbsoluteDay { get; }
        internal string ResidenceSettlementRuntimeId { get; }
        internal long LifeResidenceRevision { get; }
    }
}
