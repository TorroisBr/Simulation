using System;
using System.Collections.Generic;

public static class WorldRestorePlanner
{
    public static bool TryCreatePlan(
        WorldTimelineDescriptor timeline,
        IEnumerable<WorldCheckpointDescriptor> checkpoints,
        long targetAbsoluteDay,
        out WorldRestorePlan plan,
        out WorldTimelineFailure failure)
    {
        plan = null;

        if (timeline == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "Restore planning requires a timeline descriptor.");
            return false;
        }

        if (targetAbsoluteDay < 0L)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidAbsoluteDay,
                "Restore target day cannot be negative.");
            return false;
        }

        if (targetAbsoluteDay > timeline.HeadAbsoluteDay)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.TargetAfterTimelineHead,
                "Restore target cannot be after the timeline head.");
            return false;
        }

        if (TrySelectNearestCheckpoint(
            timeline,
            checkpoints,
            targetAbsoluteDay,
            out WorldCheckpointDescriptor checkpoint,
            out failure) == false)
        {
            return false;
        }

        plan = new WorldRestorePlan(timeline.TimelineId, targetAbsoluteDay, checkpoint);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public static bool TrySelectNearestCheckpoint(
        WorldTimelineDescriptor timeline,
        IEnumerable<WorldCheckpointDescriptor> checkpoints,
        long targetAbsoluteDay,
        out WorldCheckpointDescriptor selected,
        out WorldTimelineFailure failure)
    {
        selected = null;

        if (timeline == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "Checkpoint selection requires a timeline descriptor.");
            return false;
        }

        if (targetAbsoluteDay < 0L)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidAbsoluteDay,
                "Checkpoint selection target day cannot be negative.");
            return false;
        }

        if (checkpoints != null)
        {
            foreach (WorldCheckpointDescriptor checkpoint in checkpoints)
            {
                if (checkpoint == null)
                {
                    failure = WorldTimelineFailure.Create(
                        WorldTimelineFailureCode.InvalidCheckpointDescriptor,
                        "Checkpoint collection contains a null descriptor.");
                    return false;
                }

                if (checkpoint.TimelineId != timeline.TimelineId
                    || checkpoint.AbsoluteDay > targetAbsoluteDay)
                {
                    continue;
                }

                if (selected == null || IsPreferred(checkpoint, selected))
                {
                    selected = checkpoint;
                }
            }
        }

        if (selected == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.CheckpointNotFound,
                "No checkpoint exists on or before the requested target day for this timeline.");
            return false;
        }

        failure = WorldTimelineFailure.None;
        return true;
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

public static class WorldForkPlanner
{
    public static bool TryCreatePlan(
        WorldTimelineDescriptor parentTimeline,
        IEnumerable<WorldCheckpointDescriptor> checkpoints,
        WorldTimelineId newTimelineId,
        long forkAbsoluteDay,
        out WorldForkPlan plan,
        out WorldTimelineFailure failure)
    {
        plan = null;

        if (parentTimeline == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "Fork planning requires a parent timeline descriptor.");
            return false;
        }

        if (newTimelineId == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Fork planning requires a new timeline ID.");
            return false;
        }

        if (newTimelineId == parentTimeline.TimelineId)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.ChildTimelineSameAsParent,
                "A fork must create a timeline with a different ID from its parent.");
            return false;
        }

        if (forkAbsoluteDay < 0L || forkAbsoluteDay > parentTimeline.HeadAbsoluteDay)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidForkDay,
                "Fork day must be between zero and the parent head day.");
            return false;
        }

        if (WorldRestorePlanner.TrySelectNearestCheckpoint(
            parentTimeline,
            checkpoints,
            forkAbsoluteDay,
            out WorldCheckpointDescriptor checkpoint,
            out failure) == false)
        {
            return false;
        }

        WorldRevision? forkRevision = checkpoint.AbsoluteDay == forkAbsoluteDay
            ? checkpoint.Revision
            : (WorldRevision?)null;
        long replayStart = checkpoint.AbsoluteDay < forkAbsoluteDay
            ? checkpoint.AbsoluteDay + 1L
            : forkAbsoluteDay;

        plan = new WorldForkPlan(
            parentTimeline,
            newTimelineId,
            forkAbsoluteDay,
            forkRevision,
            checkpoint,
            replayStart);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public static bool TryCreateChildTimelineDescriptor(
        WorldForkPlan plan,
        WorldRevision childHeadRevision,
        out WorldTimelineDescriptor childTimeline,
        out WorldTimelineFailure failure)
    {
        childTimeline = null;

        if (plan == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "A child descriptor requires a fork plan.");
            return false;
        }

        return WorldTimelineDescriptor.TryCreateFork(
            plan.ParentTimeline,
            plan.NewTimelineId,
            plan.ForkAbsoluteDay,
            plan.ForkRevision,
            plan.ForkAbsoluteDay,
            childHeadRevision,
            out childTimeline,
            out failure);
    }
}

public sealed class PlannedHistoricalWorldStateProvider : IHistoricalWorldStateProvider
{
    private readonly IWorldTimelineStore timelineStore;
    private readonly IWorldCheckpointStore checkpointStore;

    public PlannedHistoricalWorldStateProvider(
        IWorldTimelineStore timelineStore,
        IWorldCheckpointStore checkpointStore)
    {
        this.timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        this.checkpointStore = checkpointStore ?? throw new ArgumentNullException(nameof(checkpointStore));
    }

    public bool TryCreateRestorePlan(
        HistoricalWorldStateQuery query,
        out WorldRestorePlan plan,
        out WorldTimelineFailure failure)
    {
        plan = null;

        if (query == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Historical state query cannot be null.");
            return false;
        }

        if (timelineStore.TryGet(
            query.TimelineId,
            out WorldTimelineDescriptor timeline,
            out failure) == false)
        {
            return false;
        }

        return WorldRestorePlanner.TryCreatePlan(
            timeline,
            checkpointStore.GetForTimeline(query.TimelineId),
            query.AbsoluteDay,
            out plan,
            out failure);
    }
}
