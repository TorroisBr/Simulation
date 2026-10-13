using System;
using System.IO;

/// <summary>Detached values owned by one NPC lifecycle section.</summary>
internal sealed class P12ANpcLifeStateOwnerValue
{
    internal P12ANpcLifeStateOwnerValue(string runtimeId, string npcDefinitionId,
        NpcLifeState lifeState, NpcInjurySeverity injurySeverity, long revision)
    {
        RuntimeId = runtimeId;
        NpcDefinitionId = npcDefinitionId;
        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal string NpcDefinitionId { get; }
    internal NpcLifeState LifeState { get; }
    internal NpcInjurySeverity InjurySeverity { get; }
    internal long Revision { get; }
}

/// <summary>Explicit Daily-v1 codec for the per-NPC life-state owner section.</summary>
internal static class P12ANpcLifeStateOwnerCodec
{
    private const string PayloadContract = "p12a.npc-life-state/v1";

    internal static bool TryEncode(P12DNpcDRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                NpcLifecycleCensusProvider.LifeStateSectionPrefix);
            return false;
        }
        return TryEncode(new P12ANpcLifeStateOwnerValue(source.RuntimeId, source.NpcDefinitionId,
            source.LifeState, source.InjurySeverity, source.LifeStateRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcLifeStateOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcLifecycleCensusProvider.LifeStateSectionPrefix);
        if (!TryValidate(value, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(value.RuntimeId);
            P12AWire.GetEncodedStringByteCount(value.NpcDefinitionId);
            P12AWire.GetEncodedStringByteCount(
                P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId));
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.RuntimeId);
                P12AWire.WriteString(writer, value.NpcDefinitionId);
                writer.Write((ushort)value.LifeState);
                writer.Write((ushort)value.InjurySeverity);
                writer.Write(value.Revision);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }

                section = new P12AOwnerSection(sectionId, NpcLifecycleCensusProvider.SchemaVersion, 0,
                    OwnerSectionRole.Required,
                    P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId), value.Revision,
                    new[] { new P12AOwnerRow(value.RuntimeId, value.Revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, sectionId, value.RuntimeId);
            section = null;
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12ANpcLifeStateOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? NpcLifecycleCensusProvider.LifeStateSectionPrefix);
        if (section == null || section.Rows.Count != 1
            || !string.Equals(section.Id, NpcLifecycleCensusProvider.LifeStateSectionPrefix
                + ExtractRuntimeId(section.Id), StringComparison.Ordinal)
            || section.Major != NpcLifecycleCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required
            || !string.Equals(section.AliasGroup,
                P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal)
            || section.Revision < 0L)
            return false;

        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.Revision != section.Revision || row.PayloadLength < 0
            || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
            || string.IsNullOrWhiteSpace(row.OwnerKey)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadContract(reader)
                    || !ReadRequiredString(reader, out string runtimeId)
                    || !ReadRequiredString(reader, out string definitionId)) return false;
                NpcLifeState lifeState = (NpcLifeState)reader.ReadUInt16();
                NpcInjurySeverity injurySeverity = (NpcInjurySeverity)reader.ReadUInt16();
                long revision = reader.ReadInt64();
                P12ANpcLifeStateOwnerValue candidate = new P12ANpcLifeStateOwnerValue(
                    runtimeId, definitionId, lifeState, injurySeverity, revision);
                if (stream.Position != stream.Length || row.OwnerKey != runtimeId
                    || section.Id != NpcLifecycleCensusProvider.SectionIdFor(runtimeId, residence: false)
                    || section.Revision != revision || !TryValidate(candidate, out _)) return false;
                value = candidate;
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id, row.OwnerKey);
            return false;
        }
    }

    private static bool TryValidate(P12ANpcLifeStateOwnerValue value, out string sectionId)
    {
        sectionId = null;
        if (value == null || string.IsNullOrWhiteSpace(value.RuntimeId) || value.RuntimeId.Contains("/")
            || string.IsNullOrWhiteSpace(value.NpcDefinitionId) || value.Revision < 0L
            || !Enum.IsDefined(typeof(NpcLifeState), value.LifeState)
            || !NpcInjuryRules.IsValid(value.InjurySeverity)) return false;
        try { sectionId = NpcLifecycleCensusProvider.SectionIdFor(value.RuntimeId, residence: false); }
        catch (ArgumentException) { return false; }
        return true;
    }

    private static string ExtractRuntimeId(string sectionId)
    {
        if (sectionId == null || !sectionId.StartsWith(
                NpcLifecycleCensusProvider.LifeStateSectionPrefix, StringComparison.Ordinal)) return null;
        string runtimeId = sectionId.Substring(NpcLifecycleCensusProvider.LifeStateSectionPrefix.Length);
        return string.IsNullOrWhiteSpace(runtimeId) || runtimeId.Contains("/") ? null : runtimeId;
    }

    private static bool ReadContract(BinaryReader reader)
        => P12AWire.ReadString(reader, out string value)
            && string.Equals(value, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is System.Text.DecoderFallbackException;
}
