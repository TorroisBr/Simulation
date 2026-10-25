using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DNpcCoreOwnerHydratorTests
{
    [Test]
    public void P12DRowsHydrateSelectedNpcOwnersFromCanonicalDefinitionsWithExactRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        DailyV1StatusDefinition status = definitions.Statuses.Values.First();
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        const string runtimeId = "npc/p12d-core-hydration";
        const string locationId = "location/p12d-core-hydration";

        DailyV1P12DNpcCoreOwnerHydrationInput input = CreateInput(
            CreateDRow(npc.DefinitionId, runtimeId,
                new[] { status.DefinitionId },
                new[] { new P12DNpcInventoryRowValue(item.DefinitionId, 5, 2.25f) },
                action.DefinitionId),
            action.DefinitionId,
            item.DefinitionId,
            locationId,
            new[] { new P12DNpcCommercialMarketValue(locationId, item.DefinitionId,
                3.5f, 7, 4L, 5L, CommercialKnowledgeSource.DirectObservation, null) },
            siteObservations, 45L, oppositionObservations,
            notableItemObservations, commonResourceObservations,
            accessObservations, 46L);

        Assert.That(DailyV1P12DNpcCoreOwnerHydrator.TryCreate(
            input, definitions, Array.Empty<string>(), new[] { locationId },
            Array.Empty<string>(), Array.Empty<string>(), new[] { runtimeId },
            out DailyV1NpcRuntimeOwners owners,
            out DailyV1P12DNpcCoreOwnerHydrationFailure failure), Is.True, failure.ToString());

        Assert.That(owners.ExplorableSiteKnowledge.Revision, Is.EqualTo(45L));
        Assert.That(owners.ExplorableSiteKnowledge.Observations.Single().SiteRuntimeId,
            Is.EqualTo("site/p12d-core-hydration"));
        Assert.That(owners.ExplorableSiteKnowledge.Observations.Single().LocationRuntimeId,
            Is.EqualTo(locationId));
        Assert.That(owners.AdventureSiteIntelKnowledge.Revision, Is.EqualTo(46L));
        Assert.That(owners.AdventureSiteIntelKnowledge.OppositionObservations.Single().ObservedState,
            Is.EqualTo(AdventureOppositionObservedState.Resolved));
        Assert.That(owners.AdventureSiteIntelKnowledge.NotableItemObservations.Single().ItemDefinitionId,
            Is.EqualTo("item-definition/p12d"));
        Assert.That(owners.AdventureSiteIntelKnowledge.CommonResourceObservations.Single().ObservedAmount,
            Is.EqualTo(7));
        Assert.That(owners.AdventureSiteIntelKnowledge.AccessObservations.Single().ObservedAccessState,
            Is.EqualTo(PlaceAccessState.Contested));

        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(npc));
        Assert.That(owners.Status.Statuses.Single(), Is.SameAs(status));
        Assert.That(owners.Inventory.Items.Single().Item, Is.SameAs(item));
        Assert.That(owners.Inventory.Items.Single().Amount, Is.EqualTo(5));
        Assert.That(owners.Inventory.Items.Single().AverageUnitCost, Is.EqualTo(2.25f));
        Assert.That(owners.Inventory.Revision, Is.EqualTo(31L));
        Assert.That(owners.MoneyAccount.Balance, Is.EqualTo(17.5f));
        Assert.That(owners.MoneyAccount.Revision, Is.EqualTo(32L));
        Assert.That(owners.Lifecycle.LifeStateRevision, Is.EqualTo(33L));
        Assert.That(owners.CrimeJusticeRevision, Is.EqualTo(34L));
        Assert.That(owners.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(owners.ActionSlot.CurrentPayload.TargetItem, Is.SameAs(item));
        Assert.That(owners.ActionSlot.Revision, Is.EqualTo(35L));
        Assert.That(owners.Travel.Revision, Is.EqualTo(36L));
        Assert.That(owners.TravelPlan.Revision, Is.EqualTo(37L));
        Assert.That(owners.CommercialKnowledge.Revision, Is.EqualTo(38L));
        Assert.That(owners.CommercialKnowledge.Observations.Single().ItemDefinition, Is.SameAs(item));
        Assert.That(owners.SpatialKnowledge.Revision, Is.EqualTo(39L));
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.OwnerRuntimeId, Is.EqualTo(runtimeId));
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.PlaceObservationCount, Is.Zero);
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.ConnectionObservationCount, Is.Zero);
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.Revision, Is.Zero);
    }

    [TestCase("unknown-status", (int)DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidStatusDefinition)]
    [TestCase("unknown-item", (int)DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidInventoryItem)]
    public void P12DCoreHydratorRejectsUnresolvedStatusAndInventoryIdentityWithoutOwners(
        string invalidIdentity, int expectedFailureValue)
    {
        DailyV1P12DNpcCoreOwnerHydrationFailure expectedFailure =
            (DailyV1P12DNpcCoreOwnerHydrationFailure)expectedFailureValue;
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        P12DNpcDRow d = expectedFailure == DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidStatusDefinition
            ? CreateDRow(npc.DefinitionId, "npc/p12d-invalid", new[] { invalidIdentity },
                Array.Empty<P12DNpcInventoryRowValue>(), null)
            : CreateDRow(npc.DefinitionId, "npc/p12d-invalid", Array.Empty<string>(),
                new[] { new P12DNpcInventoryRowValue(invalidIdentity, 1, 1f) }, null);
        DailyV1P12DNpcCoreOwnerHydrationInput input = CreateInput(d, null, null, null,
            Array.Empty<P12DNpcCommercialMarketValue>());

        Assert.That(DailyV1P12DNpcCoreOwnerHydrator.TryCreate(
            input, definitions, Array.Empty<string>(), new[] { "location/p12d-invalid" },
            Array.Empty<string>(), Array.Empty<string>(), new[] { "npc/p12d-invalid" },
            out DailyV1NpcRuntimeOwners owners, out DailyV1P12DNpcCoreOwnerHydrationFailure failure), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(expectedFailure));
    }

    [Test]
    public void P12DCoreHydratorRejectsDanglingCommercialLocationBeforePublishingOwner()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        DailyV1P12DNpcCoreOwnerHydrationInput input = CreateInput(
            CreateDRow(npc.DefinitionId, "npc/p12d-dangling", Array.Empty<string>(),
                Array.Empty<P12DNpcInventoryRowValue>(), null),
            null, null, null,
            new[] { new P12DNpcCommercialMarketValue("location/missing", item.DefinitionId,
                1f, 1, 0L, 0L, CommercialKnowledgeSource.DirectObservation, null) });

        Assert.That(DailyV1P12DNpcCoreOwnerHydrator.TryCreate(
            input, definitions, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), Array.Empty<string>(), new[] { "npc/p12d-dangling" },
            out DailyV1NpcRuntimeOwners owners,
            out DailyV1P12DNpcCoreOwnerHydrationFailure failure), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidReference));
    }

    private static P12DNpcDRow CreateDRow(
        string definitionId,
        string runtimeId,
        string[] statuses,
        P12DNpcInventoryRowValue[] inventory,
        string actionDefinitionId) =>
        new P12DNpcDRow(runtimeId, definitionId, null, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, statuses, 0,
            actionDefinitionId, null, null, null, null, inventory,
            31L, 17.5f, 32L, 33L, 0L, 34L, 35L);

    private static readonly ExplorableSiteKnowledgeObservation[] siteObservations =
    {
        new ExplorableSiteKnowledgeObservation("site/p12d-core-hydration",
            "location/p12d-core-hydration", 2L, 3L,
            ExplorableSiteKnowledgeSource.DirectObservation)
    };
    private static readonly AdventureOppositionObservation[] oppositionObservations =
    {
        new AdventureOppositionObservation("site/p12d-core-hydration", null,
            "opposition/p12d", AdventureOppositionObservedState.Resolved, 3L, 4L,
            AdventureIntelSource.DirectObservation)
    };
    private static readonly AdventureNotableItemObservation[] notableItemObservations =
    {
        new AdventureNotableItemObservation("site/p12d-core-hydration", null,
            "item/p12d", "item-definition/p12d", 4L, 5L,
            AdventureIntelSource.DirectObservation)
    };
    private static readonly AdventureCommonResourceObservation[] commonResourceObservations =
    {
        new AdventureCommonResourceObservation("site/p12d-core-hydration", null,
            "item-definition/p12d", 7, "resource/p12d", 5L, 6L,
            AdventureIntelSource.DirectObservation)
    };
    private static readonly AdventureAccessObservation[] accessObservations =
    {
        new AdventureAccessObservation("site/p12d-core-hydration", null,
            PlaceAccessState.Contested, 6L, 7L, AdventureIntelSource.DirectObservation)
    };

    private static DailyV1P12DNpcCoreOwnerHydrationInput CreateInput(
        P12DNpcDRow d,
        string actionDefinitionId,
        string targetItemDefinitionId,
        string commercialLocationId,
        P12DNpcCommercialMarketValue[] markets,
        IReadOnlyList<ExplorableSiteKnowledgeObservation> siteObservations = null,
        long siteRevision = 0L,
        IReadOnlyList<AdventureOppositionObservation> oppositionObservations = null,
        IReadOnlyList<AdventureNotableItemObservation> notableItemObservations = null,
        IReadOnlyList<AdventureCommonResourceObservation> commonResourceObservations = null,
        IReadOnlyList<AdventureAccessObservation> accessObservations = null,
        long adventureRevision = 0L)
    {
        P12DNpcActionValue action = actionDefinitionId == null ? null
            : new P12DNpcActionValue(actionDefinitionId, null, null, targetItemDefinitionId,
                1, 2f, 1f, NpcTravelReason.None, 0f, null, null, null, null);
        return new DailyV1P12DNpcCoreOwnerHydrationInput(
            d, action, 0, 0, null, false, null, null, 36L,
            new P12DNpcMerchantPlanValue(null, null, null, 0, 0, 0f, 0, 0, null, 0L),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, 37L),
            string.IsNullOrEmpty(commercialLocationId) ? Array.Empty<string>() : new[] { commercialLocationId },
            Array.Empty<string>(), 39L, markets, Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<DailyV1CommercialKnowledgeShareReceipt>(), 38L,
            siteObservations ?? Array.Empty<ExplorableSiteKnowledgeObservation>(), siteRevision,
            oppositionObservations ?? Array.Empty<AdventureOppositionObservation>(),
            notableItemObservations ?? Array.Empty<AdventureNotableItemObservation>(),
            commonResourceObservations ?? Array.Empty<AdventureCommonResourceObservation>(),
            accessObservations ?? Array.Empty<AdventureAccessObservation>(), adventureRevision);
    }
}
