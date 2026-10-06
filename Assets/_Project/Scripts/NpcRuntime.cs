using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class NpcRuntime : ICapabilityConditionSource
{
    [NonSerialized] private MutationGuardBinding runtimeMutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private long travelStateRevision;
    [NonSerialized] private long currentActionRevision;
    [NonSerialized] private long lifeStateRevision;
    [NonSerialized] private long residenceRevision;
    [NonSerialized] private long p12CrimeJusticeRevision;
    [NonSerialized] private ReadOnlyCollection<NpcStatusData> currentStatusView;
    [NonSerialized] private Func<NpcRuntime, bool> p12CrimeJusticeMutationAdmission;
    [NonSerialized] private Action<NpcRuntime> p12CrimeJusticeMutationCommitted;
    [NonSerialized] private Func<bool, IReadOnlyList<CityRuntime>, bool> p12TravelStateMutationAdmission;
    [NonSerialized] private Action<bool, IReadOnlyList<CityRuntime>> p12TravelStateMutationCommitted;
    [NonSerialized] private Func<NpcRuntime, bool, bool, bool, NpcActionRuntime, bool> p12LifecycleMutationAdmission;
    [NonSerialized] private Action<NpcRuntime, bool, bool, bool> p12LifecycleMutationCommitted;
    [SerializeField]private string runtimeId;
    [SerializeField]private string personIdValue;
    [NonSerialized]private PersonId personIdentity;
    [NonSerialized]private PersonRuntime personRuntime;
    [SerializeField]private NpcData npcData;
    [SerializeField]private string residenceSettlementRuntimeId;
	[SerializeField]private List<NpcStatusData> currentStatus = new List<NpcStatusData>();
	[SerializeField]private NpcActionData currentAction;
    [SerializeField]private NpcLifeState lifeState = NpcLifeState.Alive;
    [SerializeField]private NpcInjurySeverity injurySeverity = NpcInjurySeverity.None;
    [NonSerialized]private NpcActionRuntime currentActionRuntime;
    [SerializeField]private InventoryRuntime inventory = new InventoryRuntime();
    [SerializeField]private MoneyAccountRuntime moneyAccount = new MoneyAccountRuntime();
    [NonSerialized]private SpatialLocationRuntime currentLocation;
    [NonSerialized]private SpatialLocationRuntime destinationLocation;
    [NonSerialized]private CityRuntime currentCity;
    [NonSerialized]private CityRuntime destinationCity;
    [SerializeField]private int travelDaysRemaining;
    [SerializeField]private int travelDaysTotal;
    [SerializeField]private string travelRouteRuntimeId;
    [SerializeField]private bool travelStartedToday;
    [SerializeField]private string travelOriginDecisionId;
    [SerializeField]private string activeTravelPartyId;
    [SerializeField]private int hiddenDaysRemaining;
    [SerializeField]private MerchantTradePlanRuntime merchantTradePlan = new MerchantTradePlanRuntime();
    [SerializeField]private NpcTravelPlanRuntime travelPlan = new NpcTravelPlanRuntime();
    [SerializeField]private CommercialKnowledgeRuntime commercialKnowledge = new CommercialKnowledgeRuntime();
    [SerializeField]private NpcLocalKnowledgeObservationRuntime localKnowledgeObservationRuntime = new NpcLocalKnowledgeObservationRuntime();
    [SerializeField]private NpcMerchantTradeStateRuntime merchantTradeStateRuntime = new NpcMerchantTradeStateRuntime();
    [SerializeField]private ExplorableSiteKnowledgeRuntime explorableSiteKnowledge;
    [SerializeField]private SpatialKnowledgeRuntime spatialKnowledge;
    [SerializeField]private LocalTopologyKnowledgeRuntime localTopologyKnowledge;
    [SerializeField]private AdventureSiteIntelKnowledgeRuntime adventureSiteIntelKnowledge;

    public string RuntimeId => runtimeId;
    public PersonId PersonId
    {
        get
        {
            if (personIdentity != null)
            {
                return personIdentity;
            }

            if (string.IsNullOrWhiteSpace(personIdValue) == true
                || PersonId.TryCreate(personIdValue, out personIdentity) == false)
            {
                return null;
            }

            return personIdentity;
        }
    }
    public NpcData NpcData => npcData;
    public string DefinitionId => npcData != null ? npcData.DefinitionId : string.Empty;
    public string ResidenceSettlementRuntimeId => personRuntime != null
        ? personRuntime.ResidenceSettlementRuntimeId
        : residenceSettlementRuntimeId;
    internal PersonRuntime BoundPersonRuntime => personRuntime;
    public IReadOnlyList<NpcStatusData> CurrentStatus
    {
        get
        {
            if (currentStatus == null) currentStatus = new List<NpcStatusData>();
            if (currentStatusView == null) currentStatusView = currentStatus.AsReadOnly();
            return currentStatusView;
        }
    }
    internal bool TryReserveCurrentStatusCapacity(int additionalCount)
    {
        if (additionalCount < 0) return false;
        if (currentStatus == null) currentStatus = new List<NpcStatusData>();
        if (additionalCount > int.MaxValue - currentStatus.Count) return false;
        int required = currentStatus.Count + additionalCount;
        if (currentStatus.Capacity < required) currentStatus.Capacity = required;
        return true;
    }
    public NpcActionData CurrentAction => currentAction;
    public NpcActionRuntime CurrentActionRuntime => currentActionRuntime;
    internal long CurrentActionRevision => currentActionRevision;
    internal bool HasConsistentCurrentActionSlot =>
        (currentAction == null && currentActionRuntime == null)
        || (currentActionRuntime != null
            && currentActionRuntime.Action != null
            && ReferenceEquals(currentActionRuntime.Action, currentAction));
    public NpcLifeState LifeState => lifeState;
    public bool IsAlive => lifeState == NpcLifeState.Alive;
    public bool IsDead => lifeState == NpcLifeState.Dead;
    public NpcInjurySeverity InjurySeverity => injurySeverity;
    public InventoryRuntime Inventory => inventory ?? (inventory = new InventoryRuntime());
    internal InventoryRuntime ExistingInventory => inventory;
    public MoneyAccountRuntime MoneyAccount => moneyAccount;
    public float Money => MoneyAccount.Balance;
    public SpatialLocationRuntime CurrentLocation => currentLocation;
    public SpatialLocationRuntime DestinationLocation => destinationLocation;
    public CityRuntime CurrentCity => currentCity;
    public CityRuntime DestinationCity => destinationCity;
    public int TravelDaysRemaining => travelDaysRemaining;
    public int TravelDaysTotal => travelDaysTotal;
    public string TravelRouteRuntimeId => travelRouteRuntimeId;
    public bool TravelStartedToday => travelStartedToday;
    public string TravelOriginDecisionId => travelOriginDecisionId;
    public string ActiveTravelPartyId => activeTravelPartyId;
    public long TravelStateRevision => travelStateRevision;
    internal long LifeStateRevision => lifeStateRevision;
    internal long ResidenceRevision => residenceRevision;
    internal long P12CrimeJusticeRevision => p12CrimeJusticeRevision;
    internal bool IsP12CrimeJusticeBound => p12CrimeJusticeMutationAdmission != null;
    public bool IsTraveling => destinationLocation != null && travelDaysRemaining > 0;
    public int HiddenDaysRemaining => hiddenDaysRemaining;
    public bool IsHidden => hiddenDaysRemaining > 0;
    public MerchantTradePlanRuntime MerchantTradePlan => merchantTradePlan ?? (merchantTradePlan = new MerchantTradePlanRuntime());
    public NpcTravelPlanRuntime TravelPlan => travelPlan ?? (travelPlan = new NpcTravelPlanRuntime());
    internal MerchantTradePlanRuntime ExistingMerchantTradePlan => merchantTradePlan;
    internal NpcTravelPlanRuntime ExistingTravelPlan => travelPlan;
    public CommercialKnowledgeRuntime CommercialKnowledge => commercialKnowledge ?? (commercialKnowledge = new CommercialKnowledgeRuntime());
    internal CommercialKnowledgeRuntime ExistingCommercialKnowledge => commercialKnowledge;
    internal NpcLocalKnowledgeObservationRuntime LocalKnowledgeObservationRuntime => localKnowledgeObservationRuntime ?? (localKnowledgeObservationRuntime = new NpcLocalKnowledgeObservationRuntime());
    internal NpcMerchantTradeStateRuntime MerchantTradeStateRuntime => merchantTradeStateRuntime ?? (merchantTradeStateRuntime = new NpcMerchantTradeStateRuntime());
    public ExplorableSiteKnowledgeRuntime ExplorableSiteKnowledge => explorableSiteKnowledge ?? (explorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime(runtimeId));
    internal ExplorableSiteKnowledgeRuntime ExistingExplorableSiteKnowledge => explorableSiteKnowledge;
    public SpatialKnowledgeRuntime SpatialKnowledge => spatialKnowledge ?? (spatialKnowledge = new SpatialKnowledgeRuntime(runtimeId));
    internal SpatialKnowledgeRuntime ExistingSpatialKnowledge => spatialKnowledge;
    public LocalTopologyKnowledgeRuntime LocalTopologyKnowledge => localTopologyKnowledge ?? (localTopologyKnowledge = new LocalTopologyKnowledgeRuntime(runtimeId));
    internal LocalTopologyKnowledgeRuntime ExistingLocalTopologyKnowledge => localTopologyKnowledge;
    public AdventureSiteIntelKnowledgeRuntime AdventureSiteIntelKnowledge => adventureSiteIntelKnowledge ?? (adventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime(runtimeId));
    internal AdventureSiteIntelKnowledgeRuntime ExistingAdventureSiteIntelKnowledge => adventureSiteIntelKnowledge;
	public string NpcName => npcData != null ? npcData.name : "NPC desconhecido";

    internal bool CanBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return runtimeMutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return runtimeMutationGuardBinding.TryBindTo(guard);
    }

    private void EnsureRuntimeMutationGuardBinding()
    {
        if (runtimeMutationGuardBinding == null)
        {
            runtimeMutationGuardBinding = new MutationGuardBinding();
        }
    }

    internal void BindP12TravelStateMutationBoundary(
        Func<bool, IReadOnlyList<CityRuntime>, bool> admission,
        Action<bool, IReadOnlyList<CityRuntime>> committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12TravelStateMutationAdmission != null || p12TravelStateMutationCommitted != null)
            throw new InvalidOperationException("NpcRuntime is already bound to a P12 travel-state mutation boundary.");
        p12TravelStateMutationAdmission = admission;
        p12TravelStateMutationCommitted = committed;
    }

    internal bool UnbindP12TravelStateMutationBoundary(
        Func<bool, IReadOnlyList<CityRuntime>, bool> admission,
        Action<bool, IReadOnlyList<CityRuntime>> committed)
    {
        if (!ReferenceEquals(p12TravelStateMutationAdmission, admission)
            || !ReferenceEquals(p12TravelStateMutationCommitted, committed)) return false;
        p12TravelStateMutationAdmission = null;
        p12TravelStateMutationCommitted = null;
        return true;
    }

    private bool CanCommitP12TravelStateMutation(
        bool travelStateChanged,
        IReadOnlyList<CityRuntime> changedCities)
    {
        if (travelStateChanged && travelStateRevision == long.MaxValue) return false;
        if (p12TravelStateMutationAdmission == null) return true;
        bool admitted;
        try { admitted = p12TravelStateMutationAdmission(travelStateChanged, changedCities); }
        catch { admitted = false; }
        if (!admitted)
            throw new InvalidOperationException(
                "The committed P12 travel-state or City-presence owner could not admit its mutation.");
        return true;
    }

    private void NotifyP12TravelStateMutationCommitted(
        bool travelStateChanged,
        IReadOnlyList<CityRuntime> changedCities)
    {
        if (p12TravelStateMutationCommitted == null) return;
        try { p12TravelStateMutationCommitted(travelStateChanged, changedCities); }
        catch { }
    }

    internal void BindP12LifecycleMutationBoundary(
        Func<NpcRuntime, bool, bool, bool, NpcActionRuntime, bool> admission,
        Action<NpcRuntime, bool, bool, bool> committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12LifecycleMutationAdmission != null || p12LifecycleMutationCommitted != null)
            throw new InvalidOperationException("NpcRuntime is already bound to a P12 lifecycle boundary.");
        if (currentActionRuntime != null && !currentActionRuntime.CanBindToP12Owner(this))
            throw new InvalidOperationException("NpcActionRuntime is already installed in another P12 owner slot.");
        p12LifecycleMutationAdmission = admission;
        p12LifecycleMutationCommitted = committed;
        if (currentActionRuntime != null && !currentActionRuntime.TryBindP12Owner(this))
        {
            p12LifecycleMutationAdmission = null;
            p12LifecycleMutationCommitted = null;
            throw new InvalidOperationException("NpcActionRuntime could not bind to its exact P12 owner slot.");
        }
    }

    internal bool IsP12LifecycleBound => p12LifecycleMutationAdmission != null;

    internal bool CanAdvanceP12LifecycleRevisions(
        int lifeStateIncrements,
        int residenceIncrements,
        int currentActionIncrements = 0)
    {
        return p12LifecycleMutationAdmission == null
            || (lifeStateIncrements >= 0
                && residenceIncrements >= 0
                && currentActionIncrements >= 0
                && lifeStateRevision <= long.MaxValue - lifeStateIncrements
                && residenceRevision <= long.MaxValue - residenceIncrements
                && currentActionRevision <= long.MaxValue - currentActionIncrements);
    }

    internal bool CanCommitP12LifecycleMutation(
        bool lifeStateChanged,
        bool residenceChanged,
        bool currentActionChanged = false,
        NpcActionRuntime nextActionRuntime = null)
    {
        if (p12LifecycleMutationAdmission == null)
            return true;
        if (!CanAdvanceP12LifecycleRevisions(
                lifeStateChanged ? 1 : 0,
                residenceChanged ? 1 : 0,
                currentActionChanged ? 1 : 0))
            return false;
        try
        {
            return p12LifecycleMutationAdmission(
                this,
                lifeStateChanged,
                residenceChanged,
                currentActionChanged,
                nextActionRuntime);
        }
        catch { return false; }
    }

    private void NotifyP12LifecycleMutationCommitted(
        bool lifeStateChanged,
        bool residenceChanged,
        bool currentActionChanged = false)
    {
        if (lifeStateChanged && lifeStateRevision < long.MaxValue) lifeStateRevision++;
        if (residenceChanged && residenceRevision < long.MaxValue) residenceRevision++;
        if (currentActionChanged && currentActionRevision < long.MaxValue) currentActionRevision++;
        if (p12LifecycleMutationCommitted == null) return;
        try { p12LifecycleMutationCommitted(this, lifeStateChanged, residenceChanged, currentActionChanged); }
        catch { }
    }

	public NpcRuntime(string runtimeId, NpcData npcData)
		: this(runtimeId, npcData, null, 0f)
	{
	}

	public NpcRuntime(string runtimeId, NpcData npcData, CityRuntime startingCity, float initialMoney)
	{
		if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("NpcRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

		this.runtimeId = runtimeId;
        this.npcData = npcData;
        spatialKnowledge = new SpatialKnowledgeRuntime(runtimeId);
        explorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime(runtimeId);
        localTopologyKnowledge = new LocalTopologyKnowledgeRuntime(runtimeId);
        adventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime(runtimeId);
        moneyAccount = new MoneyAccountRuntime(initialMoney);

        if (npcData != null && npcData.statusPadrao != null)
        {
            currentStatus.AddRange(npcData.statusPadrao);
        }

        if (startingCity != null)
        {
            startingCity.AddImportantNpc(this);
		}
	}

    internal bool TryAssignPersonId(PersonId personId)
    {
        if (personId == null)
        {
            return false;
        }

        PersonId currentPersonId = PersonId;
        if (currentPersonId != null && currentPersonId != personId)
        {
            return false;
        }

        if (currentPersonId == null && string.IsNullOrWhiteSpace(personIdValue) == false)
        {
            return false;
        }

        personIdentity = personId;
        personIdValue = personId.Value;
        return true;
    }

    internal void ClearPersonId()
    {
        personIdentity = null;
        personIdValue = null;
    }

    internal bool TryBindPersonRuntime(PersonRuntime person)
    {
        if (person == null || PersonId == null || PersonId != person.PersonId)
        {
            return false;
        }

        if (personRuntime != null && ReferenceEquals(personRuntime, person) == false)
        {
            return false;
        }

        personRuntime = person;
        return true;
    }

    internal void ClearPersonRuntime()
    {
        personRuntime = null;
    }

    public void SetCurrentAction(NpcActionData action)
    {
        if (IsAlive == false && action != null)
        {
            return;
        }
        TryInstallCurrentActionRuntime(action != null ? new NpcActionRuntime(action) : null);
    }

    public void SetCurrentActionRuntime(NpcActionRuntime actionRuntime)
    {
        if (IsAlive == false && actionRuntime != null)
        {
            return;
        }

        TryInstallCurrentActionRuntime(actionRuntime);
    }

    private bool TryInstallCurrentActionRuntime(NpcActionRuntime actionRuntime)
    {
        if (actionRuntime == currentActionRuntime
            && ReferenceEquals(currentAction, actionRuntime?.Action))
            return true;
        bool p12Bound = p12LifecycleMutationAdmission != null;
        if ((p12Bound && !HasConsistentCurrentActionSlot)
            || (actionRuntime != null
                && (actionRuntime.Action == null
                    || (p12Bound
                        ? !actionRuntime.CanBindToP12Owner(this)
                        : actionRuntime.IsP12Bound)))
            || !CanCommitP12LifecycleMutation(
                false,
                false,
                true,
                actionRuntime))
            return false;

        NpcActionRuntime previous = currentActionRuntime;
        if (p12Bound && actionRuntime != null && !actionRuntime.TryBindP12Owner(this))
            return false;
        if (p12Bound && previous != null && !previous.UnbindP12Owner(this))
        {
            actionRuntime?.UnbindP12Owner(this);
            return false;
        }

        currentActionRuntime = actionRuntime;
        currentAction = actionRuntime != null ? actionRuntime.Action : null;
        NotifyP12LifecycleMutationCommitted(false, false, true);
        return true;
    }

    internal bool CanCommitInstalledP12ActionMutation(NpcActionRuntime actionRuntime)
    {
        return actionRuntime != null
            && ReferenceEquals(currentActionRuntime, actionRuntime)
            && HasConsistentCurrentActionSlot
            && CanCommitP12LifecycleMutation(
                false,
                false,
                true,
                actionRuntime);
    }

    internal void NotifyInstalledP12ActionMutationCommitted(NpcActionRuntime actionRuntime)
    {
        if (actionRuntime == null || !ReferenceEquals(currentActionRuntime, actionRuntime))
        {
            return;
        }
        NotifyP12LifecycleMutationCommitted(false, false, true);
    }

    private void ClearCurrentActionSlotAfterAdmission()
    {
        NpcActionRuntime previous = currentActionRuntime;
        currentAction = null;
        currentActionRuntime = null;
        previous?.UnbindP12Owner(this);
    }

    public bool TryApplyInjury(NpcInjurySeverity severity)
    {
        // Injury severity has no owner section or operation in this bounded P12 profile.
        // Fail closed before mutation while bound; unbound runtimes retain normal injury behavior.
        if (p12LifecycleMutationAdmission != null
            || IsAlive == false || NpcInjuryRules.IsValid(severity) == false)
        {
            return false;
        }

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        return true;
    }

    public bool TryApplyDeath()
    {
        bool currentActionChanged = currentActionRuntime != null;
        if (personRuntime != null
            || IsDead == true
            || string.IsNullOrWhiteSpace(ResidenceSettlementRuntimeId) == false
            || !CanCommitP12LifecycleMutation(true, false, currentActionChanged))
        {
            return false;
        }

        lifeState = NpcLifeState.Dead;
        ClearCurrentActionSlotAfterAdmission();
        NotifyP12LifecycleMutationCommitted(true, false, currentActionChanged);
        return true;
    }

    /// <summary>
    /// Mirrors an already-validated factual death from the bound Person. Person
    /// death remains authoritative; this method cannot be called publicly.
    /// </summary>
    internal bool ApplyPersonDeathAfterValidation()
    {
        return ApplyPersonDeathAfterValidation(NpcInjurySeverity.None);
    }

    internal bool ApplyPersonDeathAfterValidation(NpcInjurySeverity severity)
    {
        bool currentActionChanged = currentActionRuntime != null;
        if (NpcInjuryRules.IsValid(severity) == false
            || IsDead
            || !CanCommitP12LifecycleMutation(true, false, currentActionChanged))
        {
            return false;
        }

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        ClearCurrentActionSlotAfterAdmission();
        NotifyP12LifecycleMutationCommitted(true, false, currentActionChanged);
        return true;
    }

    /// <summary>
    /// Commits a resident death only after NpcPopulationLifecycleSystem has validated
    /// and applied the matching aggregate transition. The residence is cleared so a
    /// dead NPC remains world-known without remaining a living resident member.
    /// </summary>
    internal bool ApplyResidentDeathAfterPopulationValidation()
    {
        return ApplyResidentDeathAfterPopulationValidation(NpcInjurySeverity.None);
    }

    /// <summary>
    /// Commits the already-validated conflict injury together with the resident death.
    /// This is internal so conflict callers cannot bypass the population boundary.
    /// </summary>
    internal bool ApplyResidentDeathAfterPopulationValidation(NpcInjurySeverity severity)
    {
        bool residenceChanged = !string.IsNullOrWhiteSpace(residenceSettlementRuntimeId);
        bool currentActionChanged = currentActionRuntime != null;
        if (NpcInjuryRules.IsValid(severity) == false
            || IsDead
            || !CanCommitP12LifecycleMutation(true, residenceChanged, currentActionChanged))
        {
            return false;
        }

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        if (residenceChanged) residenceSettlementRuntimeId = null;
        ClearCurrentActionSlotAfterAdmission();
        NotifyP12LifecycleMutationCommitted(true, residenceChanged, currentActionChanged);
        return true;
    }

    internal bool ApplyPersonBackedResidentDeathAfterPopulationValidation(
        NpcInjurySeverity severity,
        PersonDeathTransition personDeathTransition)
    {
        bool currentActionChanged = currentActionRuntime != null;
        if (NpcInjuryRules.IsValid(severity) == false
            || IsDead
            || personRuntime == null
            || personDeathTransition == null
            || !CanCommitP12LifecycleMutation(true, false, currentActionChanged))
        {
            return false;
        }

        if (!personRuntime.RecordDeathAfterValidation(personDeathTransition.DeathAbsoluteDay))
            return false;

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        SetResidenceSettlementRuntimeId(null);
        ClearCurrentActionSlotAfterAdmission();
        NotifyP12LifecycleMutationCommitted(true, false, currentActionChanged);
        return true;
    }

    internal bool CanApplyPersonBackedDeath(long absoluteDay)
    {
        return personRuntime != null
            && personRuntime.DeathAbsoluteDay.HasValue == false
            && absoluteDay >= 0L
            && (personRuntime.BirthAbsoluteDay.HasValue == false
                || absoluteDay >= personRuntime.BirthAbsoluteDay.Value);
    }

    public bool CanApplyConflictConsequence(
        NpcInjurySeverity severity,
        bool shouldDie)
    {
        return IsAlive == true
            && NpcInjuryRules.IsValid(severity) == true
            && (shouldDie == false || personRuntime == null)
            && (shouldDie == false || (lifeState == NpcLifeState.Alive
                && string.IsNullOrWhiteSpace(ResidenceSettlementRuntimeId) == true));
    }

    public bool ApplyConflictConsequence(
        NpcInjurySeverity severity,
        bool shouldDie)
    {
        if (CanApplyConflictConsequence(severity, shouldDie) == false)
        {
            return false;
        }

        TryApplyInjury(severity);
        if (shouldDie == true)
        {
            TryApplyDeath();
        }

        return true;
    }

    public string ConditionSourceId => "injury:" + RuntimeId;

    public float GetCapabilityMultiplier()
    {
        return NpcInjuryRules.GetCapabilityMultiplier(injurySeverity);
    }

    public void AddStatus(NpcStatusData status)
    {
        if (status == null) return;
        if (currentStatus == null) currentStatus = new List<NpcStatusData>();
        if (currentStatus.Contains(status)) return;
        EnsureP12CrimeJusticeMutationAllowed();
        currentStatus.Add(status);
        NotifyP12CrimeJusticeMutationCommitted();
    }

    public void RemoveStatus(NpcStatusData status)
    {
        if (status == null || currentStatus == null || !currentStatus.Contains(status)) return;
        EnsureP12CrimeJusticeMutationAllowed();
        currentStatus.Remove(status);
        NotifyP12CrimeJusticeMutationCommitted();
    }

    internal bool TryBindP12CrimeJusticeMutationBoundary(
        Func<NpcRuntime, bool> admission,
        Action<NpcRuntime> committed)
    {
        if (admission == null || committed == null
            || p12CrimeJusticeMutationAdmission != null
            || p12CrimeJusticeMutationCommitted != null)
            return false;
        p12CrimeJusticeMutationAdmission = admission;
        p12CrimeJusticeMutationCommitted = committed;
        return true;
    }

    private void EnsureP12CrimeJusticeMutationAllowed()
    {
        if (p12CrimeJusticeMutationAdmission == null
            && p12CrimeJusticeMutationCommitted == null) return;
        if (p12CrimeJusticeMutationAdmission == null
            || p12CrimeJusticeMutationCommitted == null
            || p12CrimeJusticeRevision == long.MaxValue)
            throw new InvalidOperationException("P12 Crime/Justice mutation is faulted.");
        bool admitted;
        try { admitted = p12CrimeJusticeMutationAdmission(this); }
        catch { admitted = false; }
        if (!admitted)
            throw new InvalidOperationException("NPC status or hidden state is outside an admitted P12 Crime/Justice boundary.");
    }

    private void NotifyP12CrimeJusticeMutationCommitted()
    {
        if (p12CrimeJusticeMutationAdmission == null) return;
        p12CrimeJusticeRevision++;
        try { p12CrimeJusticeMutationCommitted?.Invoke(this); }
        catch { }
    }

    public bool SetCurrentPresence(SpatialLocationRuntime location, CityRuntime cityProjection = null)
    {
        if (cityProjection != null && cityProjection.Location != location)
        {
            return false;
        }

        if (IsTraveling == true)
        {
            return false;
        }

        CityRuntime previousCity = currentCity;
        bool removePreviousMembership = previousCity != null
            && previousCity != cityProjection
            && previousCity.ContainsImportantNpc(this);
        bool addNewMembership = cityProjection != null
            && !cityProjection.ContainsImportantNpc(this);

        if ((removePreviousMembership
                && !previousCity.CanRemoveImportantNpcMembership(this))
            || (addNewMembership
                && !cityProjection.CanAddImportantNpcMembership(this)))
        {
            return false;
        }

        List<CityRuntime> changedCities = new List<CityRuntime>(2);
        if (removePreviousMembership) changedCities.Add(previousCity);
        if (addNewMembership && cityProjection != previousCity) changedCities.Add(cityProjection);
        if (!CanCommitP12TravelStateMutation(false, changedCities)) return false;

        if (removePreviousMembership
            && !previousCity.TryRemoveImportantNpcMembership(this))
        {
            return false;
        }

        if (addNewMembership
            && !cityProjection.TryAddImportantNpcMembership(this))
        {
            if (removePreviousMembership)
            {
                previousCity.TryAddImportantNpcMembership(this);
            }
            return false;
        }

        currentLocation = location;
        currentCity = cityProjection;

        if (changedCities.Count != 0)
            NotifyP12TravelStateMutationCommitted(false, changedCities);

        return true;
    }

    public void AddMoney(float amount)
    {
        MoneyAccount.TryCredit(amount);
    }

    public bool TrySpendMoney(float amount)
    {
        return MoneyAccount.TryDebit(amount);
    }

    public bool StartTravel(
        SpatialLocationRuntime destination,
        CityRuntime destinationCityProjection,
        int travelDays,
        string originDecisionId = null,
        string routeRuntimeId = null)
    {
        if (IsAlive == false
            || destination == null
            || (destinationCityProjection != null && destinationCityProjection.Location != destination)
            || currentLocation == null
            || IsTraveling == true
            || string.IsNullOrWhiteSpace(activeTravelPartyId) == false)
        {
            return false;
        }

        if (currentCity != null
            && !currentCity.CanRemoveImportantNpcMembership(this))
        {
            return false;
        }

        List<CityRuntime> changedCities = currentCity != null
                && currentCity.ContainsImportantNpc(this)
            ? new List<CityRuntime> { currentCity }
            : new List<CityRuntime>();
        if (!CanCommitP12TravelStateMutation(true, changedCities)) return false;

        if (currentCity != null && !currentCity.TryRemoveImportantNpcMembership(this)) return false;

        currentLocation = null;
        currentCity = null;

        destinationLocation = destination;
        destinationCity = destinationCityProjection;
        travelDaysRemaining = Mathf.Max(1, travelDays);
        travelDaysTotal = travelDaysRemaining;
        travelRouteRuntimeId = string.IsNullOrWhiteSpace(routeRuntimeId) == true ? null : routeRuntimeId;
        travelStartedToday = true;
        travelOriginDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
        travelStateRevision++;
        NotifyP12TravelStateMutationCommitted(true, changedCities);
        return true;
    }

    internal bool CanStartTravelPresenceTransition(long requiredRevisionIncrements)
    {
        return currentCity == null
            || !currentCity.ContainsImportantNpc(this)
            || currentCity.CanApplyImportantNpcRevisionIncrements(requiredRevisionIncrements);
    }

    internal bool CanApplyTravelStateRevisionIncrements(long increments)
    {
        return increments >= 0L
            && travelStateRevision <= long.MaxValue - increments;
    }

    internal bool CanPreflightTravelStateMutations(long requiredTravelStateRevisionIncrements)
    {
        if (!CanApplyTravelStateRevisionIncrements(requiredTravelStateRevisionIncrements)) return false;
        List<CityRuntime> changedCities = currentCity != null
                && currentCity.ContainsImportantNpc(this)
            ? new List<CityRuntime> { currentCity }
            : new List<CityRuntime>();
        return CanCommitP12TravelStateMutation(true, changedCities);
    }

    public bool StartTravel(CityRuntime destination, int travelDays, string originDecisionId = null)
    {
        return destination != null
            && StartTravel(destination.Location, destination, travelDays, originDecisionId);
    }

    public bool SetActiveTravelPartyId(string travelPartyId)
    {
        string next = string.IsNullOrWhiteSpace(travelPartyId) == true ? null : travelPartyId;
        if (string.Equals(activeTravelPartyId, next, StringComparison.Ordinal)) return true;
        if (!CanCommitP12TravelStateMutation(true, Array.Empty<CityRuntime>())) return false;
        activeTravelPartyId = next;
        travelStateRevision++;
        NotifyP12TravelStateMutationCommitted(true, Array.Empty<CityRuntime>());
        return true;
    }

    public bool ClearTravelStartedToday()
    {
        if (!travelStartedToday) return true;
        if (!CanCommitP12TravelStateMutation(true, Array.Empty<CityRuntime>())) return false;
        travelStartedToday = false;
        travelStateRevision++;
        NotifyP12TravelStateMutationCommitted(true, Array.Empty<CityRuntime>());
        return true;
    }

    public bool AdvanceTravelDay(out CityRuntime arrivedCity)
    {
        arrivedCity = null;

        if (IsAlive == false || IsTraveling == false)
        {
            return false;
        }

        bool arriving = travelDaysRemaining <= 1;
        CityRuntime arrivalCity = arriving ? destinationCity : null;
        bool addArrivalMembership = arrivalCity != null
            && !arrivalCity.ContainsImportantNpc(this);
        List<CityRuntime> changedCities = addArrivalMembership
            ? new List<CityRuntime> { arrivalCity }
            : new List<CityRuntime>();
        if ((arriving && destinationCity != null
                && !destinationCity.CanAddImportantNpcMembership(this))
            || !CanCommitP12TravelStateMutation(true, changedCities))
        {
            return false;
        }

        // The preflight above reserves the City revision and validates the owner
        // baseline before any travel-progress or presence projection writes.
        if (addArrivalMembership && !arrivalCity.TryAddImportantNpcMembership(this)) return false;

        travelDaysRemaining = Mathf.Max(0, travelDaysRemaining - 1);

        if (travelDaysRemaining > 0)
        {
            travelStateRevision++;
            NotifyP12TravelStateMutationCommitted(true, changedCities);
            return false;
        }

        SpatialLocationRuntime arrivedLocation = destinationLocation;
        arrivedCity = destinationCity;
        destinationLocation = null;
        destinationCity = null;
        travelDaysTotal = 0;
        travelRouteRuntimeId = null;
        travelOriginDecisionId = null;

        if (arrivedLocation != null)
        {
            if (arrivedCity != null)
            {
                currentLocation = arrivedLocation;
                currentCity = arrivedCity;
            }
            else
            {
                currentLocation = arrivedLocation;
                currentCity = null;
            }
        }

        travelStateRevision++;
        NotifyP12TravelStateMutationCommitted(true, changedCities);

        return true;
    }

    public bool CancelTravel(SpatialLocationRuntime originLocation, CityRuntime originCityProjection)
    {
        if (IsTraveling == false)
        {
            return false;
        }

        bool addOriginMembership = originCityProjection != null
            && !originCityProjection.ContainsImportantNpc(this);
        List<CityRuntime> changedCities = addOriginMembership
            ? new List<CityRuntime> { originCityProjection }
            : new List<CityRuntime>();
        if ((originCityProjection != null
                && !originCityProjection.CanAddImportantNpcMembership(this))
            || !CanCommitP12TravelStateMutation(true, changedCities))
        {
            return false;
        }

        if (addOriginMembership && !originCityProjection.TryAddImportantNpcMembership(this)) return false;

        destinationLocation = null;
        destinationCity = null;
        travelDaysRemaining = 0;
        travelDaysTotal = 0;
        travelRouteRuntimeId = null;
        travelStartedToday = false;
        travelOriginDecisionId = null;
        activeTravelPartyId = null;

        if (originCityProjection != null)
        {
            currentLocation = originLocation;
            currentCity = originCityProjection;
        }
        else
        {
            currentLocation = originLocation;
            currentCity = null;
        }

        travelStateRevision++;
        NotifyP12TravelStateMutationCommitted(true, changedCities);

        return true;
    }

    public bool CancelTravel(CityRuntime originCity)
    {
        return CancelTravel(originCity?.Location, originCity);
    }

    internal void ClearCurrentPresenceFromCity(CityRuntime city)
    {
        if (currentCity == city)
        {
            currentCity = null;
            currentLocation = null;
        }
    }

    internal bool SetResidenceSettlementRuntimeId(string settlementRuntimeId)
    {
        if (personRuntime != null)
        {
            return personRuntime.TrySetResidenceSettlementRuntimeId(settlementRuntimeId);
        }

        string normalized = string.IsNullOrWhiteSpace(settlementRuntimeId) == true
            ? null
            : settlementRuntimeId;
        if (string.Equals(residenceSettlementRuntimeId, normalized, StringComparison.Ordinal))
            return true;
        if (!CanCommitP12LifecycleMutation(false, true))
            return false;
        residenceSettlementRuntimeId = normalized;
        NotifyP12LifecycleMutationCommitted(false, true);
        return true;
    }

    public void SetTravelPlan(CityRuntime targetCity, NpcTravelReason reason, float utility, float expectedCost, string originDecisionId = null)
    {
        TravelPlan.Set(targetCity, reason, utility, expectedCost, originDecisionId);
    }

    public void SetTravelPlan(
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCityProjection,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId = null)
    {
        TravelPlan.Set(targetLocation, targetCityProjection, reason, utility, expectedCost, originDecisionId);
    }

    public void ClearTravelPlan()
    {
        TravelPlan.Clear();
    }

    public void HideForDays(int days)
    {
        int followingFullDays = Mathf.Max(1, days);
        int durationIncludingCurrentDay = followingFullDays + 1;
        int next = Mathf.Max(hiddenDaysRemaining, durationIncludingCurrentDay);
        if (next == hiddenDaysRemaining) return;
        EnsureP12CrimeJusticeMutationAllowed();
        hiddenDaysRemaining = next;
        NotifyP12CrimeJusticeMutationCommitted();
    }

    public bool AdvanceHiddenDay()
    {
        if (hiddenDaysRemaining <= 0)
        {
            return false;
        }

        EnsureP12CrimeJusticeMutationAllowed();
        hiddenDaysRemaining = Mathf.Max(0, hiddenDaysRemaining - 1);
        NotifyP12CrimeJusticeMutationCommitted();
        return hiddenDaysRemaining <= 0;
    }

    public void ClearHidden()
    {
        if (hiddenDaysRemaining == 0) return;
        EnsureP12CrimeJusticeMutationAllowed();
        hiddenDaysRemaining = 0;
        NotifyP12CrimeJusticeMutationCommitted();
    }

    public void SetMerchantTradePlan(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem, string originDecisionId = null)
    {
        MerchantTradePlan.Set(item, originCity, targetCity, plannedAmount, purchasePricePerItem, originDecisionId);
    }

    public void ClearMerchantTradePlan()
    {
        MerchantTradePlan.Clear();

        if (TravelPlan.Reason == NpcTravelReason.Trade)
        {
            ClearTravelPlan();
        }
    }
}
