using System;
using UnityEngine;

[Serializable]
public enum ExplorableSiteKind
{
    Generic,
    Ruin,
    Cave,
    Dungeon,
    Landmark,
    Wilderness
}

[CreateAssetMenu(menuName = "World Simulation/Explorable Site")]
public class ExplorableSiteData : ScriptableObject
{
    public string id;
    public string siteName;
    public ExplorableSiteKind kind = ExplorableSiteKind.Generic;

    public string DefinitionId => id;
    public string DisplayName => siteName;
}
