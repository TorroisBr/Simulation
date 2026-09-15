using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Capability Attribute")]
public sealed class CapabilityAttributeData : ScriptableObject
{
    public string id;
    public string displayName;

    public string DefinitionId => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) == true ? id : displayName;

    public bool TryValidate(out string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(id) == true)
        {
            diagnostic = "Capability attribute DefinitionId is empty.";
            return false;
        }

        diagnostic = null;
        return true;
    }
}

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Trait")]
public sealed class TraitData : ScriptableObject
{
    public string id;
    public string traitName;

    public string DefinitionId => id;
    public string DisplayName => string.IsNullOrWhiteSpace(traitName) == true ? id : traitName;

    public bool TryValidate(out string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(id) == true)
        {
            diagnostic = "Trait DefinitionId is empty.";
            return false;
        }

        diagnostic = null;
        return true;
    }
}

[Serializable]
public sealed class CapabilityAttributeValue
{
    public CapabilityAttributeData attribute;
    public float value;

    public CapabilityAttributeData Attribute => attribute;
    public float Value => value;

    public CapabilityAttributeValue()
    {
    }

    public CapabilityAttributeValue(CapabilityAttributeData attribute, float value)
    {
        this.attribute = attribute;
        this.value = value;
    }
}

[Serializable]
public sealed class CapabilityAttributeModifier
{
    public CapabilityAttributeData attribute;
    public float additiveValue;

    public CapabilityAttributeData Attribute => attribute;
    public float AdditiveValue => additiveValue;

    public CapabilityAttributeModifier()
    {
    }

    public CapabilityAttributeModifier(CapabilityAttributeData attribute, float additiveValue)
    {
        this.attribute = attribute;
        this.additiveValue = additiveValue;
    }
}

[Serializable]
public sealed class GenericTraitCapabilityModifier
{
    public TraitData trait;
    public CapabilityAttributeData attribute;
    public float additiveValue;

    public TraitData Trait => trait;
    public CapabilityAttributeData Attribute => attribute;
    public float AdditiveValue => additiveValue;

    public GenericTraitCapabilityModifier()
    {
    }

    public GenericTraitCapabilityModifier(
        TraitData trait,
        CapabilityAttributeData attribute,
        float additiveValue)
    {
        this.trait = trait;
        this.attribute = attribute;
        this.additiveValue = additiveValue;
    }
}

[Serializable]
public sealed class GenericCapabilityModelConfiguration
{
    public CapabilityAttributeData physicalAttribute;
    public CapabilityAttributeData mentalAttribute;
    public float physicalWeight = 1f;
    public float mentalWeight = 0.5f;
    public List<GenericTraitCapabilityModifier> traitModifiers = new List<GenericTraitCapabilityModifier>();

    public CapabilityAttributeData PhysicalAttribute => physicalAttribute;
    public CapabilityAttributeData MentalAttribute => mentalAttribute;
    public float PhysicalWeight => physicalWeight;
    public float MentalWeight => mentalWeight;
    public List<GenericTraitCapabilityModifier> TraitModifiers => traitModifiers ?? (traitModifiers = new List<GenericTraitCapabilityModifier>());
}

public enum CapabilityContributionSource
{
    BaseAttribute,
    Trait,
    Item,
    Condition,
    Context
}

[Serializable]
public sealed class CapabilityBreakdownEntry
{
    private readonly CapabilityContributionSource source;
    private readonly string sourceId;
    private readonly string attributeDefinitionId;
    private readonly float rawValue;
    private readonly float weight;
    private readonly float contribution;

    public CapabilityContributionSource Source => source;
    public string SourceId => sourceId;
    public string AttributeDefinitionId => attributeDefinitionId;
    public float RawValue => rawValue;
    public float Weight => weight;
    public float Contribution => contribution;

    public CapabilityBreakdownEntry(
        CapabilityContributionSource source,
        string sourceId,
        CapabilityAttributeData attribute,
        float rawValue,
        float weight,
        float contribution)
    {
        if (attribute == null || string.IsNullOrWhiteSpace(attribute.DefinitionId) == true)
        {
            throw new ArgumentException("Capability breakdown requires an attribute definition.", nameof(attribute));
        }

        if (float.IsNaN(rawValue) == true || float.IsInfinity(rawValue) == true
            || float.IsNaN(weight) == true || float.IsInfinity(weight) == true
            || float.IsNaN(contribution) == true || float.IsInfinity(contribution) == true)
        {
            throw new ArgumentException("Capability breakdown values must be finite.");
        }

        this.source = source;
        this.sourceId = string.IsNullOrWhiteSpace(sourceId) == true ? attribute.DefinitionId : sourceId;
        attributeDefinitionId = attribute.DefinitionId;
        this.rawValue = rawValue;
        this.weight = weight;
        this.contribution = contribution;
    }
}

[Serializable]
public sealed class CapabilityEvaluationResult
{
    private readonly float baseCapability;
    private readonly float effectiveCapability;
    private readonly IReadOnlyList<CapabilityBreakdownEntry> breakdown;

    public float BaseCapability => baseCapability;
    public float EffectiveCapability => effectiveCapability;
    public IReadOnlyList<CapabilityBreakdownEntry> Breakdown => breakdown;

    public CapabilityEvaluationResult(
        float baseCapability,
        float effectiveCapability,
        IReadOnlyList<CapabilityBreakdownEntry> breakdown)
    {
        if (float.IsNaN(baseCapability) == true || float.IsInfinity(baseCapability) == true
            || float.IsNaN(effectiveCapability) == true || float.IsInfinity(effectiveCapability) == true)
        {
            throw new ArgumentException("Capability results must be finite.");
        }

        this.baseCapability = Mathf.Max(0f, baseCapability);
        this.effectiveCapability = Mathf.Max(0f, effectiveCapability);
        this.breakdown = breakdown != null
            ? new List<CapabilityBreakdownEntry>(breakdown).AsReadOnly()
            : Array.Empty<CapabilityBreakdownEntry>();
    }
}

[Serializable]
public sealed class CapabilityContextModifier
{
    private readonly CapabilityAttributeData attribute;
    private readonly string modifierId;
    private readonly float additiveValue;
    private readonly float multiplier;

    public CapabilityAttributeData Attribute => attribute;
    public string ModifierId => modifierId;
    public float AdditiveValue => additiveValue;
    public float Multiplier => multiplier;

    public CapabilityContextModifier(
        string modifierId,
        CapabilityAttributeData attribute,
        float additiveValue = 0f,
        float multiplier = 1f)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        if (float.IsNaN(additiveValue) == true || float.IsInfinity(additiveValue) == true
            || float.IsNaN(multiplier) == true || float.IsInfinity(multiplier) == true
            || multiplier < 0f)
        {
            throw new ArgumentException("Capability context modifier values must be finite and non-negative where applicable.");
        }

        this.modifierId = string.IsNullOrWhiteSpace(modifierId) == true ? attribute.DefinitionId : modifierId;
        this.attribute = attribute;
        this.additiveValue = additiveValue;
        this.multiplier = multiplier;
    }
}

public interface ICapabilityConditionSource
{
    string ConditionSourceId { get; }
    float GetCapabilityMultiplier();
}

public sealed class CapabilityEvaluationContext
{
    private readonly List<CapabilityContextModifier> modifiers = new List<CapabilityContextModifier>();

    public IReadOnlyList<CapabilityContextModifier> Modifiers => modifiers.AsReadOnly();

    public CapabilityEvaluationContext()
    {
    }

    public CapabilityEvaluationContext(IEnumerable<CapabilityContextModifier> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }

        foreach (CapabilityContextModifier modifier in modifiers)
        {
            if (modifier != null)
            {
                this.modifiers.Add(modifier);
            }
        }
    }

    public void AddModifier(CapabilityContextModifier modifier)
    {
        if (modifier != null)
        {
            modifiers.Add(modifier);
        }
    }
}

public interface ICapabilityModel
{
    CapabilityEvaluationResult Evaluate(
        NpcRuntime participant,
        CapabilityEvaluationContext context = null);
}

public sealed class CapabilityEvaluationService
{
    private readonly ICapabilityModel capabilityModel;

    public ICapabilityModel CapabilityModel => capabilityModel;

    public CapabilityEvaluationService(ICapabilityModel capabilityModel)
    {
        this.capabilityModel = capabilityModel ?? throw new ArgumentNullException(nameof(capabilityModel));
    }

    public CapabilityEvaluationResult Evaluate(
        NpcRuntime participant,
        CapabilityEvaluationContext context = null)
    {
        return capabilityModel.Evaluate(participant, context);
    }
}

public sealed class GenericCapabilityModel : ICapabilityModel
{
    private readonly GenericCapabilityModelConfiguration configuration;

    public GenericCapabilityModelConfiguration Configuration => configuration;

    public GenericCapabilityModel(GenericCapabilityModelConfiguration configuration)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        CapabilityAuthoringValidator.ThrowIfInvalidConfiguration(configuration);
    }

    public CapabilityEvaluationResult Evaluate(
        NpcRuntime participant,
        CapabilityEvaluationContext context = null)
    {
        if (participant == null)
        {
            throw new ArgumentNullException(nameof(participant));
        }

        CapabilityAuthoringValidator.ThrowIfInvalidNpc(participant.NpcData);
        List<CapabilityBreakdownEntry> breakdown = new List<CapabilityBreakdownEntry>();
        float total = 0f;

        total += AddBaseContribution(participant, configuration.PhysicalAttribute, configuration.PhysicalWeight, breakdown);
        total += AddBaseContribution(participant, configuration.MentalAttribute, configuration.MentalWeight, breakdown);

        foreach (GenericTraitCapabilityModifier traitModifier in configuration.TraitModifiers)
        {
            if (traitModifier == null || participant.NpcData.traits.Contains(traitModifier.Trait) == false)
            {
                continue;
            }

            total += AddAttributeContribution(
                CapabilityContributionSource.Trait,
                traitModifier.Trait.DefinitionId,
                traitModifier.Attribute,
                traitModifier.AdditiveValue,
                GetAttributeWeight(traitModifier.Attribute),
                breakdown);
        }

        HashSet<ItemData> countedItems = new HashSet<ItemData>();
        foreach (InventoryItemRuntime inventoryItem in participant.Inventory.Items)
        {
            if (inventoryItem == null || inventoryItem.Item == null || inventoryItem.Amount <= 0
                || countedItems.Add(inventoryItem.Item) == false)
            {
                continue;
            }

            CapabilityAuthoringValidator.ThrowIfInvalidItem(inventoryItem.Item);
            foreach (CapabilityAttributeModifier itemModifier in inventoryItem.Item.CapabilityModifiers)
            {
                if (itemModifier == null)
                {
                    continue;
                }

                total += AddAttributeContribution(
                    CapabilityContributionSource.Item,
                    inventoryItem.Item.DefinitionId,
                    itemModifier.Attribute,
                    itemModifier.AdditiveValue,
                    GetAttributeWeight(itemModifier.Attribute),
                    breakdown);
            }
        }

        IReadOnlyList<CapabilityContextModifier> contextModifiers = context?.Modifiers;
        if (contextModifiers != null)
        {
            total += AddContextContributions(contextModifiers, CapabilityContributionSource.Context, breakdown);
        }

        if (participant is ICapabilityConditionSource conditionSource)
        {
            float conditionMultiplier = conditionSource.GetCapabilityMultiplier();
            if (float.IsNaN(conditionMultiplier) == true
                || float.IsInfinity(conditionMultiplier) == true
                || conditionMultiplier < 0f)
            {
                throw new InvalidOperationException("Capability condition source returned an invalid multiplier.");
            }

            float beforeCondition = total;
            total *= conditionMultiplier;
            float conditionContribution = total - beforeCondition;
            if (Mathf.Approximately(conditionContribution, 0f) == false)
            {
                breakdown.Add(new CapabilityBreakdownEntry(
                    CapabilityContributionSource.Condition,
                    conditionSource.ConditionSourceId,
                    configuration.PhysicalAttribute,
                    conditionContribution,
                    1f,
                    conditionContribution));
            }
        }

        return new CapabilityEvaluationResult(
            GetBaseCapability(breakdown),
            Mathf.Max(0f, total),
            breakdown);
    }

    private float AddBaseContribution(
        NpcRuntime participant,
        CapabilityAttributeData attribute,
        float weight,
        List<CapabilityBreakdownEntry> breakdown)
    {
        float value = GetAttributeValue(participant.NpcData.capabilityValues, attribute);
        return AddAttributeContribution(
            CapabilityContributionSource.BaseAttribute,
            attribute.DefinitionId,
            attribute,
            value,
            weight,
            breakdown);
    }

    private float AddContextContributions(
        IReadOnlyList<CapabilityContextModifier> modifiers,
        CapabilityContributionSource source,
        List<CapabilityBreakdownEntry> breakdown)
    {
        float total = 0f;
        if (modifiers == null)
        {
            return total;
        }

        foreach (CapabilityContextModifier modifier in modifiers)
        {
            if (modifier == null)
            {
                continue;
            }

            float weightedAdditive = modifier.AdditiveValue * modifier.Multiplier;
            total += AddAttributeContribution(
                source,
                modifier.ModifierId,
                modifier.Attribute,
                weightedAdditive,
                GetAttributeWeight(modifier.Attribute),
                breakdown);
        }

        return total;
    }

    private float AddAttributeContribution(
        CapabilityContributionSource source,
        string sourceId,
        CapabilityAttributeData attribute,
        float rawValue,
        float weight,
        List<CapabilityBreakdownEntry> breakdown)
    {
        if (attribute == null || IsConfiguredAttribute(attribute) == false)
        {
            return 0f;
        }

        float contribution = rawValue * Mathf.Max(0f, weight);
        if (Mathf.Approximately(contribution, 0f) == true && source != CapabilityContributionSource.BaseAttribute)
        {
            return 0f;
        }

        breakdown.Add(new CapabilityBreakdownEntry(
            source,
            sourceId,
            attribute,
            rawValue,
            weight,
            contribution));
        return contribution;
    }

    private float GetAttributeWeight(CapabilityAttributeData attribute)
    {
        if (attribute == configuration.PhysicalAttribute)
        {
            return configuration.PhysicalWeight;
        }

        if (attribute == configuration.MentalAttribute)
        {
            return configuration.MentalWeight;
        }

        return 0f;
    }

    private bool IsConfiguredAttribute(CapabilityAttributeData attribute)
    {
        return attribute == configuration.PhysicalAttribute || attribute == configuration.MentalAttribute;
    }

    private static float GetAttributeValue(
        IReadOnlyList<CapabilityAttributeValue> values,
        CapabilityAttributeData attribute)
    {
        if (values == null || attribute == null)
        {
            return 0f;
        }

        foreach (CapabilityAttributeValue value in values)
        {
            if (value != null && value.Attribute == attribute)
            {
                return Mathf.Max(0f, value.Value);
            }
        }

        return 0f;
    }

    private static float GetBaseCapability(IReadOnlyList<CapabilityBreakdownEntry> breakdown)
    {
        float total = 0f;
        if (breakdown == null)
        {
            return total;
        }

        foreach (CapabilityBreakdownEntry entry in breakdown)
        {
            if (entry != null && entry.Source == CapabilityContributionSource.BaseAttribute)
            {
                total += entry.Contribution;
            }
        }

        return total;
    }
}

public static class CapabilityAuthoringValidator
{
    public static bool ValidateNpc(NpcData npcData, out string diagnostic)
    {
        if (npcData == null)
        {
            diagnostic = "NpcData is null.";
            return false;
        }

        if (ValidateValues(npcData.capabilityValues, out diagnostic) == false)
        {
            return false;
        }

        if (npcData.traits == null)
        {
            diagnostic = "NpcData traits collection is null.";
            return false;
        }

        HashSet<string> traitIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < npcData.traits.Count; i++)
        {
            TraitData trait = npcData.traits[i];
            if (trait == null)
            {
                diagnostic = "NpcData trait at index " + i.ToString(CultureInfo.InvariantCulture) + " is null.";
                return false;
            }

            if (trait.TryValidate(out string traitDiagnostic) == false)
            {
                diagnostic = "NpcData trait at index " + i.ToString(CultureInfo.InvariantCulture) + " is invalid: " + traitDiagnostic;
                return false;
            }

            if (traitIds.Add(trait.DefinitionId) == false)
            {
                diagnostic = "NpcData contains duplicate trait DefinitionId '" + trait.DefinitionId + "'.";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }

    public static bool ValidateValues(
        IReadOnlyList<CapabilityAttributeValue> values,
        out string diagnostic)
    {
        diagnostic = null;
        if (values == null)
        {
            diagnostic = "Capability values collection is null.";
            return false;
        }

        HashSet<string> attributeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < values.Count; i++)
        {
            CapabilityAttributeValue value = values[i];
            if (value == null)
            {
                diagnostic = "Capability value at index " + i.ToString(CultureInfo.InvariantCulture) + " is null.";
                return false;
            }

            if (value.Attribute == null)
            {
                diagnostic = "Capability value at index " + i.ToString(CultureInfo.InvariantCulture) + " has a null attribute.";
                return false;
            }

            if (value.Attribute.TryValidate(out string attributeDiagnostic) == false)
            {
                diagnostic = "Capability value at index " + i.ToString(CultureInfo.InvariantCulture) + " has an invalid attribute: " + attributeDiagnostic;
                return false;
            }

            if (attributeIds.Add(value.Attribute.DefinitionId) == false)
            {
                diagnostic = "Capability values contain duplicate attribute DefinitionId '" + value.Attribute.DefinitionId + "'.";
                return false;
            }

            if (float.IsNaN(value.Value) == true || float.IsInfinity(value.Value) == true || value.Value < 0f)
            {
                diagnostic = "Capability value for attribute '" + value.Attribute.DefinitionId + "' must be finite and non-negative.";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }

    public static bool ValidateItem(ItemData itemData, out string diagnostic)
    {
        diagnostic = null;
        if (itemData == null)
        {
            diagnostic = "ItemData is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(itemData.DefinitionId) == true)
        {
            diagnostic = "ItemData DefinitionId is empty.";
            return false;
        }

        if (itemData.CapabilityModifiers == null)
        {
            diagnostic = "ItemData capability modifiers collection is null.";
            return false;
        }

        for (int i = 0; i < itemData.CapabilityModifiers.Count; i++)
        {
            CapabilityAttributeModifier modifier = itemData.CapabilityModifiers[i];
            if (modifier == null)
            {
                diagnostic = "ItemData capability modifier at index " + i.ToString(CultureInfo.InvariantCulture) + " is null.";
                return false;
            }

            if (modifier.Attribute == null)
            {
                diagnostic = "ItemData capability modifier at index " + i.ToString(CultureInfo.InvariantCulture) + " has a null attribute.";
                return false;
            }

            if (modifier.Attribute.TryValidate(out string attributeDiagnostic) == false)
            {
                diagnostic = "ItemData capability modifier at index " + i.ToString(CultureInfo.InvariantCulture) + " is invalid: " + attributeDiagnostic;
                return false;
            }

            if (float.IsNaN(modifier.AdditiveValue) == true
                || float.IsInfinity(modifier.AdditiveValue) == true
                || modifier.AdditiveValue < 0f)
            {
                diagnostic = "ItemData capability modifier for attribute '" + modifier.Attribute.DefinitionId + "' must be finite and non-negative.";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }

    public static bool ValidateDefinitions(
        IEnumerable<CapabilityAttributeData> definitions,
        out string diagnostic)
    {
        diagnostic = null;
        if (definitions == null)
        {
            diagnostic = "Capability definitions collection is null.";
            return false;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (CapabilityAttributeData definition in definitions)
        {
            if (definition == null)
            {
                diagnostic = "Capability definition at index " + index.ToString(CultureInfo.InvariantCulture) + " is null.";
                return false;
            }

            if (definition.TryValidate(out string definitionDiagnostic) == false)
            {
                diagnostic = "Capability definition at index " + index.ToString(CultureInfo.InvariantCulture) + " is invalid: " + definitionDiagnostic;
                return false;
            }

            if (ids.Add(definition.DefinitionId) == false)
            {
                diagnostic = "Capability definitions contain duplicate DefinitionId '" + definition.DefinitionId + "'.";
                return false;
            }

            index++;
        }

        diagnostic = null;
        return true;
    }

    public static void ThrowIfInvalidNpc(NpcData npcData)
    {
        if (ValidateNpc(npcData, out string diagnostic) == false)
        {
            throw new InvalidOperationException("Invalid NPC capability authoring: " + diagnostic);
        }
    }

    public static void ThrowIfInvalidItem(ItemData itemData)
    {
        if (ValidateItem(itemData, out string diagnostic) == false)
        {
            throw new InvalidOperationException("Invalid item capability authoring: " + diagnostic);
        }
    }

    public static void ThrowIfInvalidConfiguration(GenericCapabilityModelConfiguration configuration)
    {
        string diagnostic;
        if (configuration.PhysicalAttribute == null)
        {
            throw new InvalidOperationException("Invalid generic capability configuration physical attribute: null.");
        }

        if (configuration.PhysicalAttribute.TryValidate(out diagnostic) == false)
        {
            throw new InvalidOperationException("Invalid generic capability configuration physical attribute: " + diagnostic);
        }

        if (configuration.MentalAttribute == null)
        {
            throw new InvalidOperationException("Invalid generic capability configuration mental attribute: null.");
        }

        if (configuration.MentalAttribute.TryValidate(out diagnostic) == false)
        {
            throw new InvalidOperationException("Invalid generic capability configuration mental attribute: " + diagnostic);
        }

        if (configuration.PhysicalAttribute == configuration.MentalAttribute
            || string.Equals(configuration.PhysicalAttribute.DefinitionId, configuration.MentalAttribute.DefinitionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Generic capability configuration requires distinct physical and mental attributes.");
        }

        if (float.IsNaN(configuration.PhysicalWeight) == true || float.IsInfinity(configuration.PhysicalWeight) == true || configuration.PhysicalWeight < 0f
            || float.IsNaN(configuration.MentalWeight) == true || float.IsInfinity(configuration.MentalWeight) == true || configuration.MentalWeight < 0f)
        {
            throw new InvalidOperationException("Generic capability configuration weights must be finite and non-negative.");
        }

        HashSet<string> traitModifierKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (GenericTraitCapabilityModifier modifier in configuration.TraitModifiers)
        {
            if (modifier == null)
            {
                throw new InvalidOperationException("Generic capability configuration contains a null trait modifier.");
            }

            if (modifier.Trait == null)
            {
                throw new InvalidOperationException("Generic capability configuration contains a trait modifier with a null trait.");
            }

            if (modifier.Trait.TryValidate(out diagnostic) == false)
            {
                throw new InvalidOperationException("Generic capability configuration contains an invalid trait modifier: " + diagnostic);
            }

            if (modifier.Attribute == null)
            {
                throw new InvalidOperationException("Generic capability configuration contains a trait modifier with a null attribute.");
            }

            if (modifier.Attribute.TryValidate(out diagnostic) == false)
            {
                throw new InvalidOperationException("Generic capability configuration contains an invalid trait modifier: " + diagnostic);
            }

            if (modifier.AdditiveValue < 0f || float.IsNaN(modifier.AdditiveValue) == true || float.IsInfinity(modifier.AdditiveValue) == true)
            {
                throw new InvalidOperationException("Generic capability trait modifiers must be finite and non-negative.");
            }

            string key = modifier.Trait.DefinitionId + "\u001f" + modifier.Attribute.DefinitionId;
            if (traitModifierKeys.Add(key) == false)
            {
                throw new InvalidOperationException("Generic capability configuration contains duplicate trait modifier '" + key + "'.");
            }
        }
    }
}
