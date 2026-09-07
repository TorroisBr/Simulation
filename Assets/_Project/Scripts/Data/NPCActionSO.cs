using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "World Simulation/Action")]
public class NPCActionSO : ScriptableObject
{
	public List<NPCStatusSO> statusNecessariosParaFazerAcao = new List<NPCStatusSO>();
	public string actionName;

	[Range(0,REFERENCIASPARAREMOVERNOFUTURO.PESO_VALOR_MAX)]
	public float baseWeight = 1f;

	public List<StatusWeightModifier> statusModifiers;
}

[System.Serializable]
public class StatusWeightModifier
{
	public NPCStatusSO status;

	// Quanto esse status influencia essa ação
	public float multiplier = 1f;
}

public static class REFERENCIASPARAREMOVERNOFUTURO
{
	public const int PESO_VALOR_MAX = 100;
}