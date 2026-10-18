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

    private DailyV1NpcRuntimeOwners(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        DailyV1InventoryOwner inventory,
        DailyV1NpcStatusOwner status,
        MoneyAccountRuntime moneyAccount)
    {
        RuntimeId = runtimeId;
        Definitions = definitions;
        Definition = definition;
        Inventory = inventory;
        Status = status;
        MoneyAccount = moneyAccount;
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
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory, moneyAccount, out owners);
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || !IsAdmitted(definitions, definition)
            || statuses == null
            || moneyAccount == null
            || (inventory != null && !ReferenceEquals(inventory.Definitions, definitions))
            || !DailyV1NpcStatusOwner.TryCreate(definitions, statuses, out DailyV1NpcStatusOwner statusOwner))
            return false;

        owners = new DailyV1NpcRuntimeOwners(
            runtimeId, definitions, definition, inventory, statusOwner, moneyAccount);
        return true;
    }

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition) =>
        definitions != null && definition != null
        && definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition admitted)
        && ReferenceEquals(definition, admitted);

    internal bool TryCopyStatusDefinitionIds(List<string> destination) =>
        Status != null && Status.TryCopyDefinitionIds(destination);
}
