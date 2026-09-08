using System;
using System.Collections.Generic;
using UnityEngine;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private int daysToSimulate;
    
    [SerializeField] private List<NpcData> npcList = new List<NpcData>();
    
    [SerializeField] private List<NpcStatusData> npcStatusList = new List<NpcStatusData>();
    [SerializeField] private List<NpcActionData> npcActionList = new List<NpcActionData>();

    
    private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    public void Start()
    {
        Simulate();
    }

    private void Simulate()
    {
        npcRuntimeList.Add( new NpcRuntime(npcList[0])); //Testando um usario por ser mais facil
        
        for (int i = 0; i < daysToSimulate; i++)
        {
            foreach (NpcRuntime npcRuntime in npcRuntimeList)
            {
                EvaluateStatus(npcRuntime);
                EvaluateAction(npcRuntime);
            }
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
        
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        List<NpcActionData> validActions = GetAllValidActions(npcRuntime.CurrentStatus);
    }
    
    private List<NpcActionData> GetAllValidActions(List<NpcStatusData> npcCurrentStatus)
    {
        List<NpcActionData> npcActionDataList = new List<NpcActionData>();
        foreach (NpcActionData npcActionData in npcActionList)
        {
            foreach (NpcStatusData npcStatusData in npcCurrentStatus)
            {
                if (npcActionData.statusNecessariosParaFazerAcao.Contains(npcStatusData) == false)
                {
                    continue;
                }
                
                npcActionDataList.Add(npcActionData);
                break;
            }
        }
        
        return npcActionDataList;
    }
}