using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// Source-backed fixture inputs for the CORE-F WP1-Owners selected-profile
/// migration baseline. These are IDs and counts, not a second owner registry.
/// </summary>
internal static class CoreFDailyV1OwnerClosureVectors
{
    internal const int FixedFamilyCount = 61;

    internal static readonly string[] ExpectedConditionalOperationIds =
    {
        "p12.estate.owner-commit",
        "p12.faction.owner-commit",
        "p12.institution-office.owner-commit",
        "p12.political-claim.owner-commit",
        "p12.political-support.owner-commit",
        "p12.property.owner-commit",
        "runtime.advance-day",
        "runtime.bootstrap-publication",
        "runtime.economy.market-purchase",
        "runtime.economy.market-sale",
        "runtime.economy.money-transfer",
        "runtime.economy.npc-trade",
        "runtime.merchant.advance-npc-trade-state",
        "runtime.npc-membership",
        "runtime.person.death",
        "runtime.person.parentage",
        "runtime.person.residence-bind",
        "runtime.population.emigration",
        "runtime.population.immigration",
        "runtime.population.residence-migration",
        "runtime.population.resident-death",
        "runtime.travel-party.advance",
        "runtime.travel-party.start",
        "runtime.travel.start"
    };

    internal static int ExpectedSectionCount(int npcCount, int personBoundNpcCount, int personCount, int cityCount)
    {
        if (npcCount < 0 || personBoundNpcCount < 0 || personBoundNpcCount > npcCount
            || personCount < 0 || cityCount < 0)
            throw new ArgumentOutOfRangeException("The owner formula requires nonnegative coherent roster counts.");

        int unboundNpcCount = npcCount - personBoundNpcCount;
        return FixedFamilyCount + (22 * npcCount) + unboundNpcCount + personCount + (4 * cityCount);
    }

    internal static IReadOnlyList<OwnerSectionCensusSnapshot> Capture(
        ContinuationCensusProtocol protocol,
        out P12ADailyV1Census census)
    {
        if (protocol == null) throw new ArgumentNullException(nameof(protocol));
        census = null;
        if (!protocol.TryCaptureQuiescentOwnerSectionSnapshot(
                out IReadOnlyList<OwnerSectionCensusSnapshot> snapshots,
                out _,
                out ContinuationCensusFailure failure))
            throw new InvalidOperationException("Selected owner boundary was not capturable: " + failure);

        if (!P12ADailyV1Census.TryCreate(snapshots, out census, out P12AResult result))
            throw new InvalidOperationException("P12 census rejected selected owner vector: " + result.Code);
        return snapshots;
    }

    internal static IReadOnlyCollection<string> ReadSealedOperationIds(ContinuationCensusProtocol protocol)
    {
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField(
            "expectedOperations", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(typeof(ContinuationCensusProtocol).FullName, "expectedOperations");
        var ids = field.GetValue(protocol) as HashSet<string>;
        if (ids == null) throw new InvalidOperationException("Sealed operation inventory was not a HashSet<string>.");
        return ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    internal static void AssertSameWitnesses(
        IReadOnlyList<OwnerSectionCensusSnapshot> expected,
        IReadOnlyList<OwnerSectionCensusSnapshot> actual)
    {
        if (expected == null || actual == null) throw new ArgumentNullException();
        Assert.That(actual.Count, Is.EqualTo(expected.Count));

        for (int i = 0; i < expected.Count; i++)
        {
            OwnerSectionCensusSnapshot left = expected[i];
            OwnerSectionCensusSnapshot right = actual[i];
            Assert.That(right.SectionId, Is.EqualTo(left.SectionId));
            Assert.That(right.SchemaVersion, Is.EqualTo(left.SchemaVersion), left.SectionId);
            Assert.That(right.Role, Is.EqualTo(left.Role), left.SectionId);
            Assert.That(right.Cardinality, Is.EqualTo(left.Cardinality), left.SectionId);
            Assert.That(right.Revision, Is.EqualTo(left.Revision), left.SectionId);
            Assert.That(right.OwnerInstanceIdentity, Is.SameAs(left.OwnerInstanceIdentity), left.SectionId);
        }
    }

    internal static void AssertAliasGroupsMatchReferences(
        IReadOnlyList<OwnerSectionCensusSnapshot> snapshots,
        P12ADailyV1Census census)
    {
        if (snapshots == null || census == null) throw new ArgumentNullException();
        Assert.That(census.Sections.Count, Is.EqualTo(snapshots.Count));
        for (int left = 0; left < snapshots.Count; left++)
        {
            P12ACensusSectionValue leftValue = census.Sections[left];
            Assert.That(leftValue.Id, Is.EqualTo(snapshots[left].SectionId));
            for (int right = left + 1; right < snapshots.Count; right++)
            {
                bool sameOwner = ReferenceEquals(
                    snapshots[left].OwnerInstanceIdentity,
                    snapshots[right].OwnerInstanceIdentity);
                bool sameAlias = census.Sections[left].AliasGroup != null
                    && census.Sections[left].AliasGroup == census.Sections[right].AliasGroup;
                Assert.That(sameAlias, Is.EqualTo(sameOwner),
                    snapshots[left].SectionId + " / " + snapshots[right].SectionId);
            }
        }
    }
}
