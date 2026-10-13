using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcLifeStateOwnerCodecTests
{
    [Test]
    public void LifeStateCodecPreservesSingletonIdentityTypedValuesAndExactRevision()
    {
        P12ANpcLifeStateOwnerValue source = new P12ANpcLifeStateOwnerValue(
            "npc-0007", "villager", NpcLifeState.Alive, NpcInjurySeverity.SeriouslyInjured, 0L);

        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcLifecycleCensusProvider.SectionIdFor(source.RuntimeId, false)));
        Assert.That(section.Major, Is.EqualTo(NpcLifecycleCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Revision, Is.Zero);
        Assert.That(section.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { source.RuntimeId }));
        Assert.That(section.Rows.Single().Revision, Is.Zero);

        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(section,
            out P12ANpcLifeStateOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(decoded.NpcDefinitionId, Is.EqualTo(source.NpcDefinitionId));
        Assert.That(decoded.LifeState, Is.EqualTo(source.LifeState));
        Assert.That(decoded.InjurySeverity, Is.EqualTo(source.InjurySeverity));
        Assert.That(decoded.Revision, Is.Zero);

        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection reencoded, out _), Is.True);
        Assert.That(reencoded.Id, Is.EqualTo(section.Id));
        Assert.That(reencoded.AliasGroup, Is.EqualTo(section.AliasGroup));
        Assert.That(reencoded.Revision, Is.EqualTo(section.Revision));
        Assert.That(reencoded.Rows.Single().OwnerKey, Is.EqualTo(section.Rows.Single().OwnerKey));
        Assert.That(reencoded.Rows.Single().Payload, Is.EqualTo(section.Rows.Single().Payload));
    }

    [Test]
    public void LifeStateCodecPreservesDeadStateAndNonzeroRevision()
    {
        P12ANpcLifeStateOwnerValue source = new P12ANpcLifeStateOwnerValue(
            "npc-dead", "named-person", NpcLifeState.Dead, NpcInjurySeverity.Incapacitated, 19L);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(section,
            out P12ANpcLifeStateOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(decoded.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Incapacitated));
        Assert.That(decoded.Revision, Is.EqualTo(19L));
    }

    [Test]
    public void LifeStateCodecUsesCapturedDRowsWithoutReadingLiveNpcState()
    {
        P12DNpcDRow source = new P12DNpcDRow(
            "npc-captured", "merchant", null, null, NpcLifeState.Dead, NpcInjurySeverity.Incapacitated,
            Array.Empty<string>(), 0, null, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 8L, 0L, 0L, 0L);

        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(section,
            out P12ANpcLifeStateOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(decoded.NpcDefinitionId, Is.EqualTo(source.NpcDefinitionId));
        Assert.That(decoded.LifeState, Is.EqualTo(source.LifeState));
        Assert.That(decoded.InjurySeverity, Is.EqualTo(source.InjurySeverity));
        Assert.That(decoded.Revision, Is.EqualTo(source.LifeStateRevision));
    }

    [Test]
    public void LifeStateCodecRejectsInvalidValuesAndOversizedDynamicKeys()
    {
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode((P12ANpcLifeStateOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            " ", "villager", NpcLifeState.Alive, NpcInjurySeverity.None, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            "npc/with/slash", "villager", NpcLifeState.Alive, NpcInjurySeverity.None, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            "npc-a", "", NpcLifeState.Alive, NpcInjurySeverity.None, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            "npc-a", "villager", NpcLifeState.Alive, NpcInjurySeverity.None, -1L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            "npc-a", "villager", (NpcLifeState)999, NpcInjurySeverity.None, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            "npc-a", "villager", NpcLifeState.Alive, (NpcInjurySeverity)999, 0L), out _, out _), Is.False);

        string oversizedRuntimeId = new string('n', P12AContinuationCodec.MaxStringBytes);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(new P12ANpcLifeStateOwnerValue(
            oversizedRuntimeId, "villager", NpcLifeState.Alive, NpcInjurySeverity.None, 0L),
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void LifeStateCodecRejectsMetadataIdentityRevisionAndPayloadCorruption()
    {
        P12AOwnerSection section = Encode(new P12ANpcLifeStateOwnerValue(
            "npc-a", "villager", NpcLifeState.Alive, NpcInjurySeverity.Hurt, 4L));
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            major: section.Major + 1), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "wrong"), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-other", 4L, section.Rows.Single().Payload) }),
            out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows.Single(), section.Rows.Single() }), out _, out _), Is.False);

        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Create("npc-a", 999, (ushort)NpcInjurySeverity.None, 4L),
            out _, out _), Is.False);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Create("npc-a", (ushort)NpcLifeState.Alive, 999, 4L),
            out _, out _), Is.False);
        P12AOwnerSection otherNpc = Create("npc-b", (ushort)NpcLifeState.Alive,
            (ushort)NpcInjurySeverity.Hurt, 4L);
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(otherNpc,
            sectionId: section.Id), out _, out _), Is.False);
        byte[] staleRevisionPayload = Create("npc-a", (ushort)NpcLifeState.Alive,
            (ushort)NpcInjurySeverity.Hurt, 3L).Rows.Single().Payload;
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, staleRevisionPayload) }), out _, out _), Is.False);

        byte[] payload = section.Rows.Single().Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcLifeStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, payload) }), out _, out _), Is.False);
    }

    private static P12AOwnerSection Encode(P12ANpcLifeStateOwnerValue value)
    {
        Assert.That(P12ANpcLifeStateOwnerCodec.TryEncode(value,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12AOwnerSection Create(string runtimeId, ushort lifeState,
        ushort injurySeverity, long revision)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-life-state/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, "villager");
            writer.Write(lifeState);
            writer.Write(injurySeverity);
            writer.Write(revision);
            writer.Flush();
            string id = NpcLifecycleCensusProvider.SectionIdFor(runtimeId, false);
            return new P12AOwnerSection(id, NpcLifecycleCensusProvider.SchemaVersion, 0,
                OwnerSectionRole.Required, P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision,
                new[] { new P12AOwnerRow(runtimeId, revision, stream.ToArray()) });
        }
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, long? revision = null,
        string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);
}
