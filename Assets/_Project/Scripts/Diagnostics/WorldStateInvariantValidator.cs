using System;
using System.Collections.Generic;

public enum WorldStateInvariantSeverity
{
    Warning,
    Error
}

public sealed class WorldStateInvariantIssue
{
    public WorldStateInvariantSeverity Severity { get; }
    public string Code { get; }
    public string Identity { get; }
    public string Message { get; }

    public bool IsError => Severity == WorldStateInvariantSeverity.Error;

    public WorldStateInvariantIssue(
        WorldStateInvariantSeverity severity,
        string code,
        string identity,
        string message)
    {
        Severity = severity;
        Code = code;
        Identity = identity;
        Message = message;
    }
}

public sealed class WorldStateInvariantReport
{
    public IReadOnlyList<WorldStateInvariantIssue> Issues { get; }
    public bool HasErrors { get; }
    public bool IsValid => HasErrors == false;
    public bool IsEmpty => Issues.Count == 0;

    public WorldStateInvariantReport(IEnumerable<WorldStateInvariantIssue> issues)
    {
        List<WorldStateInvariantIssue> copied = SnapshotCollections.Materialize(issues);
        copied.Sort((left, right) =>
        {
            int severity = SeverityOrder(left.Severity).CompareTo(SeverityOrder(right.Severity));
            if (severity != 0) return severity;
            int code = StringComparer.Ordinal.Compare(left.Code, right.Code);
            if (code != 0) return code;
            return StringComparer.Ordinal.Compare(left.Identity, right.Identity);
        });

        HasErrors = false;
        foreach (WorldStateInvariantIssue issue in copied)
        {
            if (issue != null && issue.IsError)
            {
                HasErrors = true;
                break;
            }
        }

        Issues = copied.AsReadOnly();
    }

    private static int SeverityOrder(WorldStateInvariantSeverity severity)
    {
        return severity == WorldStateInvariantSeverity.Error ? 0 : 1;
    }

    public IReadOnlyList<WorldStateInvariantIssue> Errors
    {
        get { return Filter(WorldStateInvariantSeverity.Error); }
    }

    public IReadOnlyList<WorldStateInvariantIssue> Warnings
    {
        get { return Filter(WorldStateInvariantSeverity.Warning); }
    }

    private IReadOnlyList<WorldStateInvariantIssue> Filter(WorldStateInvariantSeverity severity)
    {
        List<WorldStateInvariantIssue> result = new List<WorldStateInvariantIssue>();
        foreach (WorldStateInvariantIssue issue in Issues)
        {
            if (issue != null && issue.Severity == severity)
            {
                result.Add(issue);
            }
        }

        return result.AsReadOnly();
    }
}

public static class WorldStateInvariantValidator
{
    public static WorldStateInvariantReport Validate(WorldStateSnapshot snapshot)
    {
        List<WorldStateInvariantIssue> issues = new List<WorldStateInvariantIssue>();
        if (snapshot == null)
        {
            AddError(issues, "SnapshotMissing", "world", "World state snapshot is null.");
            return new WorldStateInvariantReport(issues);
        }

        HashSet<string> settlementIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateCitySnapshot city in snapshot.Cities)
        {
            if (city == null)
            {
                AddError(issues, "SettlementNull", "settlement", "Snapshot contains a null settlement entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(city.RuntimeId))
            {
                AddError(issues, "SettlementIdMissing", "settlement", "Settlement RuntimeId is empty.");
            }
            else if (settlementIds.Add(city.RuntimeId) == false)
            {
                AddError(issues, "DuplicateSettlementRuntimeId", city.RuntimeId, "Settlement RuntimeId appears more than once.");
            }

            if (city.CurrentPopulation < 0)
            {
                AddError(issues, "NegativePopulation", city.RuntimeId, "Settlement population cannot be negative.");
            }

            if (city.PopulationRevision < 0L)
            {
                AddError(issues, "NegativePopulationRevision", city.RuntimeId, "Settlement population revision cannot be negative.");
            }

            if (city.NamedResidentCount < 0 || city.NamedPresentCount < 0)
            {
                AddError(issues, "NegativeNamedPopulationCount", city.RuntimeId, "Named settlement counts cannot be negative.");
            }

            if (city.NamedResidentCount > city.CurrentPopulation)
            {
                AddError(issues, "NamedResidentsExceedPopulation", city.RuntimeId, "Named living residents exceed aggregate settlement population.");
            }

            if (float.IsNaN(city.MarketBalance) || float.IsInfinity(city.MarketBalance))
            {
                AddError(issues, "NonFiniteMarketBalance", city.RuntimeId, "Settlement market balance is not finite.");
            }

            ValidateMarketStock(city, issues);
        }

        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            if (npc == null)
            {
                AddError(issues, "NpcNull", "npc", "Snapshot contains a null NPC entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(npc.RuntimeId))
            {
                AddError(issues, "NpcIdMissing", "npc", "NPC RuntimeId is empty.");
            }
            else if (npcIds.Add(npc.RuntimeId) == false)
            {
                AddError(issues, "DuplicateNpcRuntimeId", npc.RuntimeId, "NPC RuntimeId appears more than once.");
            }

            string npcIdentity = string.IsNullOrWhiteSpace(npc.RuntimeId) ? "npc" : npc.RuntimeId;
            if (npc.LifeState == NpcLifeState.Alive
                && string.IsNullOrWhiteSpace(npc.ResidenceSettlementRuntimeId) == false
                && settlementIds.Contains(npc.ResidenceSettlementRuntimeId) == false)
            {
                AddError(issues, "ResidenceSettlementMissing", npcIdentity, "Living NPC has a residence in a settlement absent from the snapshot.");
            }

            if (npc.IsTraveling && string.IsNullOrWhiteSpace(npc.DestinationLocationRuntimeId))
            {
                AddError(issues, "TravelDestinationMissing", npcIdentity, "Traveling NPC has no destination location.");
            }

            if (npc.RemainingTravelDays < 0)
            {
                AddError(issues, "NegativeTravelDays", npcIdentity, "NPC travel days remaining cannot be negative.");
            }

            if (float.IsNaN(npc.MoneyBalance) || float.IsInfinity(npc.MoneyBalance))
            {
                AddError(issues, "NonFiniteMoney", npcIdentity, "NPC money balance is not finite.");
            }

            if (npc.LifeState == NpcLifeState.Dead && npc.CurrentAction != null)
            {
                AddError(issues, "DeadNpcHasAction", npcIdentity, "Dead NPC has an active action.");
            }

            ValidateInventory(npc, issues);
            ValidateAction(npc, issues);
            ValidateTradePlan(npc, issues);
        }

        return new WorldStateInvariantReport(issues);
    }

    private static void ValidateMarketStock(WorldStateCitySnapshot city, List<WorldStateInvariantIssue> issues)
    {
        if (city.MarketStock == null)
        {
            return;
        }

        foreach (WorldStateMarketStackSnapshot stock in city.MarketStock)
        {
            if (stock == null)
            {
                AddError(issues, "MarketStockNull", city.RuntimeId, "Settlement contains a null market stock entry.");
                continue;
            }

            string identity = city.RuntimeId + "/item:" + stock.ItemDefinitionId;
            if (stock.Amount < 0 || stock.DesiredAmount < 0)
            {
                AddError(issues, "NegativeMarketStock", identity, "Market stock quantities cannot be negative.");
            }

            if (float.IsNaN(stock.CurrentPrice) || float.IsInfinity(stock.CurrentPrice))
            {
                AddError(issues, "NonFiniteMarketPrice", identity, "Market price is not finite.");
            }
        }
    }

    private static void ValidateInventory(WorldStateNpcSnapshot npc, List<WorldStateInvariantIssue> issues)
    {
        if (npc.Inventory == null)
        {
            return;
        }

        foreach (WorldStateInventoryStackSnapshot stack in npc.Inventory)
        {
            if (stack == null)
            {
                AddError(issues, "InventoryStackNull", npc.RuntimeId, "NPC inventory contains a null stack entry.");
                continue;
            }

            string identity = npc.RuntimeId + "/item:" + stack.ItemDefinitionId;
            if (stack.Amount < 0)
            {
                AddError(issues, "NegativeInventoryAmount", identity, "Inventory amount cannot be negative.");
            }

            if (float.IsNaN(stack.AverageUnitCost) || float.IsInfinity(stack.AverageUnitCost))
            {
                AddError(issues, "NonFiniteInventoryCost", identity, "Inventory average unit cost is not finite.");
            }
        }
    }

    private static void ValidateAction(WorldStateNpcSnapshot npc, List<WorldStateInvariantIssue> issues)
    {
        WorldStateActionSnapshot action = npc.CurrentAction;
        if (action == null)
        {
            return;
        }

        string identity = npc.RuntimeId + "/action";
        if (action.Amount < 0)
        {
            AddWarning(issues, "NegativeActionAmount", identity, "Action amount is negative.");
        }

        if (float.IsNaN(action.ExpectedUnitPrice) || float.IsInfinity(action.ExpectedUnitPrice)
            || float.IsNaN(action.ExpectedNetValue) || float.IsInfinity(action.ExpectedNetValue)
            || float.IsNaN(action.SuccessChanceMultiplier) || float.IsInfinity(action.SuccessChanceMultiplier))
        {
            AddWarning(issues, "NonFiniteActionValue", identity, "An action diagnostic value is not finite.");
        }
    }

    private static void ValidateTradePlan(WorldStateNpcSnapshot npc, List<WorldStateInvariantIssue> issues)
    {
        WorldStateMerchantTradePlanSnapshot plan = npc.MerchantTradePlan;
        if (plan == null)
        {
            return;
        }

        string identity = npc.RuntimeId + "/trade-plan";
        if (plan.PlannedAmount < 0 || plan.RemainingAmount < 0 || plan.WaitDaysAtDestination < 0 || plan.PendingTravelDays < 0)
        {
            AddWarning(issues, "NegativeTradePlanValue", identity, "Merchant trade plan contains a negative quantity or day count.");
        }

        if (float.IsNaN(plan.PurchasePricePerItem) || float.IsInfinity(plan.PurchasePricePerItem))
        {
            AddWarning(issues, "NonFiniteTradePlanPrice", identity, "Merchant trade plan purchase price is not finite.");
        }
    }

    private static void AddWarning(List<WorldStateInvariantIssue> issues, string code, string identity, string message)
    {
        issues.Add(new WorldStateInvariantIssue(WorldStateInvariantSeverity.Warning, code, identity, message));
    }

    private static void AddError(List<WorldStateInvariantIssue> issues, string code, string identity, string message)
    {
        issues.Add(new WorldStateInvariantIssue(WorldStateInvariantSeverity.Error, code, identity, message));
    }
}
