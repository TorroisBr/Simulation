using System;

/// <summary>
/// Daily-v1 witness for the two P10-excluded per-NPC LocalTopologyKnowledge sections.
/// P12-D capture admits this profile only when both owner lists and their shared revision are zero.
/// </summary>
internal static class P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec
{
    private const int PlaceSectionKind = 1;
    private const int ConnectionSectionKind = 2;

    internal static bool TryEncode(string runtimeId,
        OwnerSectionCensusSnapshot placeWitness, OwnerSectionCensusSnapshot connectionWitness,
        out P12AOwnerSection places, out P12AOwnerSection connections, out P12AResult result)
    {
        places = null;
        connections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(PlaceSectionKind, runtimeId), runtimeId);
        if (!IsRuntimeId(runtimeId) || placeWitness == null || connectionWitness == null) return false;
        string placeId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(PlaceSectionKind, runtimeId);
        string connectionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(ConnectionSectionKind, runtimeId);
        if (!IsExactEmpty(placeWitness, placeId) || !IsExactEmpty(connectionWitness, connectionId)
            || !ReferenceEquals(placeWitness.OwnerInstanceIdentity, connectionWitness.OwnerInstanceIdentity))
            return false;
        string placeAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(placeId);
        string connectionAlias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(connectionId);
        if (string.IsNullOrWhiteSpace(placeAlias) || placeAlias != connectionAlias) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(placeId);
            P12AWire.GetEncodedStringByteCount(connectionId);
            P12AWire.GetEncodedStringByteCount(placeAlias);
        }
        catch (Exception exception) when (exception is P12ALimitException
            || exception is ArgumentException || exception is OverflowException)
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, placeId, runtimeId);
            return false;
        }
        places = new P12AOwnerSection(placeId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
            OwnerSectionRole.Required, placeAlias, 0L, Array.Empty<P12AOwnerRow>());
        connections = new P12AOwnerSection(connectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
            OwnerSectionRole.Required, connectionAlias, 0L, Array.Empty<P12AOwnerRow>());
        result = null;
        return true;
    }

    internal static bool TryDecode(P12AOwnerSection places, P12AOwnerSection connections,
        out string runtimeId, out P12AResult result)
    {
        runtimeId = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            places?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(PlaceSectionKind, string.Empty));
        if (places == null || connections == null
            || string.IsNullOrWhiteSpace(places.Id)
            || !places.Id.StartsWith("p12f.local-topology-knowledge.places/", StringComparison.Ordinal))
            return false;
        string candidateId = places.Id.Substring("p12f.local-topology-knowledge.places/".Length);
        string placeId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(PlaceSectionKind, candidateId);
        string connectionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(ConnectionSectionKind, candidateId);
        if (!IsRuntimeId(candidateId) || !IsExactEmptySection(places, placeId)
            || !IsExactEmptySection(connections, connectionId)
            || places.AliasGroup != connections.AliasGroup)
            return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(places.Id);
            P12AWire.GetEncodedStringByteCount(connections.Id);
            P12AWire.GetEncodedStringByteCount(places.AliasGroup);
        }
        catch (Exception exception) when (exception is P12ALimitException
            || exception is ArgumentException || exception is OverflowException)
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, places.Id, candidateId);
            return false;
        }
        runtimeId = candidateId;
        result = null;
        return true;
    }

    private static bool IsExactEmpty(OwnerSectionCensusSnapshot witness, string id)
        => witness != null && witness.SectionId == id
            && witness.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
            && witness.Role == OwnerSectionRole.Required
            && witness.Cardinality == 0 && witness.Revision == 0L
            && witness.OwnerInstanceIdentity != null;

    private static bool IsExactEmptySection(P12AOwnerSection section, string id)
        => section != null && section.Id == id
            && section.Major == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required && section.Revision == 0L
            && section.Rows.Count == 0
            && section.AliasGroup == DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(id);

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;
}
