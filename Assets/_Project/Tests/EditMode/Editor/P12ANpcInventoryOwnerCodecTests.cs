using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcInventoryOwnerCodecTests
{
    [Test]
    public void InventoryCodecPreservesCapturedItemOrderValuesAndRevision()
    {
        List<P12DNpcInventoryRowValue> sourceRows = new List<P12DNpcInventoryRowValue>
        {
            new P12DNpcInventoryRowValue("z-item", 3, 1.25f),
            new P12DNpcInventoryRowValue("a-item", 0, 0f)
        };
        P12ANpcInventoryOwnerValue source =
            new P12ANpcInventoryOwnerValue("npc-7", sourceRows, 19L);
        sourceRows[0] = new P12DNpcInventoryRowValue("mutated", 99, 99f);

        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcInventoryCensusProvider.SectionPrefix + "npc-7"));
        Assert.That(section.Major, Is.EqualTo(NpcInventoryCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.AliasGroup, Is.Null, "Inventory is not an alias-grouped owner section in the selected census.");
        Assert.That(section.Revision, Is.EqualTo(19L));
        Assert.That(section.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "item/z-item", "item/a-item" }));
        Assert.That(section.Rows.All(row => row.Revision == 19L), Is.True);

        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(section,
            out P12ANpcInventoryOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo("npc-7"));
        Assert.That(decoded.Revision, Is.EqualTo(19L));
        Assert.That(decoded.Rows.Select(row => row.ItemDefinitionId),
            Is.EqualTo(new[] { "z-item", "a-item" }));
        Assert.That(decoded.Rows.Select(row => row.Amount), Is.EqualTo(new[] { 3, 0 }));
        Assert.That(BitConverter.GetBytes(decoded.Rows[0].AverageUnitCost),
            Is.EqualTo(BitConverter.GetBytes(1.25f)));

        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows.Select(row => row.Payload),
            Is.EqualTo(section.Rows.Select(row => row.Payload)));
    }

    [Test]
    public void EmptyInventoryRemainsRequiredAndRetainsZeroRevision()
    {
        P12ANpcInventoryOwnerValue source =
            new P12ANpcInventoryOwnerValue("npc-empty", Array.Empty<P12DNpcInventoryRowValue>(), 0L);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(source, out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Is.Empty);
        Assert.That(section.Revision, Is.Zero);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(section,
            out P12ANpcInventoryOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Rows, Is.Empty);
        Assert.That(decoded.Revision, Is.Zero);
    }

    [Test]
    public void DRowAdapterUsesOnlyCapturedInventoryValues()
    {
        P12DNpcDRow source = CreateDRow("npc-captured", new[]
        {
            new P12DNpcInventoryRowValue("ore", 4, 2.5f),
            new P12DNpcInventoryRowValue("bread", 1, 0.75f)
        }, 7L);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(section,
            out P12ANpcInventoryOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Rows.Select(row => new
            { row.ItemDefinitionId, row.Amount, row.AverageUnitCost }),
            Is.EqualTo(source.InventoryRows.Select(row => new
                { row.ItemDefinitionId, row.Amount, row.AverageUnitCost })));
        Assert.That(decoded.Revision, Is.EqualTo(source.InventoryRevision));
    }

    [Test]
    public void RejectsInvalidInventoryValuesAndOversizedIdentifiers()
    {
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode((P12ANpcInventoryOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue(" ", Array.Empty<P12DNpcInventoryRowValue>(), 0L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc/invalid", Array.Empty<P12DNpcInventoryRowValue>(), 0L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a", new[]
            {
                new P12DNpcInventoryRowValue("duplicate", 1, 0f),
                new P12DNpcInventoryRowValue("duplicate", 2, 0f)
            }, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a",
                new[] { new P12DNpcInventoryRowValue("item", -1, 0f) }, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a",
                new[] { new P12DNpcInventoryRowValue("item", 1, float.NaN) }, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a", Array.Empty<P12DNpcInventoryRowValue>(), -1L), out _, out _), Is.False);

        string oversized = new string('i', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a",
                new[] { new P12DNpcInventoryRowValue(oversized, 1, 0f) }, 0L),
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        string maximumItemId = new string('i', P12AContinuationCodec.MaxStringBytes);
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(
            new P12ANpcInventoryOwnerValue("npc-a",
                new[] { new P12DNpcInventoryRowValue(maximumItemId, 1, 0f) }, 0L),
            out _, out P12AResult prefixedKeyLimitError), Is.False);
        Assert.That(prefixedKeyLimitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsWrongMetadataIdentityRevisionDuplicateRowsAndMalformedPayloads()
    {
        P12AOwnerSection section = Encode(new P12ANpcInventoryOwnerValue("npc-a",
            new[] { new P12DNpcInventoryRowValue("item-a", 3, 1f) }, 4L));
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "wrong"), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            sectionId: NpcInventoryCensusProvider.SectionPrefix + "npc-other"), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows.Single(), section.Rows.Single() }), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("item/other", 4L, section.Rows.Single().Payload) }), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("item/item-a", 3L, section.Rows.Single().Payload) }), out _, out _), Is.False);

        P12AOwnerSection nullId = new P12AOwnerSection(null, section.Major, section.Minor,
            section.Role, section.AliasGroup, section.Revision, section.Rows);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(nullId, out _, out _), Is.False);

        byte[] trailing = section.Rows.Single().Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("item/item-a", 4L, trailing) }), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("item/item-a", 4L, Payload("npc-other", "item-a", 3, 1f)) }), out _, out _), Is.False);
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("item/item-a", 4L, Payload("npc-a", "item-other", 3, 1f)) }), out _, out _), Is.False);

        string maximumItemId = new string('i', P12AContinuationCodec.MaxStringBytes);
        P12AOwnerSection oversizedOwnerKey = new P12AOwnerSection(section.Id, section.Major,
            section.Minor, section.Role, section.AliasGroup, 4L,
            new[] { new P12AOwnerRow("item/" + maximumItemId, 4L,
                Payload("npc-a", maximumItemId, 1, 0f)) });
        Assert.That(P12ANpcInventoryOwnerCodec.TryDecode(oversizedOwnerKey,
            out _, out P12AResult keyLimitError), Is.False);
        Assert.That(keyLimitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    private static P12AOwnerSection Encode(P12ANpcInventoryOwnerValue value)
    {
        Assert.That(P12ANpcInventoryOwnerCodec.TryEncode(value,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12DNpcDRow CreateDRow(string runtimeId,
        IEnumerable<P12DNpcInventoryRowValue> inventoryRows, long inventoryRevision)
        => new P12DNpcDRow(runtimeId, "villager", null, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            null, null, null, null, null, inventoryRows, inventoryRevision,
            0f, 0L, 0L, 0L, 0L, 0L);

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, long? revision = null,
        string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] Payload(string runtimeId, string itemId, int amount, float averageCost)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-inventory/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, itemId);
            writer.Write(amount);
            writer.Write(averageCost);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
