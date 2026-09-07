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
        npcRuntimeList.Add( new NpcRuntime(npcList[0]));
        
        for (int i = 0; i < daysToSimulate; i++)
        {
            
        }
    }
}

public class NpcRuntime
{
    NpcData npcDataData;

    List<NpcStatusData> currentStatus;
    NpcActionData currentAction;

    public NpcRuntime(NpcData npcDataData)
    {
        this.npcDataData = npcDataData;
    }
}