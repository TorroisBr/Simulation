using System;
using System.Collections.Generic;
using System.Globalization;

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
        ValidatePoliticalClaims(
            snapshot.PoliticalClaims,
            personIds,
            snapshot.InstitutionIds,
            snapshot.HasInstitutionCatalog,
            snapshot.OfficeIds,
            snapshot.HasOfficeCatalog,
            snapshot.PropertyIds,
            snapshot.HasPropertyCatalog,
            snapshot.AbsoluteDay,
            issues);
        ValidatePoliticalClaimRecognitions(
            snapshot.PoliticalClaimRecognitions,
            snapshot.PoliticalClaims,
            snapshot.InstitutionIds,
            snapshot.HasInstitutionCatalog,
            snapshot.AbsoluteDay,
            issues);
        ValidateFactions(snapshot.Factions, snapshot.FactionAffiliations, personIds, snapshot.AbsoluteDay, issues);
        ValidatePoliticalSupports(
            snapshot.PoliticalSupports,
            personIds,
            snapshot.Factions,
            snapshot.PoliticalClaims,
            snapshot.AbsoluteDay,
            issues);
        ValidatePoliticalDecisions(
            snapshot.PoliticalDecisions,
            personIds,
            snapshot.PoliticalClaims,
            snapshot.InstitutionIds,
            snapshot.HasInstitutionCatalog,
            snapshot.OfficeIds,
            snapshot.HasOfficeCatalog,
            snapshot.AbsoluteDay,
            issues);
        ValidatePoliticalKnowledge(
            snapshot.PoliticalKnowledge,
            snapshot.HasPoliticalKnowledgeState,
            snapshot.PoliticalKnowledgeRevision,
            personIds,
            snapshot.InstitutionIds,
            snapshot.HasInstitutionCatalog,
            snapshot.PoliticalClaims,
            snapshot.Factions,
            snapshot.OfficeIds,
            snapshot.HasOfficeCatalog,
            snapshot.OfficeInstitutionIds,
            snapshot.PropertyIds,
            snapshot.HasPropertyCatalog,
            snapshot.AbsoluteDay,
            issues);

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

    private static void ValidatePoliticalClaims(
        IReadOnlyList<WorldStatePoliticalClaimSnapshot> claims,
        HashSet<string> personIds,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        IReadOnlyList<string> officeIds,
        bool hasOfficeCatalog,
        IReadOnlyList<string> propertyIds,
        bool hasPropertyCatalog,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> institutionIdSet = hasInstitutionCatalog
            ? new HashSet<string>(institutionIds, StringComparer.Ordinal)
            : null;
        HashSet<string> officeIdSet = hasOfficeCatalog
            ? new HashSet<string>(officeIds, StringComparer.Ordinal)
            : null;
        HashSet<string> propertyIdSet = hasPropertyCatalog
            ? new HashSet<string>(propertyIds, StringComparer.Ordinal)
            : null;
        if (claims == null)
        {
            return;
        }

        foreach (WorldStatePoliticalClaimSnapshot claim in claims)
        {
            if (claim == null)
            {
                AddError(issues, "PoliticalClaimNull", "claim", "Snapshot contains a null political claim entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(claim.ClaimId) ? "claim" : claim.ClaimId;
            if (string.IsNullOrWhiteSpace(claim.ClaimId))
            {
                AddError(issues, "PoliticalClaimIdMissing", identity, "Political claim has no ClaimId.");
            }
            else if (claimIds.Add(claim.ClaimId) == false)
            {
                AddError(issues, "DuplicatePoliticalClaimId", identity, "Political ClaimId appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(claim.ClaimantPersonId))
            {
                AddError(issues, "PoliticalClaimantMissing", identity, "Political claim has no claimant PersonId.");
            }
            else if (personIds.Contains(claim.ClaimantPersonId) == false)
            {
                AddError(issues, "PoliticalClaimantPersonMissing", identity, "Political claim claimant is absent from the Person snapshot.");
            }

            if (string.IsNullOrWhiteSpace(claim.TargetId))
            {
                AddError(issues, "PoliticalClaimTargetMissing", identity, "Political claim has no target id.");
            }
            else if (PoliticalClaimRecord.IsTargetCompatible(claim.ClaimType, claim.TargetKind) == false)
            {
                AddError(issues, "PoliticalClaimTargetTypeMismatch", identity, "Political claim target kind is incompatible with its claim type.");
            }

            if (claim.TargetKind == PoliticalClaimTargetKind.Person
                && personIds.Contains(claim.TargetId) == false)
            {
                AddError(issues, "PoliticalClaimTargetPersonMissing", identity, "Political claim target PersonId is absent from the Person snapshot.");
            }
            else if (claim.TargetKind == PoliticalClaimTargetKind.Institution
                && hasInstitutionCatalog
                && institutionIdSet.Contains(claim.TargetId) == false)
            {
                AddError(issues, "PoliticalClaimTargetInstitutionMissing", identity, "Political claim target InstitutionId is absent from the institution catalog.");
            }
            else if (claim.TargetKind == PoliticalClaimTargetKind.Office
                && hasOfficeCatalog
                && officeIdSet.Contains(claim.TargetId) == false)
            {
                AddError(issues, "PoliticalClaimTargetOfficeMissing", identity, "Political claim target OfficeId is absent from the office catalog.");
            }
            else if (claim.TargetKind == PoliticalClaimTargetKind.Property
                && hasPropertyCatalog
                && propertyIdSet.Contains(claim.TargetId) == false)
            {
                AddError(issues, "PoliticalClaimTargetPropertyMissing", identity, "Political claim target PropertyId is absent from the property catalog.");
            }

            if (claim.CreatedAbsoluteDay < 0L || claim.CreatedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "PoliticalClaimCreationDayInvalid", identity, "Political claim creation day is outside the snapshot timeline.");
            }

            if (Enum.IsDefined(typeof(PoliticalClaimStatus), claim.Status) == false)
            {
                AddError(issues, "PoliticalClaimStatusInvalid", identity, "Political claim status is invalid.");
            }

            if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), claim.RecognitionState) == false)
            {
                AddError(issues, "PoliticalClaimRecognitionInvalid", identity, "Political claim recognition state is invalid.");
            }
            else if (claim.RecognitionState == PoliticalClaimRecognitionState.Unrecognized)
            {
                if (string.IsNullOrWhiteSpace(claim.RecognizingInstitutionId) == false
                    || claim.RecognitionAbsoluteDay.HasValue
                    || string.IsNullOrWhiteSpace(claim.RecognitionReason) == false)
                {
                    AddError(issues, "UnrecognizedClaimHasRecognitionMetadata", identity, "An unrecognized claim cannot carry recognition metadata.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(claim.RecognizingInstitutionId))
                {
                    AddError(issues, "RecognizedClaimInstitutionMissing", identity, "Recognized political claims require a recognizing institution.");
                }

                if (claim.RecognitionAbsoluteDay.HasValue == false
                    || claim.RecognitionAbsoluteDay.Value < claim.CreatedAbsoluteDay
                    || claim.RecognitionAbsoluteDay.Value > absoluteDay)
                {
                    AddError(issues, "PoliticalClaimRecognitionDayInvalid", identity, "Political claim recognition day is inconsistent with the claim timeline.");
                }
            }

            if (claim.RecognitionState != PoliticalClaimRecognitionState.Unrecognized
                && hasInstitutionCatalog
                && (string.IsNullOrWhiteSpace(claim.RecognizingInstitutionId)
                    || institutionIdSet.Contains(claim.RecognizingInstitutionId) == false))
            {
                AddError(issues, "PoliticalClaimRecognizingInstitutionMissing", identity, "Political claim recognizing institution is absent from the institution catalog.");
            }

            if (claim.Status == PoliticalClaimStatus.Active)
            {
                if (claim.ResolutionAbsoluteDay.HasValue)
                {
                    AddError(issues, "ActivePoliticalClaimHasResolutionDay", identity, "An active political claim cannot have a resolution day.");
                }
            }
            else if (claim.ResolutionAbsoluteDay.HasValue == false
                || claim.ResolutionAbsoluteDay.Value < claim.CreatedAbsoluteDay
                || claim.ResolutionAbsoluteDay.Value > absoluteDay)
            {
                AddError(issues, "PoliticalClaimResolutionDayInvalid", identity, "A terminal political claim requires a valid resolution day.");
            }
        }
    }

    private static void ValidateFactions(
        IReadOnlyList<WorldStateFactionSnapshot> factions,
        IReadOnlyList<WorldStateFactionAffiliationSnapshot> affiliations,
        HashSet<string> personIds,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, long> factionCreationDays = new Dictionary<string, long>(StringComparer.Ordinal);
        Dictionary<string, FactionMembershipPolicy> factionPolicies = new Dictionary<string, FactionMembershipPolicy>(StringComparer.Ordinal);
        if (factions != null)
        {
            foreach (WorldStateFactionSnapshot faction in factions)
            {
                if (faction == null)
                {
                    AddError(issues, "FactionNull", "faction", "Snapshot contains a null faction entry.");
                    continue;
                }

                string identity = string.IsNullOrWhiteSpace(faction.FactionId) ? "faction" : faction.FactionId;
                if (string.IsNullOrWhiteSpace(faction.FactionId))
                {
                    AddError(issues, "FactionIdMissing", identity, "Faction has no FactionId.");
                }
                else if (factionIds.Add(faction.FactionId) == false)
                {
                    AddError(issues, "DuplicateFactionId", identity, "FactionId appears more than once.");
                }
                else
                {
                    factionCreationDays[faction.FactionId] = faction.CreatedAbsoluteDay;
                    factionPolicies[faction.FactionId] = faction.MembershipPolicy;
                }

                if (Enum.IsDefined(typeof(FactionMembershipPolicy), faction.MembershipPolicy) == false)
                {
                    AddError(issues, "FactionMembershipPolicyInvalid", faction.FactionId, "Faction membership policy is invalid.");
                }

                if (faction.CreatedAbsoluteDay < 0L || faction.CreatedAbsoluteDay > absoluteDay)
                {
                    AddError(issues, "FactionCreationDayInvalid", identity, "Faction creation day is outside the snapshot timeline.");
                }
            }
        }

        HashSet<string> affiliationKeys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> activeAffiliationPairs = new HashSet<string>(StringComparer.Ordinal);
        if (affiliations == null)
        {
            return;
        }

        foreach (WorldStateFactionAffiliationSnapshot affiliation in affiliations)
        {
            if (affiliation == null)
            {
                AddError(issues, "FactionAffiliationNull", "affiliation", "Snapshot contains a null faction affiliation entry.");
                continue;
            }

            string identity = (affiliation.FactionId ?? "faction") + "/" + (affiliation.PersonId ?? "person");
            string key = affiliation.AffiliationId
                ?? (affiliation.FactionId ?? string.Empty) + "\u001f" + (affiliation.PersonId ?? string.Empty);
            if (affiliationKeys.Add(key) == false)
            {
                AddError(issues, "DuplicateFactionAffiliation", identity, "Faction/person affiliation appears more than once.");
            }

            string pair = (affiliation.FactionId ?? string.Empty) + "\u001f" + (affiliation.PersonId ?? string.Empty);
            if (affiliation.IsActive && activeAffiliationPairs.Add(pair) == false)
            {
                AddError(issues, "DuplicateActiveFactionAffiliation", identity, "A faction/person pair has more than one active affiliation tenure.");
            }

            if (factionPolicies.TryGetValue(affiliation.FactionId, out FactionMembershipPolicy membershipPolicy)
                && Enum.IsDefined(typeof(FactionMembershipPolicy), membershipPolicy) == false)
            {
                AddError(issues, "FactionMembershipPolicyInvalid", identity, "Faction membership policy is invalid.");
            }

            if (factionIds.Contains(affiliation.FactionId) == false)
            {
                AddError(issues, "FactionAffiliationFactionMissing", identity, "Faction affiliation references a faction absent from the snapshot.");
            }
            else if (factionCreationDays.TryGetValue(affiliation.FactionId, out long createdDay)
                && affiliation.JoinedAbsoluteDay < createdDay)
            {
                AddError(issues, "FactionAffiliationBeforeFactionCreation", identity, "Faction affiliation begins before the faction was created.");
            }

            if (string.IsNullOrWhiteSpace(affiliation.PersonId)
                || personIds.Contains(affiliation.PersonId) == false)
            {
                AddError(issues, "FactionAffiliationPersonMissing", identity, "Faction affiliation PersonId is absent from the Person snapshot.");
            }

            if (affiliation.JoinedAbsoluteDay < 0L || affiliation.JoinedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "FactionAffiliationJoinDayInvalid", identity, "Faction affiliation join day is outside the snapshot timeline.");
            }

            if (affiliation.EndedAbsoluteDay.HasValue
                && (affiliation.EndedAbsoluteDay.Value < affiliation.JoinedAbsoluteDay
                    || affiliation.EndedAbsoluteDay.Value > absoluteDay))
            {
                AddError(issues, "FactionAffiliationEndDayInvalid", identity, "Faction affiliation end day is inconsistent with the snapshot timeline.");
            }

            if (affiliation.EndReason.HasValue
                && Enum.IsDefined(typeof(FactionAffiliationEndReason), affiliation.EndReason.Value) == false)
            {
                AddError(issues, "FactionAffiliationEndReasonInvalid", identity, "Faction affiliation end reason is invalid.");
            }

            if (affiliation.EndReason.HasValue && affiliation.EndedAbsoluteDay.HasValue == false)
            {
                AddError(issues, "FactionAffiliationEndReasonInvalid", identity, "An active faction affiliation cannot carry an end reason.");
            }
        }
    }

    private static void ValidatePoliticalSupports(
        IReadOnlyList<WorldStatePoliticalSupportSnapshot> supports,
        HashSet<string> personIds,
        IReadOnlyList<WorldStateFactionSnapshot> factions,
        IReadOnlyList<WorldStatePoliticalClaimSnapshot> claims,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateFactionSnapshot faction in factions ?? Array.Empty<WorldStateFactionSnapshot>())
        {
            if (faction != null && string.IsNullOrWhiteSpace(faction.FactionId) == false)
            {
                factionIds.Add(faction.FactionId);
            }
        }

        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalClaimSnapshot claim in claims ?? Array.Empty<WorldStatePoliticalClaimSnapshot>())
        {
            if (claim != null && string.IsNullOrWhiteSpace(claim.ClaimId) == false)
            {
                claimIds.Add(claim.ClaimId);
            }
        }

        HashSet<string> relationIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> activePairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalSupportSnapshot support in supports ?? Array.Empty<WorldStatePoliticalSupportSnapshot>())
        {
            if (support == null)
            {
                AddError(issues, "PoliticalSupportNull", "support", "Snapshot contains a null political support entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(support.RelationId) ? "support" : support.RelationId;
            if (string.IsNullOrWhiteSpace(support.RelationId))
            {
                AddError(issues, "PoliticalSupportIdMissing", identity, "Political support relation has no RelationId.");
            }
            else if (relationIds.Add(support.RelationId) == false)
            {
                AddError(issues, "DuplicatePoliticalSupportId", identity, "Political support RelationId appears more than once.");
            }

            bool validSource = Enum.IsDefined(typeof(PoliticalSupportSourceKind), support.SourceKind)
                && string.IsNullOrWhiteSpace(support.SourceId) == false;
            if (validSource == false)
            {
                AddError(issues, "PoliticalSupportSourceInvalid", identity, "Political support source is invalid.");
            }
            else if (support.SourceKind == PoliticalSupportSourceKind.Person && personIds.Contains(support.SourceId) == false)
            {
                AddError(issues, "PoliticalSupportSourcePersonMissing", identity, "Political support source PersonId is absent.");
            }
            else if (support.SourceKind == PoliticalSupportSourceKind.Faction && factionIds.Contains(support.SourceId) == false)
            {
                AddError(issues, "PoliticalSupportSourceFactionMissing", identity, "Political support source FactionId is absent.");
            }

            bool validTarget = Enum.IsDefined(typeof(PoliticalSupportTargetKind), support.TargetKind)
                && string.IsNullOrWhiteSpace(support.TargetId) == false;
            if (validTarget == false)
            {
                AddError(issues, "PoliticalSupportTargetInvalid", identity, "Political support target is invalid.");
            }
            else if (support.TargetKind == PoliticalSupportTargetKind.PoliticalClaim && claimIds.Contains(support.TargetId) == false)
            {
                AddError(issues, "PoliticalSupportTargetClaimMissing", identity, "Political support target claim is absent.");
            }
            else if (support.TargetKind == PoliticalSupportTargetKind.SuccessionCandidate && personIds.Contains(support.TargetId) == false)
            {
                AddError(issues, "PoliticalSupportTargetCandidateMissing", identity, "Political support target candidate PersonId is absent.");
            }

            if (Enum.IsDefined(typeof(PoliticalSupportDisposition), support.Disposition) == false)
            {
                AddError(issues, "PoliticalSupportDispositionInvalid", identity, "Political support disposition is invalid.");
            }

            if (support.StartedAbsoluteDay < 0L || support.StartedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "PoliticalSupportStartDayInvalid", identity, "Political support start day is outside the snapshot timeline.");
            }

            if (support.EndedAbsoluteDay.HasValue
                && (support.EndedAbsoluteDay.Value < support.StartedAbsoluteDay
                    || support.EndedAbsoluteDay.Value > absoluteDay))
            {
                AddError(issues, "PoliticalSupportEndDayInvalid", identity, "Political support end day is inconsistent with the snapshot timeline.");
            }

            if (support.IsActive && validSource && validTarget)
            {
                string pair = (int)support.SourceKind + ":"
                    + support.SourceId.Length + ":" + support.SourceId
                    + ":" + (int)support.TargetKind + ":"
                    + support.TargetId.Length + ":" + support.TargetId;
                if (activePairs.Add(pair) == false)
                {
                    AddError(issues, "DuplicateActivePoliticalSupportPair", identity, "More than one active support relation exists for a source and target pair.");
                }
            }
        }
    }

    private static void ValidatePoliticalDecisions(
        IReadOnlyList<WorldStatePoliticalDecisionSnapshot> decisions,
        HashSet<string> personIds,
        IReadOnlyList<WorldStatePoliticalClaimSnapshot> claims,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        IReadOnlyList<string> officeIds,
        bool hasOfficeCatalog,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> decisionIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalClaimSnapshot claim in claims ?? Array.Empty<WorldStatePoliticalClaimSnapshot>())
        {
            if (claim != null && string.IsNullOrWhiteSpace(claim.ClaimId) == false)
            {
                claimIds.Add(claim.ClaimId);
            }
        }

        foreach (WorldStatePoliticalDecisionSnapshot decision in decisions ?? Array.Empty<WorldStatePoliticalDecisionSnapshot>())
        {
            if (decision == null)
            {
                AddError(issues, "PoliticalDecisionNull", "decision", "Snapshot contains a null political decision entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(decision.DecisionId) ? "decision" : decision.DecisionId;
            if (string.IsNullOrWhiteSpace(decision.DecisionId))
            {
                AddError(issues, "PoliticalDecisionIdMissing", identity, "Political decision has no DecisionId.");
            }
            else if (decisionIds.Add(decision.DecisionId) == false)
            {
                AddError(issues, "DuplicatePoliticalDecisionId", identity, "Political DecisionId appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(decision.DeciderStableId))
            {
                AddError(issues, "PoliticalDecisionDeciderMissing", identity, "Political decision has no decider holder.");
            }
            else
            {
                int separator = decision.DeciderStableId.IndexOf(':');
                string holderKind = separator < 0 ? string.Empty : decision.DeciderStableId.Substring(0, separator);
                string holderId = separator < 0 ? string.Empty : decision.DeciderStableId.Substring(separator + 1);
                if (holderKind == PoliticalKnowledgeHolderKind.Person.ToString())
                {
                    if (personIds.Contains(holderId) == false)
                    {
                        AddError(issues, "PoliticalDecisionDeciderPersonMissing", identity, "Political decision decider PersonId is absent.");
                    }
                }
                else if (holderKind == PoliticalKnowledgeHolderKind.Institution.ToString())
                {
                    if (hasInstitutionCatalog && ContainsString(institutionIds, holderId) == false)
                    {
                        AddError(issues, "PoliticalDecisionDeciderInstitutionMissing", identity, "Political decision decider InstitutionId is absent.");
                    }
                }
                else
                {
                    AddError(issues, "PoliticalDecisionDeciderInvalid", identity, "Political decision decider holder kind is invalid.");
                }
            }

            if (Enum.IsDefined(typeof(PoliticalDecisionKind), decision.DecisionKind) == false)
            {
                AddError(issues, "PoliticalDecisionKindInvalid", identity, "Political decision kind is invalid.");
            }

            if (Enum.IsDefined(typeof(PoliticalDecisionOutcomeKind), decision.OutcomeKind) == false)
            {
                AddError(issues, "PoliticalDecisionOutcomeInvalid", identity, "Political decision outcome is invalid.");
            }

            if (decision.ObservedAbsoluteDay < 0L
                || decision.ObservedAbsoluteDay > decision.DecisionAbsoluteDay
                || decision.DecisionAbsoluteDay > absoluteDay)
            {
                AddError(issues, "PoliticalDecisionDayInvalid", identity, "Political decision days are outside the snapshot timeline.");
            }

            if (decision.ExpectedWorldRevision < 0L || decision.ExpectedKnowledgeRevision < 0L)
            {
                AddError(issues, "PoliticalDecisionRevisionInvalid", identity, "Political decision revisions cannot be negative.");
            }

            if ((decision.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                    || decision.DecisionKind == PoliticalDecisionKind.OfficeSelection)
                && string.IsNullOrWhiteSpace(decision.OfficeId))
            {
                AddError(issues, "PoliticalDecisionOfficeMissing", identity, "Office decisions must identify an office.");
            }
            else if (hasOfficeCatalog
                && string.IsNullOrWhiteSpace(decision.OfficeId) == false
                && ContainsString(officeIds, decision.OfficeId) == false)
            {
                AddError(issues, "PoliticalDecisionOfficeMissing", identity, "Political decision office is absent from the office catalog.");
            }

            if (decision.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
                && string.IsNullOrWhiteSpace(decision.RecognizingInstitutionId))
            {
                AddError(issues, "PoliticalDecisionRecognizingInstitutionMissing", identity, "Claim recognition decisions must identify a recognizing institution.");
            }
            else if (hasInstitutionCatalog
                && string.IsNullOrWhiteSpace(decision.RecognizingInstitutionId) == false
                && ContainsString(institutionIds, decision.RecognizingInstitutionId) == false)
            {
                AddError(issues, "PoliticalDecisionRecognizingInstitutionMissing", identity, "Political decision recognizing institution is absent from the institution catalog.");
            }

            foreach (string candidateId in decision.CandidatePersonIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(candidateId) || personIds.Contains(candidateId) == false)
                {
                    AddError(issues, "PoliticalDecisionCandidateMissing", identity, "Political decision candidate PersonId is absent.");
                }
            }

            if (TryBuildCandidateFingerprint(decision.CandidatePersonIds, out string expectedFingerprint) == false
                || string.Equals(expectedFingerprint, decision.CandidateFingerprint, StringComparison.Ordinal) == false)
            {
                AddError(issues, "PoliticalDecisionFingerprintInvalid", identity, "Political decision candidate fingerprint does not match its candidate set.");
            }

            bool hasSelectedCandidate = string.IsNullOrWhiteSpace(decision.SelectedCandidatePersonId) == false;
            bool selectedCandidateListed = hasSelectedCandidate
                && Array.IndexOf(ToArray(decision.CandidatePersonIds), decision.SelectedCandidatePersonId) >= 0;
            if (hasSelectedCandidate && selectedCandidateListed == false)
            {
                AddError(issues, "PoliticalDecisionSelectionMissing", identity, "Selected political decision candidate is absent from the candidate set.");
            }

            if (decision.OutcomeKind == PoliticalDecisionOutcomeKind.CandidateSelected && hasSelectedCandidate == false)
            {
                AddError(issues, "PoliticalDecisionSelectionMissing", identity, "CandidateSelected decisions must identify a candidate.");
            }

            if ((decision.OutcomeKind == PoliticalDecisionOutcomeKind.NoSelection
                    || decision.OutcomeKind == PoliticalDecisionOutcomeKind.Rejected)
                && (hasSelectedCandidate || string.IsNullOrWhiteSpace(decision.ReferencedClaimId) == false))
            {
                AddError(issues, "PoliticalDecisionOutcomeShapeInvalid", identity, "NoSelection and Rejected decisions cannot carry a candidate or claim reference.");
            }

            if ((decision.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                    || decision.DecisionKind == PoliticalDecisionKind.OfficeSelection)
                && decision.OutcomeKind != PoliticalDecisionOutcomeKind.CandidateSelected
                && decision.OutcomeKind != PoliticalDecisionOutcomeKind.NoSelection
                && decision.OutcomeKind != PoliticalDecisionOutcomeKind.Rejected)
            {
                AddError(issues, "PoliticalDecisionOutcomeShapeInvalid", identity, "Office decisions require candidate selection, no selection, or rejection.");
            }

            if (decision.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
                && decision.OutcomeKind != PoliticalDecisionOutcomeKind.ClaimRecognitionProposed)
            {
                AddError(issues, "PoliticalDecisionOutcomeShapeInvalid", identity, "Claim recognition decisions require a claim recognition proposal.");
            }

            if (string.IsNullOrWhiteSpace(decision.ReferencedClaimId) == false
                && claimIds.Contains(decision.ReferencedClaimId) == false)
            {
                AddError(issues, "PoliticalDecisionClaimMissing", identity, "Referenced political decision claim is absent.");
            }

            if (decision.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
                && string.IsNullOrWhiteSpace(decision.ReferencedClaimId))
            {
                AddError(issues, "PoliticalDecisionClaimMissing", identity, "Claim recognition decisions must identify a claim.");
            }

            if (decision.OutcomeKind == PoliticalDecisionOutcomeKind.ClaimRecognitionProposed
                && string.IsNullOrWhiteSpace(decision.ReferencedClaimId))
            {
                AddError(issues, "PoliticalDecisionClaimMissing", identity, "Claim recognition outcomes must identify a claim.");
            }

            if (decision.OutcomeKind != PoliticalDecisionOutcomeKind.CandidateSelected
                && hasSelectedCandidate)
            {
                AddError(issues, "PoliticalDecisionOutcomeShapeInvalid", identity, "Only candidate-selected outcomes may carry a selected PersonId.");
            }

            if (decision.OutcomeKind != PoliticalDecisionOutcomeKind.ClaimRecognitionProposed
                && string.IsNullOrWhiteSpace(decision.ReferencedClaimId) == false)
            {
                AddError(issues, "PoliticalDecisionOutcomeShapeInvalid", identity, "Only claim-recognition outcomes may carry a claim reference.");
            }

            ValidateDecisionReferences(decision.EvidenceReferences, identity, "Evidence", issues);
            ValidateDecisionReferences(decision.KnowledgeReferences, identity, "Knowledge", issues);
        }
    }

    private static void ValidatePoliticalKnowledge(
        IReadOnlyList<WorldStatePoliticalKnowledgeSnapshot> knowledge,
        bool hasKnowledgeState,
        long knowledgeRevision,
        HashSet<string> personIds,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        IReadOnlyList<WorldStatePoliticalClaimSnapshot> claims,
        IReadOnlyList<WorldStateFactionSnapshot> factions,
        IReadOnlyList<string> officeIds,
        bool hasOfficeCatalog,
        IReadOnlyDictionary<string, string> officeInstitutionIds,
        IReadOnlyList<string> propertyIds,
        bool hasPropertyCatalog,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalClaimSnapshot claim in claims ?? Array.Empty<WorldStatePoliticalClaimSnapshot>())
        {
            if (claim != null && string.IsNullOrWhiteSpace(claim.ClaimId) == false)
            {
                claimIds.Add(claim.ClaimId);
            }
        }

        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateFactionSnapshot faction in factions ?? Array.Empty<WorldStateFactionSnapshot>())
        {
            if (faction != null && string.IsNullOrWhiteSpace(faction.FactionId) == false)
            {
                factionIds.Add(faction.FactionId);
            }
        }

        if (knowledgeRevision < 0L)
        {
            AddError(issues, "PoliticalKnowledgeRevisionInvalid", "world", "Political knowledge revision cannot be negative.");
        }

        if (hasKnowledgeState == false
            && (knowledge.Count > 0 || knowledgeRevision != 0L))
        {
            AddError(issues, "PoliticalKnowledgeStateMissing", "world", "Political knowledge data is present without a knowledge state marker.");
        }

        HashSet<string> holderIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalKnowledgeSnapshot holder in knowledge ?? Array.Empty<WorldStatePoliticalKnowledgeSnapshot>())
        {
            if (holder == null)
            {
                AddError(issues, "PoliticalKnowledgeHolderNull", "knowledge", "Snapshot contains a null political knowledge holder.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(holder.HolderStableId)
                ? "knowledge"
                : holder.HolderStableId;
            if (string.IsNullOrWhiteSpace(holder.HolderStableId))
            {
                AddError(issues, "PoliticalKnowledgeHolderIdMissing", identity, "Political knowledge holder StableId is empty.");
            }
            else if (holderIds.Add(holder.HolderStableId) == false)
            {
                AddError(issues, "DuplicatePoliticalKnowledgeHolder", identity, "Political knowledge holder appears more than once.");
            }

            if (Enum.IsDefined(typeof(PoliticalKnowledgeHolderKind), holder.HolderKind) == false)
            {
                AddError(issues, "PoliticalKnowledgeHolderKindInvalid", identity, "Political knowledge holder kind is invalid.");
            }

            string expectedStableId = holder.HolderKind + ":"
                + (holder.HolderKind == PoliticalKnowledgeHolderKind.Person
                    ? holder.HolderPersonId
                    : holder.HolderKind == PoliticalKnowledgeHolderKind.Institution
                        ? holder.HolderInstitutionId
                        : holder.HolderFactionId);
            if (string.IsNullOrWhiteSpace(holder.HolderStableId) == false
                && string.Equals(holder.HolderStableId, expectedStableId, StringComparison.Ordinal) == false)
            {
                AddError(issues, "PoliticalKnowledgeHolderIdentityInvalid", identity, "Political knowledge holder StableId does not match its typed endpoint.");
            }

            if (holder.HolderKind == PoliticalKnowledgeHolderKind.Person)
            {
                if (string.IsNullOrWhiteSpace(holder.HolderPersonId))
                {
                    AddError(issues, "PoliticalKnowledgeHolderPersonMissing", identity, "Person knowledge holder has no PersonId.");
                }
                else if (personIds.Contains(holder.HolderPersonId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeHolderPersonMissing", identity, "Political knowledge holder PersonId is absent from the snapshot.");
                }

                if (holder.HolderInstitutionId != null)
                {
                    AddError(issues, "PoliticalKnowledgeHolderShapeInvalid", identity, "Person knowledge holder cannot carry an InstitutionId.");
                }

                if (holder.HolderFactionId != null)
                {
                    AddError(issues, "PoliticalKnowledgeHolderShapeInvalid", identity, "Person knowledge holder cannot carry a FactionId.");
                }
            }
            else if (holder.HolderKind == PoliticalKnowledgeHolderKind.Institution)
            {
                if (string.IsNullOrWhiteSpace(holder.HolderInstitutionId))
                {
                    AddError(issues, "PoliticalKnowledgeHolderInstitutionMissing", identity, "Institution knowledge holder has no InstitutionId.");
                }
                else if (hasInstitutionCatalog
                    && ContainsString(institutionIds, holder.HolderInstitutionId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeHolderInstitutionMissing", identity, "Political knowledge holder InstitutionId is absent from the snapshot.");
                }

                if (holder.HolderPersonId != null)
                {
                    AddError(issues, "PoliticalKnowledgeHolderShapeInvalid", identity, "Institution knowledge holder cannot carry a PersonId.");
                }

                if (holder.HolderFactionId != null)
                {
                    AddError(issues, "PoliticalKnowledgeHolderShapeInvalid", identity, "Institution knowledge holder cannot carry a FactionId.");
                }
            }
            else if (holder.HolderKind == PoliticalKnowledgeHolderKind.Faction)
            {
                if (string.IsNullOrWhiteSpace(holder.HolderFactionId))
                {
                    AddError(issues, "PoliticalKnowledgeHolderFactionMissing", identity, "Faction knowledge holder has no FactionId.");
                }
                else if (factionIds.Contains(holder.HolderFactionId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeHolderFactionMissing", identity, "Political knowledge holder FactionId is absent from the snapshot.");
                }

                if (holder.HolderPersonId != null || holder.HolderInstitutionId != null)
                {
                    AddError(issues, "PoliticalKnowledgeHolderShapeInvalid", identity, "Faction knowledge holder cannot carry a PersonId or InstitutionId.");
                }
            }

            HashSet<string> observationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorldStatePoliticalKnowledgeObservationSnapshot observation in holder.Observations ?? Array.Empty<WorldStatePoliticalKnowledgeObservationSnapshot>())
            {
                if (observation == null)
                {
                    AddError(issues, "PoliticalKnowledgeObservationNull", identity, "Political knowledge holder contains a null observation.");
                    continue;
                }

                string observationIdentity = identity + "/" + observation.IdentityKey;
                if (string.IsNullOrWhiteSpace(observation.IdentityKey))
                {
                    AddError(issues, "PoliticalKnowledgeObservationIdMissing", observationIdentity, "Political knowledge observation identity is empty.");
                }
                else if (observationIds.Add(observation.IdentityKey) == false)
                {
                    AddError(issues, "DuplicatePoliticalKnowledgeObservation", observationIdentity, "Political knowledge observation identity appears more than once for a holder.");
                }

                if (Enum.IsDefined(typeof(PoliticalKnowledgeFactKind), observation.FactKind) == false)
                {
                    AddError(issues, "PoliticalKnowledgeFactKindInvalid", observationIdentity, "Political knowledge observation fact kind is invalid.");
                }

                bool validIdentity = TryParsePoliticalKnowledgeIdentity(
                    observation.IdentityKey,
                    observation.FactKind,
                    out string rawIdentity);
                if (validIdentity == false)
                {
                    AddError(issues, "PoliticalKnowledgeObservationIdentityInvalid", observationIdentity, "Political knowledge observation identity does not match its fact-kind composite format.");
                }
                else
                {
                    ValidatePoliticalKnowledgeEndpoint(
                        observation,
                        rawIdentity,
                        claimIds,
                        factionIds,
                        personIds,
                        institutionIds,
                        hasInstitutionCatalog,
                        officeIds,
                        hasOfficeCatalog,
                        officeInstitutionIds,
                        propertyIds,
                        hasPropertyCatalog,
                        observationIdentity,
                        issues);
                }

                if (observation.ObservedAbsoluteDay < 0L
                    || observation.ReceivedAbsoluteDay < observation.ObservedAbsoluteDay
                    || observation.ReceivedAbsoluteDay > absoluteDay)
                {
                    AddError(issues, "PoliticalKnowledgeObservationDayInvalid", observationIdentity, "Political knowledge observation days are outside the snapshot timeline.");
                }

                if (Enum.IsDefined(typeof(PoliticalKnowledgeSource), observation.Source) == false)
                {
                    AddError(issues, "PoliticalKnowledgeSourceInvalid", observationIdentity, "Political knowledge observation source is invalid.");
                }

                if (observation.SourcePersonId != null && observation.SourceInstitutionId != null)
                {
                    AddError(issues, "PoliticalKnowledgeProvenanceShapeInvalid", observationIdentity, "Political knowledge provenance cannot identify both a Person and an Institution.");
                }

                if (observation.SourcePersonId != null
                    && personIds.Contains(observation.SourcePersonId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeProvenancePersonMissing", observationIdentity, "Political knowledge provenance source PersonId is absent from the snapshot.");
                }

                if (observation.SourceInstitutionId != null
                    && hasInstitutionCatalog
                    && ContainsString(institutionIds, observation.SourceInstitutionId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeProvenanceInstitutionMissing", observationIdentity, "Political knowledge provenance source InstitutionId is absent from the institution catalog.");
                }

                if (observation.Source == PoliticalKnowledgeSource.SharedByPerson
                    && string.IsNullOrWhiteSpace(observation.SourcePersonId))
                {
                    AddError(issues, "PoliticalKnowledgeProvenanceShapeInvalid", observationIdentity, "SharedByPerson knowledge requires a source PersonId.");
                }

                if ((observation.Source == PoliticalKnowledgeSource.SharedByInstitution
                        || observation.Source == PoliticalKnowledgeSource.InstitutionalRecord)
                    && string.IsNullOrWhiteSpace(observation.SourceInstitutionId))
                {
                    AddError(issues, "PoliticalKnowledgeProvenanceShapeInvalid", observationIdentity, "Institutional knowledge requires a source InstitutionId.");
                }

                if ((observation.Source == PoliticalKnowledgeSource.DirectObservation
                        || observation.Source == PoliticalKnowledgeSource.InitialScenarioKnowledge)
                    && (observation.SourcePersonId != null || observation.SourceInstitutionId != null))
                {
                    AddError(issues, "PoliticalKnowledgeProvenanceShapeInvalid", observationIdentity, "Direct and initial knowledge cannot carry a transmission holder.");
                }

                if (IsValidPoliticalKnowledgeStateKey(observation.FactKind, observation.StateKey) == false)
                {
                    AddError(issues, "PoliticalKnowledgeObservationStateInvalid", observationIdentity, "Political knowledge observation state key is invalid for its fact kind.");
                }
            }
        }
    }

    private static void ValidatePoliticalKnowledgeEndpoint(
        WorldStatePoliticalKnowledgeObservationSnapshot observation,
        string rawIdentity,
        HashSet<string> claimIds,
        HashSet<string> factionIds,
        HashSet<string> personIds,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        IReadOnlyList<string> officeIds,
        bool hasOfficeCatalog,
        IReadOnlyDictionary<string, string> officeInstitutionIds,
        IReadOnlyList<string> propertyIds,
        bool hasPropertyCatalog,
        string identity,
        List<WorldStateInvariantIssue> issues)
    {
        switch (observation.FactKind)
        {
            case PoliticalKnowledgeFactKind.PoliticalClaim:
                bool claimIdentityParsed = TryParseClaimKnowledgeIdentity(rawIdentity, out string claimId, out string recognitionInstitutionId);
                if (claimIdentityParsed == false)
                {
                    AddError(issues, "PoliticalKnowledgeClaimIdentityInvalid", identity, "Political knowledge claim identity is malformed.");
                }
                else if (claimIds.Contains(claimId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeClaimMissing", identity, "Political knowledge claim endpoint is absent from the snapshot.");
                }
                else if (recognitionInstitutionId != null
                    && hasInstitutionCatalog
                    && ContainsString(institutionIds, recognitionInstitutionId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeRecognitionInstitutionMissing", identity, "Political knowledge recognition institution is absent from the institution catalog.");
                }
                ValidatePoliticalClaimState(
                    observation.StateKey,
                    observation.ObservedAbsoluteDay,
                    personIds,
                    institutionIds,
                    hasInstitutionCatalog,
                    officeIds,
                    hasOfficeCatalog,
                    propertyIds,
                    hasPropertyCatalog,
                    identity,
                    issues);
                break;
            case PoliticalKnowledgeFactKind.Faction:
                if (factionIds.Contains(rawIdentity) == false)
                {
                    AddError(issues, "PoliticalKnowledgeFactionMissing", identity, "Political knowledge faction endpoint is absent from the snapshot.");
                }
                break;
            case PoliticalKnowledgeFactKind.FactionAffiliation:
                if (TryParseLengthPrefixedPair(rawIdentity, out string factionId, out string personId) == false)
                {
                    AddError(issues, "PoliticalKnowledgeAffiliationIdentityInvalid", identity, "Political knowledge affiliation identity is malformed.");
                }
                else
                {
                    if (factionIds.Contains(factionId) == false)
                    {
                        AddError(issues, "PoliticalKnowledgeFactionMissing", identity, "Political knowledge affiliation faction endpoint is absent from the snapshot.");
                    }

                    if (personIds.Contains(personId) == false)
                    {
                        AddError(issues, "PoliticalKnowledgePersonMissing", identity, "Political knowledge affiliation PersonId is absent from the snapshot.");
                    }
                }
                break;
            case PoliticalKnowledgeFactKind.OfficeVacancy:
                if (hasOfficeCatalog && ContainsString(officeIds, rawIdentity) == false)
                {
                    AddError(issues, "PoliticalKnowledgeOfficeMissing", identity, "Political knowledge office endpoint is absent from the snapshot.");
                }
                else if (TryParseOfficeVacancyState(observation.StateKey, out string institutionId, out bool isVacant, out bool isRecognized) == false)
                {
                    AddError(issues, "PoliticalKnowledgeOfficeStateInvalid", identity, "Political knowledge office state is malformed.");
                }
                else
                {
                    if (isRecognized && isVacant == false)
                    {
                        AddError(issues, "PoliticalKnowledgeOfficeStateInvalid", identity, "Recognized office vacancy requires the office to be vacant.");
                    }

                    if (officeInstitutionIds != null
                        && officeInstitutionIds.TryGetValue(rawIdentity, out string expectedInstitutionId)
                        && string.Equals(expectedInstitutionId, institutionId, StringComparison.Ordinal) == false)
                    {
                        AddError(issues, "PoliticalKnowledgeOfficeInstitutionMismatch", identity, "Political knowledge office institution does not match the authoritative office catalog.");
                    }
                }
                break;
            case PoliticalKnowledgeFactKind.PersonDeath:
                if (personIds.Contains(rawIdentity) == false)
                {
                    AddError(issues, "PoliticalKnowledgePersonMissing", identity, "Political knowledge death PersonId is absent from the snapshot.");
                }
                break;
        }
    }

    private static bool TryParsePoliticalKnowledgeIdentity(
        string composite,
        PoliticalKnowledgeFactKind factKind,
        out string rawIdentity)
    {
        rawIdentity = null;
        if (string.IsNullOrWhiteSpace(composite)
            || Enum.IsDefined(typeof(PoliticalKnowledgeFactKind), factKind) == false)
        {
            return false;
        }

        string prefix = ((int)factKind).ToString(CultureInfo.InvariantCulture) + ":";
        if (composite.StartsWith(prefix, StringComparison.Ordinal) == false)
        {
            return false;
        }

        string lengthPrefixed = composite.Substring(prefix.Length);
        if (factKind == PoliticalKnowledgeFactKind.PoliticalClaim)
        {
            if (TryReadLengthPrefixed(lengthPrefixed, 0, out string claimIdentity, out int claimIdentityEnd) == false
                || claimIdentityEnd != lengthPrefixed.Length)
            {
                return false;
            }

            if (TryParseClaimKnowledgeIdentity(claimIdentity, out _, out _) == false)
            {
                return false;
            }

            rawIdentity = claimIdentity;
            return true;
        }
        if (TryReadLengthPrefixed(lengthPrefixed, 0, out rawIdentity, out int end) == false
            || end != lengthPrefixed.Length)
        {
            rawIdentity = null;
            return false;
        }

        return true;
    }

    private static void ValidatePoliticalClaimRecognitions(
        IReadOnlyList<WorldStatePoliticalClaimRecognitionSnapshot> recognitions,
        IReadOnlyList<WorldStatePoliticalClaimSnapshot> claims,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalClaimSnapshot claim in claims ?? Array.Empty<WorldStatePoliticalClaimSnapshot>())
        {
            if (claim?.ClaimId != null) claimIds.Add(claim.ClaimId);
        }

        HashSet<string> relationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStatePoliticalClaimRecognitionSnapshot recognition in recognitions ?? Array.Empty<WorldStatePoliticalClaimRecognitionSnapshot>())
        {
            if (recognition == null)
            {
                AddError(issues, "PoliticalClaimRecognitionNull", "recognition", "Snapshot contains a null claim recognition relation.");
                continue;
            }

            string identity = recognition.ClaimId + "\u001f" + recognition.InstitutionId;
            if (relationIds.Add(identity) == false)
            {
                AddError(issues, "DuplicatePoliticalClaimRecognition", identity, "Claim recognition relation appears more than once.");
            }
            if (claimIds.Contains(recognition.ClaimId) == false)
            {
                AddError(issues, "PoliticalClaimRecognitionClaimMissing", identity, "Claim recognition references a claim absent from the snapshot.");
            }
            if (hasInstitutionCatalog && ContainsString(institutionIds, recognition.InstitutionId) == false)
            {
                AddError(issues, "PoliticalClaimRecognitionInstitutionMissing", identity, "Claim recognition institution is absent from the institution catalog.");
            }
            if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognition.State)
                == false)
            {
                AddError(issues, "PoliticalClaimRecognitionStateInvalid", identity, "Claim recognition relation has an invalid recognition state.");
            }
            if (recognition.RecognitionAbsoluteDay < 0L || recognition.RecognitionAbsoluteDay > absoluteDay)
            {
                AddError(issues, "PoliticalClaimRecognitionDayInvalid", identity, "Claim recognition day is outside the snapshot timeline.");
            }

            PoliticalClaimRecognitionState? previousState = null;
            long previousDay = -1L;
            foreach (WorldStatePoliticalClaimRecognitionHistorySnapshot entry in recognition.History ?? Array.Empty<WorldStatePoliticalClaimRecognitionHistorySnapshot>())
            {
                if (entry == null || Enum.IsDefined(typeof(PoliticalClaimRecognitionState), entry.State) == false
                    || entry.RecognitionAbsoluteDay < previousDay)
                {
                    AddError(issues, "PoliticalClaimRecognitionHistoryInvalid", identity, "Claim recognition history is malformed or not chronological.");
                    continue;
                }
                previousState = entry.State;
                previousDay = entry.RecognitionAbsoluteDay;
            }

            if (previousState.HasValue && previousState.Value != recognition.State)
            {
                AddError(issues, "PoliticalClaimRecognitionHistoryCurrentMismatch", identity, "Claim recognition history does not end at the current relation state.");
            }
        }
    }

    private static bool TryParseClaimKnowledgeIdentity(
        string value,
        out string claimId,
        out string institutionId)
    {
        claimId = null;
        institutionId = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int unrecognizedMarker = value.LastIndexOf("0:", StringComparison.Ordinal);
        if (unrecognizedMarker == value.Length - 2
            && TryReadLengthPrefixed(value.Substring(0, unrecognizedMarker), 0, out claimId, out int unrecognizedEnd)
            && unrecognizedEnd == unrecognizedMarker)
        {
            return true;
        }

        if (TryReadLengthPrefixed(value, 0, out claimId, out int claimEnd) == false
            || claimEnd >= value.Length)
        {
            claimId = null;
            return false;
        }

        if (value[claimEnd] == '0'
            && value.Substring(claimEnd + 1) == ":")
        {
            return true;
        }

        if (value[claimEnd] != '1'
            || claimEnd + 1 >= value.Length
            || value[claimEnd + 1] != ':')
        {
            claimId = null;
            return false;
        }

        return TryReadLengthPrefixed(value, claimEnd + 2, out institutionId, out int institutionEnd)
            && institutionEnd == value.Length;
    }

    private static bool TryParseLengthPrefixedPair(
        string value,
        out string first,
        out string second)
    {
        first = null;
        second = null;
        if (TryReadLengthPrefixed(value, 0, out first, out int firstEnd) == false
            || TryReadLengthPrefixed(value, firstEnd, out second, out int secondEnd) == false
            || secondEnd != value.Length)
        {
            first = null;
            second = null;
            return false;
        }

        return true;
    }

    private static bool TryReadLengthPrefixed(
        string value,
        int start,
        out string component,
        out int end)
    {
        component = null;
        end = start;
        if (value == null || start < 0 || start >= value.Length)
        {
            return false;
        }

        int separator = value.IndexOf(':', start);
        if (separator <= start
            || int.TryParse(
                value.Substring(start, separator - start),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int length) == false
            || length < 0)
        {
            return false;
        }

        int componentStart = separator + 1;
        if (componentStart + length > value.Length)
        {
            return false;
        }

        component = value.Substring(componentStart, length);
        end = componentStart + length;
        return true;
    }

    private static void ValidatePoliticalClaimState(
        string stateKey,
        long observedAbsoluteDay,
        HashSet<string> personIds,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        IReadOnlyList<string> officeIds,
        bool hasOfficeCatalog,
        IReadOnlyList<string> propertyIds,
        bool hasPropertyCatalog,
        string identity,
        List<WorldStateInvariantIssue> issues)
    {
        string[] fields = stateKey?.Split(new[] { '\u001F' });
        if (fields == null || fields.Length != 12
            || TryReadBooleanToken(fields[0], out _) == false
            || TryReadLengthPrefixedWhole(fields[1], out string claimantPersonId) == false
            || int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out int claimTypeValue) == false
            || int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int targetKindValue) == false
            || TryReadLengthPrefixedWhole(fields[4], out string targetId) == false
            || int.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out int basisValue) == false
            || long.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out long createdAbsoluteDay) == false
            || int.TryParse(fields[7], NumberStyles.None, CultureInfo.InvariantCulture, out int statusValue) == false
            || TryReadOptionalLong(fields[8], out long? resolutionAbsoluteDay) == false
            || int.TryParse(fields[9], NumberStyles.None, CultureInfo.InvariantCulture, out int recognitionStateValue) == false
            || TryReadOptionalLengthPrefixed(fields[10], out string recognizingInstitutionId) == false
            || TryReadOptionalLong(fields[11], out long? recognitionAbsoluteDay) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "Political knowledge claim state is not a complete typed payload.");
            return;
        }

        PoliticalClaimType claimType = (PoliticalClaimType)claimTypeValue;
        PoliticalClaimTargetKind targetKind = (PoliticalClaimTargetKind)targetKindValue;
        PoliticalClaimBasis basis = (PoliticalClaimBasis)basisValue;
        PoliticalClaimStatus status = (PoliticalClaimStatus)statusValue;
        PoliticalClaimRecognitionState recognitionState = (PoliticalClaimRecognitionState)recognitionStateValue;
        if (Enum.IsDefined(typeof(PoliticalClaimType), claimType) == false
            || Enum.IsDefined(typeof(PoliticalClaimTargetKind), targetKind) == false
            || Enum.IsDefined(typeof(PoliticalClaimBasis), basis) == false
            || Enum.IsDefined(typeof(PoliticalClaimStatus), status) == false
            || Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognitionState) == false
            || PoliticalClaimRecord.IsTargetCompatible(claimType, targetKind) == false
            || string.IsNullOrWhiteSpace(claimantPersonId)
            || string.IsNullOrWhiteSpace(targetId)
            || createdAbsoluteDay < 0L
            || createdAbsoluteDay > observedAbsoluteDay
            || observedAbsoluteDay < 0L)
        {
            AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "Political knowledge claim state contains an invalid typed value or timeline.");
            return;
        }

        if (personIds.Contains(claimantPersonId) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimantPersonMissing", identity, "Political knowledge claim claimant PersonId is absent from the snapshot.");
        }

        if (targetKind == PoliticalClaimTargetKind.Person
            && personIds.Contains(targetId) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimTargetPersonMissing", identity, "Political knowledge claim target PersonId is absent from the snapshot.");
        }
        else if (targetKind == PoliticalClaimTargetKind.Institution
            && hasInstitutionCatalog
            && ContainsString(institutionIds, targetId) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimTargetInstitutionMissing", identity, "Political knowledge claim target InstitutionId is absent from the institution catalog.");
        }
        else if (targetKind == PoliticalClaimTargetKind.Office
            && hasOfficeCatalog
            && ContainsString(officeIds, targetId) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimTargetOfficeMissing", identity, "Political knowledge claim target OfficeId is absent from the office catalog.");
        }
        else if (targetKind == PoliticalClaimTargetKind.Property
            && hasPropertyCatalog
            && ContainsString(propertyIds, targetId) == false)
        {
            AddError(issues, "PoliticalKnowledgeClaimTargetPropertyMissing", identity, "Political knowledge claim target PropertyId is absent from the property catalog.");
        }

        if (status == PoliticalClaimStatus.Active)
        {
            if (resolutionAbsoluteDay.HasValue)
            {
                AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "An active political knowledge claim cannot carry a resolution day.");
            }
        }
        else if (resolutionAbsoluteDay.HasValue == false
            || resolutionAbsoluteDay.Value < createdAbsoluteDay
            || resolutionAbsoluteDay.Value > observedAbsoluteDay)
        {
            AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "A terminal political knowledge claim requires a valid resolution day.");
        }

        if (recognitionState == PoliticalClaimRecognitionState.Unrecognized)
        {
            bool hasRecognitionPerspective = string.IsNullOrWhiteSpace(recognizingInstitutionId) == false;
            if (hasRecognitionPerspective != recognitionAbsoluteDay.HasValue
                || (recognitionAbsoluteDay.HasValue
                    && (recognitionAbsoluteDay.Value < createdAbsoluteDay
                        || recognitionAbsoluteDay.Value > observedAbsoluteDay)))
            {
                AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "An unrecognized political knowledge claim must carry either no recognition perspective or a valid institution and recognition day.");
            }
            else if (hasRecognitionPerspective
                && hasInstitutionCatalog
                && ContainsString(institutionIds, recognizingInstitutionId) == false)
            {
                AddError(issues, "PoliticalKnowledgeClaimRecognizingInstitutionMissing", identity, "Political knowledge claim recognizing institution is absent from the institution catalog.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(recognizingInstitutionId)
                || recognitionAbsoluteDay.HasValue == false
                || recognitionAbsoluteDay.Value < createdAbsoluteDay
                || recognitionAbsoluteDay.Value > observedAbsoluteDay)
            {
                AddError(issues, "PoliticalKnowledgeClaimStateInvalid", identity, "A recognized political knowledge claim requires valid recognition metadata.");
            }
            else if (hasInstitutionCatalog
                && ContainsString(institutionIds, recognizingInstitutionId) == false)
            {
                AddError(issues, "PoliticalKnowledgeClaimRecognizingInstitutionMissing", identity, "Political knowledge claim recognizing institution is absent from the institution catalog.");
            }
        }
    }

    private static string LengthKey(string value)
    {
        return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
    }

    private static bool TryReadLengthPrefixedWhole(string value, out string component)
    {
        component = null;
        return value != null
            && TryReadLengthPrefixed(value, 0, out component, out int end)
            && end == value.Length;
    }

    private static bool TryReadOptionalLengthPrefixed(string value, out string component)
    {
        if (string.IsNullOrEmpty(value))
        {
            component = null;
            return true;
        }

        return TryReadLengthPrefixedWhole(value, out component);
    }

    private static bool TryReadOptionalLong(string value, out long? result)
    {
        result = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) == false)
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static bool TryParseOfficeVacancyState(
        string stateKey,
        out string institutionId,
        out bool isVacant,
        out bool isRecognized)
    {
        institutionId = null;
        isVacant = false;
        isRecognized = false;
        if (string.IsNullOrWhiteSpace(stateKey))
        {
            return false;
        }

        int firstSeparator = stateKey.IndexOf('\u001F');
        int secondSeparator = firstSeparator < 0
            ? -1
            : stateKey.IndexOf('\u001F', firstSeparator + 1);
        if (firstSeparator <= 0 || secondSeparator <= firstSeparator + 1)
        {
            return false;
        }

        if (TryReadLengthPrefixed(
                stateKey.Substring(0, firstSeparator),
                0,
                out institutionId,
                out int institutionEnd) == false
            || institutionEnd != firstSeparator
            || TryReadBooleanToken(
                stateKey.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1),
                out isVacant) == false
            || TryReadBooleanToken(
                stateKey.Substring(secondSeparator + 1),
                out isRecognized) == false)
        {
            institutionId = null;
            isVacant = false;
            isRecognized = false;
            return false;
        }

        return string.IsNullOrWhiteSpace(institutionId) == false;
    }

    private static bool TryReadBooleanToken(string value, out bool result)
    {
        result = false;
        if (value == "0")
        {
            return true;
        }

        if (value == "1")
        {
            result = true;
            return true;
        }

        return false;
    }

    private static bool IsValidPoliticalKnowledgeStateKey(
        PoliticalKnowledgeFactKind factKind,
        string stateKey)
    {
        if (string.IsNullOrWhiteSpace(stateKey))
        {
            return false;
        }

        switch (factKind)
        {
            case PoliticalKnowledgeFactKind.Faction:
            case PoliticalKnowledgeFactKind.FactionAffiliation:
                return stateKey == "0" || stateKey == "1";
            case PoliticalKnowledgeFactKind.PoliticalClaim:
                return stateKey.Length > 1
                    && (stateKey[0] == '0' || stateKey[0] == '1')
                    && stateKey[1] == '\u001F';
            case PoliticalKnowledgeFactKind.OfficeVacancy:
                int firstSeparator = stateKey.IndexOf('\u001F');
                int secondSeparator = firstSeparator < 0
                    ? -1
                    : stateKey.IndexOf('\u001F', firstSeparator + 1);
                return firstSeparator > 0
                    && secondSeparator > firstSeparator + 1
                    && TryReadLengthPrefixed(
                        stateKey.Substring(0, firstSeparator),
                        0,
                        out _,
                        out int institutionEnd)
                    && institutionEnd == firstSeparator
                    && (stateKey[firstSeparator + 1] == '0' || stateKey[firstSeparator + 1] == '1')
                    && (stateKey[secondSeparator + 1] == '0' || stateKey[secondSeparator + 1] == '1')
                    && secondSeparator + 2 == stateKey.Length;
            case PoliticalKnowledgeFactKind.PersonDeath:
                int separator = stateKey.IndexOf('\u001F');
                if (separator != 1 || (stateKey[0] != '0' && stateKey[0] != '1'))
                {
                    return false;
                }

                string deathDay = stateKey.Substring(separator + 1);
                return stateKey[0] == '0'
                    ? deathDay.Length == 0
                    : deathDay.Length == 0
                        || long.TryParse(deathDay, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            default:
                return false;
        }
    }

    private static void ValidateDecisionReferences(
        IReadOnlyList<string> references,
        string identity,
        string referenceKind,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string reference in references ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(reference) || seen.Add(reference) == false)
            {
                AddError(issues, "PoliticalDecisionReferenceInvalid", identity, referenceKind + " decision references must be non-empty and unique.");
            }
        }
    }

    private static bool TryBuildCandidateFingerprint(
        IReadOnlyList<string> candidateIds,
        out string fingerprint)
    {
        fingerprint = null;
        if (candidateIds == null)
        {
            return false;
        }

        List<PersonId> people = new List<PersonId>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string candidateId in candidateIds)
        {
            if (string.IsNullOrWhiteSpace(candidateId) || seen.Add(candidateId) == false)
            {
                return false;
            }

            people.Add(new PersonId(candidateId));
        }

        fingerprint = PoliticalDecisionRecord.BuildCandidateFingerprint(people);
        return true;
    }

    private static string[] ToArray(IReadOnlyList<string> values)
    {
        if (values == null)
        {
            return Array.Empty<string>();
        }

        string[] result = new string[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            result[index] = values[index];
        }

        return result;
    }

    private static bool ContainsString(IReadOnlyList<string> values, string value)
    {
        if (values == null)
        {
            return false;
        }

        for (int index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
