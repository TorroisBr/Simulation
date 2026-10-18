using System.Collections.Generic;
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
        long currentAbsoluteDay)
    {
        if (mutationGuardBinding.CanMutate == false || npcRuntime == null || npcRuntime.IsAlive == false)
            return null;

        List<DailyV1StatusDefinition> activeStatuses = new List<DailyV1StatusDefinition>();
        if (!npcRuntime.TryGetSelectedDailyV1ActiveStatusNodes(activeStatuses)
            || !DailyV1NpcActionDecision.TryCreateCandidates(
                npcRuntime.SelectedDailyV1Definition,
                npcRuntime.SelectedDailyV1DefinitionGraph,
                availableActions,
                activeStatuses,
                out List<DailyV1NpcActionCandidate> candidates))
            return null;

        Dictionary<DailyV1NpcActionCandidate, NpcActionRuntime> providerActions =
            new Dictionary<DailyV1NpcActionCandidate, NpcActionRuntime>();
        foreach (DailyV1NpcActionCandidate candidate in candidates)
        {
            if ((NpcActionType)(candidate.Action.Values.actionType?.Value ?? int.MinValue) == NpcActionType.Normal)
                continue;

            if (!npcRuntime.TryGetSelectedDailyV1ActionAsset(candidate.Action, out NpcActionData actionAsset))
            {
                candidate.Utility = 0f;
                continue;
            }

            float utility = candidate.Utility;
            NpcActionRuntime providerAction = CreateRuntimeAction(npcRuntime, actionAsset, ref utility, true);
            if (providerAction == null)
            {
                candidate.Utility = 0f;
                continue;
            }

            candidate.Utility = UnityMathfCompatibility.Max(0f, utility);
            providerActions.Add(candidate, providerAction);
        }

        string randomStreamKey = "npc-decision|" + npcRuntime.RuntimeId + "|" + currentAbsoluteDay;
        DailyV1NpcActionCandidate selected = DailyV1NpcActionDecision.ChooseWeightedCandidate(
            candidates, randomSource, randomStreamKey);
        if (selected == null)
            return null;

        if (providerActions.TryGetValue(selected, out NpcActionRuntime selectedProviderAction))
            return selectedProviderAction;

        return npcRuntime.TryGetSelectedDailyV1ActionAsset(selected.Action, out NpcActionData selectedAsset)
            ? new NpcActionRuntime(selectedAsset)
            : null;
    }

    public NpcActionRuntime CreateRequestedAction(NpcRuntime npcRuntime, NpcActionData action)
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

        float ignoredUtility = 0f;
        return CreateRuntimeAction(npcRuntime, action, ref ignoredUtility, false);
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
