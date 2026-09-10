using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]

[CreateAssetMenu(menuName = "World Simulation/NpcData")]
public class NpcData : ScriptableObject
{
    public string id;
    public string name;

    public string DefinitionId => id;

    public NpcJobData job;

    public List<NPCDefaultAction> acoesPadrao = new List<NPCDefaultAction>();

    public List<NpcStatusData> statusPadrao = new List<NpcStatusData>();
}
[System.Serializable]
public class NPCDefaultAction
{
    public NpcActionData action;

    public float baseUtility;
}
