using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12CrimeSocialAppraisalInvalidationTests
{
    private const string MembershipOperationId = "runtime.npc-membership";
    private const string DailyAdvanceOperationId = "runtime.advance-day";

    private sealed class DailyOperationProbeTheftOutcomeSink : ITheftOutcomeSink
    {
        private readonly ITheftOutcomeSink inner;
        private readonly Func<SimulationRuntime> runtimeProvider;

        public readonly List<int> TheftOutcomeCommitActiveOperationCounts = new List<int>();
        public readonly List<bool> TheftOutcomeCompositeEpochReadsSucceeded = new List<bool>();
        public readonly List<long> TheftOutcomeCompositeEpochDeltas = new List<long>();

        public DailyOperationProbeTheftOutcomeSink(
            ITheftOutcomeSink inner,
            Func<SimulationRuntime> runtimeProvider)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.runtimeProvider = runtimeProvider ?? throw new ArgumentNullException(nameof(runtimeProvider));
        }

        public bool CanAcceptTheftOutcome(TheftOutcome outcome) => inner.CanAcceptTheftOutcome(outcome);

        public bool TryAcceptTheftOutcome(TheftOutcome outcome)
        {
            SimulationRuntime runtime = runtimeProvider();
            ContinuationCensusProtocol protocol = runtime == null ? null : GetProtocol(runtime);
            int activeOperationCount = -1;
            bool operationRead = protocol != null
                && protocol.TryReadActiveOperationCount(out activeOperationCount, out _);
            TheftOutcomeCommitActiveOperationCounts.Add(operationRead ? activeOperationCount : -1);

            long epochBefore = 0L;
            bool epochBeforeRead = protocol != null
                && protocol.TryReadMutationEpoch(out epochBefore, out _);
            bool accepted = inner.TryAcceptTheftOutcome(outcome);
            long epochAfter = 0L;
            bool epochAfterRead = protocol != null
                && protocol.TryReadMutationEpoch(out epochAfter, out _);
            bool epochReadsSucceeded = epochBeforeRead && epochAfterRead;
            TheftOutcomeCompositeEpochReadsSucceeded.Add(epochReadsSucceeded);
            TheftOutcomeCompositeEpochDeltas.Add(epochReadsSucceeded ? epochAfter - epochBefore : -1L);
            return accepted;
        }
    }

    private sealed class StaleAfterOwnerWriteProvider : IOwnerSectionCensusProvider
    {
        private readonly IOwnerSectionCensusProvider liveProvider;
        private readonly Func<bool> returnStale;
        private readonly OwnerSectionCensusWitness emptyBaseline;

        public StaleAfterOwnerWriteProvider(IOwnerSectionCensusProvider liveProvider, Func<bool> returnStale)
        {
            this.liveProvider = liveProvider ?? throw new ArgumentNullException(nameof(liveProvider));
            this.returnStale = returnStale ?? throw new ArgumentNullException(nameof(returnStale));
            emptyBaseline = liveProvider.GetCurrentCensus();
        }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            returnStale() ? emptyBaseline : liveProvider.GetCurrentCensus();
    }

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void SelectedProfileRegistersExactEmptyCrimeAppraisalOwnersAndKeepsThemAcrossPersonAdmission()
    {
        SimulationRuntime runtime = CreateRuntime(new PersonStore());
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            P12CrimeSocialAppraisalCensusProvider.CreateProviders(world);
        Assert.That(providers, Has.Count.EqualTo(3));
        AssertWitness(providers[0], P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, world.TheftOutcomes, 0, 0);
        AssertWitness(providers[1], P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, world.CrimeKnowledge, 0, 0);
        AssertWitness(providers[2], P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, world.SocialReactions, 0, 0);

        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(new PersonId("p12-social-admitted-person")),
            out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
        AssertWitness(providers[0], P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, world.TheftOutcomes, 0, 0);
        AssertWitness(providers[1], P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, world.CrimeKnowledge, 0, 0);
        AssertWitness(providers[2], P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, world.SocialReactions, 0, 0);
        AssertCensus(runtime);
    }

    [Test]
    public void SelectedDailyV1AdvanceDayExecutesStealInsideOuterOperationAndReconcilesCrimeSocialOwners()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        NpcActionData steal = AssetDatabase.LoadAssetAtPath<NpcActionData>(
            "Assets/_Project/Data/Actions/Action-Roubar.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(steal, Is.Not.Null);
        bool originalStealCanFail = steal.canFail;

        GameObject bootstrapObject = new GameObject("P12 Crime/Social daily operation integration test");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            SetPrivateField(bootstrap, "simulationConfig", config);
            SetPrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
            bootstrap.Start();

            SimulationRuntime runtime = bootstrap.Runtime;
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.Configuration.Crime.Enabled, Is.True);
            Assert.That(runtime.Configuration.MerchantTrade.Enabled, Is.True);
            Assert.That(runtime.PersonStore.Persons, Is.Empty,
                "Daily-v1 starts with its accepted empty PersonStore; this fixture adds the local action participants through runtime admission.");
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialCensusFailure),
                Is.True, initialCensusFailure.ToString());

            NpcRuntime thief = null;
            foreach (NpcRuntime candidate in runtime.NpcRuntimes)
            {
                if (candidate?.NpcData?.acoesPadrao == null)
                    continue;
                foreach (NPCDefaultAction preference in candidate.NpcData.acoesPadrao)
                {
                    if (preference != null && ReferenceEquals(preference.action, steal))
                    {
                        thief = candidate;
                        break;
                    }
                }
                if (thief != null)
                    break;
            }
            Assert.That(thief, Is.Not.Null);
            CityRuntime city = thief.CurrentCity;
            Assert.That(city, Is.Not.Null);
            Assert.That(city.ImportantNpcs.Count, Is.GreaterThan(1));
            Assert.That(thief.MerchantTradePlan.IsActive, Is.False);

            List<NpcActionData> runtimeActions = (List<NpcActionData>)GetPrivateField(runtime, "configuredActions");
            Assert.That(runtimeActions, Does.Contain(steal));
            runtimeActions.Clear();
            runtimeActions.Add(steal);

            foreach (NpcRuntime participant in city.ImportantNpcs)
            {
                PersonId personId = new PersonId("p12-social-runtime-" + participant.RuntimeId);
                Assert.That(runtime.TryRegisterPerson(new PersonRuntime(personId, runtime.CurrentDay),
                    out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
                Assert.That(runtime.TryBindExistingNpcToPerson(personId, participant.RuntimeId,
                    out PersonMaterializationFailure bindingFailure), Is.True, bindingFailure.ToString());
            }
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure boundCensusFailure),
                Is.True, boundCensusFailure.ToString());

            CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
            Assert.That(world.TheftOutcomes.Count, Is.Zero);
            Assert.That(world.CrimeKnowledge.Count, Is.Zero);
            Assert.That(world.SocialReactions.Count, Is.Zero);

            CrimeSystem crime = (CrimeSystem)GetPrivateField(runtime, "crimeSystem");
            Assert.That(crime, Is.Not.Null);
            ITheftOutcomeSink integration = crime.TheftOutcomeSink;
            Assert.That(integration, Is.SameAs(world.Integration));
            DailyOperationProbeTheftOutcomeSink probe = new DailyOperationProbeTheftOutcomeSink(
                integration,
                () => runtime);
            SetPrivateField(crime, "theftOutcomeSink", probe);

            ContinuationCensusProtocol protocol = GetProtocol(runtime);
            HashSet<string> expectedOperations = (HashSet<string>)GetPrivateField(protocol, "expectedOperations");
            Assert.That(expectedOperations, Does.Contain(DailyAdvanceOperationId));
            Assert.That(protocol.TryReadMutationEpoch(out long startingEpoch, out ContinuationCensusFailure startFailure),
                Is.True, startFailure.ToString());
            steal.canFail = false;
            Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());

            Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
            Assert.That(bootstrap.Decisions.Decisions, Has.Count.EqualTo(1));
            Assert.That(bootstrap.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
            Assert.That(bootstrap.Decisions.Decisions[0].ActionDefinitionId, Is.EqualTo(steal.DefinitionId));
            Assert.That(world.TheftOutcomes.Count, Is.EqualTo(1));
            Assert.That(world.CrimeKnowledge.Count, Is.EqualTo(1));
            Assert.That(world.SocialReactions.Count, Is.EqualTo(1));
            Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(1L));
            Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.EqualTo(1L));
            Assert.That(world.SocialReactions.P12CensusRevision, Is.EqualTo(1L));
            Assert.That(probe.TheftOutcomeCommitActiveOperationCounts, Is.EqualTo(new[] { 1 }),
                "Crime/Social commit runs while the selected Daily-v1 runtime.advance-day operation is active.");
            Assert.That(probe.TheftOutcomeCompositeEpochReadsSucceeded, Is.EqualTo(new[] { true }));
            Assert.That(probe.TheftOutcomeCompositeEpochDeltas, Is.EqualTo(new[] { 1L }),
                "The complete Crime/Social composite commits as one shared mutation epoch inside runtime.advance-day.");

            Assert.That(protocol.TryReadActiveOperationCount(out int activeOperationCount, out ContinuationCensusFailure operationFailure),
                Is.True, operationFailure.ToString());
            Assert.That(activeOperationCount, Is.Zero);
            Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure quiescenceFailure),
                Is.True, quiescenceFailure.ToString());
            Assert.That(protocol.TryReadMutationEpoch(out long endingEpoch, out ContinuationCensusFailure endFailure),
                Is.True, endFailure.ToString());
            Assert.That(endingEpoch, Is.GreaterThan(startingEpoch));
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure),
                Is.True, censusFailure.ToString());
        }
        finally
        {
            steal.canFail = originalStealCanFail;
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void DirectStoreWritersNotifySeparatelyOutsideAndInsideRegisteredOperations()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long start, out ContinuationCensusFailure startFailure),
            Is.True, startFailure.ToString());

        TheftOutcome first = CreateOutcome(perpetrator, victim, "direct-outside");
        Assert.That(world.TheftOutcomes.TryRecord(first, out CrimeOutcomeStoreFailure outcomeFailure),
            Is.True, outcomeFailure.ToString());
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(1));
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.Zero);
        Assert.That(world.SocialReactions.P12CensusRevision, Is.Zero);
        AssertEpoch(runtime, start + 1);

        CrimeKnowledgeObservation observation = CreateObservation(first, perpetrator, CrimeKnowledgeRole.Perpetrator);
        Assert.That(world.CrimeKnowledge.TryRecord(observation, out CrimeKnowledgeStoreFailure knowledgeFailure),
            Is.True, knowledgeFailure.ToString());
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.EqualTo(1));
        AssertEpoch(runtime, start + 2);

        Assert.That(world.SocialReactions.TryRecordAppraisal(
            victim,
            new SocialSourceReference("p12-test", "direct-reaction"),
            SocialReactionTarget.ForPerson(perpetrator),
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "direct-reaction"),
            SocialAppraisalResult.Reaction(SocialReactionValence.Positive, SocialReactionSalience.Low),
            0L,
            null,
            out _,
            out SocialReactionStoreFailure reactionFailure), Is.True, reactionFailure.ToString());
        Assert.That(world.SocialReactions.P12CensusRevision, Is.EqualTo(1));
        AssertEpoch(runtime, start + 3);
        AssertCensus(runtime);

        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        Assert.That(protocol.TryEnterOperation(MembershipOperationId,
            out SimulationOperationScope operation, out ContinuationCensusFailure enterFailure),
            Is.True, enterFailure.ToString());
        try
        {
            TheftOutcome second = CreateOutcome(perpetrator, victim, "direct-inside-operation");
            Assert.That(world.TheftOutcomes.TryRecord(second, out CrimeOutcomeStoreFailure secondFailure),
                Is.True, secondFailure.ToString());
            Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(2));
            AssertEpoch(runtime, start + 4);
        }
        finally
        {
            operation.Dispose();
        }
        AssertCensus(runtime);
    }

    [Test]
    public void CompositeTheftBatchesOutcomeKnowledgeAndReactionIntoOneEpochInBothModes()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long start, out ContinuationCensusFailure startFailure),
            Is.True, startFailure.ToString());

        Assert.That(world.Integration.TryAcceptTheftOutcome(CreateOutcome(perpetrator, victim, "composite-immediate")), Is.True);
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(1));
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.EqualTo(1));
        Assert.That(world.SocialReactions.P12CensusRevision, Is.EqualTo(1));
        Assert.That(world.TheftOutcomes.Count, Is.EqualTo(1));
        Assert.That(world.CrimeKnowledge.Count, Is.EqualTo(1));
        Assert.That(world.SocialReactions.Count, Is.EqualTo(1));
        AssertEpoch(runtime, start + 1);
        AssertCensus(runtime);

        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        Assert.That(protocol.TryEnterOperation(MembershipOperationId,
            out SimulationOperationScope operation, out ContinuationCensusFailure enterFailure),
            Is.True, enterFailure.ToString());
        try
        {
            Assert.That(world.Integration.TryAcceptTheftOutcome(
                CreateOutcome(perpetrator, victim, "composite-reserved")), Is.True);
            Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(2));
            Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.EqualTo(2));
            Assert.That(world.SocialReactions.P12CensusRevision, Is.EqualTo(2));
            AssertEpoch(runtime, start + 2);
        }
        finally
        {
            operation.Dispose();
        }
        AssertCensus(runtime);
    }

    [Test]
    public void NestedCoordinatorKeepsCompensatedChildWritesInOneOuterEpoch()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        TheftOutcome outcome = CreateOutcome(perpetrator, victim, "nested-compensation");
        object coordinator = GetPrivateField(runtime, "p12CrimeSocialAppraisalMutationCoordinator");
        MethodInfo begin = coordinator.GetType().GetMethod("TryBeginOperation");
        MethodInfo end = coordinator.GetType().GetMethod("EndOperation");
        Assert.That(begin, Is.Not.Null);
        Assert.That(end, Is.Not.Null);
        Type operationType = begin.GetParameters()[0].ParameterType;
        object theftOperation = Enum.Parse(operationType, "TheftAcceptance");
        object knowledgeOperation = Enum.Parse(operationType, "KnowledgeAndAppraisal");
        Assert.That((bool)begin.Invoke(coordinator, new[] { theftOperation }), Is.True);

        try
        {
            Assert.That(world.TheftOutcomes.TryRecord(outcome, out CrimeOutcomeStoreFailure outcomeFailure),
                Is.True, outcomeFailure.ToString());
            Assert.That((bool)begin.Invoke(coordinator, new[] { knowledgeOperation }), Is.True,
                "the one declared knowledge/appraisal child joins the outer theft context");
            try
            {
                Assert.That(world.CrimeKnowledge.TryRecord(
                    CreateObservation(outcome, victim, CrimeKnowledgeRole.Victim),
                    out CrimeKnowledgeStoreFailure knowledgeFailure), Is.True, knowledgeFailure.ToString());

                List<SocialReactionId> reactionIds = new List<SocialReactionId>();
                for (int i = 0; i < 2; i++)
                {
                    Assert.That(world.SocialReactions.TryRecordAppraisal(
                        victim,
                        new SocialSourceReference("p12-compensation", "reaction-" + i),
                        SocialReactionTarget.ForPerson(perpetrator),
                        SocialPerceivedAttribution.Unknown(),
                        new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "compensation"),
                        SocialAppraisalResult.Reaction(SocialReactionValence.Negative, SocialReactionSalience.Medium),
                        0L,
                        null,
                        out SocialReaction reaction,
                        out SocialReactionStoreFailure reactionFailure), Is.True, reactionFailure.ToString());
                    reactionIds.Add(reaction.ReactionId);
                }

                foreach (SocialReactionId reactionId in reactionIds)
                    Assert.That(world.SocialReactions.TryRemove(reactionId), Is.True);
                Assert.That(world.CrimeKnowledge.TryRemove(victim, outcome.OutcomeId), Is.True);
            }
            finally
            {
                Assert.That((bool)end.Invoke(coordinator, new[] { knowledgeOperation }), Is.True);
            }

            Assert.That(world.TheftOutcomes.TryRemove(outcome.OutcomeId), Is.True);
        }
        finally
        {
            Assert.That((bool)end.Invoke(coordinator, new[] { theftOperation }), Is.True);
        }

        Assert.That(world.TheftOutcomes.Count, Is.Zero);
        Assert.That(world.CrimeKnowledge.Count, Is.Zero);
        Assert.That(world.SocialReactions.Count, Is.Zero);
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(2));
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.EqualTo(2));
        Assert.That(world.SocialReactions.P12CensusRevision, Is.EqualTo(4));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure epochFailure), Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(1), "the non-empty changed-section set invalidates once even after compensating rows to their initial counts");
        AssertCensus(runtime);
    }

    [Test]
    public void ExistingEpochReservationRejectsDirectOwnerWriteBeforeMutation()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        Assert.That(protocol.TryEnterOperation(MembershipOperationId,
            out SimulationOperationScope operation, out ContinuationCensusFailure enterFailure),
            Is.True, enterFailure.ToString());
        Assert.That(protocol.TryReserveMutationEpochCapacity(
            out ContinuationMutationEpochReservation reservation, out ContinuationCensusFailure reserveFailure),
            Is.True, reserveFailure.ToString());
        try
        {
            Assert.That(world.TheftOutcomes.TryRecord(
                CreateOutcome(perpetrator, victim, "blocked-by-reservation"),
                out CrimeOutcomeStoreFailure writeFailure), Is.False);
            Assert.That(writeFailure.Code, Is.EqualTo(CrimeOutcomeStoreFailureCode.RuntimeFaulted));
            Assert.That(world.TheftOutcomes.Count, Is.Zero);
            Assert.That(world.TheftOutcomes.P12CensusRevision, Is.Zero);
        }
        finally
        {
            protocol.ReleaseMutationEpochReservation(reservation);
            operation.Dispose();
        }
        AssertCensus(runtime);
    }

    [Test]
    public void EmptyCompositeReleasesItsReservedEpochToken()
    {
        PersonStore people = CreatePeople(out _, out _);
        SimulationRuntime runtime = CreateRuntime(people);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        Assert.That(protocol.TryEnterOperation(MembershipOperationId,
            out SimulationOperationScope operation, out ContinuationCensusFailure enterFailure),
            Is.True, enterFailure.ToString());
        object coordinator = GetPrivateField(runtime, "p12CrimeSocialAppraisalMutationCoordinator");
        MethodInfo begin = coordinator.GetType().GetMethod("TryBeginOperation");
        MethodInfo end = coordinator.GetType().GetMethod("EndOperation");
        Type operationType = begin.GetParameters()[0].ParameterType;
        object knowledgeOperation = Enum.Parse(operationType, "KnowledgeAndAppraisal");
        try
        {
            Assert.That((bool)begin.Invoke(coordinator, new[] { knowledgeOperation }), Is.True);
            Assert.That((bool)end.Invoke(coordinator, new[] { knowledgeOperation }), Is.True);
            Assert.That(protocol.TryReserveMutationEpochCapacity(
                out ContinuationMutationEpochReservation reservation, out ContinuationCensusFailure reserveFailure),
                Is.True, "the no-change close releases its unused capacity token: " + reserveFailure);
            protocol.ReleaseMutationEpochReservation(reservation);
        }
        finally
        {
            operation.Dispose();
        }
        AssertCensus(runtime);
    }

    [Test]
    public void CompositeLocalRevisionHeadroomRefusalLeavesAllCrimeRowsUnchanged()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        SetPrivateField(world.TheftOutcomes, "p12CensusRevision", long.MaxValue - 1L);
        Assert.That(protocol.NotifyCommittedMutations(
            new[] { P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId }, out ContinuationCensusFailure refreshFailure),
            Is.True, refreshFailure.ToString());
        AssertCensus(runtime);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        Assert.That(world.Integration.TryAcceptTheftOutcome(
            CreateOutcome(perpetrator, victim, "composite-capacity-refusal")), Is.False);
        Assert.That(world.TheftOutcomes.Count, Is.Zero);
        Assert.That(world.CrimeKnowledge.Count, Is.Zero);
        Assert.That(world.SocialReactions.Count, Is.Zero);
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.Zero);
        Assert.That(world.SocialReactions.P12CensusRevision, Is.Zero);
        AssertEpoch(runtime, before);
        AssertCensus(runtime);
    }

    [Test]
    public void DirectSingleWriteRevisionExhaustionRejectsBeforeMutation()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        SetPrivateField(world.TheftOutcomes, "p12CensusRevision", long.MaxValue);
        Assert.That(protocol.NotifyCommittedMutations(
            new[] { P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId }, out ContinuationCensusFailure refreshFailure),
            Is.True, refreshFailure.ToString());
        AssertCensus(runtime);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        Assert.That(world.TheftOutcomes.TryRecord(
            CreateOutcome(perpetrator, victim, "direct-capacity-refusal"),
            out CrimeOutcomeStoreFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(CrimeOutcomeStoreFailureCode.RuntimeFaulted));
        Assert.That(world.TheftOutcomes.Count, Is.Zero);
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.EqualTo(long.MaxValue));
        AssertEpoch(runtime, before);
        AssertCensus(runtime);
    }

    [Test]
    public void TheftActionCompensatesMoneyWhenCrimeAppraisalRevisionBudgetIsExhausted()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationTime time = new SimulationTime();
        CrimeSystem crime = new CrimeSystem(new JusticeSystem(null, null, null, null), null, null, simulationTime: time);
        CityRuntime city = SimulationTestFactory.CreateCity("p12-social-compensation-city", "p12-social-compensation-location");
        Assert.That(people.TryBindMaterializedNpc(perpetrator, "p12-social-thief", out PersonStoreFailure thiefBindingFailure),
            Is.True, thiefBindingFailure.ToString());
        Assert.That(people.TryBindMaterializedNpc(victim, "p12-social-victim", out PersonStoreFailure victimBindingFailure),
            Is.True, victimBindingFailure.ToString());
        Assert.That(people.TryGet(perpetrator, out PersonRuntime thiefPerson), Is.True);
        Assert.That(people.TryGet(victim, out PersonRuntime victimPerson), Is.True);
        NpcRuntime thief = new NpcRuntime("p12-social-thief", SimulationTestFactory.CreateNpc("p12-social-thief"), city, 0f);
        NpcRuntime target = new NpcRuntime("p12-social-victim", SimulationTestFactory.CreateNpc("p12-social-victim"), city, 50f);
        AssignPerson(thief, thiefPerson);
        AssignPerson(target, victimPerson);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { thief, target },
            crimeSystem: crime,
            personStore: people,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;

        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        SetPrivateField(world.TheftOutcomes, "p12CensusRevision", long.MaxValue - 1L);
        Assert.That(protocol.NotifyCommittedMutations(
            new[] { P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId }, out ContinuationCensusFailure refreshFailure),
            Is.True, refreshFailure.ToString());
        AssertCensus(runtime);

        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-social-compensation-theft", NpcActionType.Steal, NpcActionCategory.Crime);
        action.crimeSettings.amount = 20;
        NpcActionRuntime attempt = new NpcActionRuntime(action, target, 20);
        attempt.SetStableOccurrenceKey("p12-social-compensation-occurrence");
        Assert.That(crime.TryExecuteAction(thief, attempt).Success, Is.False);

        Assert.That(thief.Money, Is.EqualTo(0f));
        Assert.That(target.Money, Is.EqualTo(50f));
        Assert.That(world.TheftOutcomes.Count, Is.Zero);
        Assert.That(world.CrimeKnowledge.Count, Is.Zero);
        Assert.That(world.SocialReactions.Count, Is.Zero);
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.Zero);
        Assert.That(world.SocialReactions.P12CensusRevision, Is.Zero);
        AssertCensus(runtime);
    }

    [Test]
    public void TheftActionDoesNotReverseCommittedMoneyWhenPostCommitEpochNotificationFails()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationTime time = new SimulationTime();
        CrimeSystem crime = new CrimeSystem(new JusticeSystem(null, null, null, null), null, null, simulationTime: time);
        CityRuntime city = SimulationTestFactory.CreateCity("p12-social-notify-failure-city", "p12-social-notify-failure-location");
        Assert.That(people.TryBindMaterializedNpc(perpetrator, "p12-social-notify-thief", out PersonStoreFailure thiefBindingFailure),
            Is.True, thiefBindingFailure.ToString());
        Assert.That(people.TryBindMaterializedNpc(victim, "p12-social-notify-victim", out PersonStoreFailure victimBindingFailure),
            Is.True, victimBindingFailure.ToString());
        Assert.That(people.TryGet(perpetrator, out PersonRuntime thiefPerson), Is.True);
        Assert.That(people.TryGet(victim, out PersonRuntime victimPerson), Is.True);
        NpcRuntime thief = new NpcRuntime("p12-social-notify-thief", SimulationTestFactory.CreateNpc("p12-social-notify-thief"), city, 0f);
        NpcRuntime target = new NpcRuntime("p12-social-notify-victim", SimulationTestFactory.CreateNpc("p12-social-notify-victim"), city, 50f);
        AssignPerson(thief, thiefPerson);
        AssignPerson(target, victimPerson);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { thief, target },
            crimeSystem: crime,
            personStore: people,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = P12CrimeSocialAppraisalCensusProvider.CreateProviders(world);
        ReplaceRegisteredProvider(
            protocol,
            P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId,
            new StaleAfterOwnerWriteProvider(providers[0], () => world.TheftOutcomes.Count > 0));

        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-social-notify-failure-theft", NpcActionType.Steal, NpcActionCategory.Crime);
        action.crimeSettings.amount = 20;
        NpcActionRuntime attempt = new NpcActionRuntime(action, target, 20);
        attempt.SetStableOccurrenceKey("p12-social-notify-failure-occurrence");

        Assert.That(crime.TryExecuteAction(thief, attempt).Success, Is.True);
        Assert.That(thief.Money, Is.EqualTo(20f));
        Assert.That(target.Money, Is.EqualTo(30f));
        Assert.That(world.TheftOutcomes.Count, Is.EqualTo(1));
        Assert.That(world.CrimeKnowledge.Count, Is.EqualTo(1));
        Assert.That(world.SocialReactions.Count, Is.EqualTo(1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void SharedEpochSaturationRejectsCompositeBeforeTheFirstCrimeWrite()
    {
        PersonStore people = CreatePeople(out PersonId perpetrator, out PersonId victim);
        SimulationRuntime runtime = CreateRuntime(people);
        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        SetPrivateField(protocol, "mutationEpoch", long.MaxValue);

        Assert.That(world.Integration.TryAcceptTheftOutcome(
            CreateOutcome(perpetrator, victim, "shared-epoch-saturated")), Is.False);
        Assert.That(world.TheftOutcomes.Count, Is.Zero);
        Assert.That(world.CrimeKnowledge.Count, Is.Zero);
        Assert.That(world.SocialReactions.Count, Is.Zero);
        Assert.That(world.TheftOutcomes.P12CensusRevision, Is.Zero);
        Assert.That(world.CrimeKnowledge.P12CensusRevision, Is.Zero);
        Assert.That(world.SocialReactions.P12CensusRevision, Is.Zero);
    }

    private static SimulationRuntime CreateRuntime(PersonStore people)
    {
        SimulationTime time = new SimulationTime();
        CityRuntime city = SimulationTestFactory.CreateCity("p12-social-city", "p12-social-location");
        CrimeSystem crime = new CrimeSystem(new JusticeSystem(null, null, null, null), null, null, simulationTime: time);
        return new SimulationRuntime(
            time,
            new[] { city },
            Array.Empty<NpcRuntime>(),
            crimeSystem: crime,
            personStore: people,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static PersonStore CreatePeople(out PersonId perpetrator, out PersonId victim)
    {
        PersonStore people = new PersonStore();
        perpetrator = new PersonId("p12-social-perpetrator");
        victim = new PersonId("p12-social-victim");
        Assert.That(people.TryRegister(new PersonRuntime(perpetrator), out PersonStoreFailure perpetratorFailure),
            Is.True, perpetratorFailure.ToString());
        Assert.That(people.TryRegister(new PersonRuntime(victim), out PersonStoreFailure victimFailure),
            Is.True, victimFailure.ToString());
        return people;
    }

    private static TheftOutcome CreateOutcome(PersonId perpetrator, PersonId victim, string occurrence)
    {
        TheftOutcomeId id = TheftOutcomeId.Create(perpetrator, victim, 0L, occurrence);
        return new TheftOutcome(id, perpetrator, victim, 10, 0L, occurrence);
    }

    private static CrimeKnowledgeObservation CreateObservation(
        TheftOutcome outcome,
        PersonId evaluator,
        CrimeKnowledgeRole role) => new CrimeKnowledgeObservation(
            evaluator,
            outcome.OutcomeId,
            role,
            false,
            SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "p12-direct-census-test"),
            0L);

    private static void AssertWitness(IOwnerSectionCensusProvider provider, string sectionId, object owner, int count, long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(sectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(P12CrimeSocialAppraisalCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(count));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (ContinuationCensusProtocol)field.GetValue(runtime);
    }

    private static void ReplaceRegisteredProvider(
        ContinuationCensusProtocol protocol,
        string sectionId,
        IOwnerSectionCensusProvider provider)
    {
        IDictionary sections = (IDictionary)GetPrivateField(protocol, "registeredSections");
        object section = sections[sectionId];
        Assert.That(section, Is.Not.Null);
        Type sectionType = section.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo contractField = sectionType.GetField("Contract", flags);
        Assert.That(contractField, Is.Not.Null);
        ConstructorInfo constructor = sectionType.GetConstructor(
            flags,
            null,
            new[] { typeof(OwnerSectionContract), typeof(IOwnerSectionCensusProvider) },
            null);
        Assert.That(constructor, Is.Not.Null);
        object replacement = constructor.Invoke(new[] { contractField.GetValue(section), provider });
        foreach (string fieldName in new[] { "OwnerInstanceIdentity", "LastCardinality", "LastRevision", "HasBaseline" })
        {
            FieldInfo field = sectionType.GetField(fieldName, flags);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(replacement, field.GetValue(section));
        }
        sections[sectionId] = replacement;
    }

    private static void SetPrivateField(object owner, string fieldName, object value)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(owner, value);
    }

    private static object GetPrivateField(object owner, string fieldName)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(owner);
    }

    private static void AssignPerson(NpcRuntime npc, PersonRuntime person)
    {
        MethodInfo assign = typeof(NpcRuntime).GetMethod("TryAssignPersonId", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo bind = typeof(NpcRuntime).GetMethod("TryBindPersonRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(assign, Is.Not.Null);
        Assert.That(bind, Is.Not.Null);
        Assert.That((bool)assign.Invoke(npc, new object[] { person.PersonId }), Is.True);
        Assert.That((bool)bind.Invoke(npc, new object[] { person }), Is.True);
    }

    private static void AssertEpoch(SimulationRuntime runtime, long expected)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        Assert.That(epoch, Is.EqualTo(expected));
    }

    private static void AssertCensus(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True, failure.ToString());
    }
}
