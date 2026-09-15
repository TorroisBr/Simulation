using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string itemName;
    public float basePrice = 1f;
    public List<CapabilityAttributeModifier> capabilityModifiers = new List<CapabilityAttributeModifier>();

    public string DefinitionId => id;

    public List<CapabilityAttributeModifier> CapabilityModifiers => capabilityModifiers ?? (capabilityModifiers = new List<CapabilityAttributeModifier>());

    public bool TryValidateCapabilityAuthoring(out string diagnostic)
    {
        return CapabilityAuthoringValidator.ValidateItem(this, out diagnostic);
    }
}
