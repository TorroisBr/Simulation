using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Selected Daily-v1 NPC status membership owned by the immutable definition graph.</summary>
internal sealed class DailyV1NpcStatusOwner
{
    private readonly DailyV1EffectiveDefinitions definitions;
    private readonly List<DailyV1StatusDefinition> statuses;
    private ReadOnlyCollection<DailyV1StatusDefinition> readOnlyStatuses;

    internal IReadOnlyList<DailyV1StatusDefinition> Statuses =>
        readOnlyStatuses ?? (readOnlyStatuses = statuses.AsReadOnly());

    private DailyV1NpcStatusOwner(
        DailyV1EffectiveDefinitions definitions,
        List<DailyV1StatusDefinition> statuses)
    {
        this.definitions = definitions;
        this.statuses = statuses;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1StatusDefinition> initialStatuses,
        out DailyV1NpcStatusOwner owner)
    {
        owner = null;
        if (definitions == null || initialStatuses == null) return false;

        List<DailyV1StatusDefinition> resolved = new List<DailyV1StatusDefinition>();
        foreach (DailyV1StatusDefinition status in initialStatuses)
        {
            if (!IsAdmitted(definitions, status)) return false;
            // Preserve the selected profile's authored order and multiplicity.
            resolved.Add(status);
        }

        owner = new DailyV1NpcStatusOwner(definitions, resolved);
        return true;
    }

    internal bool Contains(DailyV1StatusDefinition status)
    {
        if (!IsAdmitted(definitions, status)) return false;
        return statuses.Contains(status);
    }

    internal bool TryAdd(DailyV1StatusDefinition status)
    {
        if (!IsAdmitted(definitions, status) || statuses.Contains(status)) return false;
        statuses.Add(status);
        return true;
    }

    internal bool TryRemove(DailyV1StatusDefinition status)
    {
        if (!IsAdmitted(definitions, status)) return false;
        return statuses.Remove(status);
    }

    internal bool TryApplyOrderedChanges(
        IEnumerable<DailyV1StatusDefinition> statusToRemove,
        IEnumerable<DailyV1StatusDefinition> statusToAdd,
        Action mutationAdmission,
        Action mutationCommitted)
    {
        if (mutationAdmission == null || mutationCommitted == null) return false;

        foreach (DailyV1StatusDefinition status in statusToRemove ?? Array.Empty<DailyV1StatusDefinition>())
        {
            if (!Contains(status)) continue;
            mutationAdmission();
            if (TryRemove(status)) mutationCommitted();
        }

        foreach (DailyV1StatusDefinition status in statusToAdd ?? Array.Empty<DailyV1StatusDefinition>())
        {
            if (Contains(status)) continue;
            mutationAdmission();
            if (TryAdd(status)) mutationCommitted();
        }

        return true;
    }

    internal bool TryCopyDefinitionIds(List<string> destination)
    {
        if (destination == null) return false;
        destination.Clear();
        foreach (DailyV1StatusDefinition status in statuses)
        {
            if (!IsAdmitted(definitions, status))
            {
                destination.Clear();
                return false;
            }
            destination.Add(status.DefinitionId);
        }
        return true;
    }

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1StatusDefinition status) =>
        definitions != null && status != null
        && definitions.Statuses.TryGetValue(status.DefinitionId, out DailyV1StatusDefinition admitted)
        && ReferenceEquals(admitted, status);
}
