using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exercises the accepted synthetic P12-G political binding source without
/// adding a Daily-v1 political writer or changing the authored bootstrap.
/// </summary>
public sealed partial class P12GPoliticalBindingRestoreTests
{
    private const string DailyConfigPath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";
    private const string DecisionIdValue = "p12g-fixture-political-decision";
    private const string KnowledgeReferenceA = "p12g-fixture:knowledge-a";
    private const string KnowledgeReferenceB = "p12g-fixture:knowledge-b";
    private static readonly string[] DailyV1TypedParityCoverage =
    {
        "C: WorldId, P9 genesis manifest, P8 geography, SimulationTime, CalendarDefinition values/identity, RuntimeIdentityRegistry values, RuntimeIdAllocator census identity/counters, SimulationRecordSequence census identity/cursor, DeterministicRandom provider/algorithm identity and seed.",
        "D: full typed City/Market/population/receipt rows, NPC D/F projections and nested evidence/provenance, PersonStore membership/bindings, Genealogy parentage, legacy SpatialNetwork routes, and ExplorableSite identities/references.",
        "E: full typed Institution/Office, Property/transfer/Estate, Faction/affiliation, PoliticalClaim/recognition/history, PoliticalSupport, PoliticalDecision, Military, Conflict/War/Battle, Justice, and Crime/Social rows, including nested references and provenance.",
        "F: full typed PoliticalKnowledge holder/observation/provenance values, ScheduledDirective copied rows and lifecycle values, ActorChoice input/disposition values, TravelParty membership/cost/reference values, and Expedition state/objective/reference values.",
        "Every typed snapshot comparison includes its schema, capture day where present, cardinality/count, and owner-local revision; all fixture-populated values are compared at restore and each later successful boundary. No included row is omitted from a populated family.",
        "Noncapturable included rows: none. The ScheduledDirective detached wrapper exposes only count/revision, so every row is compared through its supported detached owner read (TryCaptureP12FState); DeterministicRandomSource is stateless, so provider/algorithm identity and seed exhaust its persisted root facts."
    };

    private readonly List<GameObject> bootstrapObjects = new List<GameObject>();

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject bootstrapObject in bootstrapObjects)
            if (bootstrapObject != null)
                UnityEngine.Object.DestroyImmediate(bootstrapObject);
        bootstrapObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void PopulatedPoliticalOwnersRoundTripFromRealAdvanceAndContinueWithExactParity()
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G political anchor");
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G political source");
        SyntheticSession control = CreateSyntheticSession(anchor, "P12-G political control");

        Assert.That(source.Session.AdmissionContext.IsRestoredContinuation, Is.True);
        Assert.That(control.Session.AdmissionContext.IsRestoredContinuation, Is.True);
        Assert.That(source.Session.Runtime.PoliticalWorldRevision, Is.EqualTo(1L));
        Assert.That(control.Session.Runtime.PoliticalWorldRevision, Is.EqualTo(1L));
        Assert.That(source.Session.Runtime.TryGetCompletedDailyCaptureToken(out _, out _), Is.False,
            "A private synthetic baseline has no completed token before its own positive advance.");

        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
            Is.True, sourceAdvance.ToString());
        Assert.That(control.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
            Is.True, controlAdvance.ToString());
        DailyCaptureEligibilityToken sourceToken = GetCompletedToken(source.Session.Runtime);
        DailyCaptureEligibilityToken controlToken = GetCompletedToken(control.Session.Runtime);
        Assert.That(source.Session.Runtime.NpcRuntimes.Any(npc =>
                npc?.SelectedDailyV1StatusOwner?.Statuses.Count > 0), Is.True,
            "The accepted selected-profile fixture must exercise populated typed NPC status state.");
        AssertIndependentOwners(source.Session, sourceToken, control.Session, controlToken);
        Assert.That(sourceToken.BoundaryProvenance, Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
        Assert.That(sourceToken.CompletedCoreSequence, Is.GreaterThan(0L));
        Assert.That(sourceToken, Is.Not.SameAs(controlToken));
        AssertDailyV1Parity(source.Session, sourceToken, control.Session, controlToken,
            "independent fixture source/control before restore");
        AssertOwnerVectorFactsEqual(sourceToken.OwnerSections, controlToken.OwnerSections);
        AssertPoliticalOwnersAreAuthenticAndPopulated(source.Session, sourceToken);

        DailyV1ExecutionBoundaryState sourceExecutionState = source.Session.Runtime.ExecutionBoundaryState;
        Assert.That(sourceExecutionState.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        try
        {
            Assert.That(source.Session.Runtime.TryGetCompletedDailyCaptureToken(out _,
                out DailyCaptureEligibilityFailure publicQueryFailure), Is.False,
                "Public completed-token queries remain strict while a save operation is active.");
            Assert.That(publicQueryFailure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
            Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken,
                out DailyCaptureEligibilityFailure publicValidationFailure), Is.False,
                "Public token validation cannot borrow the save-only capture allowance.");
            Assert.That(publicValidationFailure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
            Assert.That(source.Session.Runtime.TryGetCompletedDailyCaptureTokenForContinuationSave(
                out DailyCaptureEligibilityToken saveToken, out DailyCaptureEligibilityFailure saveFailure), Is.True,
                saveFailure.ToString());
            Assert.That(saveToken, Is.SameAs(sourceToken));
            Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureTokenForContinuationSave(
                sourceToken, out saveFailure), Is.True, saveFailure.ToString());
        }
        finally
        {
            Assert.That(sourceExecutionState.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        }

        List<P12GDailyV1RestoreStage> stages = new List<P12GDailyV1RestoreStage>();
        using (SimulationRuntime.DailyGameplayExecutionProbeForTest gameplayProbe =
               SimulationRuntime.BeginDailyGameplayExecutionProbeForTest())
        {
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    source.Session,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stages.Add,
                    restoredAdmissionContextFactoryForTest: _ =>
                        source.NormalBootstrapContext.CreateRestoredContinuationContext()),
                Is.True, failure + ": " + diagnostic);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored, Is.Not.SameAs(source.Session));
            Assert.That(restored.AdmissionContext, Is.Not.SameAs(source.Session.AdmissionContext));
            Assert.That(restored.AdmissionContext.IsRestoredContinuation, Is.True);
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.FOwnerSnapshotsCaptured));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.EPoliticalDecisionsOwnerStaged));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.FPoliticalKnowledgeStaged));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.TargetPoliticalKnowledgeBindingsValidated));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.BoundaryAdmitted));
            Assert.That(gameplayProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero,
                "Restore must not invoke the daily gameplay pipeline.");

            DailyCaptureEligibilityToken restoredToken = GetCompletedToken(restored.Runtime);
            Assert.That(restoredToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
            Assert.That(restoredToken, Is.Not.SameAs(sourceToken));
            Assert.That(restoredToken.WorldId.Value, Is.EqualTo(sourceToken.WorldId.Value));
            Assert.That(restoredToken.AbsoluteDay, Is.EqualTo(sourceToken.AbsoluteDay));
            Assert.That(restoredToken.CompletedCoreSequence, Is.EqualTo(sourceToken.CompletedCoreSequence));
            AssertDailyV1Parity(source.Session, sourceToken, restored, restoredToken,
                "source/target immediately after staging the exact E/F binding");
            AssertDailyV1Parity(restored, restoredToken, control.Session, controlToken,
                "target/control immediately after the exact staged E/F binding resolves");
            AssertOwnerVectorFactsEqual(restoredToken.OwnerSections, controlToken.OwnerSections);
            AssertPoliticalOwnersAreAuthenticAndPopulated(restored, restoredToken);
            AssertIndependentOwners(source.Session, sourceToken, restored, restoredToken);
            AssertIndependentOwners(restored, restoredToken, control.Session, controlToken);

            Assert.That(sourceExecutionState.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
            bool captureSucceeded;
            P12AContinuationImage capturedImage;
            SimulationActiveSession capturedImageCandidate;
            P12GDailyV1RestoreFailure captureFailure;
            string captureDiagnostic;
            try
            {
                captureSucceeded = P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                    source.Session,
                    source.NormalBootstrapContext,
                    out capturedImage,
                    out capturedImageCandidate,
                    out captureFailure,
                    out captureDiagnostic);
            }
            finally
            {
                Assert.That(sourceExecutionState.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
            }
            Assert.That(captureSucceeded, Is.True, captureFailure + ": " + captureDiagnostic);
            Assert.That(capturedImageCandidate, Is.Not.Null);
            Assert.That(capturedImage, Is.Not.Null);
            Assert.That(capturedImage.Header.Day, Is.EqualTo(sourceToken.AbsoluteDay));
            Assert.That(capturedImage.Header.Sequence, Is.EqualTo(sourceToken.CompletedCoreSequence));
            Assert.That(P12ADailyV1Census.TryCreateForImage(
                    capturedImage,
                    out P12ADailyV1Census imageCensus,
                    out P12AResult censusResult),
                Is.True, censusResult?.Code.ToString());
            Assert.That(P12AContinuationCodec.TryEncode(
                    capturedImage,
                    imageCensus.Manifest,
                    out byte[] encodedImage,
                    out P12AResult encodeResult),
                Is.True, encodeResult?.Code.ToString());
            Assert.That(P12AContinuationCodec.TryDecode(
                    encodedImage,
                    imageCensus.Manifest,
                    capturedImage.Header.Compatibility,
                    out P12AContinuationImage decodedImage,
                    out P12AResult decodeResult),
                Is.True, decodeResult?.Code.ToString());

            DailyV1HostEnvironmentIdentity hostIdentity = new DailyV1HostEnvironmentIdentity(1,
                ".NET 8.0", "8.0.1.0", "Windows", "win-x64", "x64", "net8.0",
                new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                    "4b37a0ac-246b-47c5-9bbc-fd79704e0666") }, "numeric", 1);
            Assert.That(sourceExecutionState.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
            bool dotNetCaptureSucceeded;
            P12AContinuationImage dotNetImage;
            SimulationActiveSession dotNetCaptureCandidate;
            P12GDailyV1RestoreFailure dotNetCaptureFailure;
            string dotNetCaptureDiagnostic;
            try
            {
                dotNetCaptureSucceeded = P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureDotNetV2Image(
                    source.Session, source.NormalBootstrapContext, hostIdentity,
                    out dotNetImage,
                    out dotNetCaptureCandidate,
                    out dotNetCaptureFailure,
                    out dotNetCaptureDiagnostic);
            }
            finally
            {
                Assert.That(sourceExecutionState.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
            }
            Assert.That(dotNetCaptureSucceeded, Is.True,
                dotNetCaptureFailure + ": " + dotNetCaptureDiagnostic);
            Assert.That(dotNetCaptureCandidate, Is.Not.Null);
            Assert.That(dotNetImage.Header.CompatibilityV2, Is.Not.Null);
            Assert.That(P12AContinuationCodec.TryEncode(dotNetImage, imageCensus.Manifest,
                out byte[] dotNetBytes, out P12AResult dotNetEncodeResult), Is.True,
                dotNetEncodeResult?.Code.ToString());
            Assert.That(P12AContinuationCodec.TryDecodeDotNetV2(dotNetBytes, imageCensus.Manifest,
                dotNetImage.Header.CompatibilityV2, out P12AContinuationImage dotNetDecoded,
                out P12AResult dotNetDecodeResult), Is.True, dotNetDecodeResult?.Code.ToString());
            List<P12GDailyV1RestoreStage> dotNetRestoreStages = new List<P12GDailyV1RestoreStage>();
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromDotNetV2Image(
                    dotNetDecoded, dotNetImage.Header.CompatibilityV2, anchor.Configuration, anchor.Runtime.Configuration,
                    anchor.Composition.Calendar, anchor.Composition.SelectedDefinitions,
                    source.NormalBootstrapContext, out SimulationActiveSession dotNetRestored,
                    out P12GDailyV1RestoreFailure dotNetRestoreFailure,
                    out string dotNetRestoreDiagnostic, dotNetRestoreStages.Add),
                Is.True, dotNetRestoreFailure + ": " + dotNetRestoreDiagnostic);
            Assert.That(dotNetRestored, Is.Not.Null);
            Assert.That(dotNetRestoreStages, Does.Contain(P12GDailyV1RestoreStage.BoundaryAdmitted));
            Assert.That(EncodeCompletedImage(dotNetRestored, source.NormalBootstrapContext), Is.Not.Empty);

            List<P12GDailyV1RestoreStage> imageRestoreStages = new List<P12GDailyV1RestoreStage>();
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                    decodedImage,
                    capturedImage.Header.Compatibility,
                    anchor.Configuration,
                    anchor.Runtime.Configuration,
                    anchor.Composition.Calendar,
                    anchor.Composition.SelectedDefinitions,
                    source.NormalBootstrapContext,
                    out SimulationActiveSession imageRestored,
                    out P12GDailyV1RestoreFailure imageRestoreFailure,
                    out string imageRestoreDiagnostic,
                    imageRestoreStages.Add),
                Is.True, imageRestoreFailure + ": " + imageRestoreDiagnostic);
            Assert.That(imageRestored, Is.Not.Null);
            Assert.That(imageRestoreStages, Does.Contain(P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured));
            Assert.That(imageRestoreStages, Does.Contain(P12GDailyV1RestoreStage.BoundaryAdmitted));
            DailyCaptureEligibilityToken imageRestoredToken = GetCompletedToken(imageRestored.Runtime);
            Assert.That(imageRestoredToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
            AssertDailyV1Parity(source.Session, sourceToken, imageRestored, imageRestoredToken,
                "persisted-image source/target after real capture, encode/decode, restore, and target recapture");
            AssertDailyV1Parity(restored, restoredToken, imageRestored, imageRestoredToken,
                "in-memory and persisted-image restore parity");
            AssertDailyV1Parity(imageRestored, imageRestoredToken, control.Session, controlToken,
                "persisted-image target/control before continuation");
            AssertOwnerVectorFactsEqual(imageRestoredToken.OwnerSections, controlToken.OwnerSections);
            AssertPoliticalOwnersAreAuthenticAndPopulated(imageRestored, imageRestoredToken);
            AssertIndependentOwners(source.Session, sourceToken, imageRestored, imageRestoredToken);
            AssertIndependentOwners(imageRestored, imageRestoredToken, control.Session, controlToken);
            Assert.That(gameplayProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero,
                "Persisted-image capture and restore must not invoke the daily gameplay pipeline.");

            for (int boundary = 0; boundary < 2; boundary++)
            {
                Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextSourceAdvance),
                    Is.True, nextSourceAdvance.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredAdvance),
                    Is.True, restoredAdvance.ToString());
                Assert.That(imageRestored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure imageRestoredAdvance),
                    Is.True, imageRestoredAdvance.ToString());
                Assert.That(control.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextControlAdvance),
                    Is.True, nextControlAdvance.ToString());
                DailyCaptureEligibilityToken nextSourceToken = GetCompletedToken(source.Session.Runtime);
                DailyCaptureEligibilityToken nextRestoredToken = GetCompletedToken(restored.Runtime);
                DailyCaptureEligibilityToken nextImageRestoredToken = GetCompletedToken(imageRestored.Runtime);
                DailyCaptureEligibilityToken nextControlToken = GetCompletedToken(control.Session.Runtime);
                Assert.That(nextSourceToken.BoundaryProvenance,
                    Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
                Assert.That(nextRestoredToken.BoundaryProvenance,
                    Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
                AssertDailyV1Parity(source.Session, nextSourceToken, restored, nextRestoredToken,
                    "continued source/target boundary " + boundary);
                AssertDailyV1Parity(source.Session, nextSourceToken, imageRestored, nextImageRestoredToken,
                    "continued persisted-image source/target boundary " + boundary);
                AssertDailyV1Parity(source.Session, nextSourceToken, control.Session, nextControlToken,
                    "continued source/control boundary " + boundary);
                AssertDailyV1Parity(restored, nextRestoredToken, control.Session, nextControlToken,
                    "continued target/control boundary " + boundary);
                AssertDailyV1Parity(imageRestored, nextImageRestoredToken, control.Session, nextControlToken,
                    "continued persisted-image target/control boundary " + boundary);
                AssertOwnerVectorFactsEqual(nextRestoredToken.OwnerSections, nextControlToken.OwnerSections);
                AssertOwnerVectorFactsEqual(nextImageRestoredToken.OwnerSections, nextControlToken.OwnerSections);
            }
        }
    }

    [TestCase(PrivateTargetCorruption.UnboundDecider)]
    [TestCase(PrivateTargetCorruption.PoliticalWorldRevision)]
    [TestCase(PrivateTargetCorruption.KnowledgeReferenceMismatch)]
    public void PoliticalBindingRejectionsReachTheNamedValidatorAndPreserveTheSource(
        PrivateTargetCorruption corruption)
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G rejection anchor " + corruption);
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G political rejection " + corruption);
        SyntheticSession control = CreateSyntheticSession(anchor, "P12-G political rejection control " + corruption);
        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
            Is.True, sourceAdvance.ToString());
        Assert.That(control.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
            Is.True, controlAdvance.ToString());
        DailyCaptureEligibilityToken sourceToken = GetCompletedToken(source.Session.Runtime);
        DailyCaptureEligibilityToken controlToken = GetCompletedToken(control.Session.Runtime);
        string sourceSignature = CaptureTypedParitySignature(source.Session, sourceToken);
        SimulationActiveSession sourceIdentity = source.Session;
        List<P12GDailyV1RestoreStage> stages = new List<P12GDailyV1RestoreStage>();
        bool corruptionApplied = false;

        using (SimulationRuntime.DailyGameplayExecutionProbeForTest gameplayProbe =
               SimulationRuntime.BeginDailyGameplayExecutionProbeForTest())
        {
            bool restored = P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                source.Session,
                out SimulationActiveSession rejected,
                out P12GDailyV1RestoreFailure failure,
                out string diagnostic,
                stages.Add,
                candidate =>
                {
                    corruptionApplied = true;
                    ApplyPrivateTargetCorruption(candidate, corruption, source.DecisionId, source.Holder.PersonId);
                },
                restoredAdmissionContextFactoryForTest: _ =>
                    source.NormalBootstrapContext.CreateRestoredContinuationContext());

            Assert.That(restored, Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(corruptionApplied, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed));
            Assert.That(diagnostic, Does.Contain("staged PoliticalDecision"));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.TargetOwnerVectorValidated));
            Assert.That(stages, Does.Contain(P12GDailyV1RestoreStage.TargetSentinelsValidated));
            Assert.That(stages.Contains(P12GDailyV1RestoreStage.TargetPoliticalKnowledgeBindingsValidated), Is.False);
            Assert.That(stages.Contains(P12GDailyV1RestoreStage.BoundaryAdmitted), Is.False);
            Assert.That(gameplayProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero);
        }

        Assert.That(source.Session, Is.SameAs(sourceIdentity));
        Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
        Assert.That(CaptureTypedParitySignature(source.Session, sourceToken), Is.EqualTo(sourceSignature));
        AssertDailyV1Parity(source.Session, sourceToken, control.Session, controlToken,
            "source/control after private-target rejection");

        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                source.Session,
                out SimulationActiveSession retry,
                out P12GDailyV1RestoreFailure retryFailure,
                out string retryDiagnostic,
                restoredAdmissionContextFactoryForTest: _ =>
                    source.NormalBootstrapContext.CreateRestoredContinuationContext()),
            Is.True, retryFailure + ": " + retryDiagnostic);
        Assert.That(retry, Is.Not.Null);
        DailyCaptureEligibilityToken retryToken = GetCompletedToken(retry.Runtime);
        AssertDailyV1Parity(retry, retryToken, control.Session, controlToken,
            "successful retry/control after private-target rejection");
    }

    [TestCase(AdmissionContextCorruption.Null)]
    [TestCase(AdmissionContextCorruption.SourceContext)]
    [TestCase(AdmissionContextCorruption.NormalAnchorContext)]
    [TestCase(AdmissionContextCorruption.WrongOwnerThread)]
    [TestCase(AdmissionContextCorruption.UnsupportedProfile)]
    public void RestoredContextFactoryRejectsInvalidPrivateResults(AdmissionContextCorruption corruption)
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G context anchor " + corruption);
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G context rejection " + corruption);
        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advance),
            Is.True, advance.ToString());
        DailyCaptureEligibilityToken token = GetCompletedToken(source.Session.Runtime);
        List<P12GDailyV1RestoreStage> stages = new List<P12GDailyV1RestoreStage>();

        bool restored = P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
            source.Session,
            out SimulationActiveSession candidate,
            out P12GDailyV1RestoreFailure failure,
            out string diagnostic,
            stages.Add,
            restoredAdmissionContextFactoryForTest: sourceContext =>
                CreateInvalidContext(source, sourceContext, corruption));

        Assert.That(restored, Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetCompositionFailed));
        Assert.That(diagnostic, Is.Not.Empty);
        Assert.That(stages.Contains(P12GDailyV1RestoreStage.CandidateComposed), Is.False,
            "Invalid context must reject before candidate runtime composition.");
        Assert.That(stages.Contains(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured), Is.False);
        Assert.That(stages.Contains(P12GDailyV1RestoreStage.BoundaryAdmitted), Is.False);
        Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
    }

    [Test]
    public void PrivateContextFactoryCannotExtendANormalBootstrapSource()
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G normal-source anchor");
        SyntheticSession fixture = CreateSyntheticSession(
            anchor, "P12-G context factory normal-source rejection", seedPoliticalOwners: false,
            restoredContext: false);
        SimulationActiveSession normalSource = fixture.Session;
        Assert.That(normalSource.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advance),
            Is.True, advance.ToString());
        DailyCaptureEligibilityToken token = GetCompletedToken(normalSource.Runtime);
        List<P12GDailyV1RestoreStage> stages = new List<P12GDailyV1RestoreStage>();

        Assert.That(normalSource.AdmissionContext.IsRestoredContinuation, Is.False);
        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                normalSource,
                out SimulationActiveSession candidate,
                out P12GDailyV1RestoreFailure failure,
                out string diagnostic,
                stages.Add,
                restoredAdmissionContextFactoryForTest: _ =>
                    anchor.NormalContext.CreateRestoredContinuationContext()),
            Is.False);

        Assert.That(candidate, Is.Null);
        Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetCompositionFailed));
        Assert.That(diagnostic, Does.Contain("restored-context source"));
        Assert.That(stages.Contains(P12GDailyV1RestoreStage.CandidateComposed), Is.False);
        Assert.That(stages.Contains(P12GDailyV1RestoreStage.BoundaryAdmitted), Is.False);
        Assert.That(normalSource.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
    }

    [Test]
    public void PostTokenPoliticalWriteCannotBeUsedAsAValidSource()
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G stale-source anchor");
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G stale political source");
        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advance),
            Is.True, advance.ToString());
        DailyCaptureEligibilityToken token = GetCompletedToken(source.Session.Runtime);
        PersonRuntime person = source.Session.Runtime.PersonStore.Persons
            .Single(candidate => candidate.PersonId == source.Holder.PersonId);
        PersonDeathKnowledgeObservation lateObservation = new PersonDeathKnowledgeObservation(
            person.PersonId,
            false,
            null,
            token.AbsoluteDay,
            token.AbsoluteDay,
            new PoliticalKnowledgeProvenance(
                PoliticalKnowledgeSource.DirectObservation,
                "p12g-fixture:post-token-mutation"));

        bool wrote = source.Session.Runtime.TryRecordPoliticalKnowledge(
            source.Holder, lateObservation, out PoliticalKnowledgeFailure writeFailure);
        Assert.That(wrote, Is.True, writeFailure.ToString());
        Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.False,
            "A direct post-token owner revision change must invalidate the captured source boundary.");
        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                source.Session,
                out SimulationActiveSession rejected,
                out P12GDailyV1RestoreFailure failure,
                out string diagnostic,
                restoredAdmissionContextFactoryForTest: _ =>
                    source.NormalBootstrapContext.CreateRestoredContinuationContext()),
            Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.SourceBoundaryUnavailable));
        Assert.That(diagnostic, Does.Contain("source").IgnoreCase);
    }

    internal AnchorFixture CreateAnchorFixture(string objectName)
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(DailyConfigPath);
        Assert.That(config, Is.Not.Null, "The accepted P12 Daily-v1 config must be present.");

        GameObject bootstrapObject = new GameObject(objectName);
        bootstrapObjects.Add(bootstrapObject);
        TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
        SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
        serializedBootstrap.FindProperty("simulationConfig").objectReferenceValue = config;
        serializedBootstrap.FindProperty("runtimeAdmissionProfile").enumValueIndex =
            (int)SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;
        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        bootstrap.Start();

        SimulationBootstrapComposition composition = bootstrap.Bootstrap;
        SimulationRuntime runtime = bootstrap.Runtime;
        Assert.That(composition, Is.Not.Null);
        Assert.That(composition.SelectedDefinitions, Is.Not.Null,
            "Daily-v1 composition must retain its immutable selected-definition graph.");
        Assert.That(composition.SelectedDefinitions.ExplorableSites, Is.Empty,
            "The accepted Daily-v1 proving profile remains exact-empty for P10 sites.");
        Assert.That(composition.SelectedDefinitions.Traits, Is.Empty,
            "The accepted Daily-v1 proving profile does not admit TraitData.");
        Assert.That(composition.SelectedDefinitions.CapabilityAttributes, Is.Empty,
            "The accepted Daily-v1 proving profile does not admit CapabilityAttributeData.");
        Assert.That(runtime, Is.SameAs(composition.Runtime));
        SimulationRuntimeAdmissionContext normalContext =
            ((IFactualReadRuntimeState)runtime).AdmissionContext;
        Assert.That(normalContext, Is.Not.Null);
        Assert.That(normalContext.IsRestoredContinuation, Is.False);
        Assert.That(normalContext.Profile, Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(composition.ProfileContractIdentity,
            Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure anchorAdvance),
            Is.True, anchorAdvance.ToString());
        DailyCaptureEligibilityToken anchorToken = GetCompletedToken(runtime);

        DailyV1RuntimeIdentityIndex identityIndex = GetOwner<DailyV1RuntimeIdentityIndex>(
            anchorToken.OwnerSections, RuntimeIdentityRegistryCensusProvider.NpcsSectionId);
        RuntimeIdentityRegistry identityRegistry = ReadPrivateField<RuntimeIdentityRegistry>(
            runtime, "p12RuntimeIdentityRegistry");
        Assert.That(identityIndex, Is.Not.Null);
        Assert.That(identityRegistry, Is.Not.Null);
        Assert.That(identityIndex, Is.SameAs(identityRegistry.CoreIdentityIndex),
            "The P12 C identity census must bind to the Core authority projected by the Unity registry.");
        return new AnchorFixture(bootstrap, config, composition, runtime, normalContext, anchorToken, identityRegistry);
    }

    internal SyntheticSession CreateSyntheticSession(
        AnchorFixture anchor,
        string objectName,
        bool seedPoliticalOwners = true,
        bool restoredContext = true,
        Action<P12DDailyV1OwnerPackage, P12EDailyV1OwnerPackage, P12FDailyV1OwnerPackage> seedAdditionalOwners = null)
    {
        SimulationConfigData config = anchor.Configuration;
        SimulationRuntime anchorRuntime = anchor.Runtime;
        SimulationBootstrapComposition anchorComposition = anchor.Composition;
        DailyCaptureEligibilityToken anchorToken = anchor.Token;

        Assert.That(DailyCaptureStagingAttempt.TryBegin(
            anchorRuntime, anchorToken, anchorToken.OwnerSections,
            out DailyCaptureStagingAttempt stagingAttempt), Is.True);
        P12FDailyV1OwnerCapture.TryCapture(
            anchorRuntime, anchorComposition, anchorToken, anchorToken.OwnerSections,
            out P12FDailyV1OwnerCapture capturedF,
            out P12FDailyV1OwnerPackageFailure fCaptureFailure).ShouldBeTrue(fCaptureFailure);

        Assert.That(P12CWorldIdentitySnapshot.TryCapture(
            anchorComposition.WorldId,
            out P12CWorldIdentitySnapshot worldIdentitySnapshot,
            out string worldIdentityDiagnostic), Is.True, worldIdentityDiagnostic);
        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            anchorComposition.Manifest,
            out P12CP9GenesisManifestSnapshot manifestSnapshot,
            out string manifestDiagnostic), Is.True, manifestDiagnostic);
        Assert.That(P12CSpatialAuthoritySnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            anchorComposition.Manifest.SelectedP9ContractIdentity,
            anchorComposition.Manifest.SelectedP9SchemaVersion,
            anchorComposition.SpatialAuthority,
            out P12CSpatialAuthoritySnapshot spatialSnapshot,
            out P12CSpatialAuthoritySnapshotFailure spatialFailure), Is.True, spatialFailure?.Message);
        DeterministicRandomSource sourceRandom = new DeterministicRandomSource(manifestSnapshot.Seed);
        Assert.That(P12CContinuationRootStager.TryStageForRestore(
            stagingAttempt,
            worldIdentitySnapshot,
            anchorComposition.RuntimeIdAllocator.CaptureSnapshot(),
            anchorComposition.RecordSequence.CaptureSnapshot(),
            spatialSnapshot,
            manifestSnapshot,
            sourceRandom.CaptureSnapshot(),
            out P12CStagedContinuationRoot stagedC,
            out string rootDiagnostic,
            null), Is.True, rootDiagnostic);

        BuildDefinitions(anchorRuntime, out List<CityData> cityDefinitions,
            out List<ItemData> itemDefinitions, out List<NpcData> npcDefinitions);
        Assert.That(P12DDailyV1OwnerPackage.TryCaptureAndStage(
            anchorRuntime,
            anchorToken,
            stagingAttempt,
            anchorToken.OwnerSections,
            anchor.IdentityRegistry,
            anchorComposition.SpatialNetwork,
            anchorComposition.ExplorableSites,
            new RuntimeIdentityRegistry(),
            stagedC.WorldIdentity,
            cityDefinitions,
            itemDefinitions,
            npcDefinitions,
            config.Actions,
            config.Statuses,
            Array.Empty<DailyV1ExplorableSiteDefinition>(),
            capturedF.TravelPartyIds,
            out P12DDailyV1OwnerPackage stagedD,
            out P12DDailyV1OwnerPackageFailure dFailure,
            selectedDefinitions: anchorComposition.SelectedDefinitions), Is.True, dFailure.ToString());

        bool stagedSelectedStatusIsPopulated = false;
        foreach (NpcRuntime stagedNpc in stagedD.Npcs)
        {
            P12DNpcDRow stagedRow = stagedD.NpcDRows.Single(row =>
                string.Equals(row.RuntimeId, stagedNpc.RuntimeId, StringComparison.Ordinal));
            Assert.That(stagedNpc.SelectedDailyV1StatusOwner, Is.Not.Null,
                "Selected P12-D staging must install the canonical status owner before Unity binding.");
            List<string> stagedStatusIds = new List<string>();
            Assert.That(stagedNpc.TryCopySelectedCurrentStatusDefinitionIds(stagedStatusIds), Is.True);
            Assert.That(stagedStatusIds, Is.EqualTo(stagedRow.CurrentStatusDefinitionIds),
                "The staged owner must contain the exact ordered/multiplicity-preserving P12-D status identities.");
            if (stagedStatusIds.Count > 0) stagedSelectedStatusIsPopulated = true;
        }
        Assert.That(stagedSelectedStatusIsPopulated, Is.True,
            "The selected P12-D fixture must prove canonical status-owner staging with populated rows.");

        if (seedPoliticalOwners)
        {
            // The accepted authored Daily-v1 bootstrap is intentionally empty
            // of Persons. Seed this proving fixture into the detached D graph
            // before E/F owner staging and before runtime census/admission.
            Assert.That(stagedD.Persons.TryRegister(
                new PersonRuntime(new PersonId("p12g-political-holder")),
                out PersonStoreFailure holderPersonFailure), Is.True, holderPersonFailure.ToString());
            Assert.That(stagedD.Persons.TryRegister(
                new PersonRuntime(new PersonId("p12g-political-observer")),
                out PersonStoreFailure observerPersonFailure), Is.True, observerPersonFailure.ToString());
            Assert.That(stagedD.Persons.Persons.Count, Is.EqualTo(2));
        }

        SimulationTime stagedTime = new SimulationTime(anchorToken.AbsoluteDay);
        // PoliticalWorldRevision is a constructor seed for this unpublished
        // runtime root; no registered-owner write is needed after census.
        SimulationLogger logger = UnitySimulationLoggerAdapter.Create(config.LogSettings);
        HistoryStore history = new HistoryStore();
        DomainEventStore eventStore = new DomainEventStore(history, new HistoryPolicy(), logger);
        NpcDecisionStore decisions = new NpcDecisionStore();
        DomainEventRecorder eventRecorder = new DomainEventRecorder(
            stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, eventStore, logger);
        NpcDecisionRecorder decisionRecorder = new NpcDecisionRecorder(
            stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, decisions, logger);
        EconomyTransactionService economyTransactions = new EconomyTransactionService();
        P12EDailyV1OwnerStagingContext eContext = new P12EDailyV1OwnerStagingContext(
            stagingAttempt,
            stagedC,
            stagedD,
            stagedTime,
            config.freeStatus,
            config.wantedStatus,
            config.arrestedStatus,
            config.hiddenStatus,
            eventRecorder,
            logger);
        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            anchorRuntime,
            anchorToken,
            anchorToken.OwnerSections,
            eContext,
            out P12EDailyV1OwnerPackage stagedE,
            out P12EDailyV1OwnerPackageFailure eFailure), Is.True, eFailure.ToString());
        Assert.That(capturedF.TryStage(
            anchorRuntime,
            anchorToken,
            anchorToken.OwnerSections,
            eContext,
            stagedE,
            anchorComposition.SelectedDefinitions,
            out P12FDailyV1OwnerPackage stagedF,
            out P12FDailyV1OwnerPackageFailure fStageFailure), Is.True, fStageFailure.ToString());

        if (seedPoliticalOwners)
        {
            SeedInstitutionAndClaimOwners(stagedE, stagedD.Persons, stagedTime.AbsoluteDay);
        }

        PoliticalKnowledgeHolder holder = null;
        PoliticalDecisionId decisionId = null;
        if (seedPoliticalOwners)
        {
            PersonRuntime holderPerson = stagedD.Persons.Persons.First(person => person?.PersonId != null);
            PersonRuntime observedPerson = stagedD.Persons.Persons.First(person =>
                person?.PersonId != null && person.PersonId != holderPerson.PersonId);
            Assert.That(observedPerson, Is.Not.Null, "The accepted bootstrap fixture requires two staged Persons.");
            holder = PoliticalKnowledgeHolder.ForPerson(holderPerson.PersonId);
            Assert.That(stagedF.PoliticalKnowledge.TryRegisterHolder(
                holder, stagedTime.AbsoluteDay, out PoliticalKnowledgeFailure holderFailure), Is.True,
                holderFailure.ToString());
            Assert.That(stagedF.PoliticalKnowledge.TryRecordObservation(
                holder,
                new PersonDeathKnowledgeObservation(
                    observedPerson.PersonId,
                    false,
                    null,
                    stagedTime.AbsoluteDay,
                    stagedTime.AbsoluteDay,
                    new PoliticalKnowledgeProvenance(
                        PoliticalKnowledgeSource.DirectObservation,
                        "p12g-fixture:direct-observation")),
                stagedTime.AbsoluteDay,
                out PoliticalKnowledgeFailure observationFailure), Is.True, observationFailure.ToString());

            decisionId = new PoliticalDecisionId(DecisionIdValue);
            Assert.That(stagedE.PoliticalDecisions.TryBindToPersonStore(stagedD.Persons), Is.True,
                "The detached decision owner must bind to its exact detached PersonStore before runtime construction.");
            PoliticalDecisionRecord decision = new PoliticalDecisionRecord(
                decisionId,
                holder,
                PoliticalDecisionKind.Other,
                Array.Empty<PersonId>(),
                PoliticalDecisionOutcome.None(),
                stagedTime.AbsoluteDay,
                stagedTime.AbsoluteDay,
                new[] { "p12g-fixture:evidence" },
                new[] { KnowledgeReferenceA, KnowledgeReferenceB },
                expectedWorldRevision: 1L,
                expectedKnowledgeRevision: stagedF.PoliticalKnowledge.Revision);
            Assert.That(stagedE.PoliticalDecisions.TryRegister(decision, out PoliticalDecisionFailure decisionFailure),
                Is.True, decisionFailure.ToString());
            Assert.That(stagedE.PoliticalDecisions.Revision, Is.EqualTo(1L));
            Assert.That(stagedF.PoliticalKnowledge.Revision, Is.EqualTo(2L));
        }

        // Qualification fixtures may populate already-staged owners here, before
        // the unpublished graph is composed or any owner is admitted to Runtime.
        seedAdditionalOwners?.Invoke(stagedD, stagedE, stagedF);

        SimulationRuntimeAdmissionContext sessionContext = restoredContext
            ? anchor.NormalContext.CreateRestoredContinuationContext()
            : anchor.NormalContext;
        SimulationActiveSession synthetic = ComposeFixtureSession(
            anchor,
            stagedC,
            stagedD,
            stagedE,
            stagedF,
            stagedTime,
            eventStore,
            history,
            decisions,
            decisionRecorder,
            eventRecorder,
            economyTransactions,
            logger,
            sessionContext,
            politicalWorldRevision: seedPoliticalOwners ? 1L : 0L);
        Assert.That(synthetic.Runtime.WorldId.Value, Is.EqualTo(anchorToken.WorldId.Value));
        if (seedPoliticalOwners)
        {
            Assert.That(synthetic.Runtime.PoliticalDecisionRecords.Single().KnowledgeReferences,
                Is.EqualTo(new[] { KnowledgeReferenceA, KnowledgeReferenceB }));
            Assert.That(synthetic.Runtime.PoliticalKnowledgeHolderCount, Is.EqualTo(1));
            Assert.That(synthetic.Runtime.PoliticalWorldRevision, Is.EqualTo(1L));
        }
        if (sessionContext.IsRestoredContinuation)
            Assert.That(synthetic.Runtime.TryBindRestoredCandidateMutationGuard(out string guardDiagnostic),
                Is.True, guardDiagnostic);
        Assert.That(synthetic.Runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        Assert.That(synthetic.Runtime.TryGetCompletedDailyCaptureToken(out _, out _), Is.False);

        return new SyntheticSession(anchor, synthetic, holder, decisionId);
    }

    private static SimulationActiveSession ComposeFixtureSession(
        AnchorFixture anchor,
        P12CStagedContinuationRoot stagedC,
        P12DDailyV1OwnerPackage stagedD,
        P12EDailyV1OwnerPackage stagedE,
        P12FDailyV1OwnerPackage stagedF,
        SimulationTime stagedTime,
        DomainEventStore eventStore,
        HistoryStore history,
        NpcDecisionStore decisions,
        NpcDecisionRecorder decisionRecorder,
        DomainEventRecorder eventRecorder,
        EconomyTransactionService economyTransactions,
        SimulationLogger logger,
        SimulationRuntimeAdmissionContext admissionContext,
        long politicalWorldRevision)
    {
        SimulationConfigData config = anchor.Configuration;
        EffectiveSimulationConfiguration effective = anchor.Runtime.Configuration;
        Dictionary<SpatialLocationRuntime, CityRuntime> cityByLocation =
            new Dictionary<SpatialLocationRuntime, CityRuntime>();
        foreach (CityRuntime city in stagedD.Cities)
        {
            Assert.That(city?.Location, Is.Not.Null);
            Assert.That(cityByLocation.ContainsKey(city.Location), Is.False);
            cityByLocation.Add(city.Location, city);
        }

        TravelSystem travel = new TravelSystem(
            stagedD.SpatialNetwork,
            location => location != null && cityByLocation.TryGetValue(location, out CityRuntime city)
                ? city
                : null,
            effective.Travel,
            eventRecorder,
            logger,
            economyTransactions);
        ScheduledDirectiveSystem directives = new ScheduledDirectiveSystem(
            stagedF.ScheduledDirectives, stagedD.RuntimeIdentities, logger);
        TravelPartySystem travelParties = new TravelPartySystem(
            stagedF.TravelParties,
            stagedC.RuntimeIdAllocator,
            stagedD.RuntimeIdentities,
            travel,
            stagedTime,
            stagedC.RecordSequence,
            eventRecorder,
            logger,
            economyTransactions);
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();
        ExpeditionSystem expeditions = new ExpeditionSystem(
            stagedF.Expeditions,
            stagedC.RuntimeIdAllocator,
            stagedD.RuntimeIdentities,
            stagedD.EmptyExplorableSites,
            travelParties,
            stagedF.TravelParties,
            siteKnowledge,
            stagedTime,
            eventRecorder,
            logger);

        MerchantSystem merchant = null;
        CommercialKnowledgeSharingSystem commercialKnowledge = null;
        if (effective.MerchantTrade.Enabled == true)
        {
            merchant = new MerchantSystem(
                effective.MerchantTrade,
                effective.CommercialKnowledge,
                travel,
                stagedTime,
                decisionRecorder,
                logger,
                economyTransactions);
            commercialKnowledge = new CommercialKnowledgeSharingSystem(stagedTime, effective.CommercialKnowledge);
        }

        List<INpcActionProvider> actionProviders = new List<INpcActionProvider>
        {
            new TravelActionProvider(travel, merchant)
        };
        if (merchant != null) actionProviders.Add(merchant);
        CrimeSystem crime = new CrimeSystem(
            stagedE.Justice,
            travel,
            config.hiddenStatus,
            logger,
            economyTransactions,
            effective.Crime,
            stagedC.DeterministicRandom,
            stagedTime);
        if (effective.Crime.Enabled == true) actionProviders.Add(crime);
        if (effective.GuardCrime.Enabled == true)
            actionProviders.Add(new GuardSystem(stagedE.Justice, config.hiddenStatus, effective.GuardCrime));
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(actionProviders, stagedC.DeterministicRandom);

        NpcChronicleService chronicles = new NpcChronicleService(decisions, eventStore);
        Assert.That(DailyV1DefinitionAdapter.TryCreateBindings(
                config,
                anchor.Composition.SelectedDefinitions,
                out DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
                out string bindingDiagnostic), Is.True, bindingDiagnostic);
        foreach (CityRuntime city in stagedD.Cities)
        {
            Assert.That(city.CityData, Is.Null,
                "Selected P12 City staging retains its typed definition before the Unity projection is attached.");
            Assert.That(anchor.Composition.SelectedDefinitions.Cities.TryGetValue(
                    city.DefinitionId, out DailyV1CityDefinition cityNode), Is.True);
            Assert.That(cityNode.DefinitionId, Is.EqualTo(city.DefinitionId));
            Assert.That(city.TryBindSelectedDailyV1Definition(
                cityNode, anchor.Composition.SelectedDefinitions, selectedBindings), Is.True);
        }
        foreach (NpcRuntime npc in stagedD.Npcs)
        {
            Assert.That(selectedBindings.TryGet(
                    npc.NpcData, anchor.Composition.SelectedDefinitions,
                    out DailyV1NpcDefinition npcNode), Is.True);
            Assert.That(npc.TryBindSelectedDailyV1Definition(
                npcNode, anchor.Composition.SelectedDefinitions, selectedBindings,
                cityRuntimeId => stagedD.Cities.FirstOrDefault(city => city != null
                    && string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))), Is.True);
        }
        SimulationRuntime runtime = new SimulationRuntime(
            simulationTime: stagedTime,
            cities: stagedD.Cities,
            npcRuntimes: stagedD.Npcs,
            configuredActions: config.Actions,
            scheduledDirectiveSystem: directives,
            justiceSystem: stagedE.Justice,
            crimeSystem: crime,
            npcDecisionSystem: decisionSystem,
            travelSystem: travel,
            travelPartySystem: travelParties,
            merchantSystem: merchant,
            commercialKnowledgeSharingSystem: commercialKnowledge,
            decisionRecorder: decisionRecorder,
            logger: logger,
            explorableSiteStore: stagedD.EmptyExplorableSites,
            explorableSiteKnowledgeSystem: siteKnowledge,
            expeditionSystem: expeditions,
            configuration: effective,
            randomSource: stagedC.DeterministicRandom,
            personStore: stagedD.Persons,
            genealogyStore: stagedD.Genealogy,
            institutionStore: stagedE.Institutions,
            officeStore: stagedE.Offices,
            calendarDefinition: anchor.Composition.Calendar,
            propertyOwnershipStore: stagedE.PropertyOwnership,
            estateStore: stagedE.Estates,
            politicalClaimStore: stagedE.PoliticalClaims,
            factionStore: stagedE.Factions,
            politicalSupportStore: stagedE.PoliticalSupport,
            politicalKnowledgeStore: stagedF.PoliticalKnowledge,
            politicalDecisionStore: stagedE.PoliticalDecisions,
            politicalWorldRevision: politicalWorldRevision,
            armedForceStore: stagedE.ArmedForces,
            conflictStore: stagedE.Conflicts,
            warStore: stagedE.Wars,
            battleStore: stagedE.Battles,
            spatialAuthorityStore: stagedC.SpatialAuthority,
            armedForceSpatialStateStore: stagedE.ArmedForcePositions,
            contingentManpowerStateStore: stagedE.ContingentManpower,
            actorChoiceStore: stagedF.ActorChoices,
            runtimeAdmissionContext: admissionContext,
            recordSequence: stagedC.RecordSequence,
            worldId: stagedC.WorldIdentity,
            runtimeIdAllocator: stagedC.RuntimeIdAllocator,
            economyTransactionService: economyTransactions,
            requireP12ReceiptCensusOwners: true,
            runtimeIdentityRegistry: stagedD.RuntimeIdentities,
            spatialNetworkRuntime: stagedD.SpatialNetwork,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            p12CrimeSocialAppraisalWorldState: stagedE.CrimeSocialAppraisal);

        NpcChronicleFormatter formatter = TesteSimulacao.CreateNpcChronicleFormatter(
            stagedD.RuntimeIdentities,
            stagedD.SpatialNetwork,
            cityByLocation,
            stagedD.EmptyExplorableSites,
            stagedD.Cities,
            config,
            anchor.Composition.SelectedDefinitions);
        TravelPartySystem exactGroupTravel = travelParties;
        SimulationBootstrapComposition composition = new SimulationBootstrapComposition(
            stagedC.WorldIdentity,
            stagedC.GenesisManifest,
            stagedTime,
            anchor.Composition.Calendar,
            stagedD.SpatialNetwork,
            eventStore,
            history,
            stagedF.ScheduledDirectives,
            decisions,
            decisionRecorder,
            stagedC.RecordSequence,
            economyTransactions,
            chronicles,
            formatter,
            stagedF.TravelParties,
            exactGroupTravel,
            runtime,
            stagedD.RuntimeIdentities,
            stagedC.RuntimeIdAllocator,
            stagedD.EmptyExplorableSites,
            stagedF.Expeditions,
            expeditions,
            anchor.Composition.SelectedDefinitions);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            runtime.ExecutionBoundaryState,
            composition.SelectedDefinitions,
            anchor.Token.AbsoluteDay,
            out DailyV1ActiveRuntimeSession coreRuntimeSession,
            out string activeSessionDiagnostic), Is.True, activeSessionDiagnostic);
        return new SimulationActiveSession(
            composition,
            logger,
            stagedE.Justice,
            stagedD.RuntimeIdentities,
            stagedD.SpatialNetwork,
            cityByLocation,
            config,
            admissionContext,
            stagedC.DeterministicRandom,
            anchor.Token.AbsoluteDay,
            coreRuntimeSession);
    }

    private static void BuildDefinitions(
        SimulationRuntime runtime,
        out List<CityData> cities,
        out List<ItemData> items,
        out List<NpcData> npcs)
    {
        cities = new List<CityData>();
        items = new List<ItemData>();
        npcs = new List<NpcData>();
        foreach (CityRuntime city in runtime.Cities)
        {
            if (city?.CityData == null || city.Market == null)
                Assert.Fail("The accepted Daily-v1 source must expose its authored City and Market roots.");
            cities.Add(city.CityData);
            foreach (MarketItemRuntime row in city.Market.Items)
                AddUniqueReference(items, row?.Item);
        }
        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (npc?.NpcData == null)
                Assert.Fail("The accepted Daily-v1 source must expose its authored NPC definitions.");
            npcs.Add(npc.NpcData);
            foreach (InventoryItemRuntime row in npc.ExistingInventory?.Items ?? Array.Empty<InventoryItemRuntime>())
                AddUniqueReference(items, row?.Item);
        }
        Assert.That(cities, Is.Not.Empty);
    }

    private static void AddUniqueReference<T>(List<T> values, T candidate) where T : class
    {
        if (candidate == null || values.Any(value => ReferenceEquals(value, candidate))) return;
        values.Add(candidate);
    }

    private static void AssertPoliticalOwnersAreAuthenticAndPopulated(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token)
    {
        SimulationRuntime runtime = session.Runtime;
        PoliticalDecisionStore decisions = GetOwner<PoliticalDecisionStore>(
            token.OwnerSections, PoliticalDecisionStoreCensusProvider.SectionId);
        PoliticalKnowledgeStore knowledge = runtime.PoliticalKnowledgeStoreForWorldBoundary;
        OwnerSectionCensusSnapshot decisionRow = token.OwnerSections.Single(row =>
            row.SectionId == PoliticalDecisionStoreCensusProvider.SectionId);
        OwnerSectionCensusSnapshot knowledgeRow = token.OwnerSections.Single(row =>
            row.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId);
        Assert.That(decisionRow.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(decisionRow.Cardinality, Is.EqualTo(1));
        Assert.That(decisionRow.Revision, Is.EqualTo(decisions.Revision));
        Assert.That(decisionRow.OwnerInstanceIdentity, Is.SameAs(decisions));
        Assert.That(knowledgeRow.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(knowledgeRow.Cardinality, Is.EqualTo(1));
        Assert.That(knowledgeRow.Revision, Is.EqualTo(knowledge.Revision));
        Assert.That(knowledgeRow.OwnerInstanceIdentity, Is.SameAs(knowledge));
        Assert.That(runtime.PoliticalDecisionRecords.Single().KnowledgeReferences,
            Is.EqualTo(new[] { KnowledgeReferenceA, KnowledgeReferenceB }));
        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(
                runtime,
                token,
                token.OwnerSections,
                out P12EPoliticalDecisionOwnerSnapshot decisionSnapshot,
                out P12EPoliticalDecisionSnapshotFailure decisionFailure),
            Is.True, decisionFailure?.Message);
        Assert.That(decisionSnapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(decisionSnapshot.Decisions.RecordCount, Is.EqualTo(1));
        Assert.That(decisionSnapshot.Decisions.Revision, Is.EqualTo(decisions.Revision));
        P12EPoliticalDecisionSnapshotRow decisionRowValue = decisionSnapshot.Decisions.Records.Single();
        Assert.That(decisionRowValue.DecisionId, Is.EqualTo(DecisionIdValue));
        Assert.That(decisionRowValue.KnowledgeReferences,
            Is.EqualTo(new[] { KnowledgeReferenceA, KnowledgeReferenceB }));
        Assert.That(decisionRowValue.ExpectedWorldRevision, Is.EqualTo(1L));
        Assert.That(decisionRowValue.ExpectedKnowledgeRevision, Is.EqualTo(knowledge.Revision));
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(
                knowledge,
                token,
                token.OwnerSections,
                out P12FPoliticalKnowledgeOwnerSnapshot knowledgeSnapshot,
                out string knowledgeFailure),
            Is.True, knowledgeFailure);
        Assert.That(knowledgeSnapshot.Cardinality, Is.EqualTo(1));
        Assert.That(knowledgeSnapshot.Revision, Is.EqualTo(knowledge.Revision));
        Assert.That(knowledgeSnapshot.CopyDetachedRuntimes().Single().PersonDeathObservations,
            Has.Count.EqualTo(1));
        Assert.That(runtime.PersonStore.TryGet(
                runtime.PoliticalDecisionRecords.Single().Decider.PersonId,
                out PersonRuntime deciderPerson), Is.True);
        Assert.That(deciderPerson, Is.SameAs(runtime.PersonStore.Persons.Single(person =>
            person.PersonId == runtime.PoliticalDecisionRecords.Single().Decider.PersonId)));
        Assert.That(runtime.PoliticalKnowledgeHolderCount, Is.EqualTo(1));
        Assert.That(runtime.TryGetPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(runtime.PoliticalDecisionRecords.Single().Decider.PersonId),
            out PoliticalKnowledgeRuntime facts), Is.True);
        Assert.That(facts.PersonDeathObservations, Has.Count.EqualTo(1));

        InstitutionStore institutions = runtime.InstitutionStoreForWorldBoundary;
        OfficeStore offices = runtime.OfficeStoreForWorldBoundary;
        PoliticalClaimStore claims = GetOwner<PoliticalClaimStore>(
            token.OwnerSections, PoliticalClaimStoreCensusProvider.ClaimsSectionId);
        Assert.That(institutions.Revision, Is.EqualTo(1L));
        Assert.That(offices.Revision, Is.EqualTo(3L));
        Assert.That(InstitutionOfficeOwnerBridge.GetInstitutionOwner(offices), Is.SameAs(institutions));
        IReadOnlyList<OfficeTenureRecord> tenures =
            InstitutionOfficeOwnerBridge.GetTenureHistoryInMutationOrder(offices);
        Assert.That(tenures, Has.Count.EqualTo(1));
        Assert.That(tenures[0].IsClosed, Is.True);
        Assert.That(tenures[0].StartAbsoluteDay.HasValue, Is.True);
        long ownerMutationDay = tenures[0].StartAbsoluteDay.Value;
        Assert.That(ownerMutationDay, Is.LessThanOrEqualTo(token.AbsoluteDay));
        Assert.That(tenures[0].EndAbsoluteDay, Is.EqualTo(ownerMutationDay),
            "The proving tenure closes on its same logical day.");
        AssertSection(token.OwnerSections, InstitutionOfficeCensusProvider.InstitutionsSectionId,
            OwnerSectionRole.Required, 1, institutions.Revision, institutions);
        AssertSection(token.OwnerSections, InstitutionOfficeCensusProvider.OfficesSectionId,
            OwnerSectionRole.Required, 1, offices.Revision, offices);
        AssertSection(token.OwnerSections, InstitutionOfficeCensusProvider.IncumbenciesSectionId,
            OwnerSectionRole.Required, 0, offices.Revision, offices);
        AssertSection(token.OwnerSections, InstitutionOfficeCensusProvider.TenuresSectionId,
            OwnerSectionRole.Required, 1, offices.Revision, offices);

        Assert.That(claims.Count, Is.EqualTo(2));
        Assert.That(claims.RecognitionCount, Is.EqualTo(2));
        Assert.That(claims.Revision, Is.EqualTo(7L));
        AssertSection(token.OwnerSections, PoliticalClaimStoreCensusProvider.ClaimsSectionId,
            OwnerSectionRole.Required, 2, claims.Revision, claims);
        AssertSection(token.OwnerSections, PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
            OwnerSectionRole.Required, 2, claims.Revision, claims);
        Assert.That(P12EInstitutionOfficeCaptureAdapter.TryCapture(
            runtime,
            token,
            token.OwnerSections,
            out P12EInstitutionOfficeOwnerSnapshot institutionSnapshot,
            out P12EInstitutionOfficeSnapshotFailure institutionFailure), Is.True,
            institutionFailure?.Message);
        Assert.That(institutionSnapshot.Offices.Revision, Is.EqualTo(3L));
        Assert.That(institutionSnapshot.Tenures.Records, Has.Count.EqualTo(1));
        Assert.That(institutionSnapshot.Tenures.Records[0].StartAbsoluteDay, Is.EqualTo(ownerMutationDay));
        Assert.That(institutionSnapshot.Tenures.Records[0].EndAbsoluteDay, Is.EqualTo(ownerMutationDay));
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(
            runtime,
            token,
            token.OwnerSections,
            out P12EPoliticalClaimOwnerSnapshot claimSnapshot,
            out P12EPoliticalClaimSnapshotFailure claimFailure), Is.True, claimFailure?.Message);
        Assert.That(claimSnapshot.Claims.Revision, Is.EqualTo(7L));
        Assert.That(claimSnapshot.Claims.Records, Has.Count.EqualTo(2));
        P12EPoliticalClaimRow resolvedClaim = claimSnapshot.Claims.Records.Single(
            row => row.ClaimId == "p12g-claim-resolved-office");
        Assert.That(resolvedClaim.Status, Is.EqualTo((int)PoliticalClaimStatus.Resolved));
        Assert.That(resolvedClaim.ResolutionAbsoluteDay, Is.EqualTo(ownerMutationDay));
        P12EPoliticalClaimRecognitionRow reRecognized = claimSnapshot.Recognitions.Records.Single(
            row => row.ClaimId == "p12g-claim-resolved-office");
        Assert.That(reRecognized.State, Is.EqualTo((int)PoliticalClaimRecognitionState.Recognized));
        Assert.That(reRecognized.History, Has.Count.EqualTo(2));
        Assert.That(reRecognized.History[0].State, Is.EqualTo((int)PoliticalClaimRecognitionState.Unrecognized));
        Assert.That(reRecognized.History[1].State, Is.EqualTo((int)PoliticalClaimRecognitionState.Recognized));
    }

    private static void SeedInstitutionAndClaimOwners(
        P12EDailyV1OwnerPackage stagedOwners,
        PersonStore persons,
        long day)
    {
        InstitutionId institutionId = new InstitutionId("p12g-institution");
        OfficeId officeId = new OfficeId("p12g-office");
        PersonId claimantId = new PersonId("p12g-political-holder");
        PersonId targetId = new PersonId("p12g-political-observer");
        Assert.That(persons.TryGet(claimantId, out _), Is.True);
        Assert.That(persons.TryGet(targetId, out _), Is.True);
        Assert.That(stagedOwners.Institutions.TryRegister(
            new InstitutionRecord(institutionId, "P12-G institution fixture"), out InstitutionFoundationFailure institutionFailure),
            Is.True, institutionFailure.ToString());
        Assert.That(stagedOwners.Offices.TryRegister(
            new OfficeRecord(officeId, institutionId, "P12-G office fixture"), out InstitutionFoundationFailure officeFailure),
            Is.True, officeFailure.ToString());
        Assert.That(stagedOwners.Offices.TryAssignIncumbent(
            officeId, claimantId, day, out InstitutionFoundationFailure assignmentFailure),
            Is.True, assignmentFailure.ToString());
        Assert.That(stagedOwners.Offices.TryVacateOffice(
            officeId, day, InstitutionalVacancyRecognitionReason.ExplicitDecision,
            out InstitutionFoundationFailure vacancyFailure), Is.True, vacancyFailure.ToString());
        Assert.That(stagedOwners.Offices.Revision, Is.EqualTo(3L));

        PoliticalClaimId activeClaimId = new PoliticalClaimId("p12g-claim-active-office");
        PoliticalClaimRecord activeClaim = new PoliticalClaimRecord(
            activeClaimId,
            claimantId,
            PoliticalClaimType.OfficeEntitlement,
            PoliticalClaimTarget.ForOffice(officeId),
            PoliticalClaimBasis.OfficeIncumbency,
            "P12-G active office claim",
            day,
            Array.Empty<string>());
        Assert.That(stagedOwners.PoliticalClaims.TryRegister(activeClaim, out PoliticalClaimFailure activeFailure),
            Is.True, activeFailure.ToString());
        ApplySyntheticRecognition(stagedOwners.PoliticalClaims, activeClaimId, institutionId,
            PoliticalClaimRecognitionState.Unrecognized, day, "initial active recognition");
        ApplySyntheticRecognition(stagedOwners.PoliticalClaims, activeClaimId, institutionId,
            PoliticalClaimRecognitionState.Recognized, day, "updated active recognition");

        PoliticalClaimId resolvedClaimId = new PoliticalClaimId("p12g-claim-resolved-office");
        PoliticalClaimRecord resolvedClaim = new PoliticalClaimRecord(
            resolvedClaimId,
            claimantId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(targetId),
            PoliticalClaimBasis.Other,
            "P12-G resolved claim re-recognition",
            day,
            Array.Empty<string>());
        Assert.That(stagedOwners.PoliticalClaims.TryRegister(resolvedClaim, out PoliticalClaimFailure resolvedFailure),
            Is.True, resolvedFailure.ToString());
        ApplySyntheticRecognition(stagedOwners.PoliticalClaims, resolvedClaimId, institutionId,
            PoliticalClaimRecognitionState.Unrecognized, day, "initial resolved-claim recognition");
        Assert.That(PoliticalClaimOwnerBridge.TryProposeResolution(
            stagedOwners.PoliticalClaims,
            resolvedClaimId,
            PoliticalClaimStatus.Resolved,
            day,
            day,
            out PoliticalClaimResolutionTransition resolution,
            out PoliticalClaimFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(PoliticalClaimOwnerBridge.TryApplyResolution(
            stagedOwners.PoliticalClaims, resolution, out PoliticalClaimFailure resolutionFailure),
            Is.True, resolutionFailure.ToString());
        ApplySyntheticRecognition(stagedOwners.PoliticalClaims, resolvedClaimId, institutionId,
            PoliticalClaimRecognitionState.Recognized, day, "re-recognized after resolution");
        Assert.That(stagedOwners.PoliticalClaims.Revision, Is.EqualTo(7L));
    }

    private static void ApplySyntheticRecognition(
        PoliticalClaimStore owner,
        PoliticalClaimId claimId,
        InstitutionId institutionId,
        PoliticalClaimRecognitionState state,
        long day,
        string reason)
    {
        Assert.That(PoliticalClaimOwnerBridge.TryProposeRecognition(
            owner,
            claimId,
            institutionId,
            state,
            day,
            day,
            reason,
            out PoliticalClaimRecognitionTransition transition,
            out PoliticalClaimFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(PoliticalClaimOwnerBridge.TryApplyRecognition(
            owner, transition, out PoliticalClaimFailure applyFailure), Is.True, applyFailure.ToString());
    }

    private static void AssertSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        OwnerSectionRole role,
        int cardinality,
        long revision,
        object owner)
    {
        OwnerSectionCensusSnapshot row = sections.Single(candidate => candidate.SectionId == sectionId);
        Assert.That(row.Role, Is.EqualTo(role), sectionId);
        Assert.That(row.Cardinality, Is.EqualTo(cardinality), sectionId);
        Assert.That(row.Revision, Is.EqualTo(revision), sectionId);
        Assert.That(row.OwnerInstanceIdentity, Is.SameAs(owner), sectionId);
    }

    internal static void AssertOwnerVectorFactsEqual(
        IReadOnlyList<OwnerSectionCensusSnapshot> left,
        IReadOnlyList<OwnerSectionCensusSnapshot> right)
    {
        Assert.That(left, Is.Not.Null);
        Assert.That(right, Is.Not.Null);
        Assert.That(right, Has.Count.EqualTo(left.Count));
        Dictionary<string, OwnerSectionCensusSnapshot> rightById = right.ToDictionary(row => row.SectionId,
            StringComparer.Ordinal);
        Assert.That(rightById, Has.Count.EqualTo(right.Count), "Owner section identities must be unique.");
        foreach (OwnerSectionCensusSnapshot row in left)
        {
            Assert.That(rightById.TryGetValue(row.SectionId, out OwnerSectionCensusSnapshot match), Is.True,
                "Missing owner section " + row.SectionId);
            Assert.That(match.SchemaVersion, Is.EqualTo(row.SchemaVersion), row.SectionId);
            Assert.That(match.Role, Is.EqualTo(row.Role), row.SectionId);
            Assert.That(match.Cardinality, Is.EqualTo(row.Cardinality), row.SectionId);
            string revisionDiagnostic = row.SectionId;
            if (row.SectionId.StartsWith(NpcPlanCensusProvider.MerchantTradePlanSectionPrefix,
                    StringComparison.Ordinal))
                revisionDiagnostic += "; expected " + DescribeMerchantPlanOwner(row.OwnerInstanceIdentity)
                    + "; actual " + DescribeMerchantPlanOwner(match.OwnerInstanceIdentity);
            Assert.That(match.Revision, Is.EqualTo(row.Revision), revisionDiagnostic);
            Assert.That(match.OwnerInstanceIdentity, Is.Not.SameAs(row.OwnerInstanceIdentity),
                row.SectionId + " must be a private mutable owner instance");
        }

        OwnerSectionCensusSnapshot[] leftRows = left.ToArray();
        OwnerSectionCensusSnapshot[] rightRows = leftRows
            .Select(row => rightById[row.SectionId])
            .ToArray();
        for (int i = 0; i < leftRows.Length; i++)
        {
            for (int j = i + 1; j < leftRows.Length; j++)
            {
                bool leftAliases = ReferenceEquals(
                    leftRows[i].OwnerInstanceIdentity, leftRows[j].OwnerInstanceIdentity);
                bool rightAliases = ReferenceEquals(
                    rightRows[i].OwnerInstanceIdentity, rightRows[j].OwnerInstanceIdentity);
                Assert.That(rightAliases, Is.EqualTo(leftAliases),
                    "Owner-instance alias shape must remain consistent for "
                    + leftRows[i].SectionId + " and " + leftRows[j].SectionId + ".");
            }
        }
    }

    private static string DescribeMerchantPlanOwner(object owner)
    {
        if (owner is DailyV1NpcMerchantTradePlanOwner selected)
            return $"CorePlan(rev={selected.Revision},item={selected.ItemDefinition?.DefinitionId ?? "<null>"},"
                + $"origin={selected.OriginCityRuntimeId ?? "<null>"},target={selected.TargetCityRuntimeId ?? "<null>"},"
                + $"planned={selected.PlannedAmount},remaining={selected.RawRemainingAmount},"
                + $"wait={selected.WaitDaysAtDestination},travel={selected.PendingTravelDays})";
        if (owner is MerchantTradePlanRuntime legacy)
        {
            MerchantTradePlanState state = legacy.CaptureOwnerState();
            return $"UnityPlan(rev={legacy.Revision},item={state.Item?.DefinitionId ?? "<null>"},"
                + $"origin={state.OriginCity?.RuntimeId ?? "<null>"},target={state.TargetCity?.RuntimeId ?? "<null>"},"
                + $"planned={state.PlannedAmount},remaining={state.RawRemainingAmount},"
                + $"wait={state.WaitDaysAtDestination},travel={state.PendingTravelDays})";
        }
        return owner?.GetType().Name ?? "<null>";
    }

    internal static void AssertDailyV1Parity(
        SimulationActiveSession expected,
        DailyCaptureEligibilityToken expectedToken,
        SimulationActiveSession actual,
        DailyCaptureEligibilityToken actualToken,
        string context)
    {
        AssertOwnerVectorFactsEqual(expectedToken.OwnerSections, actualToken.OwnerSections);
        AssertSelectedInventoryAndMarketIdentity(expected);
        AssertSelectedInventoryAndMarketIdentity(actual);
        Assert.That(actual.Composition.Calendar, Is.SameAs(expected.Composition.Calendar),
            "Continuation must preserve the exact CalendarDefinition object supplied to the source composition.");
        Assert.That(actual.Composition.Manifest.EffectiveConfiguration,
            Is.EqualTo(expected.Composition.Manifest.EffectiveConfiguration),
            "Continuation must preserve all effective Daily-v1 configuration values.");
        Assert.That(CaptureTypedParitySignature(actual, actualToken),
            Is.EqualTo(CaptureTypedParitySignature(expected, expectedToken)),
            context + ". Explicit C-F coverage: " + string.Join(" | ", DailyV1TypedParityCoverage));
        AssertPoliticalOwnersAreAuthenticAndPopulated(expected, expectedToken);
        AssertPoliticalOwnersAreAuthenticAndPopulated(actual, actualToken);
    }

    private static void AssertSelectedInventoryAndMarketIdentity(SimulationActiveSession session)
    {
        DailyV1EffectiveDefinitions definitions = session?.Composition?.SelectedDefinitions;
        Assert.That(definitions, Is.Not.Null, "Daily-v1 owner state must retain the admitted typed definition graph.");
        foreach (CityRuntime city in session.Runtime.Cities)
        {
            DailyV1MarketOwner owner = city?.Market?.SelectedDailyV1Owner;
            Assert.That(owner, Is.Not.Null, "Selected Daily-v1 City stock must be owned by shared Core state.");
            Assert.That(owner.Definitions, Is.SameAs(definitions));
            foreach (DailyV1MarketItemState row in owner.Items)
            {
                Assert.That(row?.Item, Is.Not.Null);
                Assert.That(definitions.Items[row.Item.DefinitionId], Is.SameAs(row.Item),
                    "Market rows must retain the exact admitted graph node after capture/restore.");
            }
        }
        foreach (NpcRuntime npc in session.Runtime.NpcRuntimes)
        {
            DailyV1InventoryOwner owner = npc?.ExistingInventory?.SelectedDailyV1Owner;
            Assert.That(owner, Is.Not.Null, "Selected Daily-v1 NPC inventory must be owned by shared Core state.");
            Assert.That(owner.Definitions, Is.SameAs(definitions));
            foreach (DailyV1InventoryItemState row in owner.Items)
            {
                Assert.That(row?.Item, Is.Not.Null);
                Assert.That(definitions.Items[row.Item.DefinitionId], Is.SameAs(row.Item),
                    "Inventory rows must retain the exact admitted graph node after capture/restore.");
            }
        }
    }

    /// <summary>
    /// A reflection-free typed parity signature for the roots and owners that
    /// this fixture exercises. The full token census below covers every
    /// registered C-F section's schema, cardinality, revision, identity, and
    /// alias shape. C roots and the populated E/F political owners are compared
    /// from their typed public/internal APIs rather than a generic formatter.
    /// </summary>
    private static string CaptureTypedParitySignature(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token)
    {
        Assert.That(session, Is.Not.Null);
        Assert.That(token, Is.Not.Null);
        Assert.That(session.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
        Assert.That(ReferenceEquals(session.Runtime.WorldId, session.Composition.WorldId), Is.True,
            "Runtime and bootstrap must retain the exact same WorldId root instance.");
        Assert.That(ReferenceEquals(session.Runtime.SimulationTime, session.Composition.SimulationTime), Is.True,
            "Runtime and bootstrap must retain the exact same SimulationTime root instance.");
        Assert.That(session.Runtime.HasSameSimulationRecordSequenceOwner(
            session.Composition.SimulationRecordSequenceCensusProvider), Is.True,
            "Runtime and bootstrap must retain the exact same record-sequence root.");

        StringBuilder signature = new StringBuilder();
        AppendValue(signature, "C/world-id", session.Composition.WorldId.Value);
        AppendValue(signature, "C/profile-contract", session.Composition.ProfileContractIdentity);
        AppendValue(signature, "C/profile-fingerprint", session.Composition.ProfileFingerprint);
        AppendValue(signature, "C/time-absolute-day", session.Composition.SimulationTime.AbsoluteDay);
        AppendValue(signature, "C/runtime-current-day", session.Runtime.CurrentDay);
        AppendValue(signature, "C/calendar-months", session.Composition.Calendar.MonthsPerYear);
        AppendValue(signature, "C/calendar-weeks", session.Composition.Calendar.WeeksPerMonth);
        AppendValue(signature, "C/calendar-days", session.Composition.Calendar.DaysPerWeek);
        AppendValue(signature, "C/calendar-days-per-month", session.Composition.Calendar.DaysPerMonth);
        AppendValue(signature, "C/calendar-days-per-year", session.Composition.Calendar.DaysPerYear);
        AppendValue(signature, "C/calendar-custom-month-lengths", session.Composition.Calendar.UsesCustomMonthLengths);
        AppendValues(signature, "C/calendar-month-lengths", session.Composition.Calendar.monthLengths);
        SimulationDate date = session.Runtime.Calendar.GetDate(session.Composition.SimulationTime.AbsoluteDay);
        AppendValue(signature, "C/calendar-date-year", date.Year);
        AppendValue(signature, "C/calendar-date-month", date.Month);
        AppendValue(signature, "C/calendar-date-day", date.DayOfMonth);

        Assert.That(P12CWorldIdentitySnapshot.TryCapture(
            session.Composition.WorldId,
            out P12CWorldIdentitySnapshot worldSnapshot,
            out string worldDiagnostic), Is.True, worldDiagnostic);
        AppendValue(signature, "C/world-snapshot-contract", worldSnapshot.SnapshotContract);
        AppendValue(signature, "C/world-snapshot-schema", worldSnapshot.SchemaVersion);
        AppendValue(signature, "C/world-snapshot-value", worldSnapshot.WorldIdValue);

        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            session.Composition.Manifest,
            out P12CP9GenesisManifestSnapshot manifest,
            out string manifestDiagnostic), Is.True, manifestDiagnostic);
        AppendValue(signature, "C/manifest-snapshot-contract", manifest.SnapshotContract);
        AppendValue(signature, "C/manifest-snapshot-schema", manifest.SnapshotSchemaVersion);
        AppendValue(signature, "C/manifest-admission-profile", manifest.P12AdmissionProfile);
        AppendValue(signature, "C/manifest-contract", manifest.ContractIdentity);
        AppendValue(signature, "C/manifest-schema", manifest.SchemaVersion);
        AppendValue(signature, "C/manifest-fingerprint", manifest.Fingerprint);
        AppendValue(signature, "C/manifest-p9-fingerprint", manifest.SelectedP9ProfileFingerprint);
        AppendValue(signature, "C/manifest-p9-contract", manifest.SelectedP9ContractIdentity);
        AppendValue(signature, "C/manifest-p9-schema", manifest.SelectedP9SchemaVersion);
        AppendValues(signature, "C/manifest-provenance", manifest.CanonicalProvenanceRecords);
        AppendValues(signature, "C/manifest-definitions", manifest.AuthoredDefinitionIds);
        AppendValues(signature, "C/manifest-output-owners", manifest.OutputOwners);
        AppendValues(signature, "C/manifest-stage-dependencies", manifest.StageDependencyRecords);
        AppendValues(signature, "C/manifest-stage-order", manifest.StageOrder);
        AppendValue(signature, "C/manifest-first-boundary", manifest.FirstSimulatedBoundary);
        AppendValue(signature, "C/manifest-seed", manifest.Seed);
        AppendValue(signature, "C/manifest-seed-source", manifest.SeedSource);
        AppendValue(signature, "C/manifest-calendar-months", manifest.CalendarMonthsPerYear);
        AppendValue(signature, "C/manifest-calendar-weeks", manifest.CalendarWeeksPerMonth);
        AppendValue(signature, "C/manifest-calendar-days", manifest.CalendarDaysPerWeek);
        AppendValues(signature, "C/manifest-month-lengths", manifest.MonthLengths);

        Assert.That(P12CSpatialAuthoritySnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            session.Composition.Manifest.SelectedP9ContractIdentity,
            session.Composition.Manifest.SelectedP9SchemaVersion,
            session.Composition.SpatialAuthority,
            out P12CSpatialAuthoritySnapshot spatial,
            out P12CSpatialAuthoritySnapshotFailure spatialFailure), Is.True, spatialFailure?.Message);
        AppendValue(signature, "C/spatial-schema", spatial.SchemaId);
        AppendValue(signature, "C/spatial-schema-version", spatial.SchemaVersion);
        AppendValue(signature, "C/spatial-owner-revision", spatial.OwnerRevision);
        AppendValue(signature, "C/spatial-coordinate-version", spatial.CoordinateConventionVersion);
        AppendValue(signature, "C/spatial-canonical-order", spatial.CoordinateCanonicalOrder);
        foreach (P12CSpatialHexSnapshot hex in spatial.Hexes)
        {
            AppendValue(signature, "C/spatial-hex", hex.Id);
            AppendValue(signature, "C/spatial-hex-q", hex.Q);
            AppendValue(signature, "C/spatial-hex-r", hex.R);
            AppendValue(signature, "C/spatial-hex-terrain", hex.TerrainDefinitionId);
            AppendValue(signature, "C/spatial-hex-revision", hex.AuthoredRevisionToken);
        }
        foreach (P12CSpatialLocationSnapshot location in spatial.Locations)
        {
            AppendValue(signature, "C/spatial-location", location.Id);
            AppendValue(signature, "C/spatial-location-anchor", location.AnchorHexId);
        }
        AppendValue(signature, "C/spatial-scale-convention", spatial.ScaleContext.ResolvedConventionId);
        AppendValue(signature, "C/spatial-scale-source", spatial.ScaleContext.SourceIdentity);
        AppendValue(signature, "C/spatial-scale-version", spatial.ScaleContext.SourceVersion);
        AppendValue(signature, "C/spatial-scale-distance", spatial.ScaleContext.DistancePerNeighborStep);
        AppendValue(signature, "C/spatial-scale-unit", spatial.ScaleContext.Unit);

        SimulationRecordSequenceSnapshot sequence = session.Composition.RecordSequence.CaptureSnapshot();
        SimulationRecordSequenceOwnerBridge.CaptureCensusIdentity(
            session.Composition.RecordSequence,
            out object sequenceOwnerIdentity,
            out _);
        OwnerSectionCensusSnapshot sequenceOwner = token.OwnerSections.Single(row =>
            row.SectionId == SimulationRecordSequenceCensusProvider.SectionId);
        Assert.That(sequenceOwner.OwnerInstanceIdentity,
            Is.SameAs(sequenceOwnerIdentity),
            "The runtime token must census the exact composition record-sequence root.");
        AppendValue(signature, "C/sequence-schema", sequence.SchemaId);
        AppendValue(signature, "C/sequence-version", sequence.SchemaVersion);
        AppendValue(signature, "C/sequence-next", sequence.NextSequence);
        RuntimeIdAllocatorSnapshot allocator = session.Composition.RuntimeIdAllocator.CaptureSnapshot();
        IOwnerSectionCensusProvider eventCounterProvider = session.Composition.RuntimeIdAllocatorCensusProviders
            .Single(provider => provider.GetCurrentCensus().SectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId);
        IOwnerSectionCensusProvider decisionCounterProvider = session.Composition.RuntimeIdAllocatorCensusProviders
            .Single(provider => provider.GetCurrentCensus().SectionId == RuntimeIdAllocatorCensusProvider.DecisionsSectionId);
        Assert.That(session.Runtime.HasSameRuntimeIdAllocatorEventCounterOwner(eventCounterProvider), Is.True,
            "Runtime and bootstrap must retain the exact event-counter allocator root.");
        Assert.That(session.Runtime.HasSameRuntimeIdAllocatorDecisionCounterOwner(decisionCounterProvider), Is.True,
            "Runtime and bootstrap must retain the exact decision-counter allocator root.");
        RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
            session.Composition.RuntimeIdAllocator,
            RuntimeIdAllocatorCensusCounter.Events,
            out object allocatorOwnerIdentity,
            out _);
        foreach (OwnerSectionCensusSnapshot allocatorOwner in token.OwnerSections.Where(row =>
                     row.SectionId.StartsWith("p12c.runtime-id-allocator.", StringComparison.Ordinal)))
        {
            Assert.That(allocatorOwner.OwnerInstanceIdentity,
                Is.SameAs(allocatorOwnerIdentity),
                allocatorOwner.SectionId + " must identify the exact composition allocator root.");
        }
        AppendValue(signature, "C/allocator-schema", allocator.SchemaId);
        AppendValue(signature, "C/allocator-version", allocator.SchemaVersion);
        foreach (RuntimeIdAllocatorCounterSnapshot counter in allocator.Counters)
        {
            AppendValue(signature, "C/allocator-family", counter.FamilyId);
            AppendValue(signature, "C/allocator-next", counter.NextSequence);
        }
        Assert.That(session.RandomSource, Is.TypeOf<DeterministicRandomSource>());
        DeterministicRandomRootSnapshot random = ((DeterministicRandomSource)session.RandomSource).CaptureSnapshot();
        AppendValue(signature, "C/random-schema", random.SchemaId);
        AppendValue(signature, "C/random-version", random.SchemaVersion);
        AppendValue(signature, "C/random-provider", random.ProviderId);
        AppendValue(signature, "C/random-provider-version", random.ProviderVersion);
        AppendValue(signature, "C/random-algorithm", random.AlgorithmId);
        AppendValue(signature, "C/random-algorithm-version", random.AlgorithmVersion);
        AppendValue(signature, "C/random-seed", random.Seed);

        DailyV1RuntimeIdentityIndex identities = GetOwner<DailyV1RuntimeIdentityIndex>(
            token.OwnerSections, RuntimeIdentityRegistryCensusProvider.NpcsSectionId);
        Assert.That(identities, Is.SameAs(session.IdentityRegistry.CoreIdentityIndex),
            "The C census must retain the exact Core identity authority projected by the admitted Unity registry.");
        foreach (KeyValuePair<string, string> identity in session.IdentityRegistry.CaptureRegisteredRuntimeIdentityValues()
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            AppendValue(signature, "C/runtime-identity-family", identity.Key);
            AppendValue(signature, "C/runtime-identity-value", identity.Value);
        }

        foreach (OwnerSectionCensusSnapshot section in token.OwnerSections.OrderBy(row => row.SectionId, StringComparer.Ordinal))
        {
            AppendValue(signature, "C-F/section-id", section.SectionId);
            AppendValue(signature, "C-F/section-schema", section.SchemaVersion);
            AppendValue(signature, "C-F/section-role", section.Role);
            AppendValue(signature, "C-F/section-cardinality", section.Cardinality);
            AppendValue(signature, "C-F/section-revision", section.Revision);
        }

        PoliticalDecisionStore decisionOwner = GetOwner<PoliticalDecisionStore>(
            token.OwnerSections, PoliticalDecisionStoreCensusProvider.SectionId);
        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(
            session.Runtime,
            token,
            token.OwnerSections,
            out P12EPoliticalDecisionOwnerSnapshot decisionSnapshot,
            out P12EPoliticalDecisionSnapshotFailure decisionFailure), Is.True, decisionFailure?.Message);
        AppendValue(signature, "E/decision-captured-day", decisionSnapshot.CapturedAbsoluteDay);
        AppendValue(signature, "E/decision-count", decisionSnapshot.Decisions.RecordCount);
        AppendValue(signature, "E/decision-revision", decisionSnapshot.Decisions.Revision);
        foreach (P12EPoliticalDecisionSnapshotRow decision in decisionSnapshot.Decisions.Records)
        {
            AppendValue(signature, "E/decision-id", decision.DecisionId);
            AppendValue(signature, "E/decision-decider-kind", decision.DeciderKind);
            AppendValue(signature, "E/decision-decider-id", decision.DeciderId);
            AppendValue(signature, "E/decision-kind", decision.DecisionKind);
            AppendValues(signature, "E/decision-candidates", decision.CandidatePersonIds);
            AppendValue(signature, "E/decision-outcome-kind", decision.OutcomeKind);
            AppendValue(signature, "E/decision-selected-person", decision.SelectedCandidatePersonId);
            AppendValue(signature, "E/decision-referenced-claim", decision.ReferencedClaimId);
            AppendValue(signature, "E/decision-observed-day", decision.ObservedAbsoluteDay);
            AppendValue(signature, "E/decision-day", decision.DecisionAbsoluteDay);
            AppendValues(signature, "E/decision-evidence", decision.EvidenceReferences);
            AppendValues(signature, "E/decision-knowledge-references", decision.KnowledgeReferences);
            AppendValue(signature, "E/decision-world-revision", decision.ExpectedWorldRevision);
            AppendValue(signature, "E/decision-knowledge-revision", decision.ExpectedKnowledgeRevision);
            AppendValue(signature, "E/decision-office", decision.OfficeId);
            AppendValue(signature, "E/decision-recognizing-institution", decision.RecognizingInstitutionId);
        }
        AppendValue(signature, "E/decision-owner-count", decisionOwner.Count);
        AppendValue(signature, "E/decision-owner-revision", decisionOwner.Revision);

        PoliticalKnowledgeStore knowledgeOwner = GetOwner<PoliticalKnowledgeStore>(
            token.OwnerSections, PoliticalKnowledgeStoreCensusProvider.SectionId);
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(
            knowledgeOwner,
            token,
            token.OwnerSections,
            out P12FPoliticalKnowledgeOwnerSnapshot knowledgeSnapshot,
            out string knowledgeDiagnostic), Is.True, knowledgeDiagnostic);
        AppendValue(signature, "F/knowledge-cardinality", knowledgeSnapshot.Cardinality);
        AppendValue(signature, "F/knowledge-revision", knowledgeSnapshot.Revision);
        foreach (PoliticalKnowledgeRuntime knowledge in knowledgeSnapshot.CopyDetachedRuntimes())
        {
            AppendPoliticalKnowledgeRuntime(signature, "F/knowledge", knowledge);
        }
        AppendAllIncludedOwnerSnapshotFacts(session, token, signature);
        return signature.ToString();
    }

    /// <summary>
    /// Captures every included D/E/F owner package through its typed snapshot
    /// API, then projects each exposed field explicitly. The generic structural
    /// projection in SimulationRuntimeAdmissionTests is intentionally not used
    /// here because this positive-path parity proof must remain reflection-free.
    /// </summary>
    private static void AppendAllIncludedOwnerSnapshotFacts(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token,
        StringBuilder signature)
    {
        SimulationRuntime runtime = session.Runtime;
        SimulationBootstrapComposition composition = session.Composition;
        IReadOnlyList<OwnerSectionCensusSnapshot> vector = token.OwnerSections;
        object captureStamp = new object();

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, captureStamp, vector,
            out P12DNpcDProjection npcD,
            out P12DNpcFProjection npcF,
            out P12DNpcRootOwnerSnapshotFailure npcFailure), Is.True, npcFailure.ToString());
        Assert.That(npcD.Token, Is.SameAs(token));
        Assert.That(npcF.Token, Is.SameAs(token));
        Assert.That(npcD.CaptureStamp, Is.SameAs(captureStamp));
        Assert.That(npcF.CaptureStamp, Is.SameAs(captureStamp));
        Assert.That(npcD.OwnerSectionVector, Is.SameAs(vector));
        Assert.That(npcF.OwnerSectionVector, Is.SameAs(vector));
        Assert.That(npcD.Evidence.HasSameCaptureIdentity(token, captureStamp, vector), Is.True);
        Assert.That(npcF.Evidence.HasSameCaptureIdentity(token, captureStamp, vector), Is.True);
        Assert.That(npcD.Evidence.HasSameCaptureIdentity(npcF.Evidence), Is.True,
            "D and F projections must preserve the same source-roster capture provenance.");
        Dictionary<string, NpcRuntime> runtimeRoster = runtime.NpcRuntimes.ToDictionary(
            npc => npc.RuntimeId, StringComparer.Ordinal);
        Assert.That(npcD.Evidence.CoversStagedNpcRuntimeIds(runtimeRoster), Is.True,
            "D/F capture provenance must cover every runtime NPC identity exactly once.");
        foreach (P12DNpcDRow row in npcD.Rows)
        {
            Assert.That(runtimeRoster.TryGetValue(row.RuntimeId, out NpcRuntime npc), Is.True);
            List<string> selectedStatusIds = new List<string>();
            Assert.That(npc.TryCopySelectedCurrentStatusDefinitionIds(selectedStatusIds), Is.True,
                "Selected P12-D status capture must come from the typed Core owner.");
            Assert.That(row.CurrentStatusDefinitionIds, Is.EqualTo(selectedStatusIds),
                "The unchanged P12-D row must preserve typed status identity, order, and multiplicity.");
            Assert.That(npc.CurrentStatus.Select(status => status?.DefinitionId), Is.EqualTo(selectedStatusIds),
                "The selected Unity compatibility projection must match the exact Core status owner.");

            if (npc.UsesSelectedDailyV1Definitions)
            {
                DailyV1NpcActionSlotOwner actionSlot = npc.SelectedRuntimeOwners?.ActionSlot;
                Assert.That(actionSlot, Is.Not.Null);
                Assert.That(actionSlot.TryCapturePayload(
                    out DailyV1NpcActionPayloadSnapshot payload), Is.True,
                    "Selected P12 action facts must be capturable from the exact Core action slot.");
                P12DNpcFRow fRow = npcF.Rows.Single(value => value.RuntimeId == row.RuntimeId);
                if (payload == null)
                {
                    Assert.That(npc.CurrentActionRuntime, Is.Null,
                        "An empty Core action slot cannot retain an independent Unity action projection.");
                    Assert.That(fRow.Action, Is.Null);
                }
                else
                {
                    Assert.That(payload.Owner, Is.SameAs(actionSlot.CurrentPayload));
                    Assert.That(npc.CurrentActionRuntime.SelectedOwner, Is.SameAs(payload.Owner));
                    Assert.That(npc.CurrentActionRuntime.MatchesSelectedOwner(), Is.True);
                    Assert.That(fRow.Action.ActionDefinitionId, Is.EqualTo(payload.ActionDefinitionId));
                    Assert.That(fRow.Action.TargetNpcRuntimeId, Is.EqualTo(payload.TargetNpcRuntimeId));
                    Assert.That(fRow.Action.TargetCityRuntimeId, Is.EqualTo(payload.TargetCityRuntimeId));
                    Assert.That(fRow.Action.TargetItemDefinitionId, Is.EqualTo(payload.TargetItemDefinitionId));
                    Assert.That(fRow.Action.Amount, Is.EqualTo(payload.Amount));
                    Assert.That(fRow.Action.ExpectedUnitPrice, Is.EqualTo(payload.ExpectedUnitPrice));
                    Assert.That(fRow.Action.SuccessChanceMultiplier, Is.EqualTo(payload.SuccessChanceMultiplier));
                    Assert.That(fRow.Action.TravelReason, Is.EqualTo(payload.TravelReason));
                    Assert.That(fRow.Action.ExpectedNetValue, Is.EqualTo(payload.ExpectedNetValue));
                    Assert.That(fRow.Action.OriginDecisionId, Is.EqualTo(payload.OriginDecisionId));
                    Assert.That(fRow.Action.StableOccurrenceKey, Is.EqualTo(payload.StableOccurrenceKey));
                    Assert.That(fRow.Action.CommercialDecisionEvidence,
                        Is.SameAs(payload.CommercialDecisionEvidence));
                    Assert.That(fRow.Action.CommercialScoutingEvidence,
                        Is.SameAs(payload.CommercialScoutingEvidence));
                }
            }
        }

        for (int index = 0; index < runtime.Cities.Count; index++)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(
                runtime.Cities[index], token, captureStamp, vector,
                out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityEnvelope,
                out P12DCityRootOwnerSnapshotFailure cityFailure), Is.True, cityFailure.ToString());
            Assert.That(cityEnvelope.HasSameCaptureIdentity(npcD.Evidence), Is.True,
                "Each City D snapshot must share the exact completed token, capture stamp, and owner vector with the NPC D/F projections.");
            AppendCitySnapshot(signature, "D/city/" + index, cityEnvelope.Snapshot);
        }

        AppendNpcRows(signature, "D/npc", npcD.Rows);
        AppendNpcFRows(signature, "D/npc-f", npcF.Rows);

        PersonStoreOwnerSnapshot persons = runtime.PersonStore.CaptureOwnerSnapshot();
        AppendValue(signature, "D/person-store/schema-id", persons.SchemaId);
        AppendValue(signature, "D/person-store/schema", persons.SchemaVersion);
        AppendValue(signature, "D/person-store/revision", persons.Revision);
        AppendValue(signature, "D/person-store/membership-count", persons.MembershipCount);
        AppendValue(signature, "D/person-store/binding-count", persons.BindingCount);
        int personIndex = 0;
        foreach (PersonStoreOwnerSnapshotRow row in persons.Rows)
        {
            string key = "D/person-store/row/" + personIndex++;
            AppendValue(signature, key + "/id", row.PersonIdValue);
            AppendValue(signature, key + "/birth", row.BirthAbsoluteDay);
            AppendValue(signature, key + "/death", row.DeathAbsoluteDay);
            AppendValue(signature, key + "/residence", row.ResidenceSettlementRuntimeId);
            AppendValue(signature, key + "/npc-binding", row.MaterializedNpcRuntimeId);
            AppendValue(signature, key + "/life-residence-revision", row.LifeResidenceRevision);
        }

        GenealogyOwnerSnapshot genealogy = new GenealogyOwnerSnapshot(
            GenealogyOwnerBridge.CaptureOwnerSnapshot(runtime.GenealogyStoreForWorldBoundary));
        AppendValue(signature, "D/genealogy/schema", genealogy.SchemaVersion);
        AppendValue(signature, "D/genealogy/revision", genealogy.Revision);
        int genealogyIndex = 0;
        foreach (ParentageRecord row in genealogy.Records)
        {
            string key = "D/genealogy/row/" + genealogyIndex++;
            AppendValue(signature, key + "/parent", row.ParentId.Value);
            AppendValue(signature, key + "/child", row.ChildId.Value);
        }

        SpatialNetworkOwnerSnapshot spatial = session.SpatialNetwork.CaptureOwnerSnapshot();
        AppendValue(signature, "D/spatial-network/schema", spatial.SchemaVersion);
        AppendValue(signature, "D/spatial-network/revision", spatial.Revision);
        AppendValues(signature, "D/spatial-network/locations", spatial.LocationRuntimeIds);
        int routeIndex = 0;
        foreach (SpatialRouteOwnerSnapshotRecord row in spatial.Routes)
        {
            string key = "D/spatial-network/route/" + routeIndex++;
            AppendValue(signature, key + "/id", row.RuntimeId);
            AppendValue(signature, key + "/origin", row.OriginLocationRuntimeId);
            AppendValue(signature, key + "/destination", row.DestinationLocationRuntimeId);
            AppendValue(signature, key + "/travel-days", row.TravelDays);
        }

        Assert.That(composition.ExplorableSites.TryCaptureOwnerSnapshot(
            out ExplorableSiteOwnerSnapshot sites, out ExplorableSiteSnapshotFailure siteFailure),
            Is.True, siteFailure?.Message);
        AppendValue(signature, "D/sites/schema", sites.SchemaVersion);
        AppendValue(signature, "D/sites/revision", sites.Revision);
        int siteIndex = 0;
        foreach (ExplorableSiteOwnerSnapshotRecord row in sites.Sites)
        {
            string key = "D/sites/row/" + siteIndex++;
            AppendValue(signature, key + "/runtime-id", row.RuntimeId);
            AppendValue(signature, key + "/instance-id", row.SiteInstanceId);
            AppendValue(signature, key + "/definition-id", row.DefinitionId);
            AppendValue(signature, key + "/location-reference", row.LocationRuntimeId);
        }

        AppendESnapshotFacts(runtime, token, signature);
        AppendRemainingFSnapshotFacts(session, token, signature);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True,
            "Typed C-F snapshot comparison must retain the exact completed-boundary token.");
    }

    private static void AppendESnapshotFacts(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        StringBuilder signature)
    {
        IReadOnlyList<OwnerSectionCensusSnapshot> vector = token.OwnerSections;
        Assert.That(P12EInstitutionOfficeCaptureAdapter.TryCapture(
            runtime, token, vector,
            out P12EInstitutionOfficeOwnerSnapshot institutionOffice,
            out P12EInstitutionOfficeSnapshotFailure institutionFailure), Is.True, institutionFailure?.Message);
        AppendInstitutionOfficeSnapshot(signature, institutionOffice);

        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            runtime.PropertyOwnershipStore, runtime.EstateStore, token, vector,
            out PropertyEstateOwnerSnapshot propertyEstate,
            out PropertyEstateOwnerSnapshotFailure propertyFailure), Is.True, propertyFailure?.Message);
        AppendPropertyEstateSnapshot(signature, propertyEstate);

        FactionStore factionOwner = GetOwner<FactionStore>(vector, FactionStoreCensusProvider.FactionsSectionId);
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(
            runtime, factionOwner, token, vector,
            out P12EFactionOwnerSnapshot factions,
            out P12EFactionSnapshotFailure factionFailure), Is.True, factionFailure?.Message);
        AppendFactionSnapshot(signature, factions);

        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EPoliticalClaimOwnerSnapshot claims,
            out P12EPoliticalClaimSnapshotFailure claimFailure), Is.True, claimFailure?.Message);
        AppendClaimSnapshot(signature, claims);

        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EPoliticalSupportOwnerSnapshot support,
            out P12EPoliticalSupportSnapshotFailure supportFailure), Is.True, supportFailure?.Message);
        AppendSupportSnapshot(signature, support);

        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            runtime.ArmedForceStore, runtime.ContingentManpowerStateStore,
            runtime.ArmedForceSpatialStateStore, token, vector,
            out P12EMilitaryOwnerSnapshot military,
            out P12EMilitaryOwnerSnapshotFailure militaryFailure), Is.True, militaryFailure?.Message);
        AppendMilitarySnapshot(signature, military);

        Assert.That(PersistentConflictOwnerSnapshot.TryCapture(
            runtime.ConflictStore, token, vector,
            out PersistentConflictOwnerSnapshot conflicts,
            out PersistentConflictOwnerSnapshotFailure conflictFailure), Is.True, conflictFailure?.Message);
        AppendConflictSnapshot(signature, conflicts);

        Assert.That(PersistentWarOwnerSnapshot.TryCapture(
            runtime.WarStore, token, vector,
            out PersistentWarOwnerSnapshot wars,
            out PersistentWarOwnerSnapshotFailure warFailure), Is.True, warFailure?.Message);
        AppendWarSnapshot(signature, wars);

        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(
            runtime.BattleStore, token, vector,
            out PersistentBattleOwnerSnapshot battles,
            out PersistentBattleOwnerSnapshotFailure battleFailure), Is.True, battleFailure?.Message);
        AppendBattleSnapshot(signature, battles);

        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EJusticeRecordsOwnerSnapshot justice,
            out P12EJusticeSnapshotFailure justiceFailure), Is.True, justiceFailure?.Message);
        AppendJusticeSnapshot(signature, justice);

        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocial,
            out P12ECrimeSocialAppraisalSnapshotFailure crimeSocialFailure), Is.True, crimeSocialFailure?.Message);
        AppendCrimeSocialSnapshot(signature, crimeSocial);
    }

    private static void AppendCitySnapshot(
        StringBuilder signature,
        string key,
        P12DCityRootOwnerSnapshot row)
    {
        AppendValue(signature, key + "/schema", row.SchemaVersion);
        AppendValue(signature, key + "/runtime-id", row.CityRuntimeId);
        AppendValue(signature, key + "/definition-id", row.CityDefinitionId);
        AppendValue(signature, key + "/legacy-location-reference", row.LegacyLocationRuntimeId);
        AppendValue(signature, key + "/important-npc-revision", row.ImportantNpcRevision);
        AppendValues(signature, key + "/important-npc-ids", row.ImportantNpcRuntimeIds);
        AppendValue(signature, key + "/market-counterparty", row.MarketCounterpartyRuntimeId);
        AppendValue(signature, key + "/market-liquidity-mode", row.MarketLiquidityMode);
        AppendValue(signature, key + "/market-has-account", row.MarketCounterpartyHasAccount);
        AppendValue(signature, key + "/market-revision", row.MarketRevision);
        int itemIndex = 0;
        foreach (P12DCityMarketItemSnapshot item in row.MarketItems)
        {
            string itemKey = key + "/market-item/" + itemIndex++;
            AppendValue(signature, itemKey + "/definition-id", item.ItemDefinitionId);
            AppendValue(signature, itemKey + "/amount", item.Amount);
            AppendValue(signature, itemKey + "/desired-amount", item.DesiredAmount);
            AppendValue(signature, itemKey + "/price", item.CurrentPrice);
        }
        AppendValue(signature, key + "/population-economic-owner", row.PopulationEconomicRuntimeId);
        AppendValue(signature, key + "/population-payment-mode", row.PopulationPaymentMode);
        AppendValue(signature, key + "/population-has-account", row.PopulationEconomyHasAccount);
        AppendValue(signature, key + "/population-runtime-id", row.PopulationRuntimeId);
        AppendValue(signature, key + "/population-value", row.CurrentPopulation);
        AppendValue(signature, key + "/population-revision", row.PopulationRevision);
        AppendValue(signature, key + "/population-receipt-revision", row.PopulationReceiptRevision);
        int receiptIndex = 0;
        foreach (PopulationOperationReceiptSnapshot receipt in row.PopulationReceipts)
        {
            string receiptKey = key + "/population-receipt/" + receiptIndex++;
            AppendValue(signature, receiptKey + "/identity", receipt.Identity);
            AppendValue(signature, receiptKey + "/fingerprint", receipt.Fingerprint);
            SettlementPopulationTransition transition = receipt.Transition;
            AppendValue(signature, receiptKey + "/transition-settlement", transition.SettlementRuntimeId);
            AppendValue(signature, receiptKey + "/transition-expected-revision", transition.ExpectedRevision);
            AppendValue(signature, receiptKey + "/transition-before", transition.PopulationBefore);
            AppendValue(signature, receiptKey + "/transition-births", transition.Births);
            AppendValue(signature, receiptKey + "/transition-deaths", transition.Deaths);
            AppendValue(signature, receiptKey + "/transition-immigrations", transition.Immigrations);
            AppendValue(signature, receiptKey + "/transition-emigrations", transition.Emigrations);
            AppendValue(signature, receiptKey + "/transition-net", transition.NetChange);
            AppendValue(signature, receiptKey + "/transition-after", transition.PopulationAfter);
        }
    }

    private static void AppendNpcRows(
        StringBuilder signature,
        string key,
        IReadOnlyList<P12DNpcDRow> rows)
    {
        int rowIndex = 0;
        foreach (P12DNpcDRow row in rows)
        {
            string rowKey = key + "/row/" + rowIndex++;
            AppendValue(signature, rowKey + "/runtime-id", row.RuntimeId);
            AppendValue(signature, rowKey + "/definition-id", row.NpcDefinitionId);
            AppendValue(signature, rowKey + "/person-id", row.PersonIdValue);
            AppendValue(signature, rowKey + "/residence-settlement", row.ResidenceSettlementRuntimeId);
            AppendValue(signature, rowKey + "/life-state", row.LifeState);
            AppendValue(signature, rowKey + "/injury", row.InjurySeverity);
            AppendValues(signature, rowKey + "/statuses", row.CurrentStatusDefinitionIds);
            AppendValue(signature, rowKey + "/hidden-days", row.HiddenDaysRemaining);
            AppendValue(signature, rowKey + "/action-id", row.CurrentActionDefinitionId);
            AppendValue(signature, rowKey + "/current-city", row.CurrentCityRuntimeId);
            AppendValue(signature, rowKey + "/current-location", row.CurrentLocationRuntimeId);
            AppendValue(signature, rowKey + "/destination-city", row.DestinationCityRuntimeId);
            AppendValue(signature, rowKey + "/destination-location", row.DestinationLocationRuntimeId);
            int inventoryIndex = 0;
            foreach (P12DNpcInventoryRowValue item in row.InventoryRows)
            {
                string itemKey = rowKey + "/inventory/" + inventoryIndex++;
                AppendValue(signature, itemKey + "/item", item.ItemDefinitionId);
                AppendValue(signature, itemKey + "/amount", item.Amount);
                AppendValue(signature, itemKey + "/average-unit-cost", item.AverageUnitCost);
            }
            AppendValue(signature, rowKey + "/inventory-revision", row.InventoryRevision);
            AppendValue(signature, rowKey + "/money-balance", row.MoneyBalance);
            AppendValue(signature, rowKey + "/money-revision", row.MoneyRevision);
            AppendValue(signature, rowKey + "/life-revision", row.LifeStateRevision);
            AppendValue(signature, rowKey + "/residence-revision", row.ResidenceRevision);
            AppendValue(signature, rowKey + "/crime-justice-revision", row.CrimeJusticeRevision);
            AppendValue(signature, rowKey + "/action-revision", row.CurrentActionRevision);
        }
    }

    private static void AppendNpcFRows(
        StringBuilder signature,
        string key,
        IReadOnlyList<P12DNpcFRow> rows)
    {
        int rowIndex = 0;
        foreach (P12DNpcFRow row in rows)
        {
            string rowKey = key + "/row/" + rowIndex++;
            AppendValue(signature, rowKey + "/runtime-id", row.RuntimeId);
            AppendNpcAction(signature, rowKey + "/action", row.Action);
            AppendValue(signature, rowKey + "/travel-days-remaining", row.TravelDaysRemaining);
            AppendValue(signature, rowKey + "/travel-days-total", row.TravelDaysTotal);
            AppendValue(signature, rowKey + "/travel-route", row.TravelRouteRuntimeId);
            AppendValue(signature, rowKey + "/travel-started-today", row.TravelStartedToday);
            AppendValue(signature, rowKey + "/travel-origin-decision", row.TravelOriginDecisionId);
            AppendValue(signature, rowKey + "/active-travel-party", row.ActiveTravelPartyId);
            AppendValue(signature, rowKey + "/travel-revision", row.TravelStateRevision);
            AppendNpcMerchantPlan(signature, rowKey + "/merchant-plan", row.MerchantPlan);
            AppendNpcTravelPlan(signature, rowKey + "/travel-plan", row.TravelPlan);
            int observationIndex = 0;
            foreach (ExplorableSiteKnowledgeObservation observation in row.SiteObservations)
            {
                string observationKey = rowKey + "/site-observation/" + observationIndex++;
                AppendValue(signature, observationKey + "/site", observation.SiteRuntimeId);
                AppendValue(signature, observationKey + "/location", observation.LocationRuntimeId);
                AppendValue(signature, observationKey + "/observed-day", observation.ObservedDay);
                AppendValue(signature, observationKey + "/received-day", observation.ReceivedDay);
                AppendValue(signature, observationKey + "/source", observation.Source);
            }
            AppendValue(signature, rowKey + "/site-knowledge-revision", row.SiteKnowledgeRevision);
            AppendValues(signature, rowKey + "/known-locations", row.KnownLocationRuntimeIds);
            AppendValues(signature, rowKey + "/known-routes", row.KnownRouteRuntimeIds);
            AppendValue(signature, rowKey + "/spatial-knowledge-revision", row.SpatialKnowledgeRevision);
            AppendAdventureObservations(signature, rowKey, row);
            AppendValue(signature, rowKey + "/adventure-knowledge-revision", row.AdventureKnowledgeRevision);
            AppendCommercialFacts(signature, rowKey, row);
            AppendValue(signature, rowKey + "/commercial-knowledge-revision", row.CommercialKnowledgeRevision);
        }
    }

    private static void AppendNpcAction(StringBuilder signature, string key, P12DNpcActionValue action)
    {
        AppendValue(signature, key + "/definition-id", action?.ActionDefinitionId);
        if (action == null) return;
        AppendValue(signature, key + "/target-npc", action.TargetNpcRuntimeId);
        AppendValue(signature, key + "/target-city", action.TargetCityRuntimeId);
        AppendValue(signature, key + "/target-item", action.TargetItemDefinitionId);
        AppendValue(signature, key + "/amount", action.Amount);
        AppendValue(signature, key + "/expected-unit-price", action.ExpectedUnitPrice);
        AppendValue(signature, key + "/success-chance-multiplier", action.SuccessChanceMultiplier);
        AppendValue(signature, key + "/travel-reason", action.TravelReason);
        AppendValue(signature, key + "/expected-net-value", action.ExpectedNetValue);
        AppendValue(signature, key + "/origin-decision", action.OriginDecisionId);
        AppendValue(signature, key + "/occurrence-key", action.StableOccurrenceKey);
        AppendCommercialDecisionEvidence(signature, key + "/decision-evidence", action.CommercialDecisionEvidence);
        CommercialScoutingEvidence scouting = action.CommercialScoutingEvidence;
        AppendValue(signature, key + "/scouting-target", scouting?.TargetLocationRuntimeId);
        AppendValue(signature, key + "/scouting-route", scouting?.KnownRouteRuntimeId);
        AppendValue(signature, key + "/scouting-unknown-count", scouting?.UnknownObservationCount);
        AppendValue(signature, key + "/scouting-stale-count", scouting?.StaleObservationCount);
        AppendValue(signature, key + "/scouting-has-oldest", scouting?.HasOldestObservation);
        AppendValue(signature, key + "/scouting-oldest-day", scouting?.OldestObservationDay);
        AppendValue(signature, key + "/scouting-travel-cost", scouting?.ExpectedTravelCost);
        AppendValue(signature, key + "/scouting-travel-days", scouting?.ExpectedTravelDays);
        AppendValue(signature, key + "/scouting-score", scouting?.ExpectedScore);
    }

    private static void AppendCommercialDecisionEvidence(
        StringBuilder signature,
        string key,
        CommercialDecisionEvidence evidence)
    {
        AppendValue(signature, key + "/item", evidence?.ItemDefinitionId);
        if (evidence == null) return;
        AppendValue(signature, key + "/current-location", evidence.CurrentLocationRuntimeId);
        AppendValue(signature, key + "/origin-location", evidence.TradeOriginLocationRuntimeId);
        AppendValue(signature, key + "/destination-location", evidence.TradeDestinationLocationRuntimeId);
        AppendValue(signature, key + "/previous-destination", evidence.PreviousDestinationLocationRuntimeId);
        AppendCommercialObservationEvidence(signature, key + "/origin-observation", evidence.OriginObservation);
        AppendCommercialObservationEvidence(signature, key + "/destination-observation", evidence.DestinationObservation);
        CommercialLiquidityEvidence liquidity = evidence.DestinationLiquidityObservation;
        AppendValue(signature, key + "/liquidity-location", liquidity?.LocationRuntimeId);
        AppendValue(signature, key + "/liquidity-mode", liquidity?.LiquidityMode);
        AppendValue(signature, key + "/liquidity-power", liquidity?.ObservedPurchasingPower);
        AppendValue(signature, key + "/liquidity-observed-day", liquidity?.ObservedDay);
        AppendValue(signature, key + "/liquidity-received-day", liquidity?.ReceivedDay);
        AppendValue(signature, key + "/liquidity-freshness", liquidity?.Freshness);
        AppendValue(signature, key + "/liquidity-source", liquidity?.Source);
        AppendValue(signature, key + "/liquidity-source-runtime", liquidity?.SourceRuntimeId);
        AppendValue(signature, key + "/quantity", evidence.ExpectedQuantity);
        AppendValue(signature, key + "/purchase-price", evidence.ExpectedPurchaseUnitPrice);
        AppendValue(signature, key + "/sale-price", evidence.ExpectedSaleUnitPrice);
        AppendValue(signature, key + "/travel-cost", evidence.ExpectedTravelCost);
        AppendValue(signature, key + "/gross-profit", evidence.ExpectedGrossProfit);
        AppendValue(signature, key + "/net-profit", evidence.ExpectedNetProfit);
        AppendValue(signature, key + "/score", evidence.ExpectedScore);
    }

    private static void AppendCommercialObservationEvidence(
        StringBuilder signature,
        string key,
        CommercialObservationEvidence observation)
    {
        AppendValue(signature, key + "/location", observation?.LocationRuntimeId);
        AppendValue(signature, key + "/price", observation?.KnownPrice);
        AppendValue(signature, key + "/stock", observation?.KnownStock);
        AppendValue(signature, key + "/observed-day", observation?.ObservedDay);
        AppendValue(signature, key + "/received-day", observation?.ReceivedDay);
        AppendValue(signature, key + "/freshness", observation?.Freshness);
        AppendValue(signature, key + "/source", observation?.Source);
        AppendValue(signature, key + "/source-runtime", observation?.SourceRuntimeId);
    }

    private static void AppendNpcMerchantPlan(StringBuilder signature, string key, P12DNpcMerchantPlanValue plan)
    {
        AppendValue(signature, key + "/item", plan?.ItemDefinitionId);
        AppendValue(signature, key + "/origin-city", plan?.OriginCityRuntimeId);
        AppendValue(signature, key + "/target-city", plan?.TargetCityRuntimeId);
        AppendValue(signature, key + "/planned-amount", plan?.PlannedAmount);
        AppendValue(signature, key + "/raw-remaining", plan?.RawRemainingAmount);
        AppendValue(signature, key + "/purchase-price", plan?.PurchasePricePerItem);
        AppendValue(signature, key + "/wait-days", plan?.WaitDaysAtDestination);
        AppendValue(signature, key + "/pending-travel-days", plan?.PendingTravelDays);
        AppendValue(signature, key + "/origin-decision", plan?.OriginDecisionId);
        AppendValue(signature, key + "/revision", plan?.Revision);
    }

    private static void AppendNpcTravelPlan(StringBuilder signature, string key, P12DNpcTravelPlanValue plan)
    {
        AppendValue(signature, key + "/target-location", plan?.TargetLocationRuntimeId);
        AppendValue(signature, key + "/target-city", plan?.TargetCityRuntimeId);
        AppendValue(signature, key + "/reason", plan?.Reason);
        AppendValue(signature, key + "/utility", plan?.Utility);
        AppendValue(signature, key + "/expected-cost", plan?.ExpectedCost);
        AppendValue(signature, key + "/origin-decision", plan?.OriginDecisionId);
        AppendValue(signature, key + "/revision", plan?.Revision);
    }

    private static void AppendAdventureObservations(StringBuilder signature, string key, P12DNpcFRow row)
    {
        int index = 0;
        foreach (AdventureOppositionObservation observation in row.OppositionObservations)
        {
            string entry = key + "/opposition/" + index++;
            AppendAdventureObservationBase(signature, entry, observation);
            AppendValue(signature, entry + "/opposition-id", observation.OppositionRuntimeId);
            AppendValue(signature, entry + "/state", observation.ObservedState);
        }
        index = 0;
        foreach (AdventureNotableItemObservation observation in row.NotableItemObservations)
        {
            string entry = key + "/notable-item/" + index++;
            AppendAdventureObservationBase(signature, entry, observation);
            AppendValue(signature, entry + "/item-runtime-id", observation.NotableItemRuntimeId);
            AppendValue(signature, entry + "/item-definition-id", observation.ItemDefinitionId);
        }
        index = 0;
        foreach (AdventureCommonResourceObservation observation in row.CommonResourceObservations)
        {
            string entry = key + "/common-resource/" + index++;
            AppendAdventureObservationBase(signature, entry, observation);
            AppendValue(signature, entry + "/item-definition-id", observation.ItemDefinitionId);
            AppendValue(signature, entry + "/amount", observation.ObservedAmount);
            AppendValue(signature, entry + "/category", observation.ResourceCategory);
        }
        index = 0;
        foreach (AdventureAccessObservation observation in row.AccessObservations)
        {
            string entry = key + "/access/" + index++;
            AppendAdventureObservationBase(signature, entry, observation);
            AppendValue(signature, entry + "/access-state", observation.ObservedAccessState);
        }
    }

    private static void AppendAdventureObservationBase(
        StringBuilder signature,
        string key,
        AdventureSiteIntelObservation observation)
    {
        AppendValue(signature, key + "/site", observation.SiteRuntimeId);
        AppendValue(signature, key + "/local-place", observation.LocalPlaceRuntimeId);
        AppendValue(signature, key + "/observed-day", observation.ObservedDay);
        AppendValue(signature, key + "/received-day", observation.ReceivedDay);
        AppendValue(signature, key + "/source", observation.Source);
        AppendValue(signature, key + "/source-runtime", observation.SourceRuntimeId);
    }

    private static void AppendCommercialFacts(StringBuilder signature, string key, P12DNpcFRow row)
    {
        int index = 0;
        foreach (P12DNpcCommercialMarketValue observation in row.CommercialMarkets)
        {
            string entry = key + "/commercial-market/" + index++;
            AppendValue(signature, entry + "/location", observation.LocationRuntimeId);
            AppendValue(signature, entry + "/item", observation.ItemDefinitionId);
            AppendValue(signature, entry + "/price", observation.ObservedPrice);
            AppendValue(signature, entry + "/stock", observation.ObservedStock);
            AppendValue(signature, entry + "/observed-day", observation.ObservedDay);
            AppendValue(signature, entry + "/received-day", observation.ReceivedDay);
            AppendValue(signature, entry + "/source", observation.Source);
            AppendValue(signature, entry + "/source-runtime", observation.SourceRuntimeId);
        }
        index = 0;
        foreach (P12DNpcCommercialLiquidityValue observation in row.CommercialLiquidity)
        {
            string entry = key + "/commercial-liquidity/" + index++;
            AppendValue(signature, entry + "/location", observation.LocationRuntimeId);
            AppendValue(signature, entry + "/mode", observation.Mode);
            AppendValue(signature, entry + "/power", observation.PurchasingPower);
            AppendValue(signature, entry + "/observed-day", observation.ObservedDay);
            AppendValue(signature, entry + "/received-day", observation.ReceivedDay);
            AppendValue(signature, entry + "/source", observation.Source);
            AppendValue(signature, entry + "/source-runtime", observation.SourceRuntimeId);
        }
        index = 0;
        foreach (P12DNpcCommercialShareReceiptValue receipt in row.CommercialShareReceipts)
        {
            string entry = key + "/commercial-share-receipt/" + index++;
            AppendValue(signature, entry + "/operation", receipt.OperationIdentity);
            AppendValue(signature, entry + "/fingerprint", receipt.Fingerprint);
            AppendValue(signature, entry + "/boundary", receipt.BoundaryOccurrenceId);
            AppendValue(signature, entry + "/sender-runtime", receipt.SenderRuntimeId);
            AppendValue(signature, entry + "/sender-person", receipt.SenderPersonId);
            AppendValue(signature, entry + "/receiver-runtime", receipt.ReceiverRuntimeId);
            AppendValue(signature, entry + "/receiver-person", receipt.ReceiverPersonId);
            AppendValue(signature, entry + "/source-snapshot", receipt.SourceSnapshotFingerprint);
            AppendValue(signature, entry + "/applied-count", receipt.AppliedObservationCount);
        }
    }

    private static void AppendPoliticalKnowledgeRuntime(
        StringBuilder signature,
        string key,
        PoliticalKnowledgeRuntime runtime)
    {
        PoliticalKnowledgeHolder holder = runtime.Holder;
        AppendValue(signature, key + "/holder-kind", holder.Kind);
        AppendValue(signature, key + "/holder-id", holder.RawStableId);
        int index = 0;
        foreach (PoliticalKnowledgeObservation observation in runtime.Observations)
        {
            string entry = key + "/observation/" + index++;
            AppendValue(signature, entry + "/kind", observation.FactKind);
            AppendValue(signature, entry + "/observed-day", observation.ObservedAbsoluteDay);
            AppendValue(signature, entry + "/received-day", observation.ReceivedAbsoluteDay);
            AppendValue(signature, entry + "/source", observation.Provenance.Source);
            AppendValue(signature, entry + "/source-reference", observation.Provenance.SourceReference);
            AppendValue(signature, entry + "/source-person", observation.Provenance.SourcePersonId?.Value);
            AppendValue(signature, entry + "/source-institution", observation.Provenance.SourceInstitutionId?.Value);
            if (observation is PoliticalClaimKnowledgeObservation claim)
            {
                AppendValue(signature, entry + "/claim-id", claim.ClaimId.Value);
                AppendValue(signature, entry + "/exists", claim.Exists);
                AppendValue(signature, entry + "/claimant", claim.ClaimantPersonId?.Value);
                AppendValue(signature, entry + "/claim-type", claim.ClaimType);
                AppendValue(signature, entry + "/target-kind", claim.Target?.Kind);
                AppendValue(signature, entry + "/target-id", claim.Target?.TargetId);
                AppendValue(signature, entry + "/basis", claim.Basis);
                AppendValue(signature, entry + "/created-day", claim.CreatedAbsoluteDay);
                AppendValue(signature, entry + "/status", claim.Status);
                AppendValue(signature, entry + "/resolution-day", claim.ResolutionAbsoluteDay);
                AppendValue(signature, entry + "/recognition-state", claim.RecognitionState);
                AppendValue(signature, entry + "/recognizing-institution", claim.RecognizingInstitutionId?.Value);
                AppendValue(signature, entry + "/recognition-day", claim.RecognitionAbsoluteDay);
            }
            else if (observation is FactionKnowledgeObservation faction)
            {
                AppendValue(signature, entry + "/faction-id", faction.FactionId.Value);
                AppendValue(signature, entry + "/exists", faction.Exists);
            }
            else if (observation is FactionAffiliationKnowledgeObservation affiliation)
            {
                AppendValue(signature, entry + "/faction-id", affiliation.FactionId.Value);
                AppendValue(signature, entry + "/person-id", affiliation.PersonId.Value);
                AppendValue(signature, entry + "/is-active", affiliation.IsActive);
            }
            else if (observation is OfficeVacancyKnowledgeObservation vacancy)
            {
                AppendValue(signature, entry + "/office-id", vacancy.OfficeId.Value);
                AppendValue(signature, entry + "/institution-id", vacancy.InstitutionId.Value);
                AppendValue(signature, entry + "/is-vacant", vacancy.IsVacant);
                AppendValue(signature, entry + "/recognized-vacant", vacancy.IsRecognizedVacant);
            }
            else if (observation is PersonDeathKnowledgeObservation death)
            {
                AppendValue(signature, entry + "/person-id", death.PersonId.Value);
                AppendValue(signature, entry + "/is-dead", death.IsDead);
                AppendValue(signature, entry + "/death-day", death.DeathAbsoluteDay);
            }
            else
            {
                Assert.Fail("An included PoliticalKnowledge observation has no typed parity case.");
            }
        }
    }

    private static void AppendInstitutionOfficeSnapshot(
        StringBuilder signature,
        P12EInstitutionOfficeOwnerSnapshot snapshot)
    {
        AppendInstitutionRows(signature, "E/institutions", snapshot.Institutions,
            (key, row) =>
            {
                AppendValue(signature, key + "/id", row.InstitutionIdValue);
                AppendValue(signature, key + "/name", row.DisplayName);
            });
        AppendInstitutionRows(signature, "E/offices", snapshot.Offices,
            (key, row) =>
            {
                AppendValue(signature, key + "/id", row.OfficeIdValue);
                AppendValue(signature, key + "/institution", row.InstitutionIdValue);
                AppendValue(signature, key + "/name", row.DisplayName);
            });
        AppendInstitutionRows(signature, "E/incumbencies", snapshot.Incumbencies,
            (key, row) =>
            {
                AppendValue(signature, key + "/office", row.OfficeIdValue);
                AppendValue(signature, key + "/person", row.PersonIdValue);
                AppendValue(signature, key + "/start-day", row.StartAbsoluteDay);
            });
        AppendInstitutionRows(signature, "E/tenures", snapshot.Tenures,
            (key, row) =>
            {
                AppendValue(signature, key + "/office", row.OfficeIdValue);
                AppendValue(signature, key + "/person", row.PersonIdValue);
                AppendValue(signature, key + "/start-day", row.StartAbsoluteDay);
                AppendValue(signature, key + "/end-day", row.EndAbsoluteDay);
                AppendValue(signature, key + "/end-reason", row.EndReason);
                AppendValue(signature, key + "/closed", row.IsClosed);
            });
    }

    private static void AppendInstitutionRows<T>(
        StringBuilder signature,
        string key,
        P12EInstitutionOfficeSnapshotSection<T> section,
        Action<string, T> appendRow)
    {
        AppendValue(signature, key + "/section-id", section.SectionId);
        AppendValue(signature, key + "/schema", section.SchemaVersion);
        AppendValue(signature, key + "/count", section.RecordCount);
        AppendValue(signature, key + "/revision", section.Revision);
        int index = 0;
        foreach (T row in section.Records) appendRow(key + "/row/" + index++, row);
    }

    private static void AppendPropertyEstateSnapshot(StringBuilder signature, PropertyEstateOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/property/schema", snapshot.OwnershipSchemaVersion);
        AppendValue(signature, "E/property/count", snapshot.OwnershipRecordCount);
        AppendValue(signature, "E/property/revision", snapshot.PropertyRevision);
        int index = 0;
        foreach (PropertyEstateOwnershipRow row in snapshot.OwnershipRows)
        {
            string key = "E/property/row/" + index++;
            AppendValue(signature, key + "/property-id", row.PropertyIdValue);
            AppendValue(signature, key + "/owner-person", row.OwnerPersonIdValue);
        }
        AppendValue(signature, "E/property-transfer/schema", snapshot.TransferHistorySchemaVersion);
        AppendValue(signature, "E/property-transfer/count", snapshot.TransferHistoryRecordCount);
        AppendValue(signature, "E/property-transfer/revision", snapshot.TransferHistoryRevision);
        index = 0;
        foreach (PropertyEstateTransferHistoryRow row in snapshot.TransferHistoryRows)
        {
            string key = "E/property-transfer/row/" + index++;
            AppendValue(signature, key + "/property-id", row.PropertyIdValue);
            AppendValue(signature, key + "/previous-owner", row.PreviousOwnerPersonIdValue);
            AppendValue(signature, key + "/new-owner", row.NewOwnerPersonIdValue);
            AppendValue(signature, key + "/transfer-day", row.TransferAbsoluteDay);
        }
        AppendValue(signature, "E/estate/schema", snapshot.EstateSchemaVersion);
        AppendValue(signature, "E/estate/count", snapshot.EstateRecordCount);
        AppendValue(signature, "E/estate/revision", snapshot.EstateRevision);
        index = 0;
        foreach (PropertyEstateEstateRow row in snapshot.EstateRows)
        {
            string key = "E/estate/row/" + index++;
            AppendValue(signature, key + "/estate-id", row.EstateIdValue);
            AppendValue(signature, key + "/deceased-person", row.DeceasedPersonIdValue);
            AppendValue(signature, key + "/opened-day", row.OpenedAbsoluteDay);
        }
    }

    private static void AppendFactionSnapshot(StringBuilder signature, P12EFactionOwnerSnapshot snapshot)
    {
        AppendFactionRows(signature, "E/factions", snapshot.Factions,
            (key, row) =>
            {
                AppendValue(signature, key + "/id", row.Id);
                AppendValue(signature, key + "/name", row.DisplayName);
                AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
                AppendValue(signature, key + "/membership-policy", row.MembershipPolicy);
                AppendValue(signature, key + "/expulsion-allowed", row.ExpulsionAllowed);
            });
        AppendFactionRows(signature, "E/faction-affiliations", snapshot.Affiliations,
            (key, row) =>
            {
                AppendValue(signature, key + "/affiliation-id", row.AffiliationId);
                AppendValue(signature, key + "/faction-id", row.FactionId);
                AppendValue(signature, key + "/person-id", row.PersonId);
                AppendValue(signature, key + "/joined-day", row.JoinedAbsoluteDay);
                AppendValue(signature, key + "/ended-day", row.EndedAbsoluteDay);
                AppendValue(signature, key + "/end-reason", row.EndReason);
                AppendValue(signature, key + "/active", row.IsActive);
            });
    }

    private static void AppendFactionRows<T>(
        StringBuilder signature,
        string key,
        P12EFactionSnapshotSection<T> section,
        Action<string, T> appendRow)
    {
        AppendValue(signature, key + "/section-id", section.SectionId);
        AppendValue(signature, key + "/schema", section.SchemaVersion);
        AppendValue(signature, key + "/count", section.RecordCount);
        AppendValue(signature, key + "/revision", section.Revision);
        int index = 0;
        foreach (T row in section.Records) appendRow(key + "/row/" + index++, row);
    }

    private static void AppendClaimSnapshot(StringBuilder signature, P12EPoliticalClaimOwnerSnapshot snapshot)
    {
        AppendClaimRows(signature, "E/claims", snapshot.Claims,
            (key, row) =>
            {
                AppendValue(signature, key + "/claim-id", row.ClaimId);
                AppendValue(signature, key + "/claimant", row.ClaimantPersonId);
                AppendValue(signature, key + "/claim-type", row.ClaimType);
                AppendValue(signature, key + "/target-kind", row.TargetKind);
                AppendValue(signature, key + "/target-id", row.TargetId);
                AppendValue(signature, key + "/basis", row.Basis);
                AppendValue(signature, key + "/basis-description", row.BasisDescription);
                AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
                AppendValue(signature, key + "/status", row.Status);
                AppendValue(signature, key + "/resolution-day", row.ResolutionAbsoluteDay);
                AppendValues(signature, key + "/evidence", row.EvidenceReferences);
            });
        AppendClaimRows(signature, "E/claim-recognitions", snapshot.Recognitions,
            (key, row) =>
            {
                AppendValue(signature, key + "/claim-id", row.ClaimId);
                AppendValue(signature, key + "/institution-id", row.InstitutionId);
                AppendValue(signature, key + "/recognition-id", row.RecognitionId);
                AppendValue(signature, key + "/state", row.State);
                AppendValue(signature, key + "/recognition-day", row.RecognitionAbsoluteDay);
                AppendValue(signature, key + "/reason", row.Reason);
                int historyIndex = 0;
                foreach (P12EPoliticalClaimRecognitionHistoryRow history in row.History)
                {
                    string historyKey = key + "/history/" + historyIndex++;
                    AppendValue(signature, historyKey + "/state", history.State);
                    AppendValue(signature, historyKey + "/day", history.Day);
                    AppendValue(signature, historyKey + "/reason", history.Reason);
                }
            });
    }

    private static void AppendClaimRows<T>(
        StringBuilder signature,
        string key,
        P12EPoliticalClaimSnapshotSection<T> section,
        Action<string, T> appendRow)
    {
        AppendValue(signature, key + "/section-id", section.SectionId);
        AppendValue(signature, key + "/schema", section.SchemaVersion);
        AppendValue(signature, key + "/count", section.RecordCount);
        AppendValue(signature, key + "/revision", section.Revision);
        int index = 0;
        foreach (T row in section.Records) appendRow(key + "/row/" + index++, row);
    }

    private static void AppendSupportSnapshot(StringBuilder signature, P12EPoliticalSupportOwnerSnapshot snapshot)
    {
        P12EPoliticalSupportSnapshotSection section = snapshot.Relations;
        AppendValue(signature, "E/support/captured-day", snapshot.CapturedAbsoluteDay);
        AppendValue(signature, "E/support/section-id", section.SectionId);
        AppendValue(signature, "E/support/schema", section.SchemaVersion);
        AppendValue(signature, "E/support/count", section.RecordCount);
        AppendValue(signature, "E/support/revision", section.Revision);
        int index = 0;
        foreach (P12EPoliticalSupportSnapshotRow row in section.Records)
        {
            string key = "E/support/row/" + index++;
            AppendValue(signature, key + "/relation-id", row.RelationId);
            AppendValue(signature, key + "/source-kind", row.SourceKind);
            AppendValue(signature, key + "/source-id", row.SourceId);
            AppendValue(signature, key + "/target-kind", row.TargetKind);
            AppendValue(signature, key + "/target-id", row.TargetId);
            AppendValue(signature, key + "/disposition", row.Disposition);
            AppendValue(signature, key + "/start-day", row.StartedAbsoluteDay);
            AppendValue(signature, key + "/end-day", row.EndedAbsoluteDay);
        }
    }

    private static void AppendMilitarySnapshot(StringBuilder signature, P12EMilitaryOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/military/schema", snapshot.SchemaVersion);
        AppendValue(signature, "E/military/force-revision", snapshot.ArmedForceRevision);
        AppendValue(signature, "E/military/manpower-revision", snapshot.ManpowerRevision);
        AppendValue(signature, "E/military/position-revision", snapshot.PositionRevision);
        AppendValue(signature, "E/military/force-count", snapshot.ForceCount);
        AppendValue(signature, "E/military/contingent-count", snapshot.ContingentCount);
        AppendValue(signature, "E/military/relevant-person-count", snapshot.RelevantPersonCount);
        AppendValue(signature, "E/military/manpower-state-count", snapshot.ManpowerStateCount);
        AppendValue(signature, "E/military/position-count", snapshot.PositionCount);
        int index = 0;
        foreach (P12EMilitaryForceSnapshotRecord row in snapshot.Forces)
        {
            string key = "E/military/force/" + index++;
            AppendValue(signature, key + "/id", row.ForceIdValue);
            AppendValue(signature, key + "/name", row.DisplayName);
            AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
            AppendValue(signature, key + "/parent-force", row.ParentForceIdValue);
            AppendValue(signature, key + "/operational-location", row.OperationalLocationReference);
            AppendValue(signature, key + "/commander-person", row.CommanderPersonIdValue);
            AppendValue(signature, key + "/lifecycle", row.LifecycleState);
            AppendValue(signature, key + "/terminated-day", row.TerminatedAbsoluteDay);
            AppendValue(signature, key + "/detached", row.IsDetached);
        }
        index = 0;
        foreach (P12EMilitaryContingentSnapshotRecord row in snapshot.Contingents)
        {
            string key = "E/military/contingent/" + index++;
            AppendValue(signature, key + "/id", row.ContingentIdValue);
            AppendValue(signature, key + "/force", row.ForceIdValue);
            AppendValue(signature, key + "/amount", row.Amount);
            AppendValue(signature, key + "/origin-domain", row.OriginDomain);
            AppendValue(signature, key + "/origin-value", row.OriginValue);
            AppendValue(signature, key + "/service-type", row.ServiceType);
            int characteristicIndex = 0;
            foreach (P12EMilitaryCharacteristicSnapshot characteristic in row.Characteristics)
            {
                string characteristicKey = key + "/characteristic/" + characteristicIndex++;
                AppendValue(signature, characteristicKey + "/key", characteristic.Key);
                AppendValue(signature, characteristicKey + "/value", characteristic.Value);
            }
        }
        index = 0;
        foreach (P12EMilitaryRelevantPersonSnapshotRecord row in snapshot.RelevantPersons)
        {
            string key = "E/military/relevant-person/" + index++;
            AppendValue(signature, key + "/reference", row.ReferenceIdValue);
            AppendValue(signature, key + "/force", row.ForceIdValue);
            AppendValue(signature, key + "/person", row.PersonIdValue);
            AppendValue(signature, key + "/role", row.RoleKey);
        }
        index = 0;
        foreach (P12EManpowerStateSnapshotRecord row in snapshot.ManpowerStates)
        {
            string key = "E/military/manpower-state/" + index++;
            AppendValue(signature, key + "/contingent", row.ContingentIdValue);
            AppendValue(signature, key + "/source", row.SourceIdValue);
            AppendValue(signature, key + "/revision", row.Revision);
            int cohortIndex = 0;
            foreach (P12EManpowerCohortSnapshotRecord cohort in row.Cohorts)
            {
                string cohortKey = key + "/cohort/" + cohortIndex++;
                AppendValue(signature, cohortKey + "/injury", cohort.InjuryState);
                AppendValue(signature, cohortKey + "/custody", cohort.CustodyState);
                AppendValue(signature, cohortKey + "/custodian-force", cohort.CustodianForceIdValue);
                AppendValue(signature, cohortKey + "/availability", cohort.AvailabilityState);
                AppendValue(signature, cohortKey + "/amount", cohort.Amount);
            }
        }
        index = 0;
        foreach (P12EForcePositionSnapshotRecord row in snapshot.Positions)
        {
            string key = "E/military/position/" + index++;
            AppendValue(signature, key + "/force", row.ForceIdValue);
            AppendSpatialReference(signature, key + "/reference", row.Reference);
        }
    }

    private static void AppendSpatialReference(
        StringBuilder signature,
        string key,
        P12ESpatialReferenceSnapshot reference)
    {
        AppendValue(signature, key + "/kind", reference?.Kind);
        AppendValue(signature, key + "/hex", reference?.HexIdValue);
        AppendValue(signature, key + "/location", reference?.LocationIdValue);
        AppendValue(signature, key + "/crossing", reference?.CrossingIdValue);
        AppendValue(signature, key + "/topology-owner-kind", reference?.TopologyOwnerKind);
        AppendValue(signature, key + "/topology-owner-runtime", reference?.TopologyOwnerRuntimeId);
        AppendValue(signature, key + "/sublocation", reference?.SubLocationRuntimeId);
    }

    private static void AppendConflictSnapshot(StringBuilder signature, PersistentConflictOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/conflicts/schema", snapshot.SchemaVersion);
        AppendValue(signature, "E/conflicts/count", snapshot.RecordCount);
        AppendValue(signature, "E/conflicts/revision", snapshot.Revision);
        int index = 0;
        foreach (PersistentConflictOwnerSnapshotRecord row in snapshot.Records)
        {
            string key = "E/conflicts/row/" + index++;
            AppendValue(signature, key + "/id", row.ConflictIdValue);
            AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
            AppendValue(signature, key + "/lifecycle", row.LifecycleState);
            AppendValue(signature, key + "/ended-day", row.EndedAbsoluteDay);
            int sideIndex = 0;
            foreach (PersistentConflictOwnerSideSnapshot side in row.Sides)
            {
                string sideKey = key + "/side/" + sideIndex++;
                AppendValue(signature, sideKey + "/conflict", side.ConflictIdValue);
                AppendValue(signature, sideKey + "/id", side.SideIdValue);
                AppendValue(signature, sideKey + "/name", side.DisplayName);
            }
            int bindingIndex = 0;
            foreach (PersistentConflictOwnerBindingSnapshot binding in row.ParticipantBindings)
            {
                string bindingKey = key + "/binding/" + bindingIndex++;
                AppendValue(signature, bindingKey + "/id", binding.BindingIdValue);
                AppendValue(signature, bindingKey + "/conflict", binding.ConflictIdValue);
                AppendValue(signature, bindingKey + "/side", binding.SideIdValue);
                AppendValue(signature, bindingKey + "/force", binding.ArmedForceIdValue);
            }
        }
    }

    private static void AppendWarSnapshot(StringBuilder signature, PersistentWarOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/wars/schema", snapshot.SchemaVersion);
        AppendValue(signature, "E/wars/count", snapshot.RecordCount);
        AppendValue(signature, "E/wars/revision", snapshot.Revision);
        int index = 0;
        foreach (PersistentWarOwnerSnapshotRecord row in snapshot.Records)
        {
            string key = "E/wars/row/" + index++;
            AppendValue(signature, key + "/id", row.WarIdValue);
            AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
            AppendValue(signature, key + "/lifecycle", row.LifecycleState);
            AppendValue(signature, key + "/ended-day", row.EndedAbsoluteDay);
            AppendValue(signature, key + "/conflict", row.ConflictIdValue);
            int sideIndex = 0;
            foreach (PersistentWarOwnerSideSnapshot side in row.Sides)
            {
                string sideKey = key + "/side/" + sideIndex++;
                AppendValue(signature, sideKey + "/war", side.WarIdValue);
                AppendValue(signature, sideKey + "/id", side.SideIdValue);
                AppendValue(signature, sideKey + "/name", side.DisplayName);
            }
            int bindingIndex = 0;
            foreach (PersistentWarOwnerBindingSnapshot binding in row.ParticipantBindings)
            {
                string bindingKey = key + "/binding/" + bindingIndex++;
                AppendValue(signature, bindingKey + "/id", binding.BindingIdValue);
                AppendValue(signature, bindingKey + "/war", binding.WarIdValue);
                AppendValue(signature, bindingKey + "/side", binding.SideIdValue);
                AppendValue(signature, bindingKey + "/force", binding.ArmedForceIdValue);
            }
        }
    }

    private static void AppendBattleSnapshot(StringBuilder signature, PersistentBattleOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/battles/schema", snapshot.SchemaVersion);
        AppendValue(signature, "E/battles/count", snapshot.RecordCount);
        AppendValue(signature, "E/battles/revision", snapshot.Revision);
        int index = 0;
        foreach (PersistentBattleOwnerSnapshotRecord row in snapshot.Records)
        {
            string key = "E/battles/row/" + index++;
            AppendValue(signature, key + "/id", row.BattleIdValue);
            AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
            AppendValue(signature, key + "/started-day", row.StartedAbsoluteDay);
            AppendValue(signature, key + "/lifecycle", row.LifecycleState);
            AppendValue(signature, key + "/conflict", row.ConflictIdValue);
            AppendValue(signature, key + "/war", row.WarIdValue);
            AppendBattleSpatialReference(signature, key + "/location", row.LocationReference);
            int sideIndex = 0;
            foreach (PersistentBattleOwnerSideSnapshot side in row.Sides)
            {
                string sideKey = key + "/side/" + sideIndex++;
                AppendValue(signature, sideKey + "/battle", side.BattleIdValue);
                AppendValue(signature, sideKey + "/id", side.SideIdValue);
                AppendValue(signature, sideKey + "/name", side.DisplayName);
            }
            int bindingIndex = 0;
            foreach (PersistentBattleOwnerBindingSnapshot binding in row.ParticipantBindings)
            {
                string bindingKey = key + "/binding/" + bindingIndex++;
                AppendValue(signature, bindingKey + "/id", binding.BindingIdValue);
                AppendValue(signature, bindingKey + "/battle", binding.BattleIdValue);
                AppendValue(signature, bindingKey + "/side", binding.SideIdValue);
                AppendValue(signature, bindingKey + "/force", binding.ArmedForceIdValue);
            }
            PersistentBattleOwnerOutcomeSnapshot outcome = row.TerminalOutcome;
            AppendValue(signature, key + "/outcome-battle", outcome?.BattleIdValue);
            AppendValue(signature, key + "/outcome-type", outcome?.OutcomeType);
            AppendValue(signature, key + "/outcome-winning-side", outcome?.WinningBattleSideIdValue);
            AppendValue(signature, key + "/outcome-day", outcome?.ResolvedAbsoluteDay);
            AppendBattleProvenance(signature, key + "/outcome-provenance", outcome?.Provenance);
        }
    }

    private static void AppendBattleSpatialReference(
        StringBuilder signature,
        string key,
        PersistentBattleOwnerSpatialReferenceSnapshot reference)
    {
        AppendValue(signature, key + "/kind", reference?.Kind);
        AppendValue(signature, key + "/hex", reference?.HexIdValue);
        AppendValue(signature, key + "/location", reference?.LocationIdValue);
        AppendValue(signature, key + "/crossing", reference?.CrossingIdValue);
        AppendValue(signature, key + "/topology-owner-kind", reference?.TopologyOwnerKind);
        AppendValue(signature, key + "/topology-owner-runtime", reference?.TopologyOwnerRuntimeId);
        AppendValue(signature, key + "/sublocation", reference?.SubLocationRuntimeId);
    }

    private static void AppendBattleProvenance(
        StringBuilder signature,
        string key,
        PersistentBattleOwnerProvenanceSnapshot provenance)
    {
        AppendValue(signature, key + "/policy", provenance?.PolicyFingerprint);
        AppendValue(signature, key + "/numeric-profile", provenance?.NumericExecutionProfileKey);
        AppendValue(signature, key + "/projection-version", provenance?.ProjectionVersion);
        AppendValue(signature, key + "/causal-fingerprint", provenance?.CausalResolutionFingerprint);
        AppendValue(signature, key + "/source-context", provenance?.SourceContextFingerprint);
        AppendValue(signature, key + "/capability-rule", provenance?.CapabilityRuleKey);
        AppendValue(signature, key + "/random-authority-rule", provenance?.RandomAuthorityRuleKey);
        AppendValue(signature, key + "/resolver-settings", provenance?.ResolverSettingsIdentity);
        AppendValue(signature, key + "/d6b2-policy", provenance?.D6B2PolicyFingerprint);
        AppendValue(signature, key + "/d6b2-plan-schema", provenance?.D6B2PlanSchemaVersion);
        AppendValue(signature, key + "/d6b2-coverage", provenance?.D6B2CoverageVersion);
        AppendValue(signature, key + "/d6b2-plan", provenance?.D6B2PlanFingerprint);
    }

    private static void AppendJusticeSnapshot(StringBuilder signature, P12EJusticeRecordsOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/justice/captured-day", snapshot.CapturedAbsoluteDay);
        P12EJusticeRecordsSnapshotSection section = snapshot.Records;
        AppendValue(signature, "E/justice/section-id", section.SectionId);
        AppendValue(signature, "E/justice/schema", section.SchemaVersion);
        AppendValue(signature, "E/justice/count", section.RecordCount);
        AppendValue(signature, "E/justice/revision", section.Revision);
        int index = 0;
        foreach (P12EJusticeWantedSnapshotRow row in section.WantedRows)
        {
            string key = "E/justice/wanted/" + index++;
            AppendValue(signature, key + "/ordinal", row.Ordinal);
            AppendValue(signature, key + "/target-runtime", row.TargetRuntimeId);
            AppendValue(signature, key + "/target-person", row.TargetPersonIdValue);
            AppendValue(signature, key + "/city", row.CityRuntimeId);
            AppendValue(signature, key + "/bounty", row.Bounty);
            AppendValue(signature, key + "/sentence-days", row.SentenceDays);
            AppendValue(signature, key + "/resolved", row.IsResolved);
        }
        index = 0;
        foreach (P12EJusticeSentenceSnapshotRow row in section.SentenceRows)
        {
            string key = "E/justice/sentence/" + index++;
            AppendValue(signature, key + "/ordinal", row.Ordinal);
            AppendValue(signature, key + "/target-runtime", row.TargetRuntimeId);
            AppendValue(signature, key + "/target-person", row.TargetPersonIdValue);
            AppendValue(signature, key + "/city", row.CityRuntimeId);
            AppendValue(signature, key + "/warrant-ordinal", row.WarrantOrdinal);
            AppendValue(signature, key + "/remaining-days", row.RemainingDays);
            AppendValue(signature, key + "/failed-escapes", row.FailedEscapeAttempts);
            AppendValue(signature, key + "/arrested-today", row.WasArrestedToday);
        }
    }

    private static void AppendCrimeSocialSnapshot(
        StringBuilder signature,
        P12ECrimeSocialAppraisalOwnerSnapshot snapshot)
    {
        AppendValue(signature, "E/crime-social/captured-day", snapshot.CapturedAbsoluteDay);
        AppendCrimeRows(signature, "E/crime-social/outcomes", snapshot.Outcomes,
            (key, row) =>
            {
                AppendValue(signature, key + "/id", row.OutcomeId);
                AppendValue(signature, key + "/perpetrator", row.PerpetratorPersonId);
                AppendValue(signature, key + "/victim", row.VictimPersonId);
                AppendValue(signature, key + "/loss", row.LossAmount);
                AppendValue(signature, key + "/occurred-day", row.OccurredAbsoluteDay);
                AppendValue(signature, key + "/occurrence-key", row.OccurrenceKey);
                AppendValue(signature, key + "/origin-decision", row.OriginDecisionId);
            });
        AppendCrimeRows(signature, "E/crime-social/knowledge", snapshot.Knowledge,
            (key, row) =>
            {
                AppendValue(signature, key + "/evaluator", row.EvaluatorPersonId);
                AppendValue(signature, key + "/outcome", row.OutcomeId);
                AppendValue(signature, key + "/role", row.Role);
                AppendValue(signature, key + "/knows-loss", row.KnowsLoss);
                AppendValue(signature, key + "/attribution-kind", row.AttributionKind);
                AppendValue(signature, key + "/attribution-person", row.AttributionPersonId);
                AppendValue(signature, key + "/attribution-institution", row.AttributionInstitutionId);
                AppendValue(signature, key + "/known-investigator-person", row.KnownInvestigatorPersonId);
                AppendValue(signature, key + "/known-investigator-institution", row.KnownInvestigatorInstitutionId);
                AppendValue(signature, key + "/basis-kind", row.CognitiveBasisKind);
                AppendValue(signature, key + "/basis-reference", row.CognitiveBasisReference);
                AppendValue(signature, key + "/basis-person", row.CognitiveBasisPersonId);
                AppendValue(signature, key + "/basis-institution", row.CognitiveBasisInstitutionId);
                AppendValue(signature, key + "/observed-day", row.ObservedAbsoluteDay);
            });
        AppendCrimeRows(signature, "E/crime-social/reactions", snapshot.Reactions,
            (key, row) =>
            {
                AppendValue(signature, key + "/id", row.ReactionId);
                AppendValue(signature, key + "/evaluator", row.EvaluatorPersonId);
                AppendValue(signature, key + "/source-domain", row.SourceDomain);
                AppendValue(signature, key + "/source-id", row.SourceStableId);
                AppendValue(signature, key + "/target-kind", row.TargetKind);
                AppendValue(signature, key + "/target-id", row.TargetStableId);
                AppendValue(signature, key + "/attribution-kind", row.AttributionKind);
                AppendValue(signature, key + "/attribution-person", row.AttributionPersonId);
                AppendValue(signature, key + "/attribution-institution", row.AttributionInstitutionId);
                AppendValue(signature, key + "/valence", row.Valence);
                AppendValue(signature, key + "/salience", row.Salience);
                AppendValue(signature, key + "/basis-kind", row.CognitiveBasisKind);
                AppendValue(signature, key + "/basis-reference", row.CognitiveBasisReference);
                AppendValue(signature, key + "/basis-person", row.CognitiveBasisPersonId);
                AppendValue(signature, key + "/basis-institution", row.CognitiveBasisInstitutionId);
                AppendValue(signature, key + "/created-day", row.CreatedAbsoluteDay);
                AppendValue(signature, key + "/supersedes", row.SupersedesReactionId);
            });
    }

    private static void AppendCrimeRows<T>(
        StringBuilder signature,
        string key,
        P12ECrimeOwnerSnapshotSection<T> section,
        Action<string, T> appendRow)
        where T : class
    {
        AppendValue(signature, key + "/section-id", section.SectionId);
        AppendValue(signature, key + "/schema", section.SchemaVersion);
        AppendValue(signature, key + "/count", section.RecordCount);
        AppendValue(signature, key + "/revision", section.Revision);
        int index = 0;
        foreach (T row in section.Records) appendRow(key + "/row/" + index++, row);
    }

    private static void AppendRemainingFSnapshotFacts(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token,
        StringBuilder signature)
    {
        IReadOnlyList<OwnerSectionCensusSnapshot> vector = token.OwnerSections;
        ScheduledDirectiveStore directiveOwner = GetOwner<ScheduledDirectiveStore>(
            vector, ScheduledDirectiveCensusProvider.SectionId);
        Assert.That(P12FScheduledDirectiveOwnerSnapshot.TryCapture(
            session.Composition.ScheduledDirectives, token, vector,
            out P12FScheduledDirectiveOwnerSnapshot directiveSnapshot,
            out string directiveFailure), Is.True, directiveFailure);
        AppendValue(signature, "F/directives/snapshot-cardinality", directiveSnapshot.Cardinality);
        AppendValue(signature, "F/directives/snapshot-revision", directiveSnapshot.Revision);
        Assert.That(directiveOwner.TryCaptureP12FState(
            out IReadOnlyList<ScheduledDirective> directives, out long directiveRevision), Is.True,
            "The scheduled-directive owner must expose a detached typed row read for parity.");
        AppendValue(signature, "F/directives/owner-revision", directiveRevision);
        int directiveIndex = 0;
        foreach (ScheduledDirective row in directives)
        {
            string key = "F/directives/row/" + directiveIndex++;
            AppendValue(signature, key + "/id", row.DirectiveId);
            AppendValue(signature, key + "/absolute-day", row.AbsoluteDay);
            AppendValue(signature, key + "/mode", row.Mode);
            AppendValue(signature, key + "/operation", row.Operation);
            AppendValue(signature, key + "/actor-runtime", row.ActorRuntimeId);
            AppendValue(signature, key + "/action-id", row.ActionDefinitionId);
            AppendValue(signature, key + "/state", row.State);
            AppendValue(signature, key + "/processed-day", row.ProcessedDay);
            AppendValue(signature, key + "/result-reason", row.ResultReason);
        }
        Assert.That(directives.Count, Is.EqualTo(directiveSnapshot.Cardinality));
        Assert.That(directiveRevision, Is.EqualTo(directiveSnapshot.Revision));

        Assert.That(P12FActorChoiceSnapshot.TryCapture(
            session.Runtime.ActorChoiceStore, token, vector,
            out P12FActorChoiceSnapshot actorChoices,
            out P12FActorChoiceSnapshotFailure actorChoiceFailure), Is.True, actorChoiceFailure.ToString());
        AppendValue(signature, "F/actor-choice/schema", actorChoices.SchemaVersion);
        AppendValue(signature, "F/actor-choice/next-input-sequence", actorChoices.NextInputSequence);
        AppendValue(signature, "F/actor-choice/revision", actorChoices.CensusRevision);
        int inputIndex = 0;
        foreach (ActorChoiceInput input in actorChoices.Inputs)
        {
            string key = "F/actor-choice/input/" + inputIndex++;
            AppendValue(signature, key + "/input-id", input.InputId?.Value);
            AppendValue(signature, key + "/world-command-id", input.WorldCommandId);
            AppendValue(signature, key + "/sequence", input.InputSequence);
            AppendValue(signature, key + "/person-id", input.PersonId?.Value);
            AppendValue(signature, key + "/action-id", input.ActionDefinitionId);
            AppendValue(signature, key + "/origin", input.Origin);
            AppendValue(signature, key + "/authority", input.Authority);
            AppendValue(signature, key + "/captured-day", input.CapturedAbsoluteDay);
            AppendValue(signature, key + "/status", input.Status);
            AppendActorChoiceTemporalCapture(signature, key + "/temporal-capture", input.TemporalCapture);
            int dispositionIndex = 0;
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
            {
                string dispositionKey = key + "/disposition/" + dispositionIndex++;
                AppendValue(signature, dispositionKey + "/ordinal", disposition.TransitionOrdinal);
                AppendValue(signature, dispositionKey + "/kind", disposition.Kind);
                AppendValue(signature, dispositionKey + "/absolute-day", disposition.AbsoluteDay);
                AppendValue(signature, dispositionKey + "/roster-ordinal", disposition.ActorTurnRosterOrdinal);
                AppendValue(signature, dispositionKey + "/deferral", disposition.DeferralReason);
                AppendValue(signature, dispositionKey + "/failure", disposition.Failure);
                AppendValue(signature, dispositionKey + "/decision-record", disposition.DecisionRecordId);
                AppendValue(signature, dispositionKey + "/attempt-outcome", disposition.AttemptOutcome);
                AppendValue(signature, dispositionKey + "/result-status", disposition.ReturnedResultStatus);
            }
            int temporalDispositionIndex = 0;
            foreach (ActorChoiceTemporalDisposition disposition in input.TemporalDispositions)
            {
                string dispositionKey = key + "/temporal-disposition/" + temporalDispositionIndex++;
                AppendValue(signature, dispositionKey + "/ordinal", disposition.TransitionOrdinal);
                AppendValue(signature, dispositionKey + "/kind", disposition.Kind);
                AppendActorChoiceBoundary(signature, dispositionKey + "/boundary", disposition.Boundary);
                AppendValue(signature, dispositionKey + "/operation", disposition.OperationId);
                AppendValue(signature, dispositionKey + "/decision-record", disposition.DecisionRecordId);
                AppendValue(signature, dispositionKey + "/failure", disposition.Failure);
                AppendValue(signature, dispositionKey + "/outcome", disposition.AttemptOutcome);
                AppendValue(signature, dispositionKey + "/result-status", disposition.ReturnedResultStatus);
            }
        }

        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(
            session.Composition.TravelParties, token, vector,
            out P12FTravelPartyOwnerSnapshot travelParties,
            out P12FCommitmentSnapshotFailure travelPartyFailure), Is.True, travelPartyFailure.ToString());
        AppendValue(signature, "F/travel-parties/schema", travelParties.SchemaVersion);
        AppendValue(signature, "F/travel-parties/revision", travelParties.Revision);
        int partyIndex = 0;
        foreach (P12FTravelPartyOwnerSnapshotRecord row in travelParties.Parties)
        {
            string key = "F/travel-parties/row/" + partyIndex++;
            AppendValue(signature, key + "/id", row.TravelPartyId);
            AppendValue(signature, key + "/origin", row.OriginLocationRuntimeId);
            AppendValue(signature, key + "/destination", row.DestinationLocationRuntimeId);
            AppendValue(signature, key + "/route", row.RouteRuntimeId);
            AppendValues(signature, key + "/travelers", row.TravelerRuntimeIds);
            AppendValues(signature, key + "/escorts", row.EscortRuntimeIds);
            AppendValues(signature, key + "/members", row.MemberRuntimeIds);
            int costIndex = 0;
            foreach (TravelPartyMemberCost cost in row.MemberCosts)
            {
                string costKey = key + "/member-cost/" + costIndex++;
                AppendValue(signature, costKey + "/runtime-id", cost.RuntimeId);
                AppendValue(signature, costKey + "/amount", cost.Amount);
            }
            AppendValue(signature, key + "/travel-days-total", row.TravelDaysTotal);
            AppendValue(signature, key + "/origin-decision", row.OriginDecisionId);
        }

        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(
            session.Composition.Expeditions, token, vector,
            out P12FExpeditionOwnerSnapshot expeditions,
            out P12FCommitmentSnapshotFailure expeditionFailure), Is.True, expeditionFailure.ToString());
        AppendValue(signature, "F/expeditions/schema", expeditions.SchemaVersion);
        AppendValue(signature, "F/expeditions/revision", expeditions.Revision);
        int expeditionIndex = 0;
        foreach (P12FExpeditionOwnerSnapshotRecord row in expeditions.Expeditions)
        {
            string key = "F/expeditions/row/" + expeditionIndex++;
            AppendValue(signature, key + "/id", row.ExpeditionId);
            AppendValue(signature, key + "/target-site", row.TargetSiteRuntimeId);
            AppendValue(signature, key + "/origin-location", row.OriginLocationRuntimeId);
            AppendValue(signature, key + "/target-location", row.TargetLocationRuntimeId);
            AppendValue(signature, key + "/outbound-route", row.OutboundRouteRuntimeId);
            AppendValue(signature, key + "/travel-party", row.TravelPartyId);
            AppendValue(signature, key + "/origin-decision", row.OriginDecisionId);
            AppendValues(signature, key + "/members", row.MemberRuntimeIds);
            AppendValues(signature, key + "/performers", row.PerformerRuntimeIds);
            AppendValues(signature, key + "/support", row.SupportRuntimeIds);
            AppendValue(signature, key + "/state", row.State);
            AppendValue(signature, key + "/current-local-place", row.CurrentLocalPlaceRuntimeId);
            AppendValues(signature, key + "/visited-places", row.VisitedLocalPlaceRuntimeIds);
            AppendValues(signature, key + "/observed-connections", row.ObservedLocalConnectionRuntimeIds);
            AppendValue(signature, key + "/objective-type", row.ObjectiveType);
            AppendValue(signature, key + "/target-item", row.TargetItemDefinitionId);
            AppendValue(signature, key + "/target-notable-item", row.TargetNotableItemRuntimeId);
            AppendValue(signature, key + "/target-opposition", row.TargetOppositionRuntimeId);
            AppendValue(signature, key + "/required-progress", row.RequiredProgress);
            AppendValue(signature, key + "/progress", row.Progress);
            AppendValue(signature, key + "/objective-completed", row.ObjectiveCompleted);
            AppendValue(signature, key + "/continue-after-completion", row.AllowContinueAfterCompletion);
        }
    }

    private static void AppendActorChoiceTemporalCapture(
        StringBuilder signature,
        string key,
        ActorChoiceTemporalCapture capture)
    {
        AppendValue(signature, key + "/profile", capture?.ProfileId);
        if (capture == null) return;
        AppendValue(signature, key + "/target-tick", capture.TargetInstant.Value);
        AppendTimelineInput(signature, key + "/accepted-input", capture.AcceptedInput);
    }

    private static void AppendActorChoiceBoundary(
        StringBuilder signature,
        string key,
        ActorChoiceTemporalBoundaryReference boundary)
    {
        AppendValue(signature, key + "/profile", boundary?.ProfileId);
        if (boundary == null) return;
        AppendValue(signature, key + "/tick", boundary.Instant.Value);
        AppendValue(signature, key + "/source-receipt", boundary.SourceReceiptId);
        AppendValue(signature, key + "/source-revision", boundary.SourceRevision);
    }

    private static void AppendTimelineInput(
        StringBuilder signature,
        string key,
        TimelineInputReference input)
    {
        AppendValue(signature, key + "/sequence", input?.Sequence);
        AppendValue(signature, key + "/id", input?.InputId);
        AppendValue(signature, key + "/command-kind", input?.CommandKind);
        AppendValue(signature, key + "/command-data", input?.CommandData);
        AppendValue(signature, key + "/target-tick", input?.TargetInstant.Value);
    }

    private static void AppendValues<T>(StringBuilder signature, string name, IEnumerable<T> values)
    {
        int index = 0;
        foreach (T value in values ?? Array.Empty<T>())
            AppendValue(signature, name + "/" + index++, value);
        AppendValue(signature, name + "/count", index);
    }

    private static void AppendValue(StringBuilder signature, string name, object value)
    {
        string text = value == null ? "<null>" : value.ToString();
        signature.Append(name.Length).Append(':').Append(name)
            .Append('=').Append(text.Length).Append(':').Append(text).Append('\n');
    }

    private static T GetOwner<T>(IReadOnlyList<OwnerSectionCensusSnapshot> sections, string sectionId)
        where T : class
    {
        OwnerSectionCensusSnapshot row = sections.Single(candidate => candidate.SectionId == sectionId);
        Assert.That(row.OwnerInstanceIdentity, Is.TypeOf<T>(), sectionId + " owner type");
        return (T)row.OwnerInstanceIdentity;
    }

    private static void AssertIndependentOwners(
        SimulationActiveSession left,
        DailyCaptureEligibilityToken leftToken,
        SimulationActiveSession right,
        DailyCaptureEligibilityToken rightToken)
    {
        Assert.That(left.Runtime, Is.Not.SameAs(right.Runtime));
        Assert.That(left.Composition, Is.Not.SameAs(right.Composition));
        Assert.That(left.IdentityRegistry, Is.Not.SameAs(right.IdentityRegistry));
        Assert.That(left.RandomSource, Is.Not.SameAs(right.RandomSource),
            "Each source, restored target, and control must own an independent immutable random root instance.");
        Assert.That(left.Configuration, Is.SameAs(right.Configuration),
            "Only the accepted immutable profile configuration is shared.");
        Assert.That(left.Runtime.PersonStore, Is.Not.SameAs(right.Runtime.PersonStore));
        Assert.That(left.Runtime.GenealogyStoreForWorldBoundary, Is.Not.SameAs(right.Runtime.GenealogyStoreForWorldBoundary));
        Assert.That(left.Runtime.PoliticalKnowledgeStoreForWorldBoundary,
            Is.Not.SameAs(right.Runtime.PoliticalKnowledgeStoreForWorldBoundary));
        Assert.That(GetOwner<PoliticalDecisionStore>(
                leftToken.OwnerSections, PoliticalDecisionStoreCensusProvider.SectionId),
            Is.Not.SameAs(GetOwner<PoliticalDecisionStore>(
                rightToken.OwnerSections, PoliticalDecisionStoreCensusProvider.SectionId)));
        Assert.That(GetOwner<PoliticalKnowledgeStore>(
                leftToken.OwnerSections, PoliticalKnowledgeStoreCensusProvider.SectionId),
            Is.Not.SameAs(GetOwner<PoliticalKnowledgeStore>(
                rightToken.OwnerSections, PoliticalKnowledgeStoreCensusProvider.SectionId)));
        Assert.That(left.Runtime.WorldId, Is.SameAs(left.Composition.WorldId));
        Assert.That(right.Runtime.WorldId, Is.SameAs(right.Composition.WorldId));
        Assert.That(left.Runtime.SimulationTime, Is.Not.SameAs(right.Runtime.SimulationTime),
            "Mutable simulation-time roots must be private while their captured values match.");
        Assert.That(left.Runtime.HasSameRuntimeAdmissionContext(left.AdmissionContext), Is.True);
        Assert.That(right.Runtime.HasSameRuntimeAdmissionContext(right.AdmissionContext), Is.True);
    }

    private static void ApplyPrivateTargetCorruption(
        SimulationActiveSession candidate,
        PrivateTargetCorruption corruption,
        PoliticalDecisionId decisionId,
        PersonId boundHolder)
    {
        if (corruption == PrivateTargetCorruption.PoliticalWorldRevision)
        {
            Assert.That(candidate.Runtime.PoliticalWorldRevision, Is.EqualTo(1L));
            WritePrivateField(candidate.Runtime, "politicalWorldRevision", 0L);
            return;
        }

        PoliticalDecisionStore decisions = ReadPrivateField<PoliticalDecisionStore>(
            candidate.Runtime, "politicalDecisionStore");
        Dictionary<string, PoliticalDecisionRecord> rows = ReadPrivateField<Dictionary<string, PoliticalDecisionRecord>>(
            decisions, "recordsById");
        Assert.That(rows.TryGetValue(decisionId.Value, out PoliticalDecisionRecord original), Is.True);
        Assert.That(original.Decider.PersonId, Is.EqualTo(boundHolder));

        if (corruption == PrivateTargetCorruption.UnboundDecider)
        {
            PersonId unboundPerson = candidate.Runtime.PersonStore.Persons
                .Select(person => person?.PersonId)
                .First(id => id != null && id != boundHolder);
            PoliticalKnowledgeHolder unboundHolder = PoliticalKnowledgeHolder.ForPerson(unboundPerson);
            Assert.That(candidate.Runtime.TryGetPoliticalKnowledge(unboundHolder, out _), Is.False);
            rows[decisionId.Value] = CopyDecision(original, unboundHolder, original.KnowledgeReferences);
            return;
        }

        rows[decisionId.Value] = CopyDecision(original, original.Decider,
            new[] { KnowledgeReferenceA, "p12g-fixture:knowledge-c" });
    }

    private static PoliticalDecisionRecord CopyDecision(
        PoliticalDecisionRecord original,
        PoliticalKnowledgeHolder decider,
        IEnumerable<string> knowledgeReferences)
    {
        return new PoliticalDecisionRecord(
            original.DecisionId,
            decider,
            original.DecisionKind,
            original.CandidatePersonIds,
            original.Outcome,
            original.ObservedAbsoluteDay,
            original.DecisionAbsoluteDay,
            original.EvidenceReferences,
            knowledgeReferences,
            original.ExpectedWorldRevision,
            original.ExpectedKnowledgeRevision,
            original.OfficeId,
            original.RecognizingInstitutionId);
    }

    private static SimulationRuntimeAdmissionContext CreateInvalidContext(
        SyntheticSession source,
        SimulationRuntimeAdmissionContext sourceContext,
        AdmissionContextCorruption corruption)
    {
        switch (corruption)
        {
            case AdmissionContextCorruption.Null:
                return null;
            case AdmissionContextCorruption.SourceContext:
                return sourceContext;
            case AdmissionContextCorruption.NormalAnchorContext:
                return source.NormalBootstrapContext;
            case AdmissionContextCorruption.WrongOwnerThread:
                Thread foreignThread = new Thread(() => { });
                foreignThread.Start();
                foreignThread.Join();
                return new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    foreignThread,
                    foreignThread.ManagedThreadId);
            case AdmissionContextCorruption.UnsupportedProfile:
                return new SimulationRuntimeAdmissionContext(
                    (SimulationRuntimeAdmissionProfile)int.MaxValue,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption));
        }
    }

    internal static DailyCaptureEligibilityToken GetCompletedToken(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out failure), Is.True, failure.ToString());
        return token;
    }

    private static T ReadPrivateField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
        return (T)field.GetValue(target);
    }

    private static void WritePrivateField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
        field.SetValue(target, value);
    }

    internal sealed class SyntheticSession
    {
        internal AnchorFixture Anchor { get; }
        internal SimulationActiveSession Session { get; }
        internal SimulationRuntimeAdmissionContext NormalBootstrapContext => Anchor.NormalContext;
        internal PoliticalKnowledgeHolder Holder { get; }
        internal PoliticalDecisionId DecisionId { get; }

        internal SyntheticSession(
            AnchorFixture anchor,
            SimulationActiveSession session,
            PoliticalKnowledgeHolder holder,
            PoliticalDecisionId decisionId)
        {
            Anchor = anchor;
            Session = session;
            Holder = holder;
            DecisionId = decisionId;
        }
    }

    internal sealed class AnchorFixture
    {
        internal TesteSimulacao Bootstrap { get; }
        internal SimulationConfigData Configuration { get; }
        internal SimulationBootstrapComposition Composition { get; }
        internal SimulationRuntime Runtime { get; }
        internal SimulationRuntimeAdmissionContext NormalContext { get; }
        internal DailyCaptureEligibilityToken Token { get; }
        internal RuntimeIdentityRegistry IdentityRegistry { get; }

        internal AnchorFixture(
            TesteSimulacao bootstrap,
            SimulationConfigData configuration,
            SimulationBootstrapComposition composition,
            SimulationRuntime runtime,
            SimulationRuntimeAdmissionContext normalContext,
            DailyCaptureEligibilityToken token,
            RuntimeIdentityRegistry identityRegistry)
        {
            Bootstrap = bootstrap;
            Configuration = configuration;
            Composition = composition;
            Runtime = runtime;
            NormalContext = normalContext;
            Token = token;
            IdentityRegistry = identityRegistry;
        }
    }

    public enum PrivateTargetCorruption
    {
        UnboundDecider = 0,
        PoliticalWorldRevision = 1,
        KnowledgeReferenceMismatch = 2
    }

    public enum AdmissionContextCorruption
    {
        Null = 0,
        SourceContext = 1,
        NormalAnchorContext = 2,
        WrongOwnerThread = 3,
        UnsupportedProfile = 4
    }
}

internal static class P12GPoliticalBindingTestAssertions
{
    internal static void ShouldBeTrue(this bool result, object failure)
    {
        Assert.That(result, Is.True, failure?.ToString());
    }
}
