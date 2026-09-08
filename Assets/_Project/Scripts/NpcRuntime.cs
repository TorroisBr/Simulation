using System.Collections.Generic;

public class NpcRuntime
{
	private NpcData npcData;
	private List<NpcStatusData> currentStatus = new List<NpcStatusData>();
	private NpcActionData currentAction;

    public NpcData NpcData => npcData;
    public List<NpcStatusData> CurrentStatus => currentStatus;
    public NpcActionData CurrentAction => currentAction;
    
	public NpcRuntime(NpcData npcData)
	{
		this.npcData = npcData;
		currentStatus.AddRange(npcData.statusPadrao);
	}
}