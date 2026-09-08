using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]

[CreateAssetMenu(menuName = "World Simulation/Action")]
public class NpcActionData : ScriptableObject
{
	public List<NpcStatusData> statusNecessariosParaFazerAcao = new List<NpcStatusData>();
	public string actionName;

	[Range(0,REFERENCIASPARAREMOVERNOFUTURO.PESO_VALOR_MAX)]
	public float baseUtility = 1f;

	public List<StatusWeightModifier> statusModifiers;
    
    public List<NpcStatusData> statusToAdd = new List<NpcStatusData>();
    public List<NpcStatusData> statusToRemove = new List<NpcStatusData>();
}

[System.Serializable]
public class StatusWeightModifier
{
	public NpcStatusData status;

	// Quanto esse status influencia essa ação
	public float multiplier = 1f;
}

public static class REFERENCIASPARAREMOVERNOFUTURO
{
	public const int PESO_VALOR_MAX = 100;
}