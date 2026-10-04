using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

public sealed class RuinLocalTopologyGenerationTests
{
    [Test]
    public void SameRequestProducesIdenticalStableGraphAndFingerprints()
    {
        var request = new RuinLocalTopologyGeneration.Request("p9-profile-fingerprint", 412L, "north-ruin", "ruin-definition", "location-canonical");
        var first = RuinLocalTopologyGeneration.Generate(request);
        var second = RuinLocalTopologyGeneration.Generate(new RuinLocalTopologyGeneration.Request("p9-profile-fingerprint", 412L, "north-ruin", "ruin-definition", "location-canonical"));

        Assert.That(first.Request.RequestFingerprint, Is.EqualTo(second.Request.RequestFingerprint));
        Assert.That(first.GraphFingerprint, Is.EqualTo(second.GraphFingerprint));
        Assert.That(first.ComposedProfileFingerprint, Is.EqualTo(second.ComposedProfileFingerprint));
        Assert.That(first.Places.Select(x => x.RuntimeId), Is.EqualTo(second.Places.Select(x => x.RuntimeId)));
        Assert.That(first.Connections.Select(x => x.RuntimeId), Is.EqualTo(second.Connections.Select(x => x.RuntimeId)));
        Assert.That(first.Connections.Select(x => x.TraversalCost), Is.EqualTo(second.Connections.Select(x => x.TraversalCost)));
    }

    [Test]
    public void FixedInputMatrixProducesBoundedReachableDistinctShapes()
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        for (int seed = 0; seed < 32; seed++)
        {
            var request = new RuinLocalTopologyGeneration.Request("p9-profile-fingerprint", seed, "north-ruin", "ruin-definition", "location-canonical");
            var topology = RuinLocalTopologyGeneration.Generate(request);
            Assert.That(topology.Places.Count, Is.InRange(6, 10));
            Assert.That(topology.Connections.Count, Is.InRange(5, 19));
            Assert.That(topology.Places.Count(x => x.IsEntry), Is.EqualTo(1));
            Assert.That(topology.Connections.All(x => x.TraversalCost >= 1 && x.TraversalCost <= 3), Is.True);
            fingerprints.Add(topology.GraphFingerprint);
        }
        Assert.That(fingerprints.Count, Is.GreaterThan(1));
    }

    [Test]
    public void SiteAndMemberIdentitiesBindStableKeyDefinitionAndLocation()
    {
        string first = RuinLocalTopologyGeneration.DeriveSiteInstanceId("site-a", "ruin", "location-a");
        Assert.That(first, Is.Not.EqualTo(RuinLocalTopologyGeneration.DeriveSiteInstanceId("site-b", "ruin", "location-a")));
        Assert.That(first, Is.Not.EqualTo(RuinLocalTopologyGeneration.DeriveSiteInstanceId("site-a", "ruin", "location-b")));
        Assert.That(first, Is.Not.EqualTo("ruin"));
        Assert.That(first, Is.Not.EqualTo("location-a"));
    }

    [Test]
    public void UnsupportedVersionsAndInvalidUnicodeFailClosed()
    {
        Assert.Throws<ArgumentException>(() => new RuinLocalTopologyGeneration.Request("p9", 0, "key", "def", "loc", "unknown"));
        Assert.Throws<EncoderFallbackException>(() => new RuinLocalTopologyGeneration.Request("p9", 0, "bad-\ud800", "def", "loc"));
    }
}
