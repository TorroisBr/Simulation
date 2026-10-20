using System;
using System.Collections.Generic;

/// <summary>Selected NPC job and trade policy evaluated from the admitted Core definition graph.</summary>
internal static class DailyV1NpcDefinitionRules
{
    // Serialized enum ordinals from the admitted Unity enums. Keep these named
    // values aligned with NpcJobType.Merchant and NpcActionType.SellGoods.
    internal const int MerchantJobTypeValue = 1;
    internal const int GuardJobTypeValue = 2;
    internal const int TravelingMerchantBehaviorValue = 0;
    internal const int LocalMerchantBehaviorValue = 1;
    internal const int NormalActionTypeValue = 0;
    internal const int BuyGoodsActionTypeValue = 1;
    internal const int SellGoodsActionTypeValue = 2;
    internal const int TravelActionTypeValue = 3;
    internal const int ArrestActionTypeValue = 4;
    internal const int StealActionTypeValue = 5;
    internal const int HideActionTypeValue = 6;
    internal const int FleeCityActionTypeValue = 7;
    internal const int EscapePrisonActionTypeValue = 8;

    internal static bool HasConfiguredDefaultAction(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action)
    {
        if (!IsAdmittedNpc(definitions, npc) || !IsAdmittedAction(definitions, action)) return false;
        foreach (DailyV1NPCDefaultAction row in npc.Values.acoesPadrao ?? Array.Empty<DailyV1NPCDefaultAction>())
            if (row != null && definitions.Actions.TryGetValue(row.action, out DailyV1ActionDefinition configured)
                && ReferenceEquals(configured, action))
                return true;
        return false;
    }

    internal static bool HasRequiredActionStatuses(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        IReadOnlyList<DailyV1StatusDefinition> activeStatuses)
    {
        if (!IsAdmittedAction(definitions, action)) return false;
        foreach (string requiredStatusId in action.Values.statusNecessariosParaFazerAcao ?? Array.Empty<string>())
        {
            if (requiredStatusId == null) continue;
            if (!definitions.Statuses.TryGetValue(requiredStatusId, out DailyV1StatusDefinition required)) return false;

            bool present = false;
            foreach (DailyV1StatusDefinition active in activeStatuses ?? Array.Empty<DailyV1StatusDefinition>())
                if (ReferenceEquals(active, required)) { present = true; break; }
            if (!present) return false;
        }
        return true;
    }

    internal static bool TryGetActionStatusChanges(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        out IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> statusToAdd,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
    {
        statusToRemove = null;
        statusToAdd = null;
        targetStatusToRemove = null;
        targetStatusToAdd = null;
        if (!IsAdmittedAction(definitions, action)
            || !TryResolveStatuses(definitions, action.Values.statusToRemove, out statusToRemove)
            || !TryResolveStatuses(definitions, action.Values.statusToAdd, out statusToAdd)
            || !TryResolveStatuses(definitions, action.Values.targetStatusToRemove, out targetStatusToRemove)
            || !TryResolveStatuses(definitions, action.Values.targetStatusToAdd, out targetStatusToAdd))
        {
            statusToRemove = null;
            statusToAdd = null;
            targetStatusToRemove = null;
            targetStatusToAdd = null;
            return false;
        }
        return true;
    }

    internal static DailyV1CrimeActionSettings GetCrimeActionSettings(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.crimeSettings : null;

    internal static string GetActionName(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.actionName ?? string.Empty : string.Empty;

    internal static string GetNormalActionLogText(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.normalActionLogText ?? string.Empty : string.Empty;

    internal static bool GetActionCanFail(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) && action.Values.canFail;

    internal static float GetActionBaseSuccessChance(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.baseSuccessChance : 0f;

    internal static int GetActionTypeOrder(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.actionType?.Value ?? int.MinValue : int.MinValue;

    internal static int GetActionCategoryOrder(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.actionCategory?.Value ?? int.MinValue : int.MinValue;

    internal static float GetActionBaseUtility(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        IsAdmittedAction(definitions, action) ? action.Values.baseUtility : 0f;

    internal static float ApplyStatusModifiers(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        IReadOnlyList<DailyV1StatusDefinition> activeStatuses,
        float utility)
    {
        if (!IsAdmittedAction(definitions, action)) return 0f;
        foreach (DailyV1StatusWeightModifier modifier in action.Values.statusModifiers ?? Array.Empty<DailyV1StatusWeightModifier>())
        {
            if (modifier == null || string.IsNullOrWhiteSpace(modifier.status)
                || !definitions.Statuses.TryGetValue(modifier.status, out DailyV1StatusDefinition status))
                continue;
            foreach (DailyV1StatusDefinition active in activeStatuses ?? Array.Empty<DailyV1StatusDefinition>())
            {
                if (ReferenceEquals(active, status))
                {
                    utility *= modifier.multiplier;
                    break;
                }
            }
        }
        return utility;
    }

    internal static bool HasTrait(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1TraitDefinition trait)
    {
        if (!IsAdmittedNpc(definitions, npc) || !IsAdmittedTrait(definitions, trait)) return false;
        foreach (string traitId in npc.Values.traits ?? Array.Empty<string>())
            if (string.Equals(traitId, trait.DefinitionId, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static float GetCapabilityAttributeValue(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CapabilityAttributeDefinition attribute)
    {
        if (!IsAdmittedNpc(definitions, npc) || !IsAdmittedAttribute(definitions, attribute)) return 0f;
        foreach (DailyV1CapabilityAttributeValue row in npc.Values.capabilityValues ?? Array.Empty<DailyV1CapabilityAttributeValue>())
            if (row != null && string.Equals(row.attribute, attribute.DefinitionId, StringComparison.Ordinal))
                return UnityMathfCompatibility.Max(0f, row.value);
        return 0f;
    }

    internal static DailyV1NpcJobData GetJob(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions)
    {
        if (!IsAdmittedNpc(definitions, npc)) return null;
        string jobId = npc?.Values.job;
        return definitions != null && !string.IsNullOrWhiteSpace(jobId)
            && definitions.Jobs.TryGetValue(jobId, out DailyV1JobDefinition jobNode)
            ? jobNode.Values
            : null;
    }

    internal static bool HasJobType(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        int jobType)
    {
        DailyV1NpcJobData job = GetJob(npc, definitions);
        return job?.jobType != null && job.jobType.Value == jobType;
    }

    internal static bool HasMerchantBehavior(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        int behavior)
    {
        DailyV1NpcJobData job = GetJob(npc, definitions);
        return job?.merchantBehavior != null && job.merchantBehavior.Value == behavior;
    }

    internal static float MinimumProfitPerItem(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions)
    {
        DailyV1NpcJobData job = GetJob(npc, definitions);
        return job != null ? job.minimumProfitPerItem : 1f;
    }

    internal static bool TryGetWorkActionUtility(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        out float utility)
    {
        utility = 0f;
        if (!IsAdmittedNpc(definitions, npc) || !IsAdmittedAction(definitions, action)) return false;

        DailyV1NpcJobData job = GetJob(npc, definitions);
        if (job == null || string.IsNullOrWhiteSpace(job.workAction)
            || !definitions.Actions.TryGetValue(job.workAction, out DailyV1ActionDefinition configuredAction)
            || !ReferenceEquals(configuredAction, action))
            return false;

        utility = job.workUtility;
        return true;
    }

    internal static bool TryGetDefaultActionUtility(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        out float utility)
    {
        utility = 0f;
        if (!IsAdmittedNpc(definitions, npc) || !IsAdmittedAction(definitions, action)) return false;

        foreach (DailyV1NPCDefaultAction row in npc.Values.acoesPadrao ?? Array.Empty<DailyV1NPCDefaultAction>())
        {
            if (row != null && definitions.Actions.TryGetValue(row.action, out DailyV1ActionDefinition configuredAction)
                && ReferenceEquals(configuredAction, action))
            {
                utility = row.baseUtility;
                return true;
            }
        }

        return false;
    }

    internal static void AppendPreferredTradeItemDefinitionIds(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        List<string> destination)
    {
        if (destination == null) return;
        DailyV1NpcJobData job = GetJob(npc, definitions);
        foreach (DailyV1TradeItemPreference row in job?.preferredTradeItems ?? Array.Empty<DailyV1TradeItemPreference>())
            if (row != null && !string.IsNullOrWhiteSpace(row.item)
                && definitions.Items.ContainsKey(row.item))
                destination.Add(row.item);
    }

    internal static bool TryGetPreferredTradeItemMultiplier(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ItemDefinition item,
        out float multiplier)
    {
        multiplier = 1f;
        if (!IsAdmittedItem(definitions, item)) return false;

        DailyV1NpcJobData job = GetJob(npc, definitions);
        foreach (DailyV1TradeItemPreference row in job?.preferredTradeItems ?? Array.Empty<DailyV1TradeItemPreference>())
        {
            if (row != null && string.Equals(row.item, item.DefinitionId, StringComparison.Ordinal)
                && definitions.Items.TryGetValue(row.item, out DailyV1ItemDefinition configuredItem)
                && ReferenceEquals(configuredItem, item))
            {
                multiplier = row.utilityMultiplier;
                return true;
            }
        }

        return false;
    }

    internal static bool IsAdmittedNpc(DailyV1EffectiveDefinitions definitions, DailyV1NpcDefinition npc) =>
        definitions != null && npc != null
        && definitions.Npcs.TryGetValue(npc.DefinitionId, out DailyV1NpcDefinition admitted)
        && ReferenceEquals(admitted, npc);

    internal static bool IsAdmittedAction(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        definitions != null && action != null
        && definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admitted)
        && ReferenceEquals(admitted, action);

    private static bool IsAdmittedItem(DailyV1EffectiveDefinitions definitions, DailyV1ItemDefinition item) =>
        definitions != null && item != null
        && definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
        && ReferenceEquals(admitted, item);

    private static bool IsAdmittedTrait(DailyV1EffectiveDefinitions definitions, DailyV1TraitDefinition trait) =>
        definitions != null && trait != null
        && definitions.Traits.TryGetValue(trait.DefinitionId, out DailyV1TraitDefinition admitted)
        && ReferenceEquals(admitted, trait);

    private static bool IsAdmittedAttribute(
        DailyV1EffectiveDefinitions definitions,
        DailyV1CapabilityAttributeDefinition attribute) =>
        definitions != null && attribute != null
        && definitions.CapabilityAttributes.TryGetValue(attribute.DefinitionId, out DailyV1CapabilityAttributeDefinition admitted)
        && ReferenceEquals(admitted, attribute);

    private static bool TryResolveStatuses(
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<string> definitionIds,
        out IReadOnlyList<DailyV1StatusDefinition> statuses)
    {
        List<DailyV1StatusDefinition> resolved = new List<DailyV1StatusDefinition>();
        foreach (string definitionId in definitionIds ?? Array.Empty<string>())
        {
            if (definitionId == null) continue;
            if (definitions == null || !definitions.Statuses.TryGetValue(definitionId, out DailyV1StatusDefinition status))
            {
                statuses = null;
                return false;
            }
            resolved.Add(status);
        }
        statuses = resolved.AsReadOnly();
        return true;
    }
}
