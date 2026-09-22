using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class BattleResolutionComputationTests
{
    private static readonly BattleResolutionResolverSettings ResolverSettings =
        new BattleResolutionResolverSettings(0.2f, 0.01f);

    [Test]
    public void Projection_PreservesEachDirectContingentAndAllSidesWithTypedMappings()
    {
        BattleResolutionFixture fixture = CreateFixture();
        TestCapabilityProvider capabilities = CreateCapabilities();
        CountingBattleRandomSource random = new CountingBattleRandomSource();
        BattleResolutionComputationService service = CreateService(fixture, capabilities, random);
        long battleRevision = fixture.Battles.Revision;
        long forceRevision = fixture.Forces.Revision;
        long spatialRevision = fixture.Spatial.Revision;

        Assert.That(service.TryCompute(
            fixture.Context,
            1L,
            out BattleResolutionComputation computation,
            out BattleResolutionComputationFailure failure), Is.True, failure?.Message);

        Assert.That(computation.ProjectionVersion, Is.EqualTo("battle-conflict-projection:v1"));
        Assert.That(computation.SideMappings, Has.Count.EqualTo(3));
        Assert.That(computation.ParticipantMappings, Has.Count.EqualTo(5));
        Assert.That(computation.RawResult.SideResults, Has.Count.EqualTo(3));
        Assert.That(computation.ProjectedLocationRuntimeId, Is.Null);
        Assert.That(computation.ProjectedModifierCount, Is.Zero);
        Assert.That(computation.RawResult.NpcConsequences, Is.Empty);
        Assert.That(computation.RawResult.AggregateConsequences, Is.Empty);
        Assert.That(random.OperationKeys, Has.Count.EqualTo(3));
        Assert.That(capabilities.EvaluatedContingentIds, Is.EqualTo(new[]
        {
            "contingent-a-1", "contingent-a-2", "contingent-b-1", "contingent-c-1"
        }));
        BattleExecutionContingentSnapshot capturedWithCharacteristics = capabilities.ObservedContingents[0];
        Assert.That(capturedWithCharacteristics.Origin.Domain, Is.EqualTo("source"));
        Assert.That(capturedWithCharacteristics.Origin.Value, Is.EqualTo("origin-a1"));
        Assert.That(capturedWithCharacteristics.ServiceType, Is.EqualTo("service-a"));
        Assert.That(capturedWithCharacteristics.Characteristics[0].Key, Is.EqualTo("alpha"));
        Assert.That(capturedWithCharacteristics.Characteristics[1].Key, Is.EqualTo("zeta"));

        foreach (BattleResolutionSideMapping side in computation.SideMappings)
        {
            Assert.That(side.LowerSideId, Is.EqualTo(side.SideId.Value));
            Assert.That(side.TransitionalObjective, Is.EqualTo(ConflictObjectiveType.Other));
            Assert.That(side.TransitionalStakes, Is.EqualTo(ConflictStakes.Low));
            Assert.That(computation.TryGetSideMapping(side.LowerSideId, out BattleResolutionSideMapping lookedUp), Is.True);
            Assert.That(lookedUp.SideId, Is.EqualTo(side.SideId));
        }

        foreach (BattleResolutionParticipantMapping participant in computation.ParticipantMappings)
        {
            Assert.That(participant.AggregateSourceId, Does.StartWith("battle-contingent:v1|"));
            Assert.That(participant.AggregateSourceId, Does.Not.Contain("Force A"));
            Assert.That(participant.AggregateCount, Is.Null);
            Assert.That(participant.Amount, Is.GreaterThanOrEqualTo(0L));
            Assert.That(computation.TryGetParticipantMapping(
                participant.LowerParticipantId,
                out BattleResolutionParticipantMapping lookedUp), Is.True);
            Assert.That(lookedUp.ContingentId, Is.EqualTo(participant.ContingentId));
            Assert.That(lookedUp.ForceId, Is.EqualTo(participant.ForceId));
            Assert.That(lookedUp.SideId, Is.EqualTo(participant.SideId));
        }

        foreach (ConflictSideResolutionResult sideResult in computation.RawResult.SideResults)
        {
            Assert.That(computation.TryGetSideMapping(sideResult.SideId, out BattleResolutionSideMapping sideMapping), Is.True);
            Assert.That(sideMapping.SideId.Value, Is.EqualTo(sideResult.SideId));
            foreach (ConflictParticipantCapabilityResult contribution in sideResult.ParticipantContributions)
            {
                Assert.That(computation.TryGetParticipantMapping(
                    contribution.ParticipantId,
                    out BattleResolutionParticipantMapping participantMapping), Is.True);
                Assert.That(participantMapping.SideId, Is.EqualTo(sideMapping.SideId));
                Assert.That(participantMapping.AggregateSourceId, Is.EqualTo(contribution.SourceId));
                Assert.That(participantMapping.Capability, Is.EqualTo(contribution.RawCapability));
                Assert.That(participantMapping.Capability, Is.EqualTo(contribution.EffectiveCapability));
            }
        }

        BattleResolutionParticipantMapping longAmount = FindParticipant(computation, "contingent-a-1");
        Assert.That(longAmount.Amount, Is.EqualTo(3000000000L));
        Assert.That(longAmount.AggregateCount, Is.Null, "long Amount must never be truncated into lower-level int Count");
        Assert.That(longAmount.Capability, Is.EqualTo(2.5f));
        ConflictParticipantCapabilityResult lowerParticipant = FindLowerParticipant(computation, longAmount.LowerParticipantId);
        Assert.That(lowerParticipant.RawCapability, Is.EqualTo(longAmount.Capability));
        Assert.That(lowerParticipant.EffectiveCapability, Is.EqualTo(longAmount.Capability));
        BattleResolutionParticipantMapping zeroAmount = FindParticipant(computation, "contingent-c-0");
        Assert.That(zeroAmount.Amount, Is.Zero);
        Assert.That(zeroAmount.AggregateCount, Is.Null);
        Assert.That(computation.RawResult.SideResults[0].ParticipantContributions,
            Has.All.Property("Kind").EqualTo(ConflictParticipantKind.Aggregate));
        Assert.That(fixture.Persons.Persons, Is.Empty);
        Assert.That(fixture.Battles.Revision, Is.EqualTo(battleRevision));
        Assert.That(fixture.Forces.Revision, Is.EqualTo(forceRevision));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(spatialRevision));
        Assert.That(fixture.Battles.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));

        string rawResultKey = BuildRawResultStableKey(computation.RawResult);
        string sourceContextFingerprint = computation.SourceContextFingerprint;
        string causalFingerprint = computation.CausalFingerprint;
        Assert.That(fixture.Forces.TryReplaceContingent(new ContingentRecord(
            new ContingentId("contingent-a-1"),
            new ArmedForceId("force-a"),
            3000000001L,
            new ContingentOriginReference("source", "origin-a1"),
            "service-a",
            new[] { new ArmedForceCharacteristic("alpha", "first"), new ArmedForceCharacteristic("zeta", "last") }), out _), Is.True);
        Assert.That(computation.CausalFingerprint, Is.EqualTo(causalFingerprint));
        Assert.That(computation.SourceContextFingerprint, Is.EqualTo(sourceContextFingerprint));
        Assert.That(FindParticipant(computation, "contingent-a-1").Amount, Is.EqualTo(3000000000L));
        Assert.That(BuildRawResultStableKey(computation.RawResult), Is.EqualTo(rawResultKey));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<BattleResolutionParticipantMapping>)computation.ParticipantMappings).Add(longAmount));
    }

    [Test]
    public void ZeroAvailabilityDirectContingent_IsProjectedWithZeroWithoutCallingCapabilityProvider()
    {
        BattleResolutionFixture fixture = CreateFixture();
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(fixture.Forces);
        Assert.That(manpower.TryRedistribute(new ContingentId("contingent-a-2"), new[]
        {
            new ContingentManpowerCohort(
                ManpowerInjuryState.Healthy,
                ManpowerCustodyState.Free,
                null,
                ManpowerAvailabilityState.Unavailable,
                10L)
        }, 0L, out ContingentManpowerFailure redistributionFailure), Is.True, redistributionFailure?.ToString());

        BattleExecutionContextBuilder builder = new BattleExecutionContextBuilder(
            fixture.Battles,
            fixture.Forces,
            fixture.Spatial,
            fixture.Spatial.SpatialAuthorityStore,
            personStore: fixture.Persons,
            manpowerStateStore: manpower);
        Assert.That(builder.TryCreate(fixture.BattleId, 1L, out BattleExecutionContext context,
            out BattleExecutionFailure contextFailure), Is.True, contextFailure?.ToString());

        TestCapabilityProvider provider = CreateCapabilities();
        provider.Values["contingent-a-2"] = float.MaxValue;
        BattleResolutionComputationService service = new BattleResolutionComputationService(
            builder,
            provider,
            ResolverSettings,
            new CountingBattleRandomSource());
        Assert.That(service.TryCompute(context, 1L, out BattleResolutionComputation computation,
            out BattleResolutionComputationFailure failure), Is.True, failure?.Message);

        BattleResolutionParticipantMapping unavailable = FindParticipant(computation, "contingent-a-2");
        Assert.That(unavailable.Amount, Is.EqualTo(10L));
        Assert.That(unavailable.Capability, Is.Zero,
            "Unavailable roster stays represented but cannot be assigned combat capability by a fixed provider.");
        Assert.That(provider.EvaluatedContingentIds, Does.Not.Contain("contingent-a-2"));
        Assert.That(FindLowerParticipant(computation, unavailable.LowerParticipantId).RawCapability, Is.Zero);
    }

    [Test]
    public void ExplicitParentAndChildBindingsProjectOnlyCapturedDirectContingents()
    {
        BattleResolutionFixture fixture = CreateFixture(hierarchicalSideA: true);
        BattleExecutionSideContext sideA = fixture.Context.Sides[0];
        Assert.That(sideA.SideId.Value, Is.EqualTo("side-a"));
        Assert.That(sideA.Forces, Has.Count.EqualTo(2));
        Assert.That(FindForce(sideA, "force-parent").DirectContingents, Is.Empty);
        Assert.That(FindForce(sideA, "force-a").DirectContingents, Has.Count.EqualTo(2));

        BattleResolutionComputationService service = CreateService(
            fixture,
            CreateCapabilities(),
            new CountingBattleRandomSource());
        Assert.That(service.TryCompute(fixture.Context, 1L, out BattleResolutionComputation computation, out BattleResolutionComputationFailure failure), Is.True, failure?.Message);
        Assert.That(computation.ParticipantMappings, Has.Count.EqualTo(5));
        Assert.That(computation.ParticipantMappings, Has.None.Property("ForceId").EqualTo(new ArmedForceId("force-parent")));
        Assert.That(computation.ParticipantMappings, Has.None.Property("ContingentId").EqualTo(new ContingentId("contingent-grandchild")));
        Assert.That(FindParticipant(computation, "contingent-a-1").ForceId.Value, Is.EqualTo("force-a"));
        Assert.That(FindParticipant(computation, "contingent-a-2").ForceId.Value, Is.EqualTo("force-a"));
    }

    [Test]
    public void InvalidOrStaleContext_ReturnsNoComputationAndUsesNoProviderOrRandomness()
    {
        BattleResolutionFixture fixture = CreateFixture();
        TestCapabilityProvider capabilities = CreateCapabilities();
        CountingBattleRandomSource random = new CountingBattleRandomSource();
        BattleResolutionComputationService service = CreateService(fixture, capabilities, random);

        Assert.That(service.TryCompute(null, 1L, out BattleResolutionComputation invalid, out BattleResolutionComputationFailure invalidFailure), Is.False);
        Assert.That(invalid, Is.Null);
        Assert.That(invalidFailure.Code, Is.EqualTo(BattleResolutionComputationFailureCode.ContextInvalid));
        Assert.That(capabilities.CallCount, Is.Zero);
        Assert.That(random.OperationKeys, Is.Empty);

        Assert.That(fixture.Forces.TryReplaceContingent(new ContingentRecord(
            new ContingentId("contingent-a-1"),
            new ArmedForceId("force-a"),
            99L,
            new ContingentOriginReference("source", "origin-a1"),
            "service-a"), out _), Is.True);
        Assert.That(service.TryCompute(fixture.Context, 1L, out BattleResolutionComputation stale, out BattleResolutionComputationFailure staleFailure), Is.False);
        Assert.That(stale, Is.Null);
        Assert.That(staleFailure.Code, Is.EqualTo(BattleResolutionComputationFailureCode.ContextStale));
        Assert.That(staleFailure.ValidationReport.IsStale, Is.True);
        Assert.That(capabilities.CallCount, Is.Zero);
        Assert.That(random.OperationKeys, Is.Empty);
    }

    [Test]
    public void InvalidCapabilitiesAndSideOverflowFailBeforeRandomness()
    {
        foreach (float invalidCapability in new[] { float.NaN, float.PositiveInfinity, -0.25f })
        {
            BattleResolutionFixture fixture = CreateFixture();
            TestCapabilityProvider capabilities = CreateCapabilities();
            capabilities.Values.Clear();
            capabilities.FallbackCapability = invalidCapability;
            CountingBattleRandomSource random = new CountingBattleRandomSource();
            BattleResolutionComputationService service = CreateService(fixture, capabilities, random);

            Assert.That(service.TryCompute(fixture.Context, 1L, out BattleResolutionComputation computation, out BattleResolutionComputationFailure failure), Is.False);
            Assert.That(computation, Is.Null);
            Assert.That(failure.Code, Is.EqualTo(BattleResolutionComputationFailureCode.InvalidCapability));
            Assert.That(random.OperationKeys, Is.Empty);
        }

        BattleResolutionFixture overflowFixture = CreateFixture();
        TestCapabilityProvider overflowCapabilities = CreateCapabilities();
        overflowCapabilities.Values["contingent-a-1"] = float.MaxValue;
        overflowCapabilities.Values["contingent-a-2"] = float.MaxValue;
        CountingBattleRandomSource overflowRandom = new CountingBattleRandomSource();
        BattleResolutionComputationService overflowService = CreateService(overflowFixture, overflowCapabilities, overflowRandom);
        Assert.That(overflowService.TryCompute(overflowFixture.Context, 1L, out _, out BattleResolutionComputationFailure overflowFailure), Is.False);
        Assert.That(overflowFailure.Code, Is.EqualTo(BattleResolutionComputationFailureCode.SideCapabilityOverflow));
        Assert.That(overflowRandom.OperationKeys, Is.Empty);

        BattleResolutionFixture maximumScoreFixture = CreateFixture();
        TestCapabilityProvider maximumScoreCapabilities = CreateCapabilities();
        maximumScoreCapabilities.Values.Clear();
        maximumScoreCapabilities.FallbackCapability = 0f;
        maximumScoreCapabilities.Values.Add("contingent-a-1", float.MaxValue);
        CountingBattleRandomSource maximumScoreRandom = new CountingBattleRandomSource();
        BattleResolutionComputationService maximumScoreService = CreateService(
            maximumScoreFixture,
            maximumScoreCapabilities,
            maximumScoreRandom);
        Assert.That(maximumScoreService.TryCompute(maximumScoreFixture.Context, 1L, out _, out BattleResolutionComputationFailure maximumScoreFailure), Is.False);
        Assert.That(maximumScoreFailure.Code, Is.EqualTo(BattleResolutionComputationFailureCode.SideCapabilityOverflow));
        Assert.That(maximumScoreRandom.OperationKeys, Is.Empty);
    }

    [Test]
    public void ProviderRejectionAndChangedRuleIdentityFailBeforeRandomness()
    {
        BattleResolutionFixture fixture = CreateFixture();
        TestCapabilityProvider rejectingProvider = CreateCapabilities();
        rejectingProvider.RejectedContingentId = "contingent-a-1";
        CountingBattleRandomSource random = new CountingBattleRandomSource();
        BattleResolutionComputationService service = CreateService(fixture, rejectingProvider, random);

        Assert.That(service.TryCompute(fixture.Context, 1L, out _, out BattleResolutionComputationFailure rejected), Is.False);
        Assert.That(rejected.Code, Is.EqualTo(BattleResolutionComputationFailureCode.CapabilityProjectionFailed));
        Assert.That(random.OperationKeys, Is.Empty);

        TestCapabilityProvider changingProvider = CreateCapabilities();
        CountingBattleRandomSource secondRandom = new CountingBattleRandomSource();
        BattleResolutionComputationService secondService = CreateService(fixture, changingProvider, secondRandom);
        changingProvider.RuleKey = "different-rule-after-construction";
        Assert.That(secondService.TryCompute(fixture.Context, 1L, out _, out BattleResolutionComputationFailure changed), Is.False);
        Assert.That(changed.Code, Is.EqualTo(BattleResolutionComputationFailureCode.RuleIdentityChanged));
        Assert.That(secondRandom.OperationKeys, Is.Empty);
    }

    [Test]
    public void ZeroCapabilityIsValidAndLowerLevelCanReturnDraw()
    {
        BattleResolutionFixture fixture = CreateFixture();
        TestCapabilityProvider capabilities = CreateCapabilities();
        capabilities.FallbackCapability = 0f;
        capabilities.Values.Clear();
        CountingBattleRandomSource random = new CountingBattleRandomSource();
        BattleResolutionComputationService service = CreateService(fixture, capabilities, random);

        Assert.That(service.TryCompute(fixture.Context, 1L, out BattleResolutionComputation computation, out BattleResolutionComputationFailure failure), Is.True, failure?.Message);
        Assert.That(computation.RawResult.Outcome, Is.EqualTo(ConflictOutcomeType.Draw));
        Assert.That(computation.RawResult.WinningSideId, Is.Null);
        Assert.That(computation.RawResult.SideResults, Has.All.Property("RawCapability").EqualTo(0f));
        Assert.That(random.OperationKeys, Has.Count.EqualTo(3));
    }

    [Test]
    public void InsertionOrderAndNonCausalCommandMetadataDoNotChangeCausalResult()
    {
        BattleResolutionFixture forward = CreateFixture(reverseInsertion: false);
        BattleResolutionFixture reverse = CreateFixture(reverseInsertion: true);
        TestCapabilityProvider forwardCapabilities = CreateCapabilities();
        TestCapabilityProvider reverseCapabilities = CreateCapabilities();
        DeterministicBattleConflictRandomSource random = new DeterministicBattleConflictRandomSource(81, "test-world");
        BattleResolutionComputationService forwardService = CreateService(forward, forwardCapabilities, random);
        BattleResolutionComputationService reverseService = CreateService(reverse, reverseCapabilities, random);

        Assert.That(forwardService.TryCompute(forward.Context, 1L, out BattleResolutionComputation first, out BattleResolutionComputationFailure firstFailure), Is.True, firstFailure?.Message);
        Assert.That(reverseService.TryCompute(reverse.Context, 1L, out BattleResolutionComputation second, out BattleResolutionComputationFailure secondFailure), Is.True, secondFailure?.Message);
        Assert.That(forward.Context.StableKey, Is.EqualTo(reverse.Context.StableKey));
        Assert.That(first.CausalFingerprint, Is.EqualTo(second.CausalFingerprint));
        Assert.That(first.AdaptedConflictId, Is.EqualTo(second.AdaptedConflictId));
        AssertRawResultsEqual(first.RawResult, second.RawResult);

        PersonId commanderA = new PersonId("commander-a");
        PersonId commanderB = new PersonId("commander-b");
        Assert.That(forward.Persons.TryRegister(new PersonRuntime(commanderA), out _), Is.True);
        Assert.That(forward.Persons.TryRegister(new PersonRuntime(commanderB), out _), Is.True);
        Assert.That(forward.Builder.TryCreate(
            forward.BattleId,
            1L,
            new BattleExecutionPlan(new[] { new BattleExecutionSideCommander(new BattleSideId("side-a"), commanderA) }),
            out BattleExecutionContext withCommanderA,
            out BattleExecutionFailure commanderFailureA), Is.True, commanderFailureA.ToString());
        Assert.That(forward.Builder.TryCreate(
            forward.BattleId,
            1L,
            new BattleExecutionPlan(new[] { new BattleExecutionSideCommander(new BattleSideId("side-a"), commanderB) }),
            out BattleExecutionContext withCommanderB,
            out BattleExecutionFailure commanderFailureB), Is.True, commanderFailureB.ToString());
        Assert.That(withCommanderA.StableKey, Is.Not.EqualTo(withCommanderB.StableKey));

        Assert.That(forwardService.TryCompute(withCommanderA, 1L, out BattleResolutionComputation commanderResultA, out BattleResolutionComputationFailure commanderResultFailureA), Is.True, commanderResultFailureA?.Message);
        Assert.That(forwardService.TryCompute(withCommanderB, 1L, out BattleResolutionComputation commanderResultB, out BattleResolutionComputationFailure commanderResultFailureB), Is.True, commanderResultFailureB?.Message);
        Assert.That(commanderResultA.CausalFingerprint, Is.EqualTo(commanderResultB.CausalFingerprint));
        Assert.That(commanderResultA.AdaptedConflictId, Is.EqualTo(commanderResultB.AdaptedConflictId));
        AssertRawResultsEqual(commanderResultA.RawResult, commanderResultB.RawResult);
        Assert.That(forward.Persons.Persons, Has.Count.EqualTo(2));
        Assert.That(forward.Persons.Persons, Has.All.Property("IsMaterialized").False);
    }

    [Test]
    public void CausalFingerprintIncludesRulesSettingsAndRandomAuthorityIdentity()
    {
        BattleResolutionFixture fixture = CreateFixture();
        TestCapabilityProvider baselineProvider = CreateCapabilities();
        DeterministicBattleConflictRandomSource baselineRandom = new DeterministicBattleConflictRandomSource(5, "authority-a");
        Assert.That(CreateService(fixture, baselineProvider, baselineRandom).TryCompute(fixture.Context, 1L, out BattleResolutionComputation baseline, out _), Is.True);

        TestCapabilityProvider ruleProvider = CreateCapabilities();
        ruleProvider.RuleKey = "other-capability-rule:v1";
        Assert.That(CreateService(fixture, ruleProvider, baselineRandom).TryCompute(fixture.Context, 1L, out BattleResolutionComputation otherRule, out _), Is.True);
        Assert.That(otherRule.CausalFingerprint, Is.Not.EqualTo(baseline.CausalFingerprint));

        DeterministicBattleConflictRandomSource otherAuthority = new DeterministicBattleConflictRandomSource(5, "authority-b");
        Assert.That(CreateService(fixture, CreateCapabilities(), otherAuthority).TryCompute(fixture.Context, 1L, out BattleResolutionComputation otherRandom, out _), Is.True);
        Assert.That(otherRandom.CausalFingerprint, Is.Not.EqualTo(baseline.CausalFingerprint));

        BattleResolutionComputationService otherSettingsService = new BattleResolutionComputationService(
            fixture.Builder,
            CreateCapabilities(),
            new BattleResolutionResolverSettings(0.3f, 0.01f),
            baselineRandom);
        Assert.That(otherSettingsService.TryCompute(fixture.Context, 1L, out BattleResolutionComputation otherSettings, out _), Is.True);
        Assert.That(otherSettings.CausalFingerprint, Is.Not.EqualTo(baseline.CausalFingerprint));

        TestCapabilityProvider otherValuesProvider = CreateCapabilities();
        otherValuesProvider.Values["contingent-a-1"] = 2.75f;
        Assert.That(CreateService(fixture, otherValuesProvider, baselineRandom).TryCompute(fixture.Context, 1L, out BattleResolutionComputation otherValue, out _), Is.True);
        Assert.That(otherValue.CausalFingerprint, Is.Not.EqualTo(baseline.CausalFingerprint));

        Assert.That(fixture.Forces.TryReplaceContingent(new ContingentRecord(
            new ContingentId("contingent-a-1"),
            new ArmedForceId("force-a"),
            3000000001L,
            new ContingentOriginReference("source", "origin-a1"),
            "service-a",
            new[] { new ArmedForceCharacteristic("alpha", "first"), new ArmedForceCharacteristic("zeta", "last") }), out _), Is.True);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, out BattleExecutionContext changedAmountContext, out _), Is.True);
        Assert.That(CreateService(fixture, CreateCapabilities(), baselineRandom).TryCompute(changedAmountContext, 1L, out BattleResolutionComputation changedAmount, out _), Is.True);
        Assert.That(changedAmount.CausalFingerprint, Is.Not.EqualTo(baseline.CausalFingerprint));

        Assert.That(fixture.Forces.TryReplaceContingent(new ContingentRecord(
            new ContingentId("contingent-a-1"),
            new ArmedForceId("force-a"),
            3000000001L,
            new ContingentOriginReference("source", "origin-a1"),
            "service-a",
            new[] { new ArmedForceCharacteristic("alpha", "changed"), new ArmedForceCharacteristic("zeta", "last") }), out _), Is.True);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, out BattleExecutionContext changedCharacteristicContext, out _), Is.True);
        Assert.That(CreateService(fixture, CreateCapabilities(), baselineRandom).TryCompute(changedCharacteristicContext, 1L, out BattleResolutionComputation changedCharacteristic, out _), Is.True);
        Assert.That(changedCharacteristic.CausalFingerprint, Is.Not.EqualTo(changedAmount.CausalFingerprint));
        Assert.That(baseline.CausalFingerprint, Does.StartWith("battle-causal:sha256-v1:"));
        Assert.That(baseline.CausalFingerprintCanonicalInputs, Does.Contain("3000000000"));
    }

    [Test]
    public void UnrelatedBattleEvaluationOrderDoesNotAdvanceSharedRandomSequence()
    {
        BattleResolutionFixture battleA = CreateFixture(battleIdValue: "battle-a");
        BattleResolutionFixture battleB = CreateFixture(battleIdValue: "battle-b");
        DeterministicBattleConflictRandomSource sharedFirst = new DeterministicBattleConflictRandomSource(1234, "same-authority");
        BattleResolutionComputationService serviceAFirst = CreateService(battleA, CreateCapabilities(), sharedFirst);
        BattleResolutionComputationService serviceBFirst = CreateService(battleB, CreateCapabilities(), sharedFirst);

        Assert.That(serviceAFirst.TryCompute(battleA.Context, 1L, out BattleResolutionComputation a1, out _), Is.True);
        Assert.That(serviceBFirst.TryCompute(battleB.Context, 1L, out BattleResolutionComputation b1, out _), Is.True);
        Assert.That(serviceAFirst.TryCompute(battleA.Context, 1L, out BattleResolutionComputation aAfterB, out _), Is.True);
        AssertRawResultsEqual(a1.RawResult, aAfterB.RawResult);

        BattleResolutionFixture reverseA = CreateFixture(battleIdValue: "battle-a");
        BattleResolutionFixture reverseB = CreateFixture(battleIdValue: "battle-b");
        DeterministicBattleConflictRandomSource sharedSecond = new DeterministicBattleConflictRandomSource(1234, "same-authority");
        BattleResolutionComputationService serviceASecond = CreateService(reverseA, CreateCapabilities(), sharedSecond);
        BattleResolutionComputationService serviceBSecond = CreateService(reverseB, CreateCapabilities(), sharedSecond);
        Assert.That(serviceBSecond.TryCompute(reverseB.Context, 1L, out BattleResolutionComputation b2, out _), Is.True);
        Assert.That(serviceASecond.TryCompute(reverseA.Context, 1L, out BattleResolutionComputation a2, out _), Is.True);
        Assert.That(serviceBSecond.TryCompute(reverseB.Context, 1L, out BattleResolutionComputation bAfterA, out _), Is.True);
        AssertRawResultsEqual(a1.RawResult, a2.RawResult);
        AssertRawResultsEqual(b1.RawResult, b2.RawResult);
        AssertRawResultsEqual(b2.RawResult, bAfterA.RawResult);
    }

    private static BattleResolutionParticipantMapping FindParticipant(
        BattleResolutionComputation computation,
        string contingentId)
    {
        foreach (BattleResolutionParticipantMapping mapping in computation.ParticipantMappings)
        {
            if (mapping.ContingentId.Value == contingentId) return mapping;
        }

        Assert.Fail("Missing participant mapping for " + contingentId);
        return null;
    }

    private static void AssertRawResultsEqual(
        ConflictResolutionResult expected,
        ConflictResolutionResult actual)
    {
        Assert.That(actual.Outcome, Is.EqualTo(expected.Outcome));
        Assert.That(actual.WinningSideId, Is.EqualTo(expected.WinningSideId));
        Assert.That(actual.SideResults, Has.Count.EqualTo(expected.SideResults.Count));
        for (int index = 0; index < expected.SideResults.Count; index++)
        {
            Assert.That(actual.SideResults[index].SideId, Is.EqualTo(expected.SideResults[index].SideId));
            Assert.That(actual.SideResults[index].RawCapability, Is.EqualTo(expected.SideResults[index].RawCapability));
            Assert.That(actual.SideResults[index].RandomFactor, Is.EqualTo(expected.SideResults[index].RandomFactor));
            Assert.That(actual.SideResults[index].FinalScore, Is.EqualTo(expected.SideResults[index].FinalScore));
            Assert.That(actual.SideResults[index].Disposition, Is.EqualTo(expected.SideResults[index].Disposition));
            Assert.That(actual.SideResults[index].ParticipantContributions, Has.Count.EqualTo(expected.SideResults[index].ParticipantContributions.Count));
            for (int participantIndex = 0; participantIndex < expected.SideResults[index].ParticipantContributions.Count; participantIndex++)
            {
                ConflictParticipantCapabilityResult expectedParticipant = expected.SideResults[index].ParticipantContributions[participantIndex];
                ConflictParticipantCapabilityResult actualParticipant = actual.SideResults[index].ParticipantContributions[participantIndex];
                Assert.That(actualParticipant.ParticipantId, Is.EqualTo(expectedParticipant.ParticipantId));
                Assert.That(actualParticipant.SourceId, Is.EqualTo(expectedParticipant.SourceId));
                Assert.That(actualParticipant.Kind, Is.EqualTo(expectedParticipant.Kind));
                Assert.That(actualParticipant.RawCapability, Is.EqualTo(expectedParticipant.RawCapability));
                Assert.That(actualParticipant.EffectiveCapability, Is.EqualTo(expectedParticipant.EffectiveCapability));
            }
        }
    }

    private static ConflictParticipantCapabilityResult FindLowerParticipant(
        BattleResolutionComputation computation,
        string lowerParticipantId)
    {
        foreach (ConflictSideResolutionResult side in computation.RawResult.SideResults)
        {
            foreach (ConflictParticipantCapabilityResult participant in side.ParticipantContributions)
            {
                if (participant.ParticipantId == lowerParticipantId) return participant;
            }
        }

        Assert.Fail("Missing lower-level participant " + lowerParticipantId);
        return null;
    }

    private static BattleExecutionForceContext FindForce(BattleExecutionSideContext side, string forceId)
    {
        foreach (BattleExecutionForceContext force in side.Forces)
        {
            if (force.ForceId.Value == forceId) return force;
        }

        Assert.Fail("Missing force " + forceId);
        return null;
    }

    private static string BuildRawResultStableKey(ConflictResolutionResult result)
    {
        List<string> values = new List<string>
        {
            result.ConflictId,
            result.WinningSideId ?? string.Empty,
            result.Outcome.ToString()
        };
        foreach (ConflictSideResolutionResult side in result.SideResults)
        {
            values.Add(side.SideId);
            values.Add(side.RawCapability.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            values.Add(side.RandomFactor.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            values.Add(side.FinalScore.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            values.Add(side.Disposition.ToString());
            foreach (ConflictParticipantCapabilityResult participant in side.ParticipantContributions)
            {
                values.Add(participant.ParticipantId);
                values.Add(participant.SourceId);
                values.Add(participant.Kind.ToString());
                values.Add(participant.RawCapability.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                values.Add(participant.EffectiveCapability.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return string.Join("|", values);
    }

    private static BattleResolutionComputationService CreateService(
        BattleResolutionFixture fixture,
        TestCapabilityProvider capabilities,
        IBattleContextualConflictRandomSource random,
        BattleResolutionResolverSettings settings = null)
    {
        return new BattleResolutionComputationService(
            fixture.Builder,
            capabilities,
            settings ?? ResolverSettings,
            random);
    }

    private static TestCapabilityProvider CreateCapabilities()
    {
        TestCapabilityProvider provider = new TestCapabilityProvider("test-capability:v1|set-a", 1f);
        provider.Values.Add("contingent-a-1", 2.5f);
        provider.Values.Add("contingent-a-2", 0.5f);
        provider.Values.Add("contingent-b-1", 1.25f);
        provider.Values.Add("contingent-c-0", 0f);
        provider.Values.Add("contingent-c-1", 3.5f);
        return provider;
    }

    private static BattleResolutionFixture CreateFixture(
        bool reverseInsertion = false,
        string battleIdValue = "battle-projection",
        bool hierarchicalSideA = false)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        BattleId battleId = new BattleId(battleIdValue);
        ArmedForceId forceA = new ArmedForceId("force-a");
        ArmedForceId forceB = new ArmedForceId("force-b");
        ArmedForceId forceC = new ArmedForceId("force-c");
        ArmedForceId parentForceId = new ArmedForceId("force-parent");
        ArmedForceId grandchildForceId = new ArmedForceId("force-grandchild");
        List<ArmedForceRecord> forceRecords;
        if (hierarchicalSideA)
        {
            forceRecords = new List<ArmedForceRecord>
            {
                new ArmedForceRecord(parentForceId, "Parent", 0L),
                new ArmedForceRecord(forceA, "Force A", 0L, parentForceId),
                new ArmedForceRecord(grandchildForceId, "Grandchild", 0L, forceA),
                new ArmedForceRecord(forceB, "Force B", 0L),
                new ArmedForceRecord(forceC, "Force C", 0L)
            };
            if (reverseInsertion)
            {
                forceRecords = new List<ArmedForceRecord>
                {
                    new ArmedForceRecord(forceC, "Force C", 0L),
                    new ArmedForceRecord(forceB, "Force B", 0L),
                    new ArmedForceRecord(parentForceId, "Parent", 0L),
                    new ArmedForceRecord(forceA, "Force A", 0L, parentForceId),
                    new ArmedForceRecord(grandchildForceId, "Grandchild", 0L, forceA)
                };
            }
        }
        else
        {
            forceRecords = new List<ArmedForceRecord>
            {
                new ArmedForceRecord(forceA, "Force A", 0L),
                new ArmedForceRecord(forceB, "Force B", 0L),
                new ArmedForceRecord(forceC, "Force C", 0L)
            };
            if (reverseInsertion) forceRecords.Reverse();
        }
        foreach (ArmedForceRecord force in forceRecords)
        {
            Assert.That(forces.TryRegister(force, out _), Is.True);
        }

        List<ContingentRecord> contingents = new List<ContingentRecord>
        {
            new ContingentRecord(
                new ContingentId("contingent-a-1"), forceA, 3000000000L,
                new ContingentOriginReference("source", "origin-a1"), "service-a",
                new[] { new ArmedForceCharacteristic("zeta", "last"), new ArmedForceCharacteristic("alpha", "first") }),
            new ContingentRecord(
                new ContingentId("contingent-a-2"), forceA, 10L,
                new ContingentOriginReference("source", "origin-a2"), "service-b"),
            new ContingentRecord(
                new ContingentId("contingent-b-1"), forceB, 6L,
                new ContingentOriginReference("source", "origin-b1"), "service-c"),
            new ContingentRecord(
                new ContingentId("contingent-c-1"), forceC, 7L,
                new ContingentOriginReference("source", "origin-c1"), "service-d"),
            new ContingentRecord(
                new ContingentId("contingent-c-0"), forceC, 0L,
                new ContingentOriginReference("source", "origin-c0"), "service-c")
        };
        if (hierarchicalSideA)
        {
            contingents.Add(new ContingentRecord(
                new ContingentId("contingent-grandchild"),
                grandchildForceId,
                500L,
                new ContingentOriginReference("source", "origin-grandchild"),
                "service-grandchild"));
        }
        if (reverseInsertion) contingents.Reverse();
        foreach (ContingentRecord contingent in contingents)
        {
            Assert.That(forces.TryRegisterContingent(contingent, out _), Is.True);
        }

        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-main")), out _), Is.True);
        ArmedForceSpatialStateStore spatial = new ArmedForceSpatialStateStore(forces, authority);
        List<ArmedForceId> positionedForceIds = new List<ArmedForceId> { forceA, forceB, forceC };
        if (hierarchicalSideA) positionedForceIds.Add(parentForceId);
        foreach (ArmedForceId forceId in positionedForceIds)
        {
            Assert.That(spatial.TrySetPosition(forceId, SpatialReference.ForHex(new HexId("hex-main")), out _), Is.True);
        }

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleSideId sideA = new BattleSideId("side-a");
        BattleSideId sideB = new BattleSideId("side-b");
        BattleSideId sideC = new BattleSideId("side-c");
        List<BattleStateSide> sides = new List<BattleStateSide>
        {
            new BattleStateSide(battleId, sideA, "Side A"),
            new BattleStateSide(battleId, sideB, "Side B"),
            new BattleStateSide(battleId, sideC, "Side C")
        };
        List<BattleParticipantBinding> bindings = new List<BattleParticipantBinding>
        {
            new BattleParticipantBinding(new BattleParticipantBindingId("binding-a"), battleId, sideA, forceA),
            new BattleParticipantBinding(new BattleParticipantBindingId("binding-b"), battleId, sideB, forceB),
            new BattleParticipantBinding(new BattleParticipantBindingId("binding-c"), battleId, sideC, forceC)
        };
        if (hierarchicalSideA)
        {
            bindings.Add(new BattleParticipantBinding(
                new BattleParticipantBindingId("binding-parent"),
                battleId,
                sideA,
                parentForceId));
        }
        if (reverseInsertion)
        {
            sides.Reverse();
            bindings.Reverse();
        }

        PersistentBattleRecord battle = new PersistentBattleRecord(
            battleId,
            0L,
            lifecycleState: BattleLifecycleState.Pending,
            sides: sides,
            participantBindings: bindings,
            locationReference: SpatialReference.ForHex(new HexId("hex-main")));
        Assert.That(battles.TryRegister(battle, out _), Is.True);
        Assert.That(battles.TryStart(battleId, 1L, out _), Is.True);
        BattleExecutionContextBuilder builder = new BattleExecutionContextBuilder(battles, forces, spatial, authority, personStore: persons);
        Assert.That(builder.TryCreate(battleId, 1L, out BattleExecutionContext context, out BattleExecutionFailure contextFailure), Is.True, contextFailure.ToString());

        return new BattleResolutionFixture(persons, forces, spatial, battles, builder, battleId, context);
    }

    private sealed class BattleResolutionFixture
    {
        public BattleResolutionFixture(
            PersonStore persons,
            ArmedForceStore forces,
            ArmedForceSpatialStateStore spatial,
            PersistentBattleStore battles,
            BattleExecutionContextBuilder builder,
            BattleId battleId,
            BattleExecutionContext context)
        {
            Persons = persons;
            Forces = forces;
            Spatial = spatial;
            Battles = battles;
            Builder = builder;
            BattleId = battleId;
            Context = context;
        }

        public PersonStore Persons { get; }
        public ArmedForceStore Forces { get; }
        public ArmedForceSpatialStateStore Spatial { get; }
        public PersistentBattleStore Battles { get; }
        public BattleExecutionContextBuilder Builder { get; }
        public BattleId BattleId { get; }
        public BattleExecutionContext Context { get; }
    }

    private sealed class TestCapabilityProvider : IBattleContingentCapabilityProvider
    {
        public TestCapabilityProvider(string ruleKey, float fallbackCapability)
        {
            RuleKey = ruleKey;
            FallbackCapability = fallbackCapability;
        }

        public string RuleKey { get; set; }
        public float FallbackCapability { get; set; }
        public string RejectedContingentId { get; set; }
        public int CallCount { get; private set; }
        public Dictionary<string, float> Values { get; } = new Dictionary<string, float>(StringComparer.Ordinal);
        public List<string> EvaluatedContingentIds { get; } = new List<string>();
        public List<BattleExecutionContingentSnapshot> ObservedContingents { get; } = new List<BattleExecutionContingentSnapshot>();

        public bool TryEvaluate(
            BattleExecutionContingentSnapshot contingent,
            out float capability,
            out string failureReason)
        {
            CallCount++;
            EvaluatedContingentIds.Add(contingent.ContingentId.Value);
            ObservedContingents.Add(contingent);
            if (string.Equals(contingent.ContingentId.Value, RejectedContingentId, StringComparison.Ordinal))
            {
                capability = 0f;
                failureReason = "Rejected by deterministic test rule.";
                return false;
            }

            failureReason = null;
            capability = Values.TryGetValue(contingent.ContingentId.Value, out float value)
                ? value
                : FallbackCapability;
            return true;
        }
    }

    private sealed class CountingBattleRandomSource : IBattleContextualConflictRandomSource
    {
        public string RuleKey => "counting-random:v1";
        public List<string> OperationKeys { get; } = new List<string>();

        public float NextUnit(string operationKey)
        {
            OperationKeys.Add(operationKey);
            return 0.5f;
        }

        public float NextUnit()
        {
            throw new InvalidOperationException("Battle computations must use contextual random keys.");
        }
    }
}
