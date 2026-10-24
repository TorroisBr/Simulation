using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DRelationshipValidatorTests
{
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
}