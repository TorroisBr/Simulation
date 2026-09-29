using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class ContinuationCensusProtocolTests
{
    private const string ReceiptSectionId = NpcDecisionRecorder.OccurrenceReceiptSectionId;

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void OwnerSectionAssessmentRequiresSealedRequirementsAndLiveProvider()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(ReceiptSectionId, records.DecisionRecorder, out _), Is.True);

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure beforeSeal), Is.False);
        Assert.That(beforeSeal, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.BindOwnerThread(out ContinuationCensusFailure bindBeforeSeal), Is.False);
        Assert.That(bindBeforeSeal, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));

        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("bootstrap.publish", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure complete), Is.True,
            complete.ToString());
    }

    [Test]
    public void EmptyProtocolDefaultsToOwnerCoverageIncomplete()
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void MissingProviderAfterInventorySealRemainsIncomplete()
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("bootstrap.publish", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void UnnotifiedOwnerRevisionChangeFaultsBeforeCoverageCanBeAccepted()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.Required);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure initial), Is.True,
            initial.ToString());
        Assert.That(protocol.TryReadMutationEpoch(out long initialEpoch, out _), Is.True);
        Assert.That(initialEpoch, Is.Zero);

        RecordOccurrence(records.DecisionRecorder, "required-section-receipt");
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure missingNotification), Is.False);
        Assert.That(missingNotification, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.NotifyCommittedMutation(ReceiptSectionId, out ContinuationCensusFailure notified), Is.False,
            "The prior unnotified revision change must fail closed permanently.");
        Assert.That(notified, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(protocol.TryReadMutationEpoch(out long epochAfterFault, out ContinuationCensusFailure fault), Is.False);
        Assert.That(epochAfterFault, Is.Zero);
        Assert.That(fault, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void NotifiedOwnerCommitAdvancesEpochAndRebaselinesOwnerRevision()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.Required);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out _), Is.True);
        RecordOccurrence(records.DecisionRecorder, "notified-section-receipt");

        Assert.That(protocol.NotifyCommittedMutation(ReceiptSectionId, out ContinuationCensusFailure notificationFailure), Is.True,
            notificationFailure.ToString());
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out _), Is.True);
        Assert.That(epoch, Is.EqualTo(1L));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure current), Is.True,
            current.ToString());
    }

    [Test]
    public void OwnerRevisionRollbackIsRejectedAfterAValidNotifiedChange()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        OwnerSectionCensusWitness empty = records.DecisionRecorder.GetOccurrenceReceiptCensus();
        SnapshotProvider provider = new SnapshotProvider(empty);
        ContinuationCensusProtocol protocol = CreateProtocol(provider, OwnerSectionRole.Required);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out _), Is.True);

        RecordOccurrence(records.DecisionRecorder, "revision-advance-receipt");
        provider.Witness = records.DecisionRecorder.GetOccurrenceReceiptCensus();
        Assert.That(protocol.NotifyCommittedMutation(ReceiptSectionId, out ContinuationCensusFailure notified), Is.True,
            notified.ToString());
        Assert.That(protocol.TryAssessOwnerSectionInventory(out _), Is.True);

        provider.Witness = empty;
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure rollback), Is.False);
        Assert.That(rollback, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure faulted), Is.False);
        Assert.That(faulted, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void ExplicitlyEmptyOwnerRejectsPopulatedSection()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.ExplicitlyEmpty);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out _), Is.True);
        RecordOccurrence(records.DecisionRecorder, "unexpected-section-receipt");

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void ExcludedOwnerMustStillSupplyAnExactZeroWitness()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.Excluded);

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
    }

    [Test]
    public void OwnerCensusCannotBeAssessedWhileRegisteredOperationIsActive()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(ReceiptSectionId, records.DecisionRecorder, out _), Is.True);
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out _), Is.True);

        Assert.That(protocol.TryEnterOperation("runtime.advance", out SimulationOperationScope scope, out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure busy), Is.False);
        Assert.That(busy, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));
        scope.Dispose();
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure idle), Is.True,
            idle.ToString());
    }

    [Test]
    public void SchemaOrOwnerInstanceMismatchFailsClosed()
    {
        RecordFixture first = SimulationTestFactory.CreateRecordFixture();
        RecordFixture replacement = SimulationTestFactory.CreateRecordFixture();
        SwitchingProvider provider = new SwitchingProvider(first.DecisionRecorder);
        ContinuationCensusProtocol wrongSchema = new ContinuationCensusProtocol();
        Assert.That(wrongSchema.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion + 1,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(wrongSchema.RegisterCensusProvider(ReceiptSectionId, provider, out _), Is.True);
        Assert.That(wrongSchema.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(wrongSchema.SealCensusProviderInventory(out _), Is.True);
        Assert.That(wrongSchema.RegisterExpectedOperation("bootstrap.publish", out _), Is.True);
        Assert.That(wrongSchema.SealOperationInventory(out _), Is.True);
        Assert.That(wrongSchema.BindOwnerThread(out _), Is.True);
        Assert.That(wrongSchema.TryAssessOwnerSectionInventory(out ContinuationCensusFailure schemaFailure), Is.False);
        Assert.That(schemaFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));

        ContinuationCensusProtocol changedOwner = CreateProtocol(provider, OwnerSectionRole.Required);
        Assert.That(changedOwner.TryAssessOwnerSectionInventory(out _), Is.True);
        provider.Current = replacement.DecisionRecorder;
        Assert.That(changedOwner.TryAssessOwnerSectionInventory(out ContinuationCensusFailure identityFailure), Is.False);
        Assert.That(identityFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void DuplicateAndUnexpectedSectionRegistrationsFaultProtocol()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol duplicate = new ContinuationCensusProtocol();
        OwnerSectionContract contract = new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion,
            OwnerSectionRole.Required);
        Assert.That(duplicate.RegisterExpectedSection(contract, out _), Is.True);
        Assert.That(duplicate.RegisterExpectedSection(contract, out ContinuationCensusFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(duplicate.BindOwnerThread(out _), Is.False);

        ContinuationCensusProtocol duplicateProvider = new ContinuationCensusProtocol();
        Assert.That(duplicateProvider.RegisterExpectedSection(contract, out _), Is.True);
        Assert.That(duplicateProvider.RegisterCensusProvider(ReceiptSectionId, records.DecisionRecorder, out _), Is.True);
        Assert.That(duplicateProvider.RegisterCensusProvider(ReceiptSectionId, records.DecisionRecorder,
            out ContinuationCensusFailure duplicateProviderFailure), Is.False);
        Assert.That(duplicateProviderFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));

        ContinuationCensusProtocol extraProvider = new ContinuationCensusProtocol();
        Assert.That(extraProvider.RegisterExpectedSection(contract, out _), Is.True);
        Assert.That(extraProvider.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(extraProvider.RegisterCensusProvider("unexpected-section", records.DecisionRecorder,
            out ContinuationCensusFailure extraFailure), Is.False);
        Assert.That(extraFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(extraProvider.BindOwnerThread(out _), Is.False);
    }

    [Test]
    public void RegisteredNestedOperationsAreCountedUntilEveryScopeCloses()
    {
        ContinuationCensusProtocol protocol = CreateOperationProtocol();
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure idle), Is.True,
            idle.ToString());
        Assert.That(protocol.TryEnterOperation("bootstrap.publish", out SimulationOperationScope outer, out _), Is.True);
        Assert.That(protocol.TryEnterOperation("runtime.advance", out SimulationOperationScope inner, out _), Is.True);
        Assert.That(protocol.TryReadActiveOperationCount(out int active, out _), Is.True);
        Assert.That(active, Is.EqualTo(2));
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure busy), Is.False);
        Assert.That(busy, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));

        inner.Dispose();
        inner.Dispose();
        Assert.That(protocol.TryReadActiveOperationCount(out active, out _), Is.True);
        Assert.That(active, Is.EqualTo(1));
        outer.Dispose();
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure idleAgain), Is.True,
            idleAgain.ToString());
    }

    [Test]
    public void UnregisteredOperationCannotEnterAfterInventorySeal()
    {
        ContinuationCensusProtocol protocol = CreateOperationProtocol();
        Assert.That(protocol.TryEnterOperation("unknown.operation", out SimulationOperationScope scope,
            out ContinuationCensusFailure failure), Is.False);
        Assert.That(scope, Is.Null);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure faulted), Is.False);
        Assert.That(faulted, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void OffThreadOperationEntryFaultsWithoutClaimingQuiescence()
    {
        ContinuationCensusProtocol protocol = CreateOperationProtocol();
        bool entered = true;
        ContinuationCensusFailure workerFailure = ContinuationCensusFailure.None;
        Thread worker = new Thread(() => entered = protocol.TryEnterOperation(
            "runtime.advance", out _, out workerFailure));
        worker.Start();
        worker.Join();

        Assert.That(entered, Is.False);
        Assert.That(workerFailure, Is.EqualTo(ContinuationCensusFailure.WrongOwnerThread));
        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure ownerFailure), Is.False);
        Assert.That(ownerFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void OffThreadScopeDisposalLeavesProtocolFaultedAndCountedActive()
    {
        ContinuationCensusProtocol protocol = CreateOperationProtocol();
        Assert.That(protocol.TryEnterOperation("runtime.advance", out SimulationOperationScope scope, out _), Is.True);
        Thread worker = new Thread(scope.Dispose);
        worker.Start();
        worker.Join();

        Assert.That(protocol.TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void OwnerThreadBindingIsOneShotAndSetupCannotChangeAfterBinding()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol rebind = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.Required);
        Assert.That(rebind.BindOwnerThread(out ContinuationCensusFailure rebindFailure), Is.False);
        Assert.That(rebindFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));

        ContinuationCensusProtocol protocol = CreateProtocol(records.DecisionRecorder, OwnerSectionRole.Required);
        Assert.That(protocol.RegisterExpectedOperation("late.operation", out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static ContinuationCensusProtocol CreateProtocol(
        IOwnerSectionCensusProvider provider,
        OwnerSectionRole role)
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion, role), out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(ReceiptSectionId, provider, out _), Is.True);
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("bootstrap.publish", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        return protocol;
    }

    private static ContinuationCensusProtocol CreateOperationProtocol()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterExpectedSection(new OwnerSectionContract(
            ReceiptSectionId, NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion,
            OwnerSectionRole.Required), out _), Is.True);
        Assert.That(protocol.RegisterCensusProvider(ReceiptSectionId, records.DecisionRecorder, out _), Is.True);
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("bootstrap.publish", out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        return protocol;
    }

    private static void RecordOccurrence(NpcDecisionRecorder recorder, string operationIdentity)
    {
        MethodInfo method = typeof(NpcDecisionRecorder).GetMethod(
            "TryRecordOccurrenceOnce", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments =
        {
            operationIdentity,
            "fingerprint-" + operationIdentity,
            "actor-runtime",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            null,
            null,
            null,
            null
        };
        Assert.That((bool)method.Invoke(recorder, arguments), Is.True);
    }

    private sealed class SwitchingProvider : IOwnerSectionCensusProvider
    {
        public IOwnerSectionCensusProvider Current { get; set; }

        public SwitchingProvider(IOwnerSectionCensusProvider current)
        {
            Current = current;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => Current.GetCurrentCensus();
    }

    private sealed class SnapshotProvider : IOwnerSectionCensusProvider
    {
        public OwnerSectionCensusWitness Witness { get; set; }

        public SnapshotProvider(OwnerSectionCensusWitness witness)
        {
            Witness = witness;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => Witness;
    }
}
