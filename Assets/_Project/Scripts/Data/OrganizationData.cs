using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Organization")]
public class OrganizationData : ScriptableObject
{
    public string id;
    public string displayName;

    public string DefinitionId => id;
    public string DisplayName => displayName;
}
