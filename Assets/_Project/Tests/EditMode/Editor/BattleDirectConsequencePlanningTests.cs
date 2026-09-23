using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class BattleDirectConsequencePlanningTests
{
    private const string NumericProfile = "unity-float32-current-host:v1";

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void PolicyIsExplicitAndD5FailurePreventsRuleEvaluation()
    {
        DelegateRule rule = UnchangedRule();
        Fixture noD6B2Policy = CreateFixture(StandardSeeds(), StandardParticipants(), null);
        Assert.That(noD6B2Policy.World.BattleDirectConsequencePlanningService.IsConfigured, Is.False);
        Assert.That(noD6B2Policy.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            noD6B2Policy.BattleId, out BattleDirectConsequencePlan missingPolicyPlan,
            out BattleDirectConsequenceFailure missingPolicyFailure), Is.False);
        Assert.That(missingPolicyPlan, Is.Null);
        Assert.That(missingPolicyFailure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.PolicyNotConfigured));

        Fixture noD5Policy = CreateFixture(StandardSeeds(), StandardParticipants(), rule, configureD5: false);
        Assert.That(noD5Policy.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            noD5Policy.BattleId, out BattleDirectConsequencePlan noD5Plan,
            out BattleDirectConsequenceFailure noD5Failure), Is.False);
        Assert.That(noD5Plan, Is.Null);
        Assert.That(noD5Failure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.D5PlanningFailed));
        Assert.That(rule.CallCount, Is.Zero);
    }

    [Test]
    public void RuleIsCalledExactlyOnceWithCanonicalMultipartyAvailableFreeCohortsOnly()
    {
        DelegateRule rule = UnchangedRule();
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 3L),
                Seed("force-a", "contingent-a", "source-a", 2L, ManpowerInjuryState.Wounded),
                Seed("force-a", "contingent-a", "source-a", 1L,
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured,
                    "force-b", ManpowerAvailabilityState.Unavailable),
                Seed("force-b", "contingent-b", "source-b", 4L),
                Seed("force-c", "contingent-c", "source-c", 6L)
            },
            new Dictionary<string, string>
            {
                { "force-a", "side-a" }, { "force-b", "side-b" }, { "force-c", "side-c" }
            },
            rule);

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());

        Assert.That(rule.CallCount, Is.EqualTo(1));
        Assert.That(rule.LastInput.Sides.Count, Is.EqualTo(3));
        Assert.That(rule.LastInput.Participants.Count, Is.EqualTo(3));
        Assert.That(rule.LastInput.Contingents.Count, Is.EqualTo(3));
        Assert.That(rule.LastInput.Cohorts.Count, Is.EqualTo(4));
        Assert.That(rule.LastInput.Cohorts[0].Identity.SideId.Value, Is.EqualTo("side-a"));
        Assert.That(rule.LastInput.Cohorts[0].Identity.InjuryState, Is.EqualTo(ManpowerInjuryState.Healthy));
        Assert.That(rule.LastInput.Cohorts[1].Identity.InjuryState, Is.EqualTo(ManpowerInjuryState.Wounded));
        Assert.That(rule.LastInput.Cohorts[0].SourceId.Value, Is.EqualTo("source-a"));
        Assert.That(plan.Inputs.Count, Is.EqualTo(4));
        Assert.That(plan.IsComplete, Is.True);
        Assert.That(plan.SourceGroups, Is.Empty);
    }

    [Test]
    public void EveryZeroDeathCohortStillRequiresAnExplicitUnchangedPartition()
    {
        Fixture fixture = CreateFixture(StandardSeeds(), StandardParticipants(), UnchangedRule());
        CaptureWorldState before = Capture(fixture);

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());

        Assert.That(plan.IsComplete, Is.True);
        Assert.That(plan.Partitions.Count, Is.EqualTo(plan.Inputs.Count));
        foreach (BattleCohortConsequencePartition partition in plan.Partitions)
            Assert.That(partition.DeathAmount, Is.Zero);
        AssertWorldUnchanged(fixture, before);
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport report,
            out BattleDirectConsequenceFailure validationFailure), Is.True, validationFailure?.ToString());
        Assert.That(report.IsCurrent, Is.True);
    }

    [Test]
    public void MissingDuplicateAndUnknownPartitionsFailDeterministically()
    {
        BattleDirectConsequenceCohortIdentity foreignIdentity = null;
        DelegateRule captureIdentityRule = new DelegateRule("test.capture:v1", input =>
        {
            foreignIdentity = input.Cohorts[0].Identity;
            return UnchangedPartitions(input);
        });
        Fixture source = CreateFixture(StandardSeeds("source"), StandardParticipants("source"), captureIdentityRule, suffix: "source");
        Assert.That(source.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            source.BattleId, out _, out _), Is.True);

        DelegateRule missing = new DelegateRule("test.missing:v1", input =>
            new[] { MakeUnchanged(input.Cohorts[0]) });
        Fixture missingFixture = CreateFixture(StandardSeeds("missing"), StandardParticipants("missing"), missing);
        AssertFailure(missingFixture, BattleDirectConsequenceFailureCode.MissingPartition);

        DelegateRule duplicate = new DelegateRule("test.duplicate:v1", input =>
        {
            BattleCohortConsequencePartition one = MakeUnchanged(input.Cohorts[0]);
            return new[] { one, one };
        });
        Fixture duplicateFixture = CreateFixture(StandardSeeds("duplicate"), StandardParticipants("duplicate"), duplicate);
        AssertFailure(duplicateFixture, BattleDirectConsequenceFailureCode.DuplicatePartition);

        DelegateRule unknown = new DelegateRule("test.unknown:v1", input => new[]
        {
            new BattleCohortConsequencePartition(
                foreignIdentity,
                new[] { new BattleCohortConsequenceLivingDestination(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, 1L) },
                0L)
        });
        Fixture unknownFixture = CreateFixture(StandardSeeds("unknown"), StandardParticipants("unknown"), unknown);
        AssertFailure(unknownFixture, BattleDirectConsequenceFailureCode.UnknownPartition);
    }

    [Test]
    public void InvalidOutputDiagnosticsDoNotDependOnPartitionInsertionOrder()
    {
        BattleDirectConsequenceCohortIdentity foreignIdentity = null;
        DelegateRule captureIdentityRule = new DelegateRule("test.capture-diagnostic-identity:v1", input =>
        {
            foreignIdentity = input.Cohorts[0].Identity;
            return UnchangedPartitions(input);
        });
        Fixture identityFixture = CreateFixture(
            StandardSeeds("diagnostic-source"),
            StandardParticipants("diagnostic-source"),
            captureIdentityRule,
            suffix: "diagnostic-source");
        Assert.That(identityFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            identityFixture.BattleId, out _, out _), Is.True);

        DelegateRule forward = new DelegateRule("test.invalid-order:v1", input =>
            InvalidPartitionsInOrder(input, foreignIdentity, reverse: false));
        DelegateRule reverse = new DelegateRule("test.invalid-order:v1", input =>
            InvalidPartitionsInOrder(input, foreignIdentity, reverse: true));
        Fixture forwardFixture = CreateFixture(StandardSeeds("diagnostic-forward"),
            StandardParticipants("diagnostic-forward"), forward, suffix: "diagnostic-forward");
        Fixture reverseFixture = CreateFixture(StandardSeeds("diagnostic-reverse"),
            StandardParticipants("diagnostic-reverse"), reverse, suffix: "diagnostic-reverse");

        Assert.That(forwardFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            forwardFixture.BattleId, out _, out BattleDirectConsequenceFailure forwardFailure), Is.False);
        Assert.That(reverseFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            reverseFixture.BattleId, out _, out BattleDirectConsequenceFailure reverseFailure), Is.False);
        Assert.That(reverseFailure.Code, Is.EqualTo(forwardFailure.Code));
        Assert.That(reverseFailure.Message, Is.EqualTo(forwardFailure.Message));

        DelegateRule destinationForward = new DelegateRule("test.invalid-destination-order:v1", input =>
            InvalidDestinationsInOrder(input, reverse: false));
        DelegateRule destinationReverse = new DelegateRule("test.invalid-destination-order:v1", input =>
            InvalidDestinationsInOrder(input, reverse: true));
        Fixture destinationForwardFixture = CreateFixture(StandardSeeds("destination-forward"),
            StandardParticipants("destination-forward"), destinationForward, suffix: "destination-forward");
        Fixture destinationReverseFixture = CreateFixture(StandardSeeds("destination-reverse"),
            StandardParticipants("destination-reverse"), destinationReverse, suffix: "destination-reverse");
        Assert.That(destinationForwardFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            destinationForwardFixture.BattleId, out _, out BattleDirectConsequenceFailure destinationForwardFailure), Is.False);
        Assert.That(destinationReverseFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            destinationReverseFixture.BattleId, out _, out BattleDirectConsequenceFailure destinationReverseFailure), Is.False);
        Assert.That(destinationReverseFailure.Code, Is.EqualTo(destinationForwardFailure.Code));
        Assert.That(destinationReverseFailure.Message, Is.EqualTo(destinationForwardFailure.Message));
    }

    [TestCase(-1L, BattleDirectConsequenceFailureCode.InvalidAmount)]
    [TestCase(1L, BattleDirectConsequenceFailureCode.ConservationFailure)]
    [TestCase(3L, BattleDirectConsequenceFailureCode.ConservationFailure)]
    public void InvalidDeathOrUnderAndOverConservationAreRejected(
        long livingAmount,
        BattleDirectConsequenceFailureCode expectedFailure)
    {
        DelegateRule rule = new DelegateRule("test.conservation:v1", input =>
        {
            BattleDirectConsequenceCohortInput first = input.Cohorts[0];
            long amount = livingAmount < 0L ? first.Amount : livingAmount;
            return new[] { new BattleCohortConsequencePartition(
                first.Identity,
                new[] { new BattleCohortConsequenceLivingDestination(
                    first.Identity.InjuryState, first.Identity.CustodyState,
                    first.Identity.CustodianForceId, first.Identity.AvailabilityState, amount) },
                livingAmount < 0L ? -1L : 0L) };
        });
        Fixture fixture = CreateFixture(StandardSeeds(), StandardParticipants(), rule);
        AssertFailure(fixture, expectedFailure);
    }

    [Test]
    public void DuplicateDestinationAmountsAreCheckedBeforeConservation()
    {
        DelegateRule rule = new DelegateRule("test.overflow:v1", input =>
        {
            BattleDirectConsequenceCohortInput first = input.Cohorts[0];
            BattleCohortConsequenceLivingDestination huge = new BattleCohortConsequenceLivingDestination(
                first.Identity.InjuryState, ManpowerCustodyState.Free, null,
                ManpowerAvailabilityState.Available, long.MaxValue);
            BattleCohortConsequenceLivingDestination one = new BattleCohortConsequenceLivingDestination(
                first.Identity.InjuryState, ManpowerCustodyState.Free, null,
                ManpowerAvailabilityState.Available, 1L);
            return new[] { new BattleCohortConsequencePartition(first.Identity, new[] { huge, one }, 0L) };
        });
        Fixture fixture = CreateFixture(StandardSeeds(), StandardParticipants(), rule);
        AssertFailure(fixture, BattleDirectConsequenceFailureCode.AmountOverflow);
    }

    [Test]
    public void HealthyCanBecomeWoundedAndUnexposedRosterIsRetainedInProjection()
    {
        DelegateRule rule = new DelegateRule("test.wound:v1", input =>
        {
            List<BattleCohortConsequencePartition> values = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                ManpowerInjuryState injury = cohort.Identity.InjuryState == ManpowerInjuryState.Healthy
                    ? ManpowerInjuryState.Wounded
                    : ManpowerInjuryState.Wounded;
                values.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(injury, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, cohort.Amount)
                }, 0L));
            }
            return values;
        });
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 2L),
                Seed("force-a", "contingent-a", "source-a", 3L,
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Unavailable),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), rule);

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());
        BattleDirectConsequenceContingentProjection projection = FindProjection(plan, "contingent-a");
        Assert.That(projection.LivingRosterAmount, Is.EqualTo(5L));
        Assert.That(ContainsCohort(projection.Cohorts, ManpowerInjuryState.Wounded,
            ManpowerCustodyState.Free, ManpowerAvailabilityState.Available, 2L), Is.True);
        Assert.That(ContainsCohort(projection.Cohorts, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, ManpowerAvailabilityState.Unavailable, 3L), Is.True);
    }

    [Test]
    public void PostStateProjectionMergesUnexposedCohortsAndDerivesRosterAndAvailability()
    {
        DelegateRule rule = new DelegateRule("test.projection-merge:v1", input =>
        {
            List<BattleCohortConsequencePartition> values = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                if (cohort.Identity.ContingentId.Value != "contingent-a")
                {
                    values.Add(MakeUnchanged(cohort));
                    continue;
                }
                values.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(
                        ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, 70L),
                    new BattleCohortConsequenceLivingDestination(
                        ManpowerInjuryState.Wounded, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Unavailable, 20L),
                    new BattleCohortConsequenceLivingDestination(
                        ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured,
                        new ArmedForceId("force-b"), ManpowerAvailabilityState.Unavailable, 5L)
                }, 5L));
            }
            return values;
        });
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 100L),
                Seed("force-a", "contingent-a", "source-a", 30L,
                    ManpowerInjuryState.Wounded, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Unavailable),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), rule);

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());
        BattleDirectConsequenceContingentProjection projection = FindProjection(plan, "contingent-a");
        Assert.That(projection.Cohorts.Count, Is.EqualTo(3));
        Assert.That(ContainsCohort(projection.Cohorts, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, ManpowerAvailabilityState.Available, 70L), Is.True);
        Assert.That(ContainsCohort(projection.Cohorts, ManpowerInjuryState.Wounded,
            ManpowerCustodyState.Free, ManpowerAvailabilityState.Unavailable, 50L), Is.True);
        Assert.That(ContainsCohort(projection.Cohorts, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Captured, ManpowerAvailabilityState.Unavailable, 5L), Is.True);
        Assert.That(projection.LivingRosterAmount, Is.EqualTo(125L));
        Assert.That(projection.AvailableAmount, Is.EqualTo(70L));
    }

    [Test]
    public void WoundedCohortCannotHeal()
    {
        DelegateRule rule = new DelegateRule("test.heal:v1", input =>
        {
            List<BattleCohortConsequencePartition> values = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                ManpowerInjuryState injury = cohort.Identity.InjuryState == ManpowerInjuryState.Wounded
                    ? ManpowerInjuryState.Healthy
                    : ManpowerInjuryState.Healthy;
                values.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(injury, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, cohort.Amount)
                }, 0L));
            }
            return values;
        });
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 2L, ManpowerInjuryState.Wounded),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), rule);
        AssertFailure(fixture, BattleDirectConsequenceFailureCode.InvalidTransition);
    }

    [Test]
    public void ExplicitCaptureIsAllowedOnDrawAndRequiresOpposingActiveParticipant()
    {
        DelegateRule captureRule = new DelegateRule("test.capture:v1", input =>
        {
            List<BattleCohortConsequencePartition> values = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                bool captureA = cohort.Identity.SideId.Value == "side-a";
                values.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState,
                        captureA ? ManpowerCustodyState.Captured : ManpowerCustodyState.Free,
                        captureA ? new ArmedForceId("force-b") : null,
                        captureA ? ManpowerAvailabilityState.Unavailable : ManpowerAvailabilityState.Available,
                        cohort.Amount)
                }, 0L));
            }
            return values;
        });
        Fixture draw = CreateFixture(StandardSeeds(), StandardParticipants(), captureRule);

        Assert.That(draw.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            draw.BattleId, out BattleDirectConsequencePlan drawPlan,
            out BattleDirectConsequenceFailure drawFailure), Is.True, drawFailure?.ToString());
        Assert.That(drawPlan.OutcomeType, Is.EqualTo(BattleOutcomeType.Draw));
        Assert.That(drawPlan.WinningSideId, Is.Null);
        Assert.That(drawPlan.Partitions, Has.Some.Matches<BattleCohortConsequencePartition>(p =>
            p.LivingDestinations.Count == 1
            && p.LivingDestinations[0].CustodyState == ManpowerCustodyState.Captured
            && p.LivingDestinations[0].AvailabilityState == ManpowerAvailabilityState.Unavailable));

        DelegateRule sameSide = CaptureTo("force-a");
        Fixture sameSideFixture = CreateFixture(StandardSeeds(), StandardParticipants(), sameSide);
        AssertFailure(sameSideFixture, BattleDirectConsequenceFailureCode.InvalidCustodian);

        DelegateRule nonParticipant = CaptureTo("force-c");
        Fixture nonParticipantFixture = CreateFixture(
            StandardSeeds(), StandardParticipants(), nonParticipant,
            extraForces: new[] { "force-c" });
        AssertFailure(nonParticipantFixture, BattleDirectConsequenceFailureCode.InvalidCustodian);

        Fixture inactiveCustodian = CreateFixture(
            StandardSeeds(), StandardParticipants(), CaptureSideAToB());
        DemobilizeAndTerminate(inactiveCustodian, "contingent-b", "source-b", 2L, "force-b");
        AssertFailure(inactiveCustodian, BattleDirectConsequenceFailureCode.D5PlanningFailed);
    }

    [Test]
    public void UnboundDeathsFailAndBoundDeathsAreGroupedOncePerSourceInOrdinalOrder()
    {
        DelegateRule oneDeathEach = new DelegateRule("test.death:v1", input =>
        {
            List<BattleCohortConsequencePartition> values = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                values.Add(new BattleCohortConsequencePartition(cohort.Identity,
                    new[] { new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, cohort.Amount - 1L) }, 1L));
            }
            return values;
        });
        Fixture unbound = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", null, 2L),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), oneDeathEach);
        AssertFailure(unbound, BattleDirectConsequenceFailureCode.UnboundDeath);

        Fixture bound = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-z", 2L),
                Seed("force-a", "contingent-shared", "source-z", 3L),
                Seed("force-b", "contingent-b", "source-a", 2L)
            }, StandardParticipants(), oneDeathEach);
        CaptureWorldState boundBefore = Capture(bound);
        Assert.That(bound.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            bound.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());
        AssertWorldUnchanged(bound, boundBefore);
        Assert.That(plan.SourceGroups.Count, Is.EqualTo(2));
        Assert.That(plan.SourceGroups[0].SourceId.Value, Is.EqualTo("source-a"));
        Assert.That(plan.SourceGroups[0].DeathAmount, Is.EqualTo(1L));
        Assert.That(plan.SourceGroups[1].SourceId.Value, Is.EqualTo("source-z"));
        Assert.That(plan.SourceGroups[1].DeathAmount, Is.EqualTo(2L));
        Assert.That(plan.SourceGroups[1].Traces.Count, Is.EqualTo(2));
        Assert.That(plan.SourceGroups[0].Proposal.Effect.Amount, Is.EqualTo(1L));
        Assert.That(plan.SourceGroups[1].Proposal.Effect.Amount, Is.EqualTo(2L));
        long tracedDeaths = 0L;
        foreach (BattleDirectConsequenceDeathTrace trace in plan.SourceGroups[1].Traces)
        {
            Assert.That(trace.CohortIdentity.ContingentId.Value, Does.Contain("contingent"));
            Assert.That(trace.DeathAmount, Is.EqualTo(1L));
            tracedDeaths = checked(tracedDeaths + trace.DeathAmount);
        }
        Assert.That(tracedDeaths, Is.EqualTo(plan.SourceGroups[1].DeathAmount));
    }

    [Test]
    public void D6B1RejectionNeverReturnsPartialCompletePlan()
    {
        DelegateRule rule = DeathRule(1L);
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 2L),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), rule);
        ApplyPopulationChange(fixture.CitiesBySource["source-a"].Population,
            new PopulationChangeSet(0, 1000, 0, 0));

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.False);
        Assert.That(plan, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.SourceConsequencePlanningFailed));
        Assert.That(fixture.CitiesBySource["source-a"].Population.CurrentPopulation, Is.Zero);
    }

    [Test]
    public void CurrentSourceProposalCapacityDoesNotAffectAndRelevantPopulationChangeStales()
    {
        DelegateRule killOne = new DelegateRule("test.death-one-a:v1", input =>
        {
            List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                long deaths = cohort.Identity.ContingentId.Value == "contingent-a" ? 1L : 0L;
                long survivors = cohort.Amount - deaths;
                result.Add(new BattleCohortConsequencePartition(cohort.Identity,
                    new[] { new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, survivors) }, deaths));
            }
            return result;
        });
        Fixture fixture = CreateFixture(StandardSeeds(), StandardParticipants(), killOne);
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport current,
            out BattleDirectConsequenceFailure currentFailure), Is.True, currentFailure?.ToString());
        Assert.That(current.IsCurrent, Is.True);

        CityRuntime unrelatedCity = fixture.CitiesBySource["source-b"];
        ApplyPopulationChange(unrelatedCity.Population, new PopulationChangeSet(1, 0, 0, 0));
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport unrelated,
            out BattleDirectConsequenceFailure unrelatedFailure), Is.True, unrelatedFailure?.ToString());

        CityRuntime affectedCity = fixture.CitiesBySource["source-a"];
        ApplyPopulationChange(affectedCity.Population, new PopulationChangeSet(1, 0, 0, 0));
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport stale,
            out BattleDirectConsequenceFailure staleFailure), Is.False);
        Assert.That(stale.IsStale, Is.True);
        Assert.That(stale.Reasons, Does.Contain(BattleDirectConsequenceStalenessReason.SourceProposalChanged));
        Assert.That(staleFailure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.PlanStale));
    }

    [Test]
    public void UnexposedParticipantManpowerDayAndCustodianChangesStaleThePlan()
    {
        Fixture manpowerFixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 2L),
                Seed("force-a", "contingent-a", "source-a", 1L,
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Unavailable),
                Seed("force-b", "contingent-b", "source-b", 2L)
            }, StandardParticipants(), UnchangedRule());
        Assert.That(manpowerFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            manpowerFixture.BattleId, out BattleDirectConsequencePlan manpowerPlan, out _), Is.True);
        Assert.That(manpowerFixture.World.ContingentManpowerStateStore.TryGet(
            new ContingentId("contingent-a"), out ContingentManpowerState state), Is.True);
        Assert.That(manpowerFixture.World.ContingentManpowerStateStore.TryRedistribute(
            state.ContingentId,
            new[]
            {
                new ContingentManpowerCohort(ManpowerInjuryState.Healthy,
                    ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L),
                new ContingentManpowerCohort(ManpowerInjuryState.Healthy,
                    ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Unavailable, 2L)
            },
            state.Revision,
            out ContingentManpowerFailure redistributionFailure), Is.True, redistributionFailure.ToString());
        AssertStale(manpowerFixture, manpowerPlan, BattleDirectConsequenceStalenessReason.D5PlanChanged,
            BattleDirectConsequenceStalenessReason.ParticipantManpowerChanged);

        Fixture dayFixture = CreateFixture(StandardSeeds(), StandardParticipants(), UnchangedRule());
        Assert.That(dayFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            dayFixture.BattleId, out BattleDirectConsequencePlan dayPlan, out _), Is.True);
        dayFixture.Time.AdvanceDay();
        AssertStale(dayFixture, dayPlan, BattleDirectConsequenceStalenessReason.CurrentDayChanged,
            BattleDirectConsequenceStalenessReason.D5PlanChanged);

        Fixture custodianFixture = CreateFixture(StandardSeeds(), StandardParticipants(), CaptureSideAToB());
        Assert.That(custodianFixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            custodianFixture.BattleId, out BattleDirectConsequencePlan capturePlan, out _), Is.True);
        DemobilizeAndTerminate(custodianFixture, "contingent-b", "source-b", 2L, "force-b");
        AssertStale(custodianFixture, capturePlan, BattleDirectConsequenceStalenessReason.D5PlanChanged,
            BattleDirectConsequenceStalenessReason.ParticipantManpowerChanged,
            BattleDirectConsequenceStalenessReason.CustodianChanged);
    }

    [Test]
    public void UnrelatedForceAndBattleRevisionsDoNotStalePlan()
    {
        Fixture fixture = CreateFixture(
            new[]
            {
                Seed("force-a", "contingent-a", "source-a", 2L),
                Seed("force-b", "contingent-b", "source-b", 2L),
                Seed("force-unrelated", "contingent-unrelated", "source-unrelated", 2L)
            },
            StandardParticipants(),
            UnchangedRule());
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan, out _), Is.True);
        Assert.That(fixture.World.ContingentManpowerStateStore.TryGet(
            new ContingentId("contingent-unrelated"), out ContingentManpowerState unrelatedState), Is.True);
        Assert.That(fixture.World.ContingentManpowerStateStore.TryRedistribute(
            unrelatedState.ContingentId,
            new[] { new ContingentManpowerCohort(
                ManpowerInjuryState.Wounded, ManpowerCustodyState.Free, null,
                ManpowerAvailabilityState.Unavailable, 2L) },
            unrelatedState.Revision,
            out ContingentManpowerFailure redistributionFailure), Is.True, redistributionFailure.ToString());
        Assert.That(fixture.World.ArmedForceStore.TryRegister(
            new ArmedForceRecord(new ArmedForceId("force-added-later"), "Unrelated", 0L),
            out ArmedForceFoundationFailure forceFailure), Is.True, forceFailure.ToString());
        BattleId unrelatedBattleId = new BattleId("unrelated-battle");
        Assert.That(fixture.World.BattleStore.TryRegister(
            new PersistentBattleRecord(unrelatedBattleId, fixture.World.CurrentDay,
                sides: new[]
                {
                    new BattleStateSide(unrelatedBattleId, new BattleSideId("unrelated-side-a"), "A"),
                    new BattleStateSide(unrelatedBattleId, new BattleSideId("unrelated-side-b"), "B")
                }),
            out PersistentStateFailure battleFailure), Is.True, battleFailure.ToString());
        ApplyPopulationChange(fixture.CitiesBySource["source-b"].Population,
            new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport report,
            out BattleDirectConsequenceFailure failure), Is.True, failure?.ToString());
        Assert.That(report.IsCurrent, Is.True);
    }

    [Test]
    public void SourceCapacityOnlyChangesDoNotChangeThePlanFingerprint()
    {
        Fixture lowCapacity = CreateFixture(StandardSeeds(), StandardParticipants(), DeathRule(1L), sourceCapacity: 1000L);
        Fixture highCapacity = CreateFixture(StandardSeeds(), StandardParticipants(), DeathRule(1L), sourceCapacity: 9000L);

        Assert.That(lowCapacity.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            lowCapacity.BattleId, out BattleDirectConsequencePlan lowPlan,
            out BattleDirectConsequenceFailure lowFailure), Is.True, lowFailure?.ToString());
        Assert.That(highCapacity.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            highCapacity.BattleId, out BattleDirectConsequencePlan highPlan,
            out BattleDirectConsequenceFailure highFailure), Is.True, highFailure?.ToString());

        Assert.That(highPlan.Fingerprint, Is.EqualTo(lowPlan.Fingerprint));
    }

    [Test]
    public void MutableRuleIdentityAndThrowingRuleFailClosed()
    {
        DelegateRule changedBeforePlanning = UnchangedRule();
        Fixture first = CreateFixture(StandardSeeds(), StandardParticipants(), changedBeforePlanning);
        changedBeforePlanning.RuleKey = "test.changed-after-composition:v1";
        Assert.That(first.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            first.BattleId, out _, out BattleDirectConsequenceFailure identityFailure), Is.False);
        Assert.That(identityFailure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.PolicyIdentityChanged));
        Assert.That(changedBeforePlanning.CallCount, Is.Zero);

        DelegateRule changedAfterPlanning = UnchangedRule();
        Fixture second = CreateFixture(StandardSeeds(), StandardParticipants(), changedAfterPlanning);
        Assert.That(second.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            second.BattleId, out BattleDirectConsequencePlan plan, out _), Is.True);
        changedAfterPlanning.ConfigurationIdentity = "mutated-configuration";
        Assert.That(second.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport report,
            out BattleDirectConsequenceFailure afterFailure), Is.False);
        Assert.That(report.IsInvalid, Is.True);
        Assert.That(afterFailure.Code, Is.EqualTo(BattleDirectConsequenceFailureCode.PolicyIdentityChanged));

        DelegateRule throwing = new DelegateRule("test.throw:v1", _ => throw new InvalidOperationException());
        Fixture third = CreateFixture(StandardSeeds(), StandardParticipants(), throwing);
        AssertFailure(third, BattleDirectConsequenceFailureCode.RuleFailed);
        Assert.That(throwing.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void RuleAndDestinationInsertionOrderDoNotChangeCanonicalPlanFingerprint()
    {
        DelegateRule forward = new DelegateRule("test.order:v1", input => OrderedPartitions(input, reverse: false));
        DelegateRule reverse = new DelegateRule("test.order:v1", input => OrderedPartitions(input, reverse: true));
        Fixture first = CreateFixture(StandardSeeds(), StandardParticipants(), forward);
        Fixture second = CreateFixture(StandardSeeds(), StandardParticipants(), reverse);

        Assert.That(first.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            first.BattleId, out BattleDirectConsequencePlan firstPlan,
            out BattleDirectConsequenceFailure firstFailure), Is.True, firstFailure?.ToString());
        Assert.That(second.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            second.BattleId, out BattleDirectConsequencePlan secondPlan,
            out BattleDirectConsequenceFailure secondFailure), Is.True, secondFailure?.ToString());

        Assert.That(secondPlan.Fingerprint, Is.EqualTo(firstPlan.Fingerprint));
    }

    private static IReadOnlyList<BattleCohortConsequencePartition> OrderedPartitions(
        BattleDirectConsequenceInput input,
        bool reverse)
    {
        List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
        {
            List<BattleCohortConsequenceLivingDestination> destinations = new List<BattleCohortConsequenceLivingDestination>
            {
                new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Healthy,
                    ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L),
                new BattleCohortConsequenceLivingDestination(ManpowerInjuryState.Wounded,
                    ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, cohort.Amount - 1L)
            };
            if (reverse) destinations.Reverse();
            result.Add(new BattleCohortConsequencePartition(cohort.Identity, destinations, 0L));
        }
        if (reverse) result.Reverse();
        return result;
    }

    private static IReadOnlyList<BattleCohortConsequencePartition> InvalidPartitionsInOrder(
        BattleDirectConsequenceInput input,
        BattleDirectConsequenceCohortIdentity foreignIdentity,
        bool reverse)
    {
        BattleCohortConsequencePartition duplicate = MakeUnchanged(input.Cohorts[0]);
        BattleCohortConsequencePartition unknown = new BattleCohortConsequencePartition(
            foreignIdentity,
            new[] { new BattleCohortConsequenceLivingDestination(
                ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                ManpowerAvailabilityState.Available, 1L) },
            0L);
        return reverse
            ? new[] { duplicate, duplicate, unknown }
            : new[] { unknown, duplicate, duplicate };
    }

    private static IReadOnlyList<BattleCohortConsequencePartition> InvalidDestinationsInOrder(
        BattleDirectConsequenceInput input,
        bool reverse)
    {
        BattleDirectConsequenceCohortInput cohort = input.Cohorts[0];
        BattleCohortConsequenceLivingDestination invalidInjury =
            new BattleCohortConsequenceLivingDestination(
                (ManpowerInjuryState)99, ManpowerCustodyState.Free, null,
                ManpowerAvailabilityState.Available, 1L);
        BattleCohortConsequenceLivingDestination invalidCustodian =
            new BattleCohortConsequenceLivingDestination(
                ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured,
                new ArmedForceId(cohort.Identity.ForceId.Value),
                ManpowerAvailabilityState.Unavailable, 1L);
        return new[] { new BattleCohortConsequencePartition(
            cohort.Identity,
            reverse
                ? new[] { invalidCustodian, invalidInjury }
                : new[] { invalidInjury, invalidCustodian },
            0L) };
    }

    private static DelegateRule CaptureTo(string custodianForceId)
        => new DelegateRule("test.capture-to:v1", input =>
        {
            List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                result.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState,
                        ManpowerCustodyState.Captured,
                        new ArmedForceId(custodianForceId),
                        ManpowerAvailabilityState.Unavailable,
                        cohort.Amount)
                }, 0L));
            }
            return result;
        });

    private static DelegateRule CaptureSideAToB()
        => new DelegateRule("test.capture-side-a-to-b:v1", input =>
        {
            List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                bool capture = cohort.Identity.SideId.Value == "side-a";
                result.Add(new BattleCohortConsequencePartition(cohort.Identity, new[]
                {
                    new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState,
                        capture ? ManpowerCustodyState.Captured : ManpowerCustodyState.Free,
                        capture ? new ArmedForceId("force-b") : null,
                        capture ? ManpowerAvailabilityState.Unavailable : ManpowerAvailabilityState.Available,
                        cohort.Amount)
                }, 0L));
            }
            return result;
        });

    private static DelegateRule DeathRule(long deathAmount)
        => new DelegateRule("test.death:v1", input =>
        {
            List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
            foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            {
                long deaths = Math.Min(deathAmount, cohort.Amount);
                long survivors = cohort.Amount - deaths;
                result.Add(new BattleCohortConsequencePartition(cohort.Identity,
                    survivors > 0L ? new[] { new BattleCohortConsequenceLivingDestination(
                        cohort.Identity.InjuryState, ManpowerCustodyState.Free, null,
                        ManpowerAvailabilityState.Available, survivors) }
                        : Array.Empty<BattleCohortConsequenceLivingDestination>(), deaths));
            }
            return result;
        });

    private static DelegateRule UnchangedRule()
        => new DelegateRule("test.unchanged:v1", UnchangedPartitions);

    private static IReadOnlyList<BattleCohortConsequencePartition> UnchangedPartitions(
        BattleDirectConsequenceInput input)
    {
        List<BattleCohortConsequencePartition> result = new List<BattleCohortConsequencePartition>();
        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            result.Add(MakeUnchanged(cohort));
        return result;
    }

    private static BattleCohortConsequencePartition MakeUnchanged(BattleDirectConsequenceCohortInput cohort)
        => new BattleCohortConsequencePartition(cohort.Identity, new[]
        {
            new BattleCohortConsequenceLivingDestination(
                cohort.Identity.InjuryState,
                cohort.Identity.CustodyState,
                cohort.Identity.CustodianForceId,
                cohort.Identity.AvailabilityState,
                cohort.Amount)
        }, 0L);

    private static void AssertFailure(Fixture fixture, BattleDirectConsequenceFailureCode expected)
    {
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryCreatePlan(
            fixture.BattleId, out BattleDirectConsequencePlan plan,
            out BattleDirectConsequenceFailure failure), Is.False);
        Assert.That(plan, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected));
    }

    private static void AssertStale(
        Fixture fixture,
        BattleDirectConsequencePlan plan,
        params BattleDirectConsequenceStalenessReason[] expected)
    {
        Assert.That(fixture.World.BattleDirectConsequencePlanningService.TryValidateCurrent(
            plan, out BattleDirectConsequenceValidationReport report,
            out BattleDirectConsequenceFailure failure), Is.False, failure?.ToString());
        Assert.That(report.IsStale, Is.True);
        foreach (BattleDirectConsequenceStalenessReason reason in expected)
            Assert.That(report.Reasons, Does.Contain(reason));
    }

    private static void DemobilizeAndTerminate(
        Fixture fixture,
        string contingentId,
        string sourceId,
        long amount,
        string forceId)
    {
        ContingentManpowerStateStore store = fixture.World.ContingentManpowerStateStore;
        Assert.That(store.TryGet(new ContingentId(contingentId), out ContingentManpowerState state), Is.True);
        Assert.That(fixture.World.ContingentManpowerStateStore.SourceProvider.TryGetSnapshot(
            new ManpowerSourceId(sourceId), out ManpowerSourceCapacitySnapshot snapshot), Is.True);
        Assert.That(store.TryDemobilize(
            state.ContingentId,
            amount,
            ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free,
            null,
            ManpowerAvailabilityState.Available,
            state.Revision,
            snapshot.Fingerprint,
            out ContingentManpowerFailure demobilizeFailure), Is.True, demobilizeFailure.ToString());
        Assert.That(fixture.World.ArmedForceStore.TryTerminate(
            new ArmedForceId(forceId), fixture.World.CurrentDay,
            out ArmedForceFoundationFailure terminationFailure), Is.True, terminationFailure.ToString());
    }

    private static void ApplyPopulationChange(SettlementPopulationRuntime population, PopulationChangeSet change)
    {
        Assert.That(SettlementPopulationSystem.TryPropose(
            population, change, out SettlementPopulationTransition transition,
            out PopulationTransitionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(SettlementPopulationSystem.TryApply(
            population, transition, out PopulationTransitionFailure applyFailure), Is.True, applyFailure.ToString());
    }

    private static BattleDirectConsequenceContingentProjection FindProjection(
        BattleDirectConsequencePlan plan,
        string contingentId)
    {
        foreach (BattleDirectConsequenceContingentProjection projection in plan.ContingentProjections)
            if (projection.ContingentId.Value == contingentId) return projection;
        Assert.Fail("Missing contingent projection: " + contingentId);
        return null;
    }

    private static bool ContainsCohort(
        IReadOnlyList<ContingentManpowerCohort> cohorts,
        ManpowerInjuryState injury,
        ManpowerCustodyState custody,
        ManpowerAvailabilityState availability,
        long amount)
    {
        foreach (ContingentManpowerCohort cohort in cohorts)
            if (cohort.InjuryState == injury
                && cohort.CustodyState == custody
                && cohort.AvailabilityState == availability
                && cohort.Amount == amount)
                return true;
        return false;
    }

    private static CaptureWorldState Capture(Fixture fixture)
    {
        Assert.That(fixture.World.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Dictionary<string, string> manpowerFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, long> contingentAmounts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ContingentManpowerState state in fixture.World.ContingentManpowerStateStore.States)
        {
            manpowerFingerprints.Add(state.ContingentId.Value, state.Fingerprint);
            Assert.That(fixture.World.ArmedForceStore.TryGetContingent(
                state.ContingentId, out ContingentRecord contingent), Is.True);
            contingentAmounts.Add(state.ContingentId.Value, contingent.Amount);
        }
        List<string> positions = new List<string>();
        foreach (ArmedForceSpatialPosition position in fixture.World.ArmedForceSpatialStateStore.Positions)
            positions.Add(position.StableKey + "=" + position.Position.StableKey);
        List<string> personIds = new List<string>();
        foreach (PersonRuntime person in fixture.World.PersonStore.Persons)
            personIds.Add(person.PersonId.Value);
        return new CaptureWorldState(
            fixture.World.CurrentDay,
            fixture.World.BattleStore.Revision,
            battle.LifecycleState,
            battle.StartedAbsoluteDay,
            fixture.World.ArmedForceStore.Revision,
            fixture.World.ArmedForceSpatialStateStore.Revision,
            positions,
            fixture.World.ContingentManpowerStateStore.Revision,
            manpowerFingerprints,
            contingentAmounts,
            fixture.World.PersonStore.Persons.Count,
            personIds,
            fixture.World.NpcRuntimes.Count,
            new Dictionary<string, long>(fixture.PopulationRevisions, StringComparer.Ordinal),
            new Dictionary<string, int>(fixture.Populations, StringComparer.Ordinal));
    }

    private static void AssertWorldUnchanged(Fixture fixture, CaptureWorldState before)
    {
        Assert.That(fixture.World.CurrentDay, Is.EqualTo(before.Day));
        Assert.That(fixture.World.BattleStore.Revision, Is.EqualTo(before.BattleRevision));
        Assert.That(fixture.World.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(before.BattleLifecycle));
        Assert.That(battle.StartedAbsoluteDay, Is.EqualTo(before.BattleStartedDay));
        Assert.That(fixture.World.ArmedForceStore.Revision, Is.EqualTo(before.ForceRevision));
        Assert.That(fixture.World.ArmedForceSpatialStateStore.Revision, Is.EqualTo(before.SpatialRevision));
        List<string> currentPositions = new List<string>();
        foreach (ArmedForceSpatialPosition position in fixture.World.ArmedForceSpatialStateStore.Positions)
            currentPositions.Add(position.StableKey + "=" + position.Position.StableKey);
        CollectionAssert.AreEqual(before.Positions, currentPositions);
        Assert.That(fixture.World.ContingentManpowerStateStore.Revision, Is.EqualTo(before.ManpowerRevision));
        Assert.That(fixture.World.ContingentManpowerStateStore.States.Count, Is.EqualTo(before.ManpowerFingerprints.Count));
        foreach (ContingentManpowerState state in fixture.World.ContingentManpowerStateStore.States)
        {
            Assert.That(before.ManpowerFingerprints.TryGetValue(state.ContingentId.Value, out string fingerprint), Is.True);
            Assert.That(state.Fingerprint, Is.EqualTo(fingerprint));
            Assert.That(fixture.World.ArmedForceStore.TryGetContingent(
                state.ContingentId, out ContingentRecord contingent), Is.True);
            Assert.That(contingent.Amount, Is.EqualTo(before.ContingentAmounts[state.ContingentId.Value]));
        }
        Assert.That(fixture.World.PersonStore.Persons.Count, Is.EqualTo(before.PersonCount));
        List<string> currentPersonIds = new List<string>();
        foreach (PersonRuntime person in fixture.World.PersonStore.Persons)
            currentPersonIds.Add(person.PersonId.Value);
        CollectionAssert.AreEqual(before.PersonIds, currentPersonIds);
        Assert.That(fixture.World.NpcRuntimes.Count, Is.EqualTo(before.NpcCount));
        foreach (KeyValuePair<string, CityRuntime> entry in fixture.CitiesBySource)
        {
            Assert.That(entry.Value.Population.Revision, Is.EqualTo(before.PopulationRevisions[entry.Key]));
            Assert.That(entry.Value.Population.CurrentPopulation, Is.EqualTo(before.Populations[entry.Key]));
        }
    }

    private static Fixture CreateFixture(
        IReadOnlyList<SeedSpec> seeds,
        IDictionary<string, string> participantSides,
        DelegateRule rule,
        bool configureD5 = true,
        string suffix = "main",
        IEnumerable<string> extraForces = null,
        long sourceCapacity = 1000000000L)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forceStore = new ArmedForceStore(persons);
        HashSet<string> forceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SeedSpec seed in seeds) forceIds.Add(seed.ForceId);
        foreach (string forceId in participantSides.Keys) forceIds.Add(forceId);
        if (extraForces != null)
            foreach (string forceId in extraForces) forceIds.Add(forceId);
        foreach (string forceId in forceIds)
            Assert.That(forceStore.TryRegister(
                new ArmedForceRecord(new ArmedForceId(forceId), forceId, 0L),
                out ArmedForceFoundationFailure forceFailure), Is.True, forceFailure.ToString());

        Dictionary<string, List<SeedSpec>> seedsByContingent = new Dictionary<string, List<SeedSpec>>(StringComparer.Ordinal);
        foreach (SeedSpec seed in seeds)
        {
            if (!seedsByContingent.TryGetValue(seed.ContingentId, out List<SeedSpec> group))
            {
                group = new List<SeedSpec>();
                seedsByContingent.Add(seed.ContingentId, group);
            }
            group.Add(seed);
        }
        foreach (KeyValuePair<string, List<SeedSpec>> entry in seedsByContingent)
        {
            SeedSpec first = entry.Value[0];
            long legacyAmount = 0L;
            foreach (SeedSpec seed in entry.Value)
            {
                Assert.That(seed.ForceId, Is.EqualTo(first.ForceId));
                Assert.That(seed.SourceId, Is.EqualTo(first.SourceId));
                if (first.SourceId == null) legacyAmount = checked(legacyAmount + seed.Amount);
            }
            Assert.That(forceStore.TryRegisterContingent(new ContingentRecord(
                new ContingentId(first.ContingentId),
                new ArmedForceId(first.ForceId),
                legacyAmount,
                new ContingentOriginReference("d6b2-test", first.ContingentId),
                "test-service"), out ArmedForceFoundationFailure contingentFailure), Is.True, contingentFailure.ToString());
        }

        Dictionary<string, CityRuntime> citiesBySource = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        foreach (SeedSpec seed in seeds)
        {
            if (seed.SourceId == null || citiesBySource.ContainsKey(seed.SourceId)) continue;
            CityData data = SimulationTestFactory.CreateCityData("d6b2-source-" + suffix + "-" + seed.SourceId);
            data.initialPopulation = 1000;
            citiesBySource.Add(seed.SourceId, new CityRuntime(
                "city-" + suffix + "-" + seed.SourceId,
                data,
                new SpatialLocationRuntime("location-" + suffix + "-" + seed.SourceId)));
        }
        List<SettlementManpowerSourceRegistration> registrations = new List<SettlementManpowerSourceRegistration>();
        foreach (KeyValuePair<string, CityRuntime> entry in citiesBySource)
            registrations.Add(new SettlementManpowerSourceRegistration(
                new ManpowerSourceId(entry.Key), entry.Value, sourceCapacity));

        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        HexId battleHex = new HexId("d6b2-hex-" + suffix);
        Assert.That(authority.TryRegisterHex(new HexRecord(battleHex), out _), Is.True);
        SimulationTime time = new SimulationTime(1L);
        SimulationRuntime world = new SimulationRuntime(
            time,
            new List<CityRuntime>(citiesBySource.Values),
            null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forceStore,
            spatialAuthorityStore: authority,
            battleResolutionPolicy: configureD5 ? CreateD5Policy() : null,
            settlementManpowerSourceRegistrations: registrations,
            battleDirectConsequencePolicy: rule == null ? null : new BattleDirectConsequencePolicy(rule));

        foreach (KeyValuePair<string, List<SeedSpec>> entry in seedsByContingent)
        {
            SeedSpec first = entry.Value[0];
            if (first.SourceId == null) continue;
            Assert.That(world.ContingentManpowerStateStore.SourceProvider.TryGetSnapshot(
                new ManpowerSourceId(first.SourceId), out ManpowerSourceCapacitySnapshot sourceSnapshot), Is.True);
            Assert.That(world.ContingentManpowerStateStore.TryGet(
                new ContingentId(first.ContingentId), out ContingentManpowerState state), Is.True);
            Assert.That(world.ContingentManpowerStateStore.TrySetSourceBinding(
                state.ContingentId,
                new ManpowerSourceId(first.SourceId),
                state.Revision,
                sourceSnapshot.Fingerprint,
                out ContingentManpowerFailure bindingFailure), Is.True, bindingFailure.ToString());
            foreach (SeedSpec seed in entry.Value)
            {
                Assert.That(world.ContingentManpowerStateStore.TryGet(
                    new ContingentId(seed.ContingentId), out state), Is.True);
                Assert.That(world.ContingentManpowerStateStore.TryAllocate(
                    state.ContingentId,
                    seed.Amount,
                    seed.InjuryState,
                    seed.CustodyState,
                    seed.CustodianForceId == null ? null : new ArmedForceId(seed.CustodianForceId),
                    seed.AvailabilityState,
                    state.Revision,
                    sourceSnapshot.Fingerprint,
                    out ContingentManpowerFailure allocationFailure), Is.True, allocationFailure.ToString());
            }
        }

        foreach (string forceId in participantSides.Keys)
            Assert.That(world.ArmedForceSpatialStateStore.TrySetPosition(
                new ArmedForceId(forceId), SpatialReference.ForHex(battleHex),
                out ArmedForceSpatialFailure positionFailure), Is.True, positionFailure.ToString());

        BattleId battleId = new BattleId("battle-" + suffix);
        List<BattleStateSide> sides = new List<BattleStateSide>();
        HashSet<string> uniqueSides = new HashSet<string>(participantSides.Values, StringComparer.Ordinal);
        foreach (string sideId in uniqueSides)
            sides.Add(new BattleStateSide(battleId, new BattleSideId(sideId), sideId));
        List<BattleParticipantBinding> bindings = new List<BattleParticipantBinding>();
        foreach (KeyValuePair<string, string> participant in participantSides)
            bindings.Add(new BattleParticipantBinding(
                new BattleParticipantBindingId("binding-" + suffix + "-" + participant.Key),
                battleId,
                new BattleSideId(participant.Value),
                new ArmedForceId(participant.Key)));
        Assert.That(world.BattleStore.TryRegister(new PersistentBattleRecord(
            battleId,
            0L,
            lifecycleState: BattleLifecycleState.Pending,
            sides: sides,
            participantBindings: bindings,
            locationReference: SpatialReference.ForHex(battleHex)),
            out PersistentStateFailure battleRegistrationFailure), Is.True, battleRegistrationFailure.ToString());
        Assert.That(world.BattleStore.TryStart(
            battleId, world.CurrentDay, out PersistentStateFailure startFailure), Is.True, startFailure.ToString());

        Dictionary<string, long> revisions = new Dictionary<string, long>(StringComparer.Ordinal);
        Dictionary<string, int> populations = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, CityRuntime> city in citiesBySource)
        {
            revisions.Add(city.Key, city.Value.Population.Revision);
            populations.Add(city.Key, city.Value.Population.CurrentPopulation);
        }
        return new Fixture(world, time, battleId, citiesBySource, revisions, populations);
    }

    private static BattleResolutionPolicy CreateD5Policy()
        => new BattleResolutionPolicy(
            new TestCapabilityProvider(),
            new DeterministicBattleConflictRandomSource(19, "test-d6b2-random:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            "battle-conflict-projection:v1",
            NumericProfile,
            new BattleNumericExecutionProfileCompatibility(new[] { NumericProfile }));

    private static SeedSpec[] StandardSeeds(string suffix = "")
        => new[]
        {
            Seed("force-a" + suffix, "contingent-a" + suffix, "source-a" + suffix, 2L),
            Seed("force-b" + suffix, "contingent-b" + suffix, "source-b" + suffix, 2L)
        };

    private static Dictionary<string, string> StandardParticipants(string suffix = "")
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "force-a" + suffix, "side-a" + suffix },
            { "force-b" + suffix, "side-b" + suffix }
        };

    private static SeedSpec Seed(
        string forceId,
        string contingentId,
        string sourceId,
        long amount,
        ManpowerInjuryState injury = ManpowerInjuryState.Healthy,
        ManpowerCustodyState custody = ManpowerCustodyState.Free,
        string custodian = null,
        ManpowerAvailabilityState availability = ManpowerAvailabilityState.Available)
        => new SeedSpec(forceId, contingentId, sourceId, amount, injury, custody, custodian, availability);

    private static void AddUnrelatedPopulationChange(Fixture fixture, string sourceId)
        => ApplyPopulationChange(fixture.CitiesBySource[sourceId].Population, new PopulationChangeSet(1, 0, 0, 0));

    private sealed class SeedSpec
    {
        public readonly string ForceId;
        public readonly string ContingentId;
        public readonly string SourceId;
        public readonly long Amount;
        public readonly ManpowerInjuryState InjuryState;
        public readonly ManpowerCustodyState CustodyState;
        public readonly string CustodianForceId;
        public readonly ManpowerAvailabilityState AvailabilityState;

        public SeedSpec(string forceId, string contingentId, string sourceId, long amount,
            ManpowerInjuryState injuryState, ManpowerCustodyState custodyState,
            string custodianForceId, ManpowerAvailabilityState availabilityState)
        {
            ForceId = forceId;
            ContingentId = contingentId;
            SourceId = sourceId;
            Amount = amount;
            InjuryState = injuryState;
            CustodyState = custodyState;
            CustodianForceId = custodianForceId;
            AvailabilityState = availabilityState;
        }
    }

    private sealed class Fixture
    {
        public readonly SimulationRuntime World;
        public readonly SimulationTime Time;
        public readonly BattleId BattleId;
        public readonly Dictionary<string, CityRuntime> CitiesBySource;
        public readonly Dictionary<string, long> PopulationRevisions;
        public readonly Dictionary<string, int> Populations;

        public Fixture(SimulationRuntime world, SimulationTime time, BattleId battleId,
            Dictionary<string, CityRuntime> cities, Dictionary<string, long> revisions,
            Dictionary<string, int> populations)
        {
            World = world;
            Time = time;
            BattleId = battleId;
            CitiesBySource = cities;
            PopulationRevisions = revisions;
            Populations = populations;
        }
    }

    private sealed class CaptureWorldState
    {
        public readonly long Day;
        public readonly long BattleRevision;
        public readonly BattleLifecycleState BattleLifecycle;
        public readonly long? BattleStartedDay;
        public readonly long ForceRevision;
        public readonly long SpatialRevision;
        public readonly List<string> Positions;
        public readonly long ManpowerRevision;
        public readonly Dictionary<string, string> ManpowerFingerprints;
        public readonly Dictionary<string, long> ContingentAmounts;
        public readonly int PersonCount;
        public readonly List<string> PersonIds;
        public readonly int NpcCount;
        public readonly Dictionary<string, long> PopulationRevisions;
        public readonly Dictionary<string, int> Populations;

        public CaptureWorldState(long day, long battleRevision, BattleLifecycleState battleLifecycle,
            long? battleStartedDay, long forceRevision, long spatialRevision, List<string> positions,
            long manpowerRevision, Dictionary<string, string> manpowerFingerprints,
            Dictionary<string, long> contingentAmounts, int personCount, List<string> personIds, int npcCount,
            Dictionary<string, long> populationRevisions, Dictionary<string, int> populations)
        {
            Day = day;
            BattleRevision = battleRevision;
            BattleLifecycle = battleLifecycle;
            BattleStartedDay = battleStartedDay;
            ForceRevision = forceRevision;
            SpatialRevision = spatialRevision;
            Positions = positions;
            ManpowerRevision = manpowerRevision;
            ManpowerFingerprints = manpowerFingerprints;
            ContingentAmounts = contingentAmounts;
            PersonCount = personCount;
            PersonIds = personIds;
            NpcCount = npcCount;
            PopulationRevisions = populationRevisions;
            Populations = populations;
        }
    }

    private sealed class DelegateRule : IBattleDirectConsequenceRule
    {
        private readonly Func<BattleDirectConsequenceInput, IReadOnlyList<BattleCohortConsequencePartition>> evaluate;
        public string RuleKey { get; set; }
        public string ConfigurationIdentity { get; set; } = "test-configuration:v1";
        public int Version { get; set; } = 1;
        public int CallCount { get; private set; }
        public BattleDirectConsequenceInput LastInput { get; private set; }

        public DelegateRule(string ruleKey,
            Func<BattleDirectConsequenceInput, IReadOnlyList<BattleCohortConsequencePartition>> evaluate)
        {
            RuleKey = ruleKey;
            this.evaluate = evaluate;
        }

        public IReadOnlyList<BattleCohortConsequencePartition> Evaluate(BattleDirectConsequenceInput input)
        {
            CallCount++;
            LastInput = input;
            return evaluate(input);
        }
    }

    private sealed class TestCapabilityProvider : IBattleContingentCapabilityProvider
    {
        public string RuleKey => "test-d6b2-capability:v1";
        public bool TryEvaluate(BattleExecutionContingentSnapshot contingent,
            out float capability, out string failureReason)
        {
            capability = 1f;
            failureReason = null;
            return true;
        }
    }
}
