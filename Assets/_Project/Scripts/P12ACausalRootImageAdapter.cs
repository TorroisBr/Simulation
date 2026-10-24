using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached typed C snapshots recovered from a continuation image.</summary>
internal sealed class P12ACausalRootImageSnapshots
{
    internal P12ACausalRootImageSnapshots(P12CWorldIdentitySnapshot worldIdentity,
        RuntimeIdAllocatorSnapshot allocator, SimulationRecordSequenceSnapshot recordSequence,
        P12CSpatialAuthoritySnapshot spatialAuthority, P12CP9GenesisManifestSnapshot p9Manifest,
        DeterministicRandomRootSnapshot randomRoot)
    {
        WorldIdentity = worldIdentity;
        Allocator = allocator;
        RecordSequence = recordSequence;
        SpatialAuthority = spatialAuthority;
        P9Manifest = p9Manifest;
        RandomRoot = randomRoot;
    }

    internal P12CWorldIdentitySnapshot WorldIdentity { get; }
    internal RuntimeIdAllocatorSnapshot Allocator { get; }
    internal SimulationRecordSequenceSnapshot RecordSequence { get; }
    internal P12CSpatialAuthoritySnapshot SpatialAuthority { get; }
    internal P12CP9GenesisManifestSnapshot P9Manifest { get; }
    internal DeterministicRandomRootSnapshot RandomRoot { get; }
}

/// <summary>
/// Binds the already reviewed C/P8 owner codecs to their Daily-v1 image
/// sections. Unsupported or inconsistent causal roots fail before staging.
/// </summary>
internal static class P12ACausalRootImageAdapter
{
    private static readonly string[] AllocatorSectionIds =
    {
        RuntimeIdAllocatorCensusProvider.EventsSectionId,
        RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
        RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId
    };

    internal static bool TryEncode(P12AContinuationHeader header,
        P12CWorldIdentitySnapshot worldIdentity, RuntimeIdAllocatorSnapshot allocator,
        SimulationRecordSequenceSnapshot recordSequence, P12CSpatialAuthoritySnapshot spatialAuthority,
        P12CP9GenesisManifestSnapshot p9Manifest, DeterministicRandomRootSnapshot randomRoot,
        out P12ACausalRootPayload causalRoots, out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        causalRoots = null;
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (header == null || !header.IsValid || worldIdentity == null
            || !worldIdentity.TryStage(out WorldId stagedWorldId, out _)
            || stagedWorldId.Value != header.WorldId)
            return false;

        P12ACausalRootPayload roots = new P12ACausalRootPayload(p9Manifest == null ? null : p9Manifest.CoreValue, randomRoot);
        if (!roots.IsValidFor(header)
            || !P12ACSpatialAuthorityCodec.TryComputeFingerprint(spatialAuthority, out string spatialFingerprint)
            || !string.Equals(spatialFingerprint, header.P8GeographyFingerprint, StringComparison.Ordinal))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p8a.hexes");
            return false;
        }

        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(7);
        foreach (string id in AllocatorSectionIds)
        {
            if (!TryGetAllocatorCounter(allocator, id, out long nextSequence))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue, id,
                    P12ACausalRootOwnerCodec.AllocatorOwnerKeyForSection(id));
                return false;
            }
            if (!P12ACausalRootOwnerCodec.TryEncodeRuntimeIdAllocator(allocator, id,
                    nextSequence - 1L, P12ADailyV1Census.ExpectedAliasGroupForSection(id),
                    out P12AOwnerSection allocatorSection, out result))
                return false;
            encoded.Add(allocatorSection);
        }

        if (!P12ACausalRootOwnerCodec.TryEncodeRecordSequence(recordSequence,
                recordSequence == null ? -1L : recordSequence.NextSequence - 1L,
                P12ADailyV1Census.ExpectedAliasGroupForSection(SimulationRecordSequenceCensusProvider.SectionId),
                out P12AOwnerSection sequenceSection, out result)
            || !P12ACSpatialAuthorityCodec.TryEncode(spatialAuthority,
                P12ADailyV1Census.ExpectedAliasGroupForSection("p8a.hexes"),
                out P12AOwnerSection hexSection, out P12AOwnerSection locationSection,
                out P12AOwnerSection scaleSection, out result))
            return false;

        encoded.Add(sequenceSection);
        encoded.Add(hexSection);
        encoded.Add(locationSection);
        encoded.Add(scaleSection);
        encoded.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
        causalRoots = roots;
        sections = new ReadOnlyCollection<P12AOwnerSection>(encoded);
        result = null;
        return true;
    }

    internal static bool TryDecode(P12AContinuationImage image,
        out P12ACausalRootImageSnapshots snapshots, out P12AResult result)
    {
        snapshots = null;
        result = new P12AResult(P12AResultCode.OwnerGraphRejected);
        if (image == null || !P12ADailyV1Census.TryCreateForImage(image,
                out _, out result))
            return false;
        if (!image.CausalRoots.IsValidFor(image.Header))
        { result = new P12AResult(P12AResultCode.InvalidOwnerValue, P12ACausalRootPayload.SectionId); return false; }

        P12AOwnerSection events = Find(image, RuntimeIdAllocatorCensusProvider.EventsSectionId);
        P12AOwnerSection decisions = Find(image, RuntimeIdAllocatorCensusProvider.DecisionsSectionId);
        P12AOwnerSection parties = Find(image, RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId);
        P12AOwnerSection sequenceSection = Find(image, SimulationRecordSequenceCensusProvider.SectionId);
        P12AOwnerSection hexes = Find(image, "p8a.hexes");
        P12AOwnerSection locations = Find(image, "p8a.locations");
        P12AOwnerSection scale = Find(image, "p8a.scale-context");

        if (!P12ACausalRootOwnerCodec.TryDecodeRuntimeIdAllocator(events,
                out RuntimeIdAllocatorSnapshot eventAllocator, out result)
            || !P12ACausalRootOwnerCodec.TryDecodeRuntimeIdAllocator(decisions,
                out RuntimeIdAllocatorSnapshot decisionAllocator, out result)
            || !P12ACausalRootOwnerCodec.TryDecodeRuntimeIdAllocator(parties,
                out RuntimeIdAllocatorSnapshot partyAllocator, out result)
            || !SameAllocator(eventAllocator, decisionAllocator)
            || !SameAllocator(eventAllocator, partyAllocator)
            || !P12ACausalRootOwnerCodec.TryDecodeRecordSequence(sequenceSection,
                out SimulationRecordSequenceSnapshot recordSequence, out result)
            || !P12ACSpatialAuthorityCodec.TryDecode(hexes, locations, scale,
                out P12CSpatialAuthoritySnapshot spatialAuthority, out result)
            || !P12ACSpatialAuthorityCodec.TryComputeFingerprint(spatialAuthority,
                out string spatialFingerprint)
            || !string.Equals(spatialFingerprint, image.Header.P8GeographyFingerprint,
                StringComparison.Ordinal))
        {
            if (result == null) result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12c.runtime-id-allocator.events");
            return false;
        }

        P12CWorldIdentitySnapshot identity = new P12CWorldIdentitySnapshot(
            P12CWorldIdentitySnapshot.CurrentSnapshotContract,
            P12CWorldIdentitySnapshot.CurrentSnapshotSchemaVersion,
            image.Header.WorldId);
        if (!identity.TryStage(out _, out _))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12c.world-identity");
            return false;
        }

        snapshots = new P12ACausalRootImageSnapshots(identity, eventAllocator, recordSequence,
            spatialAuthority, P12CP9GenesisManifestSnapshot.FromCore(image.CausalRoots.P9Manifest), image.CausalRoots.RandomRoot);
        result = null;
        return true;
    }

    internal static bool TryStage(P12AContinuationImage image,
        out P12CStagedContinuationRoot staged, out P12AResult result,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        staged = null;
        if (!TryDecode(image, out P12ACausalRootImageSnapshots snapshots, out result)) return false;
        if (!P12CContinuationRootStager.TryStageForImage(snapshots.WorldIdentity,
                snapshots.Allocator, snapshots.RecordSequence, snapshots.SpatialAuthority,
                snapshots.P9Manifest, snapshots.RandomRoot, out staged,
                out _, stageObserver))
        {
            staged = null;
            result = new P12AResult(P12AResultCode.OwnerGraphRejected, "p12c.continuation-roots");
            return false;
        }
        result = null;
        return true;
    }

    private static P12AOwnerSection Find(P12AContinuationImage image, string id)
    {
        foreach (P12AOwnerSection section in image.Sections)
            if (section.Id == id) return section;
        return null;
    }

    private static bool TryGetAllocatorCounter(RuntimeIdAllocatorSnapshot snapshot,
        string sectionId, out long nextSequence)
    {
        nextSequence = -1L;
        string family = sectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId ? "event"
            : sectionId == RuntimeIdAllocatorCensusProvider.DecisionsSectionId ? "decision"
            : sectionId == RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId ? "travel-party" : null;
        if (snapshot?.Counters == null || family == null) return false;
        foreach (RuntimeIdAllocatorCounterSnapshot counter in snapshot.Counters)
            if (counter != null && counter.FamilyId == family)
            { nextSequence = counter.NextSequence; return nextSequence > 0L; }
        return false;
    }

    private static bool SameAllocator(RuntimeIdAllocatorSnapshot left, RuntimeIdAllocatorSnapshot right)
    {
        if (left == null || right == null || left.SchemaId != right.SchemaId
            || left.SchemaVersion != right.SchemaVersion || left.Counters == null || right.Counters == null
            || left.Counters.Count != right.Counters.Count) return false;
        for (int i = 0; i < left.Counters.Count; i++)
        {
            RuntimeIdAllocatorCounterSnapshot a = left.Counters[i], b = right.Counters[i];
            if (a == null || b == null || a.FamilyId != b.FamilyId || a.NextSequence != b.NextSequence) return false;
        }
        return true;
    }
}
