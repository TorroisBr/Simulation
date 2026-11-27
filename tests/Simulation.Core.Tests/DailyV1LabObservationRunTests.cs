using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1LabObservationRunTests
{
    [Test]
    public void StartsFixedNoSaveProfileAndCapturesTwoAdjacentDetachedCuts()
    {
        string root = CreateFreshRoot();
        try
        {
            DailyV1HostEnvironmentIdentity identity = TestIdentity();
            Assert.That(DailyV1LabObservationRun.TryStart(root, identity,
                out DailyV1LabObservationRun run,
                out DailyV1LabObservationStartFailure startFailure), Is.True, startFailure.ToString());
            Assert.That(startFailure, Is.EqualTo(DailyV1LabObservationStartFailure.None));

            using (run)
            {
                Assert.That(Directory.GetFileSystemEntries(root), Is.Empty,
                    "NoSave startup must not create an observation or continuation slot.");

                DailyV1LabObservationAdvanceResult dayOne = run.AdvanceAndObserve();
                Assert.That(dayOne.Disposition, Is.EqualTo(DailyV1LabObservationDisposition.Captured),
                    dayOne.DiagnosticCode.ToString());
                Assert.That(dayOne.DiagnosticCode, Is.EqualTo(DailyV1LabObservationDiagnosticCode.None));
                Assert.That(dayOne.AdvanceFailure, Is.Null);
                DailyV1AtlasObservation first = dayOne.Observation;
                AssertObservationProfile(first);
                Assert.That(first.AbsoluteDay, Is.EqualTo(1L));
                Assert.That(first.CompletedSequence, Is.EqualTo(1L));

                DailyV1LabObservationAdvanceResult dayTwo = run.AdvanceAndObserve();
                Assert.That(dayTwo.Disposition, Is.EqualTo(DailyV1LabObservationDisposition.Captured),
                    dayTwo.DiagnosticCode.ToString());
                DailyV1AtlasObservation second = dayTwo.Observation;
                AssertObservationProfile(second);
                Assert.That(second.WorldId, Is.EqualTo(first.WorldId));
                Assert.That(second.AbsoluteDay, Is.EqualTo(first.AbsoluteDay + 1L));
                Assert.That(second.CompletedSequence, Is.EqualTo(first.CompletedSequence + 1L));
                Assert.That(second.EffectiveInputFingerprint,
                    Is.EqualTo(first.EffectiveInputFingerprint));

                DailyV1AtlasMarketStockObservation firstStock = first.MarketStockRows.Single();
                DailyV1AtlasMarketStockObservation secondStock = second.MarketStockRows.Single();
                Assert.That(firstStock.CityId, Is.EqualTo(first.ScopedCityId));
                Assert.That(firstStock.ItemDefinitionId, Is.EqualTo("TRIGO"));
                Assert.That(secondStock.CityId, Is.EqualTo(second.ScopedCityId));
                Assert.That(secondStock.ItemDefinitionId, Is.EqualTo(firstStock.ItemDefinitionId));
                Assert.That(secondStock.Quantity, Is.Not.EqualTo(firstStock.Quantity),
                    "The approved fixed Daily-v1 profile must expose its real adjacent stock change.");
                Assert.That(firstStock.Quantity, Is.GreaterThanOrEqualTo(0));
                Assert.That(secondStock.Quantity, Is.GreaterThanOrEqualTo(0));
                TestContext.WriteLine("Observed Campo Verde/TRIGO: day {0}={1}, day {2}={3}, delta={4}.",
                    first.AbsoluteDay, firstStock.Quantity,
                    second.AbsoluteDay, secondStock.Quantity,
                    secondStock.Quantity - firstStock.Quantity);

                Assert.That(Directory.GetFileSystemEntries(root), Is.Empty,
                    "Observing a completed day must remain read-only with respect to persistence.");
                AssertReadOnly(first.Hexes);
                AssertReadOnly(first.Locations);
                AssertReadOnly(first.Cities);
                AssertReadOnly(first.MarketStockRows);
            }
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void RejectsNonfreshRootAndInvalidHostBeforePublishingRun()
    {
        string root = CreateFreshRoot();
        try
        {
            string marker = Path.Combine(root, "existing.txt");
            File.WriteAllText(marker, "preserve");
            Assert.That(DailyV1LabObservationRun.TryStart(root, TestIdentity(),
                out DailyV1LabObservationRun nonfresh,
                out DailyV1LabObservationStartFailure nonfreshFailure), Is.False);
            Assert.That(nonfresh, Is.Null);
            Assert.That(nonfreshFailure,
                Is.EqualTo(DailyV1LabObservationStartFailure.InvalidFreshRoot));
            Assert.That(File.ReadAllText(marker), Is.EqualTo("preserve"));

            File.Delete(marker);
            Assert.That(DailyV1LabObservationRun.TryStart(root, null,
                out DailyV1LabObservationRun invalidIdentity,
                out DailyV1LabObservationStartFailure identityFailure), Is.False);
            Assert.That(invalidIdentity, Is.Null);
            Assert.That(identityFailure,
                Is.EqualTo(DailyV1LabObservationStartFailure.InvalidHostIdentity));
            Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void EquivalentFixedRunsMatchGameplayFactsAndAllocateFreshWorldIdentity()
    {
        DailyV1AtlasObservation[] firstRun = RunTwoDays();
        DailyV1AtlasObservation[] secondRun = RunTwoDays();

        Assert.That(firstRun[0].WorldId, Is.Not.EqualTo(secondRun[0].WorldId));
        Assert.That(firstRun[1].WorldId, Is.Not.EqualTo(secondRun[1].WorldId));
        for (int index = 0; index < 2; index++)
        {
            Assert.That(secondRun[index].AbsoluteDay, Is.EqualTo(firstRun[index].AbsoluteDay));
            Assert.That(secondRun[index].CompletedSequence,
                Is.EqualTo(firstRun[index].CompletedSequence));
            Assert.That(secondRun[index].EffectiveInputFingerprint,
                Is.EqualTo(firstRun[index].EffectiveInputFingerprint));
            CollectionAssert.AreEqual(firstRun[index].Hexes.Select(HexFact),
                secondRun[index].Hexes.Select(HexFact));
            CollectionAssert.AreEqual(firstRun[index].Locations.Select(LocationFact),
                secondRun[index].Locations.Select(LocationFact));
            CollectionAssert.AreEqual(firstRun[index].Cities.Select(CityFact),
                secondRun[index].Cities.Select(CityFact));
            CollectionAssert.AreEqual(firstRun[index].MarketStockRows.Select(MarketFact),
                secondRun[index].MarketStockRows.Select(MarketFact));
        }
    }

    [Test]
    public void WrongThreadStopsRunWithoutAdvancing()
    {
        string root = CreateFreshRoot();
        try
        {
            Assert.That(DailyV1LabObservationRun.TryStart(root, TestIdentity(),
                out DailyV1LabObservationRun run,
                out DailyV1LabObservationStartFailure failure), Is.True, failure.ToString());
            using (run)
            {
                DailyV1LabObservationAdvanceResult wrongThread =
                    Task.Run(() => run.AdvanceAndObserve()).GetAwaiter().GetResult();
                Assert.That(wrongThread.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.RejectedBeforeAdvance));
                Assert.That(wrongThread.DiagnosticCode,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.WrongThread));
                Assert.That(wrongThread.Observation, Is.Null);

                DailyV1LabObservationAdvanceResult stopped = run.AdvanceAndObserve();
                Assert.That(stopped.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.RejectedBeforeAdvance));
                Assert.That(stopped.DiagnosticCode,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.RunStopped));
            }
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void ReaderRejectsIncompatibleLeaseActiveOperationAndRevokedTokenWithoutMutation()
    {
        string root = CreateFreshRoot();
        try
        {
            StartDirect(root, out DailyV1EffectiveDefinitions definitions,
                out DailyV1Application application,
                out DailyV1ActiveRuntimeSession session);
            Assert.That(application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());

            DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
            IDailyV1RuntimeAdvanceEvidenceBoundary boundary = session.RuntimeAdvanceBoundary;
            long day = session.SimulationTime.AbsoluteDay;
            long sequence = state.CompletedCoreSequence;
            DailyCaptureEligibilityToken token = state.CurrentCaptureToken;
            Assert.That(token, Is.Not.Null);
            Assert.That(token.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));

            Assert.That(application.TryBeginRuntimeAdvanceReservation(
                    session,
                    out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation reservation,
                    out SimulationRuntimeAdvanceFailure reservationFailure), Is.True,
                reservationFailure.ToString());
            using (reservation)
            {
                Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
                using (lease)
                {
                    Assert.That(DailyV1AtlasObservationReader.TryCapture(
                            application, session, definitions, session.WorldId,
                            day, sequence, out DailyV1AtlasObservation leaseBlocked,
                            out DailyV1LabObservationDiagnosticCode leaseDiagnostic), Is.False);
                    Assert.That(leaseBlocked, Is.Null);
                    Assert.That(leaseDiagnostic,
                        Is.EqualTo(DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable));
                    Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day));
                    Assert.That(state.CompletedCoreSequence, Is.EqualTo(sequence));
                }

                Assert.That(state.AdvanceLeaseHeld, Is.False);
                Assert.That(boundary.TryEnterDailyAdvanceOperation(out IDisposable activeOperation),
                    Is.True, "The selected runtime operation registry should admit its own scope.");
                using (activeOperation)
                {
                    Assert.That(state.ActiveOperationCount, Is.GreaterThan(0));
                    Assert.That(DailyV1AtlasObservationReader.TryCapture(
                            application, session, definitions, session.WorldId,
                            day, sequence, out DailyV1AtlasObservation operationBlocked,
                            out DailyV1LabObservationDiagnosticCode operationDiagnostic), Is.False);
                    Assert.That(operationBlocked, Is.Null);
                    Assert.That(operationDiagnostic,
                        Is.EqualTo(DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable));
                }

                Assert.That(state.ActiveOperationCount, Is.Zero);
                Assert.That(DailyV1AtlasObservationReader.TryCapture(
                        application, session, definitions, session.WorldId,
                        day, sequence, out DailyV1AtlasObservation current,
                        out DailyV1LabObservationDiagnosticCode currentDiagnostic), Is.True,
                    currentDiagnostic.ToString());
                AssertMatchesLiveSources(current, session, definitions);

                state.InvalidateCurrentCaptureToken();
                Assert.That(state.CurrentCaptureToken, Is.Null);
                Assert.That(DailyV1AtlasObservationReader.TryCapture(
                        application, session, definitions, session.WorldId,
                        day, sequence, out DailyV1AtlasObservation revoked,
                        out DailyV1LabObservationDiagnosticCode revokedDiagnostic), Is.False);
                Assert.That(revoked, Is.Null);
                Assert.That(revokedDiagnostic,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable));
                Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day));
                Assert.That(state.CompletedCoreSequence, Is.EqualTo(sequence));
                Assert.That(boundary.TryCaptureDailyBoundaryAdmissionEvidence(
                        sessionReplacement: false,
                        out IReadOnlyList<OwnerSectionCensusSnapshot> afterRevocation,
                        out long epochAfterRevocation, out _), Is.True);
                Assert.That(epochAfterRevocation, Is.EqualTo(token.MutationEpoch));
                AssertCensusEqual(token.OwnerSections, afterRevocation);
            }
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void DisposeRequiresOwnerThreadAndRejectedDisposalPreservesRun()
    {
        string root = CreateFreshRoot();
        DailyV1LabObservationRun run = null;
        try
        {
            Assert.That(DailyV1LabObservationRun.TryStart(root, TestIdentity(),
                out run, out DailyV1LabObservationStartFailure failure), Is.True, failure.ToString());
            Assert.Throws<InvalidOperationException>(
                () => Task.Run(() => run.Dispose()).GetAwaiter().GetResult());

            DailyV1LabObservationAdvanceResult ownerAdvance = run.AdvanceAndObserve();
            Assert.That(ownerAdvance.Disposition,
                Is.EqualTo(DailyV1LabObservationDisposition.Captured),
                ownerAdvance.DiagnosticCode.ToString());
            Assert.That(ownerAdvance.Observation.AbsoluteDay, Is.EqualTo(1L));

            run.Dispose();
            DailyV1LabObservationAdvanceResult afterDispose = run.AdvanceAndObserve();
            Assert.That(afterDispose.Disposition,
                Is.EqualTo(DailyV1LabObservationDisposition.RejectedBeforeAdvance));
            Assert.That(afterDispose.DiagnosticCode,
                Is.EqualTo(DailyV1LabObservationDiagnosticCode.Disposed));
            Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
        }
        finally
        {
            run?.Dispose();
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void OrdinaryApplicationAdvanceMatchesObservingRunFacts()
    {
        DailyV1AtlasObservation[] observingRun = RunTwoDays();
        DailyV1AtlasObservation[] ordinaryRun = RunTwoDaysWithOrdinaryApplicationAdvance();
        Assert.That(ordinaryRun, Has.Length.EqualTo(observingRun.Length));
        for (int index = 0; index < observingRun.Length; index++)
        {
            Assert.That(ordinaryRun[index].WorldId,
                Is.Not.EqualTo(observingRun[index].WorldId),
                "Independent runs publish distinct WorldId objects.");
            Assert.That(ordinaryRun[index].AbsoluteDay,
                Is.EqualTo(observingRun[index].AbsoluteDay));
            Assert.That(ordinaryRun[index].CompletedSequence,
                Is.EqualTo(observingRun[index].CompletedSequence));
            Assert.That(ordinaryRun[index].EffectiveInputFingerprint,
                Is.EqualTo(observingRun[index].EffectiveInputFingerprint));
            CollectionAssert.AreEqual(observingRun[index].Hexes.Select(HexFact),
                ordinaryRun[index].Hexes.Select(HexFact));
            CollectionAssert.AreEqual(observingRun[index].Locations.Select(LocationFact),
                ordinaryRun[index].Locations.Select(LocationFact));
            CollectionAssert.AreEqual(observingRun[index].Cities.Select(CityFact),
                ordinaryRun[index].Cities.Select(CityFact));
            CollectionAssert.AreEqual(observingRun[index].MarketStockRows.Select(MarketFact),
                ordinaryRun[index].MarketStockRows.Select(MarketFact));
        }
    }

    [Test]
    public void ReaderRejectsWrongCutAndReadDoesNotAddMutationOrReleaseAdvanceReservation()
    {
        string root = CreateFreshRoot();
        try
        {
            DailyV1EffectiveDefinitions definitions =
                DailyV1LabSelectedReferenceInputs.CreateDefinitions();
            DailyV1Application application = new DailyV1Application();
            DailyV1ApplicationHost host = new DailyV1ApplicationHost(
                root, SystemAtomicFileSlotOperations.Instance, null, null, TestIdentity());
            Assert.That(application.TryStartDotNetOnExistingApplication(
                    definitions, host,
                    out DailyV1ApplicationStartPreparation preparation,
                    out P12AResult startResult,
                    out string diagnostic), Is.True, diagnostic);
            Assert.That(startResult.Code, Is.EqualTo(P12AResultCode.NoSave));
            DailyV1ActiveRuntimeSession session = preparation.CoreSession;
            Assert.That(application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure initialFailure),
                Is.True, initialFailure.ToString());

            DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
            Assert.That(session.RuntimeAdvanceBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
                    sessionReplacement: false,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> before,
                    out long epochBefore, out _), Is.True);
            DailyCaptureEligibilityToken completedToken = state.CurrentCaptureToken;
            Assert.That(completedToken, Is.Not.Null);
            Assert.That(completedToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
            Assert.That(completedToken.MutationEpoch, Is.EqualTo(epochBefore));
            Assert.That(completedToken.OwnerSections, Has.Count.EqualTo(299),
                "The facade must retain the selected profile's complete 89-family vector.");
            AssertCensusEqual(completedToken.OwnerSections, before);
            Assert.That(application.BoundHost.DotNetIdentity, Is.Not.Null);
            Assert.That(application.BoundHost.UnityV1Identity, Is.Null);
            Assert.That(application.BoundHost.StageCompleted, Is.Null);
            Assert.That(application.BoundHost.LogOutput, Is.Null);
            Assert.That(application.BoundHost.FileOperations,
                Is.SameAs(SystemAtomicFileSlotOperations.Instance));
            long day = session.SimulationTime.AbsoluteDay;
            long sequence = state.CompletedCoreSequence;
            Assert.That(application.TryBeginRuntimeAdvanceReservation(
                    session,
                    out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation reservation,
                    out SimulationRuntimeAdvanceFailure reservationFailure), Is.True,
                reservationFailure.ToString());

            using (reservation)
            {
                Assert.That(DailyV1AtlasObservationReader.TryCapture(
                        application, session, definitions, session.WorldId,
                        day + 1L, sequence + 1L,
                        out DailyV1AtlasObservation staleCut,
                        out DailyV1LabObservationDiagnosticCode staleDiagnostic), Is.False);
                Assert.That(staleCut, Is.Null);
                Assert.That(staleDiagnostic,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable));
                Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day),
                    "A failed read must not roll back the already completed day.");
                Assert.That(state.CompletedCoreSequence, Is.EqualTo(sequence));

                Assert.That(DailyV1AtlasObservationReader.TryCapture(
                        application, session, definitions, session.WorldId,
                        day, sequence,
                        out DailyV1AtlasObservation observation,
                        out DailyV1LabObservationDiagnosticCode diagnosticCode), Is.True,
                    diagnosticCode.ToString());
                Assert.That(observation.AbsoluteDay, Is.EqualTo(day));
                AssertMatchesLiveSources(observation, session, definitions);
                Assert.That(state.ActiveOperationCount, Is.Zero);
                Assert.That(state.AdvanceLeaseHeld, Is.False);
                Assert.That(application.TryAdvanceDay(
                        out SimulationRuntimeAdvanceFailure blockedFailure), Is.False,
                    "The read must not release the facade's outer runtime advance reservation.");
                Assert.That(blockedFailure,
                    Is.EqualTo(SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress));
                Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day));
            }

            Assert.That(session.RuntimeAdvanceBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
                    sessionReplacement: false,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> after,
                    out long epochAfter, out _), Is.True);
            Assert.That(epochAfter, Is.EqualTo(epochBefore),
                "The detached factual read cannot add a mutation epoch.");
            AssertCensusEqual(before, after);
            Assert.That(application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextFailure),
                Is.True, nextFailure.ToString());
            Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day + 1L));
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void ReaderRejectsForeignSessionAndEquivalentButDifferentDefinitions()
    {
        string firstRoot = CreateFreshRoot();
        string secondRoot = CreateFreshRoot();
        try
        {
            StartDirect(firstRoot, out DailyV1EffectiveDefinitions definitions,
                out DailyV1Application application,
                out DailyV1ActiveRuntimeSession session);
            StartDirect(secondRoot, out DailyV1EffectiveDefinitions foreignDefinitions,
                out DailyV1Application foreignApplication,
                out DailyV1ActiveRuntimeSession foreignSession);
            Assert.That(DailyV1AtlasObservationReader.TryCapture(
                    application, session, definitions, session.WorldId,
                    0L, 0L, out DailyV1AtlasObservation genesisObservation,
                    out DailyV1LabObservationDiagnosticCode genesisFailure), Is.False);
            Assert.That(genesisObservation, Is.Null);
            Assert.That(genesisFailure,
                Is.EqualTo(DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable));
            Assert.That(application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure),
                Is.True, failure.ToString());
            long day = session.SimulationTime.AbsoluteDay;
            long sequence = session.ExecutionBoundaryState.CompletedCoreSequence;

            Assert.That(WorldId.TryParse(session.WorldId.Value,
                out WorldId equalValueDifferentIdentity), Is.True);
            Assert.That(equalValueDifferentIdentity.Value, Is.EqualTo(session.WorldId.Value));
            Assert.That(equalValueDifferentIdentity, Is.Not.SameAs(session.WorldId));
            Assert.That(DailyV1AtlasObservationReader.TryCapture(
                    application, session, definitions, equalValueDifferentIdentity,
                    day, sequence, out DailyV1AtlasObservation replacedWorldObservation,
                    out DailyV1LabObservationDiagnosticCode replacedWorldFailure), Is.False);
            Assert.That(replacedWorldObservation, Is.Null);
            Assert.That(replacedWorldFailure,
                Is.EqualTo(DailyV1LabObservationDiagnosticCode.SessionChanged));

            Assert.That(DailyV1AtlasObservationReader.TryCapture(
                    application, foreignSession, definitions, session.WorldId,
                    day, sequence, out DailyV1AtlasObservation foreignSessionObservation,
                    out DailyV1LabObservationDiagnosticCode foreignSessionFailure), Is.False);
            Assert.That(foreignSessionObservation, Is.Null);
            Assert.That(foreignSessionFailure,
                Is.EqualTo(DailyV1LabObservationDiagnosticCode.SessionChanged));

            Assert.That(DailyV1AtlasObservationReader.TryCapture(
                    application, session, foreignDefinitions, session.WorldId,
                    day, sequence, out DailyV1AtlasObservation foreignDefinitionsObservation,
                    out DailyV1LabObservationDiagnosticCode foreignDefinitionsFailure), Is.False);
            Assert.That(foreignDefinitionsObservation, Is.Null);
            Assert.That(foreignDefinitionsFailure,
                Is.EqualTo(DailyV1LabObservationDiagnosticCode.SessionChanged));
            Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(day));
            Assert.That(session.ExecutionBoundaryState.CompletedCoreSequence,
                Is.EqualTo(sequence));
            _ = foreignApplication;
        }
        finally
        {
            DeleteOwnedRoot(firstRoot);
            DeleteOwnedRoot(secondRoot);
        }
    }

    [Test]
    public void ScopedMarketReaderDistinguishesPresentZeroFromAbsent()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities["CAMPO_VERDE"];
        DailyV1ItemDefinition item = definitions.Items["TRIGO"];

        DailyV1MarketOwner presentMarket = new DailyV1MarketOwner(definitions);
        Assert.That(presentMarket.TryInitializeFromCity(city), Is.True);
        DailyV1MarketItemState initial = presentMarket.GetItem(item);
        Assert.That(initial, Is.Not.Null);
        Assert.That(initial.Amount, Is.GreaterThan(0));
        Assert.That(presentMarket.RemoveStockUpTo(item, int.MaxValue),
            Is.EqualTo(initial.Amount));
        Assert.That(presentMarket.GetItem(item), Is.Not.Null,
            "Zero stock remains a present canonical row.");
        Assert.That(presentMarket.GetItem(item).Amount, Is.Zero);
        Assert.That(DailyV1AtlasObservationReader.TryCopyScopedMarketRows(
                presentMarket, definitions, "city-runtime", item,
                out IReadOnlyList<DailyV1AtlasMarketStockObservation> presentRows), Is.True);
        Assert.That(presentRows, Has.Count.EqualTo(1));
        Assert.That(presentRows[0].Quantity, Is.Zero);

        DailyV1MarketOwner absentMarket = new DailyV1MarketOwner(definitions);
        Assert.That(absentMarket.GetItem(item), Is.Null);
        Assert.That(DailyV1AtlasObservationReader.TryCopyScopedMarketRows(
                absentMarket, definitions, "city-runtime", item,
                out IReadOnlyList<DailyV1AtlasMarketStockObservation> absentRows), Is.True);
        Assert.That(absentRows, Is.Empty);
    }

    [Test]
    public void ScopedMarketReaderRejectsForeignOwnerIdentity()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        DailyV1EffectiveDefinitions foreignDefinitions =
            DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        DailyV1MarketOwner foreignMarket = new DailyV1MarketOwner(foreignDefinitions);
        Assert.That(foreignMarket.TryInitializeFromCity(
            foreignDefinitions.Cities["CAMPO_VERDE"]), Is.True);

        Assert.That(DailyV1AtlasObservationReader.TryCopyScopedMarketRows(
                foreignMarket, definitions, "city-runtime", definitions.Items["TRIGO"],
                out IReadOnlyList<DailyV1AtlasMarketStockObservation> rows), Is.False);
        Assert.That(rows, Is.Null);
    }

    [Test]
    public void CensusComparisonRejectsChangedAndSwappedOwnerWitnesses()
    {
        object firstOwner = new object();
        object secondOwner = new object();
        OwnerSectionCensusSnapshot[] baseline =
        {
            new OwnerSectionCensusSnapshot("section-a", 1, OwnerSectionRole.Required,
                firstOwner, 1, 3L),
            new OwnerSectionCensusSnapshot("section-b", 1, OwnerSectionRole.Required,
                secondOwner, 2, 7L)
        };
        OwnerSectionCensusSnapshot[] unchanged =
        {
            new OwnerSectionCensusSnapshot("section-b", 1, OwnerSectionRole.Required,
                secondOwner, 2, 7L),
            new OwnerSectionCensusSnapshot("section-a", 1, OwnerSectionRole.Required,
                firstOwner, 1, 3L)
        };
        OwnerSectionCensusSnapshot[] changedRevision =
        {
            new OwnerSectionCensusSnapshot("section-a", 1, OwnerSectionRole.Required,
                firstOwner, 1, 4L),
            new OwnerSectionCensusSnapshot("section-b", 1, OwnerSectionRole.Required,
                secondOwner, 2, 7L)
        };
        OwnerSectionCensusSnapshot[] swappedOwners =
        {
            new OwnerSectionCensusSnapshot("section-a", 1, OwnerSectionRole.Required,
                secondOwner, 1, 3L),
            new OwnerSectionCensusSnapshot("section-b", 1, OwnerSectionRole.Required,
                firstOwner, 2, 7L)
        };

        Assert.That(DailyV1AtlasObservationReader.SameCensus(baseline, unchanged), Is.True,
            "The same witnesses remain equal even when their enumeration order differs.");
        Assert.That(DailyV1AtlasObservationReader.SameCensus(baseline, changedRevision), Is.False,
            "A changed owner revision cannot pass the detached-cut comparison.");
        Assert.That(DailyV1AtlasObservationReader.SameCensus(baseline, swappedOwners), Is.False,
            "An owner instance moved to a different section cannot pass the cut comparison.");
    }

    [Test]
    public void ScopedMarketReaderRejectsInvalidAndForeignSourceRows()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["TRIGO"];
        DailyV1EffectiveDefinitions foreignDefinitions =
            DailyV1LabSelectedReferenceInputs.CreateDefinitions();

        AssertMalformedRowsRejected(definitions, item, rows => rows.Add(null));
        AssertMalformedRowsRejected(definitions, item, rows =>
            rows.Add(new DailyV1MarketItemState(foreignDefinitions.Items["TRIGO"], 1, 1, 1f)));
        AssertMalformedRowsRejected(definitions, item, rows =>
            rows.Add(new DailyV1MarketItemState(item, -1, 1, 1f)));
        AssertMalformedRowsRejected(definitions, item, rows =>
        {
            rows.Add(new DailyV1MarketItemState(item, 1, 1, 1f));
            rows.Add(new DailyV1MarketItemState(item, 2, 1, 1f));
        });
    }

    [Test]
    public void PartialThrowingCoreAdvanceReturnsNoCutStopsRunAndDoesNotClaimRollback()
    {
        string root = CreateFreshRoot();
        try
        {
            DailyV1EffectiveDefinitions definitions =
                DailyV1LabSelectedReferenceInputs.CreateDefinitions();
            DailyV1Application application = new DailyV1Application();
            DailyV1ApplicationHost host = new DailyV1ApplicationHost(
                root, SystemAtomicFileSlotOperations.Instance,
                stageCompleted: null,
                logOutput: null,
                dotNetIdentity: TestIdentity());
            ThrowAfterEconomyLogRuntimePorts injectedPorts = null;
            Assert.That(application.TryStartDotNetOnExistingApplication(
                    definitions, host,
                    (DailyV1ApplicationStartPreparation candidatePreparation,
                        DailyV1ActiveRuntimeSession candidateSession,
                        out IDailyV1SelectedRuntimeAdvanceHostPorts candidatePorts,
                        out string candidateDiagnostic) =>
                    {
                        bool created = TryCreateThrowAfterEconomyLogPorts(
                            candidatePreparation, candidateSession,
                            out candidatePorts, out candidateDiagnostic);
                        injectedPorts = candidatePorts as ThrowAfterEconomyLogRuntimePorts;
                        return created;
                    },
                    out DailyV1ApplicationStartPreparation preparation,
                    out P12AResult persistenceResult,
                    out string diagnostic), Is.True, diagnostic);
            Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));

            DailyV1ActiveRuntimeSession session = preparation.CoreSession;
            Assert.That(application.CurrentRuntimeSession, Is.SameAs(session));
            DailyV1LabObservationRun run = CreateRunForTest(application, session, definitions);
            using (run)
            {
                long[] openingMarketRevisions = session.OwnerGraph.Cities
                    .Select(owner => owner.Market.Revision).ToArray();
                long openingEpoch = session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch;
                long openingDay = session.SimulationTime.AbsoluteDay;
                long openingSequence = session.ExecutionBoundaryState.CompletedCoreSequence;

                DailyV1LabObservationAdvanceResult failed = run.AdvanceAndObserve();
                Assert.That(failed.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.AdvanceUnsuccessful));
                Assert.That(failed.DiagnosticCode,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.RuntimeAdvanceFailed));
                Assert.That(failed.AdvanceFailure,
                    Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
                Assert.That(failed.Observation, Is.Null);
                Assert.That(injectedPorts, Is.Not.Null);
                Assert.That(injectedPorts.FaultInjected, Is.True,
                    "The injected failure must follow an observed committed Market write. Economy callbacks: "
                    + injectedPorts.ObservedEconomyCategories);
                bool marketOwnerChanged = session.OwnerGraph.Cities
                    .Select((owner, index) => owner.Market.Revision > openingMarketRevisions[index])
                    .Any(changed => changed);
                Assert.That(marketOwnerChanged, Is.True,
                    "The injected exception follows a committed Market write; the facade must not claim rollback.");
                Assert.That(session.ExecutionBoundaryState.MutationEpochState.CurrentEpoch,
                    Is.GreaterThanOrEqualTo(openingEpoch),
                    "A failed partial advance cannot move the shared epoch backwards.");
                Assert.That(session.ExecutionBoundaryState.CurrentCaptureToken, Is.Null,
                    "A failed partial advance cannot retain or publish P12 capture eligibility.");
                Assert.That(session.SimulationTime.AbsoluteDay, Is.EqualTo(openingDay + 1L),
                    "Core advances the clock before executing the selected daily stage; this failure is partial and not rolled back.");
                Assert.That(session.ExecutionBoundaryState.CompletedCoreSequence,
                    Is.EqualTo(openingSequence));
                Assert.That(application.ActiveOperationCount, Is.Zero,
                    "The failed advance releases the run-owned reservation.");

                DailyV1LabObservationAdvanceResult stopped = run.AdvanceAndObserve();
                Assert.That(stopped.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.RejectedBeforeAdvance));
                Assert.That(stopped.DiagnosticCode,
                    Is.EqualTo(DailyV1LabObservationDiagnosticCode.RunStopped));
                Assert.That(stopped.Observation, Is.Null);
            }
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    [Test]
    public void PublicFacadeSurfaceDoesNotExposeRuntimeStateOrHostPorts()
    {
        Assert.That(typeof(DailyV1LabObservationRun).GetConstructors(), Is.Empty,
            "Callers can only obtain the fixed run through TryStart.");
        string[] runMethods = typeof(DailyV1LabObservationRun)
            .GetMethods(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(
            new[] { "AdvanceAndObserve", "Dispose", "TryStart" }, runMethods);

        Type[] forbidden =
        {
            typeof(DailyV1Application),
            typeof(DailyV1ActiveRuntimeSession),
            typeof(DailyV1SelectedRuntimeOwnerGraph),
            typeof(DailyV1MarketOwner),
            typeof(SpatialAuthorityStore),
            typeof(IAtomicFileSlotOperations),
            typeof(Delegate)
        };
        foreach (System.Reflection.PropertyInfo property in
            typeof(DailyV1AtlasObservation).GetProperties())
            foreach (Type forbiddenType in forbidden)
                Assert.That(forbiddenType.IsAssignableFrom(property.PropertyType), Is.False,
                    property.Name + " must be a detached factual value.");
        Assert.That(typeof(DailyV1AtlasObservation).GetConstructors(), Is.Empty,
            "Observation DTO construction remains internal to the coherent reader.");
    }

    private static DailyV1AtlasObservation[] RunTwoDays()
    {
        string root = CreateFreshRoot();
        try
        {
            Assert.That(DailyV1LabObservationRun.TryStart(root, TestIdentity(),
                out DailyV1LabObservationRun run,
                out DailyV1LabObservationStartFailure failure), Is.True, failure.ToString());
            using (run)
            {
                DailyV1LabObservationAdvanceResult first = run.AdvanceAndObserve();
                DailyV1LabObservationAdvanceResult second = run.AdvanceAndObserve();
                Assert.That(first.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.Captured), first.DiagnosticCode.ToString());
                Assert.That(second.Disposition,
                    Is.EqualTo(DailyV1LabObservationDisposition.Captured), second.DiagnosticCode.ToString());
                return new[] { first.Observation, second.Observation };
            }
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }

    private static DailyV1AtlasObservation[] RunTwoDaysWithOrdinaryApplicationAdvance()
    {
        string root = CreateFreshRoot();
        try
        {
            StartDirect(root, out DailyV1EffectiveDefinitions definitions,
                out DailyV1Application application,
                out DailyV1ActiveRuntimeSession session);
            DailyV1AtlasObservation[] observations = new DailyV1AtlasObservation[2];
            for (int index = 0; index < observations.Length; index++)
            {
                Assert.That(application.TryAdvanceDay(
                        out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
                    advanceFailure.ToString());
                long day = session.SimulationTime.AbsoluteDay;
                long sequence = session.ExecutionBoundaryState.CompletedCoreSequence;
                Assert.That(application.TryBeginRuntimeAdvanceReservation(
                        session,
                        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation reservation,
                        out SimulationRuntimeAdvanceFailure reservationFailure), Is.True,
                    reservationFailure.ToString());
                using (reservation)
                {
                    Assert.That(DailyV1AtlasObservationReader.TryCapture(
                            application, session, definitions, session.WorldId,
                            day, sequence, out observations[index],
                            out DailyV1LabObservationDiagnosticCode diagnosticCode), Is.True,
                        diagnosticCode.ToString());
                }
            }
            return observations;
        }
        finally
        {
            DeleteOwnedRoot(root);
        }
    }
    private static void AssertObservationProfile(DailyV1AtlasObservation observation)
    {
        Assert.That(observation, Is.Not.Null);
        Assert.That(observation.WorldId, Is.Not.Empty);
        Assert.That(observation.EffectiveInputFingerprint, Does.Match("^[0-9a-f]{64}$"));
        Assert.That(observation.Hexes, Is.Not.Empty);
        Assert.That(observation.Hexes.Any(hex =>
            string.Equals(hex.TerrainRevision, "sample-world-v1", StringComparison.Ordinal)), Is.True,
            "The opaque authored terrain revision token must be retained as text.");
        Assert.That(observation.Locations, Is.Not.Empty);
        foreach (DailyV1AtlasLocationObservation location in observation.Locations)
            Assert.That(observation.Hexes.Any(hex => hex.HexId == location.AnchorHexId), Is.True);

        Assert.That(observation.Cities, Has.Count.EqualTo(2));
        Assert.That(observation.Cities.Select(city => city.CityId).Distinct().Count(), Is.EqualTo(2));
        Assert.That(observation.Cities.Select(city => city.Label),
            Is.EquivalentTo(new[] { "Campo Verde", "Serra de Ferro" }));
        Assert.That(observation.Cities.All(city => city.P8LocationId == null), Is.True,
            "The selected exogenous Cities have no admitted top-level P8 anchor.");

        Assert.That(observation.ScopedCityId, Is.Not.Empty);
        Assert.That(observation.ScopedItemDefinitionId, Is.EqualTo("TRIGO"));
        Assert.That(observation.MarketStockRows, Has.Count.EqualTo(1));
    }

    private static void AssertMalformedRowsRejected(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ItemDefinition item,
        Action<List<DailyV1MarketItemState>> populateRows)
    {
        DailyV1MarketOwner market = new DailyV1MarketOwner(definitions);
        Assert.That(market.TryInitializeFromCity(definitions.Cities["CAMPO_VERDE"]), Is.True);
        System.Reflection.FieldInfo rowsField = typeof(DailyV1MarketOwner).GetField(
            "rows",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(rowsField, Is.Not.Null);
        List<DailyV1MarketItemState> rows =
            (List<DailyV1MarketItemState>)rowsField.GetValue(market);
        rows.Clear();
        populateRows(rows);

        Assert.That(DailyV1AtlasObservationReader.TryCopyScopedMarketRows(
                market, definitions, "city-runtime", item,
                out IReadOnlyList<DailyV1AtlasMarketStockObservation> observations), Is.False);
        Assert.That(observations, Is.Null);
    }

    private static DailyV1LabObservationRun CreateRunForTest(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session,
        DailyV1EffectiveDefinitions definitions)
    {
        System.Reflection.ConstructorInfo constructor = typeof(DailyV1LabObservationRun).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(DailyV1Application),
                typeof(DailyV1ActiveRuntimeSession),
                typeof(DailyV1EffectiveDefinitions)
            },
            modifiers: null);
        Assert.That(constructor, Is.Not.Null);
        return (DailyV1LabObservationRun)constructor.Invoke(
            new object[] { application, session, definitions });
    }

    private static bool TryCreateThrowAfterEconomyLogPorts(
        DailyV1ApplicationStartPreparation preparation,
        DailyV1ActiveRuntimeSession session,
        out IDailyV1SelectedRuntimeAdvanceHostPorts ports,
        out string diagnostic)
    {
        if (!DailyV1CoreSelectedRuntimeAdvanceHostPorts.TryCreateForApplicationStart(
                preparation, session,
                out IDailyV1SelectedRuntimeAdvanceHostPorts corePorts,
                out diagnostic))
        {
            ports = null;
            return false;
        }

        ports = new ThrowAfterEconomyLogRuntimePorts(corePorts);
        return true;
    }

    private sealed class ThrowAfterEconomyLogRuntimePorts
        : IDailyV1SelectedRuntimeAdvanceHostPorts
    {
        private readonly IDailyV1SelectedRuntimeAdvanceHostPorts inner;
        private ThrowAfterEconomyLogDailyHostPorts dailyHostPorts;

        internal ThrowAfterEconomyLogRuntimePorts(
            IDailyV1SelectedRuntimeAdvanceHostPorts inner) => this.inner = inner;
        internal bool FaultInjected => dailyHostPorts?.FaultInjected == true;
        internal string ObservedEconomyCategories =>
            dailyHostPorts == null ? string.Empty : string.Join(",", dailyHostPorts.ObservedEconomyCategories);

        public DailyV1ExecutionBoundaryState ExecutionState => inner.ExecutionState;
        public object SelectedRuntimeOwnerCensusSourceIdentity =>
            inner.SelectedRuntimeOwnerCensusSourceIdentity;

        public bool TryGetExactJusticeRecordsCensusProjectionIdentity(
            DailyV1JusticeRecordsOwner owner,
            out object projectionIdentity,
            out long p18ReceiptRevision) =>
            inner.TryGetExactJusticeRecordsCensusProjectionIdentity(
                owner, out projectionIdentity, out p18ReceiptRevision);

        public bool TryCreateSelectedDailyExecutionHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDailyExecutionHostPorts hostPorts)
        {
            if (!inner.TryCreateSelectedDailyExecutionHostPorts(graph,
                    out IDailyV1SelectedDailyExecutionHostPorts coreHostPorts))
            {
                hostPorts = null;
                return false;
            }
            dailyHostPorts = new ThrowAfterEconomyLogDailyHostPorts(coreHostPorts, graph);
            hostPorts = dailyHostPorts;
            return true;
        }

        public bool TryCaptureOwnerSectionCensus(
            DailyV1CaptureScopeAllowance allowedOuterScope,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure) =>
            inner.TryCaptureOwnerSectionCensus(
                allowedOuterScope, out ownerSections, out mutationEpoch, out failure);

        public bool TryBindRestoredCandidateMutationGuard(out string diagnostic) =>
            inner.TryBindRestoredCandidateMutationGuard(out diagnostic);

        public bool TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out string diagnostic) =>
            inner.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out ownerSections, out mutationEpoch, out diagnostic);
    }

    private sealed class ThrowAfterEconomyLogDailyHostPorts
        : IDailyV1SelectedDailyExecutionHostPorts
    {
        private readonly IDailyV1SelectedDailyExecutionHostPorts inner;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly long[] openingMarketRevisions;
        private readonly List<SimulationLogCategory> observedEconomyCategories =
            new List<SimulationLogCategory>();
        internal IReadOnlyList<SimulationLogCategory> ObservedEconomyCategories =>
            observedEconomyCategories;
        internal bool FaultInjected { get; private set; }

        internal ThrowAfterEconomyLogDailyHostPorts(
            IDailyV1SelectedDailyExecutionHostPorts inner,
            DailyV1SelectedRuntimeOwnerGraph graph)
        {
            if (graph?.Cities == null)
                throw new ArgumentException("The fault-injection profile requires the admitted City graph.", nameof(graph));
            this.inner = inner;
            this.graph = graph;
            openingMarketRevisions = graph.Cities
                .Select(owner => owner.Market.Revision).ToArray();
        }

        public void BeginLoggerDay(long absoluteDay) => inner.BeginLoggerDay(absoluteDay);
        public bool HasExactCrimeJusticeProjectionOwnerBindings(
            DailyV1SelectedRuntimeOwnerGraph graph) =>
            inner.HasExactCrimeJusticeProjectionOwnerBindings(graph);
        public bool TryCreateSelectedDemographyHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyHostPorts hostPorts) =>
            inner.TryCreateSelectedDemographyHostPorts(graph, out hostPorts);
        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result) =>
            inner.PublishSelectedDemographyResult(result);
        public void BeginSimulationDay() => inner.BeginSimulationDay();
        public void EnsureSelectedEconomyProjectionCanRun(
            DailyV1SelectedRuntimeOwnerGraph graph) =>
            inner.EnsureSelectedEconomyProjectionCanRun(graph);
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner,
            SimulationLogCategory category,
            string message)
        {
            if (category == SimulationLogCategory.EconomyProduction
                || category == SimulationLogCategory.EconomyConsumption)
                observedEconomyCategories.Add(category);
            inner.LogSelectedDailyEconomy(owner, category, message);
            if (category != SimulationLogCategory.EconomyProduction
                && category != SimulationLogCategory.EconomyConsumption)
                return;
            for (int index = 0; index < graph.Cities.Count; index++)
                if (ReferenceEquals(graph.Cities[index], owner)
                    && owner.Market.Revision > openingMarketRevisions[index])
                {
                    FaultInjected = true;
                    throw new InvalidOperationException("Injected Core facade partial-advance failure.");
                }
        }
        public void EnsureSelectedLocalKnowledgeProjectionCanRun(
            DailyV1SelectedRuntimeOwnerGraph graph,
            bool merchantTradeEnabled) =>
            inner.EnsureSelectedLocalKnowledgeProjectionCanRun(
                graph, merchantTradeEnabled);
        public bool TryCreateSelectedActorTurnHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnHostPorts hostPorts) =>
            inner.TryCreateSelectedActorTurnHostPorts(graph, out hostPorts);
        public bool TryCreateSelectedTravelDayHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayHostPorts hostPorts) =>
            inner.TryCreateSelectedTravelDayHostPorts(graph, out hostPorts);
    }

    private static DailyV1HostEnvironmentIdentity TestIdentity() =>
        DailyV1DotNetHostEnvironmentIdentityFactory.Capture(
            typeof(DailyV1LabObservationRunTests));

    private static void StartDirect(
        string root,
        out DailyV1EffectiveDefinitions definitions,
        out DailyV1Application application,
        out DailyV1ActiveRuntimeSession session)
    {
        definitions = DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        application = new DailyV1Application();
        DailyV1ApplicationHost host = new DailyV1ApplicationHost(
            root, SystemAtomicFileSlotOperations.Instance, null, null, TestIdentity());
        Assert.That(application.TryStartDotNetOnExistingApplication(
                definitions, host,
                out DailyV1ApplicationStartPreparation preparation,
                out P12AResult startResult,
                out string diagnostic), Is.True, diagnostic);
        Assert.That(startResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        session = preparation.CoreSession;
        Assert.That(application.CurrentRuntimeSession, Is.SameAs(session));
    }

    private static void AssertCensusEqual(
        IReadOnlyList<OwnerSectionCensusSnapshot> expected,
        IReadOnlyList<OwnerSectionCensusSnapshot> actual)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count));
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.That(actual[index].SectionId, Is.EqualTo(expected[index].SectionId));
            Assert.That(actual[index].SchemaVersion, Is.EqualTo(expected[index].SchemaVersion));
            Assert.That(actual[index].Role, Is.EqualTo(expected[index].Role));
            Assert.That(actual[index].Cardinality, Is.EqualTo(expected[index].Cardinality));
            Assert.That(actual[index].Revision, Is.EqualTo(expected[index].Revision));
            Assert.That(actual[index].OwnerInstanceIdentity,
                Is.SameAs(expected[index].OwnerInstanceIdentity));
        }
    }

    private static void AssertMatchesLiveSources(
        DailyV1AtlasObservation observation,
        DailyV1ActiveRuntimeSession session,
        DailyV1EffectiveDefinitions definitions)
    {
        SpatialAuthorityStore spatial = session.P12CRoots.SpatialAuthority;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        DailyV1RuntimeIdentityIndex identities = graph.RuntimeIdentityIndex;
        LegacySpatialAnchorBindingStore anchors = graph.LegacySpatialAnchors;
        DailyV1CityRuntimeOwners[] cities = graph.Cities
            .OrderBy(city => city.RuntimeId, StringComparer.Ordinal).ToArray();

        Assert.That(observation.SpatialRevision, Is.EqualTo(spatial.Revision));
        HexRecord[] expectedHexes = spatial.Hexes
            .OrderBy(hex => hex.Id.Value, StringComparer.Ordinal).ToArray();
        Assert.That(observation.Hexes, Has.Count.EqualTo(expectedHexes.Length));
        for (int index = 0; index < expectedHexes.Length; index++)
        {
            HexRecord expected = expectedHexes[index];
            DailyV1AtlasHexObservation actual = observation.Hexes[index];
            Assert.That(actual.HexId, Is.EqualTo(expected.Id.Value));
            Assert.That(actual.Q, Is.EqualTo(expected.Coordinate?.Q));
            Assert.That(actual.R, Is.EqualTo(expected.Coordinate?.R));
            Assert.That(actual.TerrainId,
                Is.EqualTo(expected.TerrainReference?.TerrainDefinitionId?.Value));
            Assert.That(actual.TerrainRevision,
                Is.EqualTo(expected.TerrainReference?.AuthoredRevisionToken));
        }

        LocationRecord[] expectedLocations = spatial.Locations
            .OrderBy(location => location.Id.Value, StringComparer.Ordinal).ToArray();
        Assert.That(observation.Locations, Has.Count.EqualTo(expectedLocations.Length));
        for (int index = 0; index < expectedLocations.Length; index++)
        {
            Assert.That(observation.Locations[index].LocationId,
                Is.EqualTo(expectedLocations[index].Id.Value));
            Assert.That(observation.Locations[index].AnchorHexId,
                Is.EqualTo(expectedLocations[index].AnchorHexId.Value));
        }

        Assert.That(observation.IdentityIndexRevision, Is.EqualTo(identities.CensusRevision));
        Assert.That(observation.AnchorRevision, Is.EqualTo(anchors.Revision));
        Assert.That(observation.Cities, Has.Count.EqualTo(cities.Length));
        for (int index = 0; index < cities.Length; index++)
        {
            DailyV1CityRuntimeOwners city = cities[index];
            Assert.That(identities.HasIdentity(city.RuntimeId, DailyV1RuntimeIdentityKind.City), Is.True);
            Assert.That(observation.Cities[index].CityId, Is.EqualTo(city.RuntimeId));
            Assert.That(observation.Cities[index].Label,
                Is.EqualTo(city.Definition.Values.cityName));
            SpatialAnchorBinding binding = anchors.Bindings.SingleOrDefault(candidate =>
                candidate.OwnerId.Kind == SpatialAnchorOwnerKind.City
                && candidate.OwnerId.Value == city.RuntimeId);
            Assert.That(observation.Cities[index].P8LocationId,
                Is.EqualTo(binding?.LocationId?.Value));
        }

        DailyV1CityRuntimeOwners scopedCity = cities.Single(city =>
            city.Definition.DefinitionId == "CAMPO_VERDE");
        DailyV1ItemDefinition item = definitions.Items["TRIGO"];
        DailyV1MarketItemState sourceStock = scopedCity.Market.GetItem(item);
        Assert.That(sourceStock, Is.Not.Null);
        Assert.That(observation.ScopedCityId, Is.EqualTo(scopedCity.RuntimeId));
        Assert.That(observation.ScopedItemDefinitionId, Is.SameAs(item.DefinitionId));
        Assert.That(observation.MarketRevision, Is.EqualTo(scopedCity.Market.Revision));
        Assert.That(observation.MarketStockRows, Has.Count.EqualTo(1));
        Assert.That(observation.MarketStockRows[0].CityId, Is.EqualTo(scopedCity.RuntimeId));
        Assert.That(observation.MarketStockRows[0].ItemDefinitionId,
            Is.SameAs(item.DefinitionId));
        Assert.That(observation.MarketStockRows[0].Quantity, Is.EqualTo(sourceStock.Amount));
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        Assert.That(values, Is.InstanceOf<IList<T>>());
        Assert.That(((IList<T>)values).IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => ((IList<T>)values).Clear());
    }

    private static string HexFact(DailyV1AtlasHexObservation value) => string.Join("|",
        value.HexId, value.Q?.ToString() ?? "<null>", value.R?.ToString() ?? "<null>",
        value.TerrainId ?? "<null>", value.TerrainRevision ?? "<null>");

    private static string LocationFact(DailyV1AtlasLocationObservation value) =>
        value.LocationId + "|" + value.AnchorHexId;

    private static string CityFact(DailyV1AtlasCityObservation value) =>
        value.CityId + "|" + value.Label + "|" + (value.P8LocationId ?? "<unbound>");

    private static string MarketFact(DailyV1AtlasMarketStockObservation value) =>
        value.CityId + "|" + value.ItemDefinitionId + "|" + value.Quantity;

    private static string CreateFreshRoot()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "LabObservationScratch",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteOwnedRoot(string root)
    {
        if (root != null && Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
