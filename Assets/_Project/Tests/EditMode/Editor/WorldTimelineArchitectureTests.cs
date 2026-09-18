using NUnit.Framework;

public sealed class WorldTimelineArchitectureTests
{
    [Test]
    public void TimelineIdsUseOrdinalValueEquality()
    {
        WorldTimelineId first = new WorldTimelineId("main");
        WorldTimelineId second = new WorldTimelineId("main");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(first == second, Is.True);
    }

    [Test]
    public void InvalidTimelineIdIsRejectedAsStructuredFailure()
    {
        bool created = WorldTimelineId.TryCreate(
            "   ",
            out WorldTimelineId timelineId,
            out WorldTimelineFailure failure);

        Assert.That(created, Is.False);
        Assert.That(timelineId, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(WorldTimelineFailureCode.InvalidTimelineId));
    }

    [Test]
    public void RootTimelineDescriptorHasNoParentAndRetainsHead()
    {
        WorldTimelineDescriptor root = CreateRoot("main", 54000L, 10241L);

        Assert.That(root.IsRoot, Is.True);
        Assert.That(root.ParentTimelineId, Is.Null);
        Assert.That(root.ForkAbsoluteDay, Is.Null);
        Assert.That(root.HeadAbsoluteDay, Is.EqualTo(54000L));
        Assert.That(root.HeadRevision, Is.EqualTo(new WorldRevision(10241L)));
    }

    [Test]
    public void ForkDescriptorCapturesParentForkAndChildHead()
    {
        WorldTimelineDescriptor parent = CreateRoot("main", 54000L, 10241L);
        bool created = WorldTimelineDescriptor.TryCreateFork(
            parent,
            new WorldTimelineId("alt-001"),
            865L,
            new WorldRevision(900L),
            865L,
            new WorldRevision(900L),
            out WorldTimelineDescriptor child,
            out WorldTimelineFailure failure);

        Assert.That(created, Is.True, failure.ToString());
        Assert.That(child.IsRoot, Is.False);
        Assert.That(child.ParentTimelineId, Is.EqualTo(parent.TimelineId));
        Assert.That(child.ForkAbsoluteDay, Is.EqualTo(865L));
        Assert.That(child.ForkRevision, Is.EqualTo(new WorldRevision(900L)));
        Assert.That(child.HeadAbsoluteDay, Is.EqualTo(865L));
    }

    [Test]
    public void ForkPlanningDoesNotMutateParentDescriptor()
    {
        WorldTimelineDescriptor parent = CreateRoot("main", 54000L, 10241L);
        WorldCheckpointDescriptor checkpoint = CreateCheckpoint("main-800", "main", 800L, 800L);

        bool planned = WorldForkPlanner.TryCreatePlan(
            parent,
            new[] { checkpoint },
            new WorldTimelineId("alt-001"),
            865L,
            out WorldForkPlan plan,
            out WorldTimelineFailure failure);

        Assert.That(planned, Is.True, failure.ToString());
        Assert.That(plan.RequiresReplay, Is.True);
        Assert.That(plan.ReplayStartAbsoluteDay, Is.EqualTo(801L));
        Assert.That(parent.HeadAbsoluteDay, Is.EqualTo(54000L));

        bool childCreated = WorldForkPlanner.TryCreateChildTimelineDescriptor(
            plan,
            new WorldRevision(11000L),
            out WorldTimelineDescriptor child,
            out failure);

        Assert.That(childCreated, Is.True, failure.ToString());
        Assert.That(child.ParentTimelineId, Is.EqualTo(parent.TimelineId));
        Assert.That(child.HeadAbsoluteDay, Is.EqualTo(865L));
        Assert.That(parent.Equals(CreateRoot("main", 54000L, 10241L)), Is.True);
    }

    [Test]
    public void ForkAfterParentHeadIsRejected()
    {
        WorldTimelineDescriptor parent = CreateRoot("main", 54000L, 10241L);
        bool created = WorldTimelineDescriptor.TryCreateFork(
            parent,
            new WorldTimelineId("alt-001"),
            54001L,
            null,
            54001L,
            new WorldRevision(10242L),
            out WorldTimelineDescriptor child,
            out WorldTimelineFailure failure);

        Assert.That(created, Is.False);
        Assert.That(child, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(WorldTimelineFailureCode.InvalidForkDay));
    }

    [Test]
    public void RestorePlannerSelectsNearestCheckpointOnOrBeforeTarget()
    {
        WorldTimelineDescriptor timeline = CreateRoot("main", 54000L, 10241L);
        WorldCheckpointDescriptor checkpoint = CreateCheckpoint("main-800", "main", 800L, 800L);

        bool planned = WorldRestorePlanner.TryCreatePlan(
            timeline,
            new[]
            {
                CreateCheckpoint("main-100", "main", 100L, 100L),
                checkpoint,
                CreateCheckpoint("main-1000", "main", 1000L, 1000L),
                CreateCheckpoint("alt-700", "alt-001", 700L, 700L)
            },
            865L,
            out WorldRestorePlan plan,
            out WorldTimelineFailure failure);

        Assert.That(planned, Is.True, failure.ToString());
        Assert.That(plan.BaseCheckpoint.CheckpointId, Is.EqualTo(checkpoint.CheckpointId));
        Assert.That(plan.ReplayStartAbsoluteDay, Is.EqualTo(801L));
        Assert.That(plan.ReplayEndAbsoluteDay, Is.EqualTo(865L));
        Assert.That(plan.RequiresReplay, Is.True);
    }

    [Test]
    public void FutureCheckpointIsIgnoredByRestorePlanner()
    {
        WorldTimelineDescriptor timeline = CreateRoot("main", 54000L, 10241L);
        bool planned = WorldRestorePlanner.TryCreatePlan(
            timeline,
            new[] { CreateCheckpoint("main-1000", "main", 1000L, 1000L) },
            865L,
            out WorldRestorePlan plan,
            out WorldTimelineFailure failure);

        Assert.That(planned, Is.False);
        Assert.That(plan, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(WorldTimelineFailureCode.CheckpointNotFound));
    }

    [Test]
    public void CheckpointOrderingDoesNotChangeRestorePlan()
    {
        WorldTimelineDescriptor timeline = CreateRoot("main", 54000L, 10241L);
        WorldCheckpointDescriptor day800 = CreateCheckpoint("main-800", "main", 800L, 800L);
        WorldCheckpointDescriptor day100 = CreateCheckpoint("main-100", "main", 100L, 100L);
        WorldCheckpointDescriptor day500 = CreateCheckpoint("main-500", "main", 500L, 500L);

        Assert.That(WorldRestorePlanner.TryCreatePlan(
            timeline,
            new[] { day800, day100, day500 },
            865L,
            out WorldRestorePlan first,
            out WorldTimelineFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(WorldRestorePlanner.TryCreatePlan(
            timeline,
            new[] { day500, day800, day100 },
            865L,
            out WorldRestorePlan second,
            out WorldTimelineFailure secondFailure), Is.True, secondFailure.ToString());

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void MultipleForksRemainIndependentInTimelineStore()
    {
        InMemoryWorldTimelineStore store = new InMemoryWorldTimelineStore();
        WorldTimelineDescriptor main = CreateRoot("main", 54000L, 10241L);
        Assert.That(store.TryCreateRoot(main, out WorldTimelineFailure failure), Is.True, failure.ToString());

        WorldTimelineDescriptor first = CreateFork(main, "alt-001", 865L, 900L);
        WorldTimelineDescriptor second = CreateFork(main, "alt-002", 865L, 901L);
        Assert.That(store.TryCreateFork(first, out failure), Is.True, failure.ToString());
        Assert.That(store.TryCreateFork(second, out failure), Is.True, failure.ToString());

        Assert.That(store.TryUpdateHead(first.TimelineId, 1000L, new WorldRevision(1100L), out failure), Is.True, failure.ToString());
        Assert.That(store.TryGet(second.TimelineId, out WorldTimelineDescriptor unchangedSecond, out failure), Is.True, failure.ToString());
        Assert.That(unchangedSecond.HeadAbsoluteDay, Is.EqualTo(865L));
        Assert.That(store.TryGet(main.TimelineId, out WorldTimelineDescriptor unchangedMain, out failure), Is.True, failure.ToString());
        Assert.That(unchangedMain.HeadAbsoluteDay, Is.EqualTo(54000L));
        Assert.That(store.Enumerate().Count, Is.EqualTo(3));
    }

    [Test]
    public void NestedForkUsesAnyExistingTimelineAsParent()
    {
        InMemoryWorldTimelineStore store = new InMemoryWorldTimelineStore();
        WorldTimelineDescriptor main = CreateRoot("main", 54000L, 10241L);
        WorldTimelineDescriptor first = CreateFork(main, "alt-001", 865L, 900L);
        Assert.That(first.TryAdvanceHead(
            2000L,
            new WorldRevision(2000L),
            out WorldTimelineDescriptor advancedFirst,
            out WorldTimelineFailure advanceFailure), Is.True, advanceFailure.ToString());
        first = advancedFirst;
        WorldTimelineDescriptor nested = CreateFork(first, "alt-001-b", 1200L, 1300L);

        Assert.That(store.TryCreateRoot(main, out WorldTimelineFailure failure), Is.True, failure.ToString());
        Assert.That(store.TryCreateFork(first, out failure), Is.True, failure.ToString());
        Assert.That(store.TryCreateFork(nested, out failure), Is.True, failure.ToString());
        Assert.That(nested.ParentTimelineId, Is.EqualTo(first.TimelineId));
        Assert.That(nested.ForkAbsoluteDay, Is.EqualTo(1200L));
    }

    [Test]
    public void DuplicateTimelineIsRejectedByInMemoryStore()
    {
        InMemoryWorldTimelineStore store = new InMemoryWorldTimelineStore();
        Assert.That(store.TryCreateRoot(CreateRoot("main"), out WorldTimelineFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(store.TryCreateRoot(CreateRoot("main"), out WorldTimelineFailure secondFailure), Is.False);
        Assert.That(secondFailure.Code, Is.EqualTo(WorldTimelineFailureCode.DuplicateTimeline));
    }

    [Test]
    public void HistoricalQueryProducesReadOnlyRestorePlan()
    {
        InMemoryWorldTimelineStore timelines = new InMemoryWorldTimelineStore();
        WorldTimelineDescriptor main = CreateRoot("main", 54000L, 10241L);
        Assert.That(timelines.TryCreateRoot(main, out WorldTimelineFailure failure), Is.True, failure.ToString());

        InMemoryWorldCheckpointStore checkpoints = new InMemoryWorldCheckpointStore(timelines);
        WorldCheckpointDescriptor checkpoint = CreateCheckpoint("main-800", "main", 800L, 800L);
        Assert.That(checkpoints.TryStore(checkpoint, out failure), Is.True, failure.ToString());
        PlannedHistoricalWorldStateProvider provider = new PlannedHistoricalWorldStateProvider(timelines, checkpoints);

        bool planned = provider.TryCreateRestorePlan(
            new HistoricalWorldStateQuery(new WorldTimelineId("main"), 865L),
            out WorldRestorePlan plan,
            out failure);

        Assert.That(planned, Is.True, failure.ToString());
        Assert.That(plan.BaseCheckpoint.Equals(checkpoint), Is.True);
        Assert.That(timelines.TryGet(new WorldTimelineId("main"), out WorldTimelineDescriptor after, out failure), Is.True, failure.ToString());
        Assert.That(after, Is.EqualTo(main));
    }

    [Test]
    public void InvalidSchemaVersionIsRejected()
    {
        bool created = WorldSchemaVersion.TryCreate(
            -1,
            0,
            out WorldSchemaVersion version,
            out WorldTimelineFailure failure);

        Assert.That(created, Is.False);
        Assert.That(version, Is.EqualTo(default(WorldSchemaVersion)));
        Assert.That(failure.Code, Is.EqualTo(WorldTimelineFailureCode.InvalidSchemaVersion));
    }

    [Test]
    public void NegativeRevisionAndDayAreRejected()
    {
        bool revisionCreated = WorldRevision.TryCreate(
            -1L,
            out WorldRevision revision,
            out WorldTimelineFailure revisionFailure);
        bool timelineCreated = WorldTimelineDescriptor.TryCreateRoot(
            new WorldTimelineId("main"),
            -1L,
            WorldRevision.Zero,
            out WorldTimelineDescriptor timeline,
            out WorldTimelineFailure dayFailure);

        Assert.That(revisionCreated, Is.False);
        Assert.That(revisionFailure.Code, Is.EqualTo(WorldTimelineFailureCode.InvalidRevision));
        Assert.That(timelineCreated, Is.False);
        Assert.That(timeline, Is.Null);
        Assert.That(dayFailure.Code, Is.EqualTo(WorldTimelineFailureCode.InvalidAbsoluteDay));
    }

    [Test]
    public void SameInputsProduceEqualRestoreAndForkPlans()
    {
        WorldTimelineDescriptor parent = CreateRoot("main", 54000L, 10241L);
        WorldCheckpointDescriptor checkpoint = CreateCheckpoint("main-800", "main", 800L, 800L);

        Assert.That(WorldRestorePlanner.TryCreatePlan(
            parent,
            new[] { checkpoint },
            865L,
            out WorldRestorePlan firstRestore,
            out WorldTimelineFailure firstRestoreFailure), Is.True, firstRestoreFailure.ToString());
        Assert.That(WorldRestorePlanner.TryCreatePlan(
            parent,
            new[] { checkpoint },
            865L,
            out WorldRestorePlan secondRestore,
            out WorldTimelineFailure secondRestoreFailure), Is.True, secondRestoreFailure.ToString());
        Assert.That(firstRestore, Is.EqualTo(secondRestore));

        Assert.That(WorldForkPlanner.TryCreatePlan(
            parent,
            new[] { checkpoint },
            new WorldTimelineId("alt-001"),
            865L,
            out WorldForkPlan firstFork,
            out WorldTimelineFailure firstForkFailure), Is.True, firstForkFailure.ToString());
        Assert.That(WorldForkPlanner.TryCreatePlan(
            parent,
            new[] { checkpoint },
            new WorldTimelineId("alt-001"),
            865L,
            out WorldForkPlan secondFork,
            out WorldTimelineFailure secondForkFailure), Is.True, secondForkFailure.ToString());
        Assert.That(firstFork, Is.EqualTo(secondFork));
    }

    [Test]
    public void SerializedStateAndCheckpointPayloadAreImmutableEnvelopes()
    {
        byte[] source = { 1, 2, 3 };
        WorldSerializedState serialized = new WorldSerializedState(source);
        source[0] = 99;
        byte[] copied = serialized.CopyBytes();
        copied[1] = 88;

        WorldCheckpointPayload payload = new WorldCheckpointPayload(
            new WorldSchemaVersion(1, 0),
            new WorldTimelineId("main"),
            new WorldCheckpointId("main-0"),
            0L,
            WorldRevision.Zero,
            WorldRandomStateToken.Unavailable,
            serialized);

        Assert.That(serialized.CopyBytes(), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(payload.SerializedWorldState, Is.SameAs(serialized));
        Assert.That(payload.RandomState.IsAvailable, Is.False);
        Assert.That(payload.ConfigurationRevision.IsSpecified, Is.False);
    }

    private static WorldTimelineDescriptor CreateRoot(
        string id,
        long headDay = 54000L,
        long headRevision = 10241L)
    {
        bool created = WorldTimelineDescriptor.TryCreateRoot(
            new WorldTimelineId(id),
            headDay,
            new WorldRevision(headRevision),
            out WorldTimelineDescriptor descriptor,
            out WorldTimelineFailure failure);
        Assert.That(created, Is.True, failure.ToString());
        return descriptor;
    }

    private static WorldTimelineDescriptor CreateFork(
        WorldTimelineDescriptor parent,
        string id,
        long forkDay,
        long revision)
    {
        bool created = WorldTimelineDescriptor.TryCreateFork(
            parent,
            new WorldTimelineId(id),
            forkDay,
            new WorldRevision(revision),
            forkDay,
            new WorldRevision(revision),
            out WorldTimelineDescriptor descriptor,
            out WorldTimelineFailure failure);
        Assert.That(created, Is.True, failure.ToString());
        return descriptor;
    }

    private static WorldCheckpointDescriptor CreateCheckpoint(
        string checkpointId,
        string timelineId,
        long day,
        long revision)
    {
        return new WorldCheckpointDescriptor(
            new WorldCheckpointId(checkpointId),
            new WorldTimelineId(timelineId),
            day,
            new WorldRevision(revision),
            new WorldSchemaVersion(1, 0));
    }
}
