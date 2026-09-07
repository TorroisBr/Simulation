using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/Job")]
public class NpcJobData : ScriptableObject
{
    public string jobName;

    public NpcActionData workAction;

    public float workUtility = 50;
}