using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SimulationRuntime
{
    private readonly SimulationTime simulationTime;
    private readonly List<CityRuntime> cities;
    private readonly List<NpcRuntime> npcRuntimes;
    private readonly IReadOnlyList<NpcRuntime> npcRuntimeSnapshot;
    private readonly Dictionary<string, NpcRuntime> npcRegistryById;
    private readonly EffectiveSimulationConfiguration configuration;
    private readonly SimulationCalendar calendar;
    private readonly IPersonNaturalMortalitySampleProvider naturalMortalitySamples;
    private readonly IAggregateDemographyProvider aggregateDemographyProvider;
    private DailyDemographyReport lastDailyDemographyReport;
    private readonly PersonStore personStore;
    private readonly GenealogyStore genealogyStore;
    private readonly InstitutionStore institutionStore;
    private readonly OfficeStore officeStore;
    private readonly PropertyOwnershipStore propertyOwnershipStore;
    private readonly EstateStore estateStore;
    private readonly PoliticalClaimStore politicalClaimStore;
    private readonly FactionStore factionStore;
    private readonly PoliticalSupportStore politicalSupportStore;
    private readonly PoliticalKnowledgeStore politicalKnowledgeStore;
    private readonly PoliticalDecisionStore politicalDecisionStore;
    private long politicalWorldRevision;
    private readonly List<NpcActionData> configuredActions;
    private readonly ScheduledDirectiveSystem scheduledDirectiveSystem;
    private readonly JusticeSystem justiceSystem;
    private readonly CrimeSystem crimeSystem;
    private readonly NpcDecisionSystem npcDecisionSystem;
    private readonly TravelSystem travelSystem;
    private readonly TravelPartySystem travelPartySystem;
    private readonly MerchantSystem merchantSystem;
    private readonly CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem;
    private readonly ExplorableSiteStore explorableSiteStore;
    private readonly ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem;
    private readonly ExpeditionSystem expeditionSystem;
    private readonly PlaceContentStore placeContentStore;
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem;
    private readonly SimulationLogger logger;

    public SimulationTime SimulationTime => simulationTime;
    public long CurrentDay => simulationTime.AbsoluteDay;
    public IReadOnlyList<CityRuntime> Cities => cities;
    public EffectiveSimulationConfiguration Configuration => configuration;
    public SimulationCalendar Calendar => calendar;
    public DailyDemographyReport LastDailyDemographyReport => lastDailyDemographyReport;
    public PersonStore PersonStore => personStore;
    public IReadOnlyList<ParentageRecord> GenealogyRecords => genealogyStore.Records;
    public IReadOnlyList<InstitutionRecord> InstitutionRecords => institutionStore.Institutions;
    public IReadOnlyList<OfficeRecord> OfficeRecords => officeStore.Offices;
    public IReadOnlyList<OfficeIncumbency> OfficeIncumbencies => officeStore.Incumbencies;
    public IReadOnlyList<OfficeTenureRecord> OfficeTenureHistory => officeStore.TenureHistory;
    public PropertyOwnershipStore PropertyOwnershipStore => propertyOwnershipStore;
    public EstateStore EstateStore => estateStore;
    public IReadOnlyList<PropertyOwnershipRecord> PropertyOwnershipRecords => propertyOwnershipStore.Records;
    public IReadOnlyList<EstateRecord> EstateRecords => estateStore.Records;
    public IReadOnlyList<PoliticalClaimRecord> PoliticalClaimRecords => politicalClaimStore.Records;
    public IReadOnlyList<FactionRecord> FactionRecords => factionStore.Factions;
    public IReadOnlyList<FactionAffiliationRecord> FactionAffiliationRecords => factionStore.Affiliations;
    public IReadOnlyList<PoliticalSupportRelationRecord> PoliticalSupportRecords => politicalSupportStore.Records;
    public int PoliticalKnowledgeHolderCount => politicalKnowledgeStore.Count;
    public long PoliticalKnowledgeRevision => politicalKnowledgeStore.Revision;
    public long PoliticalWorldRevision => GetEffectivePoliticalWorldRevision();
    public IReadOnlyList<PoliticalDecisionRecord> PoliticalDecisionRecords => politicalDecisionStore.Records;
    /// <summary>
    /// Read-only view of every named NPC registered with this world. Registration is
    /// explicit; death and emigration do not remove an NPC from this world roster.
    /// </summary>
    public IReadOnlyList<NpcRuntime> NpcRuntimes => npcRuntimeSnapshot;
    public PlaceContentStore PlaceContentStore => placeContentStore;

    public SimulationRuntime(
        SimulationTime simulationTime,
        IEnumerable<CityRuntime> cities,
        IEnumerable<NpcRuntime> npcRuntimes,
        bool? economyEnabled = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        ScheduledDirectiveSystem scheduledDirectiveSystem = null,
        JusticeSystem justiceSystem = null,
        CrimeSystem crimeSystem = null,
        NpcDecisionSystem npcDecisionSystem = null,
        TravelSystem travelSystem = null,
        TravelPartySystem travelPartySystem = null,
        MerchantSystem merchantSystem = null,
        CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem = null,
        NpcDecisionRecorder decisionRecorder = null,
        SimulationLogger logger = null,
        bool? guardCrimeEnabled = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem = null,
        ExpeditionSystem expeditionSystem = null,
        PlaceContentStore placeContentStore = null,
        AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem = null,
        EffectiveSimulationConfiguration configuration = null,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null,
        InstitutionStore institutionStore = null,
        OfficeStore officeStore = null,
        CalendarDefinition calendarDefinition = null,
        IPersonNaturalMortalitySampleProvider naturalMortalitySamples = null,
        IAggregateDemographyProvider aggregateDemographyProvider = null,
        PropertyOwnershipStore propertyOwnershipStore = null,
        EstateStore estateStore = null,
        PoliticalClaimStore politicalClaimStore = null,
        FactionStore factionStore = null,
        PoliticalSupportStore politicalSupportStore = null,
        PoliticalKnowledgeStore politicalKnowledgeStore = null,
        PoliticalDecisionStore politicalDecisionStore = null,
        long? politicalWorldRevision = null)
    {
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));

        if (configuration != null && (economyEnabled.HasValue || guardCrimeEnabled.HasValue))
        {
            throw new ArgumentException(
                "Provide EffectiveSimulationConfiguration or legacy feature flags, not both.",
                nameof(configuration));
        }

        EffectiveSimulationConfiguration resolvedConfiguration = configuration
            ?? SimulationConfigurationDefaults.CreateForRuntime(
                economyEnabled ?? true,
                guardCrimeEnabled ?? false);
        SimulationConfigurationValidationResult configurationValidation =
            SimulationConfigurationValidator.Validate(resolvedConfiguration);
        if (configurationValidation.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime configuration is invalid: "
                + string.Join("; ", configurationValidation.Errors),
                nameof(configuration));
        }

        PersonStore resolvedPersonStore = personStore ?? new PersonStore();
        GenealogyStore resolvedGenealogyStore = genealogyStore ?? new GenealogyStore();
        ValidateGenealogyStore(resolvedPersonStore, resolvedGenealogyStore);
        InstitutionStore resolvedInstitutionStore = ResolveInstitutionStore(
            institutionStore,
            officeStore);
        OfficeStore resolvedOfficeStore = CloneOfficeStore(
            officeStore,
            resolvedInstitutionStore,
            resolvedPersonStore);
        PropertyOwnershipStore resolvedPropertyOwnershipStore = ClonePropertyOwnershipStore(
            propertyOwnershipStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);
        EstateStore resolvedEstateStore = CloneEstateStore(
            estateStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);

        this.configuration = resolvedConfiguration;
        this.calendar = new SimulationCalendar(
            calendarDefinition ?? CalendarDefinition.CreateDefault());
        this.naturalMortalitySamples = naturalMortalitySamples;
        this.aggregateDemographyProvider = aggregateDemographyProvider;
        this.personStore = resolvedPersonStore;
        this.genealogyStore = CloneGenealogyStore(resolvedGenealogyStore);
        this.institutionStore = resolvedInstitutionStore;
        this.officeStore = resolvedOfficeStore;
        this.propertyOwnershipStore = resolvedPropertyOwnershipStore;
        this.estateStore = resolvedEstateStore;
        this.politicalClaimStore = ClonePoliticalClaimStore(
            politicalClaimStore,
            resolvedPersonStore,
            resolvedInstitutionStore,
            resolvedOfficeStore,
            resolvedPropertyOwnershipStore,
            simulationTime.AbsoluteDay);
        this.factionStore = CloneFactionStore(
            factionStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);
        this.politicalSupportStore = ClonePoliticalSupportStore(
            politicalSupportStore,
            resolvedPersonStore,
            this.factionStore,
            this.politicalClaimStore,
            simulationTime.AbsoluteDay);
        this.politicalKnowledgeStore = ClonePoliticalKnowledgeStore(
            politicalKnowledgeStore,
            resolvedPersonStore,
            resolvedInstitutionStore,
            simulationTime.AbsoluteDay,
            this.politicalClaimStore,
            this.factionStore,
            this.officeStore);
        long initialPoliticalWorldRevision = politicalWorldRevision
            ?? ResolveInitialPoliticalWorldRevision(politicalDecisionStore);
        this.politicalWorldRevision = initialPoliticalWorldRevision;
        this.politicalDecisionStore = ClonePoliticalDecisionStore(
            politicalDecisionStore,
            this.politicalKnowledgeStore,
            simulationTime.AbsoluteDay,
            this.personStore,
            this.institutionStore,
            this.officeStore,
            this.politicalClaimStore);
        this.cities = cities != null ? new List<CityRuntime>(cities) : new List<CityRuntime>();
        this.npcRuntimes = new List<NpcRuntime>();
        this.npcRuntimeSnapshot = this.npcRuntimes.AsReadOnly();
        this.npcRegistryById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        this.configuredActions = configuredActions != null
            ? new List<NpcActionData>(configuredActions)
            : null;
        this.scheduledDirectiveSystem = scheduledDirectiveSystem;
        this.justiceSystem = justiceSystem;
        this.crimeSystem = crimeSystem;
        this.npcDecisionSystem = npcDecisionSystem;
        this.travelSystem = travelSystem;
        this.travelPartySystem = travelPartySystem;
        this.merchantSystem = merchantSystem;
        this.commercialKnowledgeSharingSystem = commercialKnowledgeSharingSystem;
        this.explorableSiteStore = explorableSiteStore;
        this.explorableSiteKnowledgeSystem = explorableSiteKnowledgeSystem;
        this.expeditionSystem = expeditionSystem;
        this.placeContentStore = placeContentStore;
        this.decisionRecorder = decisionRecorder;
        this.adventureExpeditionAutonomySystem = adventureExpeditionAutonomySystem;
        this.logger = logger;

        this.expeditionSystem?.BindWorldRuntime(this);

        if (npcRuntimes != null)
        {
            foreach (NpcRuntime npcRuntime in npcRuntimes)
            {
                if (TryRegisterNpc(npcRuntime, out WorldNpcRegistryFailure failure) == false)
                {
                    throw new ArgumentException(
                        "The SimulationRuntime NPC roster is invalid: " + failure + ".",
                        nameof(npcRuntimes));
                }
            }
        }
    }

    /// <summary>
    /// Registers one named NPC in the world-owned roster. The operation rejects null,
    /// unidentified, and duplicate RuntimeIds and never changes population aggregates.
    /// </summary>
    public bool TryRegisterNpc(NpcRuntime npcRuntime, out WorldNpcRegistryFailure failure)
    {
        failure = WorldNpcRegistryFailure.None;

        if (npcRuntime == null)
        {
            failure = WorldNpcRegistryFailure.InvalidNpc;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId) == true)
        {
            failure = WorldNpcRegistryFailure.InvalidRuntimeId;
            return false;
        }

        if (npcRegistryById.ContainsKey(npcRuntime.RuntimeId) == true)
        {
            failure = WorldNpcRegistryFailure.DuplicateRuntimeId;
            return false;
        }

        if (npcRuntime.PersonId != null
            && (personStore.TryGet(npcRuntime.PersonId, out PersonRuntime person) == false
                || person.IsMaterialized == false
                || string.Equals(person.MaterializedNpcRuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal) == false))
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        if (npcRuntime.PersonId != null
            && personStore.TryGet(npcRuntime.PersonId, out PersonRuntime lifeStatePerson)
            && lifeStatePerson.IsDeadAt(CurrentDay) != npcRuntime.IsDead)
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        if (npcRuntime.PersonId != null
            && (personStore.TryGet(npcRuntime.PersonId, out PersonRuntime boundPerson) == false
                || npcRuntime.TryBindPersonRuntime(boundPerson) == false))
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        npcRegistryById.Add(npcRuntime.RuntimeId, npcRuntime);
        npcRuntimes.Add(npcRuntime);
        return true;
    }

    /// <summary>
    /// Explicitly unregisters an NPC from the world. A resident must first emigrate
    /// through the population lifecycle so the aggregate cannot retain a named member
    /// that disappeared from the authoritative roster.
    /// </summary>
    public bool TryUnregisterNpc(string runtimeId, out WorldNpcRegistryFailure failure)
    {
        failure = WorldNpcRegistryFailure.None;

        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            failure = WorldNpcRegistryFailure.InvalidRuntimeId;
            return false;
        }

        if (npcRegistryById.TryGetValue(runtimeId, out NpcRuntime npcRuntime) == false)
        {
            failure = WorldNpcRegistryFailure.NpcNotRegistered;
            return false;
        }

        if (personStore.TryGetByMaterializedNpcRuntimeId(runtimeId, out _))
        {
            failure = WorldNpcRegistryFailure.NpcBoundToPerson;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.ResidenceSettlementRuntimeId) == false)
        {
            failure = WorldNpcRegistryFailure.NpcHasResidence;
            return false;
        }

        npcRegistryById.Remove(runtimeId);
        npcRuntimes.Remove(npcRuntime);
        return true;
    }

    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime npcRuntime)
    {
        npcRuntime = null;
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && npcRegistryById.TryGetValue(runtimeId, out npcRuntime);
    }

    /// <summary>
    /// Computes living represented-resident floors at the world boundary. The
    /// aggregate population system receives the resulting snapshot explicitly.
    /// </summary>
    internal RepresentedResidentFloorSnapshot BuildRepresentedResidentFloorSnapshot()
    {
        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (CityRuntime city in cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId))
            {
                continue;
            }

            SettlementPopulationPresenceSummary presence =
                SettlementPopulationPresenceQuery.BuildSummary(
                    city,
                    npcRuntimeSnapshot,
                    personStore.Persons);
            floors[city.RuntimeId] = presence?.RepresentedResidentCount ?? 0;
        }

        return new RepresentedResidentFloorSnapshot(floors);
    }

    public bool TryRegisterPerson(PersonRuntime person, out PersonStoreFailure failure)
    {
        if (person != null
            && person.BirthAbsoluteDay.HasValue
            && person.BirthAbsoluteDay.Value > CurrentDay)
        {
            failure = PersonStoreFailure.BirthAbsoluteDayInFuture;
            return false;
        }

        if (person != null
            && person.DeathAbsoluteDay.HasValue
            && person.DeathAbsoluteDay.Value > CurrentDay)
        {
            failure = PersonStoreFailure.DeathAbsoluteDayInFuture;
            return false;
        }

        bool registered = personStore.TryRegister(person, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryProposePersonDeath(
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        return PersonDeathLifecycleSystem.TryProposeDeath(
            this,
            personId,
            out transition,
            out failure);
    }

    public bool TryApplyPersonDeath(
        PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        bool applied = PersonDeathLifecycleSystem.TryApplyDeath(this, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyPersonDeathWithConflictInjury(
        PersonDeathTransition transition,
        NpcInjurySeverity injurySeverity,
        out PersonDeathLifecycleFailure failure)
    {
        bool applied = PersonDeathLifecycleSystem.TryApplyDeathWithConflictInjury(
            this,
            transition,
            injurySeverity,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyPersonDeath(
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        bool applied = PersonDeathLifecycleSystem.TryApplyDeath(
            this,
            personId,
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryRegisterInstitution(
        InstitutionRecord record,
        out InstitutionFoundationFailure failure)
    {
        bool registered = institutionStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryRegisterFaction(
        FactionRecord record,
        out FactionFoundationFailure failure)
    {
        if (record == null || record.Id == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidFaction,
                "A faction with a stable FactionId is required.");
            return false;
        }

        if (record.CreatedAbsoluteDay > CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidCreationAbsoluteDay,
                "Faction creation must be within the current world timeline.");
            return false;
        }

        bool registered = factionStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryProposeFactionAffiliation(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationAddTransition transition,
        out FactionFoundationFailure failure)
    {
        transition = null;
        if (factionId == null || factionStore.TryGet(factionId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.FactionNotRegistered,
                "The faction must be registered in this world.");
            return false;
        }

        if (personId == null || personStore.TryGet(personId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.PersonNotRegistered,
                "The affiliated Person must be registered in this world.");
            return false;
        }

        return FactionAffiliationSystem.TryProposeAdd(
            factionStore,
            factionId,
            personId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyFactionAffiliation(
        FactionAffiliationAddTransition transition,
        out FactionFoundationFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.StaleAffiliation,
                "The faction affiliation proposal was created for a different world day.");
            return false;
        }

        bool applied = FactionAffiliationSystem.TryApplyAdd(factionStore, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposeFactionAffiliationEnd(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationEndTransition transition,
        out FactionFoundationFailure failure)
    {
        transition = null;
        if (factionId == null || personId == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidTransition,
                "A faction and PersonId are required.");
            return false;
        }

        return FactionAffiliationSystem.TryProposeEnd(
            factionStore,
            factionId,
            personId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyFactionAffiliationEnd(
        FactionAffiliationEndTransition transition,
        out FactionFoundationFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.StaleAffiliation,
                "The faction affiliation end proposal was created for a different world day.");
            return false;
        }

        if (transition.EndedAbsoluteDay > CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidEndAbsoluteDay,
                "Affiliation end must be within the current world timeline.");
            return false;
        }

        bool applied = FactionAffiliationSystem.TryApplyEnd(factionStore, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryRegisterPoliticalClaim(
        PoliticalClaimRecord record,
        out PoliticalClaimFailure failure)
    {
        failure = PoliticalClaimFailure.None;
        if (record == null || record.ClaimId == null || record.ClaimantPersonId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaim,
                "A political claim with a claimant and claim id is required.");
            return false;
        }

        if (record.CreatedAbsoluteDay < 0L || record.CreatedAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidCreationAbsoluteDay,
                "Claim creation must be within the current world timeline.");
            return false;
        }

        if (personStore.TryGet(record.ClaimantPersonId, out _) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimantNotRegistered,
                "The political claim claimant must be registered in this world.");
            return false;
        }

        if (TryValidatePoliticalClaimTarget(record, out failure) == false)
        {
            return false;
        }

        if (TryValidatePoliticalClaimRecognition(record, out failure) == false)
        {
            return false;
        }

        if (record.ResolutionAbsoluteDay.HasValue
            && record.ResolutionAbsoluteDay.Value > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "Claim resolution must be within the current world timeline.");
            return false;
        }

        bool registered = politicalClaimStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryProposePoliticalClaimRecognition(
        PoliticalClaimId claimId,
        InstitutionId recognizingInstitutionId,
        PoliticalClaimRecognitionState recognitionState,
        string reason,
        out PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        transition = null;
        if (recognizingInstitutionId == null
            || institutionStore.TryGet(recognizingInstitutionId, out _) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetNotFound,
                "The recognizing institution must be registered in this world.");
            return false;
        }

        return PoliticalClaimSystem.TryProposeRecognition(
            politicalClaimStore,
            claimId,
            recognizingInstitutionId,
            recognitionState,
            CurrentDay,
            CurrentDay,
            reason,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalClaimRecognition(
        PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim recognition proposal was created for a different world day.");
            return false;
        }

        if (transition.RecognitionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "Claim recognition must be applied within the current world timeline.");
            return false;
        }

        bool applied = PoliticalClaimSystem.TryApplyRecognition(
            politicalClaimStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposePoliticalClaimResolution(
        PoliticalClaimId claimId,
        PoliticalClaimStatus status,
        out PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryProposeResolution(
            politicalClaimStore,
            claimId,
            status,
            CurrentDay,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalClaimResolution(
        PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim resolution proposal was created for a different world day.");
            return false;
        }

        if (transition.ResolutionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "Claim resolution must be applied within the current world timeline.");
            return false;
        }

        bool applied = PoliticalClaimSystem.TryApplyResolution(
            politicalClaimStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    /// <summary>
    /// Registers an existing support relation while loading or composing world state.
    /// New in-world additions must use the proposal/apply pair below so the current
    /// world day and store revision are revalidated atomically.
    /// </summary>
    public bool TryRegisterPoliticalSupport(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportFailure failure)
    {
        if (record == null)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidRelation,
                "A political support relation is required.");
            return false;
        }

        if (record.StartedAbsoluteDay > CurrentDay
            || (record.EndedAbsoluteDay.HasValue && record.EndedAbsoluteDay.Value > CurrentDay))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.StaleWorldDay,
                "Political support history cannot extend into the future of the world timeline.");
            return false;
        }

        bool registered = politicalSupportStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryProposePoliticalSupportAdd(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportAddTransition transition,
        out PoliticalSupportFailure failure)
    {
        return politicalSupportStore.TryProposeAdd(
            record,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSupportAdd(
        PoliticalSupportAddTransition transition,
        out PoliticalSupportFailure failure)
    {
        bool applied = politicalSupportStore.TryApplyAdd(transition, CurrentDay, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposePoliticalSupportEnd(
        PoliticalSupportRelationId relationId,
        out PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        return politicalSupportStore.TryProposeEnd(
            relationId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSupportEnd(
        PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        bool applied = politicalSupportStore.TryApplyEnd(transition, CurrentDay, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryRecordPoliticalKnowledge(
        PoliticalKnowledgeHolder holder,
        PoliticalKnowledgeObservation observation,
        out PoliticalKnowledgeFailure failure)
    {
        return politicalKnowledgeStore.TryRecordObservation(
            holder,
            observation,
            CurrentDay,
            out failure);
    }

    public bool TryRegisterPoliticalKnowledgeHolder(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeFailure failure)
    {
        return politicalKnowledgeStore.TryRegisterHolder(
            holder,
            CurrentDay,
            out failure);
    }

    public bool TryGetPoliticalKnowledge(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeRuntime runtime)
    {
        return politicalKnowledgeStore.TryGet(holder, out runtime);
    }

    /// <summary>
    /// Registers an immutable political decision record in the world-owned
    /// decision history. Decision registration never executes its outcome.
    /// </summary>
    public bool TryRegisterPoliticalDecision(
        PoliticalDecisionRecord record,
        out PoliticalDecisionFailure failure)
    {
        failure = PoliticalDecisionFailure.None;
        if (record == null || record.DecisionId == null || record.Decider == null)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision with an id and decider is required.");
            return false;
        }

        if (record.DecisionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision cannot be registered in the future of the world timeline.");
            return false;
        }

        if (politicalKnowledgeStore.TryGet(record.Decider, out _) == false)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "The political decision decider must be registered as a knowledge holder in this world.");
            return false;
        }

        if (record.ExpectedWorldRevision != PoliticalWorldRevision
            || record.ExpectedKnowledgeRevision != PoliticalKnowledgeRevision)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.StaleDecision,
                "The political decision was captured against a stale world or knowledge revision.");
            return false;
        }

        if ((record.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                || record.DecisionKind == PoliticalDecisionKind.OfficeSelection)
            && (record.OfficeId == null || officeStore.TryGet(record.OfficeId, out _) == false))
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "An office decision must identify an office registered in this world.");
            return false;
        }

        if (record.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && (record.RecognizingInstitutionId == null
                || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false))
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A claim recognition decision must identify an institution registered in this world.");
            return false;
        }

        foreach (PersonId candidatePersonId in record.CandidatePersonIds)
        {
            if (candidatePersonId == null || personStore.TryGet(candidatePersonId, out _) == false)
            {
                failure = PoliticalDecisionFailure.Create(
                    PoliticalDecisionFailureCode.InvalidDecision,
                    "Every political decision candidate must be registered in this world.");
                return false;
            }
        }

        if (record.Outcome.ReferencedClaimId != null
            && politicalClaimStore.TryGet(record.Outcome.ReferencedClaimId, out _) == false)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision claim reference must be registered in this world.");
            return false;
        }

        return politicalDecisionStore.TryRegister(record, out failure);
    }

    public bool TryGetPoliticalDecision(
        PoliticalDecisionId decisionId,
        out PoliticalDecisionRecord record)
    {
        return politicalDecisionStore.TryGet(decisionId, out record);
    }

    public bool TryRegisterPropertyOwnership(
        PropertyOwnershipRecord record,
        out PropertyFoundationFailure failure)
    {
        if (record == null || record.OwnerPersonId == null)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.InvalidOwnershipRecord,
                "A property ownership record with a registered Person owner is required.");
            return false;
        }

        if (personStore.TryGet(record.OwnerPersonId, out _) == false)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.PersonNotRegistered,
                "The property owner PersonId must be registered in this world.");
            return false;
        }

        bool registered = propertyOwnershipStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetPropertiesOwnedBy(PersonId ownerPersonId)
    {
        return propertyOwnershipStore.GetOwnedBy(ownerPersonId);
    }

    public bool TryProposeEstateOpening(
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        transition = null;
        if (openingAbsoluteDay > CurrentDay)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "An estate cannot be opened in the future relative to the world day.");
            return false;
        }

        return EstateOpeningSystem.TryProposeOpening(
            personStore,
            estateStore,
            estateId,
            deceasedPersonId,
            openingAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyEstateOpening(
        EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        bool applied = EstateOpeningSystem.TryApplyOpening(
            personStore,
            estateStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryOpenEstate(
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateRecord estate,
        out EstateFoundationFailure failure)
    {
        estate = null;
        if (TryProposeEstateOpening(
                estateId,
                deceasedPersonId,
                openingAbsoluteDay,
                out EstateOpeningTransition transition,
                out failure) == false
            || TryApplyEstateOpening(transition, out failure) == false)
        {
            return false;
        }

        estateStore.TryGet(estateId, out estate);
        return estate != null;
    }

    public bool TryBuildSuccessionCandidates(
        PersonId subjectPersonId,
        out SuccessionCandidateSnapshot snapshot,
        out SuccessionCandidateQueryFailure failure)
    {
        SuccessionSubject subject = subjectPersonId == null
            ? null
            : new SuccessionSubject(subjectPersonId);
        return SuccessionCandidateSystem.TryBuildCandidates(
            personStore,
            genealogyStore,
            subject,
            CurrentDay,
            calendar,
            configuration.Population.MaturityAgeYears,
            out snapshot,
            out failure);
    }

    public bool TryProposePropertyTransfer(
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        transition = null;
        if (transferAbsoluteDay < 0L || transferAbsoluteDay > CurrentDay)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransferDay,
                "TransferAbsoluteDay must be within the current world timeline.");
            return false;
        }

        return PropertyTransferSystem.TryProposeTransfer(
            personStore,
            propertyOwnershipStore,
            propertyId,
            newOwnerPersonId,
            transferAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPropertyTransfer(
        PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        if (transition == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransition,
                "A valid property transfer transition is required.");
            return false;
        }

        if (transition.TransferAbsoluteDay < 0L
            || transition.TransferAbsoluteDay > CurrentDay)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransferDay,
                "TransferAbsoluteDay must be within the current world timeline.");
            return false;
        }

        bool applied = PropertyTransferSystem.TryApplyTransfer(
            personStore,
            propertyOwnershipStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryTransferProperty(
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyTransferFailure failure)
    {
        if (TryProposePropertyTransfer(
                propertyId,
                newOwnerPersonId,
                transferAbsoluteDay,
                out PropertyOwnershipTransferTransition transition,
                out failure) == false)
        {
            return false;
        }

        return TryApplyPropertyTransfer(transition, out failure);
    }

    public bool TryProposeOfficeSuccession(
        OfficeId officeId,
        PersonId selectedCandidateId,
        long startAbsoluteDay,
        out OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        return OfficeSuccessionSystem.TryPropose(
            this,
            officeId,
            selectedCandidateId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyOfficeSuccession(
        OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        bool applied = OfficeSuccessionSystem.TryApply(this, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposePoliticalOfficeSuccession(
        PoliticalDecisionId decisionId,
        OfficeId officeId,
        long startAbsoluteDay,
        out PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return PoliticalSuccessionSystem.TryPropose(
            this,
            decisionId,
            officeId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalOfficeSuccession(
        PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return PoliticalSuccessionSystem.TryApply(this, transition, out failure);
    }

    public bool TryProposePoliticalSuccession(
        PoliticalDecisionId decisionId,
        OfficeId officeId,
        long startAbsoluteDay,
        out PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return TryProposePoliticalOfficeSuccession(
            decisionId,
            officeId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSuccession(
        PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return TryApplyPoliticalOfficeSuccession(transition, out failure);
    }

    public bool TryProposeEstateSuccession(
        EstateId estateId,
        PropertyId propertyId,
        PersonId selectedCandidateId,
        long transferAbsoluteDay,
        out EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        return EstateSuccessionSystem.TryPropose(
            this,
            estateId,
            propertyId,
            selectedCandidateId,
            transferAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyEstateSuccession(
        EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        return EstateSuccessionSystem.TryApply(this, transition, out failure);
    }

    public bool TryRegisterOffice(
        OfficeRecord record,
        out InstitutionFoundationFailure failure)
    {
        bool registered = officeStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
    }

    public bool TryGetInstitution(
        InstitutionId institutionId,
        out InstitutionRecord record)
    {
        return institutionStore.TryGet(institutionId, out record);
    }

    public bool TryGetOffice(
        OfficeId officeId,
        out OfficeRecord record)
    {
        return officeStore.TryGet(officeId, out record);
    }

    public bool TryGetOfficeIncumbency(
        OfficeId officeId,
        out OfficeIncumbency incumbency)
    {
        return officeStore.TryGetIncumbency(officeId, out incumbency);
    }

    public bool TryGetCurrentOfficeIncumbent(
        OfficeId officeId,
        out PersonId incumbent)
    {
        return officeStore.TryGetCurrentIncumbent(officeId, out incumbent);
    }

    public bool IsOfficeVacant(OfficeId officeId)
    {
        return officeStore.IsVacant(officeId);
    }

    internal bool TryGetLatestClosedOfficeTenure(
        OfficeId officeId,
        out OfficeTenureRecord tenure)
    {
        return officeStore.TryGetLatestClosedTenure(officeId, out tenure);
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        out InstitutionFoundationFailure failure)
    {
        if (officeStore.TryGet(officeId, out _) == false
            || incumbent == null
            || (startAbsoluteDay.HasValue && startAbsoluteDay.Value < 0L)
            || officeStore.TryGetIncumbency(officeId, out _))
        {
            return officeStore.TryAssignIncumbent(
                officeId,
                incumbent,
                startAbsoluteDay,
                out failure);
        }

        if (personStore.TryGet(incumbent, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.PersonNotRegistered,
                "The incumbent PersonId must be registered in this world.");
            return false;
        }

        bool assigned = officeStore.TryAssignIncumbent(
            officeId,
            incumbent,
            startAbsoluteDay,
            out failure);
        if (assigned)
        {
            AdvancePoliticalWorldRevision();
        }

        return assigned;
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        out InstitutionFoundationFailure failure)
    {
        return TryAssignIncumbent(
            officeId,
            incumbent,
            CurrentDay,
            out failure);
    }

    public bool TryVacateOffice(
        OfficeId officeId,
        out InstitutionFoundationFailure failure)
    {
        bool vacated = officeStore.TryVacateOffice(officeId, out failure);
        if (vacated)
        {
            AdvancePoliticalWorldRevision();
        }

        return vacated;
    }

    public bool TryProposeInstitutionalVacancyRecognition(
        OfficeId officeId,
        InstitutionalVacancyRecognitionReason reason,
        out InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        if (reason == InstitutionalVacancyRecognitionReason.FactualDeath)
        {
            transition = null;
            if (officeStore.TryGetIncumbency(officeId, out OfficeIncumbency incumbency) == false)
            {
                failure = InstitutionalVacancyRecognitionFailure.StaleIncumbency;
                return false;
            }

            if (personStore.TryGet(incumbency.Incumbent, out PersonRuntime incumbent) == false)
            {
                failure = InstitutionalVacancyRecognitionFailure.IncumbentNotRegistered;
                return false;
            }

            if (incumbent.DeathAbsoluteDay.HasValue == false
                || incumbent.DeathAbsoluteDay.Value > CurrentDay)
            {
                failure = InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead;
                return false;
            }
        }

        return InstitutionalVacancyRecognitionSystem.TryPropose(
            officeStore,
            officeId,
            CurrentDay,
            reason,
            out transition,
            out failure);
    }

    public bool TryApplyInstitutionalVacancyRecognition(
        InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        bool applied = InstitutionalVacancyRecognitionSystem.TryApply(
            officeStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public IReadOnlyList<OfficeRecord> GetVacantOffices()
    {
        return officeStore.GetVacantOffices();
    }

    public IReadOnlyList<OfficeRecord> GetOfficesForInstitution(
        InstitutionId institutionId)
    {
        return officeStore.GetOfficesForInstitution(institutionId);
    }

    public IReadOnlyList<OfficeRecord> GetOfficesHeldBy(PersonId personId)
    {
        return officeStore.GetOfficesHeldBy(personId);
    }

    public bool TryProposeNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryProposeNamedBirth(
            this,
            settlement,
            personId,
            out transition,
            out failure);
    }

    public bool TryProposeNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        System.Collections.Generic.IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryProposeNamedBirth(
            this,
            settlement,
            personId,
            parentIds,
            out transition,
            out failure);
    }

    public bool TryApplyNamedBirth(
        PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryApplyNamedBirth(this, transition, out failure);
    }

    public bool TryApplyNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryApplyNamedBirth(
            this,
            settlement,
            personId,
            out transition,
            out failure);
    }

    public bool TryApplyNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        System.Collections.Generic.IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        bool applied = PersonBirthLifecycleSystem.TryApplyNamedBirth(
            this,
            settlement,
            personId,
            parentIds,
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryAddParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        bool added = PersonGenealogySystem.TryAddParentage(this, parentId, childId, out failure);
        if (added)
        {
            AdvancePoliticalWorldRevision();
        }

        return added;
    }

    public bool TryRemoveParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        bool removed = PersonGenealogySystem.TryRemoveParentage(this, parentId, childId, out failure);
        if (removed)
        {
            AdvancePoliticalWorldRevision();
        }

        return removed;
    }

    public bool ContainsParentage(PersonId parentId, PersonId childId)
    {
        return genealogyStore.ContainsParentage(parentId, childId);
    }

    public IReadOnlyList<PersonId> GetGenealogyParents(PersonId childId)
    {
        return genealogyStore.GetParents(childId);
    }

    public IReadOnlyList<PersonId> GetGenealogyChildren(PersonId parentId)
    {
        return genealogyStore.GetChildren(parentId);
    }

    public IReadOnlyList<PersonId> GetGenealogyAncestors(PersonId personId)
    {
        return genealogyStore.GetAncestors(personId);
    }

    public IReadOnlyList<PersonId> GetGenealogyDescendants(PersonId personId)
    {
        return genealogyStore.GetDescendants(personId);
    }

    public bool IsGenealogyDirectParent(PersonId parentId, PersonId childId)
    {
        return genealogyStore.IsDirectParent(parentId, childId);
    }

    public bool IsGenealogyAncestorOf(PersonId ancestorId, PersonId descendantId)
    {
        return genealogyStore.IsAncestorOf(ancestorId, descendantId);
    }

    public bool TryMaterializePerson(
        PersonId personId,
        NpcData npcData,
        string runtimeId,
        CityRuntime startingCity,
        float initialMoney,
        out NpcRuntime npcRuntime,
        out PersonMaterializationFailure failure)
    {
        return PersonMaterializationSystem.TryMaterializePerson(
            this,
            personId,
            npcData,
            runtimeId,
            startingCity,
            initialMoney,
            out npcRuntime,
            out failure);
    }

    public bool TryBindExistingNpcToPerson(
        PersonId personId,
        string npcRuntimeId,
        out PersonMaterializationFailure failure)
    {
        return PersonMaterializationSystem.TryBindExistingNpcToPerson(
            this,
            personId,
            npcRuntimeId,
            out failure);
    }

    public bool TryBindExistingPersonResident(
        PersonId personId,
        CityRuntime settlement,
        out PersonResidenceMembershipFailure failure)
    {
        if (personId == null || personStore.TryGet(personId, out PersonRuntime person) == false)
        {
            failure = PersonResidenceMembershipFailure.PersonNotRegistered;
            return false;
        }

        return PersonResidenceMembershipSystem.TryBindExistingResident(
            person,
            settlement,
            this,
            out failure);
    }

    /// <summary>
    /// Creates a fresh immutable authoritative roster from the world-owned registry.
    /// Callers never provide the source collection.
    /// </summary>
    public AuthoritativeNpcRoster GetAuthoritativeNpcRoster()
    {
        return new AuthoritativeNpcRoster(npcRuntimeSnapshot);
    }

    public bool TryApplyImmigration(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        bool applied = NpcPopulationLifecycleSystem.TryApplyImmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyEmigration(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        bool applied = NpcPopulationLifecycleSystem.TryApplyEmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyResidentDeath(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (npcRuntime?.BoundPersonRuntime != null)
        {
            bool applied = NpcPopulationLifecycleSystem.TryApplyResidentPersonDeath(
                this,
                npcRuntime,
                settlement,
                GetAuthoritativeNpcRoster(),
                out transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
            }

            return applied;
        }

        bool residentDeathApplied = NpcPopulationLifecycleSystem.TryApplyResidentDeath(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (residentDeathApplied)
        {
            AdvancePoliticalWorldRevision();
        }

        return residentDeathApplied;
    }

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        if (context == null)
        {
            return false;
        }

        if (expeditionSystem != null)
        {
            foreach (ActionExecutionParticipant participant in context.Participants)
            {
                if (participant != null && IsDeadNpc(participant.RuntimeId) == true)
                {
                    return false;
                }

                if (participant != null
                    && expeditionSystem.IsNpcOnActiveExpedition(participant.RuntimeId) == true)
                {
                    return false;
                }
            }
        }

        return travelPartySystem != null && travelPartySystem.TryStartTravelParty(context);
    }

    public void AdvanceDay()
    {
        simulationTime.AdvanceDay();
        placeContentStore?.AdvanceDays(1);
        logger?.BeginDay(CurrentDay);
        lastDailyDemographyReport = DailyDemographicSystem.Advance(
            this,
            naturalMortalitySamples,
            aggregateDemographyProvider);
        BeginSimulationDay();
        scheduledDirectiveSystem?.PrepareDay(CurrentDay);

        if (configuration.Economy.Enabled == true)
        {
            SimulateEconomyDay();
        }

        RefreshLocalKnowledgeAndShare();
        adventureExpeditionAutonomySystem?.AdvanceActiveExpeditions();

        if (adventureExpeditionAutonomySystem != null)
        {
            foreach (NpcRuntime npcRuntime in npcRuntimes)
            {
                if (npcRuntime != null
                    && npcRuntime.IsAlive
                    && npcRuntime.IsTraveling == false
                    && (expeditionSystem == null || expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == false))
                {
                    adventureExpeditionAutonomySystem.TryStartAutonomousExpedition(npcRuntime, npcRuntimes);
                }
            }
        }

        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime == null)
            {
                continue;
            }

            if (npcRuntime.IsAlive == false)
            {
                continue;
            }

            if (npcRuntime.IsTraveling == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                continue;
            }

            if (expeditionSystem != null
                && expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                continue;
            }

            if (adventureExpeditionAutonomySystem != null
                && adventureExpeditionAutonomySystem.IsReservedToday(npcRuntime.RuntimeId))
            {
                continue;
            }

            EvaluateStatus(npcRuntime);
            merchantSystem?.AdvanceNpcTradeState(npcRuntime);

            if (TryProcessScheduledDirective(npcRuntime) == true)
            {
                continue;
            }

            EvaluateAction(npcRuntime);
            TryExecuteCurrentAction(npcRuntime);
        }

        List<NpcRuntime> arrivedNpcs = new List<NpcRuntime>();

        if (travelPartySystem != null)
        {
            arrivedNpcs.AddRange(travelPartySystem.AdvanceParties());
        }

        if (travelSystem != null)
        {
            arrivedNpcs.AddRange(travelSystem.AdvanceTravels(npcRuntimes));
        }

        foreach (NpcRuntime arrivedNpc in arrivedNpcs)
        {
            ObserveArrivedExplorableSites(arrivedNpc);

            if (arrivedNpc?.CurrentCity != null)
            {
                merchantSystem?.ObserveCurrentMarket(arrivedNpc);
            }
        }

        expeditionSystem?.ReconcileAfterTravel(arrivedNpcs);
    }

    internal GenealogyStore GenealogyStoreForWorldBoundary => genealogyStore;

    internal InstitutionStore InstitutionStoreForWorldBoundary => institutionStore;

    internal OfficeStore OfficeStoreForWorldBoundary => officeStore;

    private bool TryValidatePoliticalClaimTarget(
        PoliticalClaimRecord record,
        out PoliticalClaimFailure failure)
    {
        failure = PoliticalClaimFailure.None;
        if (record.Target == null
            || PoliticalClaimRecord.IsTargetCompatible(record.ClaimType, record.Target.Kind) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetTypeMismatch,
                "The political claim target kind does not match the claim type.");
            return false;
        }

        bool found;
        switch (record.Target.Kind)
        {
            case PoliticalClaimTargetKind.Office:
                found = officeStore.TryGet(new OfficeId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Property:
                found = propertyOwnershipStore.TryGet(new PropertyId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Institution:
                found = institutionStore.TryGet(new InstitutionId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Person:
                found = personStore.TryGet(new PersonId(record.Target.TargetId), out _);
                break;
            default:
                found = false;
                break;
        }

        if (found == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetNotFound,
                "The political claim target must exist in this world.");
            return false;
        }

        return true;
    }

    private bool TryValidatePoliticalClaimRecognition(
        PoliticalClaimRecord record,
        out PoliticalClaimFailure failure)
    {
        failure = PoliticalClaimFailure.None;
        if (record.RecognitionState == PoliticalClaimRecognitionState.Unrecognized)
        {
            return true;
        }

        if (record.RecognizingInstitutionId == null
            || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetNotFound,
                "The political claim recognizing institution must exist in this world.");
            return false;
        }

        if (record.RecognitionAbsoluteDay.HasValue == false
            || record.RecognitionAbsoluteDay.Value > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "Claim recognition must be within the current world timeline.");
            return false;
        }

        return true;
    }

    private static void ValidateGenealogyStore(PersonStore persons, GenealogyStore genealogy)
    {
        foreach (ParentageRecord record in genealogy.Records)
        {
            if (record == null
                || persons.TryGet(record.ParentId, out _) == false
                || persons.TryGet(record.ChildId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime GenealogyStore contains parentage endpoints absent from PersonStore.",
                    nameof(genealogy));
            }
        }
    }

    private static GenealogyStore CloneGenealogyStore(GenealogyStore source)
    {
        GenealogyStore copy = new GenealogyStore();
        foreach (ParentageRecord record in source.Records)
        {
            if (copy.TryAddParentage(record, out GenealogyFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime GenealogyStore contains an invalid parentage record.",
                    nameof(source));
            }
        }

        return copy;
    }

    private static PoliticalClaimStore ClonePoliticalClaimStore(
        PoliticalClaimStore source,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PropertyOwnershipStore propertyOwnershipStore,
        long currentDay)
    {
        PoliticalClaimStore copy = new PoliticalClaimStore();
        if (source == null)
        {
            return copy;
        }

        foreach (PoliticalClaimRecord record in source.Records)
        {
            if (record == null
                || record.CreatedAbsoluteDay > currentDay
                || personStore.TryGet(record.ClaimantPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains a claim inconsistent with PersonStore or world time.",
                    nameof(source));
            }

            bool targetExists;
            switch (record.Target.Kind)
            {
                case PoliticalClaimTargetKind.Office:
                    targetExists = officeStore.TryGet(new OfficeId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Property:
                    targetExists = propertyOwnershipStore.TryGet(new PropertyId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Institution:
                    targetExists = institutionStore.TryGet(new InstitutionId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Person:
                    targetExists = personStore.TryGet(new PersonId(record.Target.TargetId), out _);
                    break;
                default:
                    targetExists = false;
                    break;
            }

            if (targetExists == false
                || (record.RecognitionAbsoluteDay.HasValue && record.RecognitionAbsoluteDay.Value > currentDay)
                || (record.ResolutionAbsoluteDay.HasValue && record.ResolutionAbsoluteDay.Value > currentDay)
                || (record.RecognitionState != PoliticalClaimRecognitionState.Unrecognized
                    && (record.RecognizingInstitutionId == null
                        || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false)))
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains a claim target or recognition state inconsistent with the world.",
                    nameof(source));
            }

            if (copy.TryRegister(record, out PoliticalClaimFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains an invalid claim: " + failure + ".",
                    nameof(source));
            }
        }

        if (copy.Count != source.Count)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalClaimStore was not copied completely.",
                nameof(source));
        }

        return source.Clone();
    }

    private static FactionStore CloneFactionStore(
        FactionStore source,
        PersonStore personStore,
        long currentDay)
    {
        FactionStore copy = new FactionStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (FactionRecord faction in source.Factions)
        {
            if (faction == null || faction.CreatedAbsoluteDay > currentDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains a faction inconsistent with world time.",
                    nameof(source));
            }

            if (copy.TryRegister(faction, out FactionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an invalid faction: " + failure + ".",
                    nameof(source));
            }
        }

        foreach (FactionAffiliationRecord affiliation in source.Affiliations)
        {
            if (affiliation == null
                || source.TryGet(affiliation.FactionId, out FactionRecord faction) == false
                || personStore.TryGet(affiliation.PersonId, out _) == false
                || faction.CreatedAbsoluteDay > affiliation.JoinedAbsoluteDay
                || affiliation.JoinedAbsoluteDay > currentDay
                || (affiliation.EndedAbsoluteDay.HasValue && affiliation.EndedAbsoluteDay.Value > currentDay))
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an affiliation inconsistent with world truth or time.",
                    nameof(source));
            }

            if (copy.TryRegisterAffiliation(affiliation, out FactionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an invalid affiliation: " + failure + ".",
                    nameof(source));
            }
        }

        if (copy.Count != source.Count || copy.AffiliationCount != source.AffiliationCount)
        {
            throw new ArgumentException(
                "The SimulationRuntime FactionStore was not copied completely.",
                nameof(source));
        }

        return source.Clone(personStore);
    }

    private static PoliticalSupportStore ClonePoliticalSupportStore(
        PoliticalSupportStore source,
        PersonStore personStore,
        FactionStore factionStore,
        PoliticalClaimStore politicalClaimStore,
        long currentDay)
    {
        PoliticalSupportStore empty = new PoliticalSupportStore(
            personStore,
            factionStore,
            politicalClaimStore);
        if (source == null)
        {
            return empty;
        }

        PoliticalSupportStore validation = new PoliticalSupportStore(
            personStore,
            factionStore,
            politicalClaimStore);
        foreach (PoliticalSupportRelationRecord record in source.Records)
        {
            if (record == null
                || record.StartedAbsoluteDay > currentDay
                || (record.EndedAbsoluteDay.HasValue && record.EndedAbsoluteDay.Value > currentDay)
                || validation.TryRegister(record, out PoliticalSupportFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalSupportStore contains an invalid relation or future history.",
                    nameof(source));
            }
        }

        return source.Clone(personStore, factionStore, politicalClaimStore);
    }

    private static PoliticalKnowledgeStore ClonePoliticalKnowledgeStore(
        PoliticalKnowledgeStore source,
        PersonStore personStore,
        InstitutionStore institutionStore,
        long currentDay,
        PoliticalClaimStore politicalClaimStore,
        FactionStore factionStore,
        OfficeStore officeStore)
    {
        if (source == null)
        {
            return new PoliticalKnowledgeStore(
                personStore,
                institutionStore,
                politicalClaimStore,
                factionStore,
                officeStore);
        }

        return source.Clone(
            personStore,
            institutionStore,
            currentDay,
            politicalClaimStore,
            factionStore,
            officeStore);
    }

    private static PoliticalDecisionStore ClonePoliticalDecisionStore(
        PoliticalDecisionStore source,
        PoliticalKnowledgeStore knowledgeStore,
        long currentDay,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PoliticalClaimStore politicalClaimStore)
    {
        PoliticalDecisionStore copy = new PoliticalDecisionStore();
        if (source == null)
        {
            return copy;
        }

        foreach (PoliticalDecisionRecord record in source.Records)
        {
            if (record == null
                || record.DecisionAbsoluteDay > currentDay
                || record.Decider == null
                || knowledgeStore.TryGet(record.Decider, out _) == false
                || HasUnregisteredPoliticalDecisionReference(
                    record,
                    personStore,
                    institutionStore,
                    officeStore,
                    politicalClaimStore)
                || copy.TryRegister(record, out PoliticalDecisionFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalDecisionStore contains an invalid decision, future history, or unregistered decider.",
                    nameof(source));
            }
        }

        if (copy.Revision != source.Revision)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalDecisionStore revision is inconsistent with its state.",
                nameof(source));
        }

        return copy;
    }

    private static long ResolveInitialPoliticalWorldRevision(PoliticalDecisionStore source)
    {
        long revision = 0L;
        if (source == null)
        {
            return revision;
        }

        foreach (PoliticalDecisionRecord record in source.Records)
        {
            if (record != null && record.ExpectedWorldRevision > revision)
            {
                revision = record.ExpectedWorldRevision;
            }
        }

        return revision;
    }

    private static bool HasUnregisteredPoliticalDecisionReference(
        PoliticalDecisionRecord record,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PoliticalClaimStore politicalClaimStore)
    {
        if (record == null || record.Outcome == null || record.CandidatePersonIds == null)
        {
            return true;
        }

        foreach (PersonId candidate in record.CandidatePersonIds)
        {
            if (candidate == null || personStore.TryGet(candidate, out _) == false)
            {
                return true;
            }
        }

        if ((record.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                || record.DecisionKind == PoliticalDecisionKind.OfficeSelection)
            && (record.OfficeId == null || officeStore.TryGet(record.OfficeId, out _) == false))
        {
            return true;
        }

        if (record.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && (record.RecognizingInstitutionId == null
                || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false))
        {
            return true;
        }

        return record.Outcome.ReferencedClaimId != null
            && politicalClaimStore.TryGet(record.Outcome.ReferencedClaimId, out _) == false;
    }

    private void AdvancePoliticalWorldRevision()
    {
        if (politicalWorldRevision < long.MaxValue)
        {
            politicalWorldRevision++;
        }
    }

    private long GetEffectivePoliticalWorldRevision()
    {
        unchecked
        {
            long fingerprint = 17L;
            List<PersonRuntime> people = new List<PersonRuntime>(personStore.Persons);
            people.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.PersonId?.Value,
                right?.PersonId?.Value));
            foreach (PersonRuntime person in people)
            {
                AppendStableString(ref fingerprint, person?.PersonId?.Value);
                fingerprint = fingerprint * 31L
                    + (person != null && person.DeathAbsoluteDay.HasValue
                        ? person.DeathAbsoluteDay.Value
                        : -1L);
            }

            foreach (ParentageRecord parentage in genealogyStore.Records)
            {
                AppendStableString(ref fingerprint, parentage?.ParentId?.Value);
                AppendStableString(ref fingerprint, parentage?.ChildId?.Value);
            }

            fingerprint = fingerprint * 31L + propertyOwnershipStore.Revision;
            fingerprint = fingerprint * 31L + estateStore.Revision;
            long result = (politicalWorldRevision * 397L) ^ fingerprint;
            return result & long.MaxValue;
        }
    }

    private static void AppendStableString(ref long hash, string value)
    {
        unchecked
        {
            hash = hash * 31L + (value == null ? 0L : value.Length);
            if (value != null)
            {
                foreach (char character in value)
                {
                    hash = hash * 31L + character;
                }
            }
        }
    }

    private static InstitutionStore ResolveInstitutionStore(
        InstitutionStore institutionStore,
        OfficeStore officeStore)
    {
        if (institutionStore != null
            && officeStore != null
            && ReferenceEquals(
                institutionStore,
                officeStore.InstitutionStoreForWorldBoundary) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime InstitutionStore and OfficeStore must belong to the same store pair.",
                nameof(officeStore));
        }

        InstitutionStore source = institutionStore
            ?? officeStore?.InstitutionStoreForWorldBoundary;
        return CloneInstitutionStore(source ?? new InstitutionStore());
    }

    private static InstitutionStore CloneInstitutionStore(InstitutionStore source)
    {
        InstitutionStore copy = new InstitutionStore();
        foreach (InstitutionRecord record in source.Institutions)
        {
            InstitutionRecord clone = new InstitutionRecord(
                new InstitutionId(record.Id.Value),
                record.DisplayName);
            if (copy.TryRegister(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime InstitutionStore contains an invalid institution record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        return copy;
    }

    private static OfficeStore CloneOfficeStore(
        OfficeStore source,
        InstitutionStore institutionStore,
        PersonStore personStore)
    {
        OfficeStore copy = new OfficeStore(institutionStore);
        if (source == null)
        {
            return copy;
        }

        foreach (OfficeRecord record in source.Offices)
        {
            OfficeRecord clone = new OfficeRecord(
                new OfficeId(record.Id.Value),
                new InstitutionId(record.InstitutionId.Value),
                record.DisplayName);
            if (copy.TryRegister(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an invalid office record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (OfficeIncumbency incumbency in source.Incumbencies)
        {
            if (incumbency == null
                || personStore.TryGet(incumbency.Incumbent, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an incumbent absent from PersonStore.",
                    nameof(source));
            }

            if (copy.TryAssignIncumbent(
                    new OfficeId(incumbency.OfficeId.Value),
                    new PersonId(incumbency.Incumbent.Value),
                    incumbency.StartAbsoluteDay,
                    out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an invalid incumbency: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (OfficeTenureRecord tenure in source.TenureHistoryInMutationOrder)
        {
            if (tenure == null || tenure.IsOpen)
            {
                continue;
            }

            OfficeTenureRecord clone = new OfficeTenureRecord(
                new OfficeId(tenure.OfficeId.Value),
                new PersonId(tenure.Incumbent.Value),
                tenure.StartAbsoluteDay,
                tenure.EndAbsoluteDay,
                tenure.EndReason,
                true);
            if (copy.TryAddHistoricalTenure(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains invalid historical tenure: "
                    + failure + ".",
                    nameof(source));
            }
        }

        return copy;
    }

    private static PropertyOwnershipStore ClonePropertyOwnershipStore(
        PropertyOwnershipStore source,
        PersonStore personStore,
        long currentDay)
    {
        if (source != null
            && source.PersonStoreForWorldBoundary != null
            && ReferenceEquals(source.PersonStoreForWorldBoundary, personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime PropertyOwnershipStore must belong to the resolved PersonStore.",
                nameof(source));
        }

        PropertyOwnershipStore copy = new PropertyOwnershipStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (PropertyOwnershipRecord record in source.Records)
        {
            if (record == null
                || record.OwnerPersonId == null
                || personStore.TryGet(record.OwnerPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains an owner absent from PersonStore.",
                    nameof(source));
            }

            PropertyOwnershipRecord clone = new PropertyOwnershipRecord(
                new PropertyId(record.PropertyId.Value),
                new PersonId(record.OwnerPersonId.Value));
            if (copy.TryRegister(clone, out PropertyFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains an invalid record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (PropertyOwnershipTransferHistoryRecord history in source.TransferHistory)
        {
            if (history == null
                || history.PropertyId == null
                || history.PreviousOwnerPersonId == null
                || history.NewOwnerPersonId == null)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains invalid transfer history.",
                    nameof(source));
            }

            if (history.TransferAbsoluteDay > currentDay
                || source.TryGet(history.PropertyId, out _) == false
                || personStore.TryGet(history.PreviousOwnerPersonId, out _) == false
                || personStore.TryGet(history.NewOwnerPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains transfer history inconsistent with the world.",
                    nameof(source));
            }

            PropertyOwnershipTransferHistoryRecord clone =
                new PropertyOwnershipTransferHistoryRecord(
                    new PropertyId(history.PropertyId.Value),
                    new PersonId(history.PreviousOwnerPersonId.Value),
                    new PersonId(history.NewOwnerPersonId.Value),
                    history.TransferAbsoluteDay);
            if (copy.TryAddHistoricalTransfer(
                    clone,
                    out PropertyTransferFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains invalid transfer history: "
                    + failure + ".",
                    nameof(source));
            }
        }

        if (copy.Revision != source.Revision)
        {
            throw new ArgumentException(
                "The SimulationRuntime PropertyOwnershipStore revision is inconsistent with its state.",
                nameof(source));
        }

        return copy;
    }

    private static EstateStore CloneEstateStore(
        EstateStore source,
        PersonStore personStore,
        long currentDay)
    {
        if (source != null
            && ReferenceEquals(source.PersonStoreForWorldBoundary, personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime EstateStore must belong to the resolved PersonStore.",
                nameof(source));
        }

        EstateStore copy = new EstateStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (EstateRecord record in source.Records)
        {
            if (record == null
                || personStore.TryGet(record.DeceasedPersonId, out PersonRuntime deceased) == false
                || deceased.DeathAbsoluteDay.HasValue == false
                || record.OpenedAbsoluteDay < deceased.DeathAbsoluteDay.Value
                || record.OpenedAbsoluteDay > currentDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime EstateStore contains an estate inconsistent with PersonStore or world time.",
                    nameof(source));
            }

            EstateRecord clone = new EstateRecord(
                new EstateId(record.EstateId.Value),
                new PersonId(record.DeceasedPersonId.Value),
                record.OpenedAbsoluteDay);
            if (copy.TryRegister(clone, out EstateFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime EstateStore contains an invalid record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        return copy;
    }

    public void AdvanceDays(int dayCount)
    {
        if (dayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount), dayCount, "dayCount cannot be negative.");
        }

        for (int i = 0; i < dayCount; i++)
        {
            AdvanceDay();
        }
    }

    private void BeginSimulationDay()
    {
        if (justiceSystem != null)
        {
            justiceSystem.BeginDay();
        }

        if (crimeSystem != null)
        {
            crimeSystem.AdvanceHiddenStatuses(npcRuntimes);
        }

        if (configuration.GuardCrime.Enabled == true && justiceSystem != null)
        {
            justiceSystem.AdvanceSentences(npcRuntimes);
        }

        if (justiceSystem != null)
        {
            justiceSystem.SyncWantedStatuses(npcRuntimes);
        }

        AdvanceMerchantPlanUrgency();
    }

    private void AdvanceMerchantPlanUrgency()
    {
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime == null
                || npcRuntime.IsAlive == false
                || npcRuntime.IsTraveling == true
                || npcRuntime.CurrentCity == null)
            {
                continue;
            }

            MerchantTradePlanRuntime tradePlan = npcRuntime.MerchantTradePlan;

            if (tradePlan.IsActive == true
                && tradePlan.TargetCity != null
                && tradePlan.TargetCity != npcRuntime.CurrentCity)
            {
                tradePlan.IncrementPendingTravelDay();
            }
        }
    }

    private void SimulateEconomyDay()
    {
        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime != null)
            {
                cityRuntime.SimulateProductionDay();
            }
        }

        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            cityRuntime.SimulateConsumptionDay();
            cityRuntime.UpdateMarketPrices();
        }
    }

    private void RefreshLocalKnowledgeAndShare()
    {
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime == null || npcRuntime.IsAlive == false || npcRuntime.CurrentLocation == null || npcRuntime.IsTraveling == true)
            {
                continue;
            }

            npcRuntime.SpatialKnowledge.DiscoverLocation(npcRuntime.CurrentLocation.RuntimeId);

            if (npcRuntime.CurrentCity != null)
            {
                merchantSystem?.ObserveCurrentMarket(npcRuntime);
            }
        }

        commercialKnowledgeSharingSystem?.ShareAmongPresentMerchants(npcRuntimes);
    }

    private bool IsDeadNpc(string runtimeId)
    {
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime != null && string.Equals(npcRuntime.RuntimeId, runtimeId, StringComparison.Ordinal) == true)
            {
                return npcRuntime.IsDead;
            }
        }

        return false;
    }

    private void ObserveArrivedExplorableSites(NpcRuntime npcRuntime)
    {
        if (npcRuntime?.CurrentLocation == null
            || explorableSiteStore == null
            || explorableSiteKnowledgeSystem == null)
        {
            return;
        }

        foreach (ExplorableSiteRuntime siteRuntime in explorableSiteStore.GetForLocation(npcRuntime.CurrentLocation))
        {
            explorableSiteKnowledgeSystem.RecordDirectObservation(
                npcRuntime,
                siteRuntime,
                CurrentDay);
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        if (npcDecisionSystem == null)
        {
            return;
        }

        NpcActionRuntime chosenAction = npcDecisionSystem.ChooseAction(npcRuntime, configuredActions);
        decisionRecorder?.RecordChosenAction(npcRuntime, chosenAction, NpcDecisionOrigin.Autonomous);
        npcRuntime.SetCurrentActionRuntime(chosenAction);
    }

    private NpcActionResult TryExecuteCurrentAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return null;
        }

        LogChosenTargetAction(npcRuntime, actionRuntime);
        NpcActionResult actionResult = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionResult != null && string.IsNullOrEmpty(actionResult.Message) == false)
        {
            logger?.Log(SimulationLogCategory.NpcAction, actionResult.Message);
        }

        if (actionResult != null && actionResult.Success == true)
        {
            ApplySuccessStatusChanges(npcRuntime, actionRuntime, action);
        }

        return actionResult;
    }

    private bool TryProcessScheduledDirective(NpcRuntime npcRuntime)
    {
        if (scheduledDirectiveSystem == null
            || scheduledDirectiveSystem.TryTakeDirective(npcRuntime, out ScheduledDirective directive) == false)
        {
            return false;
        }

        npcRuntime.SetCurrentActionRuntime(null);

        if (directive.Mode == ScheduledDirectiveMode.RequestAction)
        {
            ProcessRequestedActionDirective(npcRuntime, directive);
        }
        else if (directive.Mode == ScheduledDirectiveMode.ForceOutcome)
        {
            ProcessForcedOutcomeDirective(npcRuntime, directive);
        }
        else
        {
            SkipDirective(directive, "Directive mode is not supported.");
        }

        return true;
    }

    private void ProcessRequestedActionDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        NpcActionRuntime requestedAction = npcDecisionSystem != null
            ? npcDecisionSystem.CreateRequestedAction(npcRuntime, directive.Action)
            : null;

        if (requestedAction == null)
        {
            SkipDirective(directive, "Actor is not in a compatible state for the requested action.");
            return;
        }

        decisionRecorder?.RecordChosenAction(npcRuntime, requestedAction, NpcDecisionOrigin.ScheduledDirective);
        npcRuntime.SetCurrentActionRuntime(requestedAction);
        NpcActionResult result = TryExecuteCurrentAction(npcRuntime);

        if (result != null && result.Success == true)
        {
            directive.MarkSucceeded(CurrentDay);
            return;
        }

        string reason = result != null && string.IsNullOrEmpty(result.Message) == false
            ? result.Message
            : "Requested action was attempted and failed.";
        directive.MarkFailed(CurrentDay, reason);
    }

    private void ProcessForcedOutcomeDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        if (directive.Operation != ScheduledDirectiveOperation.EscapePrison || justiceSystem == null)
        {
            SkipDirective(directive, "Escape domain operation is unavailable.");
            return;
        }

        if (justiceSystem.IsArrested(npcRuntime) == false)
        {
            SkipDirective(directive, "Actor is not arrested; escape outcome is incompatible with current state.");
            return;
        }

        CrimeActionSettings settings = directive.Action != null && directive.Action.crimeSettings != null
            ? directive.Action.crimeSettings
            : new CrimeActionSettings();

        npcRuntime.SetCurrentActionRuntime(new NpcActionRuntime(directive.Action));

        if (justiceSystem.ApplyEscapeSuccess(npcRuntime, settings.escapeBountyPenalty) == false)
        {
            directive.MarkFailed(CurrentDay, "Canonical escape transition rejected the forced outcome.");
            return;
        }

        ApplySuccessStatusChanges(npcRuntime, npcRuntime.CurrentActionRuntime, directive.Action);
        directive.MarkSucceeded(CurrentDay);
    }

    private void SkipDirective(ScheduledDirective directive, string reason)
    {
        if (directive == null)
        {
            return;
        }

        directive.MarkSkipped(CurrentDay, reason);
        logger?.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
    }

    private NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        INpcActionProvider actionProvider = action.actionType == NpcActionType.Normal
            ? null
            : npcDecisionSystem?.GetProviderForAction(action);

        if (action.actionType != NpcActionType.Normal && actionProvider == null)
        {
            return NpcActionResult.Failed();
        }

        if (RollActionSuccess(action, actionRuntime) == false)
        {
            if (actionProvider is INpcActionFailureHandler failureHandler)
            {
                NpcActionResult failureResult = failureHandler.HandleActionFailure(npcRuntime, actionRuntime);

                if (failureResult != null)
                {
                    return failureResult;
                }
            }

            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        }

        if (action.actionType == NpcActionType.Normal)
        {
            return NpcActionResult.Succeeded(CreateNormalActionMessage(npcRuntime, action));
        }

        return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
    }

    private bool RollActionSuccess(NpcActionData action, NpcActionRuntime actionRuntime)
    {
        if (action == null || action.canFail == false)
        {
            return true;
        }

        float contextualMultiplier = actionRuntime != null ? actionRuntime.SuccessChanceMultiplier : 1f;
        float effectiveChance = Mathf.Clamp01(action.baseSuccessChance * Mathf.Max(0f, contextualMultiplier));
        return UnityEngine.Random.value <= effectiveChance;
    }

    private void ApplySuccessStatusChanges(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        ApplyStatusChanges(npcRuntime, action.statusToRemove, action.statusToAdd);

        if (actionRuntime != null && actionRuntime.TargetNpc != null)
        {
            ApplyStatusChanges(actionRuntime.TargetNpc, action.targetStatusToRemove, action.targetStatusToAdd);
        }
    }

    private void ApplyStatusChanges(NpcRuntime npcRuntime, List<NpcStatusData> statusToRemove, List<NpcStatusData> statusToAdd)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (statusToRemove != null)
        {
            foreach (NpcStatusData status in statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (statusToAdd != null)
        {
            foreach (NpcStatusData status in statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
    }

    private void LogChosenTargetAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null)
        {
            return;
        }

        if (actionRuntime.TargetNpc != null)
        {
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetNpc.NpcName}.");
        }
        else if (actionRuntime.TargetCity != null)
        {
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetCity.CityName}.");
        }
    }

    private string CreateFailureMessage(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
        string targetName = actionRuntime != null && actionRuntime.TargetNpc != null ? $" {actionRuntime.TargetNpc.NpcName}" : string.Empty;
        string targetCityName = actionRuntime != null && actionRuntime.TargetCity != null ? $" {actionRuntime.TargetCity.CityName}" : string.Empty;
        return $"{actorName} tentou {GetActionName(action)}{targetName}{targetCityName}, mas falhou.";
    }

    private string CreateNormalActionMessage(NpcRuntime npcRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";

        if (action != null && string.IsNullOrEmpty(action.normalActionLogText) == false)
        {
            return $"{actorName} {action.normalActionLogText}";
        }

        return $"{actorName} realizou {GetActionName(action)}.";
    }

    private string GetActionName(NpcActionData action)
    {
        if (action == null)
        {
            return "acao desconhecida";
        }

        return string.IsNullOrEmpty(action.actionName) == false ? action.actionName : action.actionType.ToString();
    }
}
