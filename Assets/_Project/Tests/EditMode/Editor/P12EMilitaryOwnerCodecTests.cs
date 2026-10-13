using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EMilitaryOwnerCodecTests
{
    [Test]
    public void PopulatedSnapshot_RoundTripsEveryDailyV1MilitaryOwnerField()
    {
        P12EMilitaryOwnerSnapshot source = CreateSnapshot();
        Assert.That(source.TryValidateDocument(out P12EMilitaryOwnerSnapshotFailure sourceFailure), Is.True,
            sourceFailure.Message);
        Assert.That(P12EMilitaryOwnerCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodeResult), Is.True,
            encodeResult?.SectionCode);
        Assert.That(sections.Select(section => section.Id), Is.EquivalentTo(new[]
        {
            ArmedForceStoreCensusProvider.ForcesSectionId,
            ArmedForceStoreCensusProvider.ContingentsSectionId,
            ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId,
            ContingentManpowerCensusProvider.SectionId,
            ArmedForceSpatialCensusProvider.SectionId
        }));

        Assert.That(P12EMilitaryOwnerCodec.TryDecode(sections,
            out P12EMilitaryOwnerSnapshot restored, out P12AResult decodeResult), Is.True,
            decodeResult?.SectionCode);
        Assert.That(restored.ArmedForceRevision, Is.EqualTo(7L));
        Assert.That(restored.ManpowerRevision, Is.EqualTo(3L));
        Assert.That(restored.PositionRevision, Is.EqualTo(2L));

        P12EMilitaryForceSnapshotRecord child = restored.Forces[0];
        Assert.That(child.ForceIdValue, Is.EqualTo("force-child"));
        Assert.That(child.DisplayName, Is.EqualTo("Child Force"));
        Assert.That(child.CreatedAbsoluteDay, Is.EqualTo(4L));
        Assert.That(child.ParentForceIdValue, Is.EqualTo("force-root"));
        Assert.That(child.OperationalLocationReference, Is.Null);
        Assert.That(child.CommanderPersonIdValue, Is.Null);
        Assert.That(child.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Active));
        Assert.That(child.TerminatedAbsoluteDay, Is.Null);
        Assert.That(child.IsDetached, Is.True);

        P12EMilitaryForceSnapshotRecord root = restored.Forces[1];
        Assert.That(root.DisplayName, Is.EqualTo("Root Force"));
        Assert.That(root.OperationalLocationReference, Is.EqualTo("opaque-location"));
        Assert.That(root.CommanderPersonIdValue, Is.EqualTo("person-commander"));
        Assert.That(root.IsDetached, Is.False);
        Assert.That(restored.Contingents[0].Amount, Is.EqualTo(12L));
        Assert.That(restored.Contingents[0].OriginDomain, Is.EqualTo("content-domain"));
        Assert.That(restored.Contingents[0].OriginValue, Is.EqualTo("origin-value"));
        Assert.That(restored.Contingents[0].ServiceType, Is.EqualTo("open-service"));
        Assert.That(restored.Contingents[0].Characteristics.Select(row => row.Key),
            Is.EqualTo(new[] { "branch", "kind" }));
        Assert.That(restored.Contingents[0].Characteristics.Select(row => row.Value),
            Is.EqualTo(new[] { "field", "regular" }));
        Assert.That(restored.RelevantPersons[0].ReferenceIdValue, Is.EqualTo("reference-1"));
        Assert.That(restored.RelevantPersons[0].ForceIdValue, Is.EqualTo("force-root"));
        Assert.That(restored.RelevantPersons[0].PersonIdValue, Is.EqualTo("person-officer"));
        Assert.That(restored.RelevantPersons[0].RoleKey, Is.EqualTo("officer"));
        Assert.That(restored.ManpowerStates[0].ContingentIdValue, Is.EqualTo("contingent-child"));
        Assert.That(restored.ManpowerStates[0].SourceIdValue, Is.Null);
        Assert.That(restored.ManpowerStates[0].Revision, Is.EqualTo(3L));
        Assert.That(restored.ManpowerStates[0].Cohorts.Select(row => row.Amount),
            Is.EqualTo(new[] { 3L, 2L, 7L }));
        Assert.That(restored.ManpowerStates[0].Cohorts.Select(row => row.CustodianForceIdValue),
            Is.EqualTo(new[] { null, "force-root", null }));
        Assert.That(restored.Positions[0].ForceIdValue, Is.EqualTo("force-child"));
        Assert.That(restored.Positions[0].Reference.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(restored.Positions[0].Reference.LocationIdValue, Is.EqualTo("location-1"));
        Assert.That(restored.Positions[0].Reference.HexIdValue, Is.Null);
        Assert.That(restored.Positions[0].Reference.CrossingIdValue, Is.Null);
        Assert.That(restored.Positions[0].Reference.TopologyOwnerKind, Is.Null);
        Assert.That(restored.Positions[0].Reference.TopologyOwnerRuntimeId, Is.Null);
        Assert.That(restored.Positions[0].Reference.SubLocationRuntimeId, Is.Null);

        Assert.That(P12EMilitaryOwnerCodec.TryEncode(restored,
            out IReadOnlyList<P12AOwnerSection> reencoded, out P12AResult reencodeResult), Is.True,
            reencodeResult?.SectionCode);
        Assert.That(reencoded.Select(section => section.Id), Is.EqualTo(sections.Select(section => section.Id)));
        for (int i = 0; i < sections.Count; i++)
        {
            Assert.That(reencoded[i].Revision, Is.EqualTo(sections[i].Revision));
            Assert.That(reencoded[i].Rows.Count, Is.EqualTo(sections[i].Rows.Count));
            for (int row = 0; row < sections[i].Rows.Count; row++)
            {
                Assert.That(reencoded[i].Rows[row].OwnerKey, Is.EqualTo(sections[i].Rows[row].OwnerKey));
                Assert.That(reencoded[i].Rows[row].Revision, Is.EqualTo(sections[i].Rows[row].Revision));
                Assert.That(reencoded[i].Rows[row].Payload, Is.EqualTo(sections[i].Rows[row].Payload));
            }
        }
    }

    [Test]
    public void Decode_RejectsDuplicateAndMissingOwnerSections()
    {
        P12EMilitaryOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EMilitaryOwnerCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out _), Is.True);
        List<P12AOwnerSection> duplicate = new List<P12AOwnerSection>(sections) { sections[0] };
        Assert.That(P12EMilitaryOwnerCodec.TryDecode(duplicate, out _, out _), Is.False);
        Assert.That(P12EMilitaryOwnerCodec.TryDecode(sections.Take(4).ToArray(), out _, out _), Is.False);
    }

    private static P12EMilitaryOwnerSnapshot CreateSnapshot()
    {
        return new P12EMilitaryOwnerSnapshot(
            P12EMilitaryOwnerSnapshot.CurrentSchemaVersion,
            7L, 3L, 2L, 2, 1, 1, 1, 1,
            new[]
            {
                new P12EMilitaryForceSnapshotRecord("force-child", "Child Force", 4L,
                    "force-root", null, null, ArmedForceLifecycleState.Active, null, true),
                new P12EMilitaryForceSnapshotRecord("force-root", "Root Force", 0L,
                    null, "opaque-location", "person-commander", ArmedForceLifecycleState.Active, null, false)
            },
            new[]
            {
                new P12EMilitaryContingentSnapshotRecord("contingent-child", "force-child", 12L,
                    "content-domain", "origin-value", "open-service", new[]
                    {
                        new P12EMilitaryCharacteristicSnapshot("branch", "field"),
                        new P12EMilitaryCharacteristicSnapshot("kind", "regular")
                    })
            },
            new[] { new P12EMilitaryRelevantPersonSnapshotRecord("reference-1", "force-root", "person-officer", "officer") },
            new[]
            {
                new P12EManpowerStateSnapshotRecord("contingent-child", null, 3L, new[]
                {
                    new P12EManpowerCohortSnapshotRecord(ManpowerInjuryState.Healthy,
                        ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 3L),
                    new P12EManpowerCohortSnapshotRecord(ManpowerInjuryState.Healthy,
                        ManpowerCustodyState.Captured, "force-root", ManpowerAvailabilityState.Unavailable, 2L),
                    new P12EManpowerCohortSnapshotRecord(ManpowerInjuryState.Wounded,
                        ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Unavailable, 7L)
                })
            },
            new[]
            {
                new P12EForcePositionSnapshotRecord("force-child",
                    new P12ESpatialReferenceSnapshot(SpatialReferenceKind.Location, null, "location-1", null,
                        null, null, null))
            });
    }
}
