using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1GuardActionPlanningTests
{
    [Test]
    public void SelectedArrestGraphPlanningChoosesHighestBountyThenOrdinalRuntimeId()
    {
        ArrestFixture fixture = CreateArrestFixture(
            "npc/guard", "npc/target-z", "npc/target-high", "npc/target-a");
        fixture.AddWanted("npc/target-z", 80f);
        fixture.AddWanted("npc/target-high", 100f);
        fixture.AddWanted("npc/target-a", 80f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            fixture.Graph, fixture.Guard, fixture.ArrestAction, 12f,
            out DailyV1NpcRuntimeOwners highestBountyTarget, out float highestBountyUtility), Is.True);
        Assert.That(highestBountyTarget, Is.SameAs(fixture.Npcs["npc/target-high"]));
        Assert.That(highestBountyUtility, Is.EqualTo(80f));

        Assert.That(fixture.JusticeRecords.WantedRecords[1].TryResolve(), Is.True);
        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            fixture.Graph, fixture.Guard, fixture.ArrestAction, 12f,
            out DailyV1NpcRuntimeOwners tiedTarget, out float tiedUtility), Is.True);
        Assert.That(tiedTarget, Is.SameAs(fixture.Npcs["npc/target-a"]),
            "An equal bounty selects the ordinally smaller RuntimeId, independent of City roster order.");
        Assert.That(tiedUtility, Is.EqualTo(75f));
    }

    [Test]
    public void SelectedArrestGraphPlanningFiltersInvalidTargetsButPreservesActiveSentenceSelection()
    {
        ArrestFixture fixture = CreateArrestFixture(
            "npc/guard", "npc/eligible", "npc/no-warrant", "npc/resolved",
            "npc/sentenced", "npc/dead", "npc/traveling", "npc/hidden",
            "npc/arrested", "npc/wrong-city", "npc/absent-presence");
        fixture.AddWanted("npc/eligible", 20f);
        fixture.AddWanted("npc/resolved", 500f, resolved: true);
        DailyV1JusticeWantedRecordOwner sentencedWarrant = fixture.AddWanted("npc/sentenced", 450f);
        Assert.That(fixture.JusticeRecords.TryAddSentence(new DailyV1JusticeSentenceOwner(
            "npc/sentenced", fixture.City.RuntimeId, sentencedWarrant, remainingDays: 5)), Is.True);
        fixture.AddWanted("npc/dead", 400f);
        Assert.That(fixture.Npcs["npc/dead"].Lifecycle.TrySetLifeState(NpcLifeState.Dead), Is.True);
        fixture.AddWanted("npc/traveling", 350f);
        Assert.That(fixture.Npcs["npc/traveling"].Travel.TryInstallState(
            fixture.City.LocationRuntimeId, fixture.City.RuntimeId,
            "location/in-transit", null, 2, 3, null, false, null, null, 1L), Is.True);
        fixture.AddWanted("npc/hidden", 300f);
        Assert.That(fixture.Npcs["npc/hidden"].HiddenState.TrySetRemainingDays(2), Is.True);
        fixture.AddWanted("npc/arrested", 250f);
        Assert.That(fixture.Definitions.Statuses.TryGetValue(
            fixture.Definitions.Configuration.arrestedStatus,
            out DailyV1StatusDefinition arrestedStatus), Is.True);
        Assert.That(fixture.Npcs["npc/arrested"].Status.TryAdd(arrestedStatus), Is.True);
        fixture.AddWanted("npc/wrong-city", 200f, cityRuntimeId: "city/not-current");
        fixture.AddWanted("npc/absent-presence", 150f);
        Assert.That(fixture.City.NpcPresence.TryRemove("npc/absent-presence"), Is.True);
        fixture.AddWanted("npc/absent-from-graph", 1000f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            fixture.Graph, fixture.Guard, fixture.ArrestAction, 12f,
            out DailyV1NpcRuntimeOwners target, out float utility), Is.True);
        Assert.That(target, Is.SameAs(fixture.Npcs["npc/sentenced"]),
            "Legacy planning chooses by active local warrant and arrested status; Justice rejects an existing sentence later during execution.");
        Assert.That(utility, Is.EqualTo(167.5f));
    }

    [Test]
    public void SelectedArrestRejectsAlreadySentencedTargetInsideOperationWithoutWrites()
    {
        ArrestFixture fixture = CreateArrestFixture("npc/guard", "npc/sentenced");
        DailyV1NpcRuntimeOwners target = fixture.Npcs["npc/sentenced"];
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWanted(target.RuntimeId, 450f);
        DailyV1JusticeSentenceOwner existingSentence = new DailyV1JusticeSentenceOwner(
            target.RuntimeId, fixture.City.RuntimeId, warrant, remainingDays: 5);
        Assert.That(fixture.JusticeRecords.TryAddSentence(existingSentence), Is.True);

        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Definitions, fixture.ArrestAction, target.RuntimeId, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, "decision/arrest", null,
            null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Guard, fixture.ArrestAction, payload, null), Is.True);
        RecordingGuardBoundary boundary = new RecordingGuardBoundary(fixture.JusticeRecords);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedGuardArrestActionSlot(
            fixture.Graph, fixture.Guard, fixture.ArrestAction,
            new FixedActionRandom(0f), currentAbsoluteDay: 12L, boundary,
            out bool actionRollSucceeded,
            out DailyV1SelectedActionExecutionStage.SelectedGuardArrestActionResult result), Is.True);

        Assert.That(actionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.TargetRuntimeId, Is.EqualTo(target.RuntimeId));
        Assert.That(boundary.OperationCalls, Is.EqualTo(1));
        Assert.That(boundary.AddSentenceCalls, Is.Zero);
        Assert.That(boundary.StatusChangeCalls, Is.Zero);
        Assert.That(boundary.PublishCalls, Is.Zero);
        Assert.That(fixture.JusticeRecords.Sentences, Is.EqualTo(new[] { existingSentence }));
        Assert.That(fixture.JusticeRecords.Revision, Is.Zero);
    }

    [Test]
    public void SelectedArrestDoesNotRecheckGuardVisibilityAfterPlanning()
    {
        ArrestFixture fixture = CreateArrestFixture("npc/guard", "npc/target");
        DailyV1NpcRuntimeOwners target = fixture.Npcs["npc/target"];
        fixture.AddWanted(target.RuntimeId, 80f);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Definitions, fixture.ArrestAction, target.RuntimeId, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, "decision/arrest", null,
            null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Guard, fixture.ArrestAction, payload, null), Is.True);
        Assert.That(fixture.Definitions.Statuses.TryGetValue(
            fixture.Definitions.Configuration.hiddenStatus,
            out DailyV1StatusDefinition hiddenStatus), Is.True);
        Assert.That(fixture.Guard.Status.TryAdd(hiddenStatus), Is.True,
            "The guard becomes hidden after its action was selected.");
        RecordingGuardBoundary boundary = new RecordingGuardBoundary(fixture.JusticeRecords);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedGuardArrestActionSlot(
            fixture.Graph, fixture.Guard, fixture.ArrestAction,
            new FixedActionRandom(0f), 12L, boundary,
            out bool actionRollSucceeded,
            out DailyV1SelectedActionExecutionStage.SelectedGuardArrestActionResult result), Is.True);

        Assert.That(actionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True,
            "Legacy execution validates guard role and arrest target but does not recheck guard visibility.");
        Assert.That(boundary.OperationCalls, Is.EqualTo(1));
        Assert.That(boundary.AddSentenceCalls, Is.EqualTo(1));
        Assert.That(boundary.PublishCalls, Is.EqualTo(1));
    }

    [Test]
    public void SelectedArrestGraphPlanningRejectsNonExactGuardAndActionOwners()
    {
        ArrestFixture fixture = CreateArrestFixture("npc/guard", "npc/target");
        fixture.AddWanted("npc/target", 80f);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            fixture.Guard.RuntimeId, fixture.Definitions,
            fixture.Definitions.Npcs["MARCUS"], 0f,
            out DailyV1NpcRuntimeOwners sameIdDifferentOwner), Is.True);
        DailyV1EffectiveDefinitions foreignDefinitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition foreignArrestAction = ArrestAction(foreignDefinitions);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            fixture.Graph, sameIdDifferentOwner, fixture.ArrestAction, 12f,
            out DailyV1NpcRuntimeOwners foreignGuardTarget, out float foreignGuardUtility), Is.False);
        Assert.That(foreignGuardTarget, Is.Null);
        Assert.That(foreignGuardUtility, Is.Zero);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            fixture.Graph, fixture.Guard, foreignArrestAction, 12f,
            out DailyV1NpcRuntimeOwners foreignActionTarget, out float foreignActionUtility), Is.False);
        Assert.That(foreignActionTarget, Is.Null);
        Assert.That(foreignActionUtility, Is.Zero);
    }

    [Test]
    public void SelectedArrestPlanningMatchesPreviousOrderedTargetAndUtilityRule()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition guard = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-z", 80f),
            new DailyV1GuardArrestCandidate("guard-target-a", 80f),
            new DailyV1GuardArrestCandidate("guard-target-b", 80f),
            new DailyV1GuardArrestCandidate("guard-target-higher", 100f)
        }, 12f);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-z", 80f),
            new DailyV1GuardArrestCandidate("guard-target-a", 79.99999f)
        }, 12f);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-first", -10f),
            new DailyV1GuardArrestCandidate("guard-target-second", -20f)
        }, 100f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, action, new List<DailyV1GuardArrestCandidate>(), 12f,
            out int noTargetIndex, out float noTargetUtility), Is.True);
        Assert.That(noTargetIndex, Is.EqualTo(-1));
        Assert.That(noTargetUtility, Is.Zero,
            "The existing GuardSystem sets utility to zero when no arrest target is available.");
    }

    [Test]
    public void SelectedArrestPlanningRejectsForeignActorOrActionNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition guard = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);
        List<DailyV1GuardArrestCandidate> candidates = new List<DailyV1GuardArrestCandidate>
        {
            new DailyV1GuardArrestCandidate("guard-target", 80f)
        };

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, foreign.Npcs[guard.DefinitionId], action, candidates, 12f,
            out int foreignActorIndex, out float foreignActorUtility), Is.False);
        Assert.That(foreignActorIndex, Is.EqualTo(-1));
        Assert.That(foreignActorUtility, Is.Zero);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, foreign.Actions[action.DefinitionId], candidates, 12f,
            out int foreignActionIndex, out float foreignActionUtility), Is.False);
        Assert.That(foreignActionIndex, Is.EqualTo(-1));
        Assert.That(foreignActionUtility, Is.Zero);
    }

    private static DailyV1ActionDefinition FirstAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            return action;
        Assert.Fail("The selected fixture must contain an action node.");
        return null;
    }

    private static DailyV1ActionDefinition ArrestAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            if (action.Values.actionType?.Value == DailyV1NpcDefinitionRules.ArrestActionTypeValue)
                return action;
        Assert.Fail("The selected fixture must contain an Arrest action node.");
        return null;
    }

    private static ArrestFixture CreateArrestFixture(params string[] npcRuntimeIds)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition cityDefinition = FirstCity(definitions);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city/arrest", "location/arrest", definitions, cityDefinition, null,
            out DailyV1CityRuntimeOwners city), Is.True);

        DailyV1JusticeRecordsOwner justiceRecords = new DailyV1JusticeRecordsOwner();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        Dictionary<string, DailyV1NpcRuntimeOwners> npcsByRuntimeId =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(System.StringComparer.Ordinal);
        DailyV1NpcDefinition targetDefinition = FirstNpcExcept(definitions, "MARCUS");
        foreach (string runtimeId in npcRuntimeIds)
        {
            DailyV1NpcDefinition npcDefinition = string.Equals(runtimeId, "npc/guard",
                System.StringComparison.Ordinal)
                ? definitions.Npcs["MARCUS"]
                : targetDefinition;
            Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                runtimeId, definitions, npcDefinition, 0f,
                out DailyV1NpcRuntimeOwners npc), Is.True);
            if (!string.Equals(runtimeId, "npc/guard", System.StringComparison.Ordinal))
                foreach (DailyV1StatusDefinition status in new List<DailyV1StatusDefinition>(npc.Status.Statuses))
                    Assert.That(npc.Status.TryRemove(status), Is.True);
            Assert.That(npc.Travel.TryInstallState(
                city.LocationRuntimeId, city.RuntimeId, null, null, 0, 0,
                null, false, null, null, 0L), Is.True);
            Assert.That(city.NpcPresence.TryAdd(runtimeId), Is.True);
            Assert.That(npcsByRuntimeId.TryAdd(runtimeId, npc), Is.True);
            npcs.Add(npc);
        }

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { city }, npcs, null, justiceRecords,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return new ArrestFixture(definitions, graph, city, justiceRecords,
            npcsByRuntimeId["npc/guard"], ArrestAction(definitions), npcsByRuntimeId);
    }

    private static DailyV1CityDefinition FirstCity(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1CityDefinition city in definitions.Cities.Values)
            return city;
        Assert.Fail("The selected fixture must contain a City definition.");
        return null;
    }

    private static DailyV1NpcDefinition FirstNpcExcept(
        DailyV1EffectiveDefinitions definitions, string excludedDefinitionId)
    {
        foreach (DailyV1NpcDefinition npc in definitions.Npcs.Values)
            if (!string.Equals(npc.DefinitionId, excludedDefinitionId, System.StringComparison.Ordinal))
                return npc;
        Assert.Fail("The selected fixture must contain a non-guard NPC definition.");
        return null;
    }

    private sealed class ArrestFixture
    {
        internal DailyV1EffectiveDefinitions Definitions { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1CityRuntimeOwners City { get; }
        internal DailyV1JusticeRecordsOwner JusticeRecords { get; }
        internal DailyV1NpcRuntimeOwners Guard { get; }
        internal DailyV1ActionDefinition ArrestAction { get; }
        internal Dictionary<string, DailyV1NpcRuntimeOwners> Npcs { get; }

        internal ArrestFixture(
            DailyV1EffectiveDefinitions definitions,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeRecordsOwner justiceRecords,
            DailyV1NpcRuntimeOwners guard,
            DailyV1ActionDefinition arrestAction,
            Dictionary<string, DailyV1NpcRuntimeOwners> npcs)
        {
            Definitions = definitions;
            Graph = graph;
            City = city;
            JusticeRecords = justiceRecords;
            Guard = guard;
            ArrestAction = arrestAction;
            Npcs = npcs;
        }

        internal DailyV1JusticeWantedRecordOwner AddWanted(
            string targetRuntimeId,
            float bounty,
            bool resolved = false,
            string cityRuntimeId = null)
        {
            DailyV1JusticeWantedRecordOwner wanted = new DailyV1JusticeWantedRecordOwner(
                targetRuntimeId, cityRuntimeId ?? City.RuntimeId, bounty, sentenceDays: 1,
                resolved: resolved);
            Assert.That(JusticeRecords.TryAddWantedRecord(wanted), Is.True);
            return wanted;
        }
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal FixedActionRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L) => value;
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class RecordingGuardBoundary
        : DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary
    {
        internal int OperationCalls { get; private set; }
        internal int AddSentenceCalls { get; private set; }
        internal int StatusChangeCalls { get; private set; }
        internal int PublishCalls { get; private set; }
        public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }

        internal RecordingGuardBoundary(DailyV1JusticeRecordsOwner justiceRecordsOwner) =>
            JusticeRecordsOwner = justiceRecordsOwner;

        public bool ExecuteWithinOperation(System.Func<bool> operation)
        {
            OperationCalls++;
            return operation();
        }

        public bool TryAddSentence(DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city, DailyV1JusticeWantedRecordOwner warrant,
            DailyV1JusticeSentenceOwner sentence)
        {
            AddSentenceCalls++;
            return JusticeRecordsOwner.TryAddSentence(sentence);
        }

        public bool ClearHidden(DailyV1NpcRuntimeOwners target) => true;

        public bool ApplyStatusChange(DailyV1NpcRuntimeOwners target,
            DailyV1StatusDefinition status, bool add)
        {
            StatusChangeCalls++;
            return true;
        }

        public void PublishArrest(DailyV1NpcRuntimeOwners guard,
            DailyV1NpcRuntimeOwners target, DailyV1CityRuntimeOwners city,
            string originDecisionId) => PublishCalls++;
    }

    private static void AssertMatchesPreviousRule(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition guard,
        DailyV1ActionDefinition action,
        IReadOnlyList<DailyV1GuardArrestCandidate> candidates,
        float currentUtility)
    {
        int expectedIndex = -1;
        float expectedBounty = 0f;
        string expectedRuntimeId = null;
        for (int index = 0; index < candidates.Count; index++)
        {
            DailyV1GuardArrestCandidate candidate = candidates[index];
            if (expectedIndex < 0
                || candidate.Bounty > expectedBounty
                || (UnityMathfCompatibility.Approximately(candidate.Bounty, expectedBounty)
                    && string.CompareOrdinal(candidate.RuntimeId, expectedRuntimeId) < 0))
            {
                expectedIndex = index;
                expectedBounty = candidate.Bounty;
                expectedRuntimeId = candidate.RuntimeId;
            }
        }

        float expectedUtility = expectedIndex < 0
            ? 0f
            : UnityMathfCompatibility.Max(currentUtility, 55f + expectedBounty * 0.25f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, action, candidates, currentUtility,
            out int selectedIndex, out float resultingUtility), Is.True);
        Assert.That(selectedIndex, Is.EqualTo(expectedIndex));
        Assert.That(resultingUtility, Is.EqualTo(expectedUtility));
        if (selectedIndex >= 0)
            Assert.That(candidates[selectedIndex].RuntimeId, Is.EqualTo(expectedRuntimeId));
    }
}
