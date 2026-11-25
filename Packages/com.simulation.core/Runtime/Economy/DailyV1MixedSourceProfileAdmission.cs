using System;
using System.Collections.Generic;

/// <summary>Pure admission for the selected typed City source projection.</summary>
internal static class DailyV1MixedSourceProfileAdmission
{
    internal static bool TryValidate(DailyV1CityData city, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (city == null || city.materialFlowProfile == null
            || city.materialFlowProfile.Value != (int)LocalMaterialFlowProfile.MixedSourcesDaily)
        { rejectionReason = "MissingMixedSourcesCity"; return false; }
        if (string.IsNullOrWhiteSpace(city.settlementSemanticId)
            || string.IsNullOrWhiteSpace(city.materialFlowLocationId)
            || string.IsNullOrWhiteSpace(city.marketStoreSemanticId))
        { rejectionReason = "MixedSourcesCityIdentityIncomplete"; return false; }
        if (city.productionConfigs == null || city.productionConfigs.Count != 2)
        { rejectionReason = "MixedSourcesProfileRequiresExactlyTwoSources"; return false; }

        HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
        bool hasExogenous = false;
        bool hasFinite = false;
        string itemDefinitionId = null;
        for (int i = 0; i < city.productionConfigs.Count; i++)
        {
            DailyV1CityProductionConfig source = city.productionConfigs[i];
            if (source == null || string.IsNullOrWhiteSpace(source.item)
                || source.amountPerDay <= 0 || string.IsNullOrWhiteSpace(source.productionSourceId)
                || string.IsNullOrWhiteSpace(source.contentRevision) || source.sourceKind == null
                || !Enum.IsDefined(typeof(CityProductionSourceKind), source.sourceKind.Value)
                || !sourceIds.Add(source.productionSourceId))
            { rejectionReason = "MixedSourcesSourceIdentityOrQuantityInvalid"; return false; }

            if (itemDefinitionId == null) itemDefinitionId = source.item;
            else if (!string.Equals(itemDefinitionId, source.item, StringComparison.Ordinal))
            { rejectionReason = "MixedSourcesItemMismatch"; return false; }

            if (source.sourceKind.Value == (int)CityProductionSourceKind.ExogenousDaily)
            {
                if (hasExogenous || source.initialReserve != 0)
                { rejectionReason = "MixedSourcesExogenousPolicyInvalid"; return false; }
                hasExogenous = true;
            }
            else if (source.sourceKind.Value == (int)CityProductionSourceKind.FiniteReserveDaily)
            {
                if (hasFinite || source.initialReserve < 0)
                { rejectionReason = "MixedSourcesFinitePolicyInvalid"; return false; }
                hasFinite = true;
            }
            else
            { rejectionReason = "MixedSourcesExplicitKindRequired"; return false; }
        }

        if (!hasExogenous || !hasFinite)
        { rejectionReason = "MixedSourcesRequiresOneSourceOfEachKind"; return false; }
        if (city.marketItems == null || city.marketItems.Count != 1
            || city.marketItems[0] == null || string.IsNullOrWhiteSpace(city.marketItems[0].item)
            || !string.Equals(city.marketItems[0].item, itemDefinitionId, StringComparison.Ordinal)
            || city.populationConsumption?.paymentMode == null
            || city.populationConsumption.paymentMode.Value != (int)ConsumptionPaymentMode.Free)
        { rejectionReason = "MixedSourcesRequiresMatchingSingleMarketItemAndFreeConsumption"; return false; }

        return true;
    }
}
