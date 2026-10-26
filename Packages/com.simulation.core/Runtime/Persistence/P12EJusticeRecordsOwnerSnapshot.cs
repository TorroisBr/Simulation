using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EJusticeSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSectionVector,
    InvalidOwnerValues,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    InvalidReference,
    InvalidRelation,
    InvalidOrdering,
    StageFailed
}

internal sealed class P12EJusticeSnapshotFailure
{
    internal static readonly P12EJusticeSnapshotFailure None =
        new P12EJusticeSnapshotFailure(P12EJusticeSnapshotFailureCode.None, string.Empty);

    internal P12EJusticeSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EJusticeSnapshotFailure(P12EJusticeSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EJusticeSnapshotFailure Create(
        P12EJusticeSnapshotFailureCode code,
        string message) => code == P12EJusticeSnapshotFailureCode.None
            ? None
            : new P12EJusticeSnapshotFailure(code, message);
}

internal sealed class P12EJusticeWantedSnapshotRow
{
    internal int Ordinal { get; }
    internal string TargetRuntimeId { get; }
    internal string TargetPersonIdValue { get; }
    internal string CityRuntimeId { get; }
    internal float Bounty { get; }
    internal int SentenceDays { get; }
    internal bool IsResolved { get; }

    internal P12EJusticeWantedSnapshotRow(
        int ordinal,
        string targetRuntimeId,
        string targetPersonIdValue,
        string cityRuntimeId,
        float bounty,
        int sentenceDays,
        bool isResolved)
    {
        Ordinal = ordinal;
        TargetRuntimeId = targetRuntimeId;
        TargetPersonIdValue = targetPersonIdValue;
        CityRuntimeId = cityRuntimeId;
        Bounty = bounty;
        SentenceDays = sentenceDays;
        IsResolved = isResolved;
    }

    internal P12EJusticeWantedSnapshotRow Copy() => new P12EJusticeWantedSnapshotRow(
        Ordinal, TargetRuntimeId, TargetPersonIdValue, CityRuntimeId, Bounty, SentenceDays, IsResolved);
}

internal sealed class P12EJusticeSentenceSnapshotRow
{
    internal int Ordinal { get; }
    internal string TargetRuntimeId { get; }
    internal string TargetPersonIdValue { get; }
    internal string CityRuntimeId { get; }
    internal int WarrantOrdinal { get; }
    internal int RemainingDays { get; }
    internal int FailedEscapeAttempts { get; }
    internal bool WasArrestedToday { get; }

    internal P12EJusticeSentenceSnapshotRow(
        int ordinal,
        string targetRuntimeId,
        string targetPersonIdValue,
        string cityRuntimeId,
        int warrantOrdinal,
        int remainingDays,
        int failedEscapeAttempts,
        bool wasArrestedToday)
    {
        Ordinal = ordinal;
        TargetRuntimeId = targetRuntimeId;
        TargetPersonIdValue = targetPersonIdValue;
        CityRuntimeId = cityRuntimeId;
        WarrantOrdinal = warrantOrdinal;
        RemainingDays = remainingDays;
        FailedEscapeAttempts = failedEscapeAttempts;
        WasArrestedToday = wasArrestedToday;
    }

    internal P12EJusticeSentenceSnapshotRow Copy() => new P12EJusticeSentenceSnapshotRow(
        Ordinal, TargetRuntimeId, TargetPersonIdValue, CityRuntimeId, WarrantOrdinal,
        RemainingDays, FailedEscapeAttempts, WasArrestedToday);
}

internal sealed class P12EJusticeRecordsSnapshotSection
{
    internal const string CurrentSectionId = "p12.crime-justice.records";
    internal const int CurrentSchemaVersion = 1;

    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12EJusticeWantedSnapshotRow> WantedRows { get; }
    internal IReadOnlyList<P12EJusticeSentenceSnapshotRow> SentenceRows { get; }

    internal P12EJusticeRecordsSnapshotSection(
        string sectionId,
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<P12EJusticeWantedSnapshotRow> wantedRows,
        IEnumerable<P12EJusticeSentenceSnapshotRow> sentenceRows)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        WantedRows = Copy(wantedRows);
        SentenceRows = Copy(sentenceRows);
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) where T : class
    {
        if (source == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in source)
        {
            if (row is P12EJusticeWantedSnapshotRow wanted)
                copied.Add((T)(object)wanted.Copy());
            else if (row is P12EJusticeSentenceSnapshotRow sentence)
                copied.Add((T)(object)sentence.Copy());
            else
                copied.Add(row);
        }
        return new ReadOnlyCollection<T>(copied);
    }
}

/// <summary>Detached export and private staged reconstruction for the current Justice owner rows.</summary>
internal sealed class P12EJusticeRecordsOwnerSnapshot
{
    internal const int CurrentSchemaVersion = P12EJusticeRecordsSnapshotSection.CurrentSchemaVersion;

    internal long CapturedAbsoluteDay { get; }
    internal P12EJusticeRecordsSnapshotSection Records { get; }

    internal P12EJusticeRecordsOwnerSnapshot(
        long capturedAbsoluteDay,
        P12EJusticeRecordsSnapshotSection records)
    {
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Records = records;
    }

    internal bool TryValidate(out P12EJusticeSnapshotFailure failure)
    {
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
            "Justice owner snapshot is malformed.");
        if (CapturedAbsoluteDay < 0L)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
                "Captured day must be nonnegative.", out failure);
        if (Records == null
            || !string.Equals(Records.SectionId, P12EJusticeRecordsSnapshotSection.CurrentSectionId, StringComparison.Ordinal)
            || Records.SchemaVersion != CurrentSchemaVersion)
            return Fail(P12EJusticeSnapshotFailureCode.UnsupportedSchema,
                "The current Justice owner section and schema are required.", out failure);
        if (Records.Revision < 0L)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidRevision,
                "The exact Justice local revision must be nonnegative.", out failure);
        if (Records.WantedRows == null || Records.SentenceRows == null
            || Records.RecordCount < 0)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Both detached row lists and a nonnegative combined cardinality are required.", out failure);

        int combinedCount;
        try { combinedCount = checked(Records.WantedRows.Count + Records.SentenceRows.Count); }
        catch (OverflowException)
        {
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Combined Justice row cardinality overflowed.", out failure);
        }
        if (Records.RecordCount != combinedCount)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Declared combined cardinality must equal the two detached row counts.", out failure);

        for (int i = 0; i < Records.WantedRows.Count; i++)
        {
            P12EJusticeWantedSnapshotRow row = Records.WantedRows[i];
            if (row == null || row.Ordinal != i || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(row.CityRuntimeId)
                || (row.TargetPersonIdValue != null
                    && (!PersonId.TryCreate(row.TargetPersonIdValue, out _)
                        || string.IsNullOrWhiteSpace(row.TargetPersonIdValue)))
                || float.IsNaN(row.Bounty) || float.IsInfinity(row.Bounty) || row.Bounty < 0f
                || row.SentenceDays < 1)
                return Fail(P12EJusticeSnapshotFailureCode.InvalidIdentity,
                    "A wanted row has an invalid ordinal, endpoint identity, or scalar value.", out failure);
        }

        for (int i = 0; i < Records.SentenceRows.Count; i++)
        {
            P12EJusticeSentenceSnapshotRow row = Records.SentenceRows[i];
            if (row == null || row.Ordinal != i || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(row.CityRuntimeId)
                || (row.TargetPersonIdValue != null
                    && (!PersonId.TryCreate(row.TargetPersonIdValue, out _)
                        || string.IsNullOrWhiteSpace(row.TargetPersonIdValue)))
                || row.WarrantOrdinal < 0 || row.WarrantOrdinal >= Records.WantedRows.Count
                || row.RemainingDays < 0 || row.FailedEscapeAttempts < 0)
                return Fail(P12EJusticeSnapshotFailureCode.InvalidIdentity,
                    "A sentence row has an invalid ordinal, endpoint identity, scalar, or warrant ordinal.", out failure);

            P12EJusticeWantedSnapshotRow warrant = Records.WantedRows[row.WarrantOrdinal];
            if (!string.Equals(row.TargetRuntimeId, warrant.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(row.TargetPersonIdValue, warrant.TargetPersonIdValue, StringComparison.Ordinal)
                || !string.Equals(row.CityRuntimeId, warrant.CityRuntimeId, StringComparison.Ordinal))
                return Fail(P12EJusticeSnapshotFailureCode.InvalidRelation,
                    "A sentence row must retain the exact target/City relation of its warrant.", out failure);
        }

        failure = P12EJusticeSnapshotFailure.None;
        return true;
    }
    private static bool Fail(
        P12EJusticeSnapshotFailureCode code,
        string message,
        out P12EJusticeSnapshotFailure failure)
    {
        failure = P12EJusticeSnapshotFailure.Create(code, message);
        return false;
    }
}
