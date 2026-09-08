using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private int daysToSimulate;
    
    [SerializeField] private List<NpcData> npcList = new List<NpcData>();
    [SerializeField] private List<NpcStatusData> npcStatusList = new List<NpcStatusData>();
    [SerializeField] private List<NpcActionData> npcActionList = new List<NpcActionData>();
    
    [SerializeField]private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
     
    public void Start()
    {
        npcRuntimeList.Add( new NpcRuntime(npcList[0])); //Testando um usario por ser mais facil
    }

    public void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            Simulate(1);
        }
    }

    private void Simulate(int daysToSimulate)
    {
        for (int i = 0; i < daysToSimulate; i++)
        {
            foreach (NpcRuntime npcRuntime in npcRuntimeList)
            {
                EvaluateStatus(npcRuntime);
                EvaluateAction(npcRuntime);
                ExecuteAction(npcRuntime);
            }
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
        
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        List<NpcActionData> validActions = GetAllValidActions(npcRuntime.CurrentStatus);
        Dictionary<NpcActionData, float> utilities = CalculateActionUtilities(npcRuntime, validActions);
        NpcActionData chosenAction = ChooseAction(utilities);
        npcRuntime.SetCurrentAction(chosenAction);
    }

    private void ExecuteAction(NpcRuntime npcRuntime)
    {
        NpcActionData action = npcRuntime.CurrentAction;

        if (action == null)
        {
            return;
        }

        foreach (NpcStatusData status in action.statusToRemove)
        {
            npcRuntime.RemoveStatus(status);
        }

        foreach (NpcStatusData status in action.statusToAdd)
        {
            npcRuntime.AddStatus(status);
        }
    }
    
    private Dictionary<NpcActionData, float> CalculateActionUtilities(NpcRuntime npcRuntime, List<NpcActionData> validActions)
    {
        Dictionary<NpcActionData, float> utilities = new Dictionary<NpcActionData, float>();

        foreach (NpcActionData action in validActions)
        {
            float utility = action.baseUtility;

            NPCDefaultAction defaultAction = npcRuntime.NpcData.acoesPadrao.Find(x => x.action == action);

            if (defaultAction != null)
            {
                utility = defaultAction.baseUtility;
            }
            else if (npcRuntime.NpcData.job != null && npcRuntime.NpcData.job.workAction == action)
            {
                utility = npcRuntime.NpcData.job.workUtility;
            }

            foreach (StatusWeightModifier modifier in action.statusModifiers)
            {
                if (npcRuntime.CurrentStatus.Contains(modifier.status))
                {
                    utility *= modifier.multiplier;
                }
            }

            utility = Mathf.Max(0f, utility);

            utilities.Add(action, utility);
        }

        return utilities;
    }

    private List<NpcActionData> GetAllValidActions(List<NpcStatusData> npcCurrentStatus)
    {
        List<NpcActionData> validActions = new List<NpcActionData>();

        foreach (NpcActionData action in npcActionList)
        {
            bool hasAllRequiredStatus = action.statusNecessariosParaFazerAcao.All(requiredStatus => npcCurrentStatus.Contains(requiredStatus));

            if (hasAllRequiredStatus == true)
            {
                validActions.Add(action);
            }
        }

        return validActions;
    }
    
    private NpcActionData ChooseAction(Dictionary<NpcActionData, float> utilities)
    {
        float totalWeight = 0f;

        foreach (KeyValuePair<NpcActionData, float> pair in utilities)
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

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (KeyValuePair<NpcActionData, float> pair in utilities)
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