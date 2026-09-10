using System.Collections.Generic;
using UnityEngine;

public class NpcDecisionSystem
{
    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();

    public NpcDecisionSystem(List<INpcActionProvider> actionProviders)
    {
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
        if (npcRuntime == null)
        {
            return null;
        }

        List<NpcActionData> validActions = GetAllValidActions(npcRuntime.CurrentStatus, availableActions);
        Dictionary<NpcActionRuntime, float> utilities = CalculateActionUtilities(npcRuntime, validActions);
        return ChooseWeightedAction(utilities);
    }

    public NpcActionRuntime CreateRequestedAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (npcRuntime == null || action == null || HasAllRequiredStatus(action, npcRuntime.CurrentStatus) == false)
        {
            return null;
        }

        float ignoredUtility = 0f;
        return CreateRuntimeAction(npcRuntime, action, ref ignoredUtility);
    }

    private Dictionary<NpcActionRuntime, float> CalculateActionUtilities(NpcRuntime npcRuntime, List<NpcActionData> validActions)
    {
        Dictionary<NpcActionRuntime, float> utilities = new Dictionary<NpcActionRuntime, float>();

        foreach (NpcActionData action in validActions)
        {
            float utility = CalculateBaseActionUtility(npcRuntime, action);
            NpcActionRuntime actionRuntime = CreateRuntimeAction(npcRuntime, action, ref utility);

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

    private NpcActionRuntime CreateRuntimeAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
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

        return actionProvider.CreateAction(npcRuntime, action, ref utility);
    }

    public INpcActionProvider GetProviderForAction(NpcActionData action)
    {
        if (action == null)
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

    private NpcActionRuntime ChooseWeightedAction(Dictionary<NpcActionRuntime, float> utilities)
    {
        float totalWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in utilities)
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

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (KeyValuePair<NpcActionRuntime, float> pair in utilities)
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
}
