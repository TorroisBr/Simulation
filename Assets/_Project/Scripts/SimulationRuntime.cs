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
        IAggregateDemographyProvider aggregateDemographyProvider = null)
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

        this.configuration = resolvedConfiguration;
        this.calendar = new SimulationCalendar(
            calendarDefinition ?? CalendarDefinition.CreateDefault());
        this.naturalMortalitySamples = naturalMortalitySamples;
        this.aggregateDemographyProvider = aggregateDemographyProvider;
        this.personStore = resolvedPersonStore;
        this.genealogyStore = CloneGenealogyStore(resolvedGenealogyStore);
        this.institutionStore = resolvedInstitutionStore;
        this.officeStore = resolvedOfficeStore;
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

        return personStore.TryRegister(person, out failure);
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
        return PersonDeathLifecycleSystem.TryApplyDeath(this, transition, out failure);
    }

    public bool TryApplyPersonDeathWithConflictInjury(
        PersonDeathTransition transition,
        NpcInjurySeverity injurySeverity,
        out PersonDeathLifecycleFailure failure)
    {
        return PersonDeathLifecycleSystem.TryApplyDeathWithConflictInjury(
            this,
            transition,
            injurySeverity,
            out failure);
    }

    public bool TryApplyPersonDeath(
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        return PersonDeathLifecycleSystem.TryApplyDeath(
            this,
            personId,
            out transition,
            out failure);
    }

    public bool TryRegisterInstitution(
        InstitutionRecord record,
        out InstitutionFoundationFailure failure)
    {
        return institutionStore.TryRegister(record, out failure);
    }

    public bool TryRegisterOffice(
        OfficeRecord record,
        out InstitutionFoundationFailure failure)
    {
        return officeStore.TryRegister(record, out failure);
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

        return officeStore.TryAssignIncumbent(
            officeId,
            incumbent,
            startAbsoluteDay,
            out failure);
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
        return officeStore.TryVacateOffice(officeId, out failure);
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
        return PersonBirthLifecycleSystem.TryApplyNamedBirth(
            this,
            settlement,
            personId,
            parentIds,
            out transition,
            out failure);
    }

    public bool TryAddParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        return PersonGenealogySystem.TryAddParentage(this, parentId, childId, out failure);
    }

    public bool TryRemoveParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        return PersonGenealogySystem.TryRemoveParentage(this, parentId, childId, out failure);
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
        return NpcPopulationLifecycleSystem.TryApplyImmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
    }

    public bool TryApplyEmigration(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        return NpcPopulationLifecycleSystem.TryApplyEmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
    }

    public bool TryApplyResidentDeath(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (npcRuntime?.BoundPersonRuntime != null)
        {
            return NpcPopulationLifecycleSystem.TryApplyResidentPersonDeath(
                this,
                npcRuntime,
                settlement,
                GetAuthoritativeNpcRoster(),
                out transition,
                out failure);
        }

        return NpcPopulationLifecycleSystem.TryApplyResidentDeath(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
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
