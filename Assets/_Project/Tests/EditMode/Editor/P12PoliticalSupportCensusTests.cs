using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class P12PoliticalSupportCensusTests
{
    [Test]
    public void ProviderTracksAllRelationRowsAndTheExactOwnersSharedRevision()
    {
        PersonStore people = new PersonStore();
        PersonRuntime source = new PersonRuntime(new PersonId("p12-support-provider-source"), 0L);
        PersonRuntime candidate = new PersonRuntime(new PersonId("p12-support-provider-candidate"), 0L);
        Assert.That(people.TryRegister(source, out PersonStoreFailure sourceFailure), Is.True, sourceFailure.ToString());
        Assert.That(people.TryRegister(candidate, out PersonStoreFailure candidateFailure), Is.True, candidateFailure.ToString());
        PoliticalSupportStore owner = new PoliticalSupportStore(people, new FactionStore(people), new PoliticalClaimStore());
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PoliticalSupportStoreCensusProvider.CreateProviders(owner);

        Assert.That(providers, Has.Count.EqualTo(1));
        AssertWitness(providers, owner, 0, 0L);

        PoliticalSupportRelationRecord relation = CreateRelation(
            "p12-support-provider-relation", source.PersonId, candidate.PersonId, 0L);
        Assert.That(owner.TryRegister(relation, out PoliticalSupportFailure registerFailure),
            Is.True, registerFailure.ToString());
        AssertWitness(providers, owner, 1, 1L);

        Assert.That(owner.TryProposeEnd(
            relation.RelationId,
            1L,
            out PoliticalSupportEndTransition end,
            out PoliticalSupportFailure proposalFailure), Is.True, proposalFailure.ToString());
        AssertWitness(providers, owner, 1, 1L);
        Assert.That(owner.TryApplyEnd(end, 1L, out PoliticalSupportFailure endFailure),
            Is.True, endFailure.ToString());
        AssertWitness(providers, owner, 1, 2L);
        AssertWitness(providers, owner, 1, 2L);
    }

    [Test]
    public void DailyProfileTracksCommitsAndLeavesProposalsAndRejectedWritesUnchanged()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(
            out PersonId sourceId,
            out PersonId firstCandidateId,
            out PersonId secondCandidateId);
        PoliticalSupportStore owner = GetPoliticalSupportStore(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PoliticalSupportStoreCensusProvider.CreateProviders(owner);
        long expectedPoliticalWorldRevision = runtime.PoliticalWorldRevision;
        long expectedEpoch = ReadEpoch(runtime);
        AssertRuntimeState(runtime, providers, owner, 0, 0L, expectedEpoch, expectedPoliticalWorldRevision);

        PoliticalSupportRelationRecord registered = CreateRelation(
            "p12-support-runtime-register", sourceId, firstCandidateId, runtime.CurrentDay);
        Assert.That(runtime.TryRegisterPoliticalSupport(registered, out PoliticalSupportFailure registerFailure),
            Is.True, registerFailure.ToString());
        expectedPoliticalWorldRevision++;
        expectedEpoch++;
        AssertRuntimeState(runtime, providers, owner, 1, 1L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryRegisterPoliticalSupport(registered, out PoliticalSupportFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.DuplicateRelationId));
        AssertRuntimeState(runtime, providers, owner, 1, 1L, expectedEpoch, expectedPoliticalWorldRevision);

        PoliticalSupportRelationRecord future = CreateRelation(
            "p12-support-runtime-future", sourceId, secondCandidateId, runtime.CurrentDay + 1L);
        Assert.That(runtime.TryRegisterPoliticalSupport(future, out PoliticalSupportFailure futureFailure), Is.False);
        Assert.That(futureFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleWorldDay));
        AssertRuntimeState(runtime, providers, owner, 1, 1L, expectedEpoch, expectedPoliticalWorldRevision);

        PoliticalSupportRelationRecord addRecord = CreateRelation(
            "p12-support-runtime-add", sourceId, secondCandidateId, runtime.CurrentDay);
        Assert.That(runtime.TryProposePoliticalSupportAdd(
            addRecord,
            out PoliticalSupportAddTransition add,
            out PoliticalSupportFailure addProposalFailure), Is.True, addProposalFailure.ToString());
        AssertRuntimeState(runtime, providers, owner, 1, 1L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryApplyPoliticalSupportAdd(add, out PoliticalSupportFailure addFailure),
            Is.True, addFailure.ToString());
        expectedPoliticalWorldRevision++;
        expectedEpoch++;
        AssertRuntimeState(runtime, providers, owner, 2, 2L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryApplyPoliticalSupportAdd(add, out PoliticalSupportFailure staleAddFailure), Is.False);
        Assert.That(staleAddFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleRelation));
        AssertRuntimeState(runtime, providers, owner, 2, 2L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryProposePoliticalSupportEnd(
            registered.RelationId,
            out PoliticalSupportEndTransition end,
            out PoliticalSupportFailure endProposalFailure), Is.True, endProposalFailure.ToString());
        AssertRuntimeState(runtime, providers, owner, 2, 2L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryApplyPoliticalSupportEnd(end, out PoliticalSupportFailure endFailure),
            Is.True, endFailure.ToString());
        expectedPoliticalWorldRevision++;
        expectedEpoch++;
        AssertRuntimeState(runtime, providers, owner, 2, 3L, expectedEpoch, expectedPoliticalWorldRevision);

        Assert.That(runtime.TryApplyPoliticalSupportEnd(end, out PoliticalSupportFailure staleEndFailure), Is.False);
        Assert.That(staleEndFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleRelation));
        AssertRuntimeState(runtime, providers, owner, 2, 3L, expectedEpoch, expectedPoliticalWorldRevision);
    }

    [Test]
    public void DailyProfileRejectsTransitionFromAnotherSupportStoreWithoutMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(
            out PersonId sourceId,
            out _,
            out PersonId candidateId);
        SimulationRuntime otherRuntime = CreateDailyProfileRuntime(
            out PersonId otherSourceId,
            out _,
            out PersonId otherCandidateId);
        PoliticalSupportRelationRecord relation = CreateRelation(
            "p12-support-wrong-store", sourceId, candidateId, runtime.CurrentDay);
        Assert.That(otherSourceId, Is.EqualTo(sourceId));
        Assert.That(otherCandidateId, Is.EqualTo(candidateId));
        Assert.That(otherRuntime.TryProposePoliticalSupportAdd(
            relation,
            out PoliticalSupportAddTransition transition,
            out PoliticalSupportFailure proposalFailure), Is.True, proposalFailure.ToString());

        PoliticalSupportStore owner = GetPoliticalSupportStore(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PoliticalSupportStoreCensusProvider.CreateProviders(owner);
        long politicalWorldRevision = runtime.PoliticalWorldRevision;
        long epoch = ReadEpoch(runtime);
        Assert.That(runtime.TryApplyPoliticalSupportAdd(transition, out PoliticalSupportFailure applyFailure), Is.False);
        Assert.That(applyFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.WrongSupportStore));
        AssertRuntimeState(runtime, providers, owner, 0, 0L, epoch, politicalWorldRevision);
    }

    [Test]
    public void DailyProfileKeepsRevisionOverflowRejectionOutsideTheMutationEpoch()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(
            out PersonId sourceId,
            out _,
            out PersonId candidateId);
        PoliticalSupportStore owner = GetPoliticalSupportStore(runtime);
        long politicalWorldRevision = runtime.PoliticalWorldRevision;
        long epoch = ReadEpoch(runtime);
        SetStoreRevision(owner, long.MaxValue);
        SetSectionBaselineRevision(runtime, long.MaxValue);

        Assert.That(runtime.TryRegisterPoliticalSupport(
            CreateRelation("p12-support-overflow", sourceId, candidateId, runtime.CurrentDay),
            out PoliticalSupportFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalSupportFailureCode.RevisionOverflow));
        AssertRuntimeState(runtime, PoliticalSupportStoreCensusProvider.CreateProviders(owner),
            owner, 0, long.MaxValue, epoch, politicalWorldRevision);
    }

    [Test]
    public void DailyProfileRejectsSupportCommitOffOwnerThreadBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(
            out PersonId sourceId,
            out _,
            out PersonId candidateId);
        bool result = true;
        PoliticalSupportFailure failure = null;
        Thread wrongThread = new Thread(() =>
        {
            result = runtime.TryRegisterPoliticalSupport(
                CreateRelation("p12-support-wrong-thread", sourceId, candidateId, runtime.CurrentDay),
                out failure);
        });

        wrongThread.Start();
        wrongThread.Join();

        Assert.That(result, Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalSupportFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalSupportStore(runtime).Count, Is.Zero);
        Assert.That(GetPoliticalSupportStore(runtime).Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileFailsClosedOnExhaustedEpochOrStaleSectionBaselineBeforeMutation()
    {
        SimulationRuntime exhaustedEpochRuntime = CreateDailyProfileRuntime(
            out PersonId epochSourceId,
            out _,
            out PersonId epochCandidateId);
        SetProtocolMutationEpoch(exhaustedEpochRuntime, long.MaxValue);
        Assert.That(exhaustedEpochRuntime.TryRegisterPoliticalSupport(
            CreateRelation("p12-support-epoch-full", epochSourceId, epochCandidateId, 0L),
            out PoliticalSupportFailure epochFailure), Is.False);
        Assert.That(epochFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalSupportStore(exhaustedEpochRuntime).Count, Is.Zero);
        Assert.That(GetPoliticalSupportStore(exhaustedEpochRuntime).Revision, Is.Zero);
        Assert.That(exhaustedEpochRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure epochCensusFailure), Is.False);
        Assert.That(epochCensusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));

        SimulationRuntime staleBaselineRuntime = CreateDailyProfileRuntime(
            out PersonId staleSourceId,
            out _,
            out PersonId staleCandidateId);
        SetSectionBaselineRevision(staleBaselineRuntime, 1L);
        Assert.That(staleBaselineRuntime.TryRegisterPoliticalSupport(
            CreateRelation("p12-support-stale-baseline", staleSourceId, staleCandidateId, 0L),
            out PoliticalSupportFailure baselineFailure), Is.False);
        Assert.That(baselineFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalSupportStore(staleBaselineRuntime).Count, Is.Zero);
        Assert.That(GetPoliticalSupportStore(staleBaselineRuntime).Revision, Is.Zero);
        Assert.That(staleBaselineRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure baselineCensusFailure), Is.False);
        Assert.That(baselineCensusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static void AssertWitness(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PoliticalSupportStore expectedOwner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = providers[0].GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(PoliticalSupportStoreCensusProvider.RelationsSectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(PoliticalSupportStoreCensusProvider.SchemaVersion));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static void AssertRuntimeState(
        SimulationRuntime runtime,
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PoliticalSupportStore owner,
        int cardinality,
        long revision,
        long epoch,
        long politicalWorldRevision)
    {
        AssertWitness(providers, owner, cardinality, revision);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actualEpoch, out ContinuationCensusFailure epochFailure), Is.True, epochFailure.ToString());
        Assert.That(actualEpoch, Is.EqualTo(epoch));
        Assert.That(runtime.PoliticalWorldRevision, Is.EqualTo(politicalWorldRevision));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure),
            Is.True, censusFailure.ToString());
    }

    private static SimulationRuntime CreateDailyProfileRuntime(
        out PersonId sourceId,
        out PersonId firstCandidateId,
        out PersonId secondCandidateId)
    {
        PersonStore people = new PersonStore();
        sourceId = new PersonId("p12-support-daily-source");
        firstCandidateId = new PersonId("p12-support-daily-candidate-a");
        secondCandidateId = new PersonId("p12-support-daily-candidate-b");
        Assert.That(people.TryRegister(new PersonRuntime(sourceId, 0L), out PersonStoreFailure sourceFailure),
            Is.True, sourceFailure.ToString());
        Assert.That(people.TryRegister(new PersonRuntime(firstCandidateId, 0L), out PersonStoreFailure firstFailure),
            Is.True, firstFailure.ToString());
        Assert.That(people.TryRegister(new PersonRuntime(secondCandidateId, 0L), out PersonStoreFailure secondFailure),
            Is.True, secondFailure.ToString());

        return new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            personStore: people,
            factionStore: new FactionStore(people),
            politicalClaimStore: new PoliticalClaimStore(),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static PoliticalSupportRelationRecord CreateRelation(
        string relationId,
        PersonId sourceId,
        PersonId candidateId,
        long startedDay)
    {
        return new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId(relationId),
            PoliticalSupportSource.ForPerson(sourceId),
            PoliticalSupportTarget.ForSuccessionCandidate(candidateId),
            PoliticalSupportDisposition.Support,
            startedDay);
    }

    private static PoliticalSupportStore GetPoliticalSupportStore(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            "politicalSupportStore",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (PoliticalSupportStore)field.GetValue(runtime);
    }

    private static void SetStoreRevision(PoliticalSupportStore owner, long revision)
    {
        FieldInfo field = typeof(PoliticalSupportStore).GetField(
            "revision",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(owner, revision);
    }

    private static void SetProtocolMutationEpoch(SimulationRuntime runtime, long epoch)
    {
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField(
            "mutationEpoch",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(GetProtocol(runtime), epoch);
    }

    private static void SetSectionBaselineRevision(SimulationRuntime runtime, long revision)
    {
        IDictionary sections = (IDictionary)typeof(ContinuationCensusProtocol)
            .GetField("registeredSections", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(GetProtocol(runtime));
        object section = sections[PoliticalSupportStoreCensusProvider.RelationsSectionId];
        Assert.That(section, Is.Not.Null);
        FieldInfo field = section.GetType().GetField("LastRevision", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null);
        field.SetValue(section, revision);
    }

    private static long ReadEpoch(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        return epoch;
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)field.GetValue(runtime);
        Assert.That(protocol, Is.Not.Null);
        return protocol;
    }
}
