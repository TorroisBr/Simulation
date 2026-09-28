using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Freezes legacy merchant pairing into P18-D boundary descriptors and commits
/// one retained source snapshot before routing any recipient-owned share batch.
/// </summary>
public sealed class CommercialKnowledgeSharingDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OwnerId = "commercial-knowledge-sharing";
    private const string SnapshotStepId = "commercial-share-snapshot";
    private const string SnapshotKind = "commercial-sharing.snapshot";
    private const string EdgeKind = "commercial-sharing.edge";
    private const string Version = "1";

    private readonly Func<IReadOnlyList<NpcRuntime>> roster;
    private readonly Func<string, NpcRuntime> resolveActor;
    private readonly CommercialKnowledgeSharingSystem owner;

    public CommercialKnowledgeSharingDailyBoundaryStepProvider(
        Func<IReadOnlyList<NpcRuntime>> roster,
        Func<string, NpcRuntime> resolveActor,
        CommercialKnowledgeSharingSystem owner)
    {
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
        this.resolveActor = resolveActor ?? throw new ArgumentNullException(nameof(resolveActor));
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0) return false;
        IReadOnlyList<NpcRuntime> current = roster();
        if (current == null) return false;

        List<NpcRuntime> merchants = new List<NpcRuntime>();
        HashSet<string> rosterIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in current)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !rosterIds.Add(npc.RuntimeId)) return false;
            if (IsEligibleMerchant(npc)) merchants.Add(npc);
        }
        merchants.Sort(CompareMerchants);

        List<Participant> participants = new List<Participant>(merchants.Count);
        foreach (NpcRuntime merchant in merchants)
        {
            CityRuntime city = merchant.CurrentCity;
            if (city == null || city.Location == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || string.IsNullOrWhiteSpace(city.Location.RuntimeId)) return false;
            participants.Add(new Participant
            {
                runtimeId = merchant.RuntimeId,
                personId = merchant.PersonId?.Value ?? string.Empty,
                cityRuntimeId = city.RuntimeId,
                locationRuntimeId = city.Location.RuntimeId
            });
        }

        List<Edge> edges = new List<Edge>();
        int groupStart = 0;
        while (groupStart < merchants.Count)
        {
            CityRuntime city = merchants[groupStart].CurrentCity;
            int groupEnd = groupStart + 1;
            while (groupEnd < merchants.Count && ReferenceEquals(merchants[groupEnd].CurrentCity, city)) groupEnd++;
            int groupCount = groupEnd - groupStart;
            int rotation = (int)(operation.AbsoluteDay % groupCount);
            for (int pairOffset = 0; pairOffset + 1 < groupCount; pairOffset += 2)
            {
                NpcRuntime first = merchants[groupStart + (rotation + pairOffset) % groupCount];
                NpcRuntime second = merchants[groupStart + (rotation + pairOffset + 1) % groupCount];
                edges.Add(new Edge { senderRuntimeId = first.RuntimeId, receiverRuntimeId = second.RuntimeId });
                edges.Add(new Edge { senderRuntimeId = second.RuntimeId, receiverRuntimeId = first.RuntimeId });
            }
            groupStart = groupEnd;
        }

        if (edges.Count > int.MaxValue - firstOrdinal - 1) return false;
        Plan plan = new Plan { participants = participants.ToArray(), edges = edges.ToArray() };
        List<BoundaryContinuationStep> created = new List<BoundaryContinuationStep>(edges.Count + 1)
        {
            new BoundaryContinuationStep(firstOrdinal, SnapshotStepId, OwnerId, SnapshotKind,
                Version, string.Empty, JsonUtility.ToJson(plan))
        };
        for (int i = 0; i < edges.Count; i++)
        {
            Edge edge = edges[i];
            created.Add(new BoundaryContinuationStep(firstOrdinal + i + 1,
                "commercial-share-edge:" + i.ToString(CultureInfo.InvariantCulture), OwnerId,
                EdgeKind, Version, string.Empty, JsonUtility.ToJson(edge)));
        }
        steps = created.AsReadOnly();
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) => step != null
        && step.OwnerId == OwnerId
        && (step.StepId == SnapshotStepId || step.StepId.StartsWith("commercial-share-edge:", StringComparison.Ordinal));

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (manifest == null || step == null || !OwnsStep(step) || step.OwnerId != OwnerId
            || step.OperationVersion != Version) return false;

        if (step.StepId == SnapshotStepId)
        {
            if (step.OperationKind != SnapshotKind || !TryRead(step.Payload, out Plan plan)
                || plan.participants == null || plan.edges == null) return false;
            List<NpcRuntime> frozenMerchants = new List<NpcRuntime>(plan.participants.Length);
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (Participant participant in plan.participants)
            {
                NpcRuntime merchant = ResolveParticipant(participant);
                if (merchant == null || !unique.Add(merchant.RuntimeId)) return false;
                frozenMerchants.Add(merchant);
            }
            string planFingerprint = owner.CreatePlanFingerprint(manifest, step);
            if (!owner.TryPreparePhaseSnapshot(manifest, planFingerprint, frozenMerchants,
                out CommercialSharingPhaseSnapshotCommit phaseSnapshot)) return false;
            prepared = new PhaseSnapshotCommit(phaseSnapshot);
            failure = TimelineFailure.None;
            return true;
        }

        if (step.OperationKind != EdgeKind || !TryRead(step.Payload, out Edge edge)
            || string.IsNullOrWhiteSpace(edge.senderRuntimeId) || string.IsNullOrWhiteSpace(edge.receiverRuntimeId)) return false;
        BoundaryContinuationStep snapshotStep = null;
        foreach (BoundaryContinuationStep candidate in manifest.Steps)
            if (candidate.StepId == SnapshotStepId) { snapshotStep = candidate; break; }
        if (snapshotStep == null || snapshotStep.Ordinal >= step.Ordinal
            || !TryRead(snapshotStep.Payload, out Plan frozenPlan)) return false;
        if (!TryParseEdgeOrdinal(step.StepId, out int edgeOrdinal)
            || frozenPlan.edges == null || edgeOrdinal < 0 || edgeOrdinal >= frozenPlan.edges.Length
            || frozenPlan.edges[edgeOrdinal] == null
            || step.Ordinal != snapshotStep.Ordinal + edgeOrdinal + 1
            || !string.Equals(frozenPlan.edges[edgeOrdinal].senderRuntimeId, edge.senderRuntimeId, StringComparison.Ordinal)
            || !string.Equals(frozenPlan.edges[edgeOrdinal].receiverRuntimeId, edge.receiverRuntimeId, StringComparison.Ordinal)) return false;
        string fingerprint = owner.CreatePlanFingerprint(manifest, snapshotStep);
        if (!owner.TryResolvePhaseSnapshot(manifest.BoundaryOccurrenceId, fingerprint,
                out CommercialSharingPhaseSnapshot sourceSnapshot)
            || !sourceSnapshot.TryGetSender(edge.senderRuntimeId, out CommercialSharingSenderSnapshot sender)) return false;

        Participant senderParticipant = FindParticipant(frozenPlan, edge.senderRuntimeId);
        Participant receiverParticipant = FindParticipant(frozenPlan, edge.receiverRuntimeId);
        NpcRuntime senderNpc = ResolveParticipant(senderParticipant);
        NpcRuntime receiverNpc = ResolveParticipant(receiverParticipant);
        if (senderNpc == null || receiverNpc == null || senderNpc.RuntimeId == receiverNpc.RuntimeId
            || !string.Equals(sender.SenderPersonId, senderParticipant.personId ?? string.Empty, StringComparison.Ordinal)) return false;
        CommercialKnowledgeShareBatch batch;
        try
        {
            batch = new CommercialKnowledgeShareBatch(manifest.BoundaryOccurrenceId,
                senderNpc.RuntimeId, senderParticipant.personId, receiverNpc.RuntimeId,
                receiverParticipant.personId, sourceSnapshot.Fingerprint, manifest.AbsoluteDay,
                owner.MaxSuccessfulUpdates, sender.Values);
        }
        catch (ArgumentException) { return false; }
        if (!receiverNpc.CommercialKnowledge.TryPrepareShareBatch(batch,
            out CommercialKnowledgeShareBatchCommit batchCommit)) return false;
        prepared = new RecipientCommit(batchCommit);
        failure = TimelineFailure.None;
        return true;
    }

    private NpcRuntime ResolveParticipant(Participant participant)
    {
        if (participant == null || string.IsNullOrWhiteSpace(participant.runtimeId)) return null;
        NpcRuntime npc = resolveActor(participant.runtimeId);
        if (npc == null || npc.RuntimeId != participant.runtimeId
            || !string.Equals(npc.PersonId?.Value ?? string.Empty, participant.personId ?? string.Empty, StringComparison.Ordinal)
            ) return null;
        return npc;
    }

    private static Participant FindParticipant(Plan plan, string runtimeId)
    {
        if (plan?.participants == null) return null;
        foreach (Participant participant in plan.participants)
            if (participant != null && participant.runtimeId == runtimeId) return participant;
        return null;
    }

    private static bool IsEligibleMerchant(NpcRuntime npc) => npc != null && npc.IsAlive
        && !npc.IsTraveling && npc.CurrentCity != null && npc.NpcData != null
        && npc.NpcData.job != null && npc.NpcData.job.jobType == NpcJobType.Merchant;

    private static int CompareMerchants(NpcRuntime left, NpcRuntime right)
    {
        int location = string.CompareOrdinal(left.CurrentCity?.Location?.RuntimeId ?? string.Empty,
            right.CurrentCity?.Location?.RuntimeId ?? string.Empty);
        return location != 0 ? location : string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
    }

    private static bool TryRead<T>(string json, out T value) where T : class
    {
        value = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { value = JsonUtility.FromJson<T>(json); return value != null; }
        catch (ArgumentException) { return false; }
    }

    private static bool TryParseEdgeOrdinal(string stepId, out int ordinal)
    {
        ordinal = -1;
        const string prefix = "commercial-share-edge:";
        return stepId != null && stepId.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(stepId.Substring(prefix.Length), NumberStyles.None,
                CultureInfo.InvariantCulture, out ordinal) && ordinal >= 0;
    }

    [Serializable] private sealed class Plan { public Participant[] participants; public Edge[] edges; }
    [Serializable] private sealed class Participant
    {
        public string runtimeId;
        public string personId;
        public string cityRuntimeId;
        public string locationRuntimeId;
    }
    [Serializable] private sealed class Edge { public string senderRuntimeId; public string receiverRuntimeId; }

    private sealed class PhaseSnapshotCommit : IBoundaryContinuationStepCommit
    {
        private readonly CommercialSharingPhaseSnapshotCommit commit;
        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
        public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
        public PhaseSnapshotCommit(CommercialSharingPhaseSnapshotCommit commit) { this.commit = commit; }
        public bool TryCommit(out TimelineFailure failure)
        {
            bool success = commit.TryCommit(out _);
            failure = success ? TimelineFailure.None : TimelineFailure.ContinuationFailed;
            return success;
        }
    }

    private sealed class RecipientCommit : IBoundaryContinuationStepCommit
    {
        private readonly CommercialKnowledgeShareBatchCommit commit;
        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
        public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
        public RecipientCommit(CommercialKnowledgeShareBatchCommit commit) { this.commit = commit; }
        public bool TryCommit(out TimelineFailure failure)
        {
            bool success = commit.TryCommit(out _);
            failure = success ? TimelineFailure.None : TimelineFailure.ContinuationFailed;
            return success;
        }
    }
}
