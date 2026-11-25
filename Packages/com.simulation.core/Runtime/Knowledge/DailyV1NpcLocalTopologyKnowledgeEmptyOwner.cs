using System;

/// <summary>
/// Core-owned identity for the selected Daily-v1 exact-empty witnesses of an
/// NPC's LocalTopologyKnowledge. P10 topology content remains outside this
/// profile; the Unity projection must continue to match this empty boundary.
/// </summary>
internal sealed class DailyV1NpcLocalTopologyKnowledgeEmptyOwner
{
    internal string OwnerRuntimeId { get; }
    internal int PlaceObservationCount => 0;
    internal int ConnectionObservationCount => 0;
    internal long Revision => 0L;

    internal DailyV1NpcLocalTopologyKnowledgeEmptyOwner(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId))
            throw new ArgumentException("Daily-v1 LocalTopology witness requires an NPC RuntimeId.", nameof(ownerRuntimeId));
        OwnerRuntimeId = ownerRuntimeId;
    }

    internal bool MatchesRuntimeId(string runtimeId) =>
        !string.IsNullOrWhiteSpace(runtimeId)
        && string.Equals(OwnerRuntimeId, runtimeId, StringComparison.Ordinal);

    internal bool MatchesProjection(
        string expectedRuntimeId,
        string projectionRuntimeId,
        int placeObservationCount,
        int connectionObservationCount,
        long revision) =>
        MatchesRuntimeId(expectedRuntimeId)
        && MatchesRuntimeId(projectionRuntimeId)
        && placeObservationCount == PlaceObservationCount
        && connectionObservationCount == ConnectionObservationCount
        && revision == Revision;
}