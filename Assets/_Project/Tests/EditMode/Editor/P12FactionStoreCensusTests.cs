using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class P12FactionStoreCensusTests
{
    [Test]
    public void ProvidersExposeSeparateCountsForOneExactOwnerAndSharedRevision()
    {
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("p12-faction-provider-person"), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());
        FactionStore store = new FactionStore(people);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            FactionStoreCensusProvider.CreateProviders(store);

        Assert.That(providers, Has.Count.EqualTo(2));
        AssertFactionWitnesses(providers, store, 0, 0, 0L);

        FactionId factionId = new FactionId("p12-faction-provider-faction");
        Assert.That(store.TryRegister(new FactionRecord(factionId, "Faction", 0L), out FactionFoundationFailure registerFailure),
            Is.True, registerFailure.ToString());
        AssertFactionWitnesses(providers, store, 1, 0, 1L);

        Assert.That(store.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, person.PersonId, 0L),
            out FactionFoundationFailure affiliationFailure), Is.True, affiliationFailure.ToString());
        AssertFactionWitnesses(providers, store, 1, 1, 2L);
        AssertFactionWitnesses(providers, store, 1, 1, 2L);
    }

    [Test]
    public void DailyProfileTracksEverySuccessfulFacadeCommitAndLeavesRejectedCommitsUnchanged()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime sourcePerson);
        FactionStore owner = GetFactionStore(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = FactionStoreCensusProvider.CreateProviders(owner);
        AssertRuntimeFactionState(runtime, providers, 0, 0, 0L, 0L);

        FactionId factionId = new FactionId("p12-faction-runtime-faction");
        FactionRecord faction = new FactionRecord(factionId, "Runtime faction", 0L);
        Assert.That(runtime.TryRegisterFaction(faction, out FactionFoundationFailure registerFailure),
            Is.True, registerFailure.ToString());
        AssertRuntimeFactionState(runtime, providers, 1, 0, 1L, 1L);

        Assert.That(runtime.TryRegisterFaction(faction, out FactionFoundationFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(FactionFoundationFailureCode.DuplicateFactionId));
        AssertRuntimeFactionState(runtime, providers, 1, 0, 1L, 1L);

        Assert.That(runtime.TryProposeFactionAffiliation(
            factionId,
            sourcePerson.PersonId,
            out FactionAffiliationAddTransition add,
            out FactionFoundationFailure addProposalFailure), Is.True, addProposalFailure.ToString());
        AssertRuntimeFactionState(runtime, providers, 1, 0, 1L, 1L);

        Assert.That(runtime.TryApplyFactionAffiliation(add, out FactionFoundationFailure addFailure),
            Is.True, addFailure.ToString());
        AssertRuntimeFactionState(runtime, providers, 1, 1, 2L, 2L);

        Assert.That(runtime.TryApplyFactionAffiliation(add, out FactionFoundationFailure staleAddFailure), Is.False);
        Assert.That(staleAddFailure.Code, Is.EqualTo(FactionFoundationFailureCode.StaleAffiliation));
        AssertRuntimeFactionState(runtime, providers, 1, 1, 2L, 2L);

        Assert.That(runtime.TryProposeFactionAffiliationEnd(
            factionId,
            sourcePerson.PersonId,
            out FactionAffiliationEndTransition end,
            out FactionFoundationFailure endProposalFailure), Is.True, endProposalFailure.ToString());
        Assert.That(runtime.TryApplyFactionAffiliationEnd(end, out FactionFoundationFailure endFailure),
            Is.True, endFailure.ToString());
        AssertRuntimeFactionState(runtime, providers, 1, 1, 3L, 3L);

        Assert.That(runtime.TryApplyFactionAffiliationEnd(end, out FactionFoundationFailure staleEndFailure), Is.False);
        Assert.That(staleEndFailure.Code, Is.EqualTo(FactionFoundationFailureCode.StaleAffiliation));
        AssertRuntimeFactionState(runtime, providers, 1, 1, 3L, 3L);
    }

    [Test]
    public void DailyProfileRejectsFactionCommitOffOwnerThreadBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out _);
        bool result = true;
        FactionFoundationFailure failure = null;
        Thread wrongThread = new Thread(() =>
        {
            result = runtime.TryRegisterFaction(
                new FactionRecord(new FactionId("p12-faction-wrong-thread"), "Wrong thread", 0L),
                out failure);
        });

        wrongThread.Start();
        wrongThread.Join();

        Assert.That(result, Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(GetFactionStore(runtime).Count, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsFactionCommitWhenSharedEpochIsExhausted()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out _);
        SetProtocolMutationEpoch(runtime, long.MaxValue);

        Assert.That(runtime.TryRegisterFaction(
            new FactionRecord(new FactionId("p12-faction-epoch-full"), "Epoch full", 0L),
            out FactionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(GetFactionStore(runtime).Count, Is.Zero);
        Assert.That(GetFactionStore(runtime).Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsFactionCommitAgainstStaleSectionBaselineBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out _);
        SetFactionSectionBaselineRevision(runtime, FactionStoreCensusProvider.AffiliationsSectionId, 1L);

        Assert.That(runtime.TryRegisterFaction(
            new FactionRecord(new FactionId("p12-faction-stale-baseline"), "Stale baseline", 0L),
            out FactionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(GetFactionStore(runtime).Count, Is.Zero);
        Assert.That(GetFactionStore(runtime).Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static void AssertFactionWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        FactionStore expectedOwner,
        int factionCount,
        int affiliationCount,
        long revision)
    {
        OwnerSectionCensusWitness factions = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness affiliations = providers[1].GetCurrentCensus();
        Assert.That(factions.SectionId, Is.EqualTo(FactionStoreCensusProvider.FactionsSectionId));
        Assert.That(affiliations.SectionId, Is.EqualTo(FactionStoreCensusProvider.AffiliationsSectionId));
        Assert.That(factions.SchemaVersion, Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
        Assert.That(affiliations.SchemaVersion, Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
        Assert.That(factions.Cardinality, Is.EqualTo(factionCount));
        Assert.That(affiliations.Cardinality, Is.EqualTo(affiliationCount));
        Assert.That(factions.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(affiliations.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(factions.Revision, Is.EqualTo(revision));
        Assert.That(affiliations.Revision, Is.EqualTo(revision));
    }

    private static void AssertRuntimeFactionState(
        SimulationRuntime runtime,
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        int factionCount,
        int affiliationCount,
        long revision,
        long epoch)
    {
        AssertFactionWitnesses(providers, GetFactionStore(runtime), factionCount, affiliationCount, revision);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actualEpoch, out ContinuationCensusFailure epochFailure), Is.True, epochFailure.ToString());
        Assert.That(actualEpoch, Is.EqualTo(epoch));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure),
            Is.True, censusFailure.ToString());
    }

    private static SimulationRuntime CreateDailyProfileRuntime(out PersonRuntime sourcePerson)
    {
        PersonStore sourcePeople = new PersonStore();
        sourcePerson = new PersonRuntime(new PersonId("p12-faction-daily-person"), 0L);
        Assert.That(sourcePeople.TryRegister(sourcePerson, out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());
        return new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            personStore: sourcePeople,
            factionStore: new FactionStore(sourcePeople),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static FactionStore GetFactionStore(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField("factionStore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (FactionStore)field.GetValue(runtime);
    }

    private static void SetProtocolMutationEpoch(SimulationRuntime runtime, long epoch)
    {
        object protocol = GetProtocol(runtime);
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField(
            "mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(protocol, epoch);
    }

    private static void SetFactionSectionBaselineRevision(SimulationRuntime runtime, string sectionId, long revision)
    {
        object protocol = GetProtocol(runtime);
        FieldInfo sectionsField = typeof(ContinuationCensusProtocol).GetField(
            "registeredSections", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(sectionsField, Is.Not.Null);
        IDictionary sections = (IDictionary)sectionsField.GetValue(protocol);
        object section = sections[sectionId];
        Assert.That(section, Is.Not.Null);
        FieldInfo revisionField = section.GetType().GetField("LastRevision", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(revisionField, Is.Not.Null);
        revisionField.SetValue(section, revision);
    }

    private static object GetProtocol(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        object protocol = field.GetValue(runtime);
        Assert.That(protocol, Is.Not.Null);
        return protocol;
    }
}
