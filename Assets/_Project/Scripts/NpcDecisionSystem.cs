using System.Collections.Generic;
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

        List<NpcActionData> validActions = GetAllValidActions(npcRuntime.CurrentStatus, availableActions);
        Dictionary<NpcActionRuntime, float> utilities = CalculateActionUtilities(
            npcRuntime,
            validActions,
            true);
        return ChooseWeightedAction(
            utilities,
            "npc-decision|" + npcRuntime.RuntimeId + "|" + currentAbsoluteDay);
    }

    public NpcActionRuntime CreateRequestedAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return null;
        }

        if (npcRuntime == null || npcRuntime.IsAlive == false || action == null || HasAllRequiredStatus(action, npcRuntime.CurrentStatus) == false)
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

            utilities.Add(actionRuntime, Mathf.Max(0f, utility));
        }

        return utilities;
    }

    private float CalculateBaseActionUtility(NpcRuntime npcRuntime, NpcActionData action)
    {
        float utility = action.baseUtility;

        if (TryGetJobUtility(npcRuntime.NpcData, action, out float jobUtility) == true)
        {
            utility = jobUtility;
        }

        if (TryGetNpcUtility(npcRuntime.NpcData, action, out float npcUtility) == true)
        {
            utility = npcUtility;
        }

        if (action.statusModifiers != null)
        {
            foreach (StatusWeightModifier modifier in action.statusModifiers)
            {
                if (modifier != null && modifier.status != null && npcRuntime.CurrentStatus.Contains(modifier.status) == true)
                {
                    utility *= modifier.multiplier;
                }
            }
        }

        return utility;
    }

    private bool TryGetJobUtility(NpcData npcData, NpcActionData action, out float utility)
    {
        utility = 0f;

        if (npcData == null || npcData.job == null || npcData.job.workAction != action)
        {
            return false;
        }

        utility = npcData.job.workUtility;
        return true;
    }

    private bool TryGetNpcUtility(NpcData npcData, NpcActionData action, out float utility)
    {
        utility = 0f;

        if (npcData == null || npcData.acoesPadrao == null)
        {
            return false;
        }

        NPCDefaultAction defaultAction = npcData.acoesPadrao.Find(x => x != null && x.action == action);

        if (defaultAction == null)
        {
            return false;
        }

        utility = defaultAction.baseUtility;
        return true;
    }

    private NpcActionRuntime CreateRuntimeAction(
        NpcRuntime npcRuntime,
        NpcActionData action,
        ref float utility,
        bool autonomous)
    {
        if (action.actionType == NpcActionType.Normal)
        {
            return new NpcActionRuntime(action);
        }

        INpcActionProvider actionProvider = GetProviderForAction(action);

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
        if (mutationGuardBinding.CanMutate == false || action == null)
        {
            return null;
        }

        foreach (INpcActionProvider actionProvider in actionProviders)
        {
            if (actionProvider.HandlesAction(action) == true)
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

    private List<NpcActionData> GetAllValidActions(List<NpcStatusData> npcCurrentStatus, List<NpcActionData> availableActions)
    {
        List<NpcActionData> validActions = new List<NpcActionData>();

        if (availableActions == null)
        {
            return validActions;
        }

        foreach (NpcActionData action in availableActions)
        {
            if (action == null || HasAllRequiredStatus(action, npcCurrentStatus) == false)
            {
                continue;
            }

            validActions.Add(action);
        }

        validActions.Sort(CompareActions);
        return validActions;
    }

    private bool HasAllRequiredStatus(NpcActionData action, List<NpcStatusData> npcCurrentStatus)
    {
        if (action.statusNecessariosParaFazerAcao == null)
        {
            return true;
        }

        foreach (NpcStatusData requiredStatus in action.statusNecessariosParaFazerAcao)
        {
            if (requiredStatus == null)
            {
                continue;
            }

            if (npcCurrentStatus == null || npcCurrentStatus.Contains(requiredStatus) == false)
            {
                return false;
            }
        }

        return true;
    }

    private NpcActionRuntime ChooseWeightedAction(
        Dictionary<NpcActionRuntime, float> utilities,
        string randomStreamKey)
    {
        List<KeyValuePair<NpcActionRuntime, float>> orderedUtilities = new List<KeyValuePair<NpcActionRuntime, float>>(utilities);
        orderedUtilities.Sort(CompareUtilities);
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

    private static int CompareActions(NpcActionData left, NpcActionData right)
    {
        int idComparison = string.CompareOrdinal(left?.DefinitionId ?? string.Empty, right?.DefinitionId ?? string.Empty);
        if (idComparison != 0)
        {
            return idComparison;
        }

        int nameComparison = string.CompareOrdinal(left?.actionName ?? string.Empty, right?.actionName ?? string.Empty);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return (left?.actionType ?? NpcActionType.Normal).CompareTo(right?.actionType ?? NpcActionType.Normal);
    }

    private static int CompareUtilities(
        KeyValuePair<NpcActionRuntime, float> left,
        KeyValuePair<NpcActionRuntime, float> right)
    {
        return CompareActions(left.Key?.Action, right.Key?.Action);
    }
}
