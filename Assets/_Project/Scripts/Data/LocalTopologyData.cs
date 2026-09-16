using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/Local Place Type")]
public sealed class LocalPlaceTypeData : ScriptableObject
{
    public string id;
    public string displayName;

    public string DefinitionId => id;
    public string DisplayName => displayName;
}

[CreateAssetMenu(menuName = "World Simulation/Local Connection Type")]
public sealed class LocalConnectionTypeData : ScriptableObject
{
    public string id;
    public string displayName;

    public string DefinitionId => id;
    public string DisplayName => displayName;
}
