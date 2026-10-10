using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12GDailyV1OwnerVectorTests
{
    [Test]
    public void RestoredCandidateVectorAcceptsExactRowsIndependentOfRegistrationOrder()
    {
        object sourceRequiredOwner = new object();
        object sourceEmptyOwner = new object();
        object sourceExcludedOwner = new object();
        OwnerSectionCensusSnapshot[] source =
        {
            Row("required", OwnerSectionRole.Required, sourceRequiredOwner, 2, 7L),
            Row("empty", OwnerSectionRole.ExplicitlyEmpty, sourceEmptyOwner, 0, 0L),
            Row("excluded", OwnerSectionRole.Excluded, sourceExcludedOwner, 0, 3L)
        };
        OwnerSectionCensusSnapshot[] target =
        {
            Row("excluded", OwnerSectionRole.Excluded, new object(), 0, 3L),
            Row("required", OwnerSectionRole.Required, new object(), 2, 7L),
            Row("empty", OwnerSectionRole.ExplicitlyEmpty, new object(), 0, 0L)
        };

        AssertValid(source, target);
        Assert.That(source[0].OwnerInstanceIdentity, Is.SameAs(sourceRequiredOwner));
        Assert.That(source[1].OwnerInstanceIdentity, Is.SameAs(sourceEmptyOwner));
        Assert.That(target[0].SectionId, Is.EqualTo("excluded"));
    }

    [Test]
    public void RestoredCandidateVectorRejectsMissingVectorsAndCountMismatch()
    {
        OwnerSectionCensusSnapshot[] one = { Row("a", OwnerSectionRole.Required, new object(), 0, 0L) };
        AssertFailure(null, one, P12GDailyV1OwnerVectorFailure.MissingSourceVector);
        AssertFailure(one, null, P12GDailyV1OwnerVectorFailure.MissingTargetVector);
        AssertFailure(one, new OwnerSectionCensusSnapshot[0], P12GDailyV1OwnerVectorFailure.SectionCountMismatch);
    }

    [Test]
    public void RestoredCandidateVectorRejectsMissingBlankAndDuplicateSectionIdentities()
    {
        OwnerSectionCensusSnapshot[] source =
        {
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L),
            Row("b", OwnerSectionRole.Required, new object(), 0, 0L)
        };

        AssertFailure(source, new[]
        {
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L),
            Row("c", OwnerSectionRole.Required, new object(), 0, 0L)
        }, P12GDailyV1OwnerVectorFailure.MissingSection);
        AssertFailure(source, new[]
        {
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L),
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L)
        }, P12GDailyV1OwnerVectorFailure.DuplicateSectionIdentity);
        AssertFailure(new[]
        {
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L),
            Row("a", OwnerSectionRole.Required, new object(), 0, 0L)
        }, source, P12GDailyV1OwnerVectorFailure.DuplicateSectionIdentity);
        AssertFailure(new[]
        {
            Row(" ", OwnerSectionRole.Required, new object(), 0, 0L)
        }, new[]
        {
            Row(" ", OwnerSectionRole.Required, new object(), 0, 0L)
        }, P12GDailyV1OwnerVectorFailure.DuplicateSectionIdentity);
    }

    [Test]
    public void RestoredCandidateVectorRejectsContractCardinalityRevisionAndIdentityDrift()
    {
        OwnerSectionCensusSnapshot[] source = { Row("a", OwnerSectionRole.Required, new object(), 2, 4L) };
        AssertFailure(source,
            new[] { new OwnerSectionCensusSnapshot("a", 2, OwnerSectionRole.Required, new object(), 2, 4L) },
            P12GDailyV1OwnerVectorFailure.ContractMismatch);
        AssertFailure(source,
            new[] { Row("a", OwnerSectionRole.Required, new object(), 3, 4L) },
            P12GDailyV1OwnerVectorFailure.CardinalityMismatch);
        AssertFailure(source,
            new[] { Row("a", OwnerSectionRole.Required, new object(), 2, 5L) },
            P12GDailyV1OwnerVectorFailure.RevisionMismatch);
        AssertFailure(source,
            new[] { Row("a", OwnerSectionRole.Required, source[0].OwnerInstanceIdentity, 2, 4L) },
            P12GDailyV1OwnerVectorFailure.ReusedSourceOwnerIdentity);
    }

    [Test]
    public void RestoredCandidateVectorRejectsPopulatedEmptyOrExcludedRows()
    {
        OwnerSectionCensusSnapshot[] explicitEmpty =
        {
            Row("empty", OwnerSectionRole.ExplicitlyEmpty, new object(), 1, 2L)
        };
        OwnerSectionCensusSnapshot[] excluded =
        {
            Row("excluded", OwnerSectionRole.Excluded, new object(), 1, 2L)
        };

        AssertFailure(explicitEmpty,
            new[] { Row("empty", OwnerSectionRole.ExplicitlyEmpty, new object(), 1, 2L) },
            P12GDailyV1OwnerVectorFailure.InvalidEmptyDisposition);
        AssertFailure(excluded,
            new[] { Row("excluded", OwnerSectionRole.Excluded, new object(), 1, 2L) },
            P12GDailyV1OwnerVectorFailure.InvalidEmptyDisposition);
    }

    [Test]
    public void RestoredCandidateVectorPreservesOwnerAliasRelationships()
    {
        object sourceShared = new object();
        OwnerSectionCensusSnapshot[] source =
        {
            Row("allocator.event", OwnerSectionRole.Required, sourceShared, 0, 7L),
            Row("allocator.decision", OwnerSectionRole.Required, sourceShared, 0, 4L)
        };
        OwnerSectionCensusSnapshot[] targetWithSplitOwners =
        {
            Row("allocator.event", OwnerSectionRole.Required, new object(), 0, 7L),
            Row("allocator.decision", OwnerSectionRole.Required, new object(), 0, 4L)
        };
        AssertFailure(source, targetWithSplitOwners,
            P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch);

        OwnerSectionCensusSnapshot[] sourceWithSeparateOwners =
        {
            Row("owner.a", OwnerSectionRole.Required, new object(), 0, 0L),
            Row("owner.b", OwnerSectionRole.Required, new object(), 0, 0L)
        };
        object targetShared = new object();
        OwnerSectionCensusSnapshot[] targetWithMergedOwners =
        {
            Row("owner.a", OwnerSectionRole.Required, targetShared, 0, 0L),
            Row("owner.b", OwnerSectionRole.Required, targetShared, 0, 0L)
        };
        AssertFailure(sourceWithSeparateOwners, targetWithMergedOwners,
            P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch);
    }

    [Test]
    public void RestoredAdmissionSnapshotMustRemainIdenticalThroughFinalAdmission()
    {
        object owner = new object();
        OwnerSectionCensusSnapshot[] validated =
        {
            Row("owner.a", OwnerSectionRole.Required, owner, 2, 5L)
        };
        OwnerSectionCensusSnapshot[] sameOwners =
        {
            Row("owner.a", OwnerSectionRole.Required, owner, 2, 5L)
        };
        Assert.That(P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
            validated, 9L, sameOwners, 9L,
            out P12GDailyV1OwnerVectorFailure failure,
            out string diagnostic), Is.True, diagnostic ?? failure.ToString());

        OwnerSectionCensusSnapshot[] changedOwner =
        {
            Row("owner.a", OwnerSectionRole.Required, new object(), 2, 5L)
        };
        Assert.That(P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
            validated, 9L, changedOwner, 9L,
            out failure, out diagnostic), Is.False, diagnostic);
        Assert.That(failure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.OwnerVectorChanged));

        Assert.That(P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
            validated, 9L, sameOwners, 10L,
            out failure, out diagnostic), Is.False, diagnostic);
        Assert.That(failure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.MutationEpochChanged));
    }

    [Test]
    public void AllocatorHighWaterMustContinuePastEveryRetainedTypedIdentity()
    {
        RuntimeIdAllocatorSnapshot allocator = Allocator(
            new RuntimeIdAllocatorCounterSnapshot("npc", 5L),
            new RuntimeIdAllocatorCounterSnapshot("city", 3L),
            new RuntimeIdAllocatorCounterSnapshot("event", 2L),
            new RuntimeIdAllocatorCounterSnapshot("directive", 4L),
            new RuntimeIdAllocatorCounterSnapshot("decision", 3L));
        KeyValuePair<string, string>[] identities =
        {
            new KeyValuePair<string, string>("npc", "npc-000004"),
            new KeyValuePair<string, string>("city", "city-000002"),
            new KeyValuePair<string, string>("event", "event-000001"),
            new KeyValuePair<string, string>("directive", "directive-000003"),
            new KeyValuePair<string, string>("decision", "decision-000002")
        };

        Assert.That(P12GDailyV1RestoreCoordinator.TryValidateAllocatorHighWater(
            allocator, identities, out string diagnostic), Is.True, diagnostic);
    }

    [Test]
    public void AllocatorHighWaterChecksCanonicalOpaqueDecisionReferencesWithoutResolvingOrDeduplicatingThem()
    {
        RuntimeIdAllocatorSnapshot allocator = Allocator(
            new RuntimeIdAllocatorCounterSnapshot("decision", 3L));
        string[] references =
        {
            "decision-000002",
            "decision-000002",
            "opaque-decision-reference",
            "decision-not-numeric",
            "decision-2"
        };

        Assert.That(P12GDailyV1RestoreCoordinator.TryValidateAllocatorHighWater(
            allocator, System.Array.Empty<KeyValuePair<string, string>>(), references,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(P12GDailyV1RestoreCoordinator.TryValidateAllocatorHighWater(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("decision", 2L)),
            System.Array.Empty<KeyValuePair<string, string>>(), references,
            out diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("decision-000002"));
    }

    [Test]
    public void AllocatorHighWaterRejectsStaleCountersAndMalformedOrUnownedIdentityValues()
    {
        AssertAllocatorHighWaterFailure(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("npc", 4L)),
            new KeyValuePair<string, string>("npc", "npc-000004"));
        AssertAllocatorHighWaterFailure(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("npc", 5L)),
            new KeyValuePair<string, string>("npc", "city-000004"));
        AssertAllocatorHighWaterFailure(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("npc", 5L)),
            new KeyValuePair<string, string>("city", "city-000004"));
        AssertAllocatorHighWaterFailure(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("npc", 5L)),
            new KeyValuePair<string, string>("npc", "npc-four"));
        AssertAllocatorHighWaterFailure(
            Allocator(new RuntimeIdAllocatorCounterSnapshot("npc", 5L)),
            new KeyValuePair<string, string>("npc", "npc-000004"),
            new KeyValuePair<string, string>("npc", "npc-000004"));
    }

    private static RuntimeIdAllocatorSnapshot Allocator(params RuntimeIdAllocatorCounterSnapshot[] counters)
    {
        return new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            counters);
    }

    private static void AssertAllocatorHighWaterFailure(
        RuntimeIdAllocatorSnapshot allocator,
        params KeyValuePair<string, string>[] identities)
    {
        Assert.That(P12GDailyV1RestoreCoordinator.TryValidateAllocatorHighWater(
            allocator, identities, out string diagnostic), Is.False, diagnostic);
    }

    private static OwnerSectionCensusSnapshot Row(
        string sectionId,
        OwnerSectionRole role,
        object owner,
        int cardinality,
        long revision)
    {
        return new OwnerSectionCensusSnapshot(sectionId, 1, role, owner, cardinality, revision);
    }

    private static void AssertValid(
        IReadOnlyList<OwnerSectionCensusSnapshot> source,
        IReadOnlyList<OwnerSectionCensusSnapshot> target)
    {
        Assert.That(P12GDailyV1OwnerVector.TryValidateRestoredCandidate(
            source, target,
            out P12GDailyV1OwnerVectorFailure failure,
            out string diagnostic), Is.True, diagnostic ?? failure.ToString());
        Assert.That(failure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.None));
    }

    private static void AssertFailure(
        IReadOnlyList<OwnerSectionCensusSnapshot> source,
        IReadOnlyList<OwnerSectionCensusSnapshot> target,
        P12GDailyV1OwnerVectorFailure expected)
    {
        Assert.That(P12GDailyV1OwnerVector.TryValidateRestoredCandidate(
            source, target,
            out P12GDailyV1OwnerVectorFailure failure,
            out string diagnostic), Is.False, diagnostic);
        Assert.That(failure, Is.EqualTo(expected));
    }
}
