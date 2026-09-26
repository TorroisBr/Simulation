using System;
using UnityEngine;

[Serializable]

[CreateAssetMenu(menuName = "World Simulation/Status")]
public class NpcStatusData : ScriptableObject
{
	public string id;
	public string statusName;
	public string DefinitionId => id;
}
