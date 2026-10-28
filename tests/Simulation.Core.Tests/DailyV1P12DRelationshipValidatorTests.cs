using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DRelationshipValidatorTests
{
    [Test]
    public void ValidatesDecodedImageOwnersBeforeHostProjection()
    {
        PersonStoreOwnerSnapshot persons = new PersonStoreOwnerSnapshot(
            PersonStoreOwnerSnapshot.CurrentSchemaId,
            PersonStoreOwnerSnapshot.CurrentSchemaVersion,
            1L, 1, 1,
            new[] { new PersonStoreOwnerSnapshotRow(
                "person-a", null, null, "city-a", "npc-a", 1L) });
        GenealogyOwnerSnapshot genealogy = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion, 0L, Array.Empty<ParentageRecord>());
        DailyV1P12DCityCoreOwnerHydrationInput city = new DailyV1P12DCityCoreOwnerHydrationInput(
            "city-a", "city-definition-a", "location-a", "location-a", 1L,
            new[] { "npc-a" }, "city-a", MarketLiquidityMode.Open, false, 0L,
            Array.Empty<P12DCityMarketItemSnapshot>(), "population-a",
            ConsumptionPaymentMode.Free, false, "population-a", 1, 0L, 0L,
            Array.Empty<PopulationOperationReceiptSnapshot>());
        P12DNpcDRow npc = CreateNpcRow("location-a");
        SpatialNetworkOwnerSnapshot spatial = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 1L,
            new[] { "location-a" }, Array.Empty<SpatialRouteOwnerSnapshotRecord>());

        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            persons, genealogy, new[] { city }, new[] { npc }, spatial), Is.True);

        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            persons, genealogy, new[] { city }, new[] { CreateNpcRow("location-other") }, spatial), Is.False);
    }

    [Test]
    public void AcceptsConsistentDetachedPersonGenealogyCityAndNpcLinks()
    {
        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new[]
            {
                new DailyV1P12DPersonRelation("person-a", "city-a", "npc-a"),
                new DailyV1P12DPersonRelation("person-b", null, null)
            },
            new[] { new DailyV1P12DParentageRelation("person-a", "person-b") },
            new[] { new DailyV1P12DCityRelation("city-a", "location-a", new[] { "npc-a" }) },
            new[]
            {
                new DailyV1P12DNpcRelation(
                    "npc-a", "person-a", "city-a", "city-a", "location-a", null, null)
            },
            new[] { "location-a" },
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["city-a"] = "location-a"
            }), Is.True);
    }

    [Test]
    public void RejectsPersonAndNpcMaterializationMismatch()
    {
        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new[] { new DailyV1P12DPersonRelation("person-a", "city-a", "npc-a") },
            new DailyV1P12DParentageRelation[0],
            new[] { new DailyV1P12DCityRelation("city-a", "location-a", new[] { "npc-a" }) },
            new[]
            {
                new DailyV1P12DNpcRelation(
                    "npc-a", "person-other", "city-a", "city-a", "location-a", null, null)
            },
            new[] { "location-a" },
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["city-a"] = "location-a"
            }), Is.False);
    }

    [Test]
    public void RejectsUnresolvedGenealogyPerson()
    {
        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new[] { new DailyV1P12DPersonRelation("person-a", null, null) },
            new[] { new DailyV1P12DParentageRelation("person-a", "missing-person") },
            new DailyV1P12DCityRelation[0],
            new DailyV1P12DNpcRelation[0],
            new string[0],
            new System.Collections.Generic.Dictionary<string, string>()), Is.False);
    }

    [Test]
    public void RejectsNpcCityLocationMismatch()
    {
        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new DailyV1P12DPersonRelation[0],
            new DailyV1P12DParentageRelation[0],
            new DailyV1P12DCityRelation[0],
            new[]
            {
                new DailyV1P12DNpcRelation(
                    "npc-a", null, null, "city-a", "location-wrong", null, null)
            },
            new[] { "location-a", "location-wrong" },
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["city-a"] = "location-a"
            }), Is.False);
    }

    [Test]
    public void RejectsMissingCityMembershipAndDuplicateMembership()
    {
        DailyV1P12DPersonRelation[] persons = { new DailyV1P12DPersonRelation("person-a", "city-a", "npc-a") };
        DailyV1P12DNpcRelation[] npcs =
        {
            new DailyV1P12DNpcRelation("npc-a", "person-a", "city-a", "city-a", "location-a", null, null)
        };
        System.Collections.Generic.Dictionary<string, string> cityLocations =
            new System.Collections.Generic.Dictionary<string, string> { ["city-a"] = "location-a" };

        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            persons, new DailyV1P12DParentageRelation[0],
            new[] { new DailyV1P12DCityRelation("city-a", "location-a", new string[0]) },
            npcs, new[] { "location-a" }, cityLocations), Is.False);

        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            persons, new DailyV1P12DParentageRelation[0],
            new[] { new DailyV1P12DCityRelation("city-a", "location-a", new[] { "npc-a", "npc-a" }) },
            npcs, new[] { "location-a" }, cityLocations), Is.False);
    }

    [Test]
    public void RejectsDuplicatePersonAndNpcIdentities()
    {
        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new[]
            {
                new DailyV1P12DPersonRelation("person-a", null, null),
                new DailyV1P12DPersonRelation("person-a", null, null)
            },
            new DailyV1P12DParentageRelation[0], new DailyV1P12DCityRelation[0],
            new DailyV1P12DNpcRelation[0], new string[0],
            new System.Collections.Generic.Dictionary<string, string>()), Is.False);

        Assert.That(DailyV1P12DRelationshipValidator.TryValidate(
            new DailyV1P12DPersonRelation[0], new DailyV1P12DParentageRelation[0],
            new DailyV1P12DCityRelation[0],
            new[]
            {
                new DailyV1P12DNpcRelation("npc-a", null, null, null, null, null, null),
                new DailyV1P12DNpcRelation("npc-a", null, null, null, null, null, null)
            },
            new string[0], new System.Collections.Generic.Dictionary<string, string>()), Is.False);
    }

    private static P12DNpcDRow CreateNpcRow(string currentLocationId) =>
        new P12DNpcDRow("npc-a", "npc-definition-a", "person-a", "city-a",
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            null, "city-a", currentLocationId, null, null,
            Array.Empty<P12DNpcInventoryRowValue>(), 0L, 0f, 0L, 0L, 0L, 0L, 0L);
}
