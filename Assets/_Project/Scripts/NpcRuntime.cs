using System.Collections.Generic;

public class NpcRuntime
{
	NpcData npcDataData;

	List<NpcStatusData> currentStatus;
	NpcActionData currentAction;

	public NpcRuntime(NpcData npcDataData)
	{
		this.npcDataData = npcDataData;
		currentStatus.AddRange(npcDataData.statusPadrao);
	}
}