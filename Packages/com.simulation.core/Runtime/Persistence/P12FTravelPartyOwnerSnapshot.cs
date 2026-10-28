using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12FCommitmentSnapshotFailure
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSection,
    InvalidOwner,
    InvalidOwnerValue,
    InvalidNpcBinding,
    InvalidIdentityBinding,
    InvalidSnapshot,
    StageFailed,
    StaleCapture
}

/// <summary>Detached active TravelParty owner value; contains no source owner or NPC references.</summary>
internal sealed class P12FTravelPartyOwnerSnapshotRecord
{
    internal string TravelPartyId { get; }
    internal string OriginLocationRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal string RouteRuntimeId { get; }
    internal IReadOnlyList<string> TravelerRuntimeIds { get; }
    internal IReadOnlyList<string> EscortRuntimeIds { get; }
    internal IReadOnlyList<string> MemberRuntimeIds { get; }
    internal IReadOnlyList<TravelPartyMemberCost> MemberCosts { get; }
    internal int TravelDaysTotal { get; }
    internal string OriginDecisionId { get; }

    internal P12FTravelPartyOwnerSnapshotRecord(TravelPartyRuntime party)
    {
        if (party == null) throw new ArgumentNullException(nameof(party));
        TravelPartyId = party.TravelPartyId;
        OriginLocationRuntimeId = party.OriginLocationRuntimeId;
        DestinationLocationRuntimeId = party.DestinationLocationRuntimeId;
        RouteRuntimeId = party.RouteRuntimeId;
        TravelerRuntimeIds = Copy(party.TravelerRuntimeIds);
        EscortRuntimeIds = Copy(party.EscortRuntimeIds);
        MemberRuntimeIds = Copy(party.MemberRuntimeIds);
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
            costs.Add(new TravelPartyMemberCost(cost.RuntimeId, cost.Amount));
        MemberCosts = new ReadOnlyCollection<TravelPartyMemberCost>(costs);
        TravelDaysTotal = party.TravelDaysTotal;
        OriginDecisionId = party.OriginDecisionId;
    }

    internal P12FTravelPartyOwnerSnapshotRecord(string travelPartyId, string originLocationRuntimeId,
        string destinationLocationRuntimeId, string routeRuntimeId, IEnumerable<string> travelers,
        IEnumerable<string> escorts, IEnumerable<string> members, IEnumerable<TravelPartyMemberCost> memberCosts,
        int travelDaysTotal, string originDecisionId)
    {
        TravelPartyId = travelPartyId;
        OriginLocationRuntimeId = originLocationRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        RouteRuntimeId = routeRuntimeId;
        TravelerRuntimeIds = Copy(travelers);
        EscortRuntimeIds = Copy(escorts);
        MemberRuntimeIds = Copy(members);
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();
        foreach (TravelPartyMemberCost cost in memberCosts)
            costs.Add(new TravelPartyMemberCost(cost.RuntimeId, cost.Amount));
        MemberCosts = new ReadOnlyCollection<TravelPartyMemberCost>(costs);
        TravelDaysTotal = travelDaysTotal;
        OriginDecisionId = originDecisionId;
    }

    private static IReadOnlyList<string> Copy(IEnumerable<string> source) =>
        new ReadOnlyCollection<string>(new List<string>(source));
}

/// <summary>
/// Detached P12-F TravelParty state and its Core-owned capture/materialization
/// boundary. Cross-owner NPC and geography checks remain in the restore graph
/// coordinator before this value is installed.
/// </summary>
internal sealed class P12FTravelPartyOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;
    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12FTravelPartyOwnerSnapshotRecord> Parties { get; }

    internal P12FTravelPartyOwnerSnapshot(int schemaVersion, long revision,
        IEnumerable<P12FTravelPartyOwnerSnapshotRecord> parties)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        Parties = new ReadOnlyCollection<P12FTravelPartyOwnerSnapshotRecord>(
            new List<P12FTravelPartyOwnerSnapshotRecord>(parties));
    }

    internal static bool TryCapture(TravelPartyStore source, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        out P12FTravelPartyOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12FCommitmentSnapshotFailure.InvalidCaptureContext;
        if (source == null || !P12FCommitmentSnapshotValidation.HasExactContext(token, exactOwnerSectionVector))
            return false;

        using (source.EnterReadWindow())
        {
            if (!P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    TravelPartyCensusProvider.SectionId, TravelPartyCensusProvider.SchemaVersion,
                    source, out int expectedCount, out long expectedRevision))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidOwnerSection;
                return false;
            }
            if (!source.ValidateCensus(out int count, out long revision)
                || count != expectedCount || revision != expectedRevision)
            {
                failure = P12FCommitmentSnapshotFailure.InvalidOwner;
                return false;
            }

            List<P12FTravelPartyOwnerSnapshotRecord> records =
                new List<P12FTravelPartyOwnerSnapshotRecord>(count);
            foreach (TravelPartyRuntime party in source.ActiveParties)
            {
                if (!IsValid(party))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidOwnerValue;
                    return false;
                }
                records.Add(new P12FTravelPartyOwnerSnapshotRecord(party));
            }

            if (!source.ValidateCensus(out int afterCount, out long afterRevision)
                || afterCount != expectedCount || afterRevision != expectedRevision
                || !P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    TravelPartyCensusProvider.SectionId, TravelPartyCensusProvider.SchemaVersion,
                    source, out int finalCount, out long finalRevision)
                || finalCount != afterCount || finalRevision != afterRevision)
            {
                failure = P12FCommitmentSnapshotFailure.StaleCapture;
                return false;
            }

            snapshot = new P12FTravelPartyOwnerSnapshot(CurrentSchemaVersion, revision, records);
            failure = P12FCommitmentSnapshotFailure.None;
            return true;
        }
    }

    /// <summary>
    /// Builds a detached owner store while preserving row order and the exact
    /// owner-local revision. The caller validates reciprocal NPC and spatial
    /// bindings against its private staged graph before publishing this store.
    /// </summary>
    internal static bool TryCreateStoreFromOwnerSnapshot(P12FTravelPartyOwnerSnapshot snapshot,
        out TravelPartyStore stagedStore, out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null || snapshot.SchemaVersion != CurrentSchemaVersion || snapshot.Revision < 0
            || snapshot.Parties == null)
            return false;

        HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        List<TravelPartyRuntime> stagedParties = new List<TravelPartyRuntime>(snapshot.Parties.Count);
        foreach (P12FTravelPartyOwnerSnapshotRecord record in snapshot.Parties)
        {
            if (!IsValidRecord(record) || !partyIds.Add(record.TravelPartyId))
                return false;
            foreach (string memberId in record.MemberRuntimeIds)
                if (!memberIds.Add(memberId))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                    return false;
                }
            try
            {
                stagedParties.Add(TravelPartyRuntime.CreateActiveFromOwnerSnapshot(
                    record.TravelPartyId, record.OriginLocationRuntimeId,
                    record.DestinationLocationRuntimeId, record.RouteRuntimeId,
                    record.TravelerRuntimeIds, record.EscortRuntimeIds,
                    record.TravelDaysTotal, record.OriginDecisionId, record.MemberCosts));
            }
            catch (ArgumentException)
            {
                failure = P12FCommitmentSnapshotFailure.StageFailed;
                return false;
            }
        }

        if (!TravelPartyStore.TryCreateFromOwnerRows(
                stagedParties, snapshot.Revision, out stagedStore) || stagedStore == null)
        {
            failure = P12FCommitmentSnapshotFailure.StageFailed;
            return false;
        }
        failure = P12FCommitmentSnapshotFailure.None;
        return true;
    }

    private static bool IsValid(TravelPartyRuntime party)
    {
        if (party == null || !party.IsActive || string.IsNullOrWhiteSpace(party.TravelPartyId)
            || string.IsNullOrWhiteSpace(party.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(party.DestinationLocationRuntimeId)
            || string.IsNullOrWhiteSpace(party.RouteRuntimeId) || party.TravelDaysTotal <= 0
            || party.MemberRuntimeIds == null || party.MemberRuntimeIds.Count == 0
            || party.MemberCosts == null || party.MemberCosts.Count != party.MemberRuntimeIds.Count) return false;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in party.MemberRuntimeIds) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return false;
        HashSet<string> costIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
            if (cost == null || !ids.Contains(cost.RuntimeId) || !costIds.Add(cost.RuntimeId)
                || cost.Amount < 0f || float.IsNaN(cost.Amount) || float.IsInfinity(cost.Amount)) return false;
        return costIds.Count == ids.Count;
    }

    internal static bool IsValidRecord(P12FTravelPartyOwnerSnapshotRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.TravelPartyId)
            || string.IsNullOrWhiteSpace(record.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(record.DestinationLocationRuntimeId)
            || string.IsNullOrWhiteSpace(record.RouteRuntimeId) || record.TravelDaysTotal <= 0
            || record.MemberRuntimeIds == null || record.MemberCosts == null
            || record.MemberRuntimeIds.Count == 0 || record.MemberCosts.Count != record.MemberRuntimeIds.Count) return false;
        HashSet<string> members = new HashSet<string>(record.MemberRuntimeIds, StringComparer.Ordinal);
        HashSet<string> costs = new HashSet<string>(StringComparer.Ordinal);
        if (members.Count != record.MemberRuntimeIds.Count) return false;
        foreach (TravelPartyMemberCost cost in record.MemberCosts)
            if (cost == null || !members.Contains(cost.RuntimeId) || !costs.Add(cost.RuntimeId)
                || cost.Amount < 0f || float.IsNaN(cost.Amount) || float.IsInfinity(cost.Amount)) return false;
        return costs.Count == members.Count;
    }
}

internal static class P12FCommitmentSnapshotValidation
{
    internal static bool HasExactContext(DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector) => token != null && vector != null
        && ReferenceEquals(token.OwnerSections, vector)
        && token.AdmissionContext != null
        && token.AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && token.CompletedCoreSequence > 0 && token.AbsoluteDay >= 0 && token.MutationEpoch >= 0;

    internal static bool MatchesSection(DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector, string sectionId, int schemaVersion,
        object owner, out int cardinality, out long revision)
    {
        cardinality = 0;
        revision = 0;
        if (!HasExactContext(token, vector) || string.IsNullOrWhiteSpace(sectionId) || owner == null) return false;
        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot section in vector)
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
            {
                if (match != null) return false;
                match = section;
            }
        if (match == null || match.SchemaVersion != schemaVersion || match.Role != OwnerSectionRole.Required
            || !ReferenceEquals(match.OwnerInstanceIdentity, owner) || match.Cardinality < 0 || match.Revision < 0) return false;
        cardinality = match.Cardinality;
        revision = match.Revision;
        return true;
    }
}
