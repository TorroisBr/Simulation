using System;
using System.Collections.Generic;

public sealed class ExpeditionStore
{
    private readonly List<ExpeditionRuntime> activeExpeditions = new List<ExpeditionRuntime>();
    private readonly Dictionary<string, ExpeditionRuntime> expeditionsById =
        new Dictionary<string, ExpeditionRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<ExpeditionRuntime> readOnlyActiveExpeditions;

    public IReadOnlyList<ExpeditionRuntime> ActiveExpeditions => readOnlyActiveExpeditions;

    public ExpeditionStore()
    {
        readOnlyActiveExpeditions = activeExpeditions.AsReadOnly();
    }

    public ExpeditionRuntime GetById(string expeditionId)
    {
        return string.IsNullOrWhiteSpace(expeditionId) == false
            && expeditionsById.TryGetValue(expeditionId, out ExpeditionRuntime expedition) == true
            ? expedition
            : null;
    }

    public ExpeditionRuntime GetByExpeditionId(string expeditionId)
    {
        return GetById(expeditionId);
    }

    public bool TryGetById(string expeditionId, out ExpeditionRuntime expedition)
    {
        if (string.IsNullOrWhiteSpace(expeditionId) == false
            && expeditionsById.TryGetValue(expeditionId, out expedition) == true)
        {
            return true;
        }

        expedition = null;
        return false;
    }

    public bool TryGetByExpeditionId(string expeditionId, out ExpeditionRuntime expedition)
    {
        return TryGetById(expeditionId, out expedition);
    }

    public bool Add(ExpeditionRuntime expedition)
    {
        if (expedition == null
            || expedition.IsActive == false
            || expeditionsById.ContainsKey(expedition.ExpeditionId) == true)
        {
            return false;
        }

        foreach (string memberRuntimeId in expedition.MemberRuntimeIds)
        {
            if (TryGetExpeditionForNpc(memberRuntimeId, out _))
            {
                return false;
            }
        }

        expeditionsById.Add(expedition.ExpeditionId, expedition);
        activeExpeditions.Add(expedition);
        return true;
    }

    public bool TryGetExpeditionForNpc(string npcRuntimeId, out ExpeditionRuntime expedition)
    {
        expedition = null;

        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        foreach (ExpeditionRuntime candidate in activeExpeditions)
        {
            if (candidate != null && ContainsMember(candidate.MemberRuntimeIds, npcRuntimeId) == true)
            {
                expedition = candidate;
                return true;
            }
        }

        return false;
    }

    public bool IsNpcOnActiveExpedition(string npcRuntimeId)
    {
        return TryGetExpeditionForNpc(npcRuntimeId, out _);
    }

    public bool Remove(string expeditionId)
    {
        ExpeditionRuntime expedition = GetById(expeditionId);

        if (expedition == null || expedition.State != ExpeditionState.Preparing)
        {
            return false;
        }

        expeditionsById.Remove(expeditionId);
        activeExpeditions.Remove(expedition);
        return true;
    }

    private static bool ContainsMember(IReadOnlyList<string> memberRuntimeIds, string npcRuntimeId)
    {
        if (memberRuntimeIds == null)
        {
            return false;
        }

        foreach (string memberRuntimeId in memberRuntimeIds)
        {
            if (string.Equals(memberRuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }
}
