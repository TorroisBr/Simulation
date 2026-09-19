using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Authoritative parentage store for one world boundary.
/// It intentionally knows only PersonId and does not validate world membership.
/// </summary>
public sealed class GenealogyStore
{
    private readonly HashSet<ParentageRecord> records = new HashSet<ParentageRecord>();
    private readonly Dictionary<PersonId, HashSet<PersonId>> childrenByParent =
        new Dictionary<PersonId, HashSet<PersonId>>();
    private readonly Dictionary<PersonId, HashSet<PersonId>> parentsByChild =
        new Dictionary<PersonId, HashSet<PersonId>>();

    public int Count => records.Count;

    /// <summary>
    /// Returns a deterministic read-only snapshot ordered by parent and child.
    /// </summary>
    public IReadOnlyList<ParentageRecord> Records
    {
        get
        {
            List<ParentageRecord> snapshot = new List<ParentageRecord>(records);
            snapshot.Sort(CompareRecords);
            return new ReadOnlyCollection<ParentageRecord>(snapshot);
        }
    }

    public bool TryAddParentage(
        PersonId parent,
        PersonId child,
        out GenealogyFailure failure)
    {
        if (parent == null)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.InvalidParent,
                "A parent PersonId is required.");
            return false;
        }

        if (child == null)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.InvalidChild,
                "A child PersonId is required.");
            return false;
        }

        if (parent == child)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.SelfParent,
                "A person cannot be their own parent.");
            return false;
        }

        ParentageRecord record = new ParentageRecord(parent, child);
        if (records.Contains(record))
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.DuplicateParentage,
                "The parentage edge is already registered.");
            return false;
        }

        if (CanReach(child, parent))
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.WouldCreateCycle,
                "The parentage edge would create a cycle.");
            return false;
        }

        records.Add(record);
        AddAdjacency(childrenByParent, parent, child);
        AddAdjacency(parentsByChild, child, parent);
        failure = GenealogyFailure.None;
        return true;
    }

    public bool TryAddParentage(ParentageRecord record, out GenealogyFailure failure)
    {
        if (record == null)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.InvalidParentageRecord,
                "A parentage record is required.");
            return false;
        }

        return TryAddParentage(record.ParentId, record.ChildId, out failure);
    }

    public bool ContainsParentage(PersonId parent, PersonId child)
    {
        if (parent == null || child == null || parent == child)
        {
            return false;
        }

        return records.Contains(new ParentageRecord(parent, child));
    }

    public bool TryGetParentage(
        PersonId parent,
        PersonId child,
        out ParentageRecord record)
    {
        record = null;
        if (parent == null || child == null || parent == child)
        {
            return false;
        }

        ParentageRecord candidate = new ParentageRecord(parent, child);
        foreach (ParentageRecord existing in records)
        {
            if (existing.Equals(candidate))
            {
                record = existing;
                return true;
            }
        }

        return false;
    }

    public bool TryRemoveParentage(
        PersonId parent,
        PersonId child,
        out GenealogyFailure failure)
    {
        if (parent == null)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.InvalidParent,
                "A parent PersonId is required.");
            return false;
        }

        if (child == null)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.InvalidChild,
                "A child PersonId is required.");
            return false;
        }

        if (parent == child)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.SelfParent,
                "A person cannot be their own parent.");
            return false;
        }

        ParentageRecord record = new ParentageRecord(parent, child);
        if (records.Remove(record) == false)
        {
            failure = GenealogyFailure.Create(
                GenealogyFailureCode.ParentageNotFound,
                "The parentage edge is not registered.");
            return false;
        }

        RemoveAdjacency(childrenByParent, parent, child);
        RemoveAdjacency(parentsByChild, child, parent);
        failure = GenealogyFailure.None;
        return true;
    }

    public IReadOnlyList<PersonId> GetParents(PersonId child)
    {
        return GetSortedCopy(parentsByChild, child);
    }

    public IReadOnlyList<PersonId> GetChildren(PersonId parent)
    {
        return GetSortedCopy(childrenByParent, parent);
    }

    public IReadOnlyList<PersonId> GetAncestors(PersonId person)
    {
        return Traverse(person, parentsByChild);
    }

    public IReadOnlyList<PersonId> GetDescendants(PersonId person)
    {
        return Traverse(person, childrenByParent);
    }

    public bool IsDirectParent(PersonId parent, PersonId child)
    {
        return parent != null
            && child != null
            && parentsByChild.TryGetValue(child, out HashSet<PersonId> parents)
            && parents.Contains(parent);
    }

    public bool IsAncestorOf(PersonId ancestor, PersonId descendant)
    {
        if (ancestor == null || descendant == null || ancestor == descendant)
        {
            return false;
        }

        return CanReach(ancestor, descendant);
    }

    private bool CanReach(PersonId start, PersonId target)
    {
        if (start == null || target == null)
        {
            return false;
        }

        HashSet<PersonId> visited = new HashSet<PersonId>();
        Stack<PersonId> pending = new Stack<PersonId>();
        pending.Push(start);

        while (pending.Count > 0)
        {
            PersonId current = pending.Pop();
            if (visited.Add(current) == false)
            {
                continue;
            }

            if (current == target)
            {
                return true;
            }

            if (childrenByParent.TryGetValue(current, out HashSet<PersonId> children) == false)
            {
                continue;
            }

            foreach (PersonId child in children)
            {
                if (visited.Contains(child) == false)
                {
                    pending.Push(child);
                }
            }
        }

        return false;
    }

    private static IReadOnlyList<PersonId> Traverse(
        PersonId start,
        Dictionary<PersonId, HashSet<PersonId>> adjacency)
    {
        List<PersonId> result = new List<PersonId>();
        if (start == null)
        {
            return new ReadOnlyCollection<PersonId>(result);
        }

        HashSet<PersonId> visited = new HashSet<PersonId>();
        Stack<PersonId> pending = new Stack<PersonId>();
        pending.Push(start);

        while (pending.Count > 0)
        {
            PersonId current = pending.Pop();
            if (adjacency.TryGetValue(current, out HashSet<PersonId> next) == false)
            {
                continue;
            }

            foreach (PersonId candidate in next)
            {
                if (visited.Add(candidate))
                {
                    result.Add(candidate);
                    pending.Push(candidate);
                }
            }
        }

        result.Sort(ComparePersonIds);
        return new ReadOnlyCollection<PersonId>(result);
    }

    private static IReadOnlyList<PersonId> GetSortedCopy(
        Dictionary<PersonId, HashSet<PersonId>> adjacency,
        PersonId key)
    {
        List<PersonId> result = new List<PersonId>();
        if (key != null && adjacency.TryGetValue(key, out HashSet<PersonId> values))
        {
            result.AddRange(values);
        }

        result.Sort(ComparePersonIds);
        return new ReadOnlyCollection<PersonId>(result);
    }

    private static void AddAdjacency(
        Dictionary<PersonId, HashSet<PersonId>> adjacency,
        PersonId key,
        PersonId value)
    {
        if (adjacency.TryGetValue(key, out HashSet<PersonId> values) == false)
        {
            values = new HashSet<PersonId>();
            adjacency.Add(key, values);
        }

        values.Add(value);
    }

    private static void RemoveAdjacency(
        Dictionary<PersonId, HashSet<PersonId>> adjacency,
        PersonId key,
        PersonId value)
    {
        if (adjacency.TryGetValue(key, out HashSet<PersonId> values) == false)
        {
            return;
        }

        values.Remove(value);
        if (values.Count == 0)
        {
            adjacency.Remove(key);
        }
    }

    private static int CompareRecords(ParentageRecord left, ParentageRecord right)
    {
        int parentComparison = ComparePersonIds(left.ParentId, right.ParentId);
        return parentComparison != 0
            ? parentComparison
            : ComparePersonIds(left.ChildId, right.ChildId);
    }

    private static int ComparePersonIds(PersonId left, PersonId right)
    {
        return string.CompareOrdinal(left.Value, right.Value);
    }
}
