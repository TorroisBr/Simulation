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
        ValidateSpatialAuthority(snapshot.Spatial, issues);
        ValidateArmedForces(snapshot, personIds, issues);
        ValidateContingentManpower(snapshot, issues);
        ValidateArmedForcePositions(snapshot, issues);
        ValidatePersistentConflictWarBattle(snapshot, issues);
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
        ValidateCrimeSocialAppraisal(
            snapshot.TheftOutcomes,
            snapshot.CrimeKnowledge,
            snapshot.SocialReactions,
            personIds,
            snapshot.InstitutionIds,
            snapshot.HasInstitutionCatalog,
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

    private static void ValidateSpatialAuthority(
        WorldStateSpatialSnapshot spatial,
        List<WorldStateInvariantIssue> issues)
    {
        if (spatial == null || spatial.AuthorityRevision.HasValue == false)
        {
            return;
        }

        if (spatial.AuthorityRevision.Value < 0L)
        {
            AddError(issues, "SpatialAuthorityNegativeRevision", "world", "Spatial authority revision cannot be negative.");
        }

        HashSet<string> hexIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<HexCoordinate> geographicCoordinates = new HashSet<HexCoordinate>();
        bool geographyPresent = spatial.ScaleContext != null
            || spatial.CoordinateConventionVersion != null
            || spatial.CoordinateCanonicalOrder != null;
        int geographicHexCount = 0;
        foreach (WorldStateHexSnapshot hex in spatial.Hexes ?? new List<WorldStateHexSnapshot>())
        {
            if (hex == null || string.IsNullOrWhiteSpace(hex.HexId))
            {
                AddError(issues, "SpatialHexIdMissing", "hex", "Spatial Hex has no stable identity.");
                continue;
            }

            if (hexIds.Add(hex.HexId) == false)
            {
                AddError(issues, "DuplicateSpatialHexId", hex.HexId, "Spatial Hex identity appears more than once.");
            }

            if (hex.HasGeographicFacts)
            {
                geographyPresent = true;
                geographicHexCount++;
                if (hex.Q.HasValue != hex.R.HasValue)
                {
                    AddError(issues, "SpatialHexCoordinateIncomplete", hex.HexId, "Geographic Hex must provide both axial q and r coordinates.");
                }

                if (!hex.Q.HasValue || !hex.R.HasValue)
                {
                    AddError(issues, "SpatialHexCoordinateMissing", hex.HexId, "Geographic Hex has no complete axial coordinate.");
                }
                else if (!geographicCoordinates.Add(new HexCoordinate(hex.Q.Value, hex.R.Value)))
                {
                    AddError(issues, "DuplicateSpatialHexCoordinate", hex.HexId, "Axial coordinate is assigned to more than one geographic Hex.");
                }

                if (string.IsNullOrWhiteSpace(hex.TerrainDefinitionId))
                {
                    AddError(issues, "SpatialHexTerrainReferenceMissing", hex.HexId, "Geographic Hex has no stable terrain definition reference.");
                }

                if (string.IsNullOrWhiteSpace(hex.AuthoredRevisionToken))
                {
                    AddError(issues, "SpatialHexTerrainRevisionTokenMissing", hex.HexId, "Geographic Hex has no stable authored terrain revision/version token.");
                }
            }
        }

        if (geographyPresent)
        {
            foreach (WorldStateHexSnapshot hex in spatial.Hexes ?? new List<WorldStateHexSnapshot>())
            {
                if (hex != null && !hex.HasGeographicFacts)
                {
                    AddError(issues, "SpatialGeographyHexFactsMissing", hex.HexId, "Every Hex in a geographic context must have axial coordinates, a terrain definition ID, and an authored revision token.");
                }
            }

            if (spatial.ScaleContext == null)
            {
                AddError(issues, "SpatialGeographyScaleMissing", "world", "Finite geography requires exactly one resolved world-local scale context.");
            }
            else
            {
                WorldStateSpatialScaleContextSnapshot scale = spatial.ScaleContext;
                if (string.IsNullOrWhiteSpace(scale.ResolvedConventionId))
                {
                    AddError(issues, "SpatialScaleIdentityMissing", "world", "World-local scale context has no resolved convention identity.");
                }

                if (string.IsNullOrWhiteSpace(scale.SourceIdentity)
                    || string.IsNullOrWhiteSpace(scale.SourceVersion))
                {
                    AddError(issues, "SpatialScaleProvenanceMissing", "world", "World-local scale context has incomplete source identity or version provenance.");
                }

                if (!scale.DistancePerNeighborStep.HasValue || scale.DistancePerNeighborStep.Value <= 0m)
                {
                    AddError(issues, "SpatialScaleDistanceInvalid", "world", "World-local distance per neighbor step must be present and positive.");
                }

                if (string.IsNullOrWhiteSpace(scale.Unit))
                {
                    AddError(issues, "SpatialScaleUnitMissing", "world", "World-local scale context has no authored unit.");
                }
            }

            if (!string.Equals(spatial.CoordinateConventionVersion, HexCoordinate.ConventionVersion, StringComparison.Ordinal))
            {
                AddError(issues, "SpatialCoordinateConventionUnsupported", "world", "Geography coordinate convention version is absent or unsupported.");
            }

            if (!string.Equals(spatial.CoordinateCanonicalOrder, HexCoordinate.CanonicalOrder, StringComparison.Ordinal))
            {
                AddError(issues, "SpatialCoordinateOrderUnsupported", "world", "Geography coordinate canonical order is absent or unsupported.");
            }

            if (geographicHexCount == 0)
            {
                AddError(issues, "SpatialGeographyHexMissing", "world", "Geography scale context exists without any geographic Hexes.");
            }
        }

        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateAnchoredLocationSnapshot location in spatial.AnchoredLocations ?? new List<WorldStateAnchoredLocationSnapshot>())
        {
            if (location == null || string.IsNullOrWhiteSpace(location.LocationId))
            {
                AddError(issues, "SpatialLocationIdMissing", "location", "Spatial Location has no stable identity.");
                continue;
            }

            if (locationIds.Add(location.LocationId) == false)
            {
                AddError(issues, "DuplicateSpatialLocationId", location.LocationId, "Spatial Location identity appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(location.AnchorHexId))
            {
                AddError(issues, "SpatialLocationAnchorMissing", location.LocationId, "Spatial Location has no AnchorHexId.");
            }
            else if (hexIds.Contains(location.AnchorHexId) == false)
            {
                AddError(issues, "SpatialLocationAnchorMissingHex", location.LocationId, "Spatial Location anchor Hex is absent.");
            }
        }
    }

    private static void ValidateArmedForces(
        WorldStateSnapshot snapshot,
        HashSet<string> personIds,
        List<WorldStateInvariantIssue> issues)
    {
        if (snapshot.HasArmedForceState == false)
        {
            return;
        }

        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, WorldStateArmedForceSnapshot> forcesById =
            new Dictionary<string, WorldStateArmedForceSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
        {
            if (force == null)
            {
                AddError(issues, "ArmedForceNull", "armed-force", "Snapshot contains a null ArmedForce entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(force.ArmedForceId) ? "armed-force" : force.ArmedForceId;
            if (string.IsNullOrWhiteSpace(force.ArmedForceId))
            {
                AddError(issues, "ArmedForceIdMissing", identity, "ArmedForce has no stable identity.");
            }
            else if (forceIds.Add(force.ArmedForceId) == false)
            {
                AddError(issues, "DuplicateArmedForceId", identity, "ArmedForceId appears more than once.");
            }
            else
            {
                forcesById.Add(force.ArmedForceId, force);
            }

            if (force.CreatedAbsoluteDay > snapshot.AbsoluteDay)
            {
                AddError(issues, "ArmedForceCreationDayInvalid", identity, "ArmedForce creation day is after the snapshot day.");
            }

            if (Enum.IsDefined(typeof(ArmedForceLifecycleState), force.LifecycleState) == false)
            {
                AddError(issues, "ArmedForceLifecycleInvalid", identity, "ArmedForce lifecycle state is invalid.");
            }

            if (force.LifecycleState == ArmedForceLifecycleState.Active
                && force.TerminatedAbsoluteDay.HasValue)
            {
                AddError(issues, "ActiveArmedForceHasTerminationDay", identity, "An active ArmedForce cannot have a termination day.");
            }

            if (force.LifecycleState == ArmedForceLifecycleState.Terminated)
            {
                if (force.TerminatedAbsoluteDay.HasValue == false)
                {
                    AddError(issues, "TerminatedArmedForceMissingDay", identity, "A terminated ArmedForce requires a termination day.");
                }
                else if (force.TerminatedAbsoluteDay.Value < force.CreatedAbsoluteDay
                    || force.TerminatedAbsoluteDay.Value > snapshot.AbsoluteDay)
                {
                    AddError(issues, "ArmedForceTerminationDayInvalid", identity, "ArmedForce termination day is outside its lifecycle interval.");
                }
            }

            if (force.ParentForceId == force.ArmedForceId)
            {
                AddError(issues, "ArmedForceSelfParent", identity, "An ArmedForce cannot be its own parent.");
            }

            if (force.IsDetached && string.IsNullOrWhiteSpace(force.ParentForceId))
            {
                AddError(issues, "DetachedArmedForceMissingParent", identity, "A detached ArmedForce must remain structurally subordinate.");
            }

            if (string.IsNullOrWhiteSpace(force.CommanderPersonId) == false
                && personIds.Contains(force.CommanderPersonId) == false)
            {
                AddError(issues, "ArmedForceCommanderPersonMissing", identity, "ArmedForce commander is absent from the Person snapshot.");
            }
        }

        foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
        {
            if (force == null || string.IsNullOrWhiteSpace(force.ArmedForceId)) continue;
            string identity = force.ArmedForceId;
            if (string.IsNullOrWhiteSpace(force.ParentForceId) == false)
            {
                if (forcesById.TryGetValue(force.ParentForceId, out WorldStateArmedForceSnapshot parent) == false)
                {
                    AddError(issues, "ArmedForceParentMissing", identity, "ArmedForce parent is absent from the snapshot.");
                }
                else if (force.LifecycleState == ArmedForceLifecycleState.Active
                    && parent.LifecycleState == ArmedForceLifecycleState.Terminated)
                {
                    AddError(issues, "ActiveArmedForceHasTerminatedParent", identity, "An active ArmedForce cannot have a terminated parent.");
                }
            }

            if (ContainsArmedForceParentCycle(force.ArmedForceId, forcesById))
            {
                AddError(issues, "ArmedForceHierarchyCycle", identity, "ArmedForce hierarchy contains a parent cycle.");
            }
        }

        HashSet<string> contingentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceContingentSnapshot contingent in snapshot.ArmedForceContingents)
        {
            if (contingent == null)
            {
                AddError(issues, "ArmedForceContingentNull", "contingent", "Snapshot contains a null ArmedForce contingent entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(contingent.ContingentId) ? "contingent" : contingent.ContingentId;
            if (string.IsNullOrWhiteSpace(contingent.ContingentId))
            {
                AddError(issues, "ArmedForceContingentIdMissing", identity, "Contingent has no stable identity.");
            }
            else if (contingentIds.Add(contingent.ContingentId) == false)
            {
                AddError(issues, "DuplicateArmedForceContingentId", identity, "ContingentId appears more than once.");
            }

            if (contingent.Amount < 0L)
            {
                AddError(issues, "NegativeArmedForceContingentAmount", identity, "Contingent amount cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(contingent.ForceId)
                || forcesById.ContainsKey(contingent.ForceId) == false)
            {
                AddError(issues, "ArmedForceContingentForceMissing", identity, "Contingent force is absent from the snapshot.");
            }
        }

        HashSet<string> referenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateArmedForcePersonReferenceSnapshot reference in snapshot.ArmedForceRelevantPersons)
        {
            if (reference == null)
            {
                AddError(issues, "ArmedForcePersonReferenceNull", "reference", "Snapshot contains a null ArmedForce Person reference.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(reference.ReferenceId) ? "reference" : reference.ReferenceId;
            if (string.IsNullOrWhiteSpace(reference.ReferenceId))
            {
                AddError(issues, "ArmedForcePersonReferenceIdMissing", identity, "Relevant Person reference has no stable identity.");
            }
            else if (referenceIds.Add(reference.ReferenceId) == false)
            {
                AddError(issues, "DuplicateArmedForcePersonReferenceId", identity, "Relevant Person reference identity appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(reference.ForceId)
                || forcesById.ContainsKey(reference.ForceId) == false)
            {
                AddError(issues, "ArmedForcePersonReferenceForceMissing", identity, "Relevant Person reference force is absent from the snapshot.");
            }

            if (string.IsNullOrWhiteSpace(reference.PersonId)
                || personIds.Contains(reference.PersonId) == false)
            {
                AddError(issues, "ArmedForcePersonReferencePersonMissing", identity, "Relevant Person reference PersonId is absent from the snapshot.");
            }
        }
    }

    private static void ValidateContingentManpower(
        WorldStateSnapshot snapshot,
        List<WorldStateInvariantIssue> issues)
    {
        if (!snapshot.HasContingentManpowerState) return;
        Dictionary<string, WorldStateArmedForceContingentSnapshot> contingents =
            new Dictionary<string, WorldStateArmedForceContingentSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceContingentSnapshot contingent in snapshot.ArmedForceContingents)
        {
            if (contingent?.ContingentId != null && !contingents.ContainsKey(contingent.ContingentId))
                contingents.Add(contingent.ContingentId, contingent);
        }
        Dictionary<string, WorldStateArmedForceSnapshot> forces =
            new Dictionary<string, WorldStateArmedForceSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
            if (force?.ArmedForceId != null && !forces.ContainsKey(force.ArmedForceId))
                forces.Add(force.ArmedForceId, force);

        HashSet<string> stateIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, long> sourceTotals = new Dictionary<string, long>(StringComparer.Ordinal);
        Dictionary<string, WorldStateContingentManpowerSnapshot> sourceSnapshots =
            new Dictionary<string, WorldStateContingentManpowerSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateContingentManpowerSnapshot state in snapshot.ContingentManpowerStates)
        {
            if (state == null)
            {
                AddError(issues, "ContingentManpowerNull", "manpower", "Snapshot contains a null contingent manpower state.");
                continue;
            }
            string id = string.IsNullOrWhiteSpace(state.ContingentId) ? "contingent" : state.ContingentId;
            if (string.IsNullOrWhiteSpace(state.ContingentId))
                AddError(issues, "ContingentManpowerIdMissing", id, "Manpower state has no contingent identity.");
            else if (!stateIds.Add(state.ContingentId))
                AddError(issues, "DuplicateContingentManpowerId", id, "Contingent manpower state appears more than once.");

            if (!contingents.TryGetValue(state.ContingentId ?? string.Empty, out WorldStateArmedForceContingentSnapshot contingent))
                AddError(issues, "ContingentManpowerContingentMissing", id, "Manpower state references a missing contingent.");
            else if (contingent.Amount != state.LivingRosterAmount)
                AddError(issues, "ContingentManpowerAmountMirrorMismatch", id, "Contingent Amount differs from the derived living roster.");

            if (!state.SourceResolved)
                AddError(issues, "ContingentManpowerSourceUnresolved", id, "Bound manpower source is unavailable to diagnostics.");
            if (state.SourceId != null && !state.SourceCapacity.HasValue)
                AddError(issues, "ContingentManpowerSourceCapacityMissing", id, "Resolved source has no capacity snapshot.");

            HashSet<string> cohortKeys = new HashSet<string>(StringComparer.Ordinal);
            long living = 0L;
            long available = 0L;
            foreach (WorldStateManpowerCohortSnapshot cohort in state.Cohorts)
            {
                if (cohort == null)
                {
                    AddError(issues, "ContingentManpowerCohortNull", id, "Manpower state contains a null cohort.");
                    continue;
                }
                string key = ((int)cohort.InjuryState).ToString(CultureInfo.InvariantCulture) + ":"
                    + ((int)cohort.CustodyState).ToString(CultureInfo.InvariantCulture) + ":"
                    + (cohort.CustodianForceId ?? string.Empty) + ":"
                    + ((int)cohort.AvailabilityState).ToString(CultureInfo.InvariantCulture);
                if (!cohortKeys.Add(key))
                    AddError(issues, "DuplicateContingentManpowerCohort", id, "Manpower state contains a duplicate canonical cohort key.");
                if (cohort.Amount <= 0L)
                    AddError(issues, "ContingentManpowerCohortAmountInvalid", id, "Manpower cohort amount must be positive.");
                if (!Enum.IsDefined(typeof(ManpowerInjuryState), cohort.InjuryState)
                    || !Enum.IsDefined(typeof(ManpowerCustodyState), cohort.CustodyState)
                    || !Enum.IsDefined(typeof(ManpowerAvailabilityState), cohort.AvailabilityState))
                    AddError(issues, "ContingentManpowerCohortStateInvalid", id, "Manpower cohort contains an invalid status value.");
                if (cohort.CustodyState == ManpowerCustodyState.Captured)
                {
                    if (cohort.AvailabilityState == ManpowerAvailabilityState.Available)
                        AddError(issues, "CapturedManpowerAvailable", id, "Captured manpower cannot be available to its original contingent.");
                    if (string.IsNullOrWhiteSpace(cohort.CustodianForceId)
                        || !forces.TryGetValue(cohort.CustodianForceId, out WorldStateArmedForceSnapshot custodian)
                        || custodian.LifecycleState != ArmedForceLifecycleState.Active)
                        AddError(issues, "CapturedManpowerCustodianMissing", id, "Captured manpower requires an existing active ArmedForce custodian.");
                }
                else if (!string.IsNullOrWhiteSpace(cohort.CustodianForceId))
                    AddError(issues, "FreeManpowerHasCustodian", id, "Free manpower cannot carry a custodian force.");
                try
                {
                    living = checked(living + cohort.Amount);
                    if (cohort.AvailabilityState == ManpowerAvailabilityState.Available)
                        available = checked(available + cohort.Amount);
                }
                catch (OverflowException)
                {
                    AddError(issues, "ContingentManpowerRosterOverflow", id, "Manpower cohort total exceeds Int64 capacity.");
                    break;
                }
            }
            if (living != state.LivingRosterAmount)
                AddError(issues, "ContingentManpowerLivingTotalMismatch", id, "Cohort sum differs from the stored living roster projection.");
            if (available != state.AvailableAmount)
                AddError(issues, "ContingentManpowerAvailableTotalMismatch", id, "Available cohort sum differs from the stored availability projection.");

            if (state.SourceId != null)
            {
                try
                {
                    sourceTotals[state.SourceId] = checked(sourceTotals.TryGetValue(state.SourceId, out long old)
                        ? old + state.LivingRosterAmount
                        : state.LivingRosterAmount);
                    if (!sourceSnapshots.ContainsKey(state.SourceId)) sourceSnapshots.Add(state.SourceId, state);
                }
                catch (OverflowException)
                {
                    AddError(issues, "ManpowerSourceAllocationOverflow", state.SourceId, "Allocated roster for source exceeds Int64 capacity.");
                }
            }
        }

        foreach (WorldStateArmedForceContingentSnapshot contingent in snapshot.ArmedForceContingents)
            if (contingent != null && !stateIds.Contains(contingent.ContingentId ?? string.Empty))
                AddError(issues, "ContingentManpowerStateMissing", contingent.ContingentId ?? "contingent", "Contingent has no manpower state.");

        foreach (KeyValuePair<string, long> total in sourceTotals)
        {
            if (!sourceSnapshots.TryGetValue(total.Key, out WorldStateContingentManpowerSnapshot source)) continue;
            if (source.SourceCapacity.HasValue && total.Value > source.SourceCapacity.Value)
                AddError(issues, "ManpowerSourceCapacityExceeded", total.Key, "Bound roster exceeds current source capacity.");
            if (source.SourceFactualLivingAmount.HasValue && total.Value > source.SourceFactualLivingAmount.Value)
                AddError(issues, "ManpowerSourceFactualAmountExceeded", total.Key, "Bound roster exceeds current factual living amount.");
        }

        foreach (WorldStateContingentManpowerSnapshot state in snapshot.ContingentManpowerStates)
        {
            if (state == null || !contingents.TryGetValue(state.ContingentId ?? string.Empty, out WorldStateArmedForceContingentSnapshot contingent)) continue;
            if (!forces.TryGetValue(contingent.ForceId ?? string.Empty, out WorldStateArmedForceSnapshot force)) continue;
            if (force.LifecycleState == ArmedForceLifecycleState.Terminated && state.LivingRosterAmount > 0L)
                AddError(issues, "TerminatedForceHasLivingManpower", force.ArmedForceId, "Terminated ArmedForce retains direct living roster.");
            foreach (WorldStateManpowerCohortSnapshot cohort in state.Cohorts)
                if (force.LifecycleState == ArmedForceLifecycleState.Terminated
                    && cohort?.CustodyState == ManpowerCustodyState.Captured
                    && cohort.CustodianForceId == force.ArmedForceId
                    && cohort.Amount > 0L)
                    AddError(issues, "TerminatedForceCustodiesManpower", force.ArmedForceId, "Terminated ArmedForce remains custodian of captured manpower.");
        }
    }

    private static bool ContainsArmedForceParentCycle(
        string forceId,
        Dictionary<string, WorldStateArmedForceSnapshot> forcesById)
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        string currentId = forceId;
        while (string.IsNullOrWhiteSpace(currentId) == false)
        {
            if (visited.Add(currentId) == false) return true;
            if (forcesById.TryGetValue(currentId, out WorldStateArmedForceSnapshot current) == false)
            {
                return false;
            }

            currentId = current.ParentForceId;
        }

        return false;
    }

    private static void ValidateArmedForcePositions(
        WorldStateSnapshot snapshot,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces ?? new List<WorldStateArmedForceSnapshot>())
        {
            if (force != null && string.IsNullOrWhiteSpace(force.ArmedForceId) == false)
            {
                forceIds.Add(force.ArmedForceId);
            }
        }

        HashSet<string> positionForceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateArmedForcePositionSnapshot position in snapshot.ArmedForcePositions ?? new List<WorldStateArmedForcePositionSnapshot>())
        {
            if (position == null)
            {
                AddError(issues, "ArmedForcePositionNull", "armed-force-position", "Snapshot contains a null ArmedForce position entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(position.ArmedForceId)
                ? "armed-force-position"
                : position.ArmedForceId;
            if (string.IsNullOrWhiteSpace(position.ArmedForceId))
            {
                AddError(issues, "ArmedForcePositionForceIdMissing", identity, "ArmedForce position has no stable force identity.");
            }
            else if (positionForceIds.Add(position.ArmedForceId) == false)
            {
                AddError(issues, "DuplicateArmedForcePosition", identity, "An ArmedForce has more than one current spatial position.");
            }
            else if (forceIds.Contains(position.ArmedForceId) == false)
            {
                AddError(issues, "ArmedForcePositionForceMissing", identity, "ArmedForce position references a force absent from the snapshot.");
            }

            if (position.CurrentPosition == null)
            {
                AddError(issues, "ArmedForcePositionMissingReference", identity, "ArmedForce position has no typed SpatialReference.");
                continue;
            }

            if (!string.Equals(position.CurrentPositionStableKey, position.CurrentPosition.StableKey, StringComparison.Ordinal))
            {
                AddError(issues, "ArmedForcePositionMalformed", identity, "ArmedForce position key does not match its typed SpatialReference.");
                continue;
            }

            ValidateSpatialReferencePresence(
                snapshot,
                position.CurrentPosition,
                identity,
                "ArmedForcePosition",
                issues);
        }
    }

    private static void ValidateSpatialReferencePresence(
        WorldStateSnapshot snapshot,
        SpatialReference reference,
        string identity,
        string diagnosticPrefix,
        List<WorldStateInvariantIssue> issues)
    {
        WorldStateSpatialSnapshot spatial = snapshot.Spatial ?? new WorldStateSpatialSnapshot();
        if (Enum.IsDefined(typeof(SpatialReferenceKind), reference.Kind) == false)
        {
            AddError(issues, diagnosticPrefix + "Malformed", identity, "SpatialReference kind is invalid.");
            return;
        }

        if (reference.Kind == SpatialReferenceKind.Hex)
        {
            if (reference.HexId == null || ContainsHex(spatial.Hexes, reference.HexId.Value) == false)
            {
                AddError(issues, diagnosticPrefix + "HexMissing", identity, "SpatialReference Hex is absent from the spatial authority snapshot.");
            }
            return;
        }

        if (reference.Kind == SpatialReferenceKind.Location)
        {
            if (reference.LocationId == null
                || TryGetAnchoredLocation(spatial.AnchoredLocations, reference.LocationId.Value, out WorldStateAnchoredLocationSnapshot location) == false)
            {
                AddError(issues, diagnosticPrefix + "LocationMissing", identity, "SpatialReference Location is absent from the spatial authority snapshot.");
                return;
            }

            if (string.IsNullOrWhiteSpace(location.AnchorHexId)
                || ContainsHex(spatial.Hexes, location.AnchorHexId) == false)
            {
                AddError(issues, diagnosticPrefix + "LocationAnchorMissing", identity, "SpatialReference Location does not resolve to a registered anchor Hex.");
            }
            return;
        }

        if (!reference.TopologyOwnerKind.HasValue
            || string.IsNullOrWhiteSpace(reference.TopologyOwnerRuntimeId)
            || string.IsNullOrWhiteSpace(reference.SubLocationRuntimeId))
        {
            AddError(issues, diagnosticPrefix + "Malformed", identity, "SubLocation SpatialReference is incomplete.");
            return;
        }

        WorldStateSpatialTopologyBindingSnapshot topologyBinding = null;
        foreach (WorldStateSpatialTopologyBindingSnapshot candidate in spatial.TopologyBindings ?? new List<WorldStateSpatialTopologyBindingSnapshot>())
        {
            if (candidate != null
                && candidate.OwnerKind == reference.TopologyOwnerKind.Value
                && string.Equals(candidate.OwnerRuntimeId, reference.TopologyOwnerRuntimeId, StringComparison.Ordinal))
            {
                topologyBinding = candidate;
                break;
            }
        }

        if (topologyBinding == null)
        {
            AddError(issues, diagnosticPrefix + "BindingMissing", identity, "SubLocation owner has no spatial topology binding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(topologyBinding.LocationId)
            || TryGetAnchoredLocation(spatial.AnchoredLocations, topologyBinding.LocationId, out WorldStateAnchoredLocationSnapshot boundLocation) == false
            || string.IsNullOrWhiteSpace(boundLocation.AnchorHexId)
            || ContainsHex(spatial.Hexes, boundLocation.AnchorHexId) == false)
        {
            AddError(issues, diagnosticPrefix + "BindingInvalid", identity, "SubLocation binding does not resolve to a valid Location and anchor Hex.");
            return;
        }

        bool topologyFound = false;
        bool placeFound = false;
        foreach (WorldStateLocalTopologySnapshot topology in snapshot.LocalTopologies ?? new List<WorldStateLocalTopologySnapshot>())
        {
            if (topology == null
                || topology.OwnerKind != reference.TopologyOwnerKind.Value
                || !string.Equals(topology.OwnerRuntimeId, reference.TopologyOwnerRuntimeId, StringComparison.Ordinal))
            {
                continue;
            }

            topologyFound = true;
            foreach (WorldStateLocalPlaceSnapshot place in topology.Places ?? new List<WorldStateLocalPlaceSnapshot>())
            {
                if (place != null && string.Equals(place.RuntimeId, reference.SubLocationRuntimeId, StringComparison.Ordinal))
                {
                    placeFound = true;
                    break;
                }
            }
            break;
        }

        if (!topologyFound)
        {
            AddError(issues, diagnosticPrefix + "TopologyMissing", identity, "SubLocation topology is absent from the snapshot.");
        }
        else if (!placeFound)
        {
            AddError(issues, diagnosticPrefix + "SubLocationMissing", identity, "SubLocation is absent from its owning topology.");
        }
    }

    private static void ValidatePersistentConflictWarBattle(
        WorldStateSnapshot snapshot,
        List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces ?? new List<WorldStateArmedForceSnapshot>())
        {
            if (force != null && string.IsNullOrWhiteSpace(force.ArmedForceId) == false) forceIds.Add(force.ArmedForceId);
        }

        Dictionary<string, WorldStateConflictSnapshot> conflictsById = new Dictionary<string, WorldStateConflictSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateConflictSnapshot conflict in snapshot.Conflicts ?? new List<WorldStateConflictSnapshot>())
        {
            if (conflict == null)
            {
                AddError(issues, "ConflictNull", "conflict", "Snapshot contains a null Conflict entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(conflict.ConflictId) ? "conflict" : conflict.ConflictId;
            if (string.IsNullOrWhiteSpace(conflict.ConflictId)) AddError(issues, "ConflictIdMissing", identity, "Conflict has no stable identity.");
            else if (conflictsById.ContainsKey(conflict.ConflictId)) AddError(issues, "DuplicateConflictId", identity, "ConflictId appears more than once.");
            else conflictsById.Add(conflict.ConflictId, conflict);
            ValidateLifecycleDay(conflict.CreatedAbsoluteDay, conflict.EndedAbsoluteDay, conflict.LifecycleState, snapshot.AbsoluteDay, identity, "Conflict", issues);
        }

        HashSet<string> conflictSideKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateConflictSideSnapshot side in snapshot.ConflictSides ?? new List<WorldStateConflictSideSnapshot>())
        {
            if (side == null) { AddError(issues, "ConflictSideNull", "conflict-side", "Snapshot contains a null Conflict side entry."); continue; }
            string identity = (side.ConflictId ?? "conflict") + "/" + (side.SideId ?? "side");
            if (string.IsNullOrWhiteSpace(side.ConflictId) || conflictsById.ContainsKey(side.ConflictId) == false) AddError(issues, "ConflictSideParentMissing", identity, "Conflict side parent is absent from the snapshot.");
            if (string.IsNullOrWhiteSpace(side.SideId)) AddError(issues, "ConflictSideIdMissing", identity, "Conflict side has no stable identity.");
            else if (conflictSideKeys.Add(identity) == false) AddError(issues, "DuplicateConflictSideId", identity, "Conflict side identity appears more than once.");
        }

        ValidateConflictBindings(snapshot.ConflictParticipantBindings, conflictsById, forceIds, conflictSideKeys, issues);

        Dictionary<string, WorldStateWarSnapshot> warsById = new Dictionary<string, WorldStateWarSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateWarSnapshot war in snapshot.Wars ?? new List<WorldStateWarSnapshot>())
        {
            if (war == null) { AddError(issues, "WarNull", "war", "Snapshot contains a null War entry."); continue; }
            string identity = string.IsNullOrWhiteSpace(war.WarId) ? "war" : war.WarId;
            if (string.IsNullOrWhiteSpace(war.WarId)) AddError(issues, "WarIdMissing", identity, "War has no stable identity.");
            else if (warsById.ContainsKey(war.WarId)) AddError(issues, "DuplicateWarId", identity, "WarId appears more than once.");
            else warsById.Add(war.WarId, war);
            ValidateLifecycleDay(war.CreatedAbsoluteDay, war.EndedAbsoluteDay, war.LifecycleState, snapshot.AbsoluteDay, identity, "War", issues);
            if (string.IsNullOrWhiteSpace(war.ConflictId) == false && conflictsById.ContainsKey(war.ConflictId) == false) AddError(issues, "WarConflictMissing", identity, "War Conflict reference is absent from the snapshot.");
        }

        HashSet<string> warSideKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateWarSideSnapshot side in snapshot.WarSides ?? new List<WorldStateWarSideSnapshot>())
        {
            if (side == null) { AddError(issues, "WarSideNull", "war-side", "Snapshot contains a null War side entry."); continue; }
            string identity = (side.WarId ?? "war") + "/" + (side.SideId ?? "side");
            if (string.IsNullOrWhiteSpace(side.WarId) || warsById.ContainsKey(side.WarId) == false) AddError(issues, "WarSideParentMissing", identity, "War side parent is absent from the snapshot.");
            if (string.IsNullOrWhiteSpace(side.SideId)) AddError(issues, "WarSideIdMissing", identity, "War side has no stable identity.");
            else if (warSideKeys.Add(identity) == false) AddError(issues, "DuplicateWarSideId", identity, "War side identity appears more than once.");
        }
        ValidateWarBindings(snapshot.WarParticipantBindings, warsById, forceIds, warSideKeys, issues);

        Dictionary<string, WorldStateBattleSnapshot> battlesById = new Dictionary<string, WorldStateBattleSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateBattleSnapshot battle in snapshot.Battles ?? new List<WorldStateBattleSnapshot>())
        {
            if (battle == null) { AddError(issues, "BattleNull", "battle", "Snapshot contains a null Battle entry."); continue; }
            string identity = string.IsNullOrWhiteSpace(battle.BattleId) ? "battle" : battle.BattleId;
            if (string.IsNullOrWhiteSpace(battle.BattleId)) AddError(issues, "BattleIdMissing", identity, "Battle has no stable identity.");
            else if (battlesById.ContainsKey(battle.BattleId)) AddError(issues, "DuplicateBattleId", identity, "BattleId appears more than once.");
            else battlesById.Add(battle.BattleId, battle);
            if (Enum.IsDefined(typeof(BattleLifecycleState), battle.LifecycleState) == false) AddError(issues, "BattleLifecycleInvalid", identity, "Battle lifecycle state is invalid.");
            if (battle.CreatedAbsoluteDay > snapshot.AbsoluteDay) AddError(issues, "BattleCreationDayInvalid", identity, "Battle creation day is after the snapshot day.");
            if (battle.LifecycleState == BattleLifecycleState.Pending && battle.StartedAbsoluteDay.HasValue) AddError(issues, "PendingBattleHasStartDay", identity, "A pending Battle cannot have a start day.");
            if ((battle.LifecycleState == BattleLifecycleState.Active || battle.LifecycleState == BattleLifecycleState.Resolved) && !battle.StartedAbsoluteDay.HasValue) AddError(issues, "BattleStartDayMissing", identity, "An active or resolved Battle requires a start day.");
            if (battle.LifecycleState != BattleLifecycleState.Resolved && battle.TerminalOutcome != null) AddError(issues, "NonResolvedBattleHasOutcome", identity, "Only a Resolved Battle may carry a terminal outcome.");
            if (battle.LifecycleState == BattleLifecycleState.Resolved && battle.TerminalOutcome == null) AddError(issues, "ResolvedBattleOutcomeMissing", identity, "A Resolved Battle requires exactly one terminal outcome.");
            if (battle.StartedAbsoluteDay.HasValue && (battle.StartedAbsoluteDay.Value < battle.CreatedAbsoluteDay || battle.StartedAbsoluteDay.Value > snapshot.AbsoluteDay)) AddError(issues, "BattleStartDayInvalid", identity, "Battle start day is outside its lifecycle interval.");
            if ((battle.LifecycleState == BattleLifecycleState.Active || battle.LifecycleState == BattleLifecycleState.Resolved) && string.IsNullOrWhiteSpace(battle.LocationReferenceKey)) AddError(issues, "ActiveBattleLocationMissing", identity, "An active or resolved Battle requires a physical SpatialReference.");
            if (battle.TerminalOutcome != null)
            {
                WorldStateBattleTerminalOutcomeSnapshot outcome = battle.TerminalOutcome;
                if (!string.Equals(outcome.BattleId, battle.BattleId, StringComparison.Ordinal)) AddError(issues, "BattleOutcomeIdentityMismatch", identity, "Terminal outcome BattleId does not match its owning Battle.");
                if (!Enum.IsDefined(typeof(BattleOutcomeType), outcome.OutcomeType)) AddError(issues, "BattleOutcomeTypeInvalid", identity, "Terminal outcome type is invalid.");
                if (outcome.ResolvedAbsoluteDay < 0L
                    || (battle.StartedAbsoluteDay.HasValue && outcome.ResolvedAbsoluteDay < battle.StartedAbsoluteDay.Value)
                    || outcome.ResolvedAbsoluteDay > snapshot.AbsoluteDay) AddError(issues, "BattleOutcomeDayInvalid", identity, "Terminal outcome day is outside the started Battle interval or after the snapshot day.");
                if (outcome.OutcomeType == BattleOutcomeType.Victory)
                {
                    if (string.IsNullOrWhiteSpace(outcome.WinningBattleSideId)) AddError(issues, "BattleWinnerMissing", identity, "A victory outcome requires a winning BattleSideId.");
                    else if (!HasBattleSide(snapshot.BattleSides, battle.BattleId, outcome.WinningBattleSideId)) AddError(issues, "BattleWinnerSideMissing", identity, "Winning BattleSideId is not registered on the Battle.");
                }
                else if (outcome.OutcomeType == BattleOutcomeType.Draw && outcome.WinningBattleSideId != null)
                {
                    AddError(issues, "BattleDrawHasWinner", identity, "A draw outcome cannot have a winning BattleSideId.");
                }
                if (string.IsNullOrWhiteSpace(outcome.D5PolicyFingerprint)
                    || string.IsNullOrWhiteSpace(outcome.D5NumericExecutionProfileKey)
                    || string.IsNullOrWhiteSpace(outcome.D5ProjectionVersion)
                    || string.IsNullOrWhiteSpace(outcome.D5CausalResolutionFingerprint)
                    || string.IsNullOrWhiteSpace(outcome.D5SourceContextFingerprint)
                    || string.IsNullOrWhiteSpace(outcome.D5CapabilityRuleKey)
                    || string.IsNullOrWhiteSpace(outcome.D5RandomAuthorityRuleKey)
                    || string.IsNullOrWhiteSpace(outcome.D5ResolverSettingsIdentity)
                    || string.IsNullOrWhiteSpace(outcome.D6B2PolicyFingerprint)
                    || string.IsNullOrWhiteSpace(outcome.D6B2PlanSchemaVersion)
                    || string.IsNullOrWhiteSpace(outcome.D6B2CoverageVersion)
                    || string.IsNullOrWhiteSpace(outcome.D6B2PlanFingerprint)) AddError(issues, "BattleOutcomeProvenanceInvalid", identity, "Terminal outcome requires complete stable D5/D6B2 provenance.");
            }
            ValidateBattleLocation(snapshot, battle, identity, issues);
            if (string.IsNullOrWhiteSpace(battle.ConflictId) == false && conflictsById.ContainsKey(battle.ConflictId) == false) AddError(issues, "BattleConflictMissing", identity, "Battle Conflict reference is absent from the snapshot.");
            if (string.IsNullOrWhiteSpace(battle.WarId) == false)
            {
                if (warsById.TryGetValue(battle.WarId, out WorldStateWarSnapshot battleWar) == false)
                {
                    AddError(issues, "BattleWarMissing", identity, "Battle War reference is absent from the snapshot.");
                }
                else if (battleWar != null
                    && string.IsNullOrWhiteSpace(battle.ConflictId) == false
                    && string.IsNullOrWhiteSpace(battleWar.ConflictId) == false
                    && battleWar.ConflictId != battle.ConflictId)
                {
                    AddError(issues, "BattleReferenceContradiction", identity, "Battle Conflict and War references contradict one another.");
                }
            }
        }

        HashSet<string> battleSideKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateBattleSideSnapshot side in snapshot.BattleSides ?? new List<WorldStateBattleSideSnapshot>())
        {
            if (side == null) { AddError(issues, "BattleSideNull", "battle-side", "Snapshot contains a null Battle side entry."); continue; }
            string identity = (side.BattleId ?? "battle") + "/" + (side.SideId ?? "side");
            if (string.IsNullOrWhiteSpace(side.BattleId) || battlesById.ContainsKey(side.BattleId) == false) AddError(issues, "BattleSideParentMissing", identity, "Battle side parent is absent from the snapshot.");
            if (string.IsNullOrWhiteSpace(side.SideId)) AddError(issues, "BattleSideIdMissing", identity, "Battle side has no stable identity.");
            else if (battleSideKeys.Add(identity) == false) AddError(issues, "DuplicateBattleSideId", identity, "Battle side identity appears more than once.");
        }
        ValidateBattleBindings(snapshot.BattleParticipantBindings, battlesById, forceIds, battleSideKeys, issues);
    }

    private static void ValidateBattleLocation(
        WorldStateSnapshot snapshot,
        WorldStateBattleSnapshot battle,
        string identity,
        List<WorldStateInvariantIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(battle.LocationReferenceKey))
        {
            return;
        }

        if (battle.LocationReference == null)
        {
            AddError(issues, "BattleLocationMalformed", identity, "Battle location has a key but no typed SpatialReference.");
            return;
        }

        if (!string.Equals(battle.LocationReferenceKey, battle.LocationReference.StableKey, System.StringComparison.Ordinal))
        {
            AddError(issues, "BattleLocationMalformed", identity, "Battle location key does not match its typed SpatialReference.");
            return;
        }

        WorldStateSpatialSnapshot spatial = snapshot.Spatial ?? new WorldStateSpatialSnapshot();
        SpatialReference reference = battle.LocationReference;
        if (System.Enum.IsDefined(typeof(SpatialReferenceKind), reference.Kind) == false)
        {
            AddError(issues, "BattleLocationMalformed", identity, "Battle location kind is invalid.");
            return;
        }

        if (reference.Kind == SpatialReferenceKind.Hex)
        {
            if (reference.HexId == null || ContainsHex(spatial.Hexes, reference.HexId.Value) == false)
            {
                AddError(issues, "BattleLocationHexMissing", identity, "Battle location Hex is absent from the spatial authority snapshot.");
            }
            return;
        }

        if (reference.Kind == SpatialReferenceKind.Location)
        {
            if (reference.LocationId == null || TryGetAnchoredLocation(spatial.AnchoredLocations, reference.LocationId.Value, out WorldStateAnchoredLocationSnapshot location) == false)
            {
                AddError(issues, "BattleLocationMissing", identity, "Battle location is absent from the spatial authority snapshot.");
                return;
            }

            if (string.IsNullOrWhiteSpace(location.AnchorHexId) || ContainsHex(spatial.Hexes, location.AnchorHexId) == false)
            {
                AddError(issues, "BattleLocationAnchorMissing", identity, "Battle location does not resolve to a registered anchor Hex.");
            }
            return;
        }

        if (!reference.TopologyOwnerKind.HasValue
            || string.IsNullOrWhiteSpace(reference.TopologyOwnerRuntimeId)
            || string.IsNullOrWhiteSpace(reference.SubLocationRuntimeId))
        {
            AddError(issues, "BattleLocationMalformed", identity, "Battle SubLocation reference is incomplete.");
            return;
        }

        WorldStateSpatialTopologyBindingSnapshot topologyBinding = null;
        foreach (WorldStateSpatialTopologyBindingSnapshot candidate in spatial.TopologyBindings ?? new List<WorldStateSpatialTopologyBindingSnapshot>())
        {
            if (candidate != null
                && candidate.OwnerKind == reference.TopologyOwnerKind.Value
                && string.Equals(candidate.OwnerRuntimeId, reference.TopologyOwnerRuntimeId, System.StringComparison.Ordinal))
            {
                topologyBinding = candidate;
                break;
            }
        }

        if (topologyBinding == null)
        {
            AddError(issues, "BattleSubLocationBindingMissing", identity, "Battle SubLocation owner has no spatial topology binding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(topologyBinding.LocationId)
            || TryGetAnchoredLocation(spatial.AnchoredLocations, topologyBinding.LocationId, out WorldStateAnchoredLocationSnapshot boundLocation) == false
            || string.IsNullOrWhiteSpace(boundLocation.AnchorHexId)
            || ContainsHex(spatial.Hexes, boundLocation.AnchorHexId) == false)
        {
            AddError(issues, "BattleSubLocationBindingInvalid", identity, "Battle SubLocation binding does not resolve to a valid Location and anchor Hex.");
            return;
        }

        bool topologyFound = false;
        bool placeFound = false;
        foreach (WorldStateLocalTopologySnapshot topology in snapshot.LocalTopologies ?? new List<WorldStateLocalTopologySnapshot>())
        {
            if (topology == null
                || topology.OwnerKind != reference.TopologyOwnerKind.Value
                || !string.Equals(topology.OwnerRuntimeId, reference.TopologyOwnerRuntimeId, System.StringComparison.Ordinal))
            {
                continue;
            }

            topologyFound = true;
            foreach (WorldStateLocalPlaceSnapshot place in topology.Places ?? new List<WorldStateLocalPlaceSnapshot>())
            {
                if (place != null && string.Equals(place.RuntimeId, reference.SubLocationRuntimeId, System.StringComparison.Ordinal))
                {
                    placeFound = true;
                    break;
                }
            }
            break;
        }

        if (!topologyFound) AddError(issues, "BattleSubLocationTopologyMissing", identity, "Battle SubLocation topology is absent from the snapshot.");
        else if (!placeFound) AddError(issues, "BattleSubLocationMissing", identity, "Battle SubLocation is absent from its owning topology.");
    }

    private static bool HasBattleSide(
        IReadOnlyList<WorldStateBattleSideSnapshot> sides,
        string battleId,
        string sideId)
    {
        if (sides == null) return false;
        foreach (WorldStateBattleSideSnapshot side in sides)
            if (side != null
                && string.Equals(side.BattleId, battleId, StringComparison.Ordinal)
                && string.Equals(side.SideId, sideId, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool ContainsHex(IReadOnlyList<WorldStateHexSnapshot> hexes, string hexId)
    {
        foreach (WorldStateHexSnapshot hex in hexes ?? new List<WorldStateHexSnapshot>())
        {
            if (hex != null && string.Equals(hex.HexId, hexId, System.StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static bool TryGetAnchoredLocation(
        IReadOnlyList<WorldStateAnchoredLocationSnapshot> locations,
        string locationId,
        out WorldStateAnchoredLocationSnapshot result)
    {
        foreach (WorldStateAnchoredLocationSnapshot location in locations ?? new List<WorldStateAnchoredLocationSnapshot>())
        {
            if (location != null && string.Equals(location.LocationId, locationId, System.StringComparison.Ordinal))
            {
                result = location;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static void ValidateLifecycleDay(
        long createdAbsoluteDay,
        long? endedAbsoluteDay,
        Enum lifecycleState,
        long snapshotAbsoluteDay,
        string identity,
        string domain,
        List<WorldStateInvariantIssue> issues)
    {
        if (createdAbsoluteDay > snapshotAbsoluteDay) AddError(issues, domain + "CreationDayInvalid", identity, domain + " creation day is after the snapshot day.");
        if (Enum.IsDefined(lifecycleState.GetType(), lifecycleState) == false) AddError(issues, domain + "LifecycleInvalid", identity, domain + " lifecycle state is invalid.");
        bool active = string.Equals(lifecycleState.ToString(), "Active", StringComparison.Ordinal);
        if (active && endedAbsoluteDay.HasValue) AddError(issues, "Active" + domain + "HasEndDay", identity, "An active " + domain + " cannot have an end day.");
        if (!active && Enum.IsDefined(lifecycleState.GetType(), lifecycleState) && (!endedAbsoluteDay.HasValue || endedAbsoluteDay.Value < createdAbsoluteDay || endedAbsoluteDay.Value > snapshotAbsoluteDay)) AddError(issues, domain + "EndDayInvalid", identity, domain + " end day is outside its lifecycle interval.");
    }

    private static void ValidateConflictBindings(IReadOnlyList<WorldStateConflictParticipantBindingSnapshot> bindings, Dictionary<string, WorldStateConflictSnapshot> parents, HashSet<string> forceIds, HashSet<string> sideKeys, List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateConflictParticipantBindingSnapshot binding in bindings ?? new List<WorldStateConflictParticipantBindingSnapshot>())
        {
            if (binding == null) { AddError(issues, "ConflictBindingNull", "conflict-binding", "Snapshot contains a null Conflict participant binding."); continue; }
            string identity = (binding.ConflictId ?? "conflict") + "/" + (binding.BindingId ?? "binding");
            if (string.IsNullOrWhiteSpace(binding.BindingId) || ids.Add(identity) == false) AddError(issues, "DuplicateConflictBindingId", identity, "Conflict participant binding identity is missing or duplicated.");
            if (string.IsNullOrWhiteSpace(binding.ConflictId) || parents.ContainsKey(binding.ConflictId) == false) AddError(issues, "ConflictBindingParentMissing", identity, "Conflict participant binding parent is absent.");
            if (string.IsNullOrWhiteSpace(binding.SideId) || sideKeys.Contains(binding.ConflictId + "/" + binding.SideId) == false) AddError(issues, "ConflictBindingSideMissing", identity, "Conflict participant binding side is absent.");
            if (string.IsNullOrWhiteSpace(binding.ArmedForceId) || forceIds.Contains(binding.ArmedForceId) == false) AddError(issues, "ConflictBindingForceMissing", identity, "Conflict participant binding ArmedForce is absent.");
        }
    }

    private static void ValidateWarBindings(IReadOnlyList<WorldStateWarParticipantBindingSnapshot> bindings, Dictionary<string, WorldStateWarSnapshot> parents, HashSet<string> forceIds, HashSet<string> sideKeys, List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateWarParticipantBindingSnapshot binding in bindings ?? new List<WorldStateWarParticipantBindingSnapshot>())
        {
            if (binding == null) { AddError(issues, "WarBindingNull", "war-binding", "Snapshot contains a null War participant binding."); continue; }
            string identity = (binding.WarId ?? "war") + "/" + (binding.BindingId ?? "binding");
            if (string.IsNullOrWhiteSpace(binding.BindingId) || ids.Add(identity) == false) AddError(issues, "DuplicateWarBindingId", identity, "War participant binding identity is missing or duplicated.");
            if (string.IsNullOrWhiteSpace(binding.WarId) || parents.ContainsKey(binding.WarId) == false) AddError(issues, "WarBindingParentMissing", identity, "War participant binding parent is absent.");
            if (string.IsNullOrWhiteSpace(binding.SideId) || sideKeys.Contains(binding.WarId + "/" + binding.SideId) == false) AddError(issues, "WarBindingSideMissing", identity, "War participant binding side is absent.");
            if (string.IsNullOrWhiteSpace(binding.ArmedForceId) || forceIds.Contains(binding.ArmedForceId) == false) AddError(issues, "WarBindingForceMissing", identity, "War participant binding ArmedForce is absent.");
        }
    }

    private static void ValidateBattleBindings(IReadOnlyList<WorldStateBattleParticipantBindingSnapshot> bindings, Dictionary<string, WorldStateBattleSnapshot> parents, HashSet<string> forceIds, HashSet<string> sideKeys, List<WorldStateInvariantIssue> issues)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateBattleParticipantBindingSnapshot binding in bindings ?? new List<WorldStateBattleParticipantBindingSnapshot>())
        {
            if (binding == null) { AddError(issues, "BattleBindingNull", "battle-binding", "Snapshot contains a null Battle participant binding."); continue; }
            string identity = (binding.BattleId ?? "battle") + "/" + (binding.BindingId ?? "binding");
            if (string.IsNullOrWhiteSpace(binding.BindingId) || ids.Add(identity) == false) AddError(issues, "DuplicateBattleBindingId", identity, "Battle participant binding identity is missing or duplicated.");
            if (string.IsNullOrWhiteSpace(binding.BattleId) || parents.ContainsKey(binding.BattleId) == false) AddError(issues, "BattleBindingParentMissing", identity, "Battle participant binding parent is absent.");
            if (string.IsNullOrWhiteSpace(binding.SideId) || sideKeys.Contains(binding.BattleId + "/" + binding.SideId) == false) AddError(issues, "BattleBindingSideMissing", identity, "Battle participant binding side is absent.");
            if (string.IsNullOrWhiteSpace(binding.ArmedForceId) || forceIds.Contains(binding.ArmedForceId) == false) AddError(issues, "BattleBindingForceMissing", identity, "Battle participant binding ArmedForce is absent.");
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

    private static void ValidateCrimeSocialAppraisal(
        IReadOnlyList<WorldStateTheftOutcomeSnapshot> outcomes,
        IReadOnlyList<WorldStateCrimeKnowledgeSnapshot> knowledge,
        IReadOnlyList<WorldStateSocialReactionSnapshot> reactions,
        HashSet<string> personIds,
        IReadOnlyList<string> institutionIds,
        bool hasInstitutionCatalog,
        long absoluteDay,
        List<WorldStateInvariantIssue> issues)
    {
        Dictionary<string, WorldStateTheftOutcomeSnapshot> outcomesById =
            new Dictionary<string, WorldStateTheftOutcomeSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateTheftOutcomeSnapshot outcome in outcomes ?? Array.Empty<WorldStateTheftOutcomeSnapshot>())
        {
            if (outcome == null)
            {
                AddError(issues, "TheftOutcomeNull", "theft-outcome", "Snapshot contains a null theft outcome entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(outcome.OutcomeId) ? "theft-outcome" : outcome.OutcomeId;
            if (string.IsNullOrWhiteSpace(outcome.OutcomeId))
            {
                AddError(issues, "TheftOutcomeIdMissing", identity, "Theft outcome has no stable OutcomeId.");
            }
            else if (outcomesById.ContainsKey(outcome.OutcomeId))
            {
                AddError(issues, "DuplicateTheftOutcomeId", identity, "Theft outcome OutcomeId appears more than once.");
            }
            else
            {
                outcomesById.Add(outcome.OutcomeId, outcome);
            }

            if (string.IsNullOrWhiteSpace(outcome.PerpetratorPersonId)
                || personIds.Contains(outcome.PerpetratorPersonId) == false)
            {
                AddError(issues, "TheftOutcomePerpetratorMissing", identity, "Theft outcome perpetrator PersonId is absent.");
            }

            if (string.IsNullOrWhiteSpace(outcome.VictimPersonId)
                || personIds.Contains(outcome.VictimPersonId) == false)
            {
                AddError(issues, "TheftOutcomeVictimMissing", identity, "Theft outcome victim PersonId is absent.");
            }

            if (string.Equals(outcome.PerpetratorPersonId, outcome.VictimPersonId, StringComparison.Ordinal))
            {
                AddError(issues, "TheftOutcomeSameEndpoint", identity, "Theft outcome perpetrator and victim must differ.");
            }

            if (outcome.LossAmount <= 0)
            {
                AddError(issues, "TheftOutcomeLossInvalid", identity, "Theft outcome loss amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(outcome.OccurrenceKey))
            {
                AddError(issues, "TheftOutcomeOccurrenceKeyMissing", identity, "Theft outcome has no stable semantic occurrence key.");
            }

            if (string.IsNullOrWhiteSpace(outcome.OutcomeId) == false
                && string.IsNullOrWhiteSpace(outcome.PerpetratorPersonId) == false
                && string.IsNullOrWhiteSpace(outcome.VictimPersonId) == false
                && string.IsNullOrWhiteSpace(outcome.OccurrenceKey) == false
                && outcome.OccurredAbsoluteDay >= 0L)
            {
                string expectedOutcomeId = TheftOutcomeId.Create(
                    new PersonId(outcome.PerpetratorPersonId),
                    new PersonId(outcome.VictimPersonId),
                    outcome.OccurredAbsoluteDay,
                    outcome.OccurrenceKey).Value;
                if (string.Equals(expectedOutcomeId, outcome.OutcomeId, StringComparison.Ordinal) == false)
                {
                    AddError(issues, "TheftOutcomeIdentityMismatch", identity, "Theft outcome OutcomeId does not match its canonical semantic identity.");
                }
            }

            if (outcome.OccurredAbsoluteDay < 0L || outcome.OccurredAbsoluteDay > absoluteDay)
            {
                AddError(issues, "TheftOutcomeDayInvalid", identity, "Theft outcome day is outside the snapshot timeline.");
            }
        }

        HashSet<string> knowledgeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateCrimeKnowledgeSnapshot observation in knowledge ?? Array.Empty<WorldStateCrimeKnowledgeSnapshot>())
        {
            if (observation == null)
            {
                AddError(issues, "CrimeKnowledgeNull", "crime-knowledge", "Snapshot contains a null crime knowledge entry.");
                continue;
            }

            string identity = (observation.EvaluatorPersonId ?? "person") + "/" + (observation.OutcomeId ?? "outcome");
            string key = identity;
            if (knowledgeKeys.Add(key) == false)
            {
                AddError(issues, "DuplicateCrimeKnowledge", identity, "Crime knowledge evaluator/outcome pair appears more than once.");
            }

            if (string.IsNullOrWhiteSpace(observation.EvaluatorPersonId)
                || personIds.Contains(observation.EvaluatorPersonId) == false)
            {
                AddError(issues, "CrimeKnowledgeEvaluatorMissing", identity, "Crime knowledge evaluator PersonId is absent.");
            }

            if (outcomesById.TryGetValue(observation.OutcomeId ?? string.Empty, out WorldStateTheftOutcomeSnapshot outcome) == false)
            {
                AddError(issues, "CrimeKnowledgeOutcomeMissing", identity, "Crime knowledge references an absent theft outcome.");
            }
            else
            {
                if (observation.Role == CrimeKnowledgeRole.Victim
                    && observation.EvaluatorPersonId != outcome.VictimPersonId)
                {
                    AddError(issues, "CrimeKnowledgeVictimMismatch", identity, "Victim crime knowledge evaluator must be the theft victim.");
                }

                if (observation.Role == CrimeKnowledgeRole.Perpetrator
                    && observation.EvaluatorPersonId != outcome.PerpetratorPersonId)
                {
                    AddError(issues, "CrimeKnowledgePerpetratorMismatch", identity, "Perpetrator crime knowledge evaluator must be the factual perpetrator.");
                }

                if (observation.ObservedAbsoluteDay < outcome.OccurredAbsoluteDay)
                {
                    AddError(issues, "CrimeKnowledgeBeforeOutcome", identity, "Crime knowledge cannot predate its outcome.");
                }
            }

            if (Enum.IsDefined(typeof(CrimeKnowledgeRole), observation.Role) == false)
            {
                AddError(issues, "CrimeKnowledgeRoleInvalid", identity, "Crime knowledge role is invalid.");
            }

            if (Enum.IsDefined(typeof(SocialPerceivedAttributionKind), observation.PerceivedPerpetratorKind) == false)
            {
                AddError(issues, "CrimeKnowledgeAttributionInvalid", identity, "Crime knowledge attribution kind is invalid.");
            }

            if (observation.KnowsLoss == false
                && observation.PerceivedPerpetratorKind != SocialPerceivedAttributionKind.NotApplicable)
            {
                AddError(issues, "CrimeKnowledgeAttributionWithoutLoss", identity, "Crime knowledge without known loss must not carry perpetrator attribution.");
            }

            if (observation.PerceivedPerpetratorKind == SocialPerceivedAttributionKind.BelievedPerson
                && (string.IsNullOrWhiteSpace(observation.PerceivedPerpetratorPersonId)
                    || personIds.Contains(observation.PerceivedPerpetratorPersonId) == false))
            {
                AddError(issues, "CrimeKnowledgeBelievedPersonMissing", identity, "Believed perpetrator PersonId is absent.");
            }

            if (observation.PerceivedPerpetratorKind == SocialPerceivedAttributionKind.BelievedInstitution
                && (string.IsNullOrWhiteSpace(observation.PerceivedPerpetratorInstitutionId)
                    || (hasInstitutionCatalog && ContainsString(institutionIds, observation.PerceivedPerpetratorInstitutionId) == false)))
            {
                AddError(issues, "CrimeKnowledgeBelievedInstitutionMissing", identity, "Believed perpetrator InstitutionId is absent.");
            }

            if (observation.KnownInvestigatorPersonId != null
                && (string.IsNullOrWhiteSpace(observation.KnownInvestigatorPersonId)
                    || personIds.Contains(observation.KnownInvestigatorPersonId) == false))
            {
                AddError(issues, "CrimeKnowledgeInvestigatorPersonMissing", identity, "Known investigator PersonId is absent.");
            }

            if (observation.KnownInvestigatorInstitutionId != null
                && (string.IsNullOrWhiteSpace(observation.KnownInvestigatorInstitutionId)
                    || (hasInstitutionCatalog && ContainsString(institutionIds, observation.KnownInvestigatorInstitutionId) == false)))
            {
                AddError(issues, "CrimeKnowledgeInvestigatorInstitutionMissing", identity, "Known investigator InstitutionId is absent.");
            }

            if (observation.ObservedAbsoluteDay < 0L || observation.ObservedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "CrimeKnowledgeDayInvalid", identity, "Crime knowledge observation day is outside the snapshot timeline.");
            }
        }

        Dictionary<string, WorldStateSocialReactionSnapshot> reactionsById =
            new Dictionary<string, WorldStateSocialReactionSnapshot>(StringComparer.Ordinal);
        foreach (WorldStateSocialReactionSnapshot reaction in reactions ?? Array.Empty<WorldStateSocialReactionSnapshot>())
        {
            if (reaction == null)
            {
                AddError(issues, "SocialReactionNull", "social-reaction", "Snapshot contains a null social reaction entry.");
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(reaction.ReactionId) ? "social-reaction" : reaction.ReactionId;
            if (string.IsNullOrWhiteSpace(reaction.ReactionId))
            {
                AddError(issues, "SocialReactionIdMissing", identity, "Social reaction has no stable ReactionId.");
            }
            else if (reactionsById.ContainsKey(reaction.ReactionId))
            {
                AddError(issues, "DuplicateSocialReactionId", identity, "Social reaction ReactionId appears more than once.");
            }
            else
            {
                reactionsById.Add(reaction.ReactionId, reaction);
            }

            if (string.IsNullOrWhiteSpace(reaction.EvaluatorPersonId)
                || personIds.Contains(reaction.EvaluatorPersonId) == false)
            {
                AddError(issues, "SocialReactionEvaluatorMissing", identity, "Social reaction evaluator PersonId is absent.");
            }

            if (string.IsNullOrWhiteSpace(reaction.SourceDomain) || string.IsNullOrWhiteSpace(reaction.SourceStableId))
            {
                AddError(issues, "SocialReactionSourceInvalid", identity, "Social reaction source reference is invalid.");
            }

            if (Enum.IsDefined(typeof(SocialReactionTargetKind), reaction.TargetKind)
                == false || string.IsNullOrWhiteSpace(reaction.TargetStableId))
            {
                AddError(issues, "SocialReactionTargetInvalid", identity, "Social reaction target reference is invalid.");
            }

            if (Enum.IsDefined(typeof(SocialPerceivedAttributionKind), reaction.AttributionKind) == false)
            {
                AddError(issues, "SocialReactionAttributionInvalid", identity, "Social reaction attribution kind is invalid.");
            }
            else if (reaction.AttributionKind == SocialPerceivedAttributionKind.BelievedPerson
                && (string.IsNullOrWhiteSpace(reaction.AttributionPersonId)
                    || personIds.Contains(reaction.AttributionPersonId) == false))
            {
                AddError(issues, "SocialReactionBelievedPersonMissing", identity, "Social reaction believed PersonId is absent.");
            }
            else if (reaction.AttributionKind == SocialPerceivedAttributionKind.BelievedInstitution
                && (string.IsNullOrWhiteSpace(reaction.AttributionInstitutionId)
                    || (hasInstitutionCatalog && ContainsString(institutionIds, reaction.AttributionInstitutionId) == false)))
            {
                AddError(issues, "SocialReactionBelievedInstitutionMissing", identity, "Social reaction believed InstitutionId is absent.");
            }

            if (Enum.IsDefined(typeof(SocialReactionValence), reaction.Valence) == false
                || Enum.IsDefined(typeof(SocialReactionSalience), reaction.Salience) == false
                || Enum.IsDefined(typeof(SocialCognitiveBasisKind), reaction.CognitiveBasisKind) == false)
            {
                AddError(issues, "SocialReactionClassificationInvalid", identity, "Social reaction classification is invalid.");
            }

            if (reaction.CreatedAbsoluteDay < 0L || reaction.CreatedAbsoluteDay > absoluteDay)
            {
                AddError(issues, "SocialReactionDayInvalid", identity, "Social reaction day is outside the snapshot timeline.");
            }

        }

        foreach (WorldStateSocialReactionSnapshot reaction in reactions ?? Array.Empty<WorldStateSocialReactionSnapshot>())
        {
            if (reaction == null || reaction.SupersedesReactionId == null)
            {
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(reaction.ReactionId)
                ? "social-reaction"
                : reaction.ReactionId;
            if (reactionsById.TryGetValue(
                    reaction.SupersedesReactionId,
                    out WorldStateSocialReactionSnapshot predecessor) == false)
            {
                AddError(issues, "SocialReactionSupersededMissing", identity, "Social reaction supersession predecessor is absent.");
                continue;
            }

            if (reaction.CreatedAbsoluteDay < predecessor.CreatedAbsoluteDay)
            {
                AddError(issues, "SocialReactionSupersessionDayInvalid", identity, "A social reaction cannot supersede a reaction created later.");
            }

            if (reaction.EvaluatorPersonId != predecessor.EvaluatorPersonId
                || reaction.SourceDomain != predecessor.SourceDomain
                || reaction.SourceStableId != predecessor.SourceStableId
                || reaction.TargetKind != predecessor.TargetKind
                || reaction.TargetStableId != predecessor.TargetStableId)
            {
                AddError(issues, "SocialReactionSupersessionThreadInvalid", identity, "A superseding social reaction must remain in the evaluator/source/target thread.");
            }
        }

        HashSet<string> supersededPredecessors = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldStateSocialReactionSnapshot reaction in reactions ?? Array.Empty<WorldStateSocialReactionSnapshot>())
        {
            if (reaction == null || reaction.SupersedesReactionId == null)
            {
                continue;
            }

            string identity = string.IsNullOrWhiteSpace(reaction.ReactionId)
                ? "social-reaction"
                : reaction.ReactionId;
            if (supersededPredecessors.Add(reaction.SupersedesReactionId) == false)
            {
                AddError(issues, "SocialReactionSupersessionBranch", identity, "A social reaction predecessor has more than one superseding reaction.");
            }

            HashSet<string> lineage = new HashSet<string>(StringComparer.Ordinal);
            WorldStateSocialReactionSnapshot current = reaction;
            while (current != null && current.SupersedesReactionId != null)
            {
                if (lineage.Add(current.ReactionId ?? string.Empty) == false)
                {
                    AddError(issues, "SocialReactionSupersessionCycle", identity, "Social reaction supersession lineage contains a cycle.");
                    break;
                }

                if (reactionsById.TryGetValue(current.SupersedesReactionId, out current) == false)
                {
                    break;
                }
            }
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
