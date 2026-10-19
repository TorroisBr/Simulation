using System;
using System.Collections.Generic;

/// <summary>
/// Core-owned composition of the selected Daily-v1 NPC owners used by the
/// Unity runtime adapter. The owner instances are shared with that adapter;
/// this object does not copy or mirror owner state.
/// </summary>
internal sealed class DailyV1NpcRuntimeOwners
{
    internal string RuntimeId { get; }
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal DailyV1NpcDefinition Definition { get; }
    internal DailyV1InventoryOwner Inventory { get; }
    internal DailyV1NpcStatusOwner Status { get; }
    internal MoneyAccountRuntime MoneyAccount { get; }
    internal DailyV1CommercialKnowledgeOwner CommercialKnowledge { get; }
    internal DailyV1NpcLifecycleOwner Lifecycle { get; }
    internal DailyV1NpcActionSlotOwner ActionSlot { get; }

    private DailyV1NpcRuntimeOwners(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        DailyV1InventoryOwner inventory,
        DailyV1NpcStatusOwner status,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge)
    {
        RuntimeId = runtimeId;
        Definitions = definitions;
        Definition = definition;
        Inventory = inventory;
        Status = status;
        MoneyAccount = moneyAccount;
        CommercialKnowledge = commercialKnowledge;
        Lifecycle = new DailyV1NpcLifecycleOwner();
        ActionSlot = new DailyV1NpcActionSlotOwner(definitions);
    }

    internal static bool TryCreateInitial(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId) || !IsAdmitted(definitions, definition)) return false;

        List<DailyV1StatusDefinition> statuses = new List<DailyV1StatusDefinition>();
        foreach (string statusId in definition.Values.statusPadrao ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(statusId)
                || !definitions.Statuses.TryGetValue(statusId, out DailyV1StatusDefinition status))
                return false;
            statuses.Add(status);
        }

        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        MoneyAccountRuntime moneyAccount = new MoneyAccountRuntime(initialMoney);
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory, moneyAccount,
            new DailyV1CommercialKnowledgeOwner(definitions), out owners);
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || !IsAdmitted(definitions, definition)
            || statuses == null
            || moneyAccount == null
            || commercialKnowledge == null
            || !ReferenceEquals(commercialKnowledge.Definitions, definitions)
            || (inventory != null && !ReferenceEquals(inventory.Definitions, definitions))
            || !DailyV1NpcStatusOwner.TryCreate(definitions, statuses, out DailyV1NpcStatusOwner statusOwner))
            return false;

        owners = new DailyV1NpcRuntimeOwners(
            runtimeId, definitions, definition, inventory, statusOwner, moneyAccount, commercialKnowledge);
        return true;
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        out DailyV1NpcRuntimeOwners owners) =>
        TryCreateBound(runtimeId, definitions, definition, statuses, inventory, moneyAccount,
            definitions == null ? null : new DailyV1CommercialKnowledgeOwner(definitions), out owners);

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition) =>
        definitions != null && definition != null
        && definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition admitted)
        && ReferenceEquals(definition, admitted);

    internal bool TryCopyStatusDefinitionIds(List<string> destination) =>
        Status != null && Status.TryCopyDefinitionIds(destination);
}

/// <summary>
/// Holds the exact admitted action-definition identity occupying one selected
/// NPC's current-action slot. Unity's NpcActionRuntime remains the existing
/// execution and persistence projection of this node.
/// </summary>
internal sealed class DailyV1NpcActionSlotOwner
{
    private readonly DailyV1EffectiveDefinitions definitions;

    internal DailyV1ActionDefinition CurrentAction { get; private set; }
    internal long Revision { get; private set; }

    internal DailyV1NpcActionSlotOwner(DailyV1EffectiveDefinitions definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    internal bool TrySetCurrentAction(DailyV1ActionDefinition action)
    {
        if (action != null
            && (!definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admitted)
                || !ReferenceEquals(action, admitted)))
            return false;

        CurrentAction = action;
        return true;
    }

    internal bool CanAdvanceRevision(int increments) =>
        increments >= 0 && Revision <= long.MaxValue - increments;

    internal bool TryInstallRevision(long revision)
    {
        if (revision < 0L) return false;
        Revision = revision;
        return true;
    }

    internal void NotifyCurrentActionMutationCommitted()
    {
        if (Revision < long.MaxValue) Revision++;
    }
}

/// <summary>
/// Owns the selected NPC's factual life and injury values and the existing
/// P12 life-state revision. Unity fields remain compatibility projections.
/// </summary>
internal sealed class DailyV1NpcLifecycleOwner
{
    internal NpcLifeState LifeState { get; private set; } = NpcLifeState.Alive;
    internal NpcInjurySeverity InjurySeverity { get; private set; } = NpcInjurySeverity.None;
    internal long LifeStateRevision { get; private set; }

    internal bool TryInstallState(NpcLifeState lifeState, NpcInjurySeverity injurySeverity, long revision)
    {
        if (!NpcInjuryRules.IsValid(lifeState)
            || !NpcInjuryRules.IsValid(injurySeverity)
            || revision < 0L)
            return false;

        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        LifeStateRevision = revision;
        return true;
    }

    internal bool TrySetInjurySeverity(NpcInjurySeverity severity)
    {
        if (!NpcInjuryRules.IsValid(severity)) return false;
        if (severity > InjurySeverity) InjurySeverity = severity;
        return true;
    }

    internal bool TrySetLifeState(NpcLifeState lifeState)
    {
        if (!NpcInjuryRules.IsValid(lifeState)) return false;
        LifeState = lifeState;
        return true;
    }

    internal bool CanAdvanceRevision(int increments) =>
        increments >= 0 && LifeStateRevision <= long.MaxValue - increments;

    internal void NotifyLifeStateMutationCommitted()
    {
        if (LifeStateRevision < long.MaxValue) LifeStateRevision++;
    }
}
