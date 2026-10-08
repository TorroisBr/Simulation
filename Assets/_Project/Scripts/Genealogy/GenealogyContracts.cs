using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum GenealogyFailureCode
{
    None = 0,
    InvalidParent,
    InvalidChild,
    InvalidParentageRecord,
    SelfParent,
    DuplicateParentage,
    WouldCreateCycle,
    ParentageNotFound,
    RuntimeFaulted,
    RevisionOverflow,
    InvalidSnapshot,
    UnsupportedSnapshotSchema
}

/// <summary>
/// Immutable owner-local export for the P12-D Genealogy section. It carries no
/// runtime identity or capture-token authority; those are checked by the
/// surrounding profile capture and merged graph stages.
/// </summary>
internal sealed class GenealogyOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<ParentageRecord> Records { get; }

    internal GenealogyOwnerSnapshot(
        int schemaVersion,
        long revision,
        IEnumerable<ParentageRecord> records)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        if (records == null)
        {
            Records = null;
            return;
        }

        List<ParentageRecord> copy = new List<ParentageRecord>();
        bool canSort = true;
        foreach (ParentageRecord record in records)
        {
            if (record == null)
            {
                copy.Add(null);
                canSort = false;
                continue;
            }

            if (record.ParentId == null || record.ChildId == null
                || record.ParentId == record.ChildId)
            {
                // Retain malformed internal input so the staged factory can
                // reject it as data instead of throwing during snapshot copy.
                copy.Add(record);
                canSort = false;
                continue;
            }

            copy.Add(new ParentageRecord(
                new PersonId(record.ParentId.Value),
                new PersonId(record.ChildId.Value)));
        }

        if (canSort)
        {
            copy.Sort(CompareRecords);
        }

        Records = new ReadOnlyCollection<ParentageRecord>(copy);
    }

    private static int CompareRecords(ParentageRecord left, ParentageRecord right)
    {
        int parentComparison = string.CompareOrdinal(left.ParentId.Value, right.ParentId.Value);
        return parentComparison != 0
            ? parentComparison
            : string.CompareOrdinal(left.ChildId.Value, right.ChildId.Value);
    }
}

/// <summary>
/// Structured failure for expected parentage operations.
/// </summary>
public sealed class GenealogyFailure : IEquatable<GenealogyFailure>
{
    private static readonly GenealogyFailure none =
        new GenealogyFailure(GenealogyFailureCode.None, string.Empty);

    private GenealogyFailure(GenealogyFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static GenealogyFailure None => none;

    public GenealogyFailureCode Code { get; }

    public string Message { get; }

    public bool IsFailure => Code != GenealogyFailureCode.None;

    public static GenealogyFailure Create(GenealogyFailureCode code, string message)
    {
        return code == GenealogyFailureCode.None
            ? None
            : new GenealogyFailure(code, message);
    }

    public bool Equals(GenealogyFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as GenealogyFailure);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
        }
    }

    public override string ToString()
    {
        return Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
    }
}

/// <summary>
/// One directed parent-to-child edge. Both endpoints are stable PersonIds.
/// </summary>
public sealed class ParentageRecord : IEquatable<ParentageRecord>
{
    public ParentageRecord(PersonId parentId, PersonId childId)
    {
        ParentId = parentId ?? throw new ArgumentNullException(nameof(parentId));
        ChildId = childId ?? throw new ArgumentNullException(nameof(childId));

        if (ParentId == ChildId)
        {
            throw new ArgumentException("A person cannot be their own parent.", nameof(childId));
        }
    }

    public PersonId ParentId { get; }

    public PersonId ChildId { get; }

    public bool Equals(ParentageRecord other)
    {
        return other != null
            && ParentId == other.ParentId
            && ChildId == other.ChildId;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ParentageRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((ParentId != null ? ParentId.GetHashCode() : 0) * 397)
                ^ (ChildId != null ? ChildId.GetHashCode() : 0);
        }
    }

    public override string ToString()
    {
        return ParentId + " -> " + ChildId;
    }
}
