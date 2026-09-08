using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcRuntime
{
	[SerializeField]private NpcData npcData;
	[SerializeField]private List<NpcStatusData> currentStatus = new List<NpcStatusData>();
	[SerializeField]private NpcActionData currentAction;

    public NpcData NpcData => npcData;
    public List<NpcStatusData> CurrentStatus => currentStatus;
    public NpcActionData CurrentAction => currentAction;
    
	public NpcRuntime(NpcData npcData)
	{
		this.npcData = npcData;
		currentStatus.AddRange(npcData.statusPadrao);
	}
    
    public void SetCurrentAction(NpcActionData action)
    {
        currentAction = action;
    }
}