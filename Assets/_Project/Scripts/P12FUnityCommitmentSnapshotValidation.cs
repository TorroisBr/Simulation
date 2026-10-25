using System;
using System.Collections.Generic;

internal static class P12FUnityCommitmentSnapshotValidation
{
    internal static bool TryBuildNpcRows(IReadOnlyList<NpcRuntime> npcs,
        IReadOnlyList<P12DNpcFRow> rows, out Dictionary<string, NpcRuntime> npcsById,
        out Dictionary<string, P12DNpcFRow> rowsById)
    {
        npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        rowsById = new Dictionary<string, P12DNpcFRow>(StringComparer.Ordinal);
        if (npcs == null || rows == null || npcs.Count != rows.Count) return false;
        foreach (NpcRuntime npc in npcs)
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !npcsById.TryAdd(npc.RuntimeId, npc)) return false;
        foreach (P12DNpcFRow row in rows)
            if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId) || !rowsById.TryAdd(row.RuntimeId, row)) return false;
        if (npcsById.Count != rowsById.Count) return false;
        foreach (KeyValuePair<string, NpcRuntime> pair in npcsById)
            if (!rowsById.ContainsKey(pair.Key)) return false;
        return true;
    }
}
