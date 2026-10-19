using System;
using System.Collections.Generic;
using System.Linq;
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
    [NonSerialized]private DailyV1NpcRuntimeOwners selectedRuntimeOwners;
    [NonSerialized]private Func<DailyV1SelectedRuntimeOwnerGraph> selectedRuntimeOwnerGraphProvider;
    [NonSerialized]private DailyV1DefinitionAdapter.SelectedBindings selectedDefinitionBindings;
    private DailyV1NpcDefinition selectedDailyV1Definition => selectedRuntimeOwners?.Definition;
    private DailyV1EffectiveDefinitions selectedDefinitionGraph => selectedRuntimeOwners?.Definitions;
    private DailyV1NpcStatusOwner selectedDailyV1StatusOwner => selectedRuntimeOwners?.Status;
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
    /// <summary>
    /// Compatibility bridge for Unity and the existing P12 owner codec. A
    /// selected runtime keeps its exact admitted binding, but does not take
    /// authored NpcData as a constructor input or retain it as runtime state.
    /// </summary>
    public NpcData NpcData
    {
        get
        {
            if (selectedDailyV1Definition != null
                && selectedDefinitionBindings != null
                && selectedDefinitionBindings.TryGetAsset(selectedDailyV1Definition, out NpcData selectedAsset)
                && selectedAsset != null)
                return selectedAsset;
            return npcData;
        }
    }
    public string DefinitionId => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.DefinitionId
        : npcData != null ? npcData.DefinitionId : string.Empty;
    internal DailyV1NpcDefinition SelectedDailyV1Definition => selectedDailyV1Definition;
    internal DailyV1EffectiveDefinitions SelectedDailyV1DefinitionGraph => selectedDefinitionGraph;
    internal DailyV1NpcRuntimeOwners SelectedRuntimeOwners => selectedRuntimeOwners;

    internal bool CanBindSelectedRuntimeOwnerGraphProvider(
        Func<DailyV1SelectedRuntimeOwnerGraph> provider) =>
        provider != null
        && (selectedRuntimeOwnerGraphProvider == null
            || ReferenceEquals(selectedRuntimeOwnerGraphProvider, provider));

    internal bool TryBindSelectedRuntimeOwnerGraphProvider(
        Func<DailyV1SelectedRuntimeOwnerGraph> provider)
    {
        if (!CanBindSelectedRuntimeOwnerGraphProvider(provider)) return false;
        selectedRuntimeOwnerGraphProvider = provider;
        return true;
    }

    internal bool UnbindSelectedRuntimeOwnerGraphProvider(
        Func<DailyV1SelectedRuntimeOwnerGraph> provider)
    {
        if (!ReferenceEquals(selectedRuntimeOwnerGraphProvider, provider)) return false;
        selectedRuntimeOwnerGraphProvider = null;
        return true;
    }
    internal DailyV1NpcStatusOwner SelectedDailyV1StatusOwner => selectedDailyV1StatusOwner;
    internal bool TryCopySelectedCurrentStatusDefinitionIds(List<string> destination) =>
        UsesSelectedDailyV1Definitions
        && selectedRuntimeOwners != null
        && ReferenceEquals(selectedRuntimeOwners.Definitions, selectedDefinitionGraph)
        && selectedRuntimeOwners.TryCopyStatusDefinitionIds(destination);
    internal bool HasJobType(NpcJobType jobType)
    {
        if (selectedDailyV1Definition == null)
            return npcData != null && npcData.job != null && npcData.job.jobType == jobType;
        return DailyV1NpcDefinitionRules.HasJobType(selectedDailyV1Definition, selectedDefinitionGraph, (int)jobType);
    }
    internal bool HasMerchantBehavior(MerchantBehavior behavior)
    {
        if (selectedDailyV1Definition == null)
            return npcData != null && npcData.job != null && npcData.job.merchantBehavior == behavior;
        return DailyV1NpcDefinitionRules.HasMerchantBehavior(selectedDailyV1Definition, selectedDefinitionGraph, (int)behavior);
    }
    internal float MinimumProfitPerItem
    {
        get
        {
            if (selectedDailyV1Definition == null)
                return npcData != null && npcData.job != null ? npcData.job.minimumProfitPerItem : 1f;
            return DailyV1NpcDefinitionRules.MinimumProfitPerItem(selectedDailyV1Definition, selectedDefinitionGraph);
        }
    }
    public string ResidenceSettlementRuntimeId => personRuntime != null
        ? personRuntime.ResidenceSettlementRuntimeId
        : residenceSettlementRuntimeId;
    internal PersonRuntime BoundPersonRuntime => personRuntime;
    internal IReadOnlyList<NpcStatusData> ExistingCurrentStatus => currentStatus;
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
    public NpcActionData CurrentAction
    {
        get
        {
            if (selectedRuntimeOwners == null) return currentAction;
            DailyV1ActionDefinition selectedAction = selectedRuntimeOwners.ActionSlot?.CurrentAction;
            if (selectedAction == null) return null;
            if (selectedDefinitionBindings != null)
                return selectedDefinitionBindings.TryGetAsset(selectedAction, out NpcActionData selectedAsset)
                    && selectedAsset != null
                    ? selectedAsset
                    : null;
            return currentActionRuntime?.Action;
        }
    }
    public NpcActionRuntime CurrentActionRuntime => currentActionRuntime;
    internal long CurrentActionRevision => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.ActionSlot.Revision
        : currentActionRevision;
    internal bool HasConsistentCurrentActionSlot => selectedRuntimeOwners != null
        ? HasConsistentSelectedCurrentActionSlot(
            selectedDefinitionGraph, selectedDefinitionBindings, selectedRuntimeOwners.ActionSlot)
        : (currentAction == null && currentActionRuntime == null)
            || (currentActionRuntime != null
                && currentActionRuntime.Action != null
                && ReferenceEquals(currentActionRuntime.Action, currentAction));
    internal bool HasConsistentSelectedTravelState => selectedRuntimeOwners == null
        || (string.Equals(selectedRuntimeOwners.Travel.NpcRuntimeId, runtimeId, StringComparison.Ordinal)
            && string.Equals(selectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
                currentLocation?.RuntimeId, StringComparison.Ordinal)
            && string.Equals(selectedRuntimeOwners.Travel.CurrentCityRuntimeId,
                currentCity?.RuntimeId, StringComparison.Ordinal)
            && string.Equals(selectedRuntimeOwners.Travel.DestinationLocationRuntimeId,
                destinationLocation?.RuntimeId, StringComparison.Ordinal)
            && string.Equals(selectedRuntimeOwners.Travel.DestinationCityRuntimeId,
                destinationCity?.RuntimeId, StringComparison.Ordinal)
            && selectedRuntimeOwners.Travel.TravelDaysRemaining == travelDaysRemaining
            && selectedRuntimeOwners.Travel.TravelDaysTotal == travelDaysTotal
            && string.Equals(selectedRuntimeOwners.Travel.TravelRouteRuntimeId,
                travelRouteRuntimeId, StringComparison.Ordinal)
            && selectedRuntimeOwners.Travel.TravelStartedToday == travelStartedToday
            && string.Equals(selectedRuntimeOwners.Travel.TravelOriginDecisionId,
                travelOriginDecisionId, StringComparison.Ordinal)
            && string.Equals(selectedRuntimeOwners.Travel.ActiveTravelPartyId,
                activeTravelPartyId, StringComparison.Ordinal)
            && ReferenceEquals(travelPlan?.SelectedOwner, selectedRuntimeOwners.TravelPlan)
            && travelPlan.MatchesSelectedOwner());
    public NpcLifeState LifeState => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Lifecycle.LifeState
        : lifeState;
    public bool IsAlive => LifeState == NpcLifeState.Alive;
    public bool IsDead => LifeState == NpcLifeState.Dead;
    public NpcInjurySeverity InjurySeverity => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Lifecycle.InjurySeverity
        : injurySeverity;
    public InventoryRuntime Inventory => inventory ?? (inventory = new InventoryRuntime());
    internal InventoryRuntime ExistingInventory => inventory;
    public MoneyAccountRuntime MoneyAccount => moneyAccount;
    public float Money => MoneyAccount.Balance;
    public SpatialLocationRuntime CurrentLocation => currentLocation;
    public SpatialLocationRuntime DestinationLocation => destinationLocation;
    public CityRuntime CurrentCity => currentCity;
    public CityRuntime DestinationCity => destinationCity;
    public int TravelDaysRemaining => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.TravelDaysRemaining : travelDaysRemaining;
    public int TravelDaysTotal => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.TravelDaysTotal : travelDaysTotal;
    public string TravelRouteRuntimeId => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.TravelRouteRuntimeId : travelRouteRuntimeId;
    public bool TravelStartedToday => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.TravelStartedToday : travelStartedToday;
    public string TravelOriginDecisionId => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.TravelOriginDecisionId : travelOriginDecisionId;
    public string ActiveTravelPartyId => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.ActiveTravelPartyId : activeTravelPartyId;
    public long TravelStateRevision => selectedRuntimeOwners?.Travel.Revision ?? travelStateRevision;
    internal long LifeStateRevision => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Lifecycle.LifeStateRevision
        : lifeStateRevision;
    internal long ResidenceRevision => residenceRevision;
    internal long P12CrimeJusticeRevision =>
        selectedRuntimeOwners?.CrimeJusticeRevision ?? p12CrimeJusticeRevision;
    internal bool IsP12CrimeJusticeBound => p12CrimeJusticeMutationAdmission != null;
    public bool IsTraveling => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.Travel.IsTraveling
        : destinationLocation != null && travelDaysRemaining > 0;
    public int HiddenDaysRemaining => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.HiddenState.RemainingDays
        : hiddenDaysRemaining;
    public bool IsHidden => HiddenDaysRemaining > 0;
    public MerchantTradePlanRuntime MerchantTradePlan => merchantTradePlan ?? (merchantTradePlan = new MerchantTradePlanRuntime());
    public NpcTravelPlanRuntime TravelPlan => travelPlan ?? (travelPlan = new NpcTravelPlanRuntime());
    internal MerchantTradePlanRuntime ExistingMerchantTradePlan => merchantTradePlan;
    internal object MerchantTradePlanAuthority => selectedRuntimeOwners?.MerchantTradePlan
        ?? (object)merchantTradePlan;
    internal NpcTravelPlanRuntime ExistingTravelPlan => travelPlan;
    public CommercialKnowledgeRuntime CommercialKnowledge => commercialKnowledge ?? (commercialKnowledge = new CommercialKnowledgeRuntime());
    internal CommercialKnowledgeRuntime ExistingCommercialKnowledge => commercialKnowledge;
    internal NpcLocalKnowledgeObservationRuntime LocalKnowledgeObservationRuntime => localKnowledgeObservationRuntime ?? (localKnowledgeObservationRuntime = new NpcLocalKnowledgeObservationRuntime());
    internal NpcMerchantTradeStateRuntime MerchantTradeStateRuntime => merchantTradeStateRuntime ?? (merchantTradeStateRuntime = new NpcMerchantTradeStateRuntime());
    internal NpcLocalKnowledgeObservationRuntime ExistingLocalKnowledgeObservationRuntime => localKnowledgeObservationRuntime;
    internal NpcMerchantTradeStateRuntime ExistingMerchantTradeStateRuntime => merchantTradeStateRuntime;
    public ExplorableSiteKnowledgeRuntime ExplorableSiteKnowledge => explorableSiteKnowledge ?? (explorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime(runtimeId));
    internal ExplorableSiteKnowledgeRuntime ExistingExplorableSiteKnowledge => explorableSiteKnowledge;
    public SpatialKnowledgeRuntime SpatialKnowledge => spatialKnowledge ?? (spatialKnowledge = new SpatialKnowledgeRuntime(runtimeId));
    internal SpatialKnowledgeRuntime ExistingSpatialKnowledge => spatialKnowledge;
    public LocalTopologyKnowledgeRuntime LocalTopologyKnowledge => localTopologyKnowledge ?? (localTopologyKnowledge = new LocalTopologyKnowledgeRuntime(runtimeId));
    internal LocalTopologyKnowledgeRuntime ExistingLocalTopologyKnowledge => localTopologyKnowledge;
    public AdventureSiteIntelKnowledgeRuntime AdventureSiteIntelKnowledge => adventureSiteIntelKnowledge ?? (adventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime(runtimeId));
    internal AdventureSiteIntelKnowledgeRuntime ExistingAdventureSiteIntelKnowledge => adventureSiteIntelKnowledge;
	public string NpcName => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.name
        : npcData != null ? npcData.name : "NPC desconhecido";

    internal bool HasConfiguredDefaultAction(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return npcData != null && npcData.acoesPadrao != null
                && npcData.acoesPadrao.Exists(row => row != null && row.action == action);
        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode))
            return false;
        return DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(selectedDailyV1Definition, selectedDefinitionGraph, actionNode);
    }

    internal float GetConfiguredActionBaseUtility(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action != null ? action.baseUtility : 0f;
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionBaseUtility(selectedDefinitionGraph, actionNode)
            : 0f;
    }

    internal bool HasConfiguredRequiredActionStatuses(NpcActionData action, IReadOnlyList<NpcStatusData> activeStatuses)
    {
        if (selectedDailyV1Definition == null)
        {
            if (action?.statusNecessariosParaFazerAcao == null)
                return true;
            foreach (NpcStatusData requiredStatus in action.statusNecessariosParaFazerAcao)
            {
                if (requiredStatus != null && (activeStatuses == null || !activeStatuses.Contains(requiredStatus)))
                    return false;
            }
            return true;
        }

        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)) return false;
        if (!TryResolveSelectedStatusNodes(activeStatuses, out List<DailyV1StatusDefinition> activeStatusNodes))
            return false;
        return DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
            selectedDefinitionGraph, actionNode, activeStatusNodes);
    }

    private bool TryResolveSelectedStatusNodes(
        IReadOnlyList<NpcStatusData> statusAssets,
        out List<DailyV1StatusDefinition> statusNodes)
    {
        statusNodes = null;
        if (selectedDefinitionGraph == null || selectedDefinitionBindings == null)
            return false;

        List<DailyV1StatusDefinition> resolved = new List<DailyV1StatusDefinition>();
        foreach (NpcStatusData statusAsset in statusAssets ?? Array.Empty<NpcStatusData>())
        {
            if (statusAsset == null)
                continue;
            if (!selectedDefinitionBindings.TryGet(statusAsset, out DailyV1StatusDefinition statusNode)
                || !selectedDefinitionGraph.Statuses.TryGetValue(statusNode.DefinitionId,
                    out DailyV1StatusDefinition admittedNode)
                || !ReferenceEquals(statusNode, admittedNode))
                return false;
            resolved.Add(statusNode);
        }

        statusNodes = resolved;
        return true;
    }

    internal bool TryGetConfiguredActionStatusChanges(
        NpcActionData action,
        out IReadOnlyList<NpcStatusData> statusToRemove,
        out IReadOnlyList<NpcStatusData> statusToAdd,
        out IReadOnlyList<NpcStatusData> targetStatusToRemove,
        out IReadOnlyList<NpcStatusData> targetStatusToAdd)
    {
        statusToRemove = null;
        statusToAdd = null;
        targetStatusToRemove = null;
        targetStatusToAdd = null;
        if (selectedDailyV1Definition == null)
        {
            statusToRemove = action?.statusToRemove;
            statusToAdd = action?.statusToAdd;
            targetStatusToRemove = action?.targetStatusToRemove;
            targetStatusToAdd = action?.targetStatusToAdd;
            return action != null;
        }
        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(selectedDefinitionGraph, actionNode,
                out IReadOnlyList<DailyV1StatusDefinition> statusToRemoveNodes,
                out IReadOnlyList<DailyV1StatusDefinition> statusToAddNodes,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemoveNodes,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAddNodes)
            || !TryResolveSelectedStatusAssets(statusToRemoveNodes, out List<NpcStatusData> resolvedStatusToRemove)
            || !TryResolveSelectedStatusAssets(statusToAddNodes, out List<NpcStatusData> resolvedStatusToAdd)
            || !TryResolveSelectedStatusAssets(targetStatusToRemoveNodes, out List<NpcStatusData> resolvedTargetStatusToRemove)
            || !TryResolveSelectedStatusAssets(targetStatusToAddNodes, out List<NpcStatusData> resolvedTargetStatusToAdd))
            return false;

        statusToRemove = resolvedStatusToRemove;
        statusToAdd = resolvedStatusToAdd;
        targetStatusToRemove = resolvedTargetStatusToRemove;
        targetStatusToAdd = resolvedTargetStatusToAdd;
        return true;
    }

    internal bool TryGetConfiguredActionStatusNodes(
        NpcActionData action,
        out IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> statusToAdd,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
    {
        statusToRemove = null;
        statusToAdd = null;
        targetStatusToRemove = null;
        targetStatusToAdd = null;
        return selectedDailyV1Definition != null
            && selectedDailyV1StatusOwner != null
            && TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            && DailyV1NpcDefinitionRules.TryGetActionStatusChanges(selectedDefinitionGraph, actionNode,
                out statusToRemove, out statusToAdd, out targetStatusToRemove, out targetStatusToAdd);
    }

    internal bool TryCreateConfiguredActionStatusPlan(
        NpcActionData action,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        bool targetExpected,
        DailyV1NpcRuntimeOwners targetOwner,
        out DailyV1SelectedActionStatusPlan plan)
    {
        plan = null;
        return selectedDailyV1Definition != null
            && selectedDailyV1StatusOwner != null
            && selectedRuntimeOwners != null
            && ownerGraph != null
            && ReferenceEquals(ownerGraph.Definitions, selectedDefinitionGraph)
            && TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            && ownerGraph.TryCreateActionStatusPlan(
                selectedRuntimeOwners,
                actionNode,
                targetExpected,
                targetOwner,
                out plan);
    }

    internal bool ApplySelectedDailyV1StatusChanges(
        IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
    {
        return selectedDailyV1StatusOwner != null
            && CanProjectSelectedStatusChanges(statusToAdd)
            && selectedDailyV1StatusOwner.TryApplyOrderedChanges(
                statusToRemove,
                statusToAdd,
                EnsureP12CrimeJusticeMutationAllowed,
                () =>
                {
                    RefreshSelectedStatusProjection();
                    NotifyP12CrimeJusticeMutationCommitted();
                });
    }

    private bool CanProjectSelectedStatusChanges(IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
    {
        if (selectedDailyV1StatusOwner == null || selectedDefinitionBindings == null)
            return false;

        // Validate the current projection and every possible admitted addition
        // before the Core owner starts its ordered writes. A Unity asset may have
        // been destroyed since admission; discovering that in the post-write
        // projection callback would leave owner state changed without its P12
        // revision/epoch notification.
        List<DailyV1StatusDefinition> possibleStatuses = new List<DailyV1StatusDefinition>(
            selectedDailyV1StatusOwner.Statuses.Count + (statusToAdd?.Count ?? 0));
        foreach (DailyV1StatusDefinition status in selectedDailyV1StatusOwner.Statuses)
            possibleStatuses.Add(status);
        if (statusToAdd != null)
            for (int index = 0; index < statusToAdd.Count; index++)
                possibleStatuses.Add(statusToAdd[index]);

        return TryResolveSelectedStatusAssets(possibleStatuses, out _);
    }

    private void RefreshSelectedStatusProjection()
    {
        if (selectedDailyV1StatusOwner == null || selectedDefinitionBindings == null)
            throw new InvalidOperationException("The selected NPC status owner has no admitted Unity compatibility bindings.");

        List<NpcStatusData> projection = new List<NpcStatusData>(selectedDailyV1StatusOwner.Statuses.Count);
        foreach (DailyV1StatusDefinition status in selectedDailyV1StatusOwner.Statuses)
        {
            if (!selectedDefinitionBindings.TryGetAsset(status, out NpcStatusData asset) || asset == null)
                throw new InvalidOperationException("A selected NPC status lost its exact Unity compatibility binding.");
            projection.Add(asset);
        }

        if (currentStatus == null) currentStatus = new List<NpcStatusData>();
        currentStatus.Clear();
        currentStatus.AddRange(projection);
    }

    private bool TryResolveSelectedStatusAssets(
        IReadOnlyList<DailyV1StatusDefinition> statusNodes,
        out List<NpcStatusData> statuses)
    {
        statuses = new List<NpcStatusData>();
        foreach (DailyV1StatusDefinition statusNode in statusNodes ?? Array.Empty<DailyV1StatusDefinition>())
        {
            if (statusNode == null
                || !selectedDefinitionGraph.Statuses.TryGetValue(statusNode.DefinitionId, out DailyV1StatusDefinition admittedNode)
                || !ReferenceEquals(admittedNode, statusNode)
                || !selectedDefinitionBindings.TryGetAsset(statusNode, out NpcStatusData statusAsset)
                || statusAsset == null)
                return false;
            statuses.Add(statusAsset);
        }
        return true;
    }

    internal CrimeActionSettings GetConfiguredCrimeActionSettings(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action != null && action.crimeSettings != null ? action.crimeSettings : new CrimeActionSettings();
        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode))
            return new CrimeActionSettings();
        DailyV1CrimeActionSettings settings = DailyV1NpcDefinitionRules.GetCrimeActionSettings(selectedDefinitionGraph, actionNode);
        if (settings == null) return new CrimeActionSettings();
        return new CrimeActionSettings
        {
            amount = settings.amount,
            bounty = settings.bounty,
            sentenceDays = settings.sentenceDays,
            hiddenDays = settings.hiddenDays,
            escapeBountyPenalty = settings.escapeBountyPenalty,
            failedEscapeSentencePenalty = settings.failedEscapeSentencePenalty
        };
    }

    internal string GetConfiguredActionName(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action?.actionName ?? string.Empty;
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionName(selectedDefinitionGraph, actionNode)
            : string.Empty;
    }

    internal string GetConfiguredNormalActionLogText(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action?.normalActionLogText ?? string.Empty;
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetNormalActionLogText(selectedDefinitionGraph, actionNode)
            : string.Empty;
    }

    internal bool GetConfiguredActionCanFail(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action != null && action.canFail;
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionCanFail(selectedDefinitionGraph, actionNode)
            : false;
    }

    internal float GetConfiguredActionBaseSuccessChance(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return action != null ? action.baseSuccessChance : 1f;
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionBaseSuccessChance(selectedDefinitionGraph, actionNode)
            : 0f;
    }

    internal bool TryRollSelectedDailyV1ActionSuccess(
        NpcActionData action,
        string decisionId,
        float contextualMultiplier,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource,
        out bool succeeded)
    {
        succeeded = false;
        return selectedDailyV1Definition != null
            && TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            && DailyV1NpcActionDecision.TryRollActionSuccess(
                selectedDefinitionGraph,
                actionNode,
                RuntimeId,
                decisionId,
                contextualMultiplier,
                currentAbsoluteDay,
                randomSource,
                out succeeded);
    }

    internal int GetConfiguredActionTypeOrder(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return (int)(action?.actionType ?? NpcActionType.Normal);
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionTypeOrder(selectedDefinitionGraph, actionNode)
            : int.MinValue;
    }

    internal int GetConfiguredActionCategoryOrder(NpcActionData action)
    {
        if (selectedDailyV1Definition == null)
            return (int)(action?.actionCategory ?? NpcActionCategory.General);
        return TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode)
            ? DailyV1NpcDefinitionRules.GetActionCategoryOrder(selectedDefinitionGraph, actionNode)
            : int.MinValue;
    }

    internal NpcActionType GetConfiguredActionType(NpcActionData action)
    {
        return (NpcActionType)GetConfiguredActionTypeOrder(action);
    }

    internal bool UsesSelectedDailyV1Definitions => selectedDailyV1Definition != null;

    internal bool TryGetConfiguredJobActionUtility(NpcActionData action, out float utility)
    {
        utility = 0f;
        if (selectedDailyV1Definition == null)
        {
            if (npcData == null || npcData.job == null || npcData.job.workAction != action)
                return false;
            utility = npcData.job.workUtility;
            return true;
        }

        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode))
            return false;
        return DailyV1NpcDefinitionRules.TryGetWorkActionUtility(
            selectedDailyV1Definition, selectedDefinitionGraph, actionNode, out utility);
    }

    internal bool TryGetConfiguredDefaultActionUtility(NpcActionData action, out float utility)
    {
        utility = 0f;
        if (selectedDailyV1Definition == null)
        {
            NPCDefaultAction configured = npcData?.acoesPadrao?.Find(row => row != null && row.action == action);
            if (configured == null) return false;
            utility = configured.baseUtility;
            return true;
        }

        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode))
            return false;
        return DailyV1NpcDefinitionRules.TryGetDefaultActionUtility(
            selectedDailyV1Definition, selectedDefinitionGraph, actionNode, out utility);
    }

    internal float ApplyConfiguredActionStatusModifiers(NpcActionData action, float utility)
    {
        if (selectedDailyV1Definition == null)
        {
            if (action?.statusModifiers == null) return utility;
            foreach (StatusWeightModifier modifier in action.statusModifiers)
                if (modifier != null && modifier.status != null && CurrentStatus.Contains(modifier.status))
                    utility *= modifier.multiplier;
            return utility;
        }

        if (!TryGetSelectedDailyV1Action(action, out DailyV1ActionDefinition actionNode))
            return 0f;
        return DailyV1NpcDefinitionRules.ApplyStatusModifiers(
            selectedDefinitionGraph, actionNode, selectedDailyV1StatusOwner?.Statuses, utility);
    }

    private bool TryGetSelectedDailyV1Action(NpcActionData action, out DailyV1ActionDefinition actionNode)
    {
        actionNode = null;
        return selectedDailyV1Definition != null
            && TryResolveSelectedDailyV1Action(selectedDefinitionGraph, selectedDefinitionBindings,
                action, out actionNode);
    }

    private static bool TryResolveSelectedDailyV1Action(
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        NpcActionData action,
        out DailyV1ActionDefinition actionNode)
    {
        actionNode = null;
        return definitions != null && bindings != null && action != null
            && bindings.TryGet(action, out actionNode)
            && definitions.Actions.TryGetValue(actionNode.DefinitionId, out DailyV1ActionDefinition admittedAction)
            && ReferenceEquals(actionNode, admittedAction);
    }

    private bool HasConsistentSelectedCurrentActionSlot(
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        DailyV1NpcActionSlotOwner actionSlot)
    {
        if (definitions == null || actionSlot == null)
            return false;

        if (currentActionRuntime == null)
            return currentAction == null && actionSlot.CurrentAction == null
                && actionSlot.CurrentPayload == null;

        NpcActionData actionAsset = currentActionRuntime.Action;
        DailyV1ActionDefinition currentNode = actionSlot.CurrentAction;
        if (actionAsset == null || !ReferenceEquals(currentAction, actionAsset)
            || currentNode == null
            || actionSlot.CurrentPayload == null
            || !ReferenceEquals(currentActionRuntime.SelectedOwner, actionSlot.CurrentPayload)
            || !currentActionRuntime.MatchesSelectedOwner()
            || !definitions.Actions.TryGetValue(actionAsset.DefinitionId, out DailyV1ActionDefinition admittedNode)
            || !ReferenceEquals(currentNode, admittedNode))
            return false;

        if (bindings == null) return true;
        return bindings.TryGet(actionAsset, out DailyV1ActionDefinition mappedNode)
            && ReferenceEquals(mappedNode, currentNode)
            && bindings.TryGetAsset(currentNode, out NpcActionData mappedAsset)
            && ReferenceEquals(mappedAsset, actionAsset);
    }

    internal bool TryGetSelectedDailyV1Actions(List<NpcActionData> actions)
    {
        if (actions == null)
            return false;
        actions.Clear();
        if (selectedDailyV1Definition == null || selectedDefinitionGraph == null
            || selectedDefinitionBindings == null)
            return false;

        List<NpcActionData> resolved = new List<NpcActionData>(selectedDefinitionGraph.Actions.Count);
        foreach (DailyV1ActionDefinition node in selectedDefinitionGraph.Actions.Values)
        {
            if (node == null
                || !selectedDefinitionGraph.Actions.TryGetValue(node.DefinitionId, out DailyV1ActionDefinition admittedNode)
                || !ReferenceEquals(node, admittedNode)
                || !selectedDefinitionBindings.TryGetAsset(node, out NpcActionData actionAsset)
                || actionAsset == null)
                return false;
            resolved.Add(actionAsset);
        }

        actions.AddRange(resolved);
        return true;
    }

    internal bool TryGetSelectedDailyV1ActionNodes(List<DailyV1ActionDefinition> actions)
    {
        if (actions == null)
            return false;
        actions.Clear();
        if (selectedDailyV1Definition == null || selectedDefinitionGraph == null || selectedDefinitionBindings == null)
            return false;

        List<DailyV1ActionDefinition> resolved = new List<DailyV1ActionDefinition>(selectedDefinitionGraph.Actions.Count);
        foreach (DailyV1ActionDefinition node in selectedDefinitionGraph.Actions.Values)
        {
            if (node == null
                || !selectedDefinitionGraph.Actions.TryGetValue(node.DefinitionId, out DailyV1ActionDefinition admittedNode)
                || !ReferenceEquals(node, admittedNode)
                || !selectedDefinitionBindings.TryGetAsset(node, out NpcActionData actionAsset)
                || actionAsset == null)
                return false;
            resolved.Add(node);
        }

        actions.AddRange(resolved);
        return true;
    }

    internal bool TryGetSelectedDailyV1ActionAsset(
        DailyV1ActionDefinition action,
        out NpcActionData actionAsset)
    {
        actionAsset = null;
        return selectedDailyV1Definition != null
            && selectedDefinitionGraph != null
            && selectedDefinitionBindings != null
            && action != null
            && selectedDefinitionGraph.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admittedNode)
            && ReferenceEquals(action, admittedNode)
            && selectedDefinitionBindings.TryGetAsset(action, out actionAsset)
            && actionAsset != null;
    }

    internal bool TryGetSelectedDailyV1ActionNode(
        NpcActionData actionAsset,
        out DailyV1ActionDefinition action)
    {
        return TryGetSelectedDailyV1Action(actionAsset, out action);
    }

    internal bool TryGetSelectedDailyV1ActiveStatusNodes(List<DailyV1StatusDefinition> statuses)
    {
        if (statuses == null)
            return false;
        statuses.Clear();
        if (selectedDailyV1Definition == null || selectedDefinitionGraph == null
            || selectedDefinitionBindings == null || selectedDailyV1StatusOwner == null)
            return false;
        statuses.AddRange(selectedDailyV1StatusOwner.Statuses);
        return true;
    }

    internal bool TryGetSelectedDailyV1ActionByDefinitionId(string definitionId, out NpcActionData action)
    {
        action = null;
        return selectedDailyV1Definition != null
            && !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitionGraph != null
            && selectedDefinitionBindings != null
            && selectedDefinitionGraph.Actions.TryGetValue(definitionId, out DailyV1ActionDefinition node)
            && selectedDefinitionBindings.TryGetAsset(node, out action)
            && action != null;
    }

    internal bool TryGetSelectedDailyV1Item(ItemData item, out DailyV1ItemDefinition itemNode)
    {
        itemNode = null;
        return selectedDailyV1Definition != null && item != null
            && selectedDefinitionGraph != null && selectedDefinitionBindings != null
            && selectedDefinitionBindings.TryGet(item, out itemNode)
            && selectedDefinitionGraph.Items.TryGetValue(itemNode.DefinitionId, out DailyV1ItemDefinition admittedItem)
            && ReferenceEquals(itemNode, admittedItem);
    }

    internal bool TryGetSelectedDailyV1ItemAsset(DailyV1ItemDefinition itemNode, out ItemData item)
    {
        item = null;
        return selectedDailyV1Definition != null && itemNode != null
            && selectedDefinitionGraph != null && selectedDefinitionBindings != null
            && selectedDefinitionGraph.Items.TryGetValue(itemNode.DefinitionId, out DailyV1ItemDefinition admittedItem)
            && ReferenceEquals(itemNode, admittedItem)
            && selectedDefinitionBindings.TryGetAsset(itemNode, out item)
            && item != null;
    }

    internal bool TryAddSelectedDailyV1InventoryItem(DailyV1ItemDefinition item, int amount, float averageUnitCost)
    {
        return selectedDailyV1Definition != null && selectedDefinitionGraph != null
            && selectedDefinitionGraph.Items.TryGetValue(item?.DefinitionId ?? string.Empty, out DailyV1ItemDefinition admitted)
            && ReferenceEquals(item, admitted)
            && Inventory.TryAddSelectedItem(item, amount, averageUnitCost);
    }

    internal bool TrySetSelectedDailyV1InventoryOwner(InventoryRuntime selectedInventory)
    {
        if (selectedInventory == null || selectedDailyV1Definition != null || selectedDefinitionGraph != null) return false;
        inventory = selectedInventory;
        return true;
    }

    internal bool TryGetSelectedDailyV1CapabilityAttribute(string definitionId, out CapabilityAttributeData attribute)
    {
        attribute = null;
        return selectedDailyV1Definition != null && selectedDefinitionGraph != null && selectedDefinitionBindings != null
            && !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitionGraph.CapabilityAttributes.TryGetValue(definitionId, out DailyV1CapabilityAttributeDefinition node)
            && selectedDefinitionBindings.TryGetAsset(node, out attribute)
            && attribute != null;
    }

    internal bool HasTrait(TraitData trait)
    {
        if (selectedDailyV1Definition == null)
            return npcData != null && npcData.traits != null && npcData.traits.Contains(trait);
        if (trait == null || selectedDefinitionGraph == null || selectedDefinitionBindings == null
            || !selectedDefinitionBindings.TryGet(trait, out DailyV1TraitDefinition traitNode)
            || !selectedDefinitionGraph.Traits.TryGetValue(traitNode.DefinitionId, out DailyV1TraitDefinition admittedTrait)
            || !ReferenceEquals(traitNode, admittedTrait))
            return false;
        return DailyV1NpcDefinitionRules.HasTrait(selectedDailyV1Definition, selectedDefinitionGraph, traitNode);
    }

    internal float GetCapabilityAttributeValue(CapabilityAttributeData attribute)
    {
        if (attribute == null) return 0f;
        if (selectedDailyV1Definition == null)
        {
            if (npcData?.capabilityValues == null) return 0f;
            foreach (CapabilityAttributeValue row in npcData.capabilityValues)
                if (row != null && row.Attribute == attribute)
                    return UnityMathfCompatibility.Max(0f, row.Value);
            return 0f;
        }
        if (selectedDefinitionGraph == null || selectedDefinitionBindings == null
            || !selectedDefinitionBindings.TryGet(attribute, out DailyV1CapabilityAttributeDefinition attributeNode)
            || !selectedDefinitionGraph.CapabilityAttributes.TryGetValue(attributeNode.DefinitionId, out DailyV1CapabilityAttributeDefinition admittedAttribute)
            || !ReferenceEquals(attributeNode, admittedAttribute))
            return 0f;
        return DailyV1NpcDefinitionRules.GetCapabilityAttributeValue(
            selectedDailyV1Definition, selectedDefinitionGraph, attributeNode);
    }

    internal void AppendPreferredTradeItemDefinitionIds(List<string> destination)
    {
        if (destination == null) return;
        if (selectedDailyV1Definition == null)
        {
            foreach (TradeItemPreference row in npcData?.job?.PreferredTradeItems ?? new List<TradeItemPreference>())
                if (row?.item != null) destination.Add(row.item.DefinitionId);
            return;
        }
        DailyV1NpcDefinitionRules.AppendPreferredTradeItemDefinitionIds(
            selectedDailyV1Definition, selectedDefinitionGraph, destination);
    }

    internal bool TryGetPreferredTradeItemMultiplier(ItemData item, out float multiplier)
    {
        multiplier = 1f;
        if (item == null) return false;
        if (selectedDailyV1Definition == null)
        {
            foreach (TradeItemPreference row in npcData?.job?.PreferredTradeItems ?? new List<TradeItemPreference>())
            {
                if (row != null && row.item == item)
                {
                    multiplier = row.utilityMultiplier;
                    return true;
                }
            }
            return false;
        }
        if (selectedDefinitionGraph == null || selectedDefinitionBindings == null
            || !selectedDefinitionBindings.TryGet(item, out DailyV1ItemDefinition itemNode)
            || !selectedDefinitionGraph.Items.TryGetValue(itemNode.DefinitionId, out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(itemNode, admittedItem))
            return false;
        return DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            selectedDailyV1Definition, selectedDefinitionGraph, itemNode, out multiplier);
    }

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
        if (travelStateChanged && TravelStateRevision == long.MaxValue) return false;
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
        if (selectedRuntimeOwners != null)
        {
            if (!HasConsistentSelectedTravelState)
                throw new InvalidOperationException("The selected NPC travel projection differs from its committed Core owner.");
        }
        else if (travelStateChanged && travelStateRevision < long.MaxValue)
        {
            travelStateRevision++;
        }

        if (p12TravelStateMutationCommitted == null) return;
        try { p12TravelStateMutationCommitted(travelStateChanged, changedCities); }
        catch { }
    }

    private bool TryInstallSelectedTravelProjection(DailyV1NpcTravelOwner owner, long revision) =>
        owner != null
        && owner.TryInstallState(
            currentLocation?.RuntimeId,
            currentCity?.RuntimeId,
            destinationLocation?.RuntimeId,
            destinationCity?.RuntimeId,
            travelDaysRemaining,
            travelDaysTotal,
            travelRouteRuntimeId,
            travelStartedToday,
            travelOriginDecisionId,
            activeTravelPartyId,
            revision);

    private void InstallSelectedTravelProjection(long revision)
    {
        if (selectedRuntimeOwners != null
            && !TryInstallSelectedTravelProjection(selectedRuntimeOwners.Travel, revision))
            throw new InvalidOperationException("The selected NPC travel projection could not be committed to its Core owner.");
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
                && (selectedRuntimeOwners != null
                    ? selectedRuntimeOwners.Lifecycle.CanAdvanceRevision(lifeStateIncrements)
                    : lifeStateRevision <= long.MaxValue - lifeStateIncrements)
                && residenceRevision <= long.MaxValue - residenceIncrements
                && (selectedRuntimeOwners != null
                    ? selectedRuntimeOwners.ActionSlot.CanAdvanceRevision(currentActionIncrements)
                    : currentActionRevision <= long.MaxValue - currentActionIncrements));
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
        if (lifeStateChanged)
        {
            if (selectedRuntimeOwners != null)
                selectedRuntimeOwners.Lifecycle.NotifyLifeStateMutationCommitted();
            else if (lifeStateRevision < long.MaxValue)
                lifeStateRevision++;
        }
        if (residenceChanged && residenceRevision < long.MaxValue) residenceRevision++;
        if (currentActionChanged)
        {
            if (selectedRuntimeOwners != null)
                selectedRuntimeOwners.ActionSlot.NotifyCurrentActionMutationCommitted();
            else if (currentActionRevision < long.MaxValue)
                currentActionRevision++;
        }
        if (p12LifecycleMutationCommitted == null) return;
        try { p12LifecycleMutationCommitted(this, lifeStateChanged, residenceChanged, currentActionChanged); }
        catch { }
    }

	public NpcRuntime(string runtimeId, NpcData npcData)
		: this(runtimeId, npcData, null, 0f)
	{
	}

	public NpcRuntime(string runtimeId, NpcData npcData, CityRuntime startingCity, float initialMoney)
		: this(runtimeId, npcData, startingCity, initialMoney, null)
	{
	}

	internal static NpcRuntime CreateSelectedDailyV1(
		string runtimeId,
		NpcData npcData,
		DailyV1NpcDefinition selectedDefinition,
		DailyV1EffectiveDefinitions selectedDefinitions,
		DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
		CityRuntime startingCity,
		float initialMoney)
	{
		if (selectedDefinition == null)
			throw new ArgumentNullException(nameof(selectedDefinition));
		if (selectedDefinitions == null)
			throw new ArgumentNullException(nameof(selectedDefinitions));
		if (selectedBindings == null)
			throw new ArgumentNullException(nameof(selectedBindings));
		if (npcData == null
			|| !selectedBindings.TryGet(npcData, out DailyV1NpcDefinition assetNode)
			|| !ReferenceEquals(assetNode, selectedDefinition))
			throw new ArgumentException("Selected NPC compatibility identity must resolve to the exact admitted graph node.", nameof(npcData));
		return CreateSelectedDailyV1(runtimeId, selectedDefinition, selectedDefinitions, selectedBindings,
			startingCity, initialMoney);
	}

	internal static NpcRuntime CreateSelectedDailyV1(
		string runtimeId,
		DailyV1NpcDefinition selectedDefinition,
		DailyV1EffectiveDefinitions selectedDefinitions,
		DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
		CityRuntime startingCity,
		float initialMoney)
	{
		if (selectedDefinition == null)
			throw new ArgumentNullException(nameof(selectedDefinition));
		if (selectedDefinitions == null)
			throw new ArgumentNullException(nameof(selectedDefinitions));
		if (selectedBindings == null)
			throw new ArgumentNullException(nameof(selectedBindings));
		return new NpcRuntime(runtimeId, null, startingCity, initialMoney,
			selectedDefinition, selectedDefinitions, selectedBindings);
	}

	internal static NpcRuntime CreateSelectedDailyV1(
		string runtimeId,
		DailyV1NpcDefinition selectedDefinition,
		DailyV1EffectiveDefinitions selectedDefinitions,
		DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
		DailyV1NpcRuntimeOwners selectedOwners,
		CityRuntime startingCity)
	{
		if (selectedDefinition == null) throw new ArgumentNullException(nameof(selectedDefinition));
		if (selectedDefinitions == null) throw new ArgumentNullException(nameof(selectedDefinitions));
		if (selectedBindings == null) throw new ArgumentNullException(nameof(selectedBindings));
		if (selectedOwners == null) throw new ArgumentNullException(nameof(selectedOwners));
		return new NpcRuntime(runtimeId, null, null, 0f,
			selectedDefinition, selectedDefinitions, selectedBindings, selectedOwners, startingCity);
	}

	/// <summary>
	/// Creates the selected NPC owner composition directly from the admitted
	/// Core graph. Unity asset bindings may be attached later for compatibility
	/// projections, but do not participate in owner construction.
	/// </summary>
	internal static NpcRuntime CreateSelectedDailyV1(
		string runtimeId,
		DailyV1NpcDefinition selectedDefinition,
		DailyV1EffectiveDefinitions selectedDefinitions,
		CityRuntime startingCity,
		float initialMoney)
	{
		if (selectedDefinition == null)
			throw new ArgumentNullException(nameof(selectedDefinition));
		if (selectedDefinitions == null)
			throw new ArgumentNullException(nameof(selectedDefinitions));
		return new NpcRuntime(runtimeId, null, startingCity, initialMoney,
			selectedDefinition, selectedDefinitions, null);
	}

	private NpcRuntime(string runtimeId, NpcData npcData, CityRuntime startingCity, float initialMoney,
		DailyV1NpcDefinition selectedDefinition,
		DailyV1EffectiveDefinitions selectedDefinitions = null,
		DailyV1DefinitionAdapter.SelectedBindings selectedBindings = null,
		DailyV1NpcRuntimeOwners initialSelectedOwners = null,
		CityRuntime initialSelectedCityProjection = null)
	{
		if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("NpcRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }
		if (selectedDefinition != null
			&& (selectedDefinitions == null
				|| (selectedBindings != null
					&& (!selectedBindings.TryGetAsset(selectedDefinition, out NpcData exactAsset)
						|| exactAsset == null
						|| (npcData != null && !ReferenceEquals(exactAsset, npcData))
						|| !string.Equals(selectedDefinition.DefinitionId, exactAsset.DefinitionId, StringComparison.Ordinal)))
				|| !selectedDefinitions.Npcs.TryGetValue(selectedDefinition.DefinitionId, out DailyV1NpcDefinition graphNode)
				|| !ReferenceEquals(graphNode, selectedDefinition)
				|| (initialSelectedOwners != null
					&& (!string.Equals(initialSelectedOwners.RuntimeId, runtimeId, StringComparison.Ordinal)
						|| !ReferenceEquals(initialSelectedOwners.Definitions, selectedDefinitions)
						|| !ReferenceEquals(initialSelectedOwners.Definition, selectedDefinition)))))
		{
			throw new ArgumentException(
				"The selected NPC runtime must consume an exact admitted typed definition graph node and any supplied authoring projection must map to it.",
				nameof(selectedDefinition));
		}

        this.runtimeId = runtimeId;
        this.npcData = selectedDefinition == null ? npcData : null;
        if (selectedDefinition != null)
        {
            DailyV1NpcRuntimeOwners selectedOwners = initialSelectedOwners;
            if (selectedOwners == null
                && !DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                    runtimeId, selectedDefinitions, selectedDefinition, initialMoney,
                    out selectedOwners))
                throw new ArgumentException(
                    "The selected NPC owner composition could not be created from the admitted graph.",
                    nameof(selectedDefinition));

            selectedRuntimeOwners = selectedOwners;
            if (!TravelPlan.TryBindSelectedOwner(selectedOwners.TravelPlan))
                throw new ArgumentException(
                    "The selected NPC travel plan could not bind to its Core owner.",
                    nameof(selectedDefinition));
            selectedDefinitionBindings = selectedBindings;
            commercialKnowledge = new CommercialKnowledgeRuntime(
                selectedOwners.CommercialKnowledge, selectedBindings);
            inventory = new InventoryRuntime(selectedOwners.Inventory);
            if (selectedBindings != null
                && !inventory.TryBindSelectedDailyV1Owner(selectedDefinitions, selectedBindings))
                throw new ArgumentException("The selected NPC Inventory could not bind the exact admitted Daily-v1 item graph.", nameof(selectedDefinitions));
            moneyAccount = selectedOwners.MoneyAccount;
        }
        spatialKnowledge = new SpatialKnowledgeRuntime(runtimeId);
        explorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime(runtimeId);
        localTopologyKnowledge = new LocalTopologyKnowledgeRuntime(runtimeId);
        adventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime(runtimeId);
		if (selectedDefinition == null)
            moneyAccount = new MoneyAccountRuntime(initialMoney);

        if (selectedDefinition != null && selectedBindings != null)
        {
            foreach (DailyV1StatusDefinition statusNode in selectedDailyV1StatusOwner.Statuses)
            {
                if (!selectedBindings.TryGetAsset(statusNode, out NpcStatusData statusAsset) || statusAsset == null)
                    throw new ArgumentException(
                        "A selected NPC status did not resolve through its exact admitted typed graph node.",
                        nameof(selectedDefinition));
                currentStatus.Add(statusAsset);
            }
        }
        else if (npcData != null && npcData.statusPadrao != null)
        {
            currentStatus.AddRange(npcData.statusPadrao);
        }

        if (initialSelectedOwners != null)
        {
            if (initialSelectedCityProjection != null)
            {
                if (!string.Equals(initialSelectedOwners.Travel.CurrentCityRuntimeId,
                        initialSelectedCityProjection.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(initialSelectedOwners.Travel.CurrentLocationRuntimeId,
                        initialSelectedCityProjection.Location?.RuntimeId, StringComparison.Ordinal))
                    throw new ArgumentException(
                        "The selected NPC Core presence does not match its initial City projection.",
                        nameof(initialSelectedCityProjection));
                currentCity = initialSelectedCityProjection;
                currentLocation = initialSelectedCityProjection.Location;
                if (!initialSelectedCityProjection.TryAttachInitialSelectedNpcProjection(this))
                    throw new ArgumentException(
                        "The selected NPC Core presence could not bind its initial Unity projection.",
                        nameof(initialSelectedCityProjection));
            }
            else if (initialSelectedOwners.Travel.CurrentCityRuntimeId != null
                || initialSelectedOwners.Travel.CurrentLocationRuntimeId != null)
            {
                throw new ArgumentException(
                    "The selected NPC has Core presence without a matching initial City projection.",
                    nameof(initialSelectedCityProjection));
            }
        }
        else if (startingCity != null)
        {
            startingCity.AddImportantNpc(this);
		}
    }

    /// <summary>
    /// Creates one unpublished staged NPC with its exact direct presence
    /// references. Unlike the gameplay constructor path, this does not add the
    /// NPC to a City or advance any City owner revision; the City membership
    /// projection is linked separately in captured order after all NPCs exist.
    /// </summary>
    internal static bool TryCreateForStagedPresence(
        string runtimeId,
        NpcData npcData,
        CityRuntime currentCity,
        SpatialLocationRuntime currentLocation,
        out NpcRuntime staged)
    {
        staged = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || npcData == null
            || (currentLocation != null && string.IsNullOrWhiteSpace(currentLocation.RuntimeId))
            || (currentCity != null
                && (currentCity.Location == null
                    || string.IsNullOrWhiteSpace(currentCity.RuntimeId)
                    || !ReferenceEquals(currentLocation, currentCity.Location))))
        {
            return false;
        }

        NpcRuntime candidate = new NpcRuntime(runtimeId, npcData);
        candidate.currentCity = currentCity;
        candidate.currentLocation = currentLocation;
        staged = candidate;
        return true;
    }

    internal bool TryBindSelectedDailyV1Definition(
        DailyV1NpcDefinition definition,
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        Func<string, CityRuntime> resolveCityRuntime = null)
    {
		if (definition == null || definitions == null || bindings == null
			|| !bindings.TryGetAsset(definition, out NpcData assetIdentity)
			|| assetIdentity == null
			|| (npcData != null && !ReferenceEquals(assetIdentity, npcData))
			|| !string.Equals(definition.DefinitionId, assetIdentity.DefinitionId, StringComparison.Ordinal)
            || !definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition graphNode)
            || !ReferenceEquals(graphNode, definition))
            return false;

        if (selectedRuntimeOwners != null)
        {
            if (!ReferenceEquals(selectedDailyV1Definition, definition)
                || !ReferenceEquals(selectedDefinitionGraph, definitions)
                || selectedDailyV1StatusOwner == null
                || !HasConsistentSelectedCurrentActionSlot(
                    definitions, bindings, selectedRuntimeOwners.ActionSlot)
                || !ReferenceEquals(TravelPlan.SelectedOwner, selectedRuntimeOwners.TravelPlan)
                || !TravelPlan.MatchesSelectedOwner()
                || (selectedDefinitionBindings != null
                    && !ReferenceEquals(selectedDefinitionBindings, bindings)))
                return false;

            IReadOnlyList<DailyV1StatusDefinition> selectedStatuses = selectedDailyV1StatusOwner.Statuses;
            List<NpcStatusData> stagedStatusProjection = null;
            if (selectedStatuses == null)
                return false;
            if (selectedDefinitionBindings == null)
            {
                stagedStatusProjection = new List<NpcStatusData>(selectedStatuses.Count);
                foreach (DailyV1StatusDefinition statusNode in selectedStatuses)
                {
                    if (statusNode == null
                        || !definitions.Statuses.TryGetValue(statusNode.DefinitionId,
                            out DailyV1StatusDefinition admittedStatus)
                        || !ReferenceEquals(admittedStatus, statusNode)
                        || !bindings.TryGetAsset(statusNode, out NpcStatusData statusAsset)
                        || statusAsset == null
                        || !string.Equals(statusNode.DefinitionId, statusAsset.DefinitionId, StringComparison.Ordinal))
                        return false;
                    stagedStatusProjection.Add(statusAsset);
                }
            }
            else
            {
                if (currentStatus == null || selectedStatuses.Count != currentStatus.Count)
                    return false;
                for (int i = 0; i < selectedStatuses.Count; i++)
                {
                    DailyV1StatusDefinition statusNode = selectedStatuses[i];
                    NpcStatusData statusAsset = currentStatus[i];
                    if (statusNode == null || statusAsset == null
                        || !definitions.Statuses.TryGetValue(statusNode.DefinitionId,
                            out DailyV1StatusDefinition admittedStatus)
                        || !ReferenceEquals(admittedStatus, statusNode)
                        || !string.Equals(statusNode.DefinitionId, statusAsset.DefinitionId, StringComparison.Ordinal)
                        || !bindings.TryGetAsset(statusNode, out NpcStatusData exactStatusAsset)
                        || !ReferenceEquals(exactStatusAsset, statusAsset))
                        return false;
                }
            }

            if (ExistingInventory != null
                && ExistingInventory.SelectedDailyV1Owner != null
                && !ExistingInventory.TryBindSelectedDailyV1Owner(definitions, bindings))
                return false;
            if (CommercialKnowledge.SelectedOwner == null
                || !ReferenceEquals(CommercialKnowledge.SelectedOwner,
                    selectedRuntimeOwners.CommercialKnowledge)
                || !CommercialKnowledge.TryBindSelectedDailyV1Owner(
                    selectedRuntimeOwners.CommercialKnowledge, bindings))
                return false;
            if (!MerchantTradePlan.TryBindSelectedOwner(
                    selectedRuntimeOwners.MerchantTradePlan, bindings,
                    resolveCityRuntime ?? (_ => null))) return false;

            // A detached P12 stage already owns the canonical status nodes.
            // Bind only the Unity compatibility projection here; never rebuild
            // authoritative membership from NpcStatusData during rebind.
            if (stagedStatusProjection != null)
            {
                currentStatus = stagedStatusProjection;
                currentStatusView = null;
            }
            if (currentActionRuntime != null
                && !currentActionRuntime.TryBindSelectedOwner(
                    selectedRuntimeOwners.ActionSlot.CurrentPayload, definitions, bindings))
                return false;
            selectedDefinitionBindings = bindings;
            npcData = null;
            return true;
        }

        DailyV1ActionDefinition stagedCurrentAction = null;
        DailyV1NpcMerchantTradePlanOwner stagedMerchantPlan = selectedRuntimeOwners?.MerchantTradePlan
            ?? ExistingMerchantTradePlan?.SelectedOwner;
        if (stagedMerchantPlan == null
            && !DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(definitions, out stagedMerchantPlan))
            return false;
        if (!CommercialKnowledge.TryCreateSelectedOwner(definitions, bindings,
                out DailyV1CommercialKnowledgeOwner stagedCommercialKnowledge))
            return false;
        DailyV1NpcActionPayloadOwner stagedCurrentActionPayload = null;
        if (!HasConsistentCurrentActionSlot
            || (currentActionRuntime != null
                && (!TryResolveSelectedDailyV1Action(definitions, bindings,
                        currentActionRuntime.Action, out stagedCurrentAction)
                    || !currentActionRuntime.TryCreateSelectedOwner(
                        definitions, stagedCurrentAction, out stagedCurrentActionPayload)))
            || !TryCreateSelectedStatusNodesFromAssets(definitions, bindings, currentStatus,
                out List<DailyV1StatusDefinition> stagedStatuses)
            || !DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundNpcOwner(
                runtimeId, definitions, definition, stagedStatuses,
                ExistingInventory?.SelectedDailyV1Owner, moneyAccount,
                stagedCommercialKnowledge,
                stagedMerchantPlan,
                out DailyV1NpcRuntimeOwners stagedOwners))
            return false;
        if (!stagedOwners.ActionSlot.TrySetCurrentAction(stagedCurrentAction, stagedCurrentActionPayload)
            || !stagedOwners.ActionSlot.TryInstallRevision(currentActionRevision)
            || !stagedOwners.Lifecycle.TryInstallState(lifeState, injurySeverity, lifeStateRevision)
            || !stagedOwners.TryInstallCrimeJusticeRevision(p12CrimeJusticeRevision)
            || !stagedOwners.HiddenState.TryInstallExact(hiddenDaysRemaining)
            || travelPlan == null
            || !stagedOwners.TravelPlan.TryInstallExact(
                travelPlan.TargetLocation?.RuntimeId, travelPlan.TargetCity?.RuntimeId,
                travelPlan.Reason, travelPlan.Utility, travelPlan.ExpectedCost,
                travelPlan.OriginDecisionId, travelPlan.Revision)
            || !stagedOwners.Travel.TryInstallState(
                currentLocation?.RuntimeId, currentCity?.RuntimeId,
                destinationLocation?.RuntimeId, destinationCity?.RuntimeId,
                travelDaysRemaining, travelDaysTotal, travelRouteRuntimeId,
                travelStartedToday, travelOriginDecisionId, activeTravelPartyId,
                travelStateRevision)) return false;
        if (ExistingInventory != null
            && ExistingInventory.SelectedDailyV1Owner != null
            && !ExistingInventory.TryBindSelectedDailyV1Owner(definitions, bindings)) return false;
        if (!CommercialKnowledge.TryBindSelectedDailyV1Owner(stagedCommercialKnowledge, bindings)) return false;
        if (!MerchantTradePlan.TryBindSelectedOwner(
                stagedOwners.MerchantTradePlan, bindings,
                resolveCityRuntime ?? (_ => null))) return false;
        if (!TravelPlan.TryBindSelectedOwner(stagedOwners.TravelPlan)) return false;
        if (currentActionRuntime != null
            && !currentActionRuntime.TryBindSelectedOwner(
                stagedCurrentActionPayload, definitions, bindings)) return false;
        selectedRuntimeOwners = stagedOwners;
        p12CrimeJusticeRevision = 0L;
        selectedDefinitionBindings = bindings;
        return true;
    }

    private static bool TryCreateSelectedStatusNodesFromAssets(
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        IReadOnlyList<NpcStatusData> assets,
        out List<DailyV1StatusDefinition> nodes)
    {
        nodes = null;
        if (definitions == null || bindings == null || assets == null) return false;
        List<DailyV1StatusDefinition> resolved = new List<DailyV1StatusDefinition>(assets.Count);
        foreach (NpcStatusData asset in assets)
        {
            if (asset == null || !bindings.TryGet(asset, out DailyV1StatusDefinition node)
                || !definitions.Statuses.TryGetValue(node.DefinitionId, out DailyV1StatusDefinition admitted)
                || !ReferenceEquals(node, admitted))
                return false;
            resolved.Add(node);
        }
        nodes = resolved;
        return true;
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

    /// <summary>
    /// Installs validated exact owner values on a fresh unpublished NPC. This
    /// deliberately bypasses gameplay writers and does not advance revisions.
    /// </summary>
    internal bool TryInstallP12OwnerSnapshot(P12DNpcStagedOwnerState state)
    {
        if (state == null || p12LifecycleMutationAdmission != null
            || p12TravelStateMutationAdmission != null || p12CrimeJusticeMutationAdmission != null
            || state.NpcData == null
            || !string.Equals(state.NpcData.DefinitionId, state.NpcDefinitionId, StringComparison.Ordinal)
            || state.CurrentStatus == null || state.Inventory == null || state.MoneyAccount == null
            || state.MerchantTradePlan == null || state.TravelPlan == null
            || state.CommercialKnowledge == null || state.SpatialKnowledge == null
            || state.ExplorableSiteKnowledge == null || state.LocalTopologyKnowledge == null
            || state.AdventureSiteIntelKnowledge == null
            || (state.SelectedDefinitions != null
                && (state.SelectedMerchantTradePlanOwner == null
                    || !ReferenceEquals(state.SelectedMerchantTradePlanOwner.Definitions,
                        state.SelectedDefinitions)))
            || state.LifeStateRevision < 0L || state.ResidenceRevision < 0L
            || state.CrimeJusticeRevision < 0L || state.CurrentActionRevision < 0L
            || state.TravelStateRevision < 0L || state.HiddenDaysRemaining < 0
            || !Enum.IsDefined(typeof(NpcLifeState), state.LifeState)
            || !NpcInjuryRules.IsValid(state.InjurySeverity)
            || (state.CurrentCity != null && !ReferenceEquals(state.CurrentCity.Location, state.CurrentLocation))
            || (state.DestinationCity != null && !ReferenceEquals(state.DestinationCity.Location, state.DestinationLocation))
            || (state.CurrentActionRuntime != null
                && (state.CurrentActionRuntime.Action == null
                    || !string.Equals(state.CurrentActionRuntime.Action.DefinitionId,
                        state.CurrentActionDefinitionId, StringComparison.Ordinal)
                    || !state.CurrentActionRuntime.TryBindP12Owner(this)))
            || (state.CurrentActionRuntime == null && state.CurrentActionDefinitionId != null)
            || !string.Equals(state.SpatialKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)
            || !string.Equals(state.ExplorableSiteKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)
            || !string.Equals(state.LocalTopologyKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)
            || !string.Equals(state.AdventureSiteIntelKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal))
            return false;

        DailyV1NpcRuntimeOwners stagedSelectedOwners = null;
        if (state.SelectedDefinitions != null)
        {
            if (state.SelectedNpcDefinition == null
                || !state.SelectedDefinitions.Npcs.TryGetValue(state.NpcDefinitionId,
                    out DailyV1NpcDefinition admittedNpc)
                || !ReferenceEquals(admittedNpc, state.SelectedNpcDefinition)
                || !string.Equals(state.SelectedNpcDefinition.DefinitionId,
                    state.NpcDefinitionId, StringComparison.Ordinal)
                || state.SelectedCurrentStatusDefinitions == null
                || state.SelectedCurrentStatusDefinitions.Count != state.CurrentStatus.Count
                || state.Inventory.SelectedDailyV1Owner == null
                || !ReferenceEquals(state.Inventory.SelectedDailyV1Owner.Definitions,
                    state.SelectedDefinitions)
                || state.CommercialKnowledge.SelectedOwner == null
                || !ReferenceEquals(state.CommercialKnowledge.SelectedOwner.Definitions,
                    state.SelectedDefinitions))
                return false;

            for (int i = 0; i < state.SelectedCurrentStatusDefinitions.Count; i++)
            {
                DailyV1StatusDefinition selectedStatus = state.SelectedCurrentStatusDefinitions[i];
                NpcStatusData statusAsset = state.CurrentStatus[i];
                if (selectedStatus == null || statusAsset == null
                    || !state.SelectedDefinitions.Statuses.TryGetValue(selectedStatus.DefinitionId,
                        out DailyV1StatusDefinition admittedStatus)
                    || !ReferenceEquals(admittedStatus, selectedStatus)
                    || !string.Equals(selectedStatus.DefinitionId,
                        statusAsset.DefinitionId, StringComparison.Ordinal))
                    return false;
            }

            if (!DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundNpcOwner(
                    runtimeId,
                    state.SelectedDefinitions,
                    state.SelectedNpcDefinition,
                    state.SelectedCurrentStatusDefinitions,
                    state.Inventory.SelectedDailyV1Owner,
                    state.MoneyAccount,
                    state.CommercialKnowledge.SelectedOwner,
                    state.SelectedMerchantTradePlanOwner,
                    out stagedSelectedOwners))
                return false;

            DailyV1ActionDefinition stagedCurrentAction = null;
            if (state.CurrentActionDefinitionId != null
                && (!state.SelectedDefinitions.Actions.TryGetValue(
                        state.CurrentActionDefinitionId, out stagedCurrentAction)
                    || stagedCurrentAction == null))
                return false;
            NpcTravelPlanState stagedTravelPlanState = state.TravelPlan.CaptureOwnerState();
            if (stagedTravelPlanState == null) return false;
            DailyV1NpcActionPayloadOwner stagedCurrentActionPayload = null;
            if (state.CurrentActionRuntime != null
                && !state.CurrentActionRuntime.TryCreateSelectedOwner(
                    state.SelectedDefinitions, stagedCurrentAction, out stagedCurrentActionPayload))
                return false;
            if (!stagedSelectedOwners.ActionSlot.TrySetCurrentAction(
                    stagedCurrentAction, stagedCurrentActionPayload)
                || !stagedSelectedOwners.ActionSlot.TryInstallRevision(state.CurrentActionRevision)
                || !stagedSelectedOwners.Lifecycle.TryInstallState(
                    state.LifeState, state.InjurySeverity, state.LifeStateRevision)
                || !stagedSelectedOwners.TryInstallCrimeJusticeRevision(state.CrimeJusticeRevision)
                || !stagedSelectedOwners.HiddenState.TryInstallExact(state.HiddenDaysRemaining)
                || !stagedSelectedOwners.TravelPlan.TryInstallExact(
                    stagedTravelPlanState.TargetLocation?.RuntimeId,
                    stagedTravelPlanState.TargetCity?.RuntimeId,
                    stagedTravelPlanState.Reason, stagedTravelPlanState.Utility,
                    stagedTravelPlanState.ExpectedCost, stagedTravelPlanState.OriginDecisionId,
                    state.TravelPlan.Revision)
                || !state.TravelPlan.TryBindSelectedOwner(stagedSelectedOwners.TravelPlan)
                || (state.CurrentActionRuntime != null
                    && !state.CurrentActionRuntime.TryBindSelectedOwner(
                        stagedCurrentActionPayload, state.SelectedDefinitions, null)))
                return false;
        }
        else if (state.SelectedNpcDefinition != null
            || state.SelectedCurrentStatusDefinitions != null)
        {
            return false;
        }

        PersonId stagedPersonId = null;
        if (state.PersonIdValue != null && !PersonId.TryCreate(state.PersonIdValue, out stagedPersonId))
            return false;
        if ((stagedPersonId == null) != (state.PersonRuntime == null)
            || (state.PersonRuntime != null && state.PersonRuntime.PersonId != stagedPersonId))
            return false;

        personIdentity = stagedPersonId;
        personIdValue = stagedPersonId?.Value;
        personRuntime = state.PersonRuntime;
        selectedRuntimeOwners = stagedSelectedOwners;
        selectedDefinitionBindings = null;
        residenceSettlementRuntimeId = state.ResidenceSettlementRuntimeId;
        lifeState = state.LifeState;
        injurySeverity = state.InjurySeverity;
        currentStatus = new List<NpcStatusData>(state.CurrentStatus);
        currentStatusView = null;
        currentAction = state.CurrentActionRuntime?.Action;
        currentActionRuntime = state.CurrentActionRuntime;
        inventory = state.Inventory;
        moneyAccount = state.MoneyAccount;
        destinationLocation = state.DestinationLocation;
        destinationCity = state.DestinationCity;
        travelDaysRemaining = state.TravelDaysRemaining;
        travelDaysTotal = state.TravelDaysTotal;
        travelRouteRuntimeId = state.TravelRouteRuntimeId;
        travelStartedToday = state.TravelStartedToday;
        travelOriginDecisionId = state.TravelOriginDecisionId;
        activeTravelPartyId = state.ActiveTravelPartyId;
        hiddenDaysRemaining = state.HiddenDaysRemaining;
        merchantTradePlan = state.MerchantTradePlan;
        travelPlan = state.TravelPlan;
        commercialKnowledge = state.CommercialKnowledge;
        spatialKnowledge = state.SpatialKnowledge;
        explorableSiteKnowledge = state.ExplorableSiteKnowledge;
        localTopologyKnowledge = state.LocalTopologyKnowledge;
        adventureSiteIntelKnowledge = state.AdventureSiteIntelKnowledge;
        lifeStateRevision = stagedSelectedOwners == null ? state.LifeStateRevision : 0L;
        residenceRevision = state.ResidenceRevision;
        p12CrimeJusticeRevision = stagedSelectedOwners == null ? state.CrimeJusticeRevision : 0L;
        currentActionRevision = stagedSelectedOwners == null ? state.CurrentActionRevision : 0L;
        travelStateRevision = state.TravelStateRevision;
        if (stagedSelectedOwners != null
            && !TryInstallSelectedTravelProjection(stagedSelectedOwners.Travel, state.TravelStateRevision))
            return false;
        return true;
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
            return HasConsistentCurrentActionSlot;
        DailyV1ActionDefinition selectedActionNode = null;
        DailyV1NpcActionPayloadOwner selectedActionPayload = null;
        if (selectedRuntimeOwners != null
            && (!HasConsistentCurrentActionSlot
                || selectedDefinitionBindings == null
                || (actionRuntime != null
                    && !TryResolveSelectedDailyV1Action(
                        selectedDefinitionGraph, selectedDefinitionBindings,
                        actionRuntime.Action, out selectedActionNode))))
            return false;
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

        if (selectedRuntimeOwners != null && actionRuntime != null
            && !actionRuntime.TryCreateSelectedOwner(
                selectedDefinitionGraph, selectedActionNode, out selectedActionPayload))
            return false;

        NpcActionRuntime previous = currentActionRuntime;
        if (selectedRuntimeOwners != null && actionRuntime != null
            && !actionRuntime.TryBindSelectedOwner(
                selectedActionPayload, selectedDefinitionGraph, selectedDefinitionBindings))
            return false;
        if (p12Bound && actionRuntime != null && !actionRuntime.TryBindP12Owner(this))
        {
            actionRuntime?.UnbindSelectedOwner();
            return false;
        }
        if (p12Bound && previous != null && !previous.UnbindP12Owner(this))
        {
            actionRuntime?.UnbindP12Owner(this);
            actionRuntime?.UnbindSelectedOwner();
            return false;
        }

        if (selectedRuntimeOwners != null
            && !selectedRuntimeOwners.ActionSlot.TrySetCurrentAction(
                selectedActionNode, selectedActionPayload))
        {
            if (p12Bound && previous != null) previous.TryBindP12Owner(this);
            if (p12Bound && actionRuntime != null) actionRuntime.UnbindP12Owner(this);
            actionRuntime?.UnbindSelectedOwner();
            return false;
        }

        if (previous != null && !ReferenceEquals(previous, actionRuntime))
            previous.UnbindSelectedOwner();
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
        selectedRuntimeOwners?.ActionSlot.TrySetCurrentAction(null);
        currentAction = null;
        currentActionRuntime = null;
        previous?.UnbindP12Owner(this);
        previous?.UnbindSelectedOwner();
    }

    private bool TrySetInjurySeverityAfterValidation(NpcInjurySeverity severity)
    {
        if (selectedRuntimeOwners != null)
        {
            if (!selectedRuntimeOwners.Lifecycle.TrySetInjurySeverity(severity)) return false;
            injurySeverity = selectedRuntimeOwners.Lifecycle.InjurySeverity;
            return true;
        }

        if (!NpcInjuryRules.IsValid(severity)) return false;
        if (severity > injurySeverity) injurySeverity = severity;
        return true;
    }

    private bool TrySetLifeStateAfterValidation(NpcLifeState next)
    {
        if (selectedRuntimeOwners != null
            && !selectedRuntimeOwners.Lifecycle.TrySetLifeState(next))
            return false;
        if (!NpcInjuryRules.IsValid(next)) return false;
        lifeState = next;
        return true;
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

        if (severity > InjurySeverity && !TrySetInjurySeverityAfterValidation(severity)) return false;

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

        if (!TrySetLifeStateAfterValidation(NpcLifeState.Dead)) return false;
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

        if (severity > InjurySeverity && !TrySetInjurySeverityAfterValidation(severity)) return false;

        if (!TrySetLifeStateAfterValidation(NpcLifeState.Dead)) return false;
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

        if (severity > InjurySeverity && !TrySetInjurySeverityAfterValidation(severity)) return false;

        if (!TrySetLifeStateAfterValidation(NpcLifeState.Dead)) return false;
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

        if (!PersonRuntimeOwnerBridge.RecordDeathAfterValidation(personRuntime, personDeathTransition.DeathAbsoluteDay))
            return false;

        if (severity > InjurySeverity && !TrySetInjurySeverityAfterValidation(severity)) return false;

        if (!TrySetLifeStateAfterValidation(NpcLifeState.Dead)) return false;
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
            && (shouldDie == false || (LifeState == NpcLifeState.Alive
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
        return NpcInjuryRules.GetCapabilityMultiplier(InjurySeverity);
    }

    public void AddStatus(NpcStatusData status)
    {
        if (status == null) return;
        if (selectedDailyV1Definition != null)
        {
            if (selectedDailyV1StatusOwner == null || selectedDefinitionBindings == null
                || !selectedDefinitionBindings.TryGet(status, out DailyV1StatusDefinition statusNode)) return;
            ApplySelectedDailyV1StatusChanges(
                Array.Empty<DailyV1StatusDefinition>(), new[] { statusNode });
            return;
        }
        if (currentStatus == null) currentStatus = new List<NpcStatusData>();
        if (currentStatus.Contains(status)) return;
        EnsureP12CrimeJusticeMutationAllowed();
        currentStatus.Add(status);
        NotifyP12CrimeJusticeMutationCommitted();
    }

    public void RemoveStatus(NpcStatusData status)
    {
        if (selectedDailyV1Definition != null)
        {
            if (status == null || selectedDailyV1StatusOwner == null || selectedDefinitionBindings == null
                || !selectedDefinitionBindings.TryGet(status, out DailyV1StatusDefinition statusNode)) return;
            ApplySelectedDailyV1StatusChanges(
                new[] { statusNode }, Array.Empty<DailyV1StatusDefinition>());
            return;
        }
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
            || (selectedRuntimeOwners != null
                ? !selectedRuntimeOwners.CanAdvanceCrimeJusticeRevision(1)
                : p12CrimeJusticeRevision == long.MaxValue))
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
        if (selectedRuntimeOwners != null)
            selectedRuntimeOwners.NotifyCrimeJusticeMutationCommitted();
        else
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

        if (selectedRuntimeOwners != null)
            return TrySetSelectedCurrentPresence(location, cityProjection);

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
        else
            InstallSelectedTravelProjection(TravelStateRevision);

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

        if (selectedRuntimeOwners != null)
            return TryStartSelectedTravel(destination, destinationCityProjection, travelDays, originDecisionId, routeRuntimeId);

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
        travelDaysRemaining = UnityMathfCompatibility.Max(1, travelDays);
        travelDaysTotal = travelDaysRemaining;
        travelRouteRuntimeId = string.IsNullOrWhiteSpace(routeRuntimeId) == true ? null : routeRuntimeId;
        travelStartedToday = true;
        travelOriginDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
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
            && TravelStateRevision <= long.MaxValue - increments;
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
        if (selectedRuntimeOwners != null)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                    selectedRuntimeOwners, travelPartyId, out DailyV1SelectedTravelTransitionPlan selectedPlan))
                return false;
            if (selectedPlan.IsNoOp) return true;
            return TryCommitSelectedTravelTransition(
                selectedPlan, Array.Empty<CityRuntime>(),
                currentLocation, currentCity, destinationLocation, destinationCity,
                notifyBoundary: true);
        }

        string next = string.IsNullOrWhiteSpace(travelPartyId) == true ? null : travelPartyId;
        if (string.Equals(activeTravelPartyId, next, StringComparison.Ordinal)) return true;
        if (!CanCommitP12TravelStateMutation(true, Array.Empty<CityRuntime>())) return false;
        activeTravelPartyId = next;
        NotifyP12TravelStateMutationCommitted(true, Array.Empty<CityRuntime>());
        return true;
    }

    public bool ClearTravelStartedToday()
    {
        if (selectedRuntimeOwners != null)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
                    selectedRuntimeOwners, out DailyV1SelectedTravelTransitionPlan selectedPlan))
                return false;
            if (selectedPlan.IsNoOp) return true;
            return TryCommitSelectedTravelTransition(
                selectedPlan, Array.Empty<CityRuntime>(),
                currentLocation, currentCity, destinationLocation, destinationCity,
                notifyBoundary: true);
        }

        if (!travelStartedToday) return true;
        if (!CanCommitP12TravelStateMutation(true, Array.Empty<CityRuntime>())) return false;
        travelStartedToday = false;
        NotifyP12TravelStateMutationCommitted(true, Array.Empty<CityRuntime>());
        return true;
    }

    public bool AdvanceTravelDay(out CityRuntime arrivedCity)
    {
        arrivedCity = null;

        if (selectedRuntimeOwners != null)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
                    selectedRuntimeOwners,
                    destinationCity?.SelectedRuntimeOwners,
                    out DailyV1SelectedTravelTransitionPlan selectedPlan,
                    out bool selectedArriving))
                return false;
            if (!TryCommitPreparedSelectedTravelDay(selectedPlan, selectedArriving, out arrivedCity))
                return false;
            return selectedArriving;
        }

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

        travelDaysRemaining = UnityMathfCompatibility.Max(0, travelDaysRemaining - 1);

        if (travelDaysRemaining > 0)
        {
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

        NotifyP12TravelStateMutationCommitted(true, changedCities);

        return true;
    }

    /// <summary>
    /// Applies an already prepared Core travel-day transition through this
    /// runtime's established P12 admission and Unity projection boundary.
    /// </summary>
    internal bool TryCommitPreparedSelectedTravelDay(
        DailyV1SelectedTravelTransitionPlan plan,
        bool arriving,
        out CityRuntime arrivedCity)
    {
        arrivedCity = null;
        if (selectedRuntimeOwners == null || plan == null) return false;

        CityRuntime nextCurrentCity = arriving ? destinationCity : currentCity;
        SpatialLocationRuntime nextCurrentLocation = arriving ? destinationLocation : currentLocation;
        CityRuntime nextDestinationCity = arriving ? null : destinationCity;
        SpatialLocationRuntime nextDestinationLocation = arriving ? null : destinationLocation;
        CityRuntime[] projectionCandidates = arriving && destinationCity != null
            ? new[] { destinationCity }
            : Array.Empty<CityRuntime>();
        if (!TryCommitSelectedTravelTransition(
                plan, projectionCandidates,
                nextCurrentLocation, nextCurrentCity, nextDestinationLocation, nextDestinationCity,
                notifyBoundary: true))
            return false;

        if (arriving) arrivedCity = nextCurrentCity;
        return true;
    }

    public bool CancelTravel(SpatialLocationRuntime originLocation, CityRuntime originCityProjection)
    {
        if (selectedRuntimeOwners != null)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TryCancelTravel(
                    selectedRuntimeOwners,
                    originLocation?.RuntimeId,
                    originCityProjection?.SelectedRuntimeOwners,
                    out DailyV1SelectedTravelTransitionPlan selectedPlan))
                return false;
            CityRuntime[] projectionCandidates = originCityProjection == null
                ? Array.Empty<CityRuntime>()
                : new[] { originCityProjection };
            return TryCommitSelectedTravelTransition(
                selectedPlan, projectionCandidates,
                originLocation, originCityProjection, null, null,
                notifyBoundary: true);
        }

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

        NotifyP12TravelStateMutationCommitted(true, changedCities);

        return true;
    }

    public bool CancelTravel(CityRuntime originCity)
    {
        return CancelTravel(originCity?.Location, originCity);
    }

    internal bool ClearCurrentPresenceFromCity(CityRuntime city)
    {
        if (selectedRuntimeOwners != null)
        {
            if (!DailyV1SelectedTravelTransitionPlan.TryClearCurrentPresence(
                    selectedRuntimeOwners,
                    city?.SelectedRuntimeOwners,
                    out DailyV1SelectedTravelTransitionPlan selectedPlan))
                return false;
            if (!city.CanRemoveImportantNpcMembership(this)
                || !city.CanPrepareSelectedNpcPresenceProjection(this, shouldContain: false)
                || !selectedPlan.TryCommit()
                || !city.TryApplySelectedNpcPresenceProjection(this, shouldContain: false))
                return false;
            currentCity = null;
            currentLocation = null;
            if (!HasConsistentSelectedTravelState)
                throw new InvalidOperationException("The cleared NPC presence differs from its committed Core owner.");
            return true;
        }

        if (currentCity == city)
        {
            currentCity = null;
            currentLocation = null;
            InstallSelectedTravelProjection(TravelStateRevision);
        }
        return true;
    }

    private bool TrySetSelectedCurrentPresence(SpatialLocationRuntime location, CityRuntime cityProjection)
    {
        if (!HasConsistentSelectedTravelState
            || (cityProjection != null && cityProjection.SelectedRuntimeOwners == null)
            || !DailyV1SelectedTravelTransitionPlan.TrySetCurrentPresence(
                selectedRuntimeOwners,
                currentCity?.SelectedRuntimeOwners,
                currentLocation?.RuntimeId,
                cityProjection?.SelectedRuntimeOwners,
                location?.RuntimeId,
                out DailyV1SelectedTravelTransitionPlan plan))
            return false;

        CityRuntime[] candidates = currentCity == null
            ? cityProjection == null ? Array.Empty<CityRuntime>() : new[] { cityProjection }
            : cityProjection == null || ReferenceEquals(currentCity, cityProjection)
                ? new[] { currentCity }
                : new[] { currentCity, cityProjection };
        return TryCommitSelectedTravelTransition(
            plan, candidates,
            location, cityProjection, destinationLocation, destinationCity,
            notifyBoundary: true,
            admitNoOp: true);
    }

    private bool TryStartSelectedTravel(
        SpatialLocationRuntime destination,
        CityRuntime destinationCityProjection,
        int travelDays,
        string originDecisionId,
        string routeRuntimeId)
    {
        DailyV1CityRuntimeOwners coreCurrentCity = null;
        if (selectedRuntimeOwners == null
            || selectedRuntimeOwners.Travel == null
            || !string.Equals(selectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
                currentLocation?.RuntimeId, StringComparison.Ordinal)
            || (selectedRuntimeOwners.Travel.CurrentCityRuntimeId != null
                && (currentCity?.SelectedRuntimeOwners == null
                    || !string.Equals(currentCity.SelectedRuntimeOwners.RuntimeId,
                        selectedRuntimeOwners.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)))
            || (selectedRuntimeOwners.Travel.CurrentCityRuntimeId == null
                && currentCity != null
                && (currentCity.SelectedRuntimeOwners == null
                    || !ReferenceEquals(currentCity.SelectedRuntimeOwners.Definitions, selectedRuntimeOwners.Definitions)
                    || currentCity.SelectedRuntimeOwners.NpcPresence.Contains(RuntimeId)
                    || currentCity.ContainsImportantNpc(this)))
            || (destinationCityProjection != null && destinationCityProjection.SelectedRuntimeOwners == null))
            return false;

        if (selectedRuntimeOwners.Travel.CurrentCityRuntimeId != null)
            coreCurrentCity = currentCity.SelectedRuntimeOwners;

        if (!DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                selectedRuntimeOwners,
                coreCurrentCity,
                selectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
                destination?.RuntimeId,
                destinationCityProjection?.SelectedRuntimeOwners,
                travelDays,
                originDecisionId,
                routeRuntimeId,
                out DailyV1SelectedTravelTransitionPlan plan))
            return false;

        CityRuntime[] candidates = coreCurrentCity == null
            ? Array.Empty<CityRuntime>()
            : new[] { currentCity };
        return TryCommitSelectedTravelTransition(
            plan, candidates,
            null, null, destination, destinationCityProjection,
            notifyBoundary: true,
            allowUnprojectedCurrentCityProjection: currentCity != null && coreCurrentCity == null);
    }

    private bool TryCommitSelectedTravelTransition(
        DailyV1SelectedTravelTransitionPlan plan,
        IReadOnlyList<CityRuntime> projectionCandidates,
        SpatialLocationRuntime nextCurrentLocation,
        CityRuntime nextCurrentCity,
        SpatialLocationRuntime nextDestinationLocation,
        CityRuntime nextDestinationCity,
        bool notifyBoundary,
        bool admitNoOp = false,
        bool allowUnprojectedCurrentCityProjection = false)
    {
        if (plan == null || selectedRuntimeOwners == null
            || (!HasConsistentSelectedTravelState
                && !(allowUnprojectedCurrentCityProjection && IsUnprojectedCurrentCityProjection())))
            return false;

        foreach (CityRuntime candidate in projectionCandidates ?? Array.Empty<CityRuntime>())
            if (candidate != null && !candidate.HasConsistentSelectedNpcPresenceProjection(this))
                return false;

        List<CityRuntime> changedCities = new List<CityRuntime>();
        foreach (DailyV1CityRuntimeOwners changedOwner in plan.ChangedCityOwners)
        {
            CityRuntime projection = null;
            foreach (CityRuntime candidate in projectionCandidates ?? Array.Empty<CityRuntime>())
            {
                if (candidate != null && ReferenceEquals(candidate.SelectedRuntimeOwners, changedOwner))
                {
                    projection = candidate;
                    break;
                }
            }
            if (projection == null || !plan.TryGetPresenceChange(changedOwner, out bool add))
                return false;
            if ((add ? !projection.CanAddImportantNpcMembership(this)
                     : !projection.CanRemoveImportantNpcMembership(this))
                || !projection.CanPrepareSelectedNpcPresenceProjection(this, add))
                return false;
            changedCities.Add(projection);
        }

        Func<bool> preCommitAdmission = null;
        if (!plan.IsNoOp || admitNoOp)
            preCommitAdmission = () => CanCommitP12TravelStateMutation(
                plan.TravelRevisionChanges, changedCities);
        DailyV1SelectedRuntimeOwnerGraph selectedGraph = selectedRuntimeOwnerGraphProvider?.Invoke();
        bool committed = selectedGraph != null
            && selectedGraph.ContainsExactNpcOwner(selectedRuntimeOwners)
                ? selectedGraph.TryCommitTravelTransition(selectedRuntimeOwners, plan, preCommitAdmission)
                : plan.TryCommit(preCommitAdmission);
        if (!committed) return false;

        foreach (CityRuntime changedCity in changedCities)
        {
            bool shouldContain = changedCity.SelectedRuntimeOwners.NpcPresence.Contains(RuntimeId);
            if (!changedCity.TryApplySelectedNpcPresenceProjection(this, shouldContain))
                throw new InvalidOperationException("A committed Core presence transition could not update its Unity object projection.");
        }

        currentLocation = nextCurrentLocation;
        currentCity = nextCurrentCity;
        destinationLocation = nextDestinationLocation;
        destinationCity = nextDestinationCity;
        travelDaysRemaining = selectedRuntimeOwners.Travel.TravelDaysRemaining;
        travelDaysTotal = selectedRuntimeOwners.Travel.TravelDaysTotal;
        travelRouteRuntimeId = selectedRuntimeOwners.Travel.TravelRouteRuntimeId;
        travelStartedToday = selectedRuntimeOwners.Travel.TravelStartedToday;
        travelOriginDecisionId = selectedRuntimeOwners.Travel.TravelOriginDecisionId;
        activeTravelPartyId = selectedRuntimeOwners.Travel.ActiveTravelPartyId;
        if (!HasConsistentSelectedTravelState)
            throw new InvalidOperationException("The selected NPC projection differs from the committed Core travel owner.");

        if (notifyBoundary && !plan.IsNoOp
            && (changedCities.Count != 0 || plan.TravelRevisionChanges))
            NotifyP12TravelStateMutationCommitted(plan.TravelRevisionChanges, changedCities);
        return true;
    }

    private bool IsUnprojectedCurrentCityProjection() =>
        selectedRuntimeOwners?.Travel != null
        && selectedRuntimeOwners.Travel.CurrentCityRuntimeId == null
        && string.Equals(selectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
            currentLocation?.RuntimeId, StringComparison.Ordinal)
        && currentCity?.SelectedRuntimeOwners != null
        && ReferenceEquals(currentCity.SelectedRuntimeOwners.Definitions, selectedRuntimeOwners.Definitions)
        && !currentCity.SelectedRuntimeOwners.NpcPresence.Contains(RuntimeId)
        && !currentCity.ContainsImportantNpc(this);

    internal bool SetResidenceSettlementRuntimeId(string settlementRuntimeId)
    {
        if (personRuntime != null)
        {
            return PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(personRuntime, settlementRuntimeId);
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
        int followingFullDays = UnityMathfCompatibility.Max(1, days);
        int durationIncludingCurrentDay = followingFullDays + 1;
        int current = HiddenDaysRemaining;
        int next = UnityMathfCompatibility.Max(current, durationIncludingCurrentDay);
        if (next == current) return;
        EnsureP12CrimeJusticeMutationAllowed();
        if (selectedRuntimeOwners != null)
        {
            if (!selectedRuntimeOwners.HiddenState.TrySetRemainingDays(next))
                throw new InvalidOperationException("The selected NPC hidden-day owner rejected a valid hidden duration.");
            hiddenDaysRemaining = next;
        }
        else
        {
            hiddenDaysRemaining = next;
        }
        NotifyP12CrimeJusticeMutationCommitted();
    }

    public bool AdvanceHiddenDay()
    {
        int current = HiddenDaysRemaining;
        if (current <= 0)
        {
            return false;
        }

        EnsureP12CrimeJusticeMutationAllowed();
        int next = UnityMathfCompatibility.Max(0, current - 1);
        if (selectedRuntimeOwners != null)
        {
            if (!selectedRuntimeOwners.HiddenState.TrySetRemainingDays(next))
                throw new InvalidOperationException("The selected NPC hidden-day owner rejected a valid daily decrement.");
            hiddenDaysRemaining = next;
        }
        else
        {
            hiddenDaysRemaining = next;
        }
        NotifyP12CrimeJusticeMutationCommitted();
        return next <= 0;
    }

    public void ClearHidden()
    {
        if (HiddenDaysRemaining == 0) return;
        EnsureP12CrimeJusticeMutationAllowed();
        if (selectedRuntimeOwners != null)
        {
            if (!selectedRuntimeOwners.HiddenState.TrySetRemainingDays(0))
                throw new InvalidOperationException("The selected NPC hidden-day owner rejected clearing hidden state.");
            hiddenDaysRemaining = 0;
        }
        else
        {
            hiddenDaysRemaining = 0;
        }
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
