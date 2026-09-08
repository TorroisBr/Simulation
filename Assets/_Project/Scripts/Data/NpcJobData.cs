using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Job")]
public class NpcJobData : ScriptableObject
{
    public string jobName;
    public NpcJobType jobType = NpcJobType.None;
    public NpcActionData workAction;
    public float workUtility = 50;
}

public enum NpcJobType
{
    None,
    Merchant,
    Guard
}
