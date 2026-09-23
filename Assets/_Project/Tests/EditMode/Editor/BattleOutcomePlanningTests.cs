using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class BattleOutcomePlanningTests
{
    private const string ProjectionVersion = "battle-conflict-projection:v1";
    private const string NumericProfile = "unity-float32-current-host:v1";

    [Test]
    public void RuntimeWithoutPolicy_ExplicitlyReportsPlanningUnavailable()
    {
        BattleOutcomeFixture fixture = CreateFixture(null);

        Assert.That(fixture.Runtime.BattleResolutionPolicy, Is.Null);
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.IsConfigured, Is.False);
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(plan, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PolicyNotConfigured));
    }

    [Test]
    public void PolicyFingerprint_IsStableForEquivalentCompositionAndSensitiveToRelevantInputs()
    {
        BattleResolutionPolicy first = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"));
        BattleResolutionPolicy equivalent = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"));
        BattleResolutionPolicy differentCapability = CreatePolicy(
            new TestCapabilityProvider("capability:test:v2", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"));
        BattleResolutionPolicy differentRandom = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(18, "random:test:v1"));
        BattleResolutionPolicy differentSettings = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0.1f, 0.02f));
        BattleResolutionPolicy differentProjection = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            "battle-conflict-projection:v2");
        BattleResolutionPolicy differentNumericProfile = CreatePolicy(
            new TestCapabilityProvider("capability:test:v1", 2f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            ProjectionVersion,
            "fixed-profile:test:v2",
            "fixed-profile:test:v2");

        Assert.That(first.SemanticFingerprint, Is.EqualTo(equivalent.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Is.Not.EqualTo(differentCapability.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Is.Not.EqualTo(differentRandom.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Is.Not.EqualTo(differentSettings.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Is.Not.EqualTo(differentProjection.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Is.Not.EqualTo(differentNumericProfile.SemanticFingerprint));
        Assert.That(first.SemanticFingerprint, Does.StartWith("battle-policy:sha256-v1:"));
    }

    [Test]
    public void PolicyCopiesNumericCompatibilityAndRequiresExplicitProfileIdentity()
    {
        List<string> supportedProfiles = new List<string> { NumericProfile };
        BattleNumericExecutionProfileCompatibility compatibility =
            new BattleNumericExecutionProfileCompatibility(supportedProfiles);
        BattleResolutionPolicy policy = new BattleResolutionPolicy(
            new TestCapabilityProvider("capability:test:v1", 1f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            ProjectionVersion,
            NumericProfile,
            compatibility);
        string fingerprint = policy.SemanticFingerprint;
        supportedProfiles.Clear();

        Assert.That(policy.NumericExecutionProfileSupported, Is.True);
        Assert.That(policy.SemanticFingerprint, Is.EqualTo(fingerprint));
        Assert.Throws<ArgumentException>(() => new BattleResolutionPolicy(
            new TestCapabilityProvider("capability:test:v1", 1f),
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            ProjectionVersion,
            " ",
            compatibility));
    }

    [Test]
    public void UnsupportedNumericProfileFailsBeforeCapabilityOrRandomResolution()
    {
        TestCapabilityProvider capability = new TestCapabilityProvider("capability:test:v1", 1f);
        BattleResolutionPolicy policy = CreatePolicy(
            capability,
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            ProjectionVersion,
            NumericProfile,
            "some-other-explicit-profile:v1");
        BattleOutcomeFixture fixture = CreateFixture(policy);

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out _,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.NumericProfileUnsupported));
        Assert.That(capability.CallCount, Is.Zero);
    }

    [Test]
    public void UnsupportedProjectionVersionFailsBeforeRawComputation()
    {
        TestCapabilityProvider capability = new TestCapabilityProvider("capability:test:v1", 1f);
        BattleOutcomeFixture fixture = CreateFixture(CreatePolicy(
            capability,
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            "battle-conflict-projection:unsupported"));

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out _,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.ProjectionVersionUnsupported));
        Assert.That(capability.CallCount, Is.Zero);
    }

    [Test]
    public void ChangedPolicyDependencyIdentityCannotSilentlyChangeWorldAuthority()
    {
        TestCapabilityProvider capability = new TestCapabilityProvider("capability:captured:v1", 1f);
        BattleResolutionPolicy policy = CreatePolicy(
            capability,
            new DeterministicBattleConflictRandomSource(17, "random:test:v1"));
        BattleOutcomeFixture fixture = CreateFixture(policy);
        string capturedPolicyIdentity = fixture.Runtime.BattleResolutionPolicy.SemanticFingerprint;
        capability.RuleKey = "capability:changed-after-composition:v1";

        Assert.That(fixture.Runtime.BattleResolutionPolicy.SemanticFingerprint, Is.EqualTo(capturedPolicyIdentity));
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out _,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PolicyIdentityMismatch));
    }

    [Test]
    public void PolicyDependencyRuleKeyChangeAfterPlanningInvalidatesTheOldPlan()
    {
        TestCapabilityProvider capability = new TestCapabilityProvider("capability:before:v1", 1f);
        BattleOutcomeFixture fixture = CreateFixture(CreatePolicy(
            capability,
            new DeterministicBattleConflictRandomSource(17, "random:test:v1")));
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out _), Is.True);
        capability.RuleKey = "capability:after:v1";

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            plan,
            out BattleOutcomePlanValidationReport report,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(report.IsInvalid, Is.True);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PolicyIdentityMismatch));
    }

    [Test]
    public void PlanningRecomputesWithWorldPolicyAndRejectsUnauthorizedPreviewFingerprint()
    {
        TestCapabilityProvider authorizedCapability = new TestCapabilityProvider(
            "capability:world-authority:v1",
            1f,
            new Dictionary<string, float>
            {
                { "contingent-a", 10f },
                { "contingent-b", 1f }
            });
        BattleResolutionPolicy policy = CreatePolicy(
            authorizedCapability,
            new DeterministicBattleConflictRandomSource(21, "random:world-authority:v1"),
            new BattleResolutionResolverSettings(0f, 0f));
        BattleOutcomeFixture fixture = CreateFixture(policy);

        Assert.That(fixture.Runtime.BattleExecutionContextBuilder.TryCreate(
            fixture.BattleId,
            fixture.Runtime.CurrentDay,
            out BattleExecutionContext context,
            out BattleExecutionFailure contextFailure), Is.True, contextFailure?.Message);
        TestCapabilityProvider unauthorizedCapability = new TestCapabilityProvider(
            "capability:caller-preview:v9",
            1f,
            new Dictionary<string, float>
            {
                { "contingent-a", 0.1f },
                { "contingent-b", 10f }
            });
        BattleResolutionComputationService unauthorizedService = new BattleResolutionComputationService(
            fixture.Runtime.BattleExecutionContextBuilder,
            unauthorizedCapability,
            new BattleResolutionResolverSettings(0f, 0f),
            new DeterministicBattleConflictRandomSource(999, "random:caller-preview:v9"));
        Assert.That(unauthorizedService.TryCompute(
            context,
            fixture.Runtime.CurrentDay,
            out BattleResolutionComputation unauthorizedPreview,
            out BattleResolutionComputationFailure rawFailure), Is.True, rawFailure?.Message);
        Assert.That(unauthorizedPreview.RawResult.WinningSideId, Is.EqualTo("side-b"));

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan authorizedPlan,
            out BattleOutcomePlanningFailure planningFailure), Is.True, planningFailure?.Message);
        Assert.That(authorizedPlan.Outcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
        Assert.That(authorizedPlan.Outcome.WinningBattleSideId, Is.EqualTo(new BattleSideId("side-a")));
        Assert.That(authorizedPlan.Outcome.Provenance.CapabilityRuleKey, Is.EqualTo("capability:world-authority:v1"));
        Assert.That(authorizedPlan.Outcome.Provenance.RandomAuthorityRuleKey,
            Is.EqualTo("battle-contextual-fnv1a64-v1|seed=21|authority=25:random:world-authority:v1"));
        Assert.That(authorizedPlan.Outcome.Provenance.PolicyFingerprint, Is.EqualTo(policy.SemanticFingerprint));
        Assert.That(authorizedPlan.Outcome.Provenance.NumericExecutionProfileKey, Is.EqualTo(NumericProfile));

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            null,
            unauthorizedPreview.CausalFingerprint,
            out BattleOutcomeApplicationPlan rejectedPlan,
            out BattleOutcomePlanningFailure mismatch), Is.False);
        Assert.That(rejectedPlan, Is.Null);
        Assert.That(mismatch.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.ExpectedFingerprintMismatch));
    }

    [Test]
    public void RawVictoryAndDrawMapToMinimalTypedBattleOutcomes()
    {
        BattleOutcomeFixture victoryFixture = CreateFixture(CreatePolicy(
            new TestCapabilityProvider("capability:victory:v1", 1f, new Dictionary<string, float>
            {
                { "contingent-a", 10f },
                { "contingent-b", 1f }
            }),
            new DeterministicBattleConflictRandomSource(21, "random:victory:v1"),
            new BattleResolutionResolverSettings(0f, 0f)));
        Assert.That(victoryFixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            victoryFixture.BattleId,
            out BattleOutcomeApplicationPlan victory,
            out BattleOutcomePlanningFailure victoryFailure), Is.True, victoryFailure?.Message);
        Assert.That(victory.Outcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
        Assert.That(victory.Outcome.WinningBattleSideId, Is.EqualTo(new BattleSideId("side-a")));
        Assert.That(victory.Outcome.ResolvedAbsoluteDay, Is.EqualTo(1L));

        BattleOutcomeFixture drawFixture = CreateFixture(CreatePolicy(
            new TestCapabilityProvider("capability:draw:v1", 1f),
            new DeterministicBattleConflictRandomSource(21, "random:draw:v1"),
            new BattleResolutionResolverSettings(0f, 0f)));
        Assert.That(drawFixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            drawFixture.BattleId,
            out BattleOutcomeApplicationPlan draw,
            out BattleOutcomePlanningFailure drawFailure), Is.True, drawFailure?.Message);
        Assert.That(draw.Outcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Draw));
        Assert.That(draw.Outcome.WinningBattleSideId, Is.Null);
        Assert.That(typeof(BattleOutcome).GetProperty("RawResult"), Is.Null);
        Assert.That(typeof(BattleOutcome).GetProperty("FinalScore"), Is.Null);
        Assert.That(draw.ResolutionComputation.RawResult, Is.Not.Null,
            "Raw resolution remains a separate diagnostic value on the ephemeral plan.");
    }

    [Test]
    public void ExpectedFingerprintCanBeOmittedOrConfirmedAndMismatchReturnsNoPlan()
    {
        BattleOutcomeFixture fixture = CreateFixture(CreateStandardPolicy());
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan preview,
            out BattleOutcomePlanningFailure previewFailure), Is.True, previewFailure?.Message);

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            null,
            preview.Outcome.Provenance.CausalResolutionFingerprint,
            out BattleOutcomeApplicationPlan confirmed,
            out BattleOutcomePlanningFailure confirmationFailure), Is.True, confirmationFailure?.Message);
        Assert.That(confirmed.Outcome.Provenance.CausalResolutionFingerprint,
            Is.EqualTo(preview.Outcome.Provenance.CausalResolutionFingerprint));

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            null,
            "battle-causal:sha256-v1:not-current",
            out BattleOutcomeApplicationPlan rejected,
            out BattleOutcomePlanningFailure mismatch), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(mismatch.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.ExpectedFingerprintMismatch));

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            null,
            string.Empty,
            out BattleOutcomeApplicationPlan rejectedEmpty,
            out BattleOutcomePlanningFailure emptyMismatch), Is.False);
        Assert.That(rejectedEmpty, Is.Null);
        Assert.That(emptyMismatch.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.ExpectedFingerprintMismatch));
    }

    [Test]
    public void ApplicationPlanIsExplicitlyIncompleteAndPlanningDoesNotMutateWorldTruth()
    {
        BattleOutcomeFixture fixture = CreateFixture(CreateStandardPolicy());
        long battleRevision = fixture.Runtime.BattleStore.Revision;
        long forceRevision = fixture.Runtime.ArmedForceStore.Revision;
        long spatialRevision = fixture.Runtime.ArmedForceSpatialStateStore.Revision;
        long authorityRevision = fixture.Runtime.SpatialAuthorityStore.Revision;
        Assert.That(fixture.Runtime.ArmedForceStore.TryGetContingent(
            new ContingentId("contingent-a"),
            out ContingentRecord beforeA), Is.True);

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out BattleOutcomePlanningFailure failure), Is.True, failure?.Message);

        Assert.That(plan.DirectConsequenceStatus, Is.EqualTo(BattleDirectConsequencePlanStatus.NotProvided));
        Assert.That(plan.HasCompleteDirectConsequencePlan, Is.False);
        Assert.That(plan.IsCommitReady, Is.False);
        Assert.That(fixture.Runtime.BattleStore.Revision, Is.EqualTo(battleRevision));
        Assert.That(fixture.Runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(fixture.Time.AbsoluteDay, Is.EqualTo(1L));
        Assert.That(fixture.Runtime.ArmedForceStore.Revision, Is.EqualTo(forceRevision));
        Assert.That(fixture.Runtime.ArmedForceSpatialStateStore.Revision, Is.EqualTo(spatialRevision));
        Assert.That(fixture.Runtime.SpatialAuthorityStore.Revision, Is.EqualTo(authorityRevision));
        Assert.That(fixture.Runtime.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
        Assert.That(fixture.Runtime.ArmedForceStore.TryGetContingent(
            new ContingentId("contingent-a"),
            out ContingentRecord afterA), Is.True);
        Assert.That(afterA.Amount, Is.EqualTo(beforeA.Amount));
        Assert.That(typeof(PersistentBattleRecord).GetProperty("Outcome"), Is.Null);
        Assert.That(typeof(BattleOutcomePlanningService).GetMethod("Apply"), Is.Null);
    }

    [Test]
    public void CurrentPlanValidationIgnoresUnrelatedForceChanges()
    {
        BattleOutcomeFixture fixture = CreateFixture(CreateStandardPolicy());
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out _), Is.True);
        Assert.That(fixture.Runtime.ArmedForceStore.TryRegister(
            new ArmedForceRecord(new ArmedForceId("unrelated-force-after-plan"), "Unrelated", 0L),
            out _), Is.True);

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            plan,
            out BattleOutcomePlanValidationReport report,
            out BattleOutcomePlanningFailure failure), Is.True, failure?.Message);
        Assert.That(report.IsCurrent, Is.True);
    }

    [Test]
    public void ParticipantPositionAndManpowerChangesMakePlanStale()
    {
        BattleOutcomeFixture positionFixture = CreateFixture(CreateStandardPolicy());
        Assert.That(positionFixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            positionFixture.BattleId,
            out BattleOutcomeApplicationPlan positionPlan,
            out _), Is.True);
        Assert.That(positionFixture.Runtime.ArmedForceSpatialStateStore.TrySetPosition(
            new ArmedForceId("force-a"),
            SpatialReference.ForHex(new HexId("hex-other")),
            out _), Is.True);
        Assert.That(positionFixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            positionPlan,
            out BattleOutcomePlanValidationReport positionReport,
            out BattleOutcomePlanningFailure positionFailure), Is.False);
        Assert.That(positionReport.IsStale, Is.True);
        Assert.That(positionFailure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PlanStale));

        BattleOutcomeFixture contingentFixture = CreateFixture(CreateStandardPolicy());
        Assert.That(contingentFixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            contingentFixture.BattleId,
            out BattleOutcomeApplicationPlan contingentPlan,
            out _), Is.True);
        Assert.That(contingentFixture.Runtime.ContingentManpowerStateStore.TryRedistribute(
            new ContingentId("contingent-a"),
            new[] { new ContingentManpowerCohort(
                ManpowerInjuryState.Wounded,
                ManpowerCustodyState.Free,
                null,
                ManpowerAvailabilityState.Unavailable,
                4L) },
            0L,
            out _), Is.True);
        Assert.That(contingentFixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            contingentPlan,
            out BattleOutcomePlanValidationReport contingentReport,
            out BattleOutcomePlanningFailure contingentFailure), Is.False);
        Assert.That(contingentReport.IsStale, Is.True);
        Assert.That(contingentFailure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PlanStale));
    }

    [Test]
    public void AvailabilityFlowsIntoD4CapabilityAndD5RevalidatesTheCapturedPlan()
    {
        AvailableAmountCapabilityProvider capability = new AvailableAmountCapabilityProvider();
        BattleResolutionPolicy policy = new BattleResolutionPolicy(
            capability,
            new DeterministicBattleConflictRandomSource(21, "random:available:v1"),
            new BattleResolutionResolverSettings(0f, 0f),
            ProjectionVersion,
            NumericProfile,
            new BattleNumericExecutionProfileCompatibility(new[] { NumericProfile }));
        BattleOutcomeFixture fixture = CreateFixture(policy);
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan firstPlan,
            out BattleOutcomePlanningFailure firstFailure), Is.True, firstFailure?.Message);
        string firstCausalFingerprint = firstPlan.ResolutionComputation.CausalFingerprint;
        Assert.That(capability.ObservedAvailableAmounts, Is.EqualTo(new long[] { 4L, 2L }));

        Assert.That(fixture.Runtime.ContingentManpowerStateStore.TryRedistribute(
            new ContingentId("contingent-a"),
            new[]
            {
                new ContingentManpowerCohort(ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
                    null, ManpowerAvailabilityState.Available, 2L),
                new ContingentManpowerCohort(ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
                    null, ManpowerAvailabilityState.Unavailable, 2L)
            },
            0L,
            out _), Is.True);
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            firstPlan,
            out BattleOutcomePlanValidationReport staleReport,
            out BattleOutcomePlanningFailure staleFailure), Is.False);
        Assert.That(staleReport.IsStale, Is.True);
        Assert.That(staleFailure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PlanStale));
        Assert.That(capability.ObservedAvailableAmounts, Is.EqualTo(new long[] { 4L, 2L, 2L, 2L }),
            "D5 revalidates against a newly captured D3 context, and D4 sees its current availability.");

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan currentPlan,
            out BattleOutcomePlanningFailure currentFailure), Is.True, currentFailure?.Message);
        Assert.That(capability.ObservedAvailableAmounts, Is.EqualTo(new long[] { 4L, 2L, 2L, 2L, 2L, 2L }));
        Assert.That(currentPlan.ResolutionComputation.CausalFingerprint, Is.Not.EqualTo(firstCausalFingerprint));
        Assert.That(fixture.Runtime.BattleStore.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
    }

    [Test]
    public void LogicalDayChangeMakesPlanStale()
    {
        BattleOutcomeFixture fixture = CreateFixture(CreateStandardPolicy());
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out _), Is.True);
        fixture.Time.AdvanceDay();

        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryValidateCurrent(
            plan,
            out BattleOutcomePlanValidationReport report,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(report.IsStale, Is.True);
        Assert.That(report.Reasons, Does.Contain(BattleOutcomePlanStalenessReason.CurrentDayChanged));
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PlanStale));
    }

    [Test]
    public void PlanFromDifferentWorldPolicyCannotValidateInThisRuntime()
    {
        BattleOutcomeFixture fixture = CreateFixture(CreateStandardPolicy());
        Assert.That(fixture.Runtime.BattleOutcomePlanningService.TryCreateApplicationPlan(
            fixture.BattleId,
            out BattleOutcomeApplicationPlan plan,
            out _), Is.True);
        SimulationRuntime differentPolicyRuntime = ComposeRuntime(
            fixture.Sources,
            CreatePolicy(
                new TestCapabilityProvider("capability:other-world:v1", 1f),
                new DeterministicBattleConflictRandomSource(41, "random:other-world:v1")),
            new SimulationTime(1L));

        Assert.That(differentPolicyRuntime.BattleOutcomePlanningService.TryValidateCurrent(
            plan,
            out BattleOutcomePlanValidationReport report,
            out BattleOutcomePlanningFailure failure), Is.False);
        Assert.That(report.IsInvalid, Is.True);
        Assert.That(failure.Code, Is.EqualTo(BattleOutcomePlanningFailureCode.PolicyIdentityMismatch));
    }

    private static BattleResolutionPolicy CreateStandardPolicy()
    {
        return CreatePolicy(
            new TestCapabilityProvider("capability:standard:v1", 1f),
            new DeterministicBattleConflictRandomSource(21, "random:standard:v1"),
            new BattleResolutionResolverSettings(0f, 0f));
    }

    private static BattleResolutionPolicy CreatePolicy(
        TestCapabilityProvider capability,
        IBattleContextualConflictRandomSource random,
        BattleResolutionResolverSettings settings = null,
        string projectionVersion = ProjectionVersion,
        string numericProfile = NumericProfile,
        params string[] supportedProfiles)
    {
        string[] effectiveSupportedProfiles = supportedProfiles == null || supportedProfiles.Length == 0
            ? new[] { NumericProfile }
            : supportedProfiles;
        return new BattleResolutionPolicy(
            capability,
            random,
            settings ?? new BattleResolutionResolverSettings(0f, 0f),
            projectionVersion,
            numericProfile,
            new BattleNumericExecutionProfileCompatibility(effectiveSupportedProfiles));
    }

    private static BattleOutcomeFixture CreateFixture(BattleResolutionPolicy policy)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceA = new ArmedForceId("force-a");
        ArmedForceId forceB = new ArmedForceId("force-b");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceA, "A", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceB, "B", 0L), out _), Is.True);
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            new ContingentId("contingent-a"),
            forceA,
            4L,
            new ContingentOriginReference("source", "a"),
            "service-a"), out _), Is.True);
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            new ContingentId("contingent-b"),
            forceB,
            2L,
            new ContingentOriginReference("source", "b"),
            "service-b"), out _), Is.True);

        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-main")), out _), Is.True);
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-other")), out _), Is.True);
        ArmedForceSpatialStateStore spatial = new ArmedForceSpatialStateStore(forces, authority);
        Assert.That(spatial.TrySetPosition(forceA, SpatialReference.ForHex(new HexId("hex-main")), out _), Is.True);
        Assert.That(spatial.TrySetPosition(forceB, SpatialReference.ForHex(new HexId("hex-main")), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId battleId = new BattleId("battle-d5");
        Assert.That(battles.TryRegister(new PersistentBattleRecord(
            battleId,
            0L,
            lifecycleState: BattleLifecycleState.Pending,
            sides: new[]
            {
                new BattleStateSide(battleId, new BattleSideId("side-a"), "A"),
                new BattleStateSide(battleId, new BattleSideId("side-b"), "B")
            },
            participantBindings: new[]
            {
                new BattleParticipantBinding(
                    new BattleParticipantBindingId("binding-a"), battleId, new BattleSideId("side-a"), forceA),
                new BattleParticipantBinding(
                    new BattleParticipantBindingId("binding-b"), battleId, new BattleSideId("side-b"), forceB)
            },
            locationReference: SpatialReference.ForHex(new HexId("hex-main"))), out _), Is.True);
        Assert.That(battles.TryStart(battleId, 1L, out _), Is.True);

        BattleWorldSources sources = new BattleWorldSources(
            persons,
            forces,
            authority,
            spatial,
            battles,
            battleId);
        SimulationTime time = new SimulationTime(1L);
        SimulationRuntime runtime = ComposeRuntime(sources, policy, time);
        return new BattleOutcomeFixture(sources, time, runtime, battleId);
    }

    private static SimulationRuntime ComposeRuntime(
        BattleWorldSources sources,
        BattleResolutionPolicy policy,
        SimulationTime time)
    {
        return new SimulationRuntime(
            time,
            null,
            null,
            economyEnabled: false,
            personStore: sources.Persons,
            armedForceStore: sources.Forces,
            battleStore: sources.Battles,
            spatialAuthorityStore: sources.Authority,
            armedForceSpatialStateStore: sources.Spatial,
            battleResolutionPolicy: policy);
    }

    private sealed class BattleWorldSources
    {
        public BattleWorldSources(
            PersonStore persons,
            ArmedForceStore forces,
            SpatialAuthorityStore authority,
            ArmedForceSpatialStateStore spatial,
            PersistentBattleStore battles,
            BattleId battleId)
        {
            Persons = persons;
            Forces = forces;
            Authority = authority;
            Spatial = spatial;
            Battles = battles;
            BattleId = battleId;
        }

        public PersonStore Persons { get; }
        public ArmedForceStore Forces { get; }
        public SpatialAuthorityStore Authority { get; }
        public ArmedForceSpatialStateStore Spatial { get; }
        public PersistentBattleStore Battles { get; }
        public BattleId BattleId { get; }
    }

    private sealed class BattleOutcomeFixture
    {
        public BattleOutcomeFixture(
            BattleWorldSources sources,
            SimulationTime time,
            SimulationRuntime runtime,
            BattleId battleId)
        {
            Sources = sources;
            Time = time;
            Runtime = runtime;
            BattleId = battleId;
        }

        public BattleWorldSources Sources { get; }
        public SimulationTime Time { get; }
        public SimulationRuntime Runtime { get; }
        public BattleId BattleId { get; }
    }

    private sealed class TestCapabilityProvider : IBattleContingentCapabilityProvider
    {
        private readonly IReadOnlyDictionary<string, float> capabilities;
        private readonly float fallbackCapability;

        public TestCapabilityProvider(
            string ruleKey,
            float fallbackCapability,
            IDictionary<string, float> capabilities = null)
        {
            RuleKey = ruleKey;
            this.fallbackCapability = fallbackCapability;
            this.capabilities = new Dictionary<string, float>(
                capabilities ?? new Dictionary<string, float>(),
                StringComparer.Ordinal);
        }

        public string RuleKey { get; set; }
        public int CallCount { get; private set; }

        public bool TryEvaluate(
            BattleExecutionContingentSnapshot contingent,
            out float capability,
            out string failureReason)
        {
            CallCount++;
            failureReason = null;
            capability = capabilities.TryGetValue(contingent.ContingentId.Value, out float value)
                ? value
                : fallbackCapability;
            return true;
        }
    }

    private sealed class AvailableAmountCapabilityProvider : IBattleContingentCapabilityProvider
    {
        public string RuleKey => "capability:captured-availability:v1";
        public List<long> ObservedAvailableAmounts { get; } = new List<long>();

        public bool TryEvaluate(
            BattleExecutionContingentSnapshot contingent,
            out float capability,
            out string failureReason)
        {
            ObservedAvailableAmounts.Add(contingent.AvailableAmount);
            capability = contingent.AvailableAmount;
            failureReason = null;
            return true;
        }
    }
}
