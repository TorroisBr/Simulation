using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

internal enum P12DCityRootOwnerSnapshotFailure
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidCityOwner,
    InvalidOwnerSectionVector,
    UnsupportedCityAccountState,
    ExcludedCityStatePresent,
    InvalidCityMembership,
    InvalidMarketState,
    InvalidPopulationState,
    InvalidSnapshot,
    MissingCityDefinition,
    AmbiguousCityDefinition,
    MissingItemDefinition,
    AmbiguousItemDefinition,
    MissingLegacyLocation,
    AmbiguousLegacyLocation,
    StageFailed
}

/// <summary>
/// Detached schema-v1 values for one Daily-v1 City owner and its currently
/// admitted Market and settlement-population components. The token and capture
/// stamp are inputs to a caller-owned shared capture window; neither is retained.
/// </summary>
internal sealed class P12DCityRootOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal string CityRuntimeId { get; }
    internal string CityDefinitionId { get; }
    internal string LegacyLocationRuntimeId { get; }
    internal long ImportantNpcRevision { get; }
    internal IReadOnlyList<string> ImportantNpcRuntimeIds { get; }
    internal string MarketCounterpartyRuntimeId { get; }
    internal MarketLiquidityMode MarketLiquidityMode { get; }
    internal bool MarketCounterpartyHasAccount { get; }
    internal long MarketRevision { get; }
    internal IReadOnlyList<P12DCityMarketItemSnapshot> MarketItems { get; }
    internal string PopulationEconomicRuntimeId { get; }
    internal ConsumptionPaymentMode PopulationPaymentMode { get; }
    internal bool PopulationEconomyHasAccount { get; }
    internal string PopulationRuntimeId { get; }
    internal int CurrentPopulation { get; }
    internal long PopulationRevision { get; }
    internal long PopulationReceiptRevision { get; }
    internal IReadOnlyList<PopulationOperationReceiptSnapshot> PopulationReceipts { get; }

    private P12DCityRootOwnerSnapshot(
        string cityRuntimeId,
        string cityDefinitionId,
        string legacyLocationRuntimeId,
        long importantNpcRevision,
        IReadOnlyList<string> importantNpcRuntimeIds,
        string marketCounterpartyRuntimeId,
        MarketLiquidityMode marketLiquidityMode,
        long marketRevision,
        IReadOnlyList<P12DCityMarketItemSnapshot> marketItems,
        string populationEconomicRuntimeId,
        ConsumptionPaymentMode populationPaymentMode,
        string populationRuntimeId,
        int currentPopulation,
        long populationRevision,
        long populationReceiptRevision,
        IReadOnlyList<PopulationOperationReceiptSnapshot> populationReceipts)
    {
        SchemaVersion = CurrentSchemaVersion;
        CityRuntimeId = cityRuntimeId;
        CityDefinitionId = cityDefinitionId;
        LegacyLocationRuntimeId = legacyLocationRuntimeId;
        ImportantNpcRevision = importantNpcRevision;
        ImportantNpcRuntimeIds = new ReadOnlyCollection<string>(new List<string>(importantNpcRuntimeIds));
        MarketCounterpartyRuntimeId = marketCounterpartyRuntimeId;
        MarketLiquidityMode = marketLiquidityMode;
        MarketCounterpartyHasAccount = false;
        MarketRevision = marketRevision;
        List<P12DCityMarketItemSnapshot> copiedMarketItems = new List<P12DCityMarketItemSnapshot>(marketItems.Count);
        foreach (P12DCityMarketItemSnapshot item in marketItems)
        {
            copiedMarketItems.Add(new P12DCityMarketItemSnapshot(
                item.ItemDefinitionId,
                item.Amount,
                item.DesiredAmount,
                item.CurrentPrice));
        }
        MarketItems = new ReadOnlyCollection<P12DCityMarketItemSnapshot>(copiedMarketItems);
        PopulationEconomicRuntimeId = populationEconomicRuntimeId;
        PopulationPaymentMode = populationPaymentMode;
        PopulationEconomyHasAccount = false;
        PopulationRuntimeId = populationRuntimeId;
        CurrentPopulation = currentPopulation;
        PopulationRevision = populationRevision;
        PopulationReceiptRevision = populationReceiptRevision;
        List<PopulationOperationReceiptSnapshot> copiedReceipts =
            new List<PopulationOperationReceiptSnapshot>(populationReceipts.Count);
        foreach (PopulationOperationReceiptSnapshot receipt in populationReceipts)
        {
            copiedReceipts.Add(new PopulationOperationReceiptSnapshot(
                receipt.Identity,
                receipt.Fingerprint,
                receipt.Transition));
        }
        PopulationReceipts = new ReadOnlyCollection<PopulationOperationReceiptSnapshot>(copiedReceipts);
    }

    internal static bool TryCapture(
        CityRuntime city,
        DailyCaptureEligibilityToken token,
        object sharedCaptureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12DCityRootOwnerSnapshot snapshot,
        out P12DCityRootOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12DCityRootOwnerSnapshotFailure.InvalidCityOwner;
        if (city == null
            || string.IsNullOrWhiteSpace(city.RuntimeId)
            || string.IsNullOrWhiteSpace(city.DefinitionId)
            || token == null
            || sharedCaptureStamp == null
            || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidCaptureContext;
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = P12DCityRootOwnerSnapshotFailure.UnsupportedProfile;
            return false;
        }

        if (!city.TryGetInstalledSnapshotOwners(
                out SettlementPopulationRuntime population,
                out MarketRuntime market,
                out MarketCounterpartyRuntime counterparty,
                out PopulationEconomyRuntime populationEconomy,
                out SpatialLocationRuntime location)
            || !string.Equals(population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(location.RuntimeId))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidCityOwner;
            return false;
        }

        if (counterparty.LiquidityMode != MarketLiquidityMode.Open
            || counterparty.MoneyAccount != null
            || !string.Equals(counterparty.CounterpartyRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !ReferenceEquals(market.InstalledCounterparty, counterparty)
            || populationEconomy.PaymentMode != ConsumptionPaymentMode.Free
            || populationEconomy.MoneyAccount != null
            || !string.Equals(populationEconomy.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(
                populationEconomy.PopulationEconomicRuntimeId,
                "population-" + city.RuntimeId,
                StringComparison.Ordinal))
        {
            failure = P12DCityRootOwnerSnapshotFailure.UnsupportedCityAccountState;
            return false;
        }

        city.GetDailyEconomyReceiptExclusionProof(out int cityReceiptCount, out long cityReceiptRevision);
        if (cityReceiptCount != 0 || cityReceiptRevision != 0L
            || city.HasLocalDailyMaterialFlow
            || city.FiniteProductionSources != null
            || city.LastMaterialFlow != null)
        {
            failure = P12DCityRootOwnerSnapshotFailure.ExcludedCityStatePresent;
            return false;
        }

        if (!city.TryCopyOwnerSnapshotMembership(
                out IReadOnlyList<string> memberRuntimeIds,
                out long importantNpcRevision))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidCityMembership;
            return false;
        }

        if (population.CurrentPopulation < 0
            || population.Revision < 0L
            || market.Revision < 0L)
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidPopulationState;
            return false;
        }

        if (!population.TryCaptureOperationReceiptSnapshot(
                out IReadOnlyList<PopulationOperationReceiptSnapshot> populationReceipts,
                out long populationReceiptRevision)
            || populationReceipts == null
            || populationReceiptRevision < 0L
            || populationReceiptRevision < populationReceipts.Count)
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidPopulationState;
            return false;
        }

        IReadOnlyList<MarketItemRuntime> installedRows = market.Items;
        if (installedRows == null)
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidMarketState;
            return false;
        }

        List<P12DCityMarketItemSnapshot> marketRows = new List<P12DCityMarketItemSnapshot>(installedRows.Count);
        foreach (MarketItemRuntime row in installedRows)
        {
            if (row == null
                || row.Item == null
                || string.IsNullOrWhiteSpace(row.Item.DefinitionId)
                || row.Amount < 0
                || row.DesiredAmount <= 0
                || row.CurrentPrice <= 0f
                || float.IsNaN(row.CurrentPrice)
                || float.IsInfinity(row.CurrentPrice))
            {
                failure = P12DCityRootOwnerSnapshotFailure.InvalidMarketState;
                return false;
            }

            marketRows.Add(new P12DCityMarketItemSnapshot(
                row.Item.DefinitionId,
                row.Amount,
                row.DesiredAmount,
                row.CurrentPrice));
        }

        string encodedRuntimeId = city.RuntimeId.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + city.RuntimeId;
        if (!MatchesUniqueSection(
                sharedOwnerSectionVector,
                CityNpcPresenceCensusProvider.SectionIdFor(city.RuntimeId),
                CityNpcPresenceCensusProvider.SchemaVersion,
                city,
                memberRuntimeIds.Count,
                importantNpcRevision)
            || !MatchesUniqueSection(
                sharedOwnerSectionVector,
                CityMarketCensusProvider.SectionIdPrefix + encodedRuntimeId,
                CityMarketCensusProvider.SchemaVersion,
                market,
                marketRows.Count,
                market.Revision)
            || !MatchesUniqueSection(
                sharedOwnerSectionVector,
                SettlementPopulationCensusProvider.AggregateSectionPrefix + encodedRuntimeId,
                SettlementPopulationCensusProvider.SchemaVersion,
                population,
                1,
                population.Revision)
            || !MatchesUniqueSection(
                sharedOwnerSectionVector,
                SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + encodedRuntimeId,
                SettlementPopulationCensusProvider.SchemaVersion,
                population,
                populationReceipts.Count,
                populationReceiptRevision))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidOwnerSectionVector;
            return false;
        }

        snapshot = new P12DCityRootOwnerSnapshot(
            city.RuntimeId,
            city.DefinitionId,
            location.RuntimeId,
            importantNpcRevision,
            memberRuntimeIds,
            counterparty.CounterpartyRuntimeId,
            counterparty.LiquidityMode,
            market.Revision,
            marketRows,
            populationEconomy.PopulationEconomicRuntimeId,
            populationEconomy.PaymentMode,
            population.SettlementRuntimeId,
            population.CurrentPopulation,
            population.Revision,
            populationReceiptRevision,
            populationReceipts);
        failure = P12DCityRootOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        IReadOnlyList<CityData> admittedCityDefinitions,
        IReadOnlyList<ItemData> admittedItemDefinitions,
        IReadOnlyList<SpatialLocationRuntime> stagedLocations,
        out CityRuntime city,
        out P12DCityMembershipLinker membershipLinker,
        out P12DCityRootOwnerSnapshotFailure failure)
    {
        city = null;
        membershipLinker = null;
        failure = P12DCityRootOwnerSnapshotFailure.InvalidSnapshot;
        if (SchemaVersion != CurrentSchemaVersion
            || string.IsNullOrWhiteSpace(CityRuntimeId)
            || string.IsNullOrWhiteSpace(CityDefinitionId)
            || string.IsNullOrWhiteSpace(LegacyLocationRuntimeId)
            || ImportantNpcRevision < 0L
            || ImportantNpcRuntimeIds == null
            || MarketLiquidityMode != MarketLiquidityMode.Open
            || MarketCounterpartyHasAccount
            || string.IsNullOrWhiteSpace(MarketCounterpartyRuntimeId)
            || !string.Equals(MarketCounterpartyRuntimeId, CityRuntimeId, StringComparison.Ordinal)
            || MarketRevision < 0L
            || MarketItems == null
            || PopulationPaymentMode != ConsumptionPaymentMode.Free
            || PopulationEconomyHasAccount
            || !string.Equals(PopulationEconomicRuntimeId, "population-" + CityRuntimeId, StringComparison.Ordinal)
            || !string.Equals(PopulationRuntimeId, CityRuntimeId, StringComparison.Ordinal)
            || CurrentPopulation < 0
            || PopulationRevision < 0L
            || PopulationReceiptRevision < 0L
            || PopulationReceipts == null
            || PopulationReceiptRevision < PopulationReceipts.Count
            || admittedCityDefinitions == null
            || admittedItemDefinitions == null
            || stagedLocations == null)
        {
            return false;
        }

        if (!TryResolveUniqueCityDefinition(admittedCityDefinitions, CityDefinitionId, out CityData cityDefinition,
                out failure)
            || !TryResolveUniqueLocation(stagedLocations, LegacyLocationRuntimeId, out SpatialLocationRuntime location,
                out failure))
        {
            return false;
        }

        List<MarketItemRuntime> marketRows = new List<MarketItemRuntime>(MarketItems.Count);
        foreach (P12DCityMarketItemSnapshot row in MarketItems)
        {
            if (row == null
                || string.IsNullOrWhiteSpace(row.ItemDefinitionId)
                || row.Amount < 0
                || row.DesiredAmount <= 0
                || row.CurrentPrice <= 0f
                || float.IsNaN(row.CurrentPrice)
                || float.IsInfinity(row.CurrentPrice)
                || !TryResolveUniqueItemDefinition(
                    admittedItemDefinitions,
                    row.ItemDefinitionId,
                    out ItemData itemDefinition,
                    out failure)
                || !MarketItemRuntime.TryCreateFromOwnerSnapshot(
                    itemDefinition,
                    row.Amount,
                    row.DesiredAmount,
                    row.CurrentPrice,
                    out MarketItemRuntime stagedRow))
            {
                if (failure == P12DCityRootOwnerSnapshotFailure.None)
                    failure = P12DCityRootOwnerSnapshotFailure.InvalidSnapshot;
                return false;
            }

            marketRows.Add(stagedRow);
        }

        if (!SettlementPopulationRuntime.TryCreateFromOwnerSnapshot(
                PopulationRuntimeId,
                CurrentPopulation,
                PopulationRevision,
                PopulationReceipts,
                PopulationReceiptRevision,
                out SettlementPopulationRuntime population))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidPopulationState;
            return false;
        }

        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateOpen(MarketCounterpartyRuntimeId);
        if (!MarketRuntime.TryCreateFromOwnerSnapshot(marketRows, MarketRevision, counterparty, out MarketRuntime market))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidMarketState;
            return false;
        }

        PopulationEconomyRuntime populationEconomy = new PopulationEconomyRuntime(
            CityRuntimeId,
            new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.Free },
            MarketLiquidityMode.Open);
        if (!string.Equals(
                populationEconomy.PopulationEconomicRuntimeId,
                PopulationEconomicRuntimeId,
                StringComparison.Ordinal))
        {
            failure = P12DCityRootOwnerSnapshotFailure.InvalidSnapshot;
            return false;
        }

        if (!CityRuntime.TryCreateFromOwnerSnapshot(
                CityRuntimeId,
                cityDefinition,
                location,
                counterparty,
                market,
                populationEconomy,
                population,
                ImportantNpcRevision,
                ImportantNpcRuntimeIds,
                out city,
                out membershipLinker))
        {
            failure = P12DCityRootOwnerSnapshotFailure.StageFailed;
            city = null;
            membershipLinker = null;
            return false;
        }

        failure = P12DCityRootOwnerSnapshotFailure.None;
        return true;
    }

    private static bool MatchesUniqueSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusSnapshot match = null;
        int matchCount = 0;
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < sections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = sections[i];
            if (section == null
                || string.IsNullOrWhiteSpace(section.SectionId)
                || !uniqueIds.Add(section.SectionId))
            {
                return false;
            }

            if (string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
            {
                match = section;
                matchCount++;
            }
        }

        return matchCount == 1
            && match.SchemaVersion == schemaVersion
            && match.Role == OwnerSectionRole.Required
            && ReferenceEquals(match.OwnerInstanceIdentity, owner)
            && match.Cardinality == cardinality
            && match.Revision == revision;
    }

    private static bool TryResolveUniqueCityDefinition(
        IReadOnlyList<CityData> definitions,
        string definitionId,
        out CityData result,
        out P12DCityRootOwnerSnapshotFailure failure)
    {
        result = null;
        failure = P12DCityRootOwnerSnapshotFailure.MissingCityDefinition;
        int matches = 0;
        for (int i = 0; i < definitions.Count; i++)
        {
            CityData candidate = definitions[i];
            if (candidate != null && string.Equals(candidate.DefinitionId, definitionId, StringComparison.Ordinal))
            {
                result = candidate;
                matches++;
            }
        }

        if (matches == 1)
        {
            failure = P12DCityRootOwnerSnapshotFailure.None;
            return true;
        }

        failure = matches == 0
            ? P12DCityRootOwnerSnapshotFailure.MissingCityDefinition
            : P12DCityRootOwnerSnapshotFailure.AmbiguousCityDefinition;
        result = null;
        return false;
    }

    private static bool TryResolveUniqueItemDefinition(
        IReadOnlyList<ItemData> definitions,
        string definitionId,
        out ItemData result,
        out P12DCityRootOwnerSnapshotFailure failure)
    {
        result = null;
        failure = P12DCityRootOwnerSnapshotFailure.MissingItemDefinition;
        int matches = 0;
        for (int i = 0; i < definitions.Count; i++)
        {
            ItemData candidate = definitions[i];
            if (candidate != null && string.Equals(candidate.DefinitionId, definitionId, StringComparison.Ordinal))
            {
                result = candidate;
                matches++;
            }
        }

        if (matches == 1)
        {
            failure = P12DCityRootOwnerSnapshotFailure.None;
            return true;
        }

        failure = matches == 0
            ? P12DCityRootOwnerSnapshotFailure.MissingItemDefinition
            : P12DCityRootOwnerSnapshotFailure.AmbiguousItemDefinition;
        result = null;
        return false;
    }

    private static bool TryResolveUniqueLocation(
        IReadOnlyList<SpatialLocationRuntime> locations,
        string runtimeId,
        out SpatialLocationRuntime result,
        out P12DCityRootOwnerSnapshotFailure failure)
    {
        result = null;
        failure = P12DCityRootOwnerSnapshotFailure.MissingLegacyLocation;
        int matches = 0;
        for (int i = 0; i < locations.Count; i++)
        {
            SpatialLocationRuntime candidate = locations[i];
            if (candidate != null && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                result = candidate;
                matches++;
            }
        }

        if (matches == 1)
        {
            failure = P12DCityRootOwnerSnapshotFailure.None;
            return true;
        }

        failure = matches == 0
            ? P12DCityRootOwnerSnapshotFailure.MissingLegacyLocation
            : P12DCityRootOwnerSnapshotFailure.AmbiguousLegacyLocation;
        result = null;
        return false;
    }
}

internal sealed class P12DCityMarketItemSnapshot
{
    internal string ItemDefinitionId { get; }
    internal int Amount { get; }
    internal int DesiredAmount { get; }
    internal float CurrentPrice { get; }

    internal P12DCityMarketItemSnapshot(
        string itemDefinitionId,
        int amount,
        int desiredAmount,
        float currentPrice)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        DesiredAmount = desiredAmount;
        CurrentPrice = currentPrice;
    }
}

/// <summary>
/// Private one-shot relation linker returned with an unpublished staged City.
/// It preserves the captured ordered IDs without replaying gameplay mutations.
/// The shared D/E/F graph builder still owns cross-owner completeness checks.
/// </summary>
internal sealed class P12DCityMembershipLinker
{
    private readonly CityRuntime city;
    private readonly List<NpcRuntime> backingList;
    private readonly IReadOnlyList<string> expectedRuntimeIds;
    private readonly long expectedRevision;
    private bool filled;

    internal P12DCityMembershipLinker(
        CityRuntime city,
        List<NpcRuntime> backingList,
        IReadOnlyList<string> expectedRuntimeIds,
        long expectedRevision)
    {
        this.city = city ?? throw new ArgumentNullException(nameof(city));
        this.backingList = backingList ?? throw new ArgumentNullException(nameof(backingList));
        this.expectedRuntimeIds = new ReadOnlyCollection<string>(new List<string>(expectedRuntimeIds));
        this.expectedRevision = expectedRevision;
    }

    internal bool IsFilled => filled;
    internal CityRuntime StagedCity => city;
    internal IReadOnlyList<string> PendingNpcRuntimeIds => expectedRuntimeIds;

    internal bool TryFillOnce(IReadOnlyList<NpcRuntime> orderedMembers)
    {
        if (!CanFillOnce(orderedMembers))
            return false;

        FillValidated(orderedMembers);
        return true;
    }

    internal bool CanFillOnce(IReadOnlyList<NpcRuntime> orderedMembers)
    {
        if (filled
            || orderedMembers == null
            || backingList.Count != 0
            || city.ImportantNpcRevision != expectedRevision
            || orderedMembers.Count != expectedRuntimeIds.Count)
        {
            return false;
        }

        HashSet<string> uniqueRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedMembers.Count; i++)
        {
            NpcRuntime member = orderedMembers[i];
            if (member == null
                || string.IsNullOrWhiteSpace(member.RuntimeId)
                || !uniqueRuntimeIds.Add(member.RuntimeId)
                || !string.Equals(member.RuntimeId, expectedRuntimeIds[i], StringComparison.Ordinal)
                || !ReferenceEquals(member.CurrentCity, city)
                || !ReferenceEquals(member.CurrentLocation, city.Location))
            {
                return false;
            }
        }

        return true;
    }

    internal void FillValidated(IReadOnlyList<NpcRuntime> orderedMembers)
    {
        for (int i = 0; i < orderedMembers.Count; i++)
            backingList.Add(orderedMembers[i]);
        filled = true;
    }
}

internal enum P12DCityNpcProjectionCaptureFailure
{
    None = 0,
    InvalidContext,
    UnsupportedProfile,
    InvalidNpcRoster,
    InvalidReceiptOwnerEvidence
}

/// <summary>
/// Transient capture evidence shared by disjoint D/F NPC projections. It is
/// deliberately not retained by an owner snapshot or serialized. Both
/// projections must name the exact completed-boundary token, capture stamp,
/// and owner-section revision vector.
/// </summary>
internal sealed class P12DCityNpcProjectionCaptureEvidence
{
    private readonly DailyCaptureEligibilityToken token;
    private readonly object captureStamp;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector;
    private readonly IReadOnlyList<NpcRuntime> sourceNpcRoster;

    private P12DCityNpcProjectionCaptureEvidence(
        DailyCaptureEligibilityToken token,
        object captureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        IReadOnlyList<NpcRuntime> sourceNpcRoster)
    {
        this.token = token;
        this.captureStamp = captureStamp;
        this.ownerSectionVector = ownerSectionVector;
        this.sourceNpcRoster = new ReadOnlyCollection<NpcRuntime>(new List<NpcRuntime>(sourceNpcRoster));
    }

    internal static bool TryCreate(
        DailyCaptureEligibilityToken token,
        object captureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        IReadOnlyList<NpcRuntime> sourceNpcRoster,
        out P12DCityNpcProjectionCaptureEvidence evidence,
        out P12DCityNpcProjectionCaptureFailure failure)
    {
        evidence = null;
        failure = P12DCityNpcProjectionCaptureFailure.InvalidContext;
        if (token == null
            || captureStamp == null
            || ownerSectionVector == null
            || sourceNpcRoster == null
            || !ReferenceEquals(token.OwnerSections, ownerSectionVector))
        {
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = P12DCityNpcProjectionCaptureFailure.UnsupportedProfile;
            return false;
        }

        if (!HasValidNpcRoster(sourceNpcRoster))
        {
            failure = P12DCityNpcProjectionCaptureFailure.InvalidNpcRoster;
            return false;
        }

        if (!HasExactZeroReceiptOwners(ownerSectionVector, sourceNpcRoster))
        {
            failure = P12DCityNpcProjectionCaptureFailure.InvalidReceiptOwnerEvidence;
            return false;
        }

        evidence = new P12DCityNpcProjectionCaptureEvidence(
            token, captureStamp, ownerSectionVector, sourceNpcRoster);
        failure = P12DCityNpcProjectionCaptureFailure.None;
        return true;
    }

    internal bool HasSameCaptureIdentity(P12DCityNpcProjectionCaptureEvidence other)
    {
        if (other == null
            || !ReferenceEquals(token, other.token)
            || !ReferenceEquals(captureStamp, other.captureStamp)
            || !ReferenceEquals(ownerSectionVector, other.ownerSectionVector)
            || !ReferenceEquals(token.OwnerSections, ownerSectionVector)
            || sourceNpcRoster.Count != other.sourceNpcRoster.Count)
        {
            return false;
        }

        for (int i = 0; i < sourceNpcRoster.Count; i++)
        {
            if (!ReferenceEquals(sourceNpcRoster[i], other.sourceNpcRoster[i]))
                return false;
        }

        return true;
    }

    internal bool AreReceiptOwnersStillExactZero() =>
        HasExactZeroReceiptOwners(ownerSectionVector, sourceNpcRoster);

    internal bool CoversStagedNpcRuntimeIds(IReadOnlyDictionary<string, NpcRuntime> stagedNpcs)
    {
        if (stagedNpcs == null || stagedNpcs.Count != sourceNpcRoster.Count)
            return false;

        for (int i = 0; i < sourceNpcRoster.Count; i++)
        {
            NpcRuntime sourceNpc = sourceNpcRoster[i];
            if (sourceNpc == null
                || !stagedNpcs.ContainsKey(sourceNpc.RuntimeId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasValidNpcRoster(IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null)
            return false;

        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        List<object> ownerIdentities = new List<object>(roster.Count * 2);
        for (int i = 0; i < roster.Count; i++)
        {
            NpcRuntime npc = roster[i];
            object localOwner = npc?.ExistingLocalKnowledgeObservationRuntime;
            object merchantOwner = npc?.ExistingMerchantTradeStateRuntime;
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId)
                || localOwner == null
                || merchantOwner == null
                || ReferenceEquals(localOwner, merchantOwner)
                || ContainsReference(ownerIdentities, localOwner)
                || ContainsReference(ownerIdentities, merchantOwner))
            {
                return false;
            }

            ownerIdentities.Add(localOwner);
            ownerIdentities.Add(merchantOwner);
        }

        return true;
    }

    private static bool HasExactZeroReceiptOwners(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        IReadOnlyList<NpcRuntime> roster)
    {
        if (sections == null || roster == null)
            return false;

        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null
                || string.IsNullOrWhiteSpace(section.SectionId)
                || section.SchemaVersion <= 0
                || section.Cardinality < 0
                || section.Revision < 0L
                || !sectionIds.Add(section.SectionId))
            {
                return false;
            }
        }

        HashSet<string> npcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> expectedReceiptSectionIds = new HashSet<string>(StringComparer.Ordinal);
        List<object> receiptOwnerIdentities = new List<object>(roster.Count * 2);
        for (int i = 0; i < roster.Count; i++)
        {
            NpcRuntime npc = roster[i];
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcRuntimeIds.Add(npc.RuntimeId))
            {
                return false;
            }

            object localOwner = npc.ExistingLocalKnowledgeObservationRuntime;
            object merchantOwner = npc.ExistingMerchantTradeStateRuntime;
            if (localOwner == null
                || merchantOwner == null
                || ReferenceEquals(localOwner, merchantOwner)
                || ContainsReference(receiptOwnerIdentities, localOwner)
                || ContainsReference(receiptOwnerIdentities, merchantOwner)
                || !((NpcLocalKnowledgeObservationRuntime)localOwner)
                    .TryReadP12ReceiptCensus(out int localCount, out long localRevision)
                || localCount != 0
                || localRevision != 0L
                || !((NpcMerchantTradeStateRuntime)merchantOwner)
                    .TryReadP12ReceiptCensus(out int merchantCount, out long merchantRevision)
                || merchantCount != 0
                || merchantRevision != 0L)
            {
                return false;
            }

            receiptOwnerIdentities.Add(localOwner);
            receiptOwnerIdentities.Add(merchantOwner);
            if (!MatchesReceiptSection(
                    sections,
                    P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(npc.RuntimeId),
                    localOwner)
                || !MatchesReceiptSection(
                    sections,
                    P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(npc.RuntimeId),
                    merchantOwner))
            {
                return false;
            }

            expectedReceiptSectionIds.Add(
                P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(npc.RuntimeId));
            expectedReceiptSectionIds.Add(
                P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(npc.RuntimeId));
        }

        int actualReceiptSectionCount = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            bool isReceiptSection = section.SectionId.StartsWith(
                    P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionPrefix,
                    StringComparison.Ordinal)
                || section.SectionId.StartsWith(
                    P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionPrefix,
                    StringComparison.Ordinal);
            if (!isReceiptSection)
                continue;
            actualReceiptSectionCount++;
            if (!expectedReceiptSectionIds.Contains(section.SectionId))
                return false;
        }

        return actualReceiptSectionCount == expectedReceiptSectionIds.Count;
    }

    private static bool MatchesReceiptSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        object owner)
    {
        OwnerSectionCensusSnapshot match = null;
        int count = 0;
        for (int i = 0; i < sections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = sections[i];
            if (!string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            match = section;
            count++;
        }

        return count == 1
            && match.SchemaVersion == P12DNpcReceiptOwnerCensusProvider.SchemaVersion
            && match.Role == OwnerSectionRole.Required
            && ReferenceEquals(match.OwnerInstanceIdentity, owner)
            && match.Cardinality == 0
            && match.Revision == 0L;
    }

    private static bool ContainsReference(IReadOnlyList<object> values, object candidate)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (ReferenceEquals(values[i], candidate))
                return true;
        }

        return false;
    }
}

internal enum P12DCityNpcRelationAssemblyFailure
{
    None = 0,
    InvalidProjectionEvidence,
    InvalidReceiptOwnerEvidence,
    InvalidCitySet,
    InvalidNpcRoster,
    InvalidRelation
}

/// <summary>
/// Completes the unpublished City/NPC relation graph after all Cities and
/// merged D/F NPC instances have been staged. It validates the whole graph
/// before filling any City's private ordered membership list.
/// </summary>
internal static class P12DCityNpcRelationAssembler
{
    internal static bool TryFillMembershipsOnce(
        P12DCityNpcProjectionCaptureEvidence dProjectionEvidence,
        P12DCityNpcProjectionCaptureEvidence fProjectionEvidence,
        IReadOnlyList<P12DCityMembershipLinker> cityMembershipLinkers,
        IReadOnlyList<NpcRuntime> stagedNpcRoster,
        out P12DCityNpcRelationAssemblyFailure failure)
    {
        failure = P12DCityNpcRelationAssemblyFailure.InvalidProjectionEvidence;
        if (dProjectionEvidence == null
            || fProjectionEvidence == null
            || !dProjectionEvidence.HasSameCaptureIdentity(fProjectionEvidence))
        {
            return false;
        }

        failure = P12DCityNpcRelationAssemblyFailure.InvalidReceiptOwnerEvidence;
        if (!dProjectionEvidence.AreReceiptOwnersStillExactZero()
            || !fProjectionEvidence.AreReceiptOwnersStillExactZero())
        {
            return false;
        }

        failure = P12DCityNpcRelationAssemblyFailure.InvalidCitySet;
        if (cityMembershipLinkers == null)
            return false;

        Dictionary<string, P12DCityMembershipLinker> linkersByCityId =
            new Dictionary<string, P12DCityMembershipLinker>(StringComparer.Ordinal);
        for (int i = 0; i < cityMembershipLinkers.Count; i++)
        {
            P12DCityMembershipLinker linker = cityMembershipLinkers[i];
            CityRuntime city = linker?.StagedCity;
            if (linker == null
                || linker.IsFilled
                || city == null
                || string.IsNullOrWhiteSpace(city.RuntimeId)
                || city.Location == null
                || string.IsNullOrWhiteSpace(city.Location.RuntimeId)
                || !linkersByCityId.TryAdd(city.RuntimeId, linker))
            {
                return false;
            }
        }

        failure = P12DCityNpcRelationAssemblyFailure.InvalidNpcRoster;
        if (stagedNpcRoster == null)
            return false;

        Dictionary<string, NpcRuntime> npcsByRuntimeId =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        for (int i = 0; i < stagedNpcRoster.Count; i++)
        {
            NpcRuntime npc = stagedNpcRoster[i];
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcsByRuntimeId.TryAdd(npc.RuntimeId, npc))
            {
                return false;
            }
        }

        if (!dProjectionEvidence.CoversStagedNpcRuntimeIds(npcsByRuntimeId))
        {
            failure = P12DCityNpcRelationAssemblyFailure.InvalidNpcRoster;
            return false;
        }

        failure = P12DCityNpcRelationAssemblyFailure.InvalidRelation;
        HashSet<string> globallyLinkedNpcIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<P12DCityMembershipLinker, List<NpcRuntime>> orderedMembersByLinker =
            new Dictionary<P12DCityMembershipLinker, List<NpcRuntime>>();
        foreach (KeyValuePair<string, P12DCityMembershipLinker> entry in linkersByCityId)
        {
            P12DCityMembershipLinker linker = entry.Value;
            CityRuntime city = linker.StagedCity;
            IReadOnlyList<string> expectedIds = linker.PendingNpcRuntimeIds;
            if (expectedIds == null)
                return false;

            List<NpcRuntime> orderedMembers = new List<NpcRuntime>(expectedIds.Count);
            HashSet<string> cityNpcIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < expectedIds.Count; i++)
            {
                string npcRuntimeId = expectedIds[i];
                if (string.IsNullOrWhiteSpace(npcRuntimeId)
                    || !cityNpcIds.Add(npcRuntimeId)
                    || !globallyLinkedNpcIds.Add(npcRuntimeId)
                    || !npcsByRuntimeId.TryGetValue(npcRuntimeId, out NpcRuntime npc)
                    || !ReferenceEquals(npc.CurrentCity, city)
                    || !ReferenceEquals(npc.CurrentLocation, city.Location))
                {
                    return false;
                }

                orderedMembers.Add(npc);
            }

            if (!linker.CanFillOnce(orderedMembers))
                return false;
            orderedMembersByLinker.Add(linker, orderedMembers);
        }

        foreach (NpcRuntime npc in stagedNpcRoster)
        {
            if (npc.CurrentCity == null)
            {
                if (globallyLinkedNpcIds.Contains(npc.RuntimeId))
                    return false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(npc.CurrentCity.RuntimeId)
                || !linkersByCityId.TryGetValue(npc.CurrentCity.RuntimeId, out P12DCityMembershipLinker ownerLinker)
                || !ReferenceEquals(ownerLinker.StagedCity, npc.CurrentCity)
                || !ReferenceEquals(npc.CurrentLocation, npc.CurrentCity.Location)
                || !globallyLinkedNpcIds.Contains(npc.RuntimeId))
            {
                return false;
            }
        }

        // All identity, order and reciprocal-link checks have succeeded. The
        // lists were reserved during City staging, and this commit path invokes
        // no gameplay mutation, owner callback, or revision increment.
        for (int i = 0; i < cityMembershipLinkers.Count; i++)
        {
            P12DCityMembershipLinker linker = cityMembershipLinkers[i];
            linker.FillValidated(orderedMembersByLinker[linker]);
        }

        failure = P12DCityNpcRelationAssemblyFailure.None;
        return true;
    }
}
