using System;
using System.Collections.Generic;

/// <summary>
/// Dedicated store for directed NPC-to-NPC relationships.
/// Insertion order is retained so filtered query results are deterministic.
/// </summary>
public sealed class NpcRelationStore
{
    private readonly List<NpcRelationRuntime> relations = new List<NpcRelationRuntime>();
    private readonly Dictionary<RelationKey, NpcRelationRuntime> relationsByPair = new Dictionary<RelationKey, NpcRelationRuntime>();
    private readonly IReadOnlyList<NpcRelationRuntime> readOnlyRelations;

    public IReadOnlyList<NpcRelationRuntime> Relations => readOnlyRelations;

    public NpcRelationStore()
    {
        readOnlyRelations = relations.AsReadOnly();
    }

    /// <summary>
    /// Reads a directed relation without creating one.
    /// </summary>
    public bool TryGetRelation(string sourceNpcRuntimeId, string targetNpcRuntimeId, out NpcRelationRuntime relation)
    {
        relation = null;

        if (TryCreateKey(sourceNpcRuntimeId, targetNpcRuntimeId, out RelationKey key) == false)
        {
            return false;
        }

        return relationsByPair.TryGetValue(key, out relation) == true;
    }

    /// <summary>
    /// Explicitly creates a neutral directed relation when it does not exist.
    /// Returns null for invalid or self pairs.
    /// </summary>
    public NpcRelationRuntime GetOrCreateRelation(string sourceNpcRuntimeId, string targetNpcRuntimeId)
    {
        if (TryCreateKey(sourceNpcRuntimeId, targetNpcRuntimeId, out RelationKey key) == false)
        {
            return null;
        }

        if (relationsByPair.TryGetValue(key, out NpcRelationRuntime existing) == true)
        {
            return existing;
        }

        NpcRelationRuntime created = new NpcRelationRuntime(sourceNpcRuntimeId, targetNpcRuntimeId);
        relationsByPair.Add(key, created);
        relations.Add(created);
        return created;
    }

    public bool SetAffinity(string sourceNpcRuntimeId, string targetNpcRuntimeId, float value)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.SetAffinity(value);
        return true;
    }

    public bool AdjustAffinity(string sourceNpcRuntimeId, string targetNpcRuntimeId, float amount)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.AdjustAffinity(amount);
        return true;
    }

    public bool SetTrust(string sourceNpcRuntimeId, string targetNpcRuntimeId, float value)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.SetTrust(value);
        return true;
    }

    public bool AdjustTrust(string sourceNpcRuntimeId, string targetNpcRuntimeId, float amount)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.AdjustTrust(amount);
        return true;
    }

    public bool SetFear(string sourceNpcRuntimeId, string targetNpcRuntimeId, float value)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.SetFear(value);
        return true;
    }

    public bool AdjustFear(string sourceNpcRuntimeId, string targetNpcRuntimeId, float amount)
    {
        NpcRelationRuntime relation = GetOrCreateRelation(sourceNpcRuntimeId, targetNpcRuntimeId);

        if (relation == null)
        {
            return false;
        }

        relation.AdjustFear(amount);
        return true;
    }

    /// <summary>
    /// Returns only relations whose source is sourceNpcRuntimeId.
    /// </summary>
    public IReadOnlyList<NpcRelationRuntime> GetRelationsFrom(string sourceNpcRuntimeId)
    {
        return FilterRelations(sourceNpcRuntimeId, true);
    }

    /// <summary>
    /// Returns only relations whose target is targetNpcRuntimeId.
    /// </summary>
    public IReadOnlyList<NpcRelationRuntime> GetRelationsToward(string targetNpcRuntimeId)
    {
        return FilterRelations(targetNpcRuntimeId, false);
    }

    private IReadOnlyList<NpcRelationRuntime> FilterRelations(string runtimeId, bool matchSource)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            return Array.Empty<NpcRelationRuntime>();
        }

        List<NpcRelationRuntime> matches = new List<NpcRelationRuntime>();

        foreach (NpcRelationRuntime relation in relations)
        {
            if (relation == null)
            {
                continue;
            }

            string candidateId = matchSource
                ? relation.SourceNpcRuntimeId
                : relation.TargetNpcRuntimeId;

            if (string.Equals(candidateId, runtimeId, StringComparison.Ordinal) == true)
            {
                matches.Add(relation);
            }
        }

        return matches.AsReadOnly();
    }

    private static bool TryCreateKey(string sourceNpcRuntimeId, string targetNpcRuntimeId, out RelationKey key)
    {
        key = default(RelationKey);

        if (string.IsNullOrWhiteSpace(sourceNpcRuntimeId) == true
            || string.IsNullOrWhiteSpace(targetNpcRuntimeId) == true
            || string.Equals(sourceNpcRuntimeId, targetNpcRuntimeId, StringComparison.Ordinal) == true)
        {
            return false;
        }

        key = new RelationKey(sourceNpcRuntimeId, targetNpcRuntimeId);
        return true;
    }

    private struct RelationKey : IEquatable<RelationKey>
    {
        private readonly string sourceNpcRuntimeId;
        private readonly string targetNpcRuntimeId;

        public RelationKey(string sourceNpcRuntimeId, string targetNpcRuntimeId)
        {
            this.sourceNpcRuntimeId = sourceNpcRuntimeId;
            this.targetNpcRuntimeId = targetNpcRuntimeId;
        }

        public bool Equals(RelationKey other)
        {
            return string.Equals(sourceNpcRuntimeId, other.sourceNpcRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(targetNpcRuntimeId, other.targetNpcRuntimeId, StringComparison.Ordinal) == true;
        }

        public override bool Equals(object obj)
        {
            return obj is RelationKey && Equals((RelationKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(sourceNpcRuntimeId);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(targetNpcRuntimeId);
                return hash;
            }
        }
    }
}
