using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PoliticalClaimFoundationTests
{
    [Test]
    public void ClaimTargetFactoriesPreserveTypedTargetIdentity()
    {
        PoliticalClaimTarget office = PoliticalClaimTarget.ForOffice(new OfficeId("office.crown"));
        PoliticalClaimTarget property = PoliticalClaimTarget.ForProperty(new PropertyId("property.keep"));
        PoliticalClaimTarget institution = PoliticalClaimTarget.ForInstitution(new InstitutionId("institution.council"));
        PoliticalClaimTarget person = PoliticalClaimTarget.ForPerson(new PersonId("person.target"));

        Assert.That(office.Kind, Is.EqualTo(PoliticalClaimTargetKind.Office));
        Assert.That(office.TargetId, Is.EqualTo("office.crown"));
        Assert.That(property.Kind, Is.EqualTo(PoliticalClaimTargetKind.Property));
        Assert.That(institution.Kind, Is.EqualTo(PoliticalClaimTargetKind.Institution));
        Assert.That(person.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
    }

    [Test]
    public void ClaimStoreKeepsClaimsSeparateFromUnderlyingOfficeTruth()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime incumbent = RegisterPerson(persons, "incumbent");
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "chair");
        Assert.That(offices.TryAssignIncumbent(officeId, incumbent.PersonId, 0L, out _), Is.True);

        SimulationRuntime world = CreateWorld(persons, institutions, offices);
        PoliticalClaimRecord claim = CreateOfficeClaim(claimant.PersonId, "claim.office", officeId);

        Assert.That(world.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure failure), Is.True, failure.ToString());
        Assert.That(world.PoliticalClaimRecords, Has.Count.EqualTo(1));
        Assert.That(world.TryGetCurrentOfficeIncumbent(officeId, out PersonId stillIncumbent), Is.True);
        Assert.That(stillIncumbent, Is.EqualTo(incumbent.PersonId));
        Assert.That(world.IsOfficeVacant(officeId), Is.False);
    }

    [Test]
    public void WorldRejectsClaimantAndTargetOutsideItsWorld()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        SimulationRuntime world = CreateWorld(persons);

        PoliticalClaimRecord missingTarget = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.missing"),
            claimant.PersonId,
            PoliticalClaimType.OfficeEntitlement,
            PoliticalClaimTarget.ForOffice(new OfficeId("missing.office")),
            PoliticalClaimBasis.Other,
            "unverified assertion",
            0L,
            new[] { "source:rumor" });

        Assert.That(world.TryRegisterPoliticalClaim(missingTarget, out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.ClaimTargetNotFound));
        Assert.That(world.PoliticalClaimRecords, Is.Empty);

        PoliticalClaimRecord missingClaimant = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.missing-claimant"),
            new PersonId("missing-person"),
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            null,
            0L,
            null);

        Assert.That(world.TryRegisterPoliticalClaim(missingClaimant, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.ClaimantNotRegistered));
    }

    [Test]
    public void WorldRejectsTerminalClaimResolvedInTheFuture()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        SimulationRuntime world = CreateWorld(persons);
        PoliticalClaimRecord futureResolution = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.future-resolution"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            null,
            0L,
            null,
            PoliticalClaimStatus.Resolved,
            PoliticalClaimRecognitionState.Unrecognized,
            null,
            null,
            null,
            1L);

        Assert.That(world.TryRegisterPoliticalClaim(futureResolution, out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay));
        Assert.That(world.PoliticalClaimRecords, Is.Empty);
    }

    [Test]
    public void WorldClonesPoliticalClaimsAndDoesNotExposeMutableStoreAuthority()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        PoliticalClaimStore source = new PoliticalClaimStore();
        PoliticalClaimRecord first = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.first"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            null,
            0L,
            null);
        Assert.That(source.TryRegister(first, out _), Is.True);

        SimulationRuntime world = CreateWorld(persons, politicalClaimStore: source);
        Assert.That(world.PoliticalClaimRecords, Has.Count.EqualTo(1));
        Assert.That(typeof(SimulationRuntime).GetProperty("PoliticalClaimStore"), Is.Null);

        Assert.That(source.TryRegister(
            new PoliticalClaimRecord(
                new PoliticalClaimId("claim.second"),
                claimant.PersonId,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(claimant.PersonId),
                PoliticalClaimBasis.Other,
                null,
                0L,
                null),
            out _), Is.True);
        Assert.That(source.Records, Has.Count.EqualTo(2));
        Assert.That(world.PoliticalClaimRecords, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecognitionIsExplicitInstitutionalStateAndStaleTransitionsAreRejected()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        SimulationRuntime world = CreateWorld(persons, institutions);
        PoliticalClaimRecord claim = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.status"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.ExplicitDecision,
            "council petition",
            0L,
            new[] { "petition:one", "petition:one" });

        Assert.That(claim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
        Assert.That((int)claim.ClaimType, Is.EqualTo((int)PoliticalClaimType.StatusRecognition));
        Assert.That((int)claim.Target.Kind, Is.EqualTo((int)PoliticalClaimTargetKind.Person));
        Assert.That(PoliticalClaimRecord.IsTargetCompatible(claim.ClaimType, claim.Target.Kind), Is.True);
        Assert.That(world.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure registrationFailure), Is.True, registrationFailure.ToString());
        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Recognized,
            "explicit council recognition",
            out PoliticalClaimRecognitionTransition transition,
            out PoliticalClaimFailure proposalFailure), Is.True, proposalFailure.ToString());

        Assert.That(world.TryRegisterPoliticalClaim(
            new PoliticalClaimRecord(
                new PoliticalClaimId("claim.second"),
                claimant.PersonId,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(claimant.PersonId),
                PoliticalClaimBasis.Other,
                null,
                0L,
                null),
            out _), Is.True);

        Assert.That(world.TryApplyPoliticalClaimRecognition(transition, out PoliticalClaimFailure staleFailure), Is.False);
        Assert.That(staleFailure.Code, Is.EqualTo(PoliticalClaimFailureCode.StaleClaim));

        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Recognized,
            "explicit council recognition",
            out transition,
            out proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(world.TryApplyPoliticalClaimRecognition(transition, out PoliticalClaimFailure applyFailure), Is.True, applyFailure.ToString());
        PoliticalClaimRecord recognizedClaim = GetClaim(world, claim.ClaimId);
        Assert.That(recognizedClaim, Is.Not.Null);
        Assert.That(world.PoliticalClaimRecognitionRecords, Has.Count.EqualTo(1));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].State, Is.EqualTo(PoliticalClaimRecognitionState.Recognized));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].InstitutionId, Is.EqualTo(institutionId));
        Assert.That(recognizedClaim.EvidenceReferences, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecognitionProposalBecomesStaleWhenWorldDayAdvances()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        SimulationRuntime world = CreateWorld(persons, institutions);
        PoliticalClaimRecord claim = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.day-stale"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            null,
            0L,
            null);
        Assert.That(world.TryRegisterPoliticalClaim(claim, out _), Is.True);
        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Recognized,
            "same-day petition",
            out PoliticalClaimRecognitionTransition transition,
            out _), Is.True);

        world.SimulationTime.AdvanceDay();

        Assert.That(world.TryApplyPoliticalClaimRecognition(transition, out PoliticalClaimFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PoliticalClaimFailureCode.StaleClaim));
    }

    [Test]
    public void ResolutionDoesNotRewriteClaimRecognitionOrUnderlyingTruth()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        SimulationRuntime world = CreateWorld(persons, institutions);
        PoliticalClaimRecord claim = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.resolve"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            null,
            0L,
            null);
        Assert.That(world.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure registrationFailure), Is.True, registrationFailure?.ToString());
        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Contested,
            "competing petition",
            out PoliticalClaimRecognitionTransition recognition,
            out _), Is.True);
        Assert.That(world.TryApplyPoliticalClaimRecognition(recognition, out _), Is.True);
        Assert.That(world.TryProposePoliticalClaimResolution(
            claim.ClaimId,
            PoliticalClaimStatus.Resolved,
            out PoliticalClaimResolutionTransition resolution,
            out PoliticalClaimFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(world.TryApplyPoliticalClaimResolution(resolution, out PoliticalClaimFailure applyFailure), Is.True, applyFailure.ToString());

        PoliticalClaimRecord resolvedClaim = GetClaim(world, claim.ClaimId);
        Assert.That(resolvedClaim, Is.Not.Null);
        Assert.That(resolvedClaim.Status, Is.EqualTo(PoliticalClaimStatus.Resolved));
        Assert.That(resolvedClaim.ResolutionAbsoluteDay, Is.EqualTo(0L));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].State, Is.EqualTo(PoliticalClaimRecognitionState.Contested));
    }

    [Test]
    public void OneClaimCanHaveIndependentInstitutionScopedRecognitionRelations()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId firstInstitution = RegisterInstitution(institutions, "institution.first");
        InstitutionId secondInstitution = RegisterInstitution(institutions, "institution.second");
        SimulationRuntime world = CreateWorld(persons, institutions);
        PoliticalClaimRecord claim = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.shared"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.ExplicitDecision,
            "shared claim",
            0L,
            null);
        Assert.That(world.TryRegisterPoliticalClaim(claim, out _), Is.True);

        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            firstInstitution,
            PoliticalClaimRecognitionState.Recognized,
            "first recognizes",
            out PoliticalClaimRecognitionTransition firstTransition,
            out _), Is.True);
        Assert.That(world.TryApplyPoliticalClaimRecognition(firstTransition, out _), Is.True);
        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            secondInstitution,
            PoliticalClaimRecognitionState.Rejected,
            "second rejects",
            out PoliticalClaimRecognitionTransition secondTransition,
            out _), Is.True);
        Assert.That(world.TryApplyPoliticalClaimRecognition(secondTransition, out _), Is.True);

        Assert.That(world.PoliticalClaimRecords, Has.Count.EqualTo(1));
        Assert.That(world.PoliticalClaimRecognitionRecords, Has.Count.EqualTo(2));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].InstitutionId, Is.EqualTo(firstInstitution));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].State, Is.EqualTo(PoliticalClaimRecognitionState.Recognized));
        Assert.That(world.PoliticalClaimRecognitionRecords[1].InstitutionId, Is.EqualTo(secondInstitution));
        Assert.That(world.PoliticalClaimRecognitionRecords[1].State, Is.EqualTo(PoliticalClaimRecognitionState.Rejected));

        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            firstInstitution,
            PoliticalClaimRecognitionState.Contested,
            "first contests later",
            out PoliticalClaimRecognitionTransition update,
            out _), Is.True);
        Assert.That(world.TryApplyPoliticalClaimRecognition(update, out _), Is.True);
        Assert.That(world.PoliticalClaimRecognitionRecords[0].State, Is.EqualTo(PoliticalClaimRecognitionState.Contested));
        Assert.That(world.PoliticalClaimRecognitionRecords[0].History, Has.Count.EqualTo(2));
        Assert.That(world.PoliticalClaimRecognitionRecords[1].State, Is.EqualTo(PoliticalClaimRecognitionState.Rejected));
    }

    [Test]
    public void ClaimKnowledgeKeepsInstitutionRecognitionPerspectivesSeparate()
    {
        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(new PersonId("person.reader"));
        PoliticalClaimId claimId = new PoliticalClaimId("claim.knowledge-perspectives");
        PoliticalClaimKnowledgeObservation first = CreateClaimKnowledgeObservation(
            claimId,
            new InstitutionId("institution.first"),
            PoliticalClaimRecognitionState.Recognized,
            10L);
        PoliticalClaimKnowledgeObservation second = CreateClaimKnowledgeObservation(
            claimId,
            new InstitutionId("institution.second"),
            PoliticalClaimRecognitionState.Rejected);

        Assert.That(knowledge.RecordClaimObservation(first), Is.True);
        Assert.That(knowledge.RecordClaimObservation(second), Is.True);
        Assert.That(knowledge.ClaimObservations, Has.Count.EqualTo(2));
        Assert.That(knowledge.TryGetLatestClaimRecognitionObservation(
            claimId,
            first.RecognizingInstitutionId,
            out PoliticalClaimKnowledgeObservation firstCurrent), Is.True);
        Assert.That(firstCurrent.RecognitionState, Is.EqualTo(PoliticalClaimRecognitionState.Recognized));
        Assert.That(knowledge.TryGetLatestClaimRecognitionObservation(
            claimId,
            second.RecognizingInstitutionId,
            out PoliticalClaimKnowledgeObservation secondCurrent), Is.True);
        Assert.That(secondCurrent.RecognitionState, Is.EqualTo(PoliticalClaimRecognitionState.Rejected));
    }

    [Test]
    public void DiagnosticsRejectPoliticalClaimTargetOutsideCapturedWorldCatalog()
    {
        WorldStatePoliticalClaimSnapshot claim = new WorldStatePoliticalClaimSnapshot(
            "claim.missing-target",
            "claimant",
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTargetKind.Person,
            "missing-person",
            PoliticalClaimBasis.Other,
            "unverified",
            0L,
            PoliticalClaimStatus.Active,
            null,
            PoliticalClaimRecognitionState.Unrecognized,
            null,
            null,
            null,
            null);

        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            0L,
            politicalClaims: new[] { claim });

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(snapshot);
        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue =>
            issue.Code == "PoliticalClaimTargetPersonMissing"));
    }

    [Test]
    public void PoliticalClaimsParticipateInDeterministicDiagnostics()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime claimant = RegisterPerson(persons, "claimant");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        SimulationRuntime world = CreateWorld(persons, institutions);
        PoliticalClaimRecord claim = new PoliticalClaimRecord(
            new PoliticalClaimId("claim.diagnostics"),
            claimant.PersonId,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(claimant.PersonId),
            PoliticalClaimBasis.Other,
            "diagnostic claim",
            0L,
            new[] { "evidence:z", "evidence:a" });
        Assert.That(claim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
        Assert.That((int)claim.ClaimType, Is.EqualTo((int)PoliticalClaimType.StatusRecognition));
        Assert.That((int)claim.Target.Kind, Is.EqualTo((int)PoliticalClaimTargetKind.Person));
        Assert.That(PoliticalClaimRecord.IsTargetCompatible(claim.ClaimType, claim.Target.Kind), Is.True);
        Assert.That(world.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure registrationFailure), Is.True, registrationFailure?.ToString());

        WorldStateSnapshot before = Capture(world);
        Assert.That(WorldStateDiagnostics.Export(before), Does.Contain("POLITICAL_CLAIM"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);

        Assert.That(world.TryProposePoliticalClaimRecognition(
            claim.ClaimId,
            institutionId,
            PoliticalClaimRecognitionState.Recognized,
            "recorded",
            out PoliticalClaimRecognitionTransition transition,
            out _), Is.True);
        Assert.That(world.TryApplyPoliticalClaimRecognition(transition, out _), Is.True);
        WorldStateSnapshot after = Capture(world);
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);

        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(difference =>
            difference.Section == "PoliticalClaimRecognition"
            && difference.Identity == "claim.diagnostics\u001fcouncil"
            && difference.Field == "Entity"
            && difference.ChangeKind == WorldStateDifferenceChangeKind.Added));
        Assert.That(WorldStateInvariantValidator.Validate(after).IsValid, Is.True);
    }

    private static WorldStateSnapshot Capture(SimulationRuntime world)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            calendar: world.Calendar,
            personStore: world.PersonStore,
            politicalClaims: world.PoliticalClaimRecords,
            politicalClaimRecognitions: world.PoliticalClaimRecognitionRecords,
            institutionIds: GetInstitutionIds(world),
            officeIds: GetOfficeIds(world),
            propertyIds: GetPropertyIds(world)));
    }

    private static IEnumerable<string> GetInstitutionIds(SimulationRuntime world)
    {
        List<string> ids = new List<string>();
        foreach (InstitutionRecord record in world.InstitutionRecords)
        {
            if (record?.Id != null)
            {
                ids.Add(record.Id.Value);
            }
        }

        return ids;
    }

    private static IEnumerable<string> GetOfficeIds(SimulationRuntime world)
    {
        List<string> ids = new List<string>();
        foreach (OfficeRecord record in world.OfficeRecords)
        {
            if (record?.Id != null)
            {
                ids.Add(record.Id.Value);
            }
        }

        return ids;
    }

    private static IEnumerable<string> GetPropertyIds(SimulationRuntime world)
    {
        List<string> ids = new List<string>();
        foreach (PropertyOwnershipRecord record in world.PropertyOwnershipRecords)
        {
            if (record?.PropertyId != null)
            {
                ids.Add(record.PropertyId.Value);
            }
        }

        return ids;
    }

    private static PoliticalClaimRecord GetClaim(SimulationRuntime world, PoliticalClaimId claimId)
    {
        foreach (PoliticalClaimRecord claim in world.PoliticalClaimRecords)
        {
            if (claim != null && claim.ClaimId == claimId)
            {
                return claim;
            }
        }

        return null;
    }

    private static PoliticalClaimRecord CreateOfficeClaim(PersonId claimant, string claimId, OfficeId officeId)
    {
        return new PoliticalClaimRecord(
            new PoliticalClaimId(claimId),
            claimant,
            PoliticalClaimType.OfficeEntitlement,
            PoliticalClaimTarget.ForOffice(officeId),
            PoliticalClaimBasis.Genealogy,
            "direct lineage",
            0L,
            new[] { "genealogy:direct" });
    }

    private static PoliticalClaimKnowledgeObservation CreateClaimKnowledgeObservation(
        PoliticalClaimId claimId,
        InstitutionId institutionId,
        PoliticalClaimRecognitionState recognitionState,
        long observedAbsoluteDay = 0L)
    {
        return new PoliticalClaimKnowledgeObservation(
            claimId,
            true,
            new PersonId("person.claimant"),
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(new PersonId("person.target")),
            PoliticalClaimBasis.ExplicitDecision,
            0L,
            PoliticalClaimStatus.Active,
            null,
            recognitionState,
            institutionId,
            0L,
            observedAbsoluteDay,
            observedAbsoluteDay,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "perspective"));
    }

    private static SimulationRuntime CreateWorld(
        PersonStore persons,
        InstitutionStore institutions = null,
        OfficeStore offices = null,
        PoliticalClaimStore politicalClaimStore = null)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            Array.Empty<CityRuntime>(),
            Array.Empty<NpcRuntime>(),
            personStore: persons,
            institutionStore: institutions,
            officeStore: offices,
            politicalClaimStore: politicalClaimStore);
    }

    private static PersonRuntime RegisterPerson(PersonStore store, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), 0L);
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static InstitutionId RegisterInstitution(InstitutionStore store, string id)
    {
        InstitutionId institutionId = new InstitutionId(id);
        Assert.That(store.TryRegister(new InstitutionRecord(institutionId, id), out InstitutionFoundationFailure failure), Is.True, failure.ToString());
        return institutionId;
    }

    private static OfficeId RegisterOffice(OfficeStore store, InstitutionId institutionId, string id)
    {
        OfficeId officeId = new OfficeId(id);
        Assert.That(store.TryRegister(new OfficeRecord(officeId, institutionId, id), out InstitutionFoundationFailure failure), Is.True, failure.ToString());
        return officeId;
    }
}
