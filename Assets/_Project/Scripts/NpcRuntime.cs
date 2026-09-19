using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcRuntime : ICapabilityConditionSource
{
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
    public List<NpcStatusData> CurrentStatus => currentStatus ?? (currentStatus = new List<NpcStatusData>());
    public NpcActionData CurrentAction => currentAction;
    public NpcActionRuntime CurrentActionRuntime => currentActionRuntime;
    public NpcLifeState LifeState => lifeState;
    public bool IsAlive => lifeState == NpcLifeState.Alive;
    public bool IsDead => lifeState == NpcLifeState.Dead;
    public NpcInjurySeverity InjurySeverity => injurySeverity;
    public InventoryRuntime Inventory => inventory ?? (inventory = new InventoryRuntime());
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
    public bool IsTraveling => destinationLocation != null && travelDaysRemaining > 0;
    public int HiddenDaysRemaining => hiddenDaysRemaining;
    public bool IsHidden => hiddenDaysRemaining > 0;
    public MerchantTradePlanRuntime MerchantTradePlan => merchantTradePlan ?? (merchantTradePlan = new MerchantTradePlanRuntime());
    public NpcTravelPlanRuntime TravelPlan => travelPlan ?? (travelPlan = new NpcTravelPlanRuntime());
    public CommercialKnowledgeRuntime CommercialKnowledge => commercialKnowledge ?? (commercialKnowledge = new CommercialKnowledgeRuntime());
    public ExplorableSiteKnowledgeRuntime ExplorableSiteKnowledge => explorableSiteKnowledge ?? (explorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime(runtimeId));
    public SpatialKnowledgeRuntime SpatialKnowledge => spatialKnowledge ?? (spatialKnowledge = new SpatialKnowledgeRuntime(runtimeId));
    public LocalTopologyKnowledgeRuntime LocalTopologyKnowledge => localTopologyKnowledge ?? (localTopologyKnowledge = new LocalTopologyKnowledgeRuntime(runtimeId));
    public AdventureSiteIntelKnowledgeRuntime AdventureSiteIntelKnowledge => adventureSiteIntelKnowledge ?? (adventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime(runtimeId));
    public string NpcName => npcData != null ? npcData.name : "NPC desconhecido";

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
		    CurrentStatus.AddRange(npcData.statusPadrao);
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

        currentAction = action;
        currentActionRuntime = action != null ? new NpcActionRuntime(action) : null;
    }

    public void SetCurrentActionRuntime(NpcActionRuntime actionRuntime)
    {
        if (IsAlive == false && actionRuntime != null)
        {
            return;
        }

        currentActionRuntime = actionRuntime;
        currentAction = actionRuntime != null ? actionRuntime.Action : null;
    }

    public bool TryApplyInjury(NpcInjurySeverity severity)
    {
        if (IsAlive == false || NpcInjuryRules.IsValid(severity) == false)
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
        if (personRuntime != null
            || IsDead == true
            || string.IsNullOrWhiteSpace(ResidenceSettlementRuntimeId) == false)
        {
            return false;
        }

        lifeState = NpcLifeState.Dead;
        currentAction = null;
        currentActionRuntime = null;
        return true;
    }

    /// <summary>
    /// Mirrors an already-validated factual death from the bound Person. Person
    /// death remains authoritative; this method cannot be called publicly.
    /// </summary>
    internal void ApplyPersonDeathAfterValidation()
    {
        ApplyPersonDeathAfterValidation(NpcInjurySeverity.None);
    }

    internal void ApplyPersonDeathAfterValidation(NpcInjurySeverity severity)
    {
        if (NpcInjuryRules.IsValid(severity) == false)
        {
            return;
        }

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        currentAction = null;
        currentActionRuntime = null;
    }

    /// <summary>
    /// Commits a resident death only after NpcPopulationLifecycleSystem has validated
    /// and applied the matching aggregate transition. The residence is cleared so a
    /// dead NPC remains world-known without remaining a living resident member.
    /// </summary>
    internal void ApplyResidentDeathAfterPopulationValidation()
    {
        ApplyResidentDeathAfterPopulationValidation(NpcInjurySeverity.None);
    }

    /// <summary>
    /// Commits the already-validated conflict injury together with the resident death.
    /// This is internal so conflict callers cannot bypass the population boundary.
    /// </summary>
    internal void ApplyResidentDeathAfterPopulationValidation(NpcInjurySeverity severity)
    {
        if (NpcInjuryRules.IsValid(severity) == false)
        {
            return;
        }

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        SetResidenceSettlementRuntimeId(null);
        currentAction = null;
        currentActionRuntime = null;
    }

    internal void ApplyPersonBackedResidentDeathAfterPopulationValidation(
        NpcInjurySeverity severity,
        PersonDeathTransition personDeathTransition)
    {
        if (NpcInjuryRules.IsValid(severity) == false)
        {
            return;
        }

        personRuntime.RecordDeathAfterValidation(personDeathTransition.DeathAbsoluteDay);

        if (severity > injurySeverity)
        {
            injurySeverity = severity;
        }

        lifeState = NpcLifeState.Dead;
        SetResidenceSettlementRuntimeId(null);
        currentAction = null;
        currentActionRuntime = null;
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
        if (status == null)
        {
            return;
        }

        if (CurrentStatus.Contains(status) == false)
        {
            CurrentStatus.Add(status);
        }
    }

    public void RemoveStatus(NpcStatusData status)
    {
        CurrentStatus.Remove(status);
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

        if (previousCity != null && previousCity != cityProjection)
        {
            previousCity.RemoveImportantNpc(this);
        }

        currentLocation = location;
        currentCity = cityProjection;

        if (cityProjection != null && cityProjection.ImportantNpcs.Contains(this) == false)
        {
            cityProjection.ImportantNpcs.Add(this);
        }

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

        if (currentCity != null)
        {
            currentCity.RemoveImportantNpc(this);
        }

        currentLocation = null;
        currentCity = null;

        destinationLocation = destination;
        destinationCity = destinationCityProjection;
        travelDaysRemaining = Mathf.Max(1, travelDays);
        travelDaysTotal = travelDaysRemaining;
        travelRouteRuntimeId = string.IsNullOrWhiteSpace(routeRuntimeId) == true ? null : routeRuntimeId;
        travelStartedToday = true;
        travelOriginDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
        return true;
    }

    public bool StartTravel(CityRuntime destination, int travelDays, string originDecisionId = null)
    {
        return destination != null
            && StartTravel(destination.Location, destination, travelDays, originDecisionId);
    }

    public void SetActiveTravelPartyId(string travelPartyId)
    {
        activeTravelPartyId = string.IsNullOrWhiteSpace(travelPartyId) == true ? null : travelPartyId;
    }

    public void ClearTravelStartedToday()
    {
        travelStartedToday = false;
    }

    public bool AdvanceTravelDay(out CityRuntime arrivedCity)
    {
        arrivedCity = null;

        if (IsAlive == false || IsTraveling == false)
        {
            return false;
        }

        travelDaysRemaining = Mathf.Max(0, travelDaysRemaining - 1);

        if (travelDaysRemaining > 0)
        {
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
                arrivedCity.AddImportantNpc(this);
            }
            else
            {
                SetCurrentPresence(arrivedLocation);
            }
        }

        return true;
    }

    public bool CancelTravel(SpatialLocationRuntime originLocation, CityRuntime originCityProjection)
    {
        if (IsTraveling == false)
        {
            return false;
        }

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
            originCityProjection.AddImportantNpc(this);
        }
        else
        {
            SetCurrentPresence(originLocation);
        }

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

    internal void SetResidenceSettlementRuntimeId(string settlementRuntimeId)
    {
        if (personRuntime != null)
        {
            personRuntime.TrySetResidenceSettlementRuntimeId(settlementRuntimeId);
            return;
        }

        residenceSettlementRuntimeId = string.IsNullOrWhiteSpace(settlementRuntimeId) == true
            ? null
            : settlementRuntimeId;
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
        hiddenDaysRemaining = Mathf.Max(hiddenDaysRemaining, durationIncludingCurrentDay);
    }

    public bool AdvanceHiddenDay()
    {
        if (hiddenDaysRemaining <= 0)
        {
            return false;
        }

        hiddenDaysRemaining = Mathf.Max(0, hiddenDaysRemaining - 1);
        return hiddenDaysRemaining <= 0;
    }

    public void ClearHidden()
    {
        hiddenDaysRemaining = 0;
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
