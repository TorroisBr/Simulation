using System;
using System.Collections.Generic;

/// <summary>
/// Validates the live Core Person/NPC/Genealogy join used by one unpublished
/// restored candidate. Unity separately verifies that its runtime projections
/// point back to these exact Core owners.
/// </summary>
internal static class DailyV1RestoredPersonGraphContract
{
    internal static bool TryValidate(
        PersonStore persons,
        GenealogyStore genealogy,
        IReadOnlyList<string> selectedNpcRuntimeIds,
        out string diagnostic)
    {
        diagnostic = null;
        if (persons == null || genealogy == null || selectedNpcRuntimeIds == null)
        {
            diagnostic = "The Core PersonStore, GenealogyStore, and selected NPC identity set are required.";
            return false;
        }

        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string npcRuntimeId in selectedNpcRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(npcRuntimeId) || !npcIds.Add(npcRuntimeId))
            {
                diagnostic = "The selected Core NPC identity set contains a missing or duplicate RuntimeId.";
                return false;
            }
        }

        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> materializedNpcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PersonRuntime person in persons.Persons)
        {
            string personId = person?.PersonId?.Value;
            if (string.IsNullOrWhiteSpace(personId) || !personIds.Add(personId))
            {
                diagnostic = "The Core PersonStore contains a missing or duplicate PersonId.";
                return false;
            }

            string npcRuntimeId = person.MaterializedNpcRuntimeId;
            if (npcRuntimeId == null) continue;
            if (string.IsNullOrWhiteSpace(npcRuntimeId)
                || !npcIds.Contains(npcRuntimeId)
                || !materializedNpcIds.Add(npcRuntimeId)
                || !persons.TryGetByMaterializedNpcRuntimeId(
                    npcRuntimeId, out PersonRuntime indexedPerson)
                || !ReferenceEquals(person, indexedPerson))
            {
                diagnostic = "A Core Person materialization does not resolve to exactly one selected NPC owner.";
                return false;
            }
        }

        if (materializedNpcIds.Count != persons.MaterializedBindingCount)
        {
            diagnostic = "The Core PersonStore materialization index does not match its exact bound Person set.";
            return false;
        }

        foreach (ParentageRecord edge in genealogy.Records)
        {
            if (edge?.ParentId == null || edge.ChildId == null
                || !personIds.Contains(edge.ParentId.Value)
                || !personIds.Contains(edge.ChildId.Value))
            {
                diagnostic = "A Core Genealogy edge endpoint does not resolve to a Person in the restored owner graph.";
                return false;
            }
        }

        return true;
    }
}
