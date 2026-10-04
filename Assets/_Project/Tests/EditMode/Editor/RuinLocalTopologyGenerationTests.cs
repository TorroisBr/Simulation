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
    public void VersionOneMatchesPublishedRequestAndCompleteGraphGoldenVectors()
    {
        AssertGoldenVector(
            412L,
            "site-p10b-v1-53a425eb981146cb053f342880b803c233c4bfde91aafce61d31ead6b827200e",
            "065f7620c2876cb061e573f087f2cd0543db124ad8561651499c8f2a1cf2f084",
            "3bef16581f201f7ec2971c120395d255d834e9d92f715fe417542b43bc77eaea",
            "f636a808388cdc22e63ef36aeed6bb65c3b36eeb88cd5eb492a965620f533232",
            new[]
            {
                "place/000000|place-p10b-v1-5f2903d9dbb1f01b9a4811fd921cea129afed6902fec4c204e8dc46595d8d554|-|1",
                "place/000001|place-p10b-v1-7a0622d322df7a5b1167502ef6fad9fd6085c476b3ef45bddf2d2df3d5c4186a|-|0",
                "place/000002|place-p10b-v1-79b34724944cf1f170e0c98b771aa381b06eaaa237ed4d5c78984e3758cdde68|place/000001|0",
                "place/000003|place-p10b-v1-112a76395a45fdc8582cc32bb024dfc907645f926bd221974a257709c74dd614|-|0",
                "place/000004|place-p10b-v1-6e9ac17d40e42df30b14724b3e397b8279a4aaebd2e9fcece8300762dfbd3461|place/000002|0",
                "place/000005|place-p10b-v1-c71cc0749250354c55a44cfe1b9f518326a95380457aa05bf5e2f3fb7899c81f|-|0",
                "place/000006|place-p10b-v1-40e018d8095cda20487ad1dd6869afc36a85e9c118040e754defb7afc9c080be|-|0",
                "place/000007|place-p10b-v1-9705958bba4ae6ef305c04e74fbdfaf6337aacf3035a3f2e95eb815f85e897c4|-|0"
            },
            new[]
            {
                "edge/0/1|connection-p10b-v1-4764406e54b450fb144f30b2cc49bf2d550b8ce373cb8f3273e549fadb0abd14|place/000000|place/000001|3",
                "edge/1/2|connection-p10b-v1-a1c68589a60583edc9c1c105f8ebba0ced8be4bc54b0bfbe4c3f1882c3b3c992|place/000001|place/000002|3",
                "edge/1/6|connection-p10b-v1-4173acd73c42eb954caadcee6b41709d2d9efdbf8e750d163bf879763d8331d8|place/000001|place/000006|2",
                "edge/2/3|connection-p10b-v1-b6f9ef5e1a0eb3f898d15ebc9c62ce806016263bef2a53474c95ef5b30e7608d|place/000002|place/000003|2",
                "edge/2/4|connection-p10b-v1-589e426de203d84134ee0c16a1bcdca51004f5fc63a7b3becdd5a7fa22ba16bc|place/000002|place/000004|2",
                "edge/2/5|connection-p10b-v1-9b745349ba470fa837d5658f83aafd74ba2b4495b7c3babedf51423aa217df4d|place/000002|place/000005|1",
                "edge/2/6|connection-p10b-v1-a23830f1ebf263b8f336eab5705d836d8b39126cc99b10fc251120ba58ac10ee|place/000002|place/000006|3",
                "edge/3/6|connection-p10b-v1-7e846413679cb4713a2cb0491c33f14a00d33cdccbc25a7848fdc5ca12210c2a|place/000003|place/000006|1",
                "edge/3/7|connection-p10b-v1-a430ac233dbb9067380c4297c59a74effdb8f9dfddd373613da1918c6060a1bf|place/000003|place/000007|1",
                "edge/4/5|connection-p10b-v1-bdb1a70de1a19ab3591610be3376d89090071911b030931d009b9d469cda8034|place/000004|place/000005|3",
                "edge/5/7|connection-p10b-v1-3806cab6fb4c53e984982228621b3ec0ff9ecf264a6d86bff70f1f09c6cb01bc|place/000005|place/000007|2",
                "edge/6/7|connection-p10b-v1-4850bba581d18d9f587fd425c087b2871d04354af0a474b458637efaedd9c296|place/000006|place/000007|1"
            });

        AssertGoldenVector(
            17L,
            "site-p10b-v1-53a425eb981146cb053f342880b803c233c4bfde91aafce61d31ead6b827200e",
            "04bb47bd17765e20527f551d30c1d64426423123a34861558914f8fa5ac76717",
            "35d0467b2f6a2a9b3b8d7ca6b3d4a97d4e6053850f2ecb93c560d73633abccff",
            "8bfc571e3d24de3e43893cc51492e6829aa31b9a8784a31e89f0029970b7277f",
            new[]
            {
                "place/000000|place-p10b-v1-5f2903d9dbb1f01b9a4811fd921cea129afed6902fec4c204e8dc46595d8d554|-|1",
                "place/000001|place-p10b-v1-7a0622d322df7a5b1167502ef6fad9fd6085c476b3ef45bddf2d2df3d5c4186a|place/000000|0",
                "place/000002|place-p10b-v1-79b34724944cf1f170e0c98b771aa381b06eaaa237ed4d5c78984e3758cdde68|-|0",
                "place/000003|place-p10b-v1-112a76395a45fdc8582cc32bb024dfc907645f926bd221974a257709c74dd614|-|0",
                "place/000004|place-p10b-v1-6e9ac17d40e42df30b14724b3e397b8279a4aaebd2e9fcece8300762dfbd3461|-|0",
                "place/000005|place-p10b-v1-c71cc0749250354c55a44cfe1b9f518326a95380457aa05bf5e2f3fb7899c81f|place/000003|0",
                "place/000006|place-p10b-v1-40e018d8095cda20487ad1dd6869afc36a85e9c118040e754defb7afc9c080be|-|0",
                "place/000007|place-p10b-v1-9705958bba4ae6ef305c04e74fbdfaf6337aacf3035a3f2e95eb815f85e897c4|place/000006|0",
                "place/000008|place-p10b-v1-9e9ed1e3a3b20d145b837c9009d4f98dca9bb8c35b688007c23eaf4276a076db|-|0",
                "place/000009|place-p10b-v1-bf53f811202eba513cf5386672a314e9a1e9b13c68eec5fed3e66568f85f7607|place/000008|0"
            },
            new[]
            {
                "edge/0/1|connection-p10b-v1-4764406e54b450fb144f30b2cc49bf2d550b8ce373cb8f3273e549fadb0abd14|place/000000|place/000001|2",
                "edge/0/2|connection-p10b-v1-94a3f12f0a0f13f6a9bfed7805f5868df2392dd6bf6809083bdf26fbe3542469|place/000000|place/000002|3",
                "edge/0/9|connection-p10b-v1-0e25ade09079a5b4ddeb06cb278a76cc8a7d589d0b2451d3faa1a4bd47dfc8af|place/000000|place/000009|1",
                "edge/1/2|connection-p10b-v1-a1c68589a60583edc9c1c105f8ebba0ced8be4bc54b0bfbe4c3f1882c3b3c992|place/000001|place/000002|2",
                "edge/2/3|connection-p10b-v1-b6f9ef5e1a0eb3f898d15ebc9c62ce806016263bef2a53474c95ef5b30e7608d|place/000002|place/000003|1",
                "edge/2/4|connection-p10b-v1-589e426de203d84134ee0c16a1bcdca51004f5fc63a7b3becdd5a7fa22ba16bc|place/000002|place/000004|2",
                "edge/3/5|connection-p10b-v1-d2c5739623f8c6397ae6632099a2d773484fd2109ca39ca004e448e253378b26|place/000003|place/000005|2",
                "edge/3/6|connection-p10b-v1-7e846413679cb4713a2cb0491c33f14a00d33cdccbc25a7848fdc5ca12210c2a|place/000003|place/000006|1",
                "edge/3/7|connection-p10b-v1-a430ac233dbb9067380c4297c59a74effdb8f9dfddd373613da1918c6060a1bf|place/000003|place/000007|2",
                "edge/3/8|connection-p10b-v1-1bd0231b22a4158b587f9f48ee263fb1f11b37b77df04098e08ec25a186f0341|place/000003|place/000008|3",
                "edge/3/9|connection-p10b-v1-01784db1a5506cab9f3467184252d9ec23c3bb364d9e386566cda0d9f2277014|place/000003|place/000009|3",
                "edge/5/8|connection-p10b-v1-4af3f993e956871e801e4f1be8ba295de0ec39b018cad31a8c4f43da8d2c20af|place/000005|place/000008|3",
                "edge/5/9|connection-p10b-v1-5f5f2568d39da0cb9aecdfa41476cfc65a0c07746f0410eff69d1267eea18349|place/000005|place/000009|3",
                "edge/6/7|connection-p10b-v1-4850bba581d18d9f587fd425c087b2871d04354af0a474b458637efaedd9c296|place/000006|place/000007|1",
                "edge/7/8|connection-p10b-v1-3f5ca9d615de7cbfd9df217035f3e65878901cb4414c6b9b109ce353fc00de03|place/000007|place/000008|3",
                "edge/8/9|connection-p10b-v1-a60deade2cd224065a97c0d9b09feade806b1f423c574f8783e8532448ecba60|place/000008|place/000009|3"
            });
    }

    private static void AssertGoldenVector(
        long seed,
        string expectedSiteId,
        string expectedRequestFingerprint,
        string expectedGraphFingerprint,
        string expectedProfileFingerprint,
        string[] expectedPlaces,
        string[] expectedConnections)
    {
        var request = new RuinLocalTopologyGeneration.Request(
            "p9-profile-fingerprint", seed, "north-ruin", "ruin-definition", "location-canonical");
        RuinLocalTopologyGeneration.GeneratedTopology generated = RuinLocalTopologyGeneration.Generate(request);
        Assert.That(request.SiteInstanceId, Is.EqualTo(expectedSiteId));
        Assert.That(request.RequestFingerprint, Is.EqualTo(expectedRequestFingerprint));
        Assert.That(generated.GraphFingerprint, Is.EqualTo(expectedGraphFingerprint));
        Assert.That(generated.ComposedProfileFingerprint, Is.EqualTo(expectedProfileFingerprint));
        Assert.That(generated.Places.Select(place => string.Join("|", new[]
        {
            place.LocalKey, place.RuntimeId, place.ParentLocalKey ?? "-", place.IsEntry ? "1" : "0"
        })), Is.EqualTo(expectedPlaces));
        Assert.That(generated.Connections.Select(connection => string.Join("|", new[]
        {
            connection.LocalKey, connection.RuntimeId, connection.OriginLocalKey,
            connection.DestinationLocalKey, connection.TraversalCost.ToString(System.Globalization.CultureInfo.InvariantCulture)
        })), Is.EqualTo(expectedConnections));
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
