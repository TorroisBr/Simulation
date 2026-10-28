using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1RestoredOwnerVectorContractTests
{
    [Test]
    public void ImageCandidateAcceptsFreshOwnerInstancesWithTheSameLogicalCensus()
    {
        OwnerSectionCensusSnapshot[] source =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(P12ADailyV1Census.TryCreate(source, out P12ADailyV1Census image, out _), Is.True);

        OwnerSectionCensusSnapshot[] target = CloneWithFreshOwnerInstances(source);

        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            image, target, out P12GDailyV1OwnerVectorFailure failure, out string diagnostic), Is.True);
        Assert.That(failure, Is.EqualTo(P12GDailyV1OwnerVectorFailure.None));
        Assert.That(diagnostic, Is.Null);
        for (int i = 0; i < source.Length; i++)
            Assert.That(ReferenceEquals(source[i].OwnerInstanceIdentity,
                target[i].OwnerInstanceIdentity), Is.False, source[i].SectionId);
    }

    [Test]
    public void ImageCandidateRejectsChangedOwnerRevision()
    {
        OwnerSectionCensusSnapshot[] source =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(P12ADailyV1Census.TryCreate(source, out P12ADailyV1Census image, out _), Is.True);

        OwnerSectionCensusSnapshot[] target = CloneWithFreshOwnerInstances(source);
        OwnerSectionCensusSnapshot changed = target[0];
        target[0] = new OwnerSectionCensusSnapshot(changed.SectionId, changed.SchemaVersion,
            changed.Role, changed.OwnerInstanceIdentity, changed.Cardinality, changed.Revision + 1L);

        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            image, target, out P12GDailyV1OwnerVectorFailure failure, out string diagnostic), Is.False);
        Assert.That(failure, Is.Not.EqualTo(P12GDailyV1OwnerVectorFailure.None));
        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ImageCandidateRejectsChangedLogicalOwnerAliases()
    {
        OwnerSectionCensusSnapshot[] source =
            DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
        Assert.That(P12ADailyV1Census.TryCreate(source, out P12ADailyV1Census image, out _), Is.True);

        OwnerSectionCensusSnapshot[] target = CloneWithFreshOwnerInstances(source);
        int left = 0;
        int right = 1;
        while (right < source.Length && ReferenceEquals(source[left].OwnerInstanceIdentity,
                source[right].OwnerInstanceIdentity))
            right++;
        Assert.That(right, Is.LessThan(source.Length));
        target[right] = new OwnerSectionCensusSnapshot(target[right].SectionId,
            target[right].SchemaVersion, target[right].Role, target[left].OwnerInstanceIdentity,
            target[right].Cardinality, target[right].Revision);

        Assert.That(DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            image, target, out P12GDailyV1OwnerVectorFailure failure, out string diagnostic), Is.False);
        Assert.That(failure, Is.Not.EqualTo(P12GDailyV1OwnerVectorFailure.None));
        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
    }

    private static OwnerSectionCensusSnapshot[] CloneWithFreshOwnerInstances(
        IReadOnlyList<OwnerSectionCensusSnapshot> source)
    {
        Dictionary<object, object> identities = new Dictionary<object, object>();
        OwnerSectionCensusSnapshot[] clone = new OwnerSectionCensusSnapshot[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            OwnerSectionCensusSnapshot section = source[i];
            if (!identities.TryGetValue(section.OwnerInstanceIdentity, out object identity))
                identities.Add(section.OwnerInstanceIdentity, identity = new object());
            clone[i] = new OwnerSectionCensusSnapshot(section.SectionId, section.SchemaVersion,
                section.Role, identity, section.Cardinality, section.Revision);
        }
        return clone;
    }
}
