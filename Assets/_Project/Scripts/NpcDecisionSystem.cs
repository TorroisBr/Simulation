using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class NpcDecisionSystem : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private readonly IAuthoritativeRandomSource randomSource;

    public NpcDecisionSystem(
        List<INpcActionProvider> actionProviders,
        IAuthoritativeRandomSource randomSource = null)
    {
        this.randomSource = randomSource ?? new DeterministicRandomSource();

        if (actionProviders == null)
        {
            return;
        }

        foreach (INpcActionProvider actionProvider in actionProviders)
        {
            if (actionProvider != null)
            {
                this.actionProviders.Add(actionProvider);
            }
        }
    }

    public NpcActionRuntime ChooseAction(NpcRuntime npcRuntime, List<NpcActionData> availableActions)
    {
        return ChooseAction(npcRuntime, availableActions, 0L);
    }

    public NpcActionRuntime ChooseAction(
        NpcRuntime npcRuntime,
        List<NpcActionData> availableActions,
        long currentAbsoluteDay)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return null;
        }

        if (npcRuntime == null || npcRuntime.IsAlive == false)
        {
            return null;
        }

        List<NpcActionData> validActions = GetAllValidActions(npcRuntime, availableActions);
        Dictionary<NpcActionRuntime, float> utilities = CalculateActionUtilities(
            npcRuntime,
            validActions,
            true);
        return ChooseWeightedAction(
            npcRuntime,
            utilities,
            "npc-decision|" + npcRuntime.RuntimeId + "|" + currentAbsoluteDay);
    }

    internal NpcActionRuntime ChooseSelectedDailyV1Action(
        NpcRuntime npcRuntime,
        List<DailyV1ActionDefinition> availableActions,
        long currentAbsoluteDay,
        PersonStore personStore = null)
    {
        if (mutationGuardBinding.CanMutate == false || npcRuntime == null || npcRuntime.IsAlive == false)
            return null;

        List<DailyV1StatusDefinition> activeStatuses = new List<DailyV1StatusDefinition>();
        DailyV1SelectedRuntimeOwnerGraph graph = npcRuntime.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners actor = npcRuntime.SelectedRuntimeOwners;
        if (availableActions == null
            || !npcRuntime.TryGetSelectedDailyV1ActiveStatusNodes(activeStatuses))
            return null;

        SelectedNpcActionDecisionBoundary boundary =
            new SelectedNpcActionDecisionBoundary(this, npcRuntime);
        DailyV1NpcActionCandidate selected;
        if (graph == null)
        {
            if (!DailyV1SelectedNpcActionDecisionStage.TryChooseWithoutRuntimeOwners(
                    npcRuntime.SelectedDailyV1DefinitionGraph,
                    npcRuntime.SelectedDailyV1Definition,
                    actor,
                    npcRuntime.RuntimeId,
                    availableActions,
                    activeStatuses,
                    currentAbsoluteDay,
                    randomSource,
                    boundary,
                    out selected))
                return null;
        }
        else if (graph == null
            || actor == null
            || !ReferenceEquals(graph.Definitions, npcRuntime.SelectedDailyV1DefinitionGraph)
            || !ReferenceEquals(actor.Definitions, npcRuntime.SelectedDailyV1DefinitionGraph)
            || !graph.ContainsExactNpcOwner(actor)
            || !DailyV1SelectedNpcActionDecisionStage.TryChoose(
                graph, actor, availableActions, activeStatuses, currentAbsoluteDay,
                randomSource, boundary, out selected, personStore))
            return null;

        if (selected == null)
            return null;

        if (boundary.TryGetProjectedAction(selected, out NpcActionRuntime selectedProviderAction))
            return selectedProviderAction;

        if (selected.PreparedPayload != null
            && npcRuntime.TryGetSelectedDailyV1ActionAsset(
                selected.Action, out NpcActionData selectedPayloadAction))
        {
            NpcActionRuntime selectedPayloadProjection = new NpcActionRuntime(selectedPayloadAction);
            return selectedPayloadProjection.TryUseSelectedPayload(
                selected.PreparedPayload, npcRuntime.SelectedDailyV1DefinitionGraph)
                ? selectedPayloadProjection
                : null;
        }

        return npcRuntime.TryGetSelectedDailyV1ActionAsset(selected.Action, out NpcActionData selectedAsset)
            ? new NpcActionRuntime(selectedAsset)
            : null;
    }

    internal bool TryCreateSelectedDailyV1ActionDecisionBoundary(
        NpcRuntime npcRuntime,
        out IDailyV1SelectedNpcActionDecisionBoundary boundary)
    {
        boundary = null;
        if (npcRuntime == null || !npcRuntime.UsesSelectedDailyV1Definitions
            || npcRuntime.SelectedRuntimeOwners == null
            || npcRuntime.SelectedRuntimeOwnerGraph == null
            || !npcRuntime.SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(
                npcRuntime.SelectedRuntimeOwners)
            || !ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph.Definitions,
                npcRuntime.SelectedDailyV1DefinitionGraph))
            return false;

        boundary = new SelectedNpcActionDecisionBoundary(this, npcRuntime);
        return true;
    }

    internal bool TryProjectSelectedDailyV1ActionCandidate(
        NpcRuntime npcRuntime,
        DailyV1NpcActionCandidate candidate,
        IDailyV1SelectedNpcActionDecisionBoundary boundary,
        out NpcActionRuntime actionRuntime)
    {
        actionRuntime = null;
        if (npcRuntime == null || candidate?.Action == null
            || !npcRuntime.UsesSelectedDailyV1Definitions
            || !ReferenceEquals(npcRuntime.SelectedRuntimeOwners?.Definitions,
                npcRuntime.SelectedDailyV1DefinitionGraph)
            || !npcRuntime.TryGetSelectedDailyV1ActionAsset(
                candidate.Action, out NpcActionData actionAsset))
            return false;

        if (boundary is SelectedNpcActionDecisionBoundary selectedBoundary
            && selectedBoundary.TryGetProjectedAction(candidate,
                out NpcActionRuntime projectedAction))
        {
            actionRuntime = projectedAction;
            return true;
        }

        NpcActionRuntime projection = new NpcActionRuntime(actionAsset);
        if (candidate.PreparedPayload != null
            && !projection.TryUseSelectedPayload(
                candidate.PreparedPayload, npcRuntime.SelectedDailyV1DefinitionGraph))
            return false;

        actionRuntime = projection;
        return true;
    }

    private sealed class SelectedNpcActionDecisionBoundary
        : IDailyV1SelectedNpcActionDecisionBoundary,
            IDailyV1SelectedGuardActionProjectionBoundary
    {
        private readonly NpcDecisionSystem decisionSystem;
        private readonly NpcRuntime npcRuntime;
        private readonly Dictionary<DailyV1NpcActionCandidate, NpcActionRuntime> projectedActions =
            new Dictionary<DailyV1NpcActionCandidate, NpcActionRuntime>();

        internal SelectedNpcActionDecisionBoundary(
            NpcDecisionSystem decisionSystem,
            NpcRuntime npcRuntime)
        {
            this.decisionSystem = decisionSystem;
            this.npcRuntime = npcRuntime;
        }

        public bool HasTravelActionProvider(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action) =>
            TryGetActionAsset(actor, action, out NpcActionData asset)
            && decisionSystem.GetProviderForAction(npcRuntime, asset) is TravelActionProvider;

        public bool TryProjectTravelAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            if (!IsExactActor(actor)
                || payload == null
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || decisionSystem.GetProviderForAction(npcRuntime, actionAsset)
                    is not TravelActionProvider)
                return false;

            CityRuntime targetCity = npcRuntime.TravelPlan?.TargetCity;
            DailyV1CityRuntimeOwners targetOwner = null;
            foreach (DailyV1CityRuntimeOwners cityOwner in npcRuntime.SelectedRuntimeOwnerGraph.Cities)
                if (string.Equals(cityOwner.RuntimeId, payload.TargetCityRuntimeId,
                        StringComparison.Ordinal))
                {
                    targetOwner = cityOwner;
                    break;
                }
            if (targetCity == null || targetOwner == null
                || !ReferenceEquals(targetCity.SelectedRuntimeOwners, targetOwner)
                || !string.Equals(targetCity.RuntimeId, targetOwner.RuntimeId,
                    StringComparison.Ordinal))
                return false;

            NpcActionRuntime projected = new NpcActionRuntime(
                actionAsset, targetCity, null, payload.TravelReason,
                candidate.ExpectedTravelCost, payload.ExpectedNetValue);
            if (!projected.TryUseSelectedPayload(payload, npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool TryProjectStealAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            DailyV1SelectedRuntimeOwnerGraph graph = npcRuntime.SelectedRuntimeOwnerGraph;
            if (!IsExactActor(actor)
                || payload == null
                || string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || decisionSystem.GetProviderForAction(npcRuntime, actionAsset)
                    is not CrimeSystem crimeSystem
                || !crimeSystem.AllowAutonomousAction(actionAsset)
                || !graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                    out DailyV1NpcRuntimeOwners targetOwner)
                || !graph.ContainsExactNpcOwner(targetOwner)
                || !ReferenceEquals(targetOwner.Definitions, graph.Definitions)
                || !string.Equals(targetOwner.RuntimeId, payload.TargetNpcRuntimeId,
                    StringComparison.Ordinal))
                return false;

            CityRuntime currentCity = npcRuntime.CurrentCity;
            DailyV1CityRuntimeOwners actorCityOwner = graph.Cities.FirstOrDefault(value =>
                value != null && string.Equals(value.RuntimeId,
                    actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal));
            if (currentCity == null || actorCityOwner == null
                || !ReferenceEquals(currentCity.SelectedRuntimeOwners, actorCityOwner))
                return false;
            NpcRuntime targetNpc = currentCity?.ImportantNpcs?.FirstOrDefault(value =>
                value != null && string.Equals(value.RuntimeId,
                    payload.TargetNpcRuntimeId, StringComparison.Ordinal));
            if (targetNpc == null
                || !ReferenceEquals(targetNpc.SelectedRuntimeOwners, targetOwner)
                || !ReferenceEquals(targetNpc.CurrentCity, currentCity))
                return false;

            NpcActionRuntime projected = new NpcActionRuntime(
                actionAsset, targetNpc, null, payload.Amount, 0f);
            if (!projected.TryUseSelectedPayload(payload,
                    npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool TryProjectFleeCityAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            DailyV1SelectedRuntimeOwnerGraph graph = npcRuntime.SelectedRuntimeOwnerGraph;
            if (!IsExactActor(actor)
                || payload == null
                || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || decisionSystem.GetProviderForAction(npcRuntime, actionAsset)
                    is not CrimeSystem crimeSystem
                || !crimeSystem.AllowAutonomousAction(actionAsset)
                || !crimeSystem.TryProjectSelectedCityRuntime(
                    graph, payload.TargetCityRuntimeId, out CityRuntime destinationCity))
                return false;

            NpcActionRuntime projected = new NpcActionRuntime(
                actionAsset, destinationCity, null, 0, payload.ExpectedUnitPrice);
            if (!projected.TryUseSelectedPayload(payload,
                    npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool TryProjectSelectedGuardArrestAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            DailyV1SelectedRuntimeOwnerGraph graph = npcRuntime.SelectedRuntimeOwnerGraph;
            if (!IsExactActor(actor)
                || payload == null
                || string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
                || payload.TargetCityRuntimeId != null
                || payload.TargetItem != null
                || payload.Amount != 0
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || !graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                    out DailyV1NpcRuntimeOwners targetOwner)
                || !graph.ContainsExactNpcOwner(targetOwner)
                || !ReferenceEquals(targetOwner.Definitions, graph.Definitions)
                || !string.Equals(targetOwner.RuntimeId, payload.TargetNpcRuntimeId,
                    StringComparison.Ordinal))
                return false;

            CityRuntime currentCity = npcRuntime.CurrentCity;
            DailyV1CityRuntimeOwners actorCityOwner = graph.Cities.FirstOrDefault(value =>
                value != null && string.Equals(value.RuntimeId,
                    actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal));
            if (currentCity == null
                || actorCityOwner == null
                || !ReferenceEquals(currentCity.SelectedRuntimeOwners, actorCityOwner)
                || !string.Equals(currentCity.RuntimeId, actorCityOwner.RuntimeId,
                    StringComparison.Ordinal))
                return false;

            NpcRuntime targetRuntime = currentCity.ImportantNpcs?.FirstOrDefault(value =>
                value != null
                && string.Equals(value.RuntimeId, payload.TargetNpcRuntimeId,
                    StringComparison.Ordinal)
                && ReferenceEquals(value.SelectedRuntimeOwners, targetOwner)
                && ReferenceEquals(value.CurrentCity, currentCity));
            if (targetRuntime == null)
                return false;

            NpcActionRuntime projected = new NpcActionRuntime(actionAsset, targetRuntime);
            if (!projected.TryUseSelectedPayload(payload,
                    npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool HasMerchantActionProvider(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action) =>
            TryGetActionAsset(actor, action, out NpcActionData asset)
            && decisionSystem.GetProviderForAction(npcRuntime, asset) is MerchantSystem;

        public bool HasEnabledCrimeActionProvider(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action)
        {
            if (!IsExactActor(actor) || action == null) return false;

            int? actionType = action.Values.actionType?.Value;
            if (actionType == DailyV1NpcDefinitionRules.HideActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)
                return DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                    npcRuntime.SelectedDailyV1DefinitionGraph, action);

            return TryGetActionAsset(actor, action, out NpcActionData asset)
                && decisionSystem.GetProviderForAction(npcRuntime, asset) is CrimeSystem crimeSystem
                && crimeSystem.AllowAutonomousAction(asset);
        }

        public bool TryProjectBuyAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            if (!IsExactActor(actor)
                || payload?.TargetItem == null
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || decisionSystem.GetProviderForAction(npcRuntime, actionAsset)
                    is not MerchantSystem merchantSystem
                || !merchantSystem.TryProjectSelectedCityRuntime(
                    npcRuntime.SelectedRuntimeOwnerGraph,
                    payload.TargetCityRuntimeId,
                    out CityRuntime targetCity)
                || !npcRuntime.TryGetSelectedDailyV1ItemAsset(
                    payload.TargetItem, out ItemData targetItem))
                return false;

            NpcActionRuntime projected = new NpcActionRuntime(
                actionAsset, targetCity, targetItem, payload.Amount,
                payload.ExpectedUnitPrice);
            if (!projected.TryUseSelectedPayload(payload, npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool TryProjectSellAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate)
        {
            DailyV1NpcActionPayloadOwner payload = candidate?.PreparedPayload;
            if (!IsExactActor(actor)
                || payload?.TargetItem == null
                || !TryGetActionAsset(actor, candidate.Action, out NpcActionData actionAsset)
                || decisionSystem.GetProviderForAction(npcRuntime, actionAsset)
                    is not MerchantSystem
                || !npcRuntime.TryGetSelectedDailyV1ItemAsset(
                    payload.TargetItem, out ItemData targetItem))
                return false;

            NpcRuntime targetNpc = null;
            if (!string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId))
                targetNpc = npcRuntime.CurrentCity?.ImportantNpcs?.FirstOrDefault(candidateNpc =>
                    candidateNpc != null && string.Equals(candidateNpc.RuntimeId,
                        payload.TargetNpcRuntimeId, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId) && targetNpc == null)
                return false;

            NpcActionRuntime projected = targetNpc != null
                ? new NpcActionRuntime(actionAsset, targetNpc, targetItem,
                    payload.Amount, payload.ExpectedUnitPrice)
                : new NpcActionRuntime(actionAsset, npcRuntime.CurrentCity, targetItem,
                    payload.Amount, payload.ExpectedUnitPrice);
            if (!projected.TryUseSelectedPayload(payload, npcRuntime.SelectedDailyV1DefinitionGraph))
                return false;
            projectedActions[candidate] = projected;
            return true;
        }

        public bool TryPrepareAutonomousProviderAction(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate,
            out float utility)
        {
            utility = 0f;
            DailyV1SelectedRuntimeOwnerGraph graph = npcRuntime.SelectedRuntimeOwnerGraph;
            bool admittedWithoutGraph = graph == null
                && npcRuntime.SelectedDailyV1DefinitionGraph != null
                && (actor == null
                    ? npcRuntime.SelectedRuntimeOwners == null
                    : ReferenceEquals(npcRuntime.SelectedRuntimeOwners, actor)
                        && ReferenceEquals(actor.Definitions,
                            npcRuntime.SelectedDailyV1DefinitionGraph)
                        && ReferenceEquals(actor.Definition, npcRuntime.SelectedDailyV1Definition));
            if (graph == null ? !admittedWithoutGraph : !IsExactActor(actor))
                return false;
            if (!TryGetActionAsset(actor, candidate?.Action, out NpcActionData actionAsset))
                return false;

            utility = candidate.Utility;
            NpcActionRuntime projected = decisionSystem.CreateRuntimeAction(
                npcRuntime, actionAsset, ref utility, true);
            if (projected == null) return false;
            projectedActions[candidate] = projected;
            return true;
        }

        internal bool TryGetProjectedAction(
            DailyV1NpcActionCandidate candidate,
            out NpcActionRuntime projected) =>
            projectedActions.TryGetValue(candidate, out projected);

        private bool TryGetActionAsset(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out NpcActionData asset)
        {
            asset = null;
            if (actor == null)
                return npcRuntime.SelectedRuntimeOwnerGraph == null
                    && npcRuntime.SelectedRuntimeOwners == null
                    && npcRuntime.SelectedDailyV1DefinitionGraph != null
                    && npcRuntime.TryGetSelectedDailyV1ActionAsset(action, out asset);
            if (npcRuntime.SelectedRuntimeOwnerGraph == null)
                return ReferenceEquals(npcRuntime.SelectedRuntimeOwners, actor)
                    && ReferenceEquals(actor.Definitions,
                        npcRuntime.SelectedDailyV1DefinitionGraph)
                    && ReferenceEquals(actor.Definition, npcRuntime.SelectedDailyV1Definition)
                    && npcRuntime.TryGetSelectedDailyV1ActionAsset(action, out asset);
            return IsExactActor(actor)
                && npcRuntime.TryGetSelectedDailyV1ActionAsset(action, out asset);
        }

        private bool IsExactActor(DailyV1NpcRuntimeOwners actor) =>
            actor != null
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners, actor)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph?.Definitions,
                npcRuntime.SelectedDailyV1DefinitionGraph)
            && ReferenceEquals(actor.Definitions, npcRuntime.SelectedDailyV1DefinitionGraph)
            && npcRuntime.SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(actor);
    }

    public NpcActionRuntime CreateRequestedAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        return CreateRequestedAction(npcRuntime, action, 0L);
    }

    internal NpcActionRuntime CreateRequestedAction(
        NpcRuntime npcRuntime,
        NpcActionData action,
        long currentAbsoluteDay)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return null;
        }

        if (npcRuntime == null || npcRuntime.IsAlive == false || action == null
            || npcRuntime.HasConfiguredRequiredActionStatuses(action, npcRuntime.CurrentStatus) == false)
        {
            return null;
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions
            && npcRuntime.GetConfiguredActionType(action) == NpcActionType.SellGoods)
        {
            if (!npcRuntime.TryGetSelectedDailyV1ActionNode(action,
                    out DailyV1ActionDefinition actionNode)
                || !DailyV1MerchantActionPlanning.TryPlanSelectedLocalSale(
                    npcRuntime.SelectedRuntimeOwnerGraph,
                    npcRuntime.SelectedRuntimeOwners,
                    actionNode,
                    currentAbsoluteDay,
                    0f,
                    out DailyV1MerchantLocalSalePlan plan)
                || plan?.SelectedPayload == null
                || !npcRuntime.TryGetSelectedDailyV1ItemAsset(plan.Item, out ItemData selectedItemAsset))
                return null;

            NpcActionRuntime selectedAction = new NpcActionRuntime(
                action, npcRuntime.CurrentCity, selectedItemAsset, plan.Amount, plan.LocalPrice);
            return selectedAction.TryUseSelectedPayload(
                plan.SelectedPayload, npcRuntime.SelectedDailyV1DefinitionGraph)
                ? selectedAction
                : null;
        }

        float ignoredUtility = 0f;
        return CreateRuntimeAction(npcRuntime, action, ref ignoredUtility, false);
    }

    /// <summary>Projects an already planned Core Local SellGoods payload into
    /// the existing Unity action wrapper without replanning or copying its
    /// authoritative payload.</summary>
    internal bool TryProjectSelectedLocalSale(
        NpcRuntime npcRuntime,
        DailyV1NpcActionPayloadOwner payload,
        out NpcActionRuntime actionRuntime)
    {
        actionRuntime = null;
        if (mutationGuardBinding.CanMutate == false
            || npcRuntime == null
            || npcRuntime.IsAlive == false
            || payload == null
            || payload.TargetItem == null
            || payload.Amount <= 0
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || npcRuntime.SelectedRuntimeOwners == null
            || !ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph?.Definitions,
                npcRuntime.SelectedDailyV1DefinitionGraph)
            || !ReferenceEquals(npcRuntime.SelectedRuntimeOwners.Definitions,
                npcRuntime.SelectedDailyV1DefinitionGraph)
            || payload.Action == null
            || !npcRuntime.SelectedDailyV1DefinitionGraph.Actions.TryGetValue(
                payload.Action.DefinitionId, out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, payload.Action)
            || !npcRuntime.TryGetSelectedDailyV1ActionAsset(payload.Action,
                out NpcActionData actionAsset)
            || npcRuntime.GetConfiguredActionType(actionAsset) != NpcActionType.SellGoods
            || npcRuntime.CurrentCity == null
            || !string.Equals(payload.TargetCityRuntimeId,
                npcRuntime.CurrentCity.RuntimeId, StringComparison.Ordinal)
            || !npcRuntime.TryGetSelectedDailyV1ItemAsset(payload.TargetItem,
                out ItemData itemAsset))
            return false;

        NpcActionRuntime projected = new NpcActionRuntime(
            actionAsset, npcRuntime.CurrentCity, itemAsset,
            payload.Amount, payload.ExpectedUnitPrice);
        if (!projected.TryUseSelectedPayload(
                payload, npcRuntime.SelectedDailyV1DefinitionGraph))
            return false;

        actionRuntime = projected;
        return true;
    }

    private Dictionary<NpcActionRuntime, float> CalculateActionUtilities(
        NpcRuntime npcRuntime,
        List<NpcActionData> validActions,
        bool autonomous)
    {
        Dictionary<NpcActionRuntime, float> utilities = new Dictionary<NpcActionRuntime, float>();

        foreach (NpcActionData action in validActions)
        {
            float utility = CalculateBaseActionUtility(npcRuntime, action);
            NpcActionRuntime actionRuntime = CreateRuntimeAction(npcRuntime, action, ref utility, autonomous);

            if (actionRuntime == null)
            {
                continue;
            }

            utilities.Add(actionRuntime, UnityMathfCompatibility.Max(0f, utility));
        }

        return utilities;
    }

    private float CalculateBaseActionUtility(NpcRuntime npcRuntime, NpcActionData action)
    {
        float utility = npcRuntime.GetConfiguredActionBaseUtility(action);

        if (npcRuntime.TryGetConfiguredJobActionUtility(action, out float jobUtility) == true)
        {
            utility = jobUtility;
        }

        if (npcRuntime.TryGetConfiguredDefaultActionUtility(action, out float npcUtility) == true)
        {
            utility = npcUtility;
        }

        return npcRuntime.ApplyConfiguredActionStatusModifiers(action, utility);
    }

    private NpcActionRuntime CreateRuntimeAction(
        NpcRuntime npcRuntime,
        NpcActionData action,
        ref float utility,
        bool autonomous)
    {
        NpcActionType configuredType = npcRuntime.GetConfiguredActionType(action);
        if (configuredType == NpcActionType.Normal)
        {
            return new NpcActionRuntime(action);
        }

        INpcActionProvider actionProvider = GetProviderForAction(npcRuntime, action);

        if (actionProvider == null)
        {
            utility = 0f;
            return null;
        }

        if (autonomous
            && actionProvider is IAutonomousNpcActionPolicy autonomousPolicy
            && autonomousPolicy.AllowAutonomousAction(action) == false)
        {
            utility = 0f;
            return null;
        }

        return actionProvider.CreateAction(npcRuntime, action, ref utility);
    }

    public INpcActionProvider GetProviderForAction(NpcActionData action)
    {
        return GetProviderForAction(null, action);
    }

    internal INpcActionProvider GetProviderForAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (mutationGuardBinding.CanMutate == false || action == null)
        {
            return null;
        }

        foreach (INpcActionProvider actionProvider in actionProviders)
        {
            bool handles = npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions
                ? actionProvider is ISelectedDefinitionNpcActionProvider selectedProvider
                    ? selectedProvider.HandlesAction(npcRuntime, action)
                    : npcRuntime.GetConfiguredActionType(action) == action.actionType
                        && actionProvider.HandlesAction(action)
                : actionProvider.HandlesAction(action);
            if (handles)
            {
                return actionProvider;
            }
        }

        return null;
    }

    public bool HasProvider<TProvider>() where TProvider : class, INpcActionProvider
    {
        foreach (INpcActionProvider actionProvider in actionProviders)
        {
            if (actionProvider is TProvider)
            {
                return true;
            }
        }

        return false;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (mutationGuardBinding.CanBindTo(guard) == false)
        {
            return false;
        }

        foreach (INpcActionProvider provider in actionProviders)
        {
            if (provider is IAuthoritativeMutationGuardBindable bindable
                && bindable.CanBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        return true;
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (CanBindMutationGuard(guard) == false || mutationGuardBinding.TryBindTo(guard) == false)
        {
            return false;
        }

        foreach (INpcActionProvider provider in actionProviders)
        {
            if (provider is IAuthoritativeMutationGuardBindable bindable
                && bindable.TryBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        return true;
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private List<NpcActionData> GetAllValidActions(NpcRuntime npcRuntime, List<NpcActionData> availableActions)
    {
        List<NpcActionData> validActions = new List<NpcActionData>();

        if (availableActions == null)
        {
            return validActions;
        }

        foreach (NpcActionData action in availableActions)
        {
            if (action == null || npcRuntime.HasConfiguredRequiredActionStatuses(action, npcRuntime.CurrentStatus) == false)
            {
                continue;
            }

            validActions.Add(action);
        }

        validActions.Sort((left, right) => CompareActions(npcRuntime, left, right));
        return validActions;
    }

    private NpcActionRuntime ChooseWeightedAction(
        NpcRuntime npcRuntime,
        Dictionary<NpcActionRuntime, float> utilities,
        string randomStreamKey)
    {
        List<KeyValuePair<NpcActionRuntime, float>> orderedUtilities = new List<KeyValuePair<NpcActionRuntime, float>>(utilities);
        orderedUtilities.Sort((left, right) => CompareUtilities(npcRuntime, left, right));
        float totalWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in orderedUtilities)
        {
            if (pair.Value > 0f)
            {
                totalWeight += pair.Value;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = randomSource.NextUnit(randomStreamKey) * totalWeight;
        float currentWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in orderedUtilities)
        {
            if (pair.Value <= 0f)
            {
                continue;
            }

            currentWeight += pair.Value;

            if (randomValue <= currentWeight)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static int CompareActions(NpcRuntime npcRuntime, NpcActionData left, NpcActionData right)
    {
        int idComparison = string.CompareOrdinal(left?.DefinitionId ?? string.Empty, right?.DefinitionId ?? string.Empty);
        if (idComparison != 0)
        {
            return idComparison;
        }

        int nameComparison = string.CompareOrdinal(
            npcRuntime?.GetConfiguredActionName(left) ?? left?.actionName ?? string.Empty,
            npcRuntime?.GetConfiguredActionName(right) ?? right?.actionName ?? string.Empty);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return (npcRuntime?.GetConfiguredActionTypeOrder(left) ?? (int)(left?.actionType ?? NpcActionType.Normal))
            .CompareTo(npcRuntime?.GetConfiguredActionTypeOrder(right) ?? (int)(right?.actionType ?? NpcActionType.Normal));
    }

    private static int CompareUtilities(
        NpcRuntime npcRuntime,
        KeyValuePair<NpcActionRuntime, float> left,
        KeyValuePair<NpcActionRuntime, float> right)
    {
        return CompareActions(npcRuntime, left.Key?.Action, right.Key?.Action);
    }
}
