using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public float basePrice = 1f;
}
