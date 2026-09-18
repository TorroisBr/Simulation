using System;
using System.Collections.Generic;

public interface IWorldCheckpointStore
{
    bool TryStore(WorldCheckpointDescriptor checkpoint, out WorldTimelineFailure failure);

    bool TryGet(
        WorldCheckpointId checkpointId,
        out WorldCheckpointDescriptor checkpoint,
        out WorldTimelineFailure failure);

    IReadOnlyList<WorldCheckpointDescriptor> GetForTimeline(WorldTimelineId timelineId);

    bool TryFindNearest(
        WorldTimelineId timelineId,
        long targetAbsoluteDay,
        out WorldCheckpointDescriptor checkpoint,
        out WorldTimelineFailure failure);
}

public interface IWorldTimelineStore
{
    bool TryCreateRoot(WorldTimelineDescriptor descriptor, out WorldTimelineFailure failure);

    bool TryCreateFork(WorldTimelineDescriptor descriptor, out WorldTimelineFailure failure);

    bool TryGet(
        WorldTimelineId timelineId,
        out WorldTimelineDescriptor descriptor,
        out WorldTimelineFailure failure);

    bool TryUpdateHead(
        WorldTimelineId timelineId,
        long headAbsoluteDay,
        WorldRevision headRevision,
        out WorldTimelineFailure failure);

    IReadOnlyList<WorldTimelineDescriptor> Enumerate();
}

public sealed class InMemoryWorldTimelineStore : IWorldTimelineStore
{
    private readonly Dictionary<string, WorldTimelineDescriptor> descriptors =
        new Dictionary<string, WorldTimelineDescriptor>(StringComparer.Ordinal);

    public bool TryCreateRoot(WorldTimelineDescriptor descriptor, out WorldTimelineFailure failure)
    {
        if (descriptor == null || descriptor.IsRoot == false)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "Root creation requires a root timeline descriptor.");
            return false;
        }

        return TryAdd(descriptor, out failure);
    }

    public bool TryCreateFork(WorldTimelineDescriptor descriptor, out WorldTimelineFailure failure)
    {
        if (descriptor == null || descriptor.IsRoot == true)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "Fork creation requires a child timeline descriptor.");
            return false;
        }

        if (descriptors.TryGetValue(descriptor.ParentTimelineId.Value, out WorldTimelineDescriptor parent) == false)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.TimelineNotFound,
                "The fork parent timeline does not exist in the store.");
            return false;
        }

        if (descriptor.ForkAbsoluteDay > parent.HeadAbsoluteDay
            || (descriptor.ForkAbsoluteDay == parent.HeadAbsoluteDay
                && descriptor.ForkRevision.HasValue == true
                && descriptor.ForkRevision.Value > parent.HeadRevision))
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidForkDay,
                "Fork descriptor is after the current parent head.");
            return false;
        }

        return TryAdd(descriptor, out failure);
    }

    public bool TryGet(
        WorldTimelineId timelineId,
        out WorldTimelineDescriptor descriptor,
        out WorldTimelineFailure failure)
    {
        descriptor = null;

        if (timelineId == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Timeline lookup requires a timeline ID.");
            return false;
        }

        if (descriptors.TryGetValue(timelineId.Value, out descriptor) == false)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.TimelineNotFound,
                "Timeline is not present in the store.");
            return false;
        }

        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool TryUpdateHead(
        WorldTimelineId timelineId,
        long headAbsoluteDay,
        WorldRevision headRevision,
        out WorldTimelineFailure failure)
    {
        if (TryGet(timelineId, out WorldTimelineDescriptor current, out failure) == false)
        {
            return false;
        }

        if (current.TryAdvanceHead(
            headAbsoluteDay,
            headRevision,
            out WorldTimelineDescriptor updated,
            out failure) == false)
        {
            return false;
        }

        descriptors[timelineId.Value] = updated;
        failure = WorldTimelineFailure.None;
        return true;
    }

    public IReadOnlyList<WorldTimelineDescriptor> Enumerate()
    {
        List<WorldTimelineDescriptor> result = new List<WorldTimelineDescriptor>(descriptors.Values);
        result.Sort((left, right) => left.TimelineId.CompareTo(right.TimelineId));
        return result.AsReadOnly();
    }

    private bool TryAdd(WorldTimelineDescriptor descriptor, out WorldTimelineFailure failure)
    {
        if (descriptors.ContainsKey(descriptor.TimelineId.Value) == true)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.DuplicateTimeline,
                "Timeline ID already exists in the store.");
            return false;
        }

        descriptors.Add(descriptor.TimelineId.Value, descriptor);
        failure = WorldTimelineFailure.None;
        return true;
    }
}

public sealed class InMemoryWorldCheckpointStore : IWorldCheckpointStore
{
    private readonly Dictionary<string, WorldCheckpointDescriptor> checkpoints =
        new Dictionary<string, WorldCheckpointDescriptor>(StringComparer.Ordinal);
    private readonly IWorldTimelineStore timelineStore;

    public InMemoryWorldCheckpointStore(IWorldTimelineStore timelineStore = null)
    {
        this.timelineStore = timelineStore;
    }

    public bool TryStore(WorldCheckpointDescriptor checkpoint, out WorldTimelineFailure failure)
    {
        if (checkpoint == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidCheckpointDescriptor,
                "Cannot store a null checkpoint descriptor.");
            return false;
        }

        if (timelineStore != null
            && timelineStore.TryGet(
                checkpoint.TimelineId,
                out WorldTimelineDescriptor ignoredTimeline,
                out WorldTimelineFailure timelineFailure) == false)
        {
            failure = timelineFailure;
            return false;
        }

        if (checkpoints.ContainsKey(checkpoint.CheckpointId.Value) == true)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.DuplicateCheckpoint,
                "Checkpoint ID already exists in the store.");
            return false;
        }

        checkpoints.Add(checkpoint.CheckpointId.Value, checkpoint);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool TryGet(
        WorldCheckpointId checkpointId,
        out WorldCheckpointDescriptor checkpoint,
        out WorldTimelineFailure failure)
    {
        checkpoint = null;

        if (checkpointId == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidCheckpointId,
                "Checkpoint lookup requires a checkpoint ID.");
            return false;
        }

        if (checkpoints.TryGetValue(checkpointId.Value, out checkpoint) == false)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.CheckpointNotFound,
                "Checkpoint is not present in the store.");
            return false;
        }

        failure = WorldTimelineFailure.None;
        return true;
    }

    public IReadOnlyList<WorldCheckpointDescriptor> GetForTimeline(WorldTimelineId timelineId)
    {
        if (timelineId == null)
        {
            return Array.Empty<WorldCheckpointDescriptor>();
        }

        List<WorldCheckpointDescriptor> result = new List<WorldCheckpointDescriptor>();
        foreach (WorldCheckpointDescriptor checkpoint in checkpoints.Values)
        {
            if (checkpoint.TimelineId == timelineId)
            {
                result.Add(checkpoint);
            }
        }

        result.Sort(CompareCheckpoints);
        return result.AsReadOnly();
    }

    public bool TryFindNearest(
        WorldTimelineId timelineId,
        long targetAbsoluteDay,
        out WorldCheckpointDescriptor checkpoint,
        out WorldTimelineFailure failure)
    {
        checkpoint = null;

        if (timelineId == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Checkpoint selection requires a timeline ID.");
            return false;
        }

        if (targetAbsoluteDay < 0L)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidAbsoluteDay,
                "Checkpoint selection target day cannot be negative.");
            return false;
        }

        foreach (WorldCheckpointDescriptor candidate in GetForTimeline(timelineId))
        {
            if (candidate.AbsoluteDay > targetAbsoluteDay)
            {
                continue;
            }

            if (checkpoint == null || IsPreferred(candidate, checkpoint))
            {
                checkpoint = candidate;
            }
        }

        if (checkpoint == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.CheckpointNotFound,
                "No checkpoint exists on or before the requested target day.");
            return false;
        }

        failure = WorldTimelineFailure.None;
        return true;
    }

    private static int CompareCheckpoints(
        WorldCheckpointDescriptor left,
        WorldCheckpointDescriptor right)
    {
        int dayComparison = left.AbsoluteDay.CompareTo(right.AbsoluteDay);
        if (dayComparison != 0)
        {
            return dayComparison;
        }

        int revisionComparison = left.Revision.CompareTo(right.Revision);
        return revisionComparison != 0
            ? revisionComparison
            : left.CheckpointId.CompareTo(right.CheckpointId);
    }

    private static bool IsPreferred(
        WorldCheckpointDescriptor candidate,
        WorldCheckpointDescriptor current)
    {
        if (candidate.AbsoluteDay != current.AbsoluteDay)
        {
            return candidate.AbsoluteDay > current.AbsoluteDay;
        }

        if (candidate.Revision != current.Revision)
        {
            return candidate.Revision > current.Revision;
        }

        return candidate.CheckpointId.CompareTo(current.CheckpointId) < 0;
    }
}
