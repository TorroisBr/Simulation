using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string itemName;
    public float basePrice = 1f;

    public string DefinitionId => id;
}
