using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class P12PoliticalClaimCensusTests
{
    [Test]
    public void ProvidersExposeSeparateCardinalitiesForOneExactOwnerAndSharedRevision()
    {
        PoliticalClaimStore owner = new PoliticalClaimStore();
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PoliticalClaimStoreCensusProvider.CreateProviders(owner);

        Assert.That(providers, Has.Count.EqualTo(2));
        AssertWitnesses(providers, owner, 0, 0, 0L);

        PoliticalClaimRecord claim = CreateClaim("p12-claim-provider");
        Assert.That(owner.TryRegister(claim, out PoliticalClaimFailure registerFailure), Is.True,
            registerFailure.ToString());
        AssertWitnesses(providers, owner, 1, 0, 1L);

        PoliticalClaimRecognitionRecord recognition = new PoliticalClaimRecognitionRecord(
            claim.ClaimId,
            new InstitutionId("p12-claim-provider-institution"),
            PoliticalClaimRecognitionState.Recognized,
            0L,
            "provider fixture");
        Assert.That(owner.TryRegisterRecognition(recognition, out PoliticalClaimFailure recognitionFailure), Is.True,
            recognitionFailure.ToString());
        AssertWitnesses(providers, owner, 1, 1, 2L);
        AssertWitnesses(providers, owner, 1, 1, 2L);
    }

    [Test]
    public void DailyProfileTracksSuccessfulFacadeCommitsAndLeavesRejectedCommitsUnchanged()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime claimant, out InstitutionId institutionId);
        PoliticalClaimStore owner = GetPoliticalClaimStore(runtime);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PoliticalClaimStoreCensusProvider.CreateProviders(owner);
        AssertRuntimeState(runtime, providers, 0, 0, 0L, 1L);

        PoliticalClaimRecord claim = CreateClaim("p12-claim-runtime", claimant.PersonId);
        Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure registerFailure), Is.True,
            registerFailure.ToString());
        AssertRuntimeState(runtime, providers, 1, 0, 1L, 2L);

        Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PoliticalClaimFailureCode.DuplicateClaimId));
        AssertRuntimeState(runtime, providers, 1, 0, 1L, 2L);

        Assert.That(runtime.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Recognized,
            "runtime fixture",
            out PoliticalClaimRecognitionTransition recognition,
            out PoliticalClaimFailure recognitionProposalFailure), Is.True,
            recognitionProposalFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalClaimRecognition(recognition, out PoliticalClaimFailure recognitionFailure),
            Is.True, recognitionFailure.ToString());
        AssertRuntimeState(runtime, providers, 1, 1, 2L, 3L);

        Assert.That(runtime.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Contested,
            "replacement fixture",
            out PoliticalClaimRecognitionTransition replacement,
            out PoliticalClaimFailure replacementProposalFailure), Is.True,
            replacementProposalFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalClaimRecognition(replacement, out PoliticalClaimFailure replacementFailure),
            Is.True, replacementFailure.ToString());
        AssertRuntimeState(runtime, providers, 1, 1, 3L, 4L);

        Assert.That(runtime.TryProposePoliticalClaimResolution(
            claim.ClaimId,
            PoliticalClaimStatus.Resolved,
            out PoliticalClaimResolutionTransition resolution,
            out PoliticalClaimFailure resolutionProposalFailure), Is.True,
            resolutionProposalFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalClaimResolution(resolution, out PoliticalClaimFailure resolutionFailure),
            Is.True, resolutionFailure.ToString());
        AssertRuntimeState(runtime, providers, 1, 1, 4L, 5L);

        Assert.That(runtime.TryApplyPoliticalClaimResolution(resolution, out PoliticalClaimFailure staleResolutionFailure),
            Is.False);
        Assert.That(staleResolutionFailure.Code, Is.EqualTo(PoliticalClaimFailureCode.StaleClaim));
        AssertRuntimeState(runtime, providers, 1, 1, 4L, 5L);
    }

    [Test]
    public void DailyProfileRejectsPoliticalClaimCommitOffOwnerThreadBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime claimant, out _);
        bool result = true;
        PoliticalClaimFailure failure = null;
        Thread wrongThread = new Thread(() =>
        {
            result = runtime.TryRegisterPoliticalClaim(
                CreateClaim("p12-claim-wrong-thread", claimant.PersonId),
                out failure);
        });

        wrongThread.Start();
        wrongThread.Join();

        Assert.That(result, Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalClaimStore(runtime).Count, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsPoliticalClaimCommitWhenSharedEpochIsExhausted()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime claimant, out _);
        SetProtocolMutationEpoch(runtime, long.MaxValue);

        Assert.That(runtime.TryRegisterPoliticalClaim(
            CreateClaim("p12-claim-epoch-full", claimant.PersonId),
            out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalClaimStore(runtime).Count, Is.Zero);
        Assert.That(GetPoliticalClaimStore(runtime).Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsPoliticalClaimCommitAgainstStaleSectionBaselineBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime claimant, out _);
        SetSectionBaselineRevision(runtime, PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1L);

        Assert.That(runtime.TryRegisterPoliticalClaim(
            CreateClaim("p12-claim-stale-baseline", claimant.PersonId),
            out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.RuntimeFaulted));
        Assert.That(GetPoliticalClaimStore(runtime).Count, Is.Zero);
        Assert.That(GetPoliticalClaimStore(runtime).Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsPoliticalClaimCommitAtLocalRevisionOverflowWithoutEpochAdvance()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime(out PersonRuntime claimant, out _);
        PoliticalClaimStore owner = GetPoliticalClaimStore(runtime);
        SetPoliticalClaimStoreRevision(owner, long.MaxValue);
        SetSectionBaselineRevision(runtime, PoliticalClaimStoreCensusProvider.ClaimsSectionId, long.MaxValue);
        SetSectionBaselineRevision(runtime, PoliticalClaimStoreCensusProvider.RecognitionsSectionId, long.MaxValue);

        Assert.That(runtime.TryRegisterPoliticalClaim(
            CreateClaim("p12-claim-revision-full", claimant.PersonId),
            out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.RevisionOverflow));
        AssertRuntimeState(runtime, PoliticalClaimStoreCensusProvider.CreateProviders(owner),
            0, 0, long.MaxValue, 1L);
    }

    private static void AssertWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PoliticalClaimStore expectedOwner,
        int claimCount,
        int recognitionCount,
        long revision)
    {
        OwnerSectionCensusWitness claims = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness recognitions = providers[1].GetCurrentCensus();
        Assert.That(claims.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
        Assert.That(recognitions.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
        Assert.That(claims.SchemaVersion, Is.EqualTo(PoliticalClaimStoreCensusProvider.SchemaVersion));
        Assert.That(recognitions.SchemaVersion, Is.EqualTo(PoliticalClaimStoreCensusProvider.SchemaVersion));
        Assert.That(claims.Cardinality, Is.EqualTo(claimCount));
        Assert.That(recognitions.Cardinality, Is.EqualTo(recognitionCount));
        Assert.That(claims.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(recognitions.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(claims.Revision, Is.EqualTo(revision));
        Assert.That(recognitions.Revision, Is.EqualTo(revision));
    }

    private static void AssertRuntimeState(
        SimulationRuntime runtime,
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        int claimCount,
        int recognitionCount,
        long revision,
        long epoch)
    {
        AssertWitnesses(providers, GetPoliticalClaimStore(runtime), claimCount, recognitionCount, revision);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actualEpoch, out ContinuationCensusFailure epochFailure), Is.True, epochFailure.ToString());
        Assert.That(actualEpoch, Is.EqualTo(epoch));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure),
            Is.True, censusFailure.ToString());
    }

    private static SimulationRuntime CreateDailyProfileRuntime(
        out PersonRuntime claimant,
        out InstitutionId institutionId)
    {
        PersonStore people = new PersonStore();
        claimant = new PersonRuntime(new PersonId("p12-claim-daily-person"), 0L);
        Assert.That(people.TryRegister(claimant, out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());

        InstitutionStore institutions = new InstitutionStore();
        institutionId = new InstitutionId("p12-claim-daily-institution");
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            personStore: people,
            factionStore: new FactionStore(people),
            institutionStore: institutions,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(institutionId, "Daily institution"),
            out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        return runtime;
    }

    private static PoliticalClaimRecord CreateClaim(string id, PersonId claimant = null)
    {
        PersonId resolvedClaimant = claimant ?? new PersonId("p12-claim-provider-person");
        return new PoliticalClaimRecord(
            new PoliticalClaimId(id),
            resolvedClaimant,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(resolvedClaimant),
            PoliticalClaimBasis.Other,
            "census fixture",
            0L,
            null);
    }

    private static PoliticalClaimStore GetPoliticalClaimStore(SimulationRuntime runtime)
    {
        FieldInfo field = typeof(SimulationRuntime).GetField("politicalClaimStore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (PoliticalClaimStore)field.GetValue(runtime);
    }

    private static void SetProtocolMutationEpoch(SimulationRuntime runtime, long epoch)
    {
        object protocol = GetProtocol(runtime);
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField(
            "mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(protocol, epoch);
    }

    private static void SetPoliticalClaimStoreRevision(PoliticalClaimStore owner, long revision)
    {
        FieldInfo field = typeof(PoliticalClaimStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(owner, revision);
    }

    private static void SetSectionBaselineRevision(SimulationRuntime runtime, string sectionId, long revision)
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
