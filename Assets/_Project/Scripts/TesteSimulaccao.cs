using System.Collections.Generic;
using UnityEngine;

public class TesteSimulaccao : MonoBehaviour
{
    [SerializeField] private List<NPC> npc = new List<NPC>();
    
    [SerializeField] private List<NPCStatusSO> npcStatus = new List<NPCStatusSO>();
    [SerializeField] private List<NPCActionSO> npcAction = new List<NPCActionSO>();
}
