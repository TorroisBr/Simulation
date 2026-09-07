using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/NPC")]
public class NPC : ScriptableObject
{
    public string id;
    public string name;
    
    public List<NPCActionSO> acoesPadrao = new List<NPCActionSO>();
    public List<NPCStatusSO> statusPadrao = new List<NPCStatusSO>();
}
