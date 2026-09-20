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

        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, string> personBindings = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> personResidences = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, long?> personDeaths = new Dictionary<string, long?>(StringComparer.Ordinal);
        HashSet<string> personBoundNpcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePersonSnapshot person in snapshot.Persons)
        {
            if (person == null)
            {
                AddError(issues, "PersonNull", "person", "Snapshot contains a null Person entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(person.PersonId))
            {
                AddError(issues, "PersonIdMissing", "person", "PersonId is empty.");
            }
            else if (personIds.Add(person.PersonId) == false)
            {
                AddError(issues, "DuplicatePersonId", person.PersonId, "PersonId appears more than once.");
            }

            string personIdentity = string.IsNullOrWhiteSpace(person.PersonId) ? "person" : person.PersonId;
            if (string.IsNullOrWhiteSpace(person.PersonId) == false)
            {
                if (string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId) == false
                    && settlementIds.Contains(person.ResidenceSettlementRuntimeId) == false)
                {
                    AddError(issues, "PersonResidenceSettlementMissing", personIdentity, "Person has a residence in a settlement absent from the snapshot.");
                }

                personResidences[person.PersonId] = person.ResidenceSettlementRuntimeId;
                personDeaths[person.PersonId] = person.DeathAbsoluteDay;
            }

            if (person.BirthAbsoluteDay.HasValue)
            {
                if (person.BirthAbsoluteDay.Value < 0L)
                {
                    AddError(issues, "NegativePersonBirthAbsoluteDay", personIdentity, "Person BirthAbsoluteDay cannot be negative.");
                }
                else if (person.BirthAbsoluteDay.Value > snapshot.AbsoluteDay)
                {
                    AddError(issues, "FuturePersonBirthAbsoluteDay", personIdentity, "Person BirthAbsoluteDay cannot be later than the snapshot day.");
                }
            }

            if (person.DeathAbsoluteDay.HasValue)
            {
                if (person.DeathAbsoluteDay.Value < 0L)
                {
                    AddError(issues, "NegativePersonDeathAbsoluteDay", personIdentity, "Person DeathAbsoluteDay cannot be negative.");
                }
                else if (person.DeathAbsoluteDay.Value > snapshot.AbsoluteDay)
                {
                    AddError(issues, "FuturePersonDeathAbsoluteDay", personIdentity, "Person DeathAbsoluteDay cannot be later than the snapshot day.");
                }
                else if (person.BirthAbsoluteDay.HasValue
                    && person.DeathAbsoluteDay.Value < person.BirthAbsoluteDay.Value)
                {
                    AddError(issues, "PersonDeathBeforeBirth", personIdentity, "Person DeathAbsoluteDay cannot be earlier than BirthAbsoluteDay.");
                }
            }

            if (person.AgeInDays.HasValue && person.BirthAbsoluteDay.HasValue == false)
            {
                AddError(issues, "PersonAgeWithoutBirth", personIdentity, "Person age cannot be known when BirthAbsoluteDay is unknown.");
            }

            if (person.CompletedYears.HasValue && person.BirthAbsoluteDay.HasValue == false)
            {
                AddError(issues, "PersonYearsWithoutBirth", personIdentity, "Person completed years cannot be known when BirthAbsoluteDay is unknown.");
            }

            if (person.AgeInDays.HasValue && person.AgeInDays.Value < 0L)
            {
                AddError(issues, "NegativePersonAgeInDays", personIdentity, "Person AgeInDays cannot be negative.");
            }

            if (person.CompletedYears.HasValue && person.CompletedYears.Value < 0L)
            {
                AddError(issues, "NegativePersonCompletedYears", personIdentity, "Person CompletedYears cannot be negative.");
            }

            if (person.IsMaterialized)
            {
                if (string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId))
                {
                    AddError(issues, "PersonMaterializedRuntimeMissing", person.PersonId, "Materialized Person has no NPC RuntimeId binding.");
                }
                else
                {
                    if (personBoundNpcIds.Add(person.MaterializedNpcRuntimeId) == false)
                    {
                        AddError(issues, "DuplicatePersonMaterializedRuntimeId", person.MaterializedNpcRuntimeId, "More than one Person is bound to the same NPC RuntimeId.");
                    }

                    if (npcIds.Contains(person.MaterializedNpcRuntimeId) == false)
                    {
                        AddError(issues, "PersonMaterializedNpcMissing", person.PersonId, "Materialized Person references an NPC absent from the snapshot.");
                    }

                    personBindings[person.PersonId] = person.MaterializedNpcRuntimeId;
                }
            }
        }

        ValidateParentages(snapshot.Parentages, personIds, issues);
        HashSet<string> propertyIds = ValidatePropertyOwnerships(
            snapshot.PropertyOwnerships,
            personIds,
            issues);
        ValidatePropertyTransfers(
            snapshot.PropertyTransfers,
            propertyIds,
            personIds,
            snapshot.AbsoluteDay,
            issues);
        ValidateEstates(snapshot.Estates, personIds, personDeaths, snapshot.AbsoluteDay, issues);

        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.PersonId))
            {
                continue;
            }

            string npcIdentity = string.IsNullOrWhiteSpace(npc.RuntimeId) ? "npc" : npc.RuntimeId;
            if (personIds.Contains(npc.PersonId) == false)
            {
                AddError(issues, "NpcPersonMissing", npcIdentity, "NPC references a Person absent from the snapshot.");
            }
            else if (personBindings.TryGetValue(npc.PersonId, out string boundRuntimeId) == false
                || StringComparer.Ordinal.Equals(boundRuntimeId, npc.RuntimeId) == false)
            {
                AddError(issues, "PersonBindingMismatch", npcIdentity, "NPC PersonId does not match the Person snapshot binding.");
            }

            if (personResidences.TryGetValue(npc.PersonId, out string personResidence) == true
                && string.Equals(personResidence, npc.ResidenceSettlementRuntimeId, StringComparison.Ordinal) == false)
            {
                AddError(issues, "PersonNpcResidenceMismatch", npcIdentity, "Person-backed NPC residence diverges from the Person residence authority.");
            }


            if (personDeaths.TryGetValue(npc.PersonId, out long? personDeath) == true)
            {
                bool personIsDead = personDeath.HasValue && personDeath.Value <= snapshot.AbsoluteDay;
                if (personIsDead != (npc.LifeState == NpcLifeState.Dead))
                {
                    AddError(issues, "PersonNpcLifeStateMismatch", npcIdentity, "Person-backed NPC life state diverges from factual Person death.");
                }
            }
        }

        foreach (WorldStateCitySnapshot city in snapshot.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId))
            {
                continue;
            }

            HashSet<string> representedResidentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorldStatePersonSnapshot person in snapshot.Persons)
            {
                if (person != null
                    && (person.DeathAbsoluteDay.HasValue == false
                        || person.DeathAbsoluteDay.Value > snapshot.AbsoluteDay)
                    && string.Equals(person.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    && string.IsNullOrWhiteSpace(person.PersonId) == false)
                {
                    representedResidentIds.Add("person:" + person.PersonId);
                }
            }

            foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
            {
                if (npc != null
                    && npc.LifeState == NpcLifeState.Alive
                    && string.IsNullOrWhiteSpace(npc.PersonId) == true
                    && string.Equals(npc.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    && string.IsNullOrWhiteSpace(npc.RuntimeId) == false)
                {
                    representedResidentIds.Add("npc:" + npc.RuntimeId);
                }
            }

            if (representedResidentIds.Count > city.CurrentPopulation)
            {
                AddError(issues, "RepresentedResidentsExceedPopulation", city.RuntimeId, "Represented residents exceed aggregate settlement population.");
            }
        }

        return new WorldStateInvariantReport(issues);
    }

    private static void ValidateParentages(
        IReadOnlyList<WorldStateParentageSnapshot> parentages,
        HashSet<string> personIds,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> uniqueEdges = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, List<string>> childrenByParent =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        Dictionary<string, int> incomingCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (string personId in personIds)
        {
            incomingCounts[personId] = 0;
        }

        if (parentages == null)
        {
            return;
        }

        foreach (WorldStateParentageSnapshot parentage in parentages)
        {
            if (parentage == null)
            {
                AddError(issues, "ParentageNull", "parentage", "Snapshot contains a null parentage entry.");
                continue;
            }

            string parentId = parentage.ParentPersonId;
            string childId = parentage.ChildPersonId;
            string identity = (parentId ?? "parent") + " -> " + (childId ?? "child");
            bool parentValid = string.IsNullOrWhiteSpace(parentId) == false
                && personIds.Contains(parentId);
            bool childValid = string.IsNullOrWhiteSpace(childId) == false
                && personIds.Contains(childId);

            if (string.IsNullOrWhiteSpace(parentId))
            {
                AddError(issues, "ParentageParentMissing", identity, "Parentage has no parent PersonId.");
            }
            else if (personIds.Contains(parentId) == false)
            {
                AddError(issues, "GenealogyParentMissing", identity, "Parentage parent is absent from the Person snapshot.");
            }

            if (string.IsNullOrWhiteSpace(childId))
            {
                AddError(issues, "ParentageChildMissing", identity, "Parentage has no child PersonId.");
            }
            else if (personIds.Contains(childId) == false)
            {
                AddError(issues, "GenealogyChildMissing", identity, "Parentage child is absent from the Person snapshot.");
            }

            if (parentValid && childValid && string.Equals(parentId, childId, StringComparison.Ordinal))
            {
                AddError(issues, "GenealogySelfParent", identity, "A Person cannot be their own parent.");
            }

            string edgeKey = (parentId ?? string.Empty) + "\u001f" + (childId ?? string.Empty);
            if (uniqueEdges.Add(edgeKey) == false)
            {
                AddError(issues, "GenealogyDuplicateParentage", identity, "Parentage relation appears more than once.");
                continue;
            }

            if (parentValid && childValid && parentId != childId)
            {
                if (childrenByParent.TryGetValue(parentId, out List<string> children) == false)
                {
                    children = new List<string>();
                    childrenByParent.Add(parentId, children);
                }

                children.Add(childId);
                incomingCounts[childId]++;
            }
        }

        Queue<string> ready = new Queue<string>();
        foreach (KeyValuePair<string, int> entry in incomingCounts)
        {
            if (entry.Value == 0)
            {
                ready.Enqueue(entry.Key);
            }
        }

        int processed = 0;
        while (ready.Count > 0)
        {
            string parentId = ready.Dequeue();
            processed++;
            if (childrenByParent.TryGetValue(parentId, out List<string> children) == false)
            {
                continue;
            }

            foreach (string childId in children)
            {
                incomingCounts[childId]--;
                if (incomingCounts[childId] == 0)
                {
                    ready.Enqueue(childId);
                }
            }
        }

        if (processed != incomingCounts.Count)
        {
            AddError(issues, "GenealogyCycle", "genealogy", "Parentage relations contain a cycle.");
        }
    }

    private static HashSet<string> ValidatePropertyOwnerships(
        IReadOnlyList<WorldStatePropertyOwnershipSnapshot> ownerships,
        HashSet<string> personIds,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> propertyIds = new HashSet<string>(StringComparer.Ordinal);
        if (ownerships == null)
        {
            return propertyIds;
        }

        foreach (WorldStatePropertyOwnershipSnapshot ownership in ownerships)
        {
            if (ownership == null)
            {
                AddError(issues, "PropertyOwnershipNull", "property", "Snapshot contains a null property ownership entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(ownership.PropertyId)
                ? "property"
                : ownership.PropertyId;
            if (string.IsNullOrWhiteSpace(ownership.PropertyId))
            {
                AddError(issues, "PropertyIdMissing", identity, "Property ownership has no PropertyId.");
            }
            else if (propertyIds.Add(ownership.PropertyId) == false)
            {
                AddError(issues, "DuplicatePropertyId", identity, "PropertyId appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(ownership.OwnerPersonId))
            {
                AddError(issues, "PropertyOwnerMissing", identity, "Property ownership has no owner PersonId.");
            }
            else if (personIds.Contains(ownership.OwnerPersonId) == false)
            {
                AddError(issues, "PropertyOwnerPersonMissing", identity, "Property owner PersonId is absent from the Person snapshot.");
            }
        }

        return propertyIds;
    }

    private static void ValidatePropertyTransfers(
        IReadOnlyList<WorldStatePropertyTransferSnapshot> transfers,
        HashSet<string> propertyIds,
        HashSet<string> personIds,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> transferIds = new HashSet<string>(StringComparer.Ordinal);
        if (transfers == null)
        {
            return;
        }

        foreach (WorldStatePropertyTransferSnapshot transfer in transfers)
        {
            if (transfer == null)
            {
                AddError(issues, "PropertyTransferNull", "property-transfer", "Snapshot contains a null property transfer entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(transfer.PropertyId)
                ? "property-transfer"
                : transfer.PropertyId + "@" + transfer.TransferAbsoluteDay;
            string transferKey = identity
                + "\u001f"
                + (transfer.PreviousOwnerPersonId ?? string.Empty)
                + "\u001f"
                + (transfer.NewOwnerPersonId ?? string.Empty);
            if (transferIds.Add(transferKey) == false)
            {
                AddError(issues, "DuplicatePropertyTransfer", identity, "Property transfer history entry appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(transfer.PropertyId))
            {
                AddError(issues, "PropertyTransferPropertyMissing", identity, "Property transfer has no PropertyId.");
            }
            else if (propertyIds.Contains(transfer.PropertyId) == false)
            {
                AddError(issues, "PropertyTransferPropertyAbsent", identity, "Property transfer references a property absent from the snapshot.");
            }

            if (string.IsNullOrWhiteSpace(transfer.PreviousOwnerPersonId)
                || personIds.Contains(transfer.PreviousOwnerPersonId) == false)
            {
                AddError(issues, "PropertyTransferPreviousOwnerMissing", identity, "Property transfer previous owner is absent from the Person snapshot.");
            }

            if (string.IsNullOrWhiteSpace(transfer.NewOwnerPersonId)
                || personIds.Contains(transfer.NewOwnerPersonId) == false)
            {
                AddError(issues, "PropertyTransferNewOwnerMissing", identity, "Property transfer new owner is absent from the Person snapshot.");
            }

            if (string.Equals(
                    transfer.PreviousOwnerPersonId,
                    transfer.NewOwnerPersonId,
                    StringComparison.Ordinal))
            {
                AddError(issues, "PropertyTransferSameOwner", identity, "Property transfer previous and new owners must differ.");
            }

            if (transfer.TransferAbsoluteDay < 0L)
            {
                AddError(issues, "NegativePropertyTransferDay", identity, "Property transfer day cannot be negative.");
            }
            else if (transfer.TransferAbsoluteDay > absoluteDay)
            {
                AddError(issues, "FuturePropertyTransferDay", identity, "Property transfer day cannot be later than the snapshot day.");
            }
        }
    }

    private static void ValidateEstates(
        IReadOnlyList<WorldStateEstateSnapshot> estates,
        HashSet<string> personIds,
        Dictionary<string, long?> personDeaths,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> estateIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> deceasedPersonIds = new HashSet<string>(StringComparer.Ordinal);
        if (estates == null)
        {
            return;
        }

        foreach (WorldStateEstateSnapshot estate in estates)
        {
            if (estate == null)
            {
                AddError(issues, "EstateNull", "estate", "Snapshot contains a null estate entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(estate.EstateId) ? "estate" : estate.EstateId;
            if (string.IsNullOrWhiteSpace(estate.EstateId))
            {
                AddError(issues, "EstateIdMissing", identity, "Estate has no EstateId.");
            }
            else if (estateIds.Add(estate.EstateId) == false)
            {
                AddError(issues, "DuplicateEstateId", identity, "EstateId appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(estate.DeceasedPersonId))
            {
                AddError(issues, "EstateDeceasedPersonMissing", identity, "Estate has no deceased PersonId.");
            }
            else
            {
                if (personIds.Contains(estate.DeceasedPersonId) == false)
                {
                    AddError(issues, "EstateDeceasedPersonAbsent", identity, "Estate deceased PersonId is absent from the Person snapshot.");
                }

                if (personDeaths.TryGetValue(estate.DeceasedPersonId, out long? deathAbsoluteDay) == false
                    || deathAbsoluteDay.HasValue == false
                    || deathAbsoluteDay.Value > estate.OpenedAbsoluteDay)
                {
                    AddError(issues, "EstatePersonNotFactuallyDead", identity, "Estate opening requires a factual Person death on or before the opening day.");
                }

                if (deceasedPersonIds.Add(estate.DeceasedPersonId) == false)
                {
                    AddError(issues, "DuplicateEstateForPerson", identity, "A Person has more than one estate.");
                }
            }

            if (estate.OpenedAbsoluteDay < 0L)
            {
                AddError(issues, "NegativeEstateOpeningDay", identity, "Estate opening day cannot be negative.");
            }
            else if (estate.OpenedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "FutureEstateOpeningDay", identity, "Estate opening day cannot be later than the snapshot day.");
            }
        }
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
