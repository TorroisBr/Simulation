using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ARuntimeIdentityOwnerCodecTests
{
    [Test]
    public void EncodesAdmittedIdentityFamiliesWithOneSharedRevision()
    {
        P12CRuntimeIdentityOwnerSnapshot source = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, 4L,
            new[]
            {
                new KeyValuePair<string, string>("npc", "npc-z"),
                new KeyValuePair<string, string>("city", "city-a"),
                new KeyValuePair<string, string>("npc", "npc-a"),
                new KeyValuePair<string, string>("location", "location-a")
            });
        Assert.That(P12ARuntimeIdentityOwnerCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodeError),
            Is.True, encodeError?.SectionCode + ":" + encodeError?.Code);
        Assert.That(sections.Select(section => section.Revision).Distinct(), Is.EqualTo(new[] { 4L }));
        Assert.That(sections.Single(section => section.Id == RuntimeIdentityRegistryCensusProvider.NpcsSectionId)
            .Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "npc-a", "npc-z" }));
        Assert.That(sections.Single(section => section.Id == RuntimeIdentityRegistryCensusProvider.RoutesSectionId)
            .Rows, Is.Empty);

        Assert.That(P12ARuntimeIdentityOwnerCodec.TryDecode(sections,
            out P12CRuntimeIdentityOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode + ":" + decodeError?.Code);
        Assert.That(decoded.Revision, Is.EqualTo(source.Revision));
        Assert.That(decoded.IdentityValues, Is.EquivalentTo(source.IdentityValues));
    }

    [Test]
    public void RejectsExcludedOrAliasedIdentityValues()
    {
        P12CRuntimeIdentityOwnerSnapshot excluded = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, 1L,
            new[] { new KeyValuePair<string, string>("site", "site-a") });
        Assert.That(P12ARuntimeIdentityOwnerCodec.TryEncode(excluded, out _, out P12AResult result), Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.UnsupportedProfile));

        P12CRuntimeIdentityOwnerSnapshot duplicate = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, 2L,
            new[]
            {
                new KeyValuePair<string, string>("npc", "shared-id"),
                new KeyValuePair<string, string>("city", "shared-id")
            });
        Assert.That(P12ARuntimeIdentityOwnerCodec.TryEncode(duplicate, out _, out _), Is.False);
    }
}
