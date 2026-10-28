using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

internal sealed class P12AReceiptValue
{
    internal string Identity { get; }
    internal string DescriptorFingerprint { get; }
    internal long RevisionBefore { get; }
    internal long RevisionAfter { get; }

    internal P12AReceiptValue(string identity, string descriptorFingerprint, long revisionBefore, long revisionAfter)
    {
        Identity = identity;
        DescriptorFingerprint = descriptorFingerprint;
        RevisionBefore = revisionBefore;
        RevisionAfter = revisionAfter;
    }
}

internal sealed class P12ACrimeReceiptOwnerSnapshot
{
    internal long StepRevision { get; }
    internal long CensusRevision { get; }
    internal IReadOnlyList<P12AReceiptValue> Receipts { get; }

    internal P12ACrimeReceiptOwnerSnapshot(long stepRevision, IEnumerable<P12AReceiptValue> receipts)
    {
        StepRevision = stepRevision;
        List<P12AReceiptValue> copy = Copy(receipts);
        Receipts = new ReadOnlyCollection<P12AReceiptValue>(copy);
        CensusRevision = checked(StepRevision + copy.Count);
    }

    internal bool IsValid => StepRevision >= 0L && ValidateReceipts(Receipts, StepRevision);

    private static List<P12AReceiptValue> Copy(IEnumerable<P12AReceiptValue> values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        List<P12AReceiptValue> copy = new List<P12AReceiptValue>();
        foreach (P12AReceiptValue value in values)
            copy.Add(value == null ? null : new P12AReceiptValue(value.Identity,
                value.DescriptorFingerprint, value.RevisionBefore, value.RevisionAfter));
        return copy;
    }

    internal static bool ValidateReceipts(IReadOnlyList<P12AReceiptValue> values, long revision)
    {
        if (values == null || revision < 0L || (long)values.Count != revision) return false;
        string previous = null;
        HashSet<long> observedRevisions = new HashSet<long>();
        foreach (P12AReceiptValue value in values)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Identity)
                || string.IsNullOrWhiteSpace(value.DescriptorFingerprint)
                || value.RevisionBefore < 0L || value.RevisionBefore == long.MaxValue
                || value.RevisionAfter != value.RevisionBefore + 1L
                || value.RevisionAfter > revision
                || !observedRevisions.Add(value.RevisionBefore)
                || (previous != null && StringComparer.Ordinal.Compare(previous, value.Identity) >= 0)) return false;
            previous = value.Identity;
        }
        return true;
    }
}

internal sealed class P12AJusticeReceiptOwnerSnapshot
{
    internal long BeginDayRevision { get; }
    internal long AdvanceSentencesRevision { get; }
    internal long SyncWantedStatusesRevision { get; }
    internal long CensusRevision { get; }
    internal IReadOnlyList<P12AReceiptValue> BeginDayReceipts { get; }
    internal IReadOnlyList<P12AReceiptValue> AdvanceSentencesReceipts { get; }
    internal IReadOnlyList<P12AReceiptValue> SyncWantedStatusesReceipts { get; }

    internal P12AJusticeReceiptOwnerSnapshot(long beginDayRevision,
        IEnumerable<P12AReceiptValue> beginDayReceipts,
        long advanceSentencesRevision,
        IEnumerable<P12AReceiptValue> advanceSentencesReceipts,
        long syncWantedStatusesRevision,
        IEnumerable<P12AReceiptValue> syncWantedStatusesReceipts)
    {
        BeginDayRevision = beginDayRevision;
        AdvanceSentencesRevision = advanceSentencesRevision;
        SyncWantedStatusesRevision = syncWantedStatusesRevision;
        BeginDayReceipts = Copy(beginDayReceipts);
        AdvanceSentencesReceipts = Copy(advanceSentencesReceipts);
        SyncWantedStatusesReceipts = Copy(syncWantedStatusesReceipts);
        CensusRevision = checked(checked(checked(BeginDayRevision + BeginDayReceipts.Count)
            + checked(AdvanceSentencesRevision + AdvanceSentencesReceipts.Count))
            + checked(SyncWantedStatusesRevision + SyncWantedStatusesReceipts.Count));
    }

    internal bool IsValid => BeginDayRevision >= 0L && AdvanceSentencesRevision >= 0L
        && SyncWantedStatusesRevision >= 0L
        && P12ACrimeReceiptOwnerSnapshot.ValidateReceipts(BeginDayReceipts, BeginDayRevision)
        && P12ACrimeReceiptOwnerSnapshot.ValidateReceipts(AdvanceSentencesReceipts, AdvanceSentencesRevision)
        && P12ACrimeReceiptOwnerSnapshot.ValidateReceipts(SyncWantedStatusesReceipts, SyncWantedStatusesRevision);

    private static IReadOnlyList<P12AReceiptValue> Copy(IEnumerable<P12AReceiptValue> values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        List<P12AReceiptValue> copy = new List<P12AReceiptValue>();
        foreach (P12AReceiptValue value in values)
            copy.Add(value == null ? null : new P12AReceiptValue(value.Identity,
                value.DescriptorFingerprint, value.RevisionBefore, value.RevisionAfter));
        return new ReadOnlyCollection<P12AReceiptValue>(copy);
    }
}

/// <summary>Explicit serialization for the two selected P12-B idempotency receipt owners.</summary>
internal static class P12AReceiptOwnerCodec
{
    private const string PayloadContract = "p12a.p18-receipt-owners/v1";
    private const string CrimeOwnerKey = "crime-p18-receipts";
    private const string JusticeOwnerKey = "justice-p18-receipts";

    internal static bool TryEncode(P12ACrimeReceiptOwnerSnapshot crime,
        P12AJusticeReceiptOwnerSnapshot justice, out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (crime == null || justice == null || !crime.IsValid || !justice.IsValid)
            return false;
        try
        {
            if (!TryEncodeCrime(crime, out P12AOwnerSection crimeSection, out result)
                || !TryEncodeJustice(justice, out P12AOwnerSection justiceSection, out result)) return false;
            sections = Array.AsReadOnly(new[] { crimeSection, justiceSection });
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

    internal static bool TryDecode(IReadOnlyList<P12AOwnerSection> sections,
        out P12ACrimeReceiptOwnerSnapshot crime, out P12AJusticeReceiptOwnerSnapshot justice,
        out P12AResult result)
    {
        crime = null;
        justice = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
        if (sections == null || sections.Count != 2) return false;
        P12AOwnerSection crimeSection = null, justiceSection = null;
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null) return false;
            if (section.Id == DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId)
            { if (crimeSection != null) return false; crimeSection = section; }
            else if (section.Id == DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId)
            { if (justiceSection != null) return false; justiceSection = section; }
            else return false;
        }
        if (!TryValidateSection(crimeSection, DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId,
                out result)
            || !TryValidateSection(justiceSection, DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
                out result)) return false;
        try
        {
            if (!TryReadCrime(crimeSection, out P12ACrimeReceiptOwnerSnapshot crimeSnapshot)
                || crimeSnapshot.CensusRevision != crimeSection.Revision
                || !TryReadJustice(justiceSection, out P12AJusticeReceiptOwnerSnapshot justiceSnapshot)
                || justiceSnapshot.CensusRevision != justiceSection.Revision)
            {
                result = Invalid();
                return false;
            }
            crime = crimeSnapshot;
            justice = justiceSnapshot;
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

    private static bool TryEncodeCrime(P12ACrimeReceiptOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(id: DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);
        if (snapshot.Receipts.Count > P12AContinuationCodec.MaxRows)
            return Limit(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId, out result);
        P12AOwnerRow row = EncodeRow(CrimeOwnerKey, snapshot.CensusRevision, writer =>
        {
            writer.Write(snapshot.StepRevision);
            WriteReceipts(writer, snapshot.Receipts);
        });
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.CensusRevision, row, out section, out result);
    }

    private static bool TryEncodeJustice(P12AJusticeReceiptOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(id: DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        long count = (long)snapshot.BeginDayReceipts.Count + snapshot.AdvanceSentencesReceipts.Count
            + snapshot.SyncWantedStatusesReceipts.Count;
        if (count > P12AContinuationCodec.MaxRows)
            return Limit(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, out result);
        P12AOwnerRow row = EncodeRow(JusticeOwnerKey, snapshot.CensusRevision, writer =>
        {
            writer.Write(snapshot.BeginDayRevision);
            WriteReceipts(writer, snapshot.BeginDayReceipts);
            writer.Write(snapshot.AdvanceSentencesRevision);
            WriteReceipts(writer, snapshot.AdvanceSentencesReceipts);
            writer.Write(snapshot.SyncWantedStatusesRevision);
            WriteReceipts(writer, snapshot.SyncWantedStatusesReceipts);
        });
        return BuildSection(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion, snapshot.CensusRevision, row, out section, out result);
    }

    private static bool TryReadCrime(P12AOwnerSection section, out P12ACrimeReceiptOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (section.Rows.Count != 1 || section.Rows[0].OwnerKey != CrimeOwnerKey
            || section.Rows[0].Revision != section.Revision) return false;
        using (MemoryStream stream = new MemoryStream(section.Rows[0].Payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (RequiredString(reader) != PayloadContract) return false;
            long revision = reader.ReadInt64();
            List<P12AReceiptValue> receipts = ReadReceipts(reader);
            if (stream.Position != stream.Length) return false;
            P12ACrimeReceiptOwnerSnapshot candidate = new P12ACrimeReceiptOwnerSnapshot(revision, receipts);
            if (!candidate.IsValid) return false;
            snapshot = candidate;
            return true;
        }
    }

    private static bool TryReadJustice(P12AOwnerSection section, out P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (section.Rows.Count != 1 || section.Rows[0].OwnerKey != JusticeOwnerKey
            || section.Rows[0].Revision != section.Revision) return false;
        using (MemoryStream stream = new MemoryStream(section.Rows[0].Payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (RequiredString(reader) != PayloadContract) return false;
            long beginRevision = reader.ReadInt64();
            List<P12AReceiptValue> begin = ReadReceipts(reader);
            long advanceRevision = reader.ReadInt64();
            List<P12AReceiptValue> advance = ReadReceipts(reader);
            long syncRevision = reader.ReadInt64();
            List<P12AReceiptValue> sync = ReadReceipts(reader);
            if (stream.Position != stream.Length) return false;
            P12AJusticeReceiptOwnerSnapshot candidate = new P12AJusticeReceiptOwnerSnapshot(
                beginRevision, begin, advanceRevision, advance, syncRevision, sync);
            if (!candidate.IsValid) return false;
            snapshot = candidate;
            return true;
        }
    }

    private static void WriteReceipts(BinaryWriter writer, IReadOnlyList<P12AReceiptValue> receipts)
    {
        if (receipts == null || receipts.Count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        writer.Write(receipts.Count);
        foreach (P12AReceiptValue receipt in receipts)
        {
            P12AWire.WriteString(writer, receipt.Identity);
            P12AWire.WriteString(writer, receipt.DescriptorFingerprint);
            writer.Write(receipt.RevisionBefore);
            writer.Write(receipt.RevisionAfter);
        }
    }

    private static List<P12AReceiptValue> ReadReceipts(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > P12AContinuationCodec.MaxRows) throw new P12ALimitException();
        List<P12AReceiptValue> receipts = new List<P12AReceiptValue>(count);
        for (int i = 0; i < count; i++)
            receipts.Add(new P12AReceiptValue(RequiredString(reader), RequiredString(reader),
                reader.ReadInt64(), reader.ReadInt64()));
        return receipts;
    }

    private static P12AOwnerRow EncodeRow(string ownerKey, long revision, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            write(writer);
            writer.Flush();
            byte[] payload = stream.ToArray();
            if (payload.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(ownerKey, revision, payload);
        }
    }

    private static bool BuildSection(string id, int schema, long revision, P12AOwnerRow row,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = Invalid(id: id);
        if (revision < 0L || row == null || row.Revision != revision
            || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
        {
            result = new P12AResult(row?.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, id);
            return false;
        }
        section = new P12AOwnerSection(id, schema, 0, OwnerSectionRole.Required,
            P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision, new[] { row });
        result = null;
        return true;
    }

    private static bool TryValidateSection(P12AOwnerSection section, string id, out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        if (section == null || section.Id != id) return false;
        if (section.Major != DailyV1SelectedOwnerSectionCensusContract.FixedOwnerSectionSchemaVersion || section.Minor != 0)
        { result = new P12AResult(P12AResultCode.UnsupportedSchema, id); return false; }
        if (section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(id)
            || section.Rows.Count != 1 || section.Rows[0].PayloadLength > P12AContinuationCodec.MaxSectionBytes)
        { result = Invalid(id: id); return false; }
        result = null;
        return true;
    }

    private static string RequiredString(BinaryReader reader)
    { if (!P12AWire.ReadString(reader, out string value) || value == null) throw new InvalidDataException(); return value; }
    private static P12AResult Invalid(string id = null) =>
        new P12AResult(P12AResultCode.InvalidOwnerValue, id);
    private static bool Limit(string sectionId, out P12AResult result)
    { result = new P12AResult(P12AResultCode.LimitExceeded, sectionId); return false; }
    private static bool IsWireException(Exception exception) => exception is ArgumentException
        || exception is InvalidDataException || exception is EndOfStreamException || exception is IOException
        || exception is P12ALimitException || exception is OverflowException || exception is DecoderFallbackException;
}
