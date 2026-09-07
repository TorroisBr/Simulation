using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/NpcData")]
public class NpcData : ScriptableObject
{
    public string id;
    public string name;

    public NpcJobData job;

    public List<NPCDefaultAction> acoesPadrao;

    public List<NpcStatusData> statusPadrao;
}
[System.Serializable]
public class NPCDefaultAction
{
    public NpcActionData action;

    public float baseUtility;
}