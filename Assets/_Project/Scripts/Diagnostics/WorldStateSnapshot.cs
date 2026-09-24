using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;

public sealed class WorldStateSnapshotContext
{
    public SimulationTime SimulationTime { get; }
    public SimulationCalendar Calendar { get; }
    public IEnumerable<NpcRuntime> Npcs { get; }
    public IEnumerable<CityRuntime> Cities { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public SpatialAuthorityStore SpatialAuthorityStore { get; }
    public ExplorableSiteStore ExplorableSiteStore { get; }
    public ExpeditionStore ExpeditionStore { get; }
    public PlaceContentStore PlaceContentStore { get; }
    public LocalTopologyStore LocalTopologyStore { get; }
    public PersonStore PersonStore { get; }
    public ArmedForceStore ArmedForceStore { get; }
    public ContingentManpowerStateStore ContingentManpowerStateStore { get; }
    public PersistentConflictStore ConflictStore { get; }
    public PersistentWarStore WarStore { get; }
    public PersistentBattleStore BattleStore { get; }
    public ArmedForceSpatialStateStore ArmedForceSpatialStateStore { get; }
    public IEnumerable<ParentageRecord> Parentages { get; }
    public GenealogyStore GenealogyStore { get; }
    public PropertyOwnershipStore PropertyOwnershipStore { get; }
    public EstateStore EstateStore { get; }
    public InstitutionStore InstitutionStore { get; }
    public OfficeStore OfficeStore { get; }
    public IEnumerable<string> InstitutionIds { get; }
    public IEnumerable<string> OfficeIds { get; }
    public IReadOnlyDictionary<string, string> OfficeInstitutionIds { get; }
    public IEnumerable<string> PropertyIds { get; }
    public IEnumerable<FactionRecord> Factions { get; }
    public IEnumerable<FactionAffiliationRecord> FactionAffiliations { get; }
    public IEnumerable<PoliticalClaimRecord> PoliticalClaims { get; }
    public IEnumerable<PoliticalClaimRecognitionRecord> PoliticalClaimRecognitions { get; }
    public IEnumerable<PoliticalSupportRelationRecord> PoliticalSupports { get; }
    public IEnumerable<PoliticalDecisionRecord> PoliticalDecisions { get; }
    public IEnumerable<PoliticalKnowledgeRuntime> PoliticalKnowledgeRuntimes { get; }
    public long? PoliticalKnowledgeRevision { get; }
    public CrimeSocialAppraisalWorldState CrimeSocialAppraisal { get; }

    public WorldStateSnapshotContext(
        SimulationTime simulationTime = null,
        IEnumerable<NpcRuntime> npcs = null,
        IEnumerable<CityRuntime> cities = null,
        SpatialNetworkRuntime spatialNetwork = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExpeditionStore expeditionStore = null,
        PlaceContentStore placeContentStore = null,
        LocalTopologyStore localTopologyStore = null,
        SimulationCalendar calendar = null,
        CalendarDefinition calendarDefinition = null,
        PersonStore personStore = null,
        IEnumerable<ParentageRecord> parentages = null,
        GenealogyStore genealogyStore = null,
        PropertyOwnershipStore propertyOwnershipStore = null,
        EstateStore estateStore = null,
        IEnumerable<PoliticalClaimRecord> politicalClaims = null,
        InstitutionStore institutionStore = null,
        OfficeStore officeStore = null,
        IEnumerable<string> institutionIds = null,
        IEnumerable<string> officeIds = null,
        IReadOnlyDictionary<string, string> officeInstitutionIds = null,
        IEnumerable<string> propertyIds = null,
        IEnumerable<FactionRecord> factions = null,
        IEnumerable<FactionAffiliationRecord> factionAffiliations = null,
        IEnumerable<PoliticalSupportRelationRecord> politicalSupports = null,
        IEnumerable<PoliticalDecisionRecord> politicalDecisions = null,
        IEnumerable<PoliticalKnowledgeRuntime> politicalKnowledgeRuntimes = null,
        long? politicalKnowledgeRevision = null,
        IEnumerable<PoliticalClaimRecognitionRecord> politicalClaimRecognitions = null,
        CrimeSocialAppraisalWorldState crimeSocialAppraisal = null,
        ArmedForceStore armedForceStore = null,
        PersistentConflictStore conflictStore = null,
        PersistentWarStore warStore = null,
        PersistentBattleStore battleStore = null,
        SpatialAuthorityStore spatialAuthorityStore = null,
        ArmedForceSpatialStateStore armedForceSpatialStateStore = null,
        ContingentManpowerStateStore contingentManpowerStateStore = null)
    {
        SimulationTime = simulationTime;
        Calendar = calendar ?? (calendarDefinition != null ? new SimulationCalendar(calendarDefinition) : null);
        Npcs = npcs ?? Array.Empty<NpcRuntime>();
        Cities = cities ?? Array.Empty<CityRuntime>();
        SpatialNetwork = spatialNetwork;
        SpatialAuthorityStore = spatialAuthorityStore;
        ExplorableSiteStore = explorableSiteStore;
        ExpeditionStore = expeditionStore;
        PlaceContentStore = placeContentStore;
        LocalTopologyStore = localTopologyStore;
        PersonStore = personStore;
        ArmedForceStore = armedForceStore;
        ConflictStore = conflictStore;
        WarStore = warStore;
        BattleStore = battleStore;
        ArmedForceSpatialStateStore = armedForceSpatialStateStore;
        ContingentManpowerStateStore = contingentManpowerStateStore;
        GenealogyStore = genealogyStore;
        PropertyOwnershipStore = propertyOwnershipStore;
        EstateStore = estateStore;
        InstitutionStore = institutionStore;
        OfficeStore = officeStore;
        InstitutionIds = institutionIds;
        OfficeIds = officeIds;
        OfficeInstitutionIds = officeInstitutionIds;
        PropertyIds = propertyIds;
        Factions = factions;
        FactionAffiliations = factionAffiliations;
        PoliticalClaims = politicalClaims ?? Array.Empty<PoliticalClaimRecord>();
        PoliticalClaimRecognitions = politicalClaimRecognitions ?? Array.Empty<PoliticalClaimRecognitionRecord>();
        PoliticalSupports = politicalSupports ?? Array.Empty<PoliticalSupportRelationRecord>();
        PoliticalDecisions = politicalDecisions ?? Array.Empty<PoliticalDecisionRecord>();
        PoliticalKnowledgeRuntimes = politicalKnowledgeRuntimes;
        PoliticalKnowledgeRevision = politicalKnowledgeRevision;
        CrimeSocialAppraisal = crimeSocialAppraisal;
        Parentages = parentages
            ?? genealogyStore?.Records
            ?? Array.Empty<ParentageRecord>();
    }
}

public sealed class WorldStateSnapshot
{
    public WorldStateSnapshotMetadata Metadata { get; }
    public long AbsoluteDay => Metadata.AbsoluteDay;
    public WorldStateCalendarSnapshot Calendar => Metadata.CalendarDate;
    public IReadOnlyList<WorldStateNpcSnapshot> Npcs { get; }
    public IReadOnlyList<WorldStateCitySnapshot> Cities { get; }
    public IReadOnlyList<WorldStateCitySnapshot> Settlements => Cities;
    public int SettlementCount => Cities.Count;
    public int KnownNpcCount => Npcs.Count;
    public IReadOnlyList<WorldStatePersonSnapshot> Persons { get; }
    public int PersonCount => Persons.Count;
    public IReadOnlyList<WorldStateArmedForceSnapshot> ArmedForces { get; }
    public IReadOnlyList<WorldStateArmedForceContingentSnapshot> ArmedForceContingents { get; }
    public IReadOnlyList<WorldStateArmedForcePersonReferenceSnapshot> ArmedForceRelevantPersons { get; }
    public long? ArmedForceRevision { get; }
    public bool HasArmedForceState => ArmedForceRevision.HasValue;
    public IReadOnlyList<WorldStateArmedForcePositionSnapshot> ArmedForcePositions { get; }
    public long? ArmedForceSpatialRevision { get; }
    public bool HasArmedForceSpatialState => ArmedForceSpatialRevision.HasValue;
    public IReadOnlyList<WorldStateContingentManpowerSnapshot> ContingentManpowerStates { get; }
    public long? ContingentManpowerRevision { get; }
    public bool HasContingentManpowerState => ContingentManpowerRevision.HasValue;
    public IReadOnlyList<WorldStateConflictSnapshot> Conflicts { get; }
    public IReadOnlyList<WorldStateConflictSideSnapshot> ConflictSides { get; }
    public IReadOnlyList<WorldStateConflictParticipantBindingSnapshot> ConflictParticipantBindings { get; }
    public long? ConflictRevision { get; }
    public bool HasConflictState => ConflictRevision.HasValue;
    public IReadOnlyList<WorldStateWarSnapshot> Wars { get; }
    public IReadOnlyList<WorldStateWarSideSnapshot> WarSides { get; }
    public IReadOnlyList<WorldStateWarParticipantBindingSnapshot> WarParticipantBindings { get; }
    public long? WarRevision { get; }
    public bool HasWarState => WarRevision.HasValue;
    public IReadOnlyList<WorldStateBattleSnapshot> Battles { get; }
    public IReadOnlyList<WorldStateBattleSideSnapshot> BattleSides { get; }
    public IReadOnlyList<WorldStateBattleParticipantBindingSnapshot> BattleParticipantBindings { get; }
    public long? BattleRevision { get; }
    public bool HasBattleState => BattleRevision.HasValue;
    public IReadOnlyList<WorldStateParentageSnapshot> Parentages { get; }
    public int ParentageCount => Parentages.Count;
    public IReadOnlyList<WorldStatePropertyOwnershipSnapshot> PropertyOwnerships { get; }
    public int PropertyOwnershipCount => PropertyOwnerships.Count;
    public IReadOnlyList<WorldStatePropertyTransferSnapshot> PropertyTransfers { get; }
    public int PropertyTransferCount => PropertyTransfers.Count;
    public IReadOnlyList<WorldStateEstateSnapshot> Estates { get; }
    public int EstateCount => Estates.Count;
    public IReadOnlyList<WorldStatePoliticalClaimSnapshot> PoliticalClaims { get; }
    public IReadOnlyList<WorldStatePoliticalClaimRecognitionSnapshot> PoliticalClaimRecognitions { get; }
    public int PoliticalClaimCount => PoliticalClaims.Count;
    public IReadOnlyList<string> InstitutionIds { get; }
    public IReadOnlyList<string> OfficeIds { get; }
    public IReadOnlyDictionary<string, string> OfficeInstitutionIds { get; }
    public IReadOnlyList<string> PropertyIds { get; }
    public bool HasInstitutionCatalog { get; }
    public bool HasOfficeCatalog { get; }
    public bool HasPropertyCatalog { get; }
    public IReadOnlyList<WorldStateFactionSnapshot> Factions { get; }
    public int FactionCount => Factions.Count;
    public IReadOnlyList<WorldStateFactionAffiliationSnapshot> FactionAffiliations { get; }
    public int FactionAffiliationCount => FactionAffiliations.Count;
    public IReadOnlyList<WorldStatePoliticalSupportSnapshot> PoliticalSupports { get; }
    public int PoliticalSupportCount => PoliticalSupports.Count;
    public IReadOnlyList<WorldStatePoliticalDecisionSnapshot> PoliticalDecisions { get; }
    public int PoliticalDecisionCount => PoliticalDecisions.Count;
    public IReadOnlyList<WorldStatePoliticalKnowledgeSnapshot> PoliticalKnowledge { get; }
    public int PoliticalKnowledgeHolderCount => PoliticalKnowledge.Count;
    public long PoliticalKnowledgeRevision { get; }
    public bool HasPoliticalKnowledgeState { get; }
    public IReadOnlyList<WorldStateTheftOutcomeSnapshot> TheftOutcomes { get; }
    public int TheftOutcomeCount => TheftOutcomes.Count;
    public IReadOnlyList<WorldStateCrimeKnowledgeSnapshot> CrimeKnowledge { get; }
    public int CrimeKnowledgeCount => CrimeKnowledge.Count;
    public IReadOnlyList<WorldStateSocialReactionSnapshot> SocialReactions { get; }
    public int SocialReactionCount => SocialReactions.Count;
    public WorldStateSpatialSnapshot Spatial { get; }
    public IReadOnlyList<WorldStateSiteSnapshot> Sites { get; }
    public IReadOnlyList<WorldStateExpeditionSnapshot> Expeditions { get; }
    public IReadOnlyList<WorldStatePlaceContentSnapshot> PlaceContents { get; }
    public IReadOnlyList<WorldStateNotableItemSnapshot> NotableItems { get; }
    public IReadOnlyList<WorldStateLocalTopologySnapshot> LocalTopologies { get; }

    public WorldStateSnapshot(
        long absoluteDay,
        IEnumerable<WorldStateNpcSnapshot> npcs = null,
        IEnumerable<WorldStateCitySnapshot> cities = null,
        WorldStateSpatialSnapshot spatial = null,
        IEnumerable<WorldStateSiteSnapshot> sites = null,
        IEnumerable<WorldStateExpeditionSnapshot> expeditions = null,
        IEnumerable<WorldStatePlaceContentSnapshot> placeContents = null,
        IEnumerable<WorldStateNotableItemSnapshot> notableItems = null,
        IEnumerable<WorldStateLocalTopologySnapshot> localTopologies = null,
        WorldStateCalendarSnapshot calendarDate = null,
        IEnumerable<WorldStatePersonSnapshot> persons = null,
        IEnumerable<WorldStateParentageSnapshot> parentages = null,
        IEnumerable<WorldStatePropertyOwnershipSnapshot> propertyOwnerships = null,
        IEnumerable<WorldStateEstateSnapshot> estates = null,
        IEnumerable<WorldStatePropertyTransferSnapshot> propertyTransfers = null,
        IEnumerable<WorldStatePoliticalClaimSnapshot> politicalClaims = null,
        IEnumerable<string> institutionIds = null,
        IEnumerable<string> officeIds = null,
        IEnumerable<string> propertyIds = null,
        IEnumerable<WorldStateFactionSnapshot> factions = null,
        IEnumerable<WorldStateFactionAffiliationSnapshot> factionAffiliations = null,
        IEnumerable<WorldStatePoliticalSupportSnapshot> politicalSupports = null,
        IEnumerable<WorldStatePoliticalDecisionSnapshot> politicalDecisions = null,
        IEnumerable<WorldStatePoliticalKnowledgeSnapshot> politicalKnowledge = null,
        long politicalKnowledgeRevision = 0L,
        bool hasPoliticalKnowledgeState = false,
        IReadOnlyDictionary<string, string> officeInstitutionIds = null,
        IEnumerable<WorldStatePoliticalClaimRecognitionSnapshot> politicalClaimRecognitions = null,
        IEnumerable<WorldStateTheftOutcomeSnapshot> theftOutcomes = null,
        IEnumerable<WorldStateCrimeKnowledgeSnapshot> crimeKnowledge = null,
        IEnumerable<WorldStateSocialReactionSnapshot> socialReactions = null,
        IEnumerable<WorldStateArmedForceSnapshot> armedForces = null,
        IEnumerable<WorldStateArmedForceContingentSnapshot> armedForceContingents = null,
        IEnumerable<WorldStateArmedForcePersonReferenceSnapshot> armedForceRelevantPersons = null,
        long? armedForceRevision = null,
        IEnumerable<WorldStateConflictSnapshot> conflicts = null,
        IEnumerable<WorldStateConflictSideSnapshot> conflictSides = null,
        IEnumerable<WorldStateConflictParticipantBindingSnapshot> conflictParticipantBindings = null,
        long? conflictRevision = null,
        IEnumerable<WorldStateWarSnapshot> wars = null,
        IEnumerable<WorldStateWarSideSnapshot> warSides = null,
        IEnumerable<WorldStateWarParticipantBindingSnapshot> warParticipantBindings = null,
        long? warRevision = null,
        IEnumerable<WorldStateBattleSnapshot> battles = null,
        IEnumerable<WorldStateBattleSideSnapshot> battleSides = null,
        IEnumerable<WorldStateBattleParticipantBindingSnapshot> battleParticipantBindings = null,
        long? battleRevision = null,
        IEnumerable<WorldStateArmedForcePositionSnapshot> armedForcePositions = null,
        long? armedForceSpatialRevision = null,
        IEnumerable<WorldStateContingentManpowerSnapshot> contingentManpowerStates = null,
        long? contingentManpowerRevision = null)
    {
        Metadata = new WorldStateSnapshotMetadata(absoluteDay, calendarDate);
        Npcs = SnapshotCollections.CopySorted(npcs, npc => npc?.RuntimeId);
        Cities = SnapshotCollections.CopySorted(cities, city => city?.RuntimeId);
        Spatial = spatial ?? new WorldStateSpatialSnapshot();
        Sites = SnapshotCollections.CopySorted(sites, site => site?.RuntimeId);
        Expeditions = SnapshotCollections.CopySorted(expeditions, expedition => expedition?.ExpeditionId);
        PlaceContents = SnapshotCollections.CopySorted(placeContents, content => content?.StableKey);
        NotableItems = SnapshotCollections.CopySorted(notableItems, notable => notable?.RuntimeId);
        LocalTopologies = SnapshotCollections.CopySorted(localTopologies, topology => topology?.StableKey);
        Persons = SnapshotCollections.CopySorted(persons, person => person?.PersonId);
        ArmedForces = SnapshotCollections.CopySorted(armedForces, force => force?.ArmedForceId);
        ArmedForceContingents = SnapshotCollections.CopySorted(
            armedForceContingents,
            contingent => contingent?.ContingentId);
        ArmedForceRelevantPersons = SnapshotCollections.CopySorted(
            armedForceRelevantPersons,
            reference => reference == null
                ? null
                : reference.ForceId + "\u001f" + reference.RoleKey + "\u001f" + reference.PersonId + "\u001f" + reference.ReferenceId);
        ArmedForceRevision = armedForceRevision;
        ArmedForcePositions = SnapshotCollections.CopySorted(
            armedForcePositions,
            position => position?.ArmedForceId);
        ArmedForceSpatialRevision = armedForceSpatialRevision;
        ContingentManpowerStates = SnapshotCollections.CopySorted(
            contingentManpowerStates,
            state => state?.ContingentId);
        ContingentManpowerRevision = contingentManpowerRevision;
        Conflicts = SnapshotCollections.CopySorted(conflicts, conflict => conflict?.ConflictId);
        ConflictSides = SnapshotCollections.CopySorted(
            conflictSides,
            side => side == null ? null : side.ConflictId + "\u001f" + side.SideId);
        ConflictParticipantBindings = SnapshotCollections.CopySorted(
            conflictParticipantBindings,
            binding => binding == null ? null : binding.ConflictId + "\u001f" + binding.BindingId);
        ConflictRevision = conflictRevision;
        Wars = SnapshotCollections.CopySorted(wars, war => war?.WarId);
        WarSides = SnapshotCollections.CopySorted(
            warSides,
            side => side == null ? null : side.WarId + "\u001f" + side.SideId);
        WarParticipantBindings = SnapshotCollections.CopySorted(
            warParticipantBindings,
            binding => binding == null ? null : binding.WarId + "\u001f" + binding.BindingId);
        WarRevision = warRevision;
        Battles = SnapshotCollections.CopySorted(battles, battle => battle?.BattleId);
        BattleSides = SnapshotCollections.CopySorted(
            battleSides,
            side => side == null ? null : side.BattleId + "\u001f" + side.SideId);
        BattleParticipantBindings = SnapshotCollections.CopySorted(
            battleParticipantBindings,
            binding => binding == null ? null : binding.BattleId + "\u001f" + binding.BindingId);
        BattleRevision = battleRevision;
        Parentages = SortParentages(parentages);
        PropertyOwnerships = SnapshotCollections.CopySorted(
            propertyOwnerships,
            ownership => ownership?.PropertyId);
        PropertyTransfers = SnapshotCollections.CopySorted(
            propertyTransfers,
            transfer => transfer == null
                ? null
                : transfer.PropertyId + "\u001f"
                    + transfer.TransferAbsoluteDay.ToString(CultureInfo.InvariantCulture)
                    + "\u001f"
                    + transfer.PreviousOwnerPersonId
                    + "\u001f"
                    + transfer.NewOwnerPersonId);
        Estates = SnapshotCollections.CopySorted(estates, estate => estate?.EstateId);
        PoliticalClaims = SnapshotCollections.CopySorted(politicalClaims, claim => claim?.ClaimId);
        PoliticalClaimRecognitions = SnapshotCollections.CopySorted(
            politicalClaimRecognitions,
            recognition => recognition == null ? null : recognition.ClaimId + "\u001f" + recognition.InstitutionId);
        HasInstitutionCatalog = institutionIds != null;
        HasOfficeCatalog = officeIds != null;
        HasPropertyCatalog = propertyIds != null;
        InstitutionIds = CopySortedIds(institutionIds);
        OfficeIds = CopySortedIds(officeIds);
        OfficeInstitutionIds = CopyDictionary(officeInstitutionIds);
        PropertyIds = CopySortedIds(propertyIds);
        Factions = SnapshotCollections.CopySorted(factions, faction => faction?.FactionId);
        FactionAffiliations = SnapshotCollections.CopySorted(
            factionAffiliations,
            affiliation => affiliation == null
                ? null
                : affiliation.FactionId + "\u001f" + affiliation.PersonId);
        PoliticalSupports = SnapshotCollections.CopySorted(
            politicalSupports,
            support => support?.RelationId);
        PoliticalDecisions = SnapshotCollections.CopySorted(
            politicalDecisions,
            decision => decision?.DecisionId);
        PoliticalKnowledge = SnapshotCollections.CopySorted(
            politicalKnowledge,
            knowledge => knowledge?.HolderStableId);
        PoliticalKnowledgeRevision = politicalKnowledgeRevision;
        HasPoliticalKnowledgeState = hasPoliticalKnowledgeState || politicalKnowledge != null;
        TheftOutcomes = SnapshotCollections.CopySorted(theftOutcomes, outcome => outcome?.OutcomeId);
        CrimeKnowledge = SnapshotCollections.CopySorted(
            crimeKnowledge,
            observation => observation == null
                ? null
                : observation.EvaluatorPersonId + "\u001f" + observation.OutcomeId);
        SocialReactions = SnapshotCollections.CopySorted(socialReactions, reaction => reaction?.ReactionId);
    }

    private static IReadOnlyList<WorldStateParentageSnapshot> SortParentages(
        IEnumerable<WorldStateParentageSnapshot> source)
    {
        List<WorldStateParentageSnapshot> result = new List<WorldStateParentageSnapshot>();
        if (source != null)
        {
            foreach (WorldStateParentageSnapshot parentage in source)
            {
                if (parentage != null)
                {
                    result.Add(parentage);
                }
            }
        }

        result.Sort((left, right) =>
        {
            int parent = StringComparer.Ordinal.Compare(left.ParentPersonId, right.ParentPersonId);
            return parent != 0
                ? parent
                : StringComparer.Ordinal.Compare(left.ChildPersonId, right.ChildPersonId);
        });
        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> CopySortedIds(IEnumerable<string> source)
    {
        List<string> result = new List<string>();
        if (source != null)
        {
            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value) == false && result.Contains(value) == false)
                {
                    result.Add(value);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, string> CopyDictionary(
        IReadOnlyDictionary<string, string> source)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (source != null)
        {
            foreach (KeyValuePair<string, string> entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) == false
                    && string.IsNullOrWhiteSpace(entry.Value) == false)
                {
                    result[entry.Key] = entry.Value;
                }
            }
        }

        return new ReadOnlyDictionary<string, string>(result);
    }
}

public sealed class WorldStatePersonSnapshot
{
    public string PersonId { get; }
    public long? BirthAbsoluteDay { get; }
    public bool HasKnownBirthDay => BirthAbsoluteDay.HasValue;
    public long? DeathAbsoluteDay { get; }
    public long? AgeInDays { get; }
    public long? CompletedYears { get; }
    public string ResidenceSettlementRuntimeId { get; }
    public string MaterializedNpcRuntimeId { get; }
    public bool IsMaterialized => string.IsNullOrWhiteSpace(MaterializedNpcRuntimeId) == false;

    public WorldStatePersonSnapshot(
        string personId,
        string materializedNpcRuntimeId)
        : this(personId, null, null, null, null, materializedNpcRuntimeId, null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string materializedNpcRuntimeId)
        : this(
            personId,
            birthAbsoluteDay,
            ageInDays,
            completedYears,
            null,
            materializedNpcRuntimeId,
            null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId)
        : this(
            personId,
            birthAbsoluteDay,
            ageInDays,
            completedYears,
            residenceSettlementRuntimeId,
            materializedNpcRuntimeId,
            null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId,
        long? deathAbsoluteDay)
    {
        PersonId = personId;
        BirthAbsoluteDay = birthAbsoluteDay;
        DeathAbsoluteDay = deathAbsoluteDay;
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        MaterializedNpcRuntimeId = materializedNpcRuntimeId;
    }
}

public sealed class WorldStateParentageSnapshot
{
    public string ParentPersonId { get; }
    public string ChildPersonId { get; }

    public WorldStateParentageSnapshot(string parentPersonId, string childPersonId)
    {
        ParentPersonId = parentPersonId;
        ChildPersonId = childPersonId;
    }
}

public sealed class WorldStatePropertyOwnershipSnapshot
{
    public string PropertyId { get; }
    public string OwnerPersonId { get; }

    public WorldStatePropertyOwnershipSnapshot(string propertyId, string ownerPersonId)
    {
        PropertyId = propertyId;
        OwnerPersonId = ownerPersonId;
    }
}

public sealed class WorldStatePropertyTransferSnapshot
{
    public string PropertyId { get; }
    public string PreviousOwnerPersonId { get; }
    public string NewOwnerPersonId { get; }
    public long TransferAbsoluteDay { get; }

    public WorldStatePropertyTransferSnapshot(
        string propertyId,
        string previousOwnerPersonId,
        string newOwnerPersonId,
        long transferAbsoluteDay)
    {
        PropertyId = propertyId;
        PreviousOwnerPersonId = previousOwnerPersonId;
        NewOwnerPersonId = newOwnerPersonId;
        TransferAbsoluteDay = transferAbsoluteDay;
    }
}

public sealed class WorldStateEstateSnapshot
{
    public string EstateId { get; }
    public string DeceasedPersonId { get; }
    public long OpenedAbsoluteDay { get; }

    public WorldStateEstateSnapshot(
        string estateId,
        string deceasedPersonId,
        long openedAbsoluteDay)
    {
        EstateId = estateId;
        DeceasedPersonId = deceasedPersonId;
        OpenedAbsoluteDay = openedAbsoluteDay;
    }
}

public sealed class WorldStatePoliticalClaimSnapshot
{
    public string ClaimId { get; }
    public string ClaimantPersonId { get; }
    public PoliticalClaimType ClaimType { get; }
    public PoliticalClaimTargetKind TargetKind { get; }
    public string TargetId { get; }
    public PoliticalClaimBasis Basis { get; }
    public string BasisDescription { get; }
    public long CreatedAbsoluteDay { get; }
    public PoliticalClaimStatus Status { get; }
    public long? ResolutionAbsoluteDay { get; }
    public PoliticalClaimRecognitionState RecognitionState { get; }
    public string RecognizingInstitutionId { get; }
    public long? RecognitionAbsoluteDay { get; }
    public string RecognitionReason { get; }
    public IReadOnlyList<string> EvidenceReferences { get; }

    public WorldStatePoliticalClaimSnapshot(
        string claimId,
        string claimantPersonId,
        PoliticalClaimType claimType,
        PoliticalClaimTargetKind targetKind,
        string targetId,
        PoliticalClaimBasis basis,
        string basisDescription,
        long createdAbsoluteDay,
        PoliticalClaimStatus status,
        long? resolutionAbsoluteDay,
        PoliticalClaimRecognitionState recognitionState,
        string recognizingInstitutionId,
        long? recognitionAbsoluteDay,
        string recognitionReason,
        IEnumerable<string> evidenceReferences)
    {
        ClaimId = claimId;
        ClaimantPersonId = claimantPersonId;
        ClaimType = claimType;
        TargetKind = targetKind;
        TargetId = targetId;
        Basis = basis;
        BasisDescription = basisDescription ?? string.Empty;
        CreatedAbsoluteDay = createdAbsoluteDay;
        Status = status;
        ResolutionAbsoluteDay = resolutionAbsoluteDay;
        RecognitionState = recognitionState;
        RecognizingInstitutionId = recognizingInstitutionId;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        RecognitionReason = recognitionReason ?? string.Empty;

        List<string> references = new List<string>();
        if (evidenceReferences != null)
        {
            foreach (string reference in evidenceReferences)
            {
                if (string.IsNullOrWhiteSpace(reference) == false && references.Contains(reference) == false)
                {
                    references.Add(reference);
                }
            }
        }

        references.Sort(StringComparer.Ordinal);
        EvidenceReferences = references.AsReadOnly();
    }
}

public sealed class WorldStatePoliticalClaimRecognitionSnapshot
{
    public string ClaimId { get; }
    public string InstitutionId { get; }
    public PoliticalClaimRecognitionState State { get; }
    public long RecognitionAbsoluteDay { get; }
    public string Reason { get; }
    public IReadOnlyList<WorldStatePoliticalClaimRecognitionHistorySnapshot> History { get; }

    public WorldStatePoliticalClaimRecognitionSnapshot(
        string claimId,
        string institutionId,
        PoliticalClaimRecognitionState state,
        long recognitionAbsoluteDay,
        string reason,
        IEnumerable<WorldStatePoliticalClaimRecognitionHistorySnapshot> history = null)
    {
        ClaimId = claimId;
        InstitutionId = institutionId;
        State = state;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason ?? string.Empty;
        List<WorldStatePoliticalClaimRecognitionHistorySnapshot> entries =
            new List<WorldStatePoliticalClaimRecognitionHistorySnapshot>();
        if (history != null)
        {
            foreach (WorldStatePoliticalClaimRecognitionHistorySnapshot entry in history)
            {
                if (entry != null) entries.Add(entry);
            }
        }
        History = entries.AsReadOnly();
    }
}

public sealed class WorldStatePoliticalClaimRecognitionHistorySnapshot
{
    public PoliticalClaimRecognitionState State { get; }
    public long RecognitionAbsoluteDay { get; }
    public string Reason { get; }

    public WorldStatePoliticalClaimRecognitionHistorySnapshot(
        PoliticalClaimRecognitionState state,
        long recognitionAbsoluteDay,
        string reason)
    {
        State = state;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason ?? string.Empty;
    }
}

public sealed class WorldStateFactionSnapshot
{
    public string FactionId { get; }
    public string DisplayName { get; }
    public long CreatedAbsoluteDay { get; }
    public FactionMembershipPolicy MembershipPolicy { get; }
    public bool ExpulsionAllowed { get; }

    public WorldStateFactionSnapshot(
        string factionId,
        string displayName,
        long createdAbsoluteDay,
        FactionMembershipPolicy membershipPolicy = FactionMembershipPolicy.LeaveAndRejoin,
        bool expulsionAllowed = true)
    {
        FactionId = factionId;
        DisplayName = displayName ?? string.Empty;
        CreatedAbsoluteDay = createdAbsoluteDay;
        MembershipPolicy = membershipPolicy;
        ExpulsionAllowed = expulsionAllowed;
    }
}

public sealed class WorldStateFactionAffiliationSnapshot
{
    public string AffiliationId { get; }
    public string FactionId { get; }
    public string PersonId { get; }
    public long JoinedAbsoluteDay { get; }
    public long? EndedAbsoluteDay { get; }
    public FactionAffiliationEndReason? EndReason { get; }
    public bool IsActive => EndedAbsoluteDay.HasValue == false;

    public WorldStateFactionAffiliationSnapshot(
        string factionId,
        string personId,
        long joinedAbsoluteDay,
        long? endedAbsoluteDay,
        string affiliationId = null,
        FactionAffiliationEndReason? endReason = null)
    {
        FactionId = factionId;
        PersonId = personId;
        JoinedAbsoluteDay = joinedAbsoluteDay;
        EndedAbsoluteDay = endedAbsoluteDay;
        AffiliationId = affiliationId;
        EndReason = endReason;
    }
}

public sealed class WorldStatePoliticalSupportSnapshot
{
    public string RelationId { get; }
    public PoliticalSupportSourceKind SourceKind { get; }
    public string SourceId { get; }
    public PoliticalSupportTargetKind TargetKind { get; }
    public string TargetId { get; }
    public PoliticalSupportDisposition Disposition { get; }
    public long StartedAbsoluteDay { get; }
    public long? EndedAbsoluteDay { get; }
    public bool IsActive => EndedAbsoluteDay.HasValue == false;

    public WorldStatePoliticalSupportSnapshot(
        string relationId,
        PoliticalSupportSourceKind sourceKind,
        string sourceId,
        PoliticalSupportTargetKind targetKind,
        string targetId,
        PoliticalSupportDisposition disposition,
        long startedAbsoluteDay,
        long? endedAbsoluteDay)
    {
        RelationId = relationId;
        SourceKind = sourceKind;
        SourceId = sourceId;
        TargetKind = targetKind;
        TargetId = targetId;
        Disposition = disposition;
        StartedAbsoluteDay = startedAbsoluteDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }
}

public sealed class WorldStatePoliticalDecisionSnapshot
{
    public string DecisionId { get; }
    public string DeciderStableId { get; }
    public PoliticalDecisionKind DecisionKind { get; }
    public string OfficeId { get; }
    public string RecognizingInstitutionId { get; }
    public IReadOnlyList<string> CandidatePersonIds { get; }
    public string CandidateFingerprint { get; }
    public PoliticalDecisionOutcomeKind OutcomeKind { get; }
    public string SelectedCandidatePersonId { get; }
    public string ReferencedClaimId { get; }
    public IReadOnlyList<string> EvidenceReferences { get; }
    public IReadOnlyList<string> KnowledgeReferences { get; }
    public long ObservedAbsoluteDay { get; }
    public long DecisionAbsoluteDay { get; }
    public long ExpectedWorldRevision { get; }
    public long ExpectedKnowledgeRevision { get; }

    public WorldStatePoliticalDecisionSnapshot(
        string decisionId,
        string deciderStableId,
        PoliticalDecisionKind decisionKind,
        string officeId,
        string recognizingInstitutionId,
        IEnumerable<string> candidatePersonIds,
        string candidateFingerprint,
        PoliticalDecisionOutcomeKind outcomeKind,
        string selectedCandidatePersonId,
        string referencedClaimId,
        IEnumerable<string> evidenceReferences,
        IEnumerable<string> knowledgeReferences,
        long observedAbsoluteDay,
        long decisionAbsoluteDay,
        long expectedWorldRevision,
        long expectedKnowledgeRevision)
    {
        DecisionId = decisionId;
        DeciderStableId = deciderStableId;
        DecisionKind = decisionKind;
        OfficeId = officeId;
        RecognizingInstitutionId = recognizingInstitutionId;
        List<string> candidates = new List<string>();
        if (candidatePersonIds != null)
        {
            foreach (string candidate in candidatePersonIds)
            {
                candidates.Add(candidate);
            }
        }

        CandidatePersonIds = new ReadOnlyCollection<string>(candidates);
        CandidateFingerprint = candidateFingerprint;
        OutcomeKind = outcomeKind;
        SelectedCandidatePersonId = selectedCandidatePersonId;
        ReferencedClaimId = referencedClaimId;
        EvidenceReferences = CopyStrings(evidenceReferences);
        KnowledgeReferences = CopyStrings(knowledgeReferences);
        ObservedAbsoluteDay = observedAbsoluteDay;
        DecisionAbsoluteDay = decisionAbsoluteDay;
        ExpectedWorldRevision = expectedWorldRevision;
        ExpectedKnowledgeRevision = expectedKnowledgeRevision;
    }

    private static IReadOnlyList<string> CopyStrings(IEnumerable<string> source)
    {
        List<string> result = new List<string>();
        if (source != null)
        {
            foreach (string value in source)
            {
                result.Add(value);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return new ReadOnlyCollection<string>(result);
    }
}

public sealed class WorldStatePoliticalKnowledgeSnapshot
{
    public string HolderStableId { get; }
    public PoliticalKnowledgeHolderKind HolderKind { get; }
    public string HolderPersonId { get; }
    public string HolderInstitutionId { get; }
    public string HolderFactionId { get; }
    public IReadOnlyList<WorldStatePoliticalKnowledgeObservationSnapshot> Observations { get; }

    public WorldStatePoliticalKnowledgeSnapshot(
        string holderStableId,
        PoliticalKnowledgeHolderKind holderKind,
        string holderPersonId,
        string holderInstitutionId,
        IEnumerable<WorldStatePoliticalKnowledgeObservationSnapshot> observations,
        string holderFactionId = null)
    {
        HolderStableId = holderStableId;
        HolderKind = holderKind;
        HolderPersonId = holderPersonId;
        HolderInstitutionId = holderInstitutionId;
        HolderFactionId = holderFactionId;
        List<WorldStatePoliticalKnowledgeObservationSnapshot> copied =
            new List<WorldStatePoliticalKnowledgeObservationSnapshot>();
        if (observations != null)
        {
            foreach (WorldStatePoliticalKnowledgeObservationSnapshot observation in observations)
            {
                if (observation != null)
                {
                    copied.Add(observation);
                }
            }
        }

        copied.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.IdentityKey,
            right.IdentityKey));
        Observations = copied.AsReadOnly();
    }
}

public sealed class WorldStatePoliticalKnowledgeObservationSnapshot
{
    public string IdentityKey { get; }
    public PoliticalKnowledgeFactKind FactKind { get; }
    public long ObservedAbsoluteDay { get; }
    public long ReceivedAbsoluteDay { get; }
    public PoliticalKnowledgeSource Source { get; }
    public string SourceReference { get; }
    public string SourcePersonId { get; }
    public string SourceInstitutionId { get; }
    public string StateKey { get; }

    public WorldStatePoliticalKnowledgeObservationSnapshot(
        string identityKey,
        PoliticalKnowledgeFactKind factKind,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeSource source,
        string sourceReference,
        string sourcePersonId,
        string sourceInstitutionId,
        string stateKey)
    {
        IdentityKey = identityKey;
        FactKind = factKind;
        ObservedAbsoluteDay = observedAbsoluteDay;
        ReceivedAbsoluteDay = receivedAbsoluteDay;
        Source = source;
        SourceReference = sourceReference;
        SourcePersonId = sourcePersonId;
        SourceInstitutionId = sourceInstitutionId;
        StateKey = stateKey;
    }
}

public sealed class WorldStateTheftOutcomeSnapshot
{
    public string OutcomeId { get; }
    public string PerpetratorPersonId { get; }
    public string VictimPersonId { get; }
    public int LossAmount { get; }
    public long OccurredAbsoluteDay { get; }
    public string OccurrenceKey { get; }
    public string OriginDecisionId { get; }

    public WorldStateTheftOutcomeSnapshot(
        string outcomeId,
        string perpetratorPersonId,
        string victimPersonId,
        int lossAmount,
        long occurredAbsoluteDay,
        string occurrenceKey,
        string originDecisionId)
    {
        OutcomeId = outcomeId;
        PerpetratorPersonId = perpetratorPersonId;
        VictimPersonId = victimPersonId;
        LossAmount = lossAmount;
        OccurredAbsoluteDay = occurredAbsoluteDay;
        OccurrenceKey = occurrenceKey;
        OriginDecisionId = originDecisionId;
    }
}

public sealed class WorldStateCrimeKnowledgeSnapshot
{
    public string EvaluatorPersonId { get; }
    public string OutcomeId { get; }
    public CrimeKnowledgeRole Role { get; }
    public bool KnowsLoss { get; }
    public SocialPerceivedAttributionKind PerceivedPerpetratorKind { get; }
    public string PerceivedPerpetratorPersonId { get; }
    public string PerceivedPerpetratorInstitutionId { get; }
    public string KnownInvestigatorPersonId { get; }
    public string KnownInvestigatorInstitutionId { get; }
    public SocialCognitiveBasisKind CognitiveBasisKind { get; }
    public string CognitiveBasisReference { get; }
    public string CognitiveBasisSourcePersonId { get; }
    public string CognitiveBasisSourceInstitutionId { get; }
    public long ObservedAbsoluteDay { get; }

    public WorldStateCrimeKnowledgeSnapshot(
        string evaluatorPersonId,
        string outcomeId,
        CrimeKnowledgeRole role,
        bool knowsLoss,
        SocialPerceivedAttributionKind perceivedPerpetratorKind,
        string perceivedPerpetratorPersonId,
        string perceivedPerpetratorInstitutionId,
        string knownInvestigatorPersonId,
        string knownInvestigatorInstitutionId,
        SocialCognitiveBasisKind cognitiveBasisKind,
        string cognitiveBasisReference,
        string cognitiveBasisSourcePersonId,
        string cognitiveBasisSourceInstitutionId,
        long observedAbsoluteDay)
    {
        EvaluatorPersonId = evaluatorPersonId;
        OutcomeId = outcomeId;
        Role = role;
        KnowsLoss = knowsLoss;
        PerceivedPerpetratorKind = perceivedPerpetratorKind;
        PerceivedPerpetratorPersonId = perceivedPerpetratorPersonId;
        PerceivedPerpetratorInstitutionId = perceivedPerpetratorInstitutionId;
        KnownInvestigatorPersonId = knownInvestigatorPersonId;
        KnownInvestigatorInstitutionId = knownInvestigatorInstitutionId;
        CognitiveBasisKind = cognitiveBasisKind;
        CognitiveBasisReference = cognitiveBasisReference;
        CognitiveBasisSourcePersonId = cognitiveBasisSourcePersonId;
        CognitiveBasisSourceInstitutionId = cognitiveBasisSourceInstitutionId;
        ObservedAbsoluteDay = observedAbsoluteDay;
    }
}

public sealed class WorldStateSocialReactionSnapshot
{
    public string ReactionId { get; }
    public string EvaluatorPersonId { get; }
    public string SourceDomain { get; }
    public string SourceStableId { get; }
    public SocialReactionTargetKind TargetKind { get; }
    public string TargetStableId { get; }
    public SocialPerceivedAttributionKind AttributionKind { get; }
    public string AttributionPersonId { get; }
    public string AttributionInstitutionId { get; }
    public SocialReactionValence Valence { get; }
    public SocialReactionSalience Salience { get; }
    public SocialCognitiveBasisKind CognitiveBasisKind { get; }
    public string CognitiveBasisReference { get; }
    public string CognitiveBasisSourcePersonId { get; }
    public string CognitiveBasisSourceInstitutionId { get; }
    public long CreatedAbsoluteDay { get; }
    public string SupersedesReactionId { get; }

    public WorldStateSocialReactionSnapshot(
        string reactionId,
        string evaluatorPersonId,
        string sourceDomain,
        string sourceStableId,
        SocialReactionTargetKind targetKind,
        string targetStableId,
        SocialPerceivedAttributionKind attributionKind,
        string attributionPersonId,
        string attributionInstitutionId,
        SocialReactionValence valence,
        SocialReactionSalience salience,
        SocialCognitiveBasisKind cognitiveBasisKind,
        string cognitiveBasisReference,
        string cognitiveBasisSourcePersonId,
        string cognitiveBasisSourceInstitutionId,
        long createdAbsoluteDay,
        string supersedesReactionId)
    {
        ReactionId = reactionId;
        EvaluatorPersonId = evaluatorPersonId;
        SourceDomain = sourceDomain;
        SourceStableId = sourceStableId;
        TargetKind = targetKind;
        TargetStableId = targetStableId;
        AttributionKind = attributionKind;
        AttributionPersonId = attributionPersonId;
        AttributionInstitutionId = attributionInstitutionId;
        Valence = valence;
        Salience = salience;
        CognitiveBasisKind = cognitiveBasisKind;
        CognitiveBasisReference = cognitiveBasisReference;
        CognitiveBasisSourcePersonId = cognitiveBasisSourcePersonId;
        CognitiveBasisSourceInstitutionId = cognitiveBasisSourceInstitutionId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        SupersedesReactionId = supersedesReactionId;
    }
}

public sealed class WorldStateSnapshotMetadata
{
    public long AbsoluteDay { get; }
    public WorldStateCalendarSnapshot CalendarDate { get; }

    public WorldStateSnapshotMetadata(long absoluteDay, WorldStateCalendarSnapshot calendarDate = null)
    {
        AbsoluteDay = absoluteDay;
        CalendarDate = calendarDate;
    }
}

public sealed class WorldStateCalendarSnapshot
{
    public long AbsoluteDay { get; }
    public long Year { get; }
    public int Month { get; }
    public int WeekOfMonth { get; }
    public int DayOfMonth { get; }
    public int DayOfWeek { get; }
    public long DayOfYear { get; }
    public long DaysPerMonth { get; }
    public long DaysPerYear { get; }

    public WorldStateCalendarSnapshot(SimulationDate date)
    {
        AbsoluteDay = date.AbsoluteDay;
        Year = date.Year;
        Month = date.Month;
        WeekOfMonth = date.WeekOfMonth;
        DayOfMonth = date.DayOfMonth;
        DayOfWeek = date.DayOfWeek;
        DayOfYear = date.DayOfYear;
        DaysPerMonth = date.DaysPerMonth;
        DaysPerYear = date.DaysPerYear;
    }
}

public sealed class WorldStateNpcSnapshot
{
    public string RuntimeId { get; }
    public string PersonId { get; }
    public string DefinitionId { get; }
    public string Name { get; }
    public string NpcName => Name;
    public string ResidenceSettlementRuntimeId { get; }
    public NpcLifeState LifeState { get; }
    public NpcInjurySeverity InjurySeverity { get; }
    public string CurrentLocationRuntimeId { get; }
    public string CurrentCityRuntimeId { get; }
    public string DestinationLocationRuntimeId { get; }
    public string DestinationCityRuntimeId { get; }
    public bool IsTraveling { get; }
    public string TravelRouteRuntimeId { get; }
    public int RemainingTravelDays { get; }
    public int TravelDaysRemaining => RemainingTravelDays;
    public string ActiveTravelPartyId { get; }
    public float MoneyBalance { get; }
    public float Money => MoneyBalance;
    public string ActiveExpeditionId { get; }
    public IReadOnlyList<WorldStateInventoryStackSnapshot> Inventory { get; }
    public IReadOnlyList<string> StatusNames { get; }
    public IReadOnlyList<string> Statuses => StatusNames;
    public WorldStateActionSnapshot CurrentAction { get; }
    public WorldStateMerchantTradePlanSnapshot MerchantTradePlan { get; }
    public WorldStateMerchantTradePlanSnapshot TradePlan => MerchantTradePlan;

    public WorldStateNpcSnapshot(
        string runtimeId,
        string definitionId,
        string residenceSettlementRuntimeId,
        NpcLifeState lifeState,
        NpcInjurySeverity injurySeverity,
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        bool isTraveling,
        string travelRouteRuntimeId,
        int remainingTravelDays,
        string activeTravelPartyId,
        float moneyBalance,
        string activeExpeditionId,
        IEnumerable<WorldStateInventoryStackSnapshot> inventory)
        : this(
            runtimeId,
            definitionId,
            null,
            residenceSettlementRuntimeId,
            lifeState,
            injurySeverity,
            currentLocationRuntimeId,
            currentCityRuntimeId,
            destinationLocationRuntimeId,
            destinationCityRuntimeId,
            isTraveling,
            travelRouteRuntimeId,
            remainingTravelDays,
            activeTravelPartyId,
            moneyBalance,
            activeExpeditionId,
            inventory,
            null,
            null,
            null,
            null)
    {
    }

    public WorldStateNpcSnapshot(
        string runtimeId,
        string definitionId,
        string name,
        string residenceSettlementRuntimeId,
        NpcLifeState lifeState,
        NpcInjurySeverity injurySeverity,
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        bool isTraveling,
        string travelRouteRuntimeId,
        int remainingTravelDays,
        string activeTravelPartyId,
        float moneyBalance,
        string activeExpeditionId,
        IEnumerable<WorldStateInventoryStackSnapshot> inventory,
        IEnumerable<string> statusNames = null,
        WorldStateActionSnapshot currentAction = null,
        WorldStateMerchantTradePlanSnapshot merchantTradePlan = null,
        string personId = null)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        Name = name;
        PersonId = personId;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        IsTraveling = isTraveling;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        RemainingTravelDays = remainingTravelDays;
        ActiveTravelPartyId = activeTravelPartyId;
        MoneyBalance = moneyBalance;
        ActiveExpeditionId = activeExpeditionId;
        Inventory = SnapshotCollections.CopySorted(inventory, stack => stack?.ItemDefinitionId);
        StatusNames = SnapshotCollections.CopySorted(statusNames, status => status);
        CurrentAction = currentAction;
        MerchantTradePlan = merchantTradePlan;
    }
}

public sealed class WorldStateActionSnapshot
{
    public string DefinitionId { get; }
    public string ActionName { get; }
    public NpcActionCategory Category { get; }
    public NpcActionType Type { get; }
    public string TargetNpcRuntimeId { get; }
    public string TargetCityRuntimeId { get; }
    public string TargetItemDefinitionId { get; }
    public int Amount { get; }
    public float ExpectedUnitPrice { get; }
    public float SuccessChanceMultiplier { get; }
    public NpcTravelReason TravelReason { get; }
    public float ExpectedNetValue { get; }
    public string OriginDecisionId { get; }

    public WorldStateActionSnapshot(
        string definitionId,
        string actionName,
        NpcActionCategory category,
        NpcActionType type,
        string targetNpcRuntimeId,
        string targetCityRuntimeId,
        string targetItemDefinitionId,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId)
    {
        DefinitionId = definitionId;
        ActionName = actionName;
        Category = category;
        Type = type;
        TargetNpcRuntimeId = targetNpcRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        TargetItemDefinitionId = targetItemDefinitionId;
        Amount = amount;
        ExpectedUnitPrice = expectedUnitPrice;
        SuccessChanceMultiplier = successChanceMultiplier;
        TravelReason = travelReason;
        ExpectedNetValue = expectedNetValue;
        OriginDecisionId = originDecisionId;
    }
}

public sealed class WorldStateMerchantTradePlanSnapshot
{
    public bool HasData { get; }
    public bool IsActive { get; }
    public string ItemDefinitionId { get; }
    public string OriginCityRuntimeId { get; }
    public string TargetCityRuntimeId { get; }
    public int PlannedAmount { get; }
    public int RemainingAmount { get; }
    public float PurchasePricePerItem { get; }
    public int WaitDaysAtDestination { get; }
    public int PendingTravelDays { get; }
    public string OriginDecisionId { get; }

    public WorldStateMerchantTradePlanSnapshot(
        bool hasData,
        bool isActive,
        string itemDefinitionId,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int remainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId)
    {
        HasData = hasData;
        IsActive = isActive;
        ItemDefinitionId = itemDefinitionId;
        OriginCityRuntimeId = originCityRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        PlannedAmount = plannedAmount;
        RemainingAmount = remainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
    }
}

public sealed class WorldStateInventoryStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public float AverageUnitCost { get; }

    public WorldStateInventoryStackSnapshot(string itemDefinitionId, int amount, float averageUnitCost)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        AverageUnitCost = averageUnitCost;
    }
}

public sealed class WorldStateCitySnapshot
{
    public string RuntimeId { get; }
    public string SettlementRuntimeId => RuntimeId;
    public string DefinitionId { get; }
    public string CityName { get; }
    public string SettlementName => CityName;
    public string LocationRuntimeId { get; }
    public int CurrentPopulation { get; }
    public long PopulationRevision { get; }
    public long Revision => PopulationRevision;
    public int NamedResidentCount { get; }
    public int NamedLivingResidentCount => NamedResidentCount;
    public int NamedPresentCount { get; }
    public string MarketCounterpartyRuntimeId { get; }
    public MarketLiquidityMode MarketLiquidityMode { get; }
    public float MarketBalance { get; }
    public IReadOnlyList<string> ResidentNpcRuntimeIds { get; }
    public IReadOnlyList<WorldStateMarketStackSnapshot> MarketStock { get; }

    public WorldStateCitySnapshot(
        string runtimeId,
        string definitionId,
        string locationRuntimeId,
        int currentPopulation,
        string marketCounterpartyRuntimeId,
        MarketLiquidityMode marketLiquidityMode,
        float marketBalance,
        IEnumerable<string> residentNpcRuntimeIds,
        IEnumerable<WorldStateMarketStackSnapshot> marketStock,
        string cityName = null,
        long populationRevision = 0L,
        int namedResidentCount = 0,
        int namedPresentCount = 0)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        CityName = cityName;
        LocationRuntimeId = locationRuntimeId;
        CurrentPopulation = currentPopulation;
        PopulationRevision = populationRevision;
        NamedResidentCount = namedResidentCount;
        NamedPresentCount = namedPresentCount;
        MarketCounterpartyRuntimeId = marketCounterpartyRuntimeId;
        MarketLiquidityMode = marketLiquidityMode;
        MarketBalance = marketBalance;
        ResidentNpcRuntimeIds = SnapshotCollections.CopySorted(residentNpcRuntimeIds, resident => resident);
        MarketStock = SnapshotCollections.CopySorted(marketStock, stock => stock?.ItemDefinitionId);
    }
}

public sealed class WorldStateMarketStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public int DesiredAmount { get; }
    public float CurrentPrice { get; }

    public WorldStateMarketStackSnapshot(string itemDefinitionId, int amount, int desiredAmount, float currentPrice)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        DesiredAmount = desiredAmount;
        CurrentPrice = currentPrice;
    }
}

public sealed class WorldStateSpatialSnapshot
{
    public long? AuthorityRevision { get; }
    public string CoordinateConventionVersion { get; }
    public string CoordinateCanonicalOrder { get; }
    public WorldStateSpatialScaleContextSnapshot ScaleContext { get; }
    public IReadOnlyList<WorldStateHexSnapshot> Hexes { get; }
    public IReadOnlyList<WorldStateAnchoredLocationSnapshot> AnchoredLocations { get; }
    public IReadOnlyList<WorldStateCrossingSnapshot> Crossings { get; }
    public IReadOnlyList<WorldStateSpatialTopologyBindingSnapshot> TopologyBindings { get; }
    public IReadOnlyList<WorldStateLocationSnapshot> Locations { get; }
    public IReadOnlyList<WorldStateRouteSnapshot> Routes { get; }

    public WorldStateSpatialSnapshot(
        IEnumerable<WorldStateLocationSnapshot> locations = null,
        IEnumerable<WorldStateRouteSnapshot> routes = null,
        IEnumerable<WorldStateHexSnapshot> hexes = null,
        IEnumerable<WorldStateAnchoredLocationSnapshot> anchoredLocations = null,
        long? authorityRevision = null,
        IEnumerable<WorldStateSpatialTopologyBindingSnapshot> topologyBindings = null,
        string coordinateConventionVersion = null,
        string coordinateCanonicalOrder = null,
        WorldStateSpatialScaleContextSnapshot scaleContext = null,
        IEnumerable<WorldStateCrossingSnapshot> crossings = null)
    {
        AuthorityRevision = authorityRevision;
        CoordinateConventionVersion = coordinateConventionVersion;
        CoordinateCanonicalOrder = coordinateCanonicalOrder;
        ScaleContext = scaleContext;
        Hexes = SnapshotCollections.CopySorted(hexes, hex => hex?.HexId);
        AnchoredLocations = SnapshotCollections.CopySorted(anchoredLocations, location => location?.LocationId);
        Crossings = SnapshotCollections.CopySorted(crossings, crossing => crossing?.CrossingId);
        TopologyBindings = SnapshotCollections.CopySorted(topologyBindings, binding => binding?.StableKey);
        Locations = SnapshotCollections.CopySorted(locations, location => location?.RuntimeId);
        Routes = SnapshotCollections.CopySorted(routes, route => route?.RuntimeId);
    }
}

public sealed class WorldStateCrossingSnapshot
{
    public string CrossingId { get; }
    public string FirstHexId { get; }
    public string SecondHexId { get; }
    public string AnchorHexId { get; }

    public WorldStateCrossingSnapshot(
        string crossingId,
        string firstHexId,
        string secondHexId,
        string anchorHexId)
    {
        CrossingId = crossingId;
        FirstHexId = firstHexId;
        SecondHexId = secondHexId;
        AnchorHexId = anchorHexId;
    }
}

public sealed class WorldStateSpatialTopologyBindingSnapshot
{
    public LocalTopologyOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string LocationId { get; }
    public string StableKey => WorldStateSnapshotValue.OwnerKey(OwnerKind, OwnerRuntimeId);

    public WorldStateSpatialTopologyBindingSnapshot(
        LocalTopologyOwnerKind ownerKind,
        string ownerRuntimeId,
        string locationId)
    {
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        LocationId = locationId;
    }
}

public sealed class WorldStateHexSnapshot
{
    public string HexId { get; }
    public int? Q { get; }
    public int? R { get; }
    public string TerrainDefinitionId { get; }
    public string AuthoredRevisionToken { get; }
    public bool HasGeographicFacts => Q.HasValue || R.HasValue
        || TerrainDefinitionId != null || AuthoredRevisionToken != null;

    public WorldStateHexSnapshot(
        string hexId,
        int? q = null,
        int? r = null,
        string terrainDefinitionId = null,
        string authoredRevisionToken = null)
    {
        HexId = hexId;
        Q = q;
        R = r;
        TerrainDefinitionId = terrainDefinitionId;
        AuthoredRevisionToken = authoredRevisionToken;
    }
}

public sealed class WorldStateSpatialScaleContextSnapshot
{
    public string ResolvedConventionId { get; }
    public string SourceIdentity { get; }
    public string SourceVersion { get; }
    public decimal? DistancePerNeighborStep { get; }
    public string Unit { get; }

    public WorldStateSpatialScaleContextSnapshot(
        string resolvedConventionId,
        string sourceIdentity,
        string sourceVersion,
        decimal? distancePerNeighborStep,
        string unit)
    {
        ResolvedConventionId = resolvedConventionId;
        SourceIdentity = sourceIdentity;
        SourceVersion = sourceVersion;
        DistancePerNeighborStep = distancePerNeighborStep;
        Unit = unit;
    }
}

public sealed class WorldStateAnchoredLocationSnapshot
{
    public string LocationId { get; }
    public string AnchorHexId { get; }

    public WorldStateAnchoredLocationSnapshot(string locationId, string anchorHexId)
    {
        LocationId = locationId;
        AnchorHexId = anchorHexId;
    }
}

public sealed class WorldStateLocationSnapshot
{
    public string RuntimeId { get; }

    public WorldStateLocationSnapshot(string runtimeId)
    {
        RuntimeId = runtimeId;
    }
}

public sealed class WorldStateRouteSnapshot
{
    public string RuntimeId { get; }
    public string OriginRuntimeId { get; }
    public string DestinationRuntimeId { get; }
    public int TravelDays { get; }

    public WorldStateRouteSnapshot(string runtimeId, string originRuntimeId, string destinationRuntimeId, int travelDays)
    {
        RuntimeId = runtimeId;
        OriginRuntimeId = originRuntimeId;
        DestinationRuntimeId = destinationRuntimeId;
        TravelDays = travelDays;
    }
}

public sealed class WorldStateSiteSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public string LocationRuntimeId { get; }
    public ExplorableSiteKind SiteKind { get; }

    public WorldStateSiteSnapshot(string runtimeId, string definitionId, string locationRuntimeId, ExplorableSiteKind siteKind)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        LocationRuntimeId = locationRuntimeId;
        SiteKind = siteKind;
    }
}

public sealed class WorldStateExpeditionSnapshot
{
    public string ExpeditionId { get; }
    public ExpeditionState State { get; }
    public string TargetSiteRuntimeId { get; }
    public string OriginLocationRuntimeId { get; }
    public string TargetLocationRuntimeId { get; }
    public string OutboundRouteRuntimeId { get; }
    public string OriginDecisionId { get; }
    public string TravelPartyId { get; }
    public string CurrentLocalPlaceRuntimeId { get; }
    public int ExplorationProgress { get; }
    public int ExplorationProgressRequired { get; }
    public IReadOnlyList<string> MemberRuntimeIds { get; }
    public IReadOnlyList<string> PerformerRuntimeIds { get; }
    public IReadOnlyList<string> SupportRuntimeIds { get; }
    public IReadOnlyList<string> VisitedLocalPlaceRuntimeIds { get; }
    public IReadOnlyList<string> ObservedLocalConnectionRuntimeIds { get; }
    public ExpeditionObjectiveType ObjectiveType { get; }
    public string ObjectiveTargetItemDefinitionId { get; }
    public string ObjectiveTargetNotableItemRuntimeId { get; }
    public string ObjectiveTargetOppositionRuntimeId { get; }
    public bool ObjectiveCompleted { get; }
    public bool ObjectiveAllowsContinueAfterCompletion { get; }

    public WorldStateExpeditionSnapshot(
        string expeditionId,
        ExpeditionState state,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string outboundRouteRuntimeId,
        string originDecisionId,
        string travelPartyId,
        string currentLocalPlaceRuntimeId,
        int explorationProgress,
        int explorationProgressRequired,
        IEnumerable<string> memberRuntimeIds,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        IEnumerable<string> visitedLocalPlaceRuntimeIds,
        IEnumerable<string> observedLocalConnectionRuntimeIds,
        ExpeditionObjectiveType objectiveType,
        string objectiveTargetItemDefinitionId,
        string objectiveTargetNotableItemRuntimeId,
        string objectiveTargetOppositionRuntimeId,
        bool objectiveCompleted,
        bool objectiveAllowsContinueAfterCompletion)
    {
        ExpeditionId = expeditionId;
        State = state;
        TargetSiteRuntimeId = targetSiteRuntimeId;
        OriginLocationRuntimeId = originLocationRuntimeId;
        TargetLocationRuntimeId = targetLocationRuntimeId;
        OutboundRouteRuntimeId = outboundRouteRuntimeId;
        OriginDecisionId = originDecisionId;
        TravelPartyId = travelPartyId;
        CurrentLocalPlaceRuntimeId = currentLocalPlaceRuntimeId;
        ExplorationProgress = explorationProgress;
        ExplorationProgressRequired = explorationProgressRequired;
        MemberRuntimeIds = SnapshotCollections.CopySorted(memberRuntimeIds, value => value);
        PerformerRuntimeIds = SnapshotCollections.CopySorted(performerRuntimeIds, value => value);
        SupportRuntimeIds = SnapshotCollections.CopySorted(supportRuntimeIds, value => value);
        VisitedLocalPlaceRuntimeIds = SnapshotCollections.CopySorted(visitedLocalPlaceRuntimeIds, value => value);
        ObservedLocalConnectionRuntimeIds = SnapshotCollections.CopySorted(observedLocalConnectionRuntimeIds, value => value);
        ObjectiveType = objectiveType;
        ObjectiveTargetItemDefinitionId = objectiveTargetItemDefinitionId;
        ObjectiveTargetNotableItemRuntimeId = objectiveTargetNotableItemRuntimeId;
        ObjectiveTargetOppositionRuntimeId = objectiveTargetOppositionRuntimeId;
        ObjectiveCompleted = objectiveCompleted;
        ObjectiveAllowsContinueAfterCompletion = objectiveAllowsContinueAfterCompletion;
    }
}

public sealed class WorldStatePlaceContentSnapshot
{
    public PlaceContentOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string MacroLocationRuntimeId { get; }
    public string TopologyOwnerRuntimeId { get; }
    public PlaceSiteState SiteState { get; }
    public PlaceAccessState AccessState { get; }
    public string ControllerRuntimeId { get; }
    public IReadOnlyList<WorldStatePlaceStackSnapshot> Stacks { get; }
    public IReadOnlyList<WorldStatePlaceOppositionSnapshot> Oppositions { get; }

    public string StableKey => WorldStateSnapshotValue.OwnerKey(OwnerKind, OwnerRuntimeId);

    public WorldStatePlaceContentSnapshot(
        PlaceContentOwnerKind ownerKind,
        string ownerRuntimeId,
        string macroLocationRuntimeId,
        string topologyOwnerRuntimeId,
        PlaceSiteState siteState,
        PlaceAccessState accessState,
        string controllerRuntimeId,
        IEnumerable<WorldStatePlaceStackSnapshot> stacks,
        IEnumerable<WorldStatePlaceOppositionSnapshot> oppositions)
    {
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SiteState = siteState;
        AccessState = accessState;
        ControllerRuntimeId = controllerRuntimeId;
        Stacks = SnapshotCollections.CopySorted(stacks, stack => stack?.ItemDefinitionId);
        Oppositions = SnapshotCollections.CopySorted(oppositions, opposition => opposition?.RuntimeId);
    }
}

public sealed class WorldStatePlaceStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public PlaceContentPersistencePolicy PersistencePolicy { get; }
    public int DecayPerDay { get; }
    public float AverageUnitCost { get; }

    public WorldStatePlaceStackSnapshot(
        string itemDefinitionId,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        int decayPerDay,
        float averageUnitCost)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        PersistencePolicy = persistencePolicy;
        DecayPerDay = decayPerDay;
        AverageUnitCost = averageUnitCost;
    }
}

public sealed class WorldStatePlaceOppositionSnapshot
{
    public string RuntimeId { get; }
    public bool IsActive { get; }
    public bool IsResolved { get; }
    public string OppositionSideId { get; }
    public IReadOnlyList<string> NamedParticipantRuntimeIds { get; }
    public IReadOnlyList<string> AggregateParticipantSourceIds { get; }

    public WorldStatePlaceOppositionSnapshot(
        string runtimeId,
        bool isActive,
        bool isResolved,
        string oppositionSideId,
        IEnumerable<string> namedParticipantRuntimeIds,
        IEnumerable<string> aggregateParticipantSourceIds)
    {
        RuntimeId = runtimeId;
        IsActive = isActive;
        IsResolved = isResolved;
        OppositionSideId = oppositionSideId;
        NamedParticipantRuntimeIds = SnapshotCollections.CopySorted(namedParticipantRuntimeIds, value => value);
        AggregateParticipantSourceIds = SnapshotCollections.CopySorted(aggregateParticipantSourceIds, value => value);
    }
}

public sealed class WorldStateNotableItemSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public bool IsPresent { get; }
    public NotableItemCustodyKind? CustodyKind { get; }
    public PlaceContentOwnerKind? OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string OwnerMacroLocationRuntimeId { get; }
    public string OwnerTopologyRuntimeId { get; }
    public string CustodianNpcRuntimeId { get; }

    public string CustodyKey
    {
        get
        {
            if (IsPresent == false || CustodyKind.HasValue == false)
            {
                return null;
            }

            if (CustodyKind.Value == NotableItemCustodyKind.Npc)
            {
                return "Npc:" + CustodianNpcRuntimeId;
            }

            return "Place:" + WorldStateSnapshotValue.OwnerKey(OwnerKind.Value, OwnerRuntimeId);
        }
    }

    public WorldStateNotableItemSnapshot(
        string runtimeId,
        string definitionId,
        bool isPresent,
        NotableItemCustodyKind? custodyKind,
        PlaceContentOwnerKind? ownerKind,
        string ownerRuntimeId,
        string ownerMacroLocationRuntimeId,
        string ownerTopologyRuntimeId,
        string custodianNpcRuntimeId)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        IsPresent = isPresent;
        CustodyKind = custodyKind;
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        OwnerMacroLocationRuntimeId = ownerMacroLocationRuntimeId;
        OwnerTopologyRuntimeId = ownerTopologyRuntimeId;
        CustodianNpcRuntimeId = custodianNpcRuntimeId;
    }
}

public sealed class WorldStateLocalTopologySnapshot
{
    public LocalTopologyOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string MacroLocationRuntimeId { get; }
    public LocalTopologyPublicationState PublicationState { get; }
    public IReadOnlyList<WorldStateLocalPlaceSnapshot> Places { get; }
    public IReadOnlyList<WorldStateLocalConnectionSnapshot> Connections { get; }

    public string StableKey => WorldStateSnapshotValue.OwnerKey(OwnerKind, OwnerRuntimeId);

    public WorldStateLocalTopologySnapshot(
        LocalTopologyOwnerKind ownerKind,
        string ownerRuntimeId,
        string macroLocationRuntimeId,
        LocalTopologyPublicationState publicationState,
        IEnumerable<WorldStateLocalPlaceSnapshot> places,
        IEnumerable<WorldStateLocalConnectionSnapshot> connections)
    {
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        PublicationState = publicationState;
        Places = SnapshotCollections.CopySorted(places, place => place?.RuntimeId);
        Connections = SnapshotCollections.CopySorted(connections, connection => connection?.RuntimeId);
    }
}

public sealed class WorldStateLocalPlaceSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public string ParentRuntimeId { get; }
    public bool IsEntryPoint { get; }

    public WorldStateLocalPlaceSnapshot(string runtimeId, string definitionId, string parentRuntimeId, bool isEntryPoint)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        ParentRuntimeId = parentRuntimeId;
        IsEntryPoint = isEntryPoint;
    }
}

public sealed class WorldStateLocalConnectionSnapshot
{
    public string RuntimeId { get; }
    public string OriginRuntimeId { get; }
    public string DestinationRuntimeId { get; }
    public float TraversalCost { get; }
    public string ConnectionTypeDefinitionId { get; }

    public WorldStateLocalConnectionSnapshot(
        string runtimeId,
        string originRuntimeId,
        string destinationRuntimeId,
        float traversalCost,
        string connectionTypeDefinitionId)
    {
        RuntimeId = runtimeId;
        OriginRuntimeId = originRuntimeId;
        DestinationRuntimeId = destinationRuntimeId;
        TraversalCost = traversalCost;
        ConnectionTypeDefinitionId = connectionTypeDefinitionId;
    }
}

public static class WorldStateSnapshotBuilder
{
    public static WorldStateSnapshot Capture(WorldStateSnapshotContext context)
    {
        return BuildSnapshot(context);
    }

    public static WorldStateSnapshot BuildSnapshot(WorldStateSnapshotContext context)
    {
        context = context ?? new WorldStateSnapshotContext();

        List<NpcRuntime> knownNpcs = SnapshotCollections.Materialize(context.Npcs);
        List<WorldStatePersonSnapshot> persons = BuildPersonSnapshots(
            context.PersonStore,
            context.SimulationTime,
            context.Calendar);
        List<WorldStateParentageSnapshot> parentages = BuildParentageSnapshots(context.Parentages);
        List<WorldStatePoliticalClaimSnapshot> politicalClaims =
            BuildPoliticalClaimSnapshots(context.PoliticalClaims);
        List<WorldStatePoliticalClaimRecognitionSnapshot> politicalClaimRecognitions =
            BuildPoliticalClaimRecognitionSnapshots(context.PoliticalClaimRecognitions);
        List<WorldStateFactionSnapshot> factions = BuildFactionSnapshots(context.Factions);
        List<WorldStateFactionAffiliationSnapshot> factionAffiliations =
            BuildFactionAffiliationSnapshots(context.FactionAffiliations);
        List<WorldStatePoliticalSupportSnapshot> politicalSupports =
            BuildPoliticalSupportSnapshots(context.PoliticalSupports);
        List<WorldStatePoliticalDecisionSnapshot> politicalDecisions =
            BuildPoliticalDecisionSnapshots(context.PoliticalDecisions);
        List<WorldStatePoliticalKnowledgeSnapshot> politicalKnowledge =
            BuildPoliticalKnowledgeSnapshots(context.PoliticalKnowledgeRuntimes);
        List<WorldStateTheftOutcomeSnapshot> theftOutcomes =
            BuildTheftOutcomeSnapshots(context.CrimeSocialAppraisal);
        List<WorldStateCrimeKnowledgeSnapshot> crimeKnowledge =
            BuildCrimeKnowledgeSnapshots(context.CrimeSocialAppraisal);
        List<WorldStateSocialReactionSnapshot> socialReactions =
            BuildSocialReactionSnapshots(context.CrimeSocialAppraisal);
        List<WorldStateExpeditionSnapshot> expeditions = BuildExpeditionSnapshots(context.ExpeditionStore);
        WorldStateCalendarSnapshot calendarDate = null;
        if (context.Calendar != null && context.SimulationTime != null)
        {
            calendarDate = new WorldStateCalendarSnapshot(context.Calendar.GetDate(context.SimulationTime.AbsoluteDay));
        }

        return new WorldStateSnapshot(
            context.SimulationTime != null ? context.SimulationTime.AbsoluteDay : 0L,
            BuildNpcSnapshots(knownNpcs, expeditions),
            BuildCitySnapshots(context.Cities, knownNpcs, context.PersonStore?.Persons),
            BuildSpatialSnapshot(
                context.SpatialNetwork,
                context.SpatialAuthorityStore
                    ?? context.ArmedForceSpatialStateStore?.SpatialAuthorityStore
                    ?? context.BattleStore?.SpatialAuthorityStore),
            BuildSiteSnapshots(context.ExplorableSiteStore),
            expeditions,
            BuildPlaceContentSnapshots(context.PlaceContentStore),
            BuildNotableItemSnapshots(context.PlaceContentStore),
            BuildLocalTopologySnapshots(
                context.LocalTopologyStore
                    ?? context.ArmedForceSpatialStateStore?.LocalTopologyStore
                    ?? context.BattleStore?.LocalTopologyStore),
            calendarDate,
            persons,
            parentages,
            BuildPropertyOwnershipSnapshots(context.PropertyOwnershipStore),
            BuildEstateSnapshots(context.EstateStore),
            BuildPropertyTransferSnapshots(context.PropertyOwnershipStore),
            politicalClaims,
            context.InstitutionIds ?? BuildInstitutionIds(context.InstitutionStore),
            context.OfficeIds ?? BuildOfficeIds(context.OfficeStore),
            context.PropertyIds ?? BuildPropertyIds(context.PropertyOwnershipStore),
            factions,
            factionAffiliations,
            politicalSupports,
            politicalDecisions,
            politicalKnowledge,
            context.PoliticalKnowledgeRevision ?? 0L,
            context.PoliticalKnowledgeRuntimes != null
                || context.PoliticalKnowledgeRevision.HasValue,
            context.OfficeInstitutionIds ?? BuildOfficeInstitutionIds(context.OfficeStore),
            politicalClaimRecognitions,
            theftOutcomes,
            crimeKnowledge,
            socialReactions,
            BuildArmedForceSnapshots(context.ArmedForceStore),
            BuildArmedForceContingentSnapshots(context.ArmedForceStore),
            BuildArmedForcePersonReferenceSnapshots(context.ArmedForceStore),
            context.ArmedForceStore == null ? (long?)null : context.ArmedForceStore.Revision,
            BuildConflictSnapshots(context.ConflictStore),
            BuildConflictSideSnapshots(context.ConflictStore),
            BuildConflictParticipantBindingSnapshots(context.ConflictStore),
            context.ConflictStore == null ? (long?)null : context.ConflictStore.Revision,
            BuildWarSnapshots(context.WarStore),
            BuildWarSideSnapshots(context.WarStore),
            BuildWarParticipantBindingSnapshots(context.WarStore),
            context.WarStore == null ? (long?)null : context.WarStore.Revision,
            BuildBattleSnapshots(context.BattleStore),
            BuildBattleSideSnapshots(context.BattleStore),
            BuildBattleParticipantBindingSnapshots(context.BattleStore),
            context.BattleStore == null ? (long?)null : context.BattleStore.Revision,
            BuildArmedForcePositionSnapshots(context.ArmedForceSpatialStateStore),
            context.ArmedForceSpatialStateStore == null
                ? (long?)null
                : context.ArmedForceSpatialStateStore.Revision,
            BuildContingentManpowerSnapshots(context.ContingentManpowerStateStore),
            context.ContingentManpowerStateStore == null
                ? (long?)null
                : context.ContingentManpowerStateStore.Revision);
    }

    private static List<WorldStateConflictSnapshot> BuildConflictSnapshots(PersistentConflictStore store)
    {
        List<WorldStateConflictSnapshot> result = new List<WorldStateConflictSnapshot>();
        if (store == null) return result;
        foreach (PersistentConflictRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            result.Add(new WorldStateConflictSnapshot(
                record.Id.Value,
                record.CreatedAbsoluteDay,
                record.LifecycleState,
                record.EndedAbsoluteDay));
        }
        return result;
    }

    private static List<WorldStateConflictSideSnapshot> BuildConflictSideSnapshots(PersistentConflictStore store)
    {
        List<WorldStateConflictSideSnapshot> result = new List<WorldStateConflictSideSnapshot>();
        if (store == null) return result;
        foreach (PersistentConflictRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (ConflictStateSide side in record.Sides)
            {
                if (side?.SideId == null) continue;
                result.Add(new WorldStateConflictSideSnapshot(record.Id.Value, side.SideId.Value, side.DisplayName));
            }
        }
        return result;
    }

    private static List<WorldStateConflictParticipantBindingSnapshot> BuildConflictParticipantBindingSnapshots(PersistentConflictStore store)
    {
        List<WorldStateConflictParticipantBindingSnapshot> result = new List<WorldStateConflictParticipantBindingSnapshot>();
        if (store == null) return result;
        foreach (PersistentConflictRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (ConflictParticipantBinding binding in record.ParticipantBindings)
            {
                if (binding?.BindingId == null || binding.SideId == null || binding.ArmedForceId == null) continue;
                result.Add(new WorldStateConflictParticipantBindingSnapshot(
                    record.Id.Value,
                    binding.BindingId.Value,
                    binding.SideId.Value,
                    binding.ArmedForceId.Value));
            }
        }
        return result;
    }

    private static List<WorldStateWarSnapshot> BuildWarSnapshots(PersistentWarStore store)
    {
        List<WorldStateWarSnapshot> result = new List<WorldStateWarSnapshot>();
        if (store == null) return result;
        foreach (PersistentWarRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            result.Add(new WorldStateWarSnapshot(
                record.Id.Value,
                record.CreatedAbsoluteDay,
                record.LifecycleState,
                record.EndedAbsoluteDay,
                record.ConflictId?.Value));
        }
        return result;
    }

    private static List<WorldStateWarSideSnapshot> BuildWarSideSnapshots(PersistentWarStore store)
    {
        List<WorldStateWarSideSnapshot> result = new List<WorldStateWarSideSnapshot>();
        if (store == null) return result;
        foreach (PersistentWarRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (WarStateSide side in record.Sides)
            {
                if (side?.SideId == null) continue;
                result.Add(new WorldStateWarSideSnapshot(record.Id.Value, side.SideId.Value, side.DisplayName));
            }
        }
        return result;
    }

    private static List<WorldStateWarParticipantBindingSnapshot> BuildWarParticipantBindingSnapshots(PersistentWarStore store)
    {
        List<WorldStateWarParticipantBindingSnapshot> result = new List<WorldStateWarParticipantBindingSnapshot>();
        if (store == null) return result;
        foreach (PersistentWarRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (WarParticipantBinding binding in record.ParticipantBindings)
            {
                if (binding?.BindingId == null || binding.SideId == null || binding.ArmedForceId == null) continue;
                result.Add(new WorldStateWarParticipantBindingSnapshot(
                    record.Id.Value,
                    binding.BindingId.Value,
                    binding.SideId.Value,
                    binding.ArmedForceId.Value));
            }
        }
        return result;
    }

    private static List<WorldStateBattleSnapshot> BuildBattleSnapshots(PersistentBattleStore store)
    {
        List<WorldStateBattleSnapshot> result = new List<WorldStateBattleSnapshot>();
        if (store == null) return result;
        foreach (PersistentBattleRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            result.Add(new WorldStateBattleSnapshot(
                record.Id.Value,
                record.CreatedAbsoluteDay,
                record.StartedAbsoluteDay,
                record.LifecycleState,
                record.ConflictId?.Value,
                record.WarId?.Value,
                record.LocationReference,
                terminalOutcome: record.TerminalOutcome == null
                    ? null
                    : new WorldStateBattleTerminalOutcomeSnapshot(record.TerminalOutcome)));
        }
        return result;
    }

    private static List<WorldStateBattleSideSnapshot> BuildBattleSideSnapshots(PersistentBattleStore store)
    {
        List<WorldStateBattleSideSnapshot> result = new List<WorldStateBattleSideSnapshot>();
        if (store == null) return result;
        foreach (PersistentBattleRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (BattleStateSide side in record.Sides)
            {
                if (side?.SideId == null) continue;
                result.Add(new WorldStateBattleSideSnapshot(record.Id.Value, side.SideId.Value, side.DisplayName));
            }
        }
        return result;
    }

    private static List<WorldStateBattleParticipantBindingSnapshot> BuildBattleParticipantBindingSnapshots(PersistentBattleStore store)
    {
        List<WorldStateBattleParticipantBindingSnapshot> result = new List<WorldStateBattleParticipantBindingSnapshot>();
        if (store == null) return result;
        foreach (PersistentBattleRecord record in store.Records)
        {
            if (record?.Id == null) continue;
            foreach (BattleParticipantBinding binding in record.ParticipantBindings)
            {
                if (binding?.BindingId == null || binding.SideId == null || binding.ArmedForceId == null) continue;
                result.Add(new WorldStateBattleParticipantBindingSnapshot(
                    record.Id.Value,
                    binding.BindingId.Value,
                    binding.SideId.Value,
                    binding.ArmedForceId.Value));
            }
        }
        return result;
    }

    private static List<WorldStateArmedForceSnapshot> BuildArmedForceSnapshots(
        ArmedForceStore store)
    {
        List<WorldStateArmedForceSnapshot> result = new List<WorldStateArmedForceSnapshot>();
        if (store == null) return result;

        foreach (ArmedForceRecord force in store.Forces)
        {
            if (force == null || force.Id == null) continue;
            result.Add(new WorldStateArmedForceSnapshot(
                force.Id.Value,
                force.DisplayName,
                force.CreatedAbsoluteDay,
                force.LifecycleState,
                force.TerminatedAbsoluteDay,
                force.ParentForceId?.Value,
                force.IsDetached,
                force.OperationalLocationReference,
                force.CommanderPersonId?.Value));
        }

        return result;
    }

    private static List<WorldStateArmedForceContingentSnapshot> BuildArmedForceContingentSnapshots(
        ArmedForceStore store)
    {
        List<WorldStateArmedForceContingentSnapshot> result =
            new List<WorldStateArmedForceContingentSnapshot>();
        if (store == null) return result;

        foreach (ContingentRecord contingent in store.Contingents)
        {
            if (contingent == null || contingent.Id == null || contingent.ForceId == null)
            {
                continue;
            }

            List<WorldStateArmedForceCharacteristicSnapshot> characteristics =
                new List<WorldStateArmedForceCharacteristicSnapshot>();
            foreach (ArmedForceCharacteristic characteristic in contingent.Characteristics)
            {
                if (characteristic != null)
                {
                    characteristics.Add(new WorldStateArmedForceCharacteristicSnapshot(
                        characteristic.Key,
                        characteristic.Value));
                }
            }

            result.Add(new WorldStateArmedForceContingentSnapshot(
                contingent.Id.Value,
                contingent.ForceId.Value,
                contingent.Amount,
                contingent.Origin?.Domain,
                contingent.Origin?.Value,
                contingent.ServiceType,
                characteristics));
        }

        return result;
    }

    private static List<WorldStateArmedForcePersonReferenceSnapshot> BuildArmedForcePersonReferenceSnapshots(
        ArmedForceStore store)
    {
        List<WorldStateArmedForcePersonReferenceSnapshot> result =
            new List<WorldStateArmedForcePersonReferenceSnapshot>();
        if (store == null) return result;

        foreach (ArmedForcePersonReference reference in store.RelevantPersons)
        {
            if (reference == null || reference.Id == null || reference.ForceId == null || reference.PersonId == null)
            {
                continue;
            }

            result.Add(new WorldStateArmedForcePersonReferenceSnapshot(
                reference.Id.Value,
                reference.ForceId.Value,
                reference.PersonId.Value,
                reference.RoleKey));
        }

        return result;
    }

    private static List<WorldStateArmedForcePositionSnapshot> BuildArmedForcePositionSnapshots(
        ArmedForceSpatialStateStore store)
    {
        List<WorldStateArmedForcePositionSnapshot> result =
            new List<WorldStateArmedForcePositionSnapshot>();
        if (store == null) return result;

        foreach (ArmedForceSpatialPosition position in store.Positions)
        {
            if (position?.ForceId == null || position.Position == null)
            {
                continue;
            }

            result.Add(new WorldStateArmedForcePositionSnapshot(
                position.ForceId.Value,
                position.Position));
        }

        return result;
    }

    private static List<WorldStateContingentManpowerSnapshot> BuildContingentManpowerSnapshots(
        ContingentManpowerStateStore store)
    {
        List<WorldStateContingentManpowerSnapshot> result = new List<WorldStateContingentManpowerSnapshot>();
        if (store == null) return result;
        foreach (ContingentManpowerState state in store.States)
        {
            ManpowerSourceCapacitySnapshot source = null;
            bool resolved = state.SourceId == null;
            if (state.SourceId != null && store.SourceProvider != null)
                resolved = store.SourceProvider.TryGetSnapshot(state.SourceId, out source)
                    && source != null
                    && source.SourceId == state.SourceId;
            if (!resolved) source = null;
            result.Add(new WorldStateContingentManpowerSnapshot(state, source, resolved));
        }
        return result;
    }

    private static List<WorldStateTheftOutcomeSnapshot> BuildTheftOutcomeSnapshots(
        CrimeSocialAppraisalWorldState worldState)
    {
        List<WorldStateTheftOutcomeSnapshot> result = new List<WorldStateTheftOutcomeSnapshot>();
        if (worldState?.TheftOutcomes?.Outcomes == null)
        {
            return result;
        }

        foreach (TheftOutcome outcome in worldState.TheftOutcomes.Outcomes)
        {
            if (outcome != null)
            {
                result.Add(new WorldStateTheftOutcomeSnapshot(
                    outcome.OutcomeId.Value,
                    outcome.PerpetratorPersonId.Value,
                    outcome.VictimPersonId.Value,
                    outcome.LossAmount,
                    outcome.OccurredAbsoluteDay,
                    outcome.OccurrenceKey,
                    outcome.OriginDecisionId));
            }
        }

        return result;
    }

    private static List<WorldStateCrimeKnowledgeSnapshot> BuildCrimeKnowledgeSnapshots(
        CrimeSocialAppraisalWorldState worldState)
    {
        List<WorldStateCrimeKnowledgeSnapshot> result = new List<WorldStateCrimeKnowledgeSnapshot>();
        if (worldState?.CrimeKnowledge?.CurrentObservations == null)
        {
            return result;
        }

        foreach (CrimeKnowledgeObservation observation in worldState.CrimeKnowledge.CurrentObservations)
        {
            if (observation != null)
            {
                result.Add(new WorldStateCrimeKnowledgeSnapshot(
                    observation.EvaluatorPersonId.Value,
                    observation.OutcomeId.Value,
                    observation.Role,
                    observation.KnowsLoss,
                    observation.PerceivedPerpetrator.Kind,
                    observation.PerceivedPerpetrator.PersonId?.Value,
                    observation.PerceivedPerpetrator.InstitutionId?.Value,
                    observation.KnownInvestigatorPersonId?.Value,
                    observation.KnownInvestigatorInstitutionId?.Value,
                    observation.CognitiveBasis.Kind,
                    observation.CognitiveBasis.Reference,
                    observation.CognitiveBasis.SourcePersonId?.Value,
                    observation.CognitiveBasis.SourceInstitutionId?.Value,
                    observation.ObservedAbsoluteDay));
            }
        }

        return result;
    }

    private static List<WorldStateSocialReactionSnapshot> BuildSocialReactionSnapshots(
        CrimeSocialAppraisalWorldState worldState)
    {
        List<WorldStateSocialReactionSnapshot> result = new List<WorldStateSocialReactionSnapshot>();
        if (worldState?.SocialReactions?.HistoricalReactions == null)
        {
            return result;
        }

        foreach (SocialReaction reaction in worldState.SocialReactions.HistoricalReactions)
        {
            if (reaction != null)
            {
                result.Add(new WorldStateSocialReactionSnapshot(
                    reaction.ReactionId.Value,
                    reaction.EvaluatorPersonId.Value,
                    reaction.Source.Domain,
                    reaction.Source.StableId,
                    reaction.Target.Kind,
                    reaction.Target.StableId,
                    reaction.PerceivedAttribution.Kind,
                    reaction.PerceivedAttribution.PersonId?.Value,
                    reaction.PerceivedAttribution.InstitutionId?.Value,
                    reaction.Valence,
                    reaction.Salience,
                    reaction.CognitiveBasis.Kind,
                    reaction.CognitiveBasis.Reference,
                    reaction.CognitiveBasis.SourcePersonId?.Value,
                    reaction.CognitiveBasis.SourceInstitutionId?.Value,
                    reaction.CreatedAbsoluteDay,
                    reaction.SupersedesReactionId?.Value));
            }
        }

        return result;
    }

    private static List<WorldStatePoliticalClaimSnapshot> BuildPoliticalClaimSnapshots(
        IEnumerable<PoliticalClaimRecord> source)
    {
        List<WorldStatePoliticalClaimSnapshot> result = new List<WorldStatePoliticalClaimSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PoliticalClaimRecord claim in source)
        {
            if (claim == null || claim.ClaimId == null || claim.ClaimantPersonId == null || claim.Target == null)
            {
                continue;
            }

            result.Add(new WorldStatePoliticalClaimSnapshot(
                claim.ClaimId.Value,
                claim.ClaimantPersonId.Value,
                claim.ClaimType,
                claim.Target.Kind,
                claim.Target.TargetId,
                claim.Basis,
                claim.BasisDescription,
                claim.CreatedAbsoluteDay,
                claim.Status,
                claim.ResolutionAbsoluteDay,
                PoliticalClaimRecognitionState.Unrecognized,
                null,
                null,
                string.Empty,
                claim.EvidenceReferences));
        }

        return result;
    }

    private static List<WorldStatePoliticalClaimRecognitionSnapshot> BuildPoliticalClaimRecognitionSnapshots(
        IEnumerable<PoliticalClaimRecognitionRecord> source)
    {
        List<WorldStatePoliticalClaimRecognitionSnapshot> result =
            new List<WorldStatePoliticalClaimRecognitionSnapshot>();
        if (source == null) return result;

        foreach (PoliticalClaimRecognitionRecord recognition in source)
        {
            if (recognition?.ClaimId == null || recognition.InstitutionId == null) continue;
            List<WorldStatePoliticalClaimRecognitionHistorySnapshot> history =
                new List<WorldStatePoliticalClaimRecognitionHistorySnapshot>();
            foreach (PoliticalClaimRecognitionHistoryEntry entry in recognition.History)
            {
                history.Add(new WorldStatePoliticalClaimRecognitionHistorySnapshot(
                    entry.State,
                    entry.RecognitionAbsoluteDay,
                    entry.Reason));
            }

            result.Add(new WorldStatePoliticalClaimRecognitionSnapshot(
                recognition.ClaimId.Value,
                recognition.InstitutionId.Value,
                recognition.State,
                recognition.RecognitionAbsoluteDay,
                recognition.Reason,
                history));
        }
        return result;
    }

    private static List<WorldStateFactionSnapshot> BuildFactionSnapshots(
        IEnumerable<FactionRecord> source)
    {
        List<WorldStateFactionSnapshot> result = new List<WorldStateFactionSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (FactionRecord faction in source)
        {
            if (faction?.Id != null)
            {
                result.Add(new WorldStateFactionSnapshot(
                    faction.Id.Value,
                    faction.DisplayName,
                    faction.CreatedAbsoluteDay,
                    faction.MembershipPolicy,
                    faction.ExpulsionAllowed));
            }
        }

        return result;
    }

    private static List<WorldStateFactionAffiliationSnapshot> BuildFactionAffiliationSnapshots(
        IEnumerable<FactionAffiliationRecord> source)
    {
        List<WorldStateFactionAffiliationSnapshot> result =
            new List<WorldStateFactionAffiliationSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (FactionAffiliationRecord affiliation in source)
        {
            if (affiliation?.FactionId != null && affiliation.PersonId != null)
            {
                result.Add(new WorldStateFactionAffiliationSnapshot(
                    affiliation.FactionId.Value,
                    affiliation.PersonId.Value,
                    affiliation.JoinedAbsoluteDay,
                    affiliation.EndedAbsoluteDay,
                    affiliation.AffiliationId?.Value,
                    affiliation.EndReason));
            }
        }

        return result;
    }

    private static List<WorldStatePoliticalSupportSnapshot> BuildPoliticalSupportSnapshots(
        IEnumerable<PoliticalSupportRelationRecord> source)
    {
        List<WorldStatePoliticalSupportSnapshot> result =
            new List<WorldStatePoliticalSupportSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PoliticalSupportRelationRecord support in source)
        {
            if (support?.RelationId == null || support.Source == null || support.Target == null)
            {
                continue;
            }

            result.Add(new WorldStatePoliticalSupportSnapshot(
                support.RelationId.Value,
                support.Source.Kind,
                support.Source.Value,
                support.Target.Kind,
                support.Target.Value,
                support.Disposition,
                support.StartedAbsoluteDay,
                support.EndedAbsoluteDay));
        }

        return result;
    }

    private static List<WorldStatePoliticalDecisionSnapshot> BuildPoliticalDecisionSnapshots(
        IEnumerable<PoliticalDecisionRecord> source)
    {
        List<WorldStatePoliticalDecisionSnapshot> result =
            new List<WorldStatePoliticalDecisionSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PoliticalDecisionRecord decision in source)
        {
            if (decision == null || decision.DecisionId == null || decision.Decider == null || decision.Outcome == null)
            {
                continue;
            }

            List<string> candidates = new List<string>();
            foreach (PersonId candidate in decision.CandidatePersonIds)
            {
                if (candidate != null)
                {
                    candidates.Add(candidate.Value);
                }
            }

            result.Add(new WorldStatePoliticalDecisionSnapshot(
                decision.DecisionId.Value,
                decision.Decider.StableId,
                decision.DecisionKind,
                decision.OfficeId?.Value,
                decision.RecognizingInstitutionId?.Value,
                candidates,
                decision.CandidateFingerprint,
                decision.Outcome.Kind,
                decision.Outcome.SelectedCandidatePersonId?.Value,
                decision.Outcome.ReferencedClaimId?.Value,
                decision.EvidenceReferences,
                decision.KnowledgeReferences,
                decision.ObservedAbsoluteDay,
                decision.DecisionAbsoluteDay,
                decision.ExpectedWorldRevision,
                decision.ExpectedKnowledgeRevision));
        }

        return result;
    }

    private static List<WorldStatePoliticalKnowledgeSnapshot> BuildPoliticalKnowledgeSnapshots(
        IEnumerable<PoliticalKnowledgeRuntime> source)
    {
        List<WorldStatePoliticalKnowledgeSnapshot> result =
            new List<WorldStatePoliticalKnowledgeSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PoliticalKnowledgeRuntime runtime in source)
        {
            if (runtime == null || runtime.Holder == null)
            {
                continue;
            }

            List<WorldStatePoliticalKnowledgeObservationSnapshot> observations =
                new List<WorldStatePoliticalKnowledgeObservationSnapshot>();
            foreach (PoliticalKnowledgeObservation observation in runtime.Observations)
            {
                if (observation == null || observation.Provenance == null)
                {
                    continue;
                }

                string diagnosticIdentityKey = ((int)observation.FactKind).ToString()
                    + ":" + observation.IdentityKey.Length + ":" + observation.IdentityKey;
                observations.Add(new WorldStatePoliticalKnowledgeObservationSnapshot(
                    diagnosticIdentityKey,
                    observation.FactKind,
                    observation.ObservedAbsoluteDay,
                    observation.ReceivedAbsoluteDay,
                    observation.Provenance.Source,
                    observation.Provenance.SourceReference,
                    observation.Provenance.SourcePersonId?.Value,
                    observation.Provenance.SourceInstitutionId?.Value,
                    observation.SnapshotSortKey));
            }

            result.Add(new WorldStatePoliticalKnowledgeSnapshot(
                runtime.Holder.StableId,
                runtime.Holder.Kind,
                runtime.HolderPersonId?.Value,
                runtime.HolderInstitutionId?.Value,
                observations,
                runtime.HolderFactionId?.Value));
        }

        return result;
    }

    private static IEnumerable<string> BuildInstitutionIds(InstitutionStore source)
    {
        if (source == null)
        {
            return null;
        }

        List<string> result = new List<string>();
        foreach (InstitutionRecord record in source.Institutions)
        {
            if (record?.Id != null)
            {
                result.Add(record.Id.Value);
            }
        }

        return result;
    }

    private static IEnumerable<string> BuildOfficeIds(OfficeStore source)
    {
        if (source == null)
        {
            return null;
        }

        List<string> result = new List<string>();
        foreach (OfficeRecord record in source.Offices)
        {
            if (record?.Id != null)
            {
                result.Add(record.Id.Value);
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> BuildOfficeInstitutionIds(OfficeStore source)
    {
        if (source == null)
        {
            return null;
        }

        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (OfficeRecord record in source.Offices)
        {
            if (record?.Id != null && record.InstitutionId != null)
            {
                result[record.Id.Value] = record.InstitutionId.Value;
            }
        }

        return result;
    }

    private static IEnumerable<string> BuildPropertyIds(PropertyOwnershipStore source)
    {
        if (source == null)
        {
            return null;
        }

        List<string> result = new List<string>();
        foreach (PropertyOwnershipRecord record in source.Records)
        {
            if (record?.PropertyId != null)
            {
                result.Add(record.PropertyId.Value);
            }
        }

        return result;
    }

    private static List<WorldStateNpcSnapshot> BuildNpcSnapshots(
        IEnumerable<NpcRuntime> source,
        IReadOnlyList<WorldStateExpeditionSnapshot> expeditions)
    {
        List<NpcRuntime> npcs = SnapshotCollections.Materialize(source);
        npcs.RemoveAll(npc => npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId));
        npcs.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateNpcSnapshot> result = new List<WorldStateNpcSnapshot>();
        foreach (NpcRuntime npc in npcs)
        {
            result.Add(new WorldStateNpcSnapshot(
                npc.RuntimeId,
                npc.DefinitionId,
                npc.NpcName,
                npc.ResidenceSettlementRuntimeId,
                npc.LifeState,
                npc.InjurySeverity,
                npc.CurrentLocation?.RuntimeId,
                npc.CurrentCity?.RuntimeId,
                npc.DestinationLocation?.RuntimeId,
                npc.DestinationCity?.RuntimeId,
                npc.IsTraveling,
                npc.TravelRouteRuntimeId,
                npc.TravelDaysRemaining,
                npc.ActiveTravelPartyId,
                npc.MoneyAccount?.Balance ?? 0f,
                FindActiveExpeditionId(npc.RuntimeId, expeditions),
                BuildInventorySnapshots(npc),
                BuildStatusSnapshots(npc),
                BuildActionSnapshot(npc),
                BuildMerchantTradePlanSnapshot(npc),
                npc.PersonId?.Value));
        }

        return result;
    }

    private static List<WorldStatePersonSnapshot> BuildPersonSnapshots(
        PersonStore personStore,
        SimulationTime simulationTime,
        SimulationCalendar calendar)
    {
        List<WorldStatePersonSnapshot> result = new List<WorldStatePersonSnapshot>();
        if (personStore == null)
        {
            return result;
        }

        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null || person.PersonId == null)
            {
                continue;
            }

            PersonAgeSnapshot age = null;
            if (simulationTime != null && calendar != null)
            {
                PersonAgeQuery.TryCalculate(
                    person,
                    simulationTime,
                    calendar,
                    out age,
                    out _);
            }

            result.Add(new WorldStatePersonSnapshot(
                person.PersonId.Value,
                person.BirthAbsoluteDay,
                age?.AgeInDays,
                age?.CompletedYears,
                person.ResidenceSettlementRuntimeId,
                person.MaterializedNpcRuntimeId,
                person.DeathAbsoluteDay));
        }

        return result;
    }

    private static List<WorldStateParentageSnapshot> BuildParentageSnapshots(
        IEnumerable<ParentageRecord> source)
    {
        List<WorldStateParentageSnapshot> result = new List<WorldStateParentageSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (ParentageRecord parentage in source)
        {
            if (parentage != null)
            {
                result.Add(new WorldStateParentageSnapshot(
                    parentage.ParentId?.Value,
                    parentage.ChildId?.Value));
            }
        }

        result.Sort((left, right) =>
        {
            int parent = StringComparer.Ordinal.Compare(left.ParentPersonId, right.ParentPersonId);
            return parent != 0
                ? parent
                : StringComparer.Ordinal.Compare(left.ChildPersonId, right.ChildPersonId);
        });
        return result;
    }

    private static List<WorldStatePropertyOwnershipSnapshot> BuildPropertyOwnershipSnapshots(
        PropertyOwnershipStore source)
    {
        List<WorldStatePropertyOwnershipSnapshot> result =
            new List<WorldStatePropertyOwnershipSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PropertyOwnershipRecord ownership in source.Records)
        {
            if (ownership?.PropertyId != null && ownership.OwnerPersonId != null)
            {
                result.Add(new WorldStatePropertyOwnershipSnapshot(
                    ownership.PropertyId.Value,
                    ownership.OwnerPersonId.Value));
            }
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PropertyId,
            right.PropertyId));
        return result;
    }

    private static List<WorldStateEstateSnapshot> BuildEstateSnapshots(EstateStore source)
    {
        List<WorldStateEstateSnapshot> result = new List<WorldStateEstateSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (EstateRecord estate in source.Records)
        {
            if (estate?.EstateId != null && estate.DeceasedPersonId != null)
            {
                result.Add(new WorldStateEstateSnapshot(
                    estate.EstateId.Value,
                    estate.DeceasedPersonId.Value,
                    estate.OpenedAbsoluteDay));
            }
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.EstateId,
            right.EstateId));
        return result;
    }

    private static List<WorldStatePropertyTransferSnapshot> BuildPropertyTransferSnapshots(
        PropertyOwnershipStore source)
    {
        List<WorldStatePropertyTransferSnapshot> result =
            new List<WorldStatePropertyTransferSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PropertyOwnershipTransferHistoryRecord transfer in source.TransferHistory)
        {
            if (transfer?.PropertyId != null
                && transfer.PreviousOwnerPersonId != null
                && transfer.NewOwnerPersonId != null)
            {
                result.Add(new WorldStatePropertyTransferSnapshot(
                    transfer.PropertyId.Value,
                    transfer.PreviousOwnerPersonId.Value,
                    transfer.NewOwnerPersonId.Value,
                    transfer.TransferAbsoluteDay));
            }
        }

        result.Sort((left, right) =>
        {
            int property = StringComparer.Ordinal.Compare(left.PropertyId, right.PropertyId);
            if (property != 0)
            {
                return property;
            }

            int day = left.TransferAbsoluteDay.CompareTo(right.TransferAbsoluteDay);
            if (day != 0)
            {
                return day;
            }

            int previous = StringComparer.Ordinal.Compare(
                left.PreviousOwnerPersonId,
                right.PreviousOwnerPersonId);
            return previous != 0
                ? previous
                : StringComparer.Ordinal.Compare(
                left.NewOwnerPersonId,
                right.NewOwnerPersonId);
        });
        return result;
    }

    private static List<string> BuildStatusSnapshots(NpcRuntime npc)
    {
        List<string> result = new List<string>();
        if (npc?.CurrentStatus != null)
        {
            foreach (NpcStatusData status in npc.CurrentStatus)
            {
                if (status != null && string.IsNullOrWhiteSpace(status.statusName) == false)
                {
                    result.Add(status.statusName);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static WorldStateActionSnapshot BuildActionSnapshot(NpcRuntime npc)
    {
        if (npc == null)
        {
            return null;
        }

        NpcActionRuntime actionRuntime = npc.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npc.CurrentAction;
        if (action == null)
        {
            return null;
        }

        return new WorldStateActionSnapshot(
            action.DefinitionId,
            action.actionName,
            action.actionCategory,
            action.actionType,
            actionRuntime?.TargetNpc?.RuntimeId,
            actionRuntime?.TargetCity?.RuntimeId,
            actionRuntime?.TargetItem?.DefinitionId,
            actionRuntime?.Amount ?? 0,
            actionRuntime?.ExpectedUnitPrice ?? 0f,
            actionRuntime?.SuccessChanceMultiplier ?? 1f,
            actionRuntime?.TravelReason ?? NpcTravelReason.None,
            actionRuntime?.ExpectedNetValue ?? 0f,
            actionRuntime?.OriginDecisionId);
    }

    private static WorldStateMerchantTradePlanSnapshot BuildMerchantTradePlanSnapshot(NpcRuntime npc)
    {
        if (npc == null)
        {
            return null;
        }

        MerchantTradePlanRuntime plan = npc.MerchantTradePlan;
        if (plan == null || plan.HasData == false)
        {
            return null;
        }

        return new WorldStateMerchantTradePlanSnapshot(
            plan.HasData,
            plan.IsActive,
            plan.Item?.DefinitionId,
            plan.OriginCity?.RuntimeId,
            plan.TargetCity?.RuntimeId,
            plan.PlannedAmount,
            plan.RemainingAmount,
            plan.PurchasePricePerItem,
            plan.WaitDaysAtDestination,
            plan.PendingTravelDays,
            plan.OriginDecisionId);
    }

    private static string FindActiveExpeditionId(
        string npcRuntimeId,
        IReadOnlyList<WorldStateExpeditionSnapshot> expeditions)
    {
        foreach (WorldStateExpeditionSnapshot expedition in expeditions)
        {
            if (ContainsId(expedition.MemberRuntimeIds, npcRuntimeId))
            {
                return expedition.ExpeditionId;
            }
        }

        return null;
    }

    private static bool ContainsId(IReadOnlyList<string> values, string expected)
    {
        if (values == null)
        {
            return false;
        }

        foreach (string value in values)
        {
            if (string.Equals(value, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<WorldStateInventoryStackSnapshot> BuildInventorySnapshots(NpcRuntime npc)
    {
        Dictionary<string, InventoryAggregate> aggregates = new Dictionary<string, InventoryAggregate>(StringComparer.Ordinal);
        if (npc?.Inventory?.Items != null)
        {
            foreach (InventoryItemRuntime item in npc.Inventory.Items)
            {
                string definitionId = item?.Item?.DefinitionId;
                if (string.IsNullOrWhiteSpace(definitionId) == true)
                {
                    continue;
                }

                if (aggregates.TryGetValue(definitionId, out InventoryAggregate aggregate) == false)
                {
                    aggregate = new InventoryAggregate();
                    aggregates.Add(definitionId, aggregate);
                }

                aggregate.Add(item.Amount, item.AverageUnitCost);
            }
        }

        List<string> definitionIds = new List<string>(aggregates.Keys);
        definitionIds.Sort(StringComparer.Ordinal);
        List<WorldStateInventoryStackSnapshot> result = new List<WorldStateInventoryStackSnapshot>();
        foreach (string definitionId in definitionIds)
        {
            InventoryAggregate aggregate = aggregates[definitionId];
            result.Add(new WorldStateInventoryStackSnapshot(
                definitionId,
                aggregate.Amount,
                aggregate.GetAverageUnitCost()));
        }

        return result;
    }

    private static List<WorldStateCitySnapshot> BuildCitySnapshots(
        IEnumerable<CityRuntime> source,
        IEnumerable<NpcRuntime> knownNpcs,
        IEnumerable<PersonRuntime> persons)
    {
        List<CityRuntime> cities = SnapshotCollections.Materialize(source);
        cities.RemoveAll(city => city == null || string.IsNullOrWhiteSpace(city.RuntimeId));
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateCitySnapshot> result = new List<WorldStateCitySnapshot>();
        foreach (CityRuntime city in cities)
        {
            MarketRuntime market = city.Market;
            MarketCounterpartyRuntime counterparty = market?.Counterparty;
            List<string> residents = new List<string>();
            foreach (NpcRuntime npc in city.ImportantNpcs)
            {
                if (npc != null && string.IsNullOrWhiteSpace(npc.RuntimeId) == false)
                {
                    residents.Add(npc.RuntimeId);
                }
            }

            residents.Sort(StringComparer.Ordinal);
            SettlementPopulationPresenceSummary presence = SettlementPopulationPresenceQuery.BuildSummary(city, knownNpcs, persons);
            result.Add(new WorldStateCitySnapshot(
                city.RuntimeId,
                city.DefinitionId,
                city.Location?.RuntimeId,
                city.CurrentPopulation,
                counterparty?.CounterpartyRuntimeId,
                counterparty != null ? counterparty.LiquidityMode : MarketLiquidityMode.Open,
                counterparty?.MoneyAccount?.Balance ?? 0f,
                residents,
                BuildMarketStockSnapshots(market),
                city.CityName,
                city.Population.Revision,
                presence?.NamedResidentCount ?? 0,
                presence?.NamedPresentCount ?? 0));
        }

        return result;
    }

    private static List<WorldStateMarketStackSnapshot> BuildMarketStockSnapshots(MarketRuntime market)
    {
        List<MarketItemRuntime> items = market == null ? new List<MarketItemRuntime>() : new List<MarketItemRuntime>(market.Items);
        items.RemoveAll(item => item == null || string.IsNullOrWhiteSpace(item.Item?.DefinitionId));
        items.Sort((left, right) =>
        {
            int comparison = StringComparer.Ordinal.Compare(left.Item.DefinitionId, right.Item.DefinitionId);
            if (comparison != 0) return comparison;
            comparison = left.Amount.CompareTo(right.Amount);
            if (comparison != 0) return comparison;
            comparison = left.DesiredAmount.CompareTo(right.DesiredAmount);
            if (comparison != 0) return comparison;
            return left.CurrentPrice.CompareTo(right.CurrentPrice);
        });

        List<WorldStateMarketStackSnapshot> result = new List<WorldStateMarketStackSnapshot>();
        foreach (MarketItemRuntime item in items)
        {
            result.Add(new WorldStateMarketStackSnapshot(
                item.Item.DefinitionId,
                item.Amount,
                item.DesiredAmount,
                item.CurrentPrice));
        }

        return result;
    }

    private static WorldStateSpatialSnapshot BuildSpatialSnapshot(
        SpatialNetworkRuntime network,
        SpatialAuthorityStore authority)
    {
        List<WorldStateHexSnapshot> hexSnapshots = new List<WorldStateHexSnapshot>();
        List<WorldStateAnchoredLocationSnapshot> anchoredLocationSnapshots =
            new List<WorldStateAnchoredLocationSnapshot>();
        List<WorldStateSpatialTopologyBindingSnapshot> topologyBindingSnapshots =
            new List<WorldStateSpatialTopologyBindingSnapshot>();
        List<WorldStateCrossingSnapshot> crossingSnapshots = new List<WorldStateCrossingSnapshot>();
        long? authorityRevision = null;
        string coordinateConventionVersion = null;
        string coordinateCanonicalOrder = null;
        WorldStateSpatialScaleContextSnapshot scaleContextSnapshot = null;
        if (authority != null)
        {
            authorityRevision = authority.Revision;
            coordinateConventionVersion = authority.CoordinateConventionVersion;
            coordinateCanonicalOrder = authority.CoordinateCanonicalOrder;
            if (authority.ScaleContext != null)
            {
                scaleContextSnapshot = new WorldStateSpatialScaleContextSnapshot(
                    authority.ScaleContext.ResolvedConventionId,
                    authority.ScaleContext.SourceIdentity,
                    authority.ScaleContext.SourceVersion,
                    authority.ScaleContext.DistancePerNeighborStep,
                    authority.ScaleContext.Unit);
            }

            foreach (HexRecord hex in authority.Hexes)
            {
                if (hex?.Id != null)
                {
                    hexSnapshots.Add(new WorldStateHexSnapshot(
                        hex.Id.Value,
                        hex.Coordinate.HasValue ? hex.Coordinate.Value.Q : (int?)null,
                        hex.Coordinate.HasValue ? hex.Coordinate.Value.R : (int?)null,
                        hex.TerrainDefinitionId?.Value,
                        hex.AuthoredRevisionToken));
                }
            }

            foreach (LocationRecord location in authority.Locations)
            {
                if (location?.Id != null && location.AnchorHexId != null)
                {
                    anchoredLocationSnapshots.Add(new WorldStateAnchoredLocationSnapshot(
                        location.Id.Value,
                        location.AnchorHexId.Value));
                }
            }

            foreach (SpatialLocalTopologyBinding binding in authority.LocalTopologyBindings)
            {
                if (binding?.LocationId != null)
                {
                    topologyBindingSnapshots.Add(new WorldStateSpatialTopologyBindingSnapshot(
                        binding.OwnerKind,
                        binding.OwnerRuntimeId,
                        binding.LocationId.Value));
                }
            }

            foreach (CrossingRecord crossing in authority.Crossings)
            {
                if (crossing?.Id != null && crossing.Boundary != null && crossing.AnchorHexId != null)
                {
                    crossingSnapshots.Add(new WorldStateCrossingSnapshot(
                        crossing.Id.Value,
                        crossing.Boundary.FirstHexId.Value,
                        crossing.Boundary.SecondHexId.Value,
                        crossing.AnchorHexId.Value));
                }
            }
        }

        if (network == null)
        {
            return new WorldStateSpatialSnapshot(
                hexes: hexSnapshots,
                anchoredLocations: anchoredLocationSnapshots,
                topologyBindings: topologyBindingSnapshots,
                authorityRevision: authorityRevision,
                coordinateConventionVersion: coordinateConventionVersion,
                coordinateCanonicalOrder: coordinateCanonicalOrder,
                scaleContext: scaleContextSnapshot,
                crossings: crossingSnapshots);
        }

        List<SpatialLocationRuntime> locations = new List<SpatialLocationRuntime>(network.Locations);
        locations.RemoveAll(location => location == null || string.IsNullOrWhiteSpace(location.RuntimeId));
        locations.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStateLocationSnapshot> locationSnapshots = new List<WorldStateLocationSnapshot>();
        foreach (SpatialLocationRuntime location in locations)
        {
            locationSnapshots.Add(new WorldStateLocationSnapshot(location.RuntimeId));
        }

        List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>(network.Routes);
        routes.RemoveAll(route => route == null || string.IsNullOrWhiteSpace(route.RuntimeId));
        routes.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStateRouteSnapshot> routeSnapshots = new List<WorldStateRouteSnapshot>();
        foreach (SpatialRouteRuntime route in routes)
        {
            routeSnapshots.Add(new WorldStateRouteSnapshot(
                route.RuntimeId,
                route.Origin?.RuntimeId,
                route.Destination?.RuntimeId,
                route.TravelDays));
        }

        return new WorldStateSpatialSnapshot(
            locationSnapshots,
            routeSnapshots,
            hexSnapshots,
            anchoredLocationSnapshots,
            authorityRevision,
            topologyBindingSnapshots,
            coordinateConventionVersion,
            coordinateCanonicalOrder,
            scaleContextSnapshot,
            crossingSnapshots);
    }

    private static List<WorldStateSiteSnapshot> BuildSiteSnapshots(ExplorableSiteStore store)
    {
        List<ExplorableSiteRuntime> sites = store == null
            ? new List<ExplorableSiteRuntime>()
            : new List<ExplorableSiteRuntime>(store.Sites);
        sites.RemoveAll(site => site == null || string.IsNullOrWhiteSpace(site.RuntimeId));
        sites.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateSiteSnapshot> result = new List<WorldStateSiteSnapshot>();
        foreach (ExplorableSiteRuntime site in sites)
        {
            result.Add(new WorldStateSiteSnapshot(
                site.RuntimeId,
                site.DefinitionId,
                site.Location?.RuntimeId,
                site.Definition != null ? site.Definition.kind : ExplorableSiteKind.Generic));
        }

        return result;
    }

    private static List<WorldStateExpeditionSnapshot> BuildExpeditionSnapshots(ExpeditionStore store)
    {
        List<ExpeditionRuntime> expeditions = store == null
            ? new List<ExpeditionRuntime>()
            : new List<ExpeditionRuntime>(store.ActiveExpeditions);
        expeditions.RemoveAll(expedition => expedition == null || string.IsNullOrWhiteSpace(expedition.ExpeditionId));
        expeditions.Sort((left, right) => StringComparer.Ordinal.Compare(left.ExpeditionId, right.ExpeditionId));

        List<WorldStateExpeditionSnapshot> result = new List<WorldStateExpeditionSnapshot>();
        foreach (ExpeditionRuntime expedition in expeditions)
        {
            ExpeditionObjectiveRuntime objective = expedition.Objective;
            result.Add(new WorldStateExpeditionSnapshot(
                expedition.ExpeditionId,
                expedition.State,
                expedition.TargetSiteRuntimeId,
                expedition.OriginLocationRuntimeId,
                expedition.TargetLocationRuntimeId,
                expedition.OutboundRouteRuntimeId,
                expedition.OriginDecisionId,
                expedition.TravelPartyId,
                expedition.CurrentLocalPlaceRuntimeId,
                expedition.ExplorationProgress,
                expedition.ExplorationProgressRequired,
                SortStrings(expedition.MemberRuntimeIds),
                SortStrings(expedition.PerformerRuntimeIds),
                SortStrings(expedition.SupportRuntimeIds),
                SortStrings(expedition.VisitedLocalPlaceRuntimeIds),
                SortStrings(expedition.ObservedLocalConnectionRuntimeIds),
                objective != null ? objective.ObjectiveType : ExpeditionObjectiveType.Explore,
                objective?.TargetItemDefinitionId,
                objective?.TargetNotableItemRuntimeId,
                objective?.TargetOppositionRuntimeId,
                objective?.IsCompleted ?? false,
                objective?.AllowContinueAfterCompletion ?? false));
        }

        return result;
    }

    private static List<WorldStatePlaceContentSnapshot> BuildPlaceContentSnapshots(PlaceContentStore store)
    {
        List<PlaceContentRuntime> contents = store == null
            ? new List<PlaceContentRuntime>()
            : new List<PlaceContentRuntime>(store.Places);
        contents.RemoveAll(content => content == null || content.Owner == null);
        contents.Sort((left, right) => StringComparer.Ordinal.Compare(left.Owner.StableKey, right.Owner.StableKey));

        List<WorldStatePlaceContentSnapshot> result = new List<WorldStatePlaceContentSnapshot>();
        foreach (PlaceContentRuntime content in contents)
        {
            PlaceContentOwnerReference owner = content.Owner;
            result.Add(new WorldStatePlaceContentSnapshot(
                owner.OwnerKind,
                owner.OwnerRuntimeId,
                owner.MacroLocationRuntimeId,
                owner.TopologyOwnerRuntimeId,
                content.SiteState,
                content.AccessState,
                content.ControllerRuntimeId,
                BuildPlaceStackSnapshots(content.StackedContent),
                BuildOppositionSnapshots(content.Oppositions)));
        }

        return result;
    }

    private static List<WorldStatePlaceStackSnapshot> BuildPlaceStackSnapshots(
        IEnumerable<PlaceContentStackRuntime> source)
    {
        List<PlaceContentStackRuntime> stacks = SnapshotCollections.Materialize(source);
        stacks.RemoveAll(stack => stack == null || string.IsNullOrWhiteSpace(stack.ItemDefinitionId));
        stacks.Sort((left, right) => StringComparer.Ordinal.Compare(left.ItemDefinitionId, right.ItemDefinitionId));
        List<WorldStatePlaceStackSnapshot> result = new List<WorldStatePlaceStackSnapshot>();
        foreach (PlaceContentStackRuntime stack in stacks)
        {
            result.Add(new WorldStatePlaceStackSnapshot(
                stack.ItemDefinitionId,
                stack.Amount,
                stack.PersistencePolicy,
                stack.DecayPerDay,
                stack.AverageUnitCost));
        }

        return result;
    }

    private static List<WorldStatePlaceOppositionSnapshot> BuildOppositionSnapshots(
        IEnumerable<PlaceOppositionRuntime> source)
    {
        List<PlaceOppositionRuntime> oppositions = SnapshotCollections.Materialize(source);
        oppositions.RemoveAll(opposition => opposition == null || string.IsNullOrWhiteSpace(opposition.RuntimeId));
        oppositions.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStatePlaceOppositionSnapshot> result = new List<WorldStatePlaceOppositionSnapshot>();
        foreach (PlaceOppositionRuntime opposition in oppositions)
        {
            List<string> named = new List<string>();
            foreach (NpcRuntime npc in opposition.NamedParticipants)
            {
                if (npc != null && string.IsNullOrWhiteSpace(npc.RuntimeId) == false)
                {
                    named.Add(npc.RuntimeId);
                }
            }

            List<string> aggregate = new List<string>();
            foreach (AggregateParticipantSnapshot participant in opposition.AggregateParticipants)
            {
                if (participant != null && string.IsNullOrWhiteSpace(participant.SourceId) == false)
                {
                    aggregate.Add(participant.SourceId);
                }
            }

            named.Sort(StringComparer.Ordinal);
            aggregate.Sort(StringComparer.Ordinal);
            result.Add(new WorldStatePlaceOppositionSnapshot(
                opposition.RuntimeId,
                opposition.IsActive,
                opposition.IsResolved,
                opposition.OppositionSideId,
                named,
                aggregate));
        }

        return result;
    }

    private static List<WorldStateNotableItemSnapshot> BuildNotableItemSnapshots(PlaceContentStore store)
    {
        List<NotableItemRuntime> notables = store == null
            ? new List<NotableItemRuntime>()
            : new List<NotableItemRuntime>(store.NotableItems);
        notables.RemoveAll(notable => notable == null || string.IsNullOrWhiteSpace(notable.RuntimeId));
        notables.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateNotableItemSnapshot> result = new List<WorldStateNotableItemSnapshot>();
        foreach (NotableItemRuntime notable in notables)
        {
            NotableItemCustodyReference custody = notable.Custody;
            PlaceContentOwnerReference owner = custody?.PlaceOwner;
            result.Add(new WorldStateNotableItemSnapshot(
                notable.RuntimeId,
                notable.DefinitionId,
                notable.IsPresent,
                custody?.CustodyKind,
                owner?.OwnerKind,
                owner?.OwnerRuntimeId,
                owner?.MacroLocationRuntimeId,
                owner?.TopologyOwnerRuntimeId,
                custody?.NpcRuntimeId));
        }

        return result;
    }

    private static List<WorldStateLocalTopologySnapshot> BuildLocalTopologySnapshots(LocalTopologyStore store)
    {
        List<LocalTopologyRuntime> topologies = store == null
            ? new List<LocalTopologyRuntime>()
            : new List<LocalTopologyRuntime>(store.Topologies);
        topologies.RemoveAll(topology => topology == null || topology.Owner == null);
        topologies.Sort((left, right) => StringComparer.Ordinal.Compare(
            WorldStateSnapshotValue.OwnerKey(left.Owner.OwnerKind, left.Owner.OwnerRuntimeId),
            WorldStateSnapshotValue.OwnerKey(right.Owner.OwnerKind, right.Owner.OwnerRuntimeId)));

        List<WorldStateLocalTopologySnapshot> result = new List<WorldStateLocalTopologySnapshot>();
        foreach (LocalTopologyRuntime topology in topologies)
        {
            LocalTopologyOwnerReference owner = topology.Owner;
            List<LocalPlaceRuntime> places = new List<LocalPlaceRuntime>(topology.Places);
            places.RemoveAll(place => place == null || string.IsNullOrWhiteSpace(place.RuntimeId));
            places.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
            List<WorldStateLocalPlaceSnapshot> placeSnapshots = new List<WorldStateLocalPlaceSnapshot>();
            foreach (LocalPlaceRuntime place in places)
            {
                placeSnapshots.Add(new WorldStateLocalPlaceSnapshot(
                    place.RuntimeId,
                    place.TypeDefinitionId,
                    place.Parent?.RuntimeId,
                    topology.IsEntryPoint(place)));
            }

            List<LocalTopologyConnectionRuntime> connections = new List<LocalTopologyConnectionRuntime>(topology.Connections);
            connections.RemoveAll(connection => connection == null || string.IsNullOrWhiteSpace(connection.RuntimeId));
            connections.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
            List<WorldStateLocalConnectionSnapshot> connectionSnapshots = new List<WorldStateLocalConnectionSnapshot>();
            foreach (LocalTopologyConnectionRuntime connection in connections)
            {
                connectionSnapshots.Add(new WorldStateLocalConnectionSnapshot(
                    connection.RuntimeId,
                    connection.Origin?.RuntimeId,
                    connection.Destination?.RuntimeId,
                    connection.TraversalCost,
                    connection.TypeDefinitionId));
            }

            result.Add(new WorldStateLocalTopologySnapshot(
                owner.OwnerKind,
                owner.OwnerRuntimeId,
                owner.MacroLocationRuntimeId,
                topology.PublicationState,
                placeSnapshots,
                connectionSnapshots));
        }

        return result;
    }

    private static List<string> SortStrings(IEnumerable<string> source)
    {
        List<string> result = new List<string>();
        if (source != null)
        {
            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    result.Add(value);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private sealed class InventoryAggregate
    {
        public int Amount { get; private set; }
        private double totalCost;

        public void Add(int amount, float averageUnitCost)
        {
            Amount += amount;
            totalCost += (double)amount * averageUnitCost;
        }

        public float GetAverageUnitCost()
        {
            return Amount > 0 ? (float)(totalCost / Amount) : 0f;
        }
    }
}

internal static class SnapshotCollections
{
    public static List<T> Materialize<T>(IEnumerable<T> source)
    {
        return source == null ? new List<T>() : new List<T>(source);
    }

    public static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
    {
        return Materialize(source).AsReadOnly();
    }

    public static IReadOnlyList<T> CopySorted<T>(IEnumerable<T> source, Func<T, string> keySelector)
    {
        List<T> values = Materialize(source);
        if (keySelector != null)
        {
            values.Sort((left, right) => StringComparer.Ordinal.Compare(keySelector(left) ?? string.Empty, keySelector(right) ?? string.Empty));
        }

        return values.AsReadOnly();
    }
}

internal static class WorldStateSnapshotValue
{
    public static string OwnerKey(PlaceContentOwnerKind kind, string runtimeId)
    {
        return Enum.GetName(typeof(PlaceContentOwnerKind), kind) + ":" + runtimeId;
    }

    public static string OwnerKey(LocalTopologyOwnerKind kind, string runtimeId)
    {
        return Enum.GetName(typeof(LocalTopologyOwnerKind), kind) + ":" + runtimeId;
    }
}
