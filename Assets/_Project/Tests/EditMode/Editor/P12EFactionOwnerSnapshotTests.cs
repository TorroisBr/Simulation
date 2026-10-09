using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12EFactionOwnerSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
            if (simulationObject != null) UnityEngine.Object.DestroyImmediate(simulationObject);
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void EmptySectionsPreserveRequiredIdsAndSharedRevisionThroughStage()
    {
        P12EFactionOwnerSnapshot snapshot = Snapshot(Array.Empty<P12EFactionSnapshotRecord>(),
            Array.Empty<P12EFactionAffiliationSnapshotRecord>(), 0L);

        Assert.That(snapshot.Factions.SectionId, Is.EqualTo(FactionStoreCensusProvider.FactionsSectionId));
        Assert.That(snapshot.Affiliations.SectionId, Is.EqualTo(FactionStoreCensusProvider.AffiliationsSectionId));
        Assert.That(snapshot.Factions.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.Affiliations.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.Factions.RecordCount, Is.Zero);
        Assert.That(snapshot.Affiliations.RecordCount, Is.Zero);
        Assert.That(snapshot.Factions.Revision, Is.EqualTo(snapshot.Affiliations.Revision));
        Assert.That(snapshot.TryCreateStagedOwner(new PersonStore(), 0L, out FactionStore staged, out P12EFactionSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.AffiliationCount, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
    }

    [Test]
    public void RoundTripPreservesEveryFactionFieldAndCurrentAndTerminalTenures()
    {
        PersonStore persons = Persons("p-current", "p-none", "p-voluntary", "p-expelled");
        P12EFactionOwnerSnapshot snapshot = Snapshot(
            new[]
            {
                new P12EFactionSnapshotRecord("f-cannot", " Exact Name ", 2L, (int)FactionMembershipPolicy.CannotLeave, false),
                new P12EFactionSnapshotRecord("f-no-rejoin", "No Rejoin", 1L, (int)FactionMembershipPolicy.LeaveNoRejoin, true),
                new P12EFactionSnapshotRecord("f-rejoin", "Rejoin", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, false)
            },
            new[]
            {
                new P12EFactionAffiliationSnapshotRecord("id-current", "f-rejoin", "p-current", 4L, null, null, true),
                new P12EFactionAffiliationSnapshotRecord("id-no-reason", "f-cannot", "p-none", 2L, 5L, null, false),
                new P12EFactionAffiliationSnapshotRecord("id-voluntary", "f-no-rejoin", "p-voluntary", 1L, 3L,
                    (int)FactionAffiliationEndReason.VoluntaryLeave, false),
                new P12EFactionAffiliationSnapshotRecord("id-expelled", "f-rejoin", "p-expelled", 2L, 6L,
                    (int)FactionAffiliationEndReason.Expulsion, false)
            }, 11L);

        Assert.That(snapshot.TryCreateStagedOwner(persons, 8L, out FactionStore staged, out P12EFactionSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(11L));
        Assert.That(staged.Factions, Has.Count.EqualTo(3));
        AssertFaction(staged.Factions[0], "f-cannot", " Exact Name ", 2L, FactionMembershipPolicy.CannotLeave, false);
        AssertFaction(staged.Factions[1], "f-no-rejoin", "No Rejoin", 1L, FactionMembershipPolicy.LeaveNoRejoin, true);
        AssertFaction(staged.Factions[2], "f-rejoin", "Rejoin", 0L, FactionMembershipPolicy.LeaveAndRejoin, false);
        AssertAffiliation(staged.Affiliations[0], "id-no-reason", "f-cannot", "p-none", 2L, 5L, null, false);
        AssertAffiliation(staged.Affiliations[1], "id-voluntary", "f-no-rejoin", "p-voluntary", 1L, 3L,
            FactionAffiliationEndReason.VoluntaryLeave, false);
        AssertAffiliation(staged.Affiliations[2], "id-current", "f-rejoin", "p-current", 4L, null, null, true);
        AssertAffiliation(staged.Affiliations[3], "id-expelled", "f-rejoin", "p-expelled", 2L, 6L,
            FactionAffiliationEndReason.Expulsion, false);
    }

    [Test]
    public void LeaveAndRejoinHistoryRestoresNextSequenceDerivedIdAndLeaveNoRejoinStillRejects()
    {
        PersonStore persons = Persons("p-rejoin", "p-never-rejoin");
        FactionId rejoinId = new FactionId("f-rejoin");
        FactionId noRejoinId = new FactionId("f-no-rejoin");
        FactionStore source = new FactionStore(persons);
        RegisterFaction(source, new FactionRecord(rejoinId, "R", 0L, FactionMembershipPolicy.LeaveAndRejoin));
        RegisterFaction(source, new FactionRecord(noRejoinId, "N", 0L, FactionMembershipPolicy.LeaveNoRejoin));
        AddAndEnd(source, rejoinId, new PersonId("p-rejoin"), 1L, 2L);
        AddAndEnd(source, rejoinId, new PersonId("p-rejoin"), 3L, 4L);
        AddAndEnd(source, noRejoinId, new PersonId("p-never-rejoin"), 1L, 2L);

        P12EFactionOwnerSnapshot snapshot = SnapshotFromOwner(source);
        Assert.That(source.Revision, Is.GreaterThan(snapshot.Factions.RecordCount + snapshot.Affiliations.RecordCount));
        Assert.That(snapshot.TryCreateStagedOwner(persons, 5L, out FactionStore staged, out P12EFactionSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));
        Assert.That(staged.AffiliationCount, Is.EqualTo(3));

        Assert.That(FactionAffiliationSystem.TryProposeAdd(source, rejoinId, new PersonId("p-rejoin"), 5L,
            out FactionAffiliationAddTransition sourceNext, out FactionFoundationFailure sourceFailure), Is.True, sourceFailure.ToString());
        Assert.That(FactionAffiliationSystem.TryProposeAdd(staged, rejoinId, new PersonId("p-rejoin"), 5L,
            out FactionAffiliationAddTransition stagedNext, out FactionFoundationFailure stagedFailure), Is.True, stagedFailure.ToString());
        Assert.That(stagedNext.Affiliation.AffiliationId.Value, Is.EqualTo(sourceNext.Affiliation.AffiliationId.Value));
        Assert.That(stagedNext.Affiliation.AffiliationId.Value,
            Is.EqualTo(FactionAffiliationRecord.BuildStableId(rejoinId, new PersonId("p-rejoin"), 5L, 2L).Value));
        Assert.That(FactionAffiliationSystem.TryProposeAdd(staged, noRejoinId, new PersonId("p-never-rejoin"), 5L,
            out _, out FactionFoundationFailure noRejoinFailure), Is.False);
        Assert.That(noRejoinFailure.Code, Is.EqualTo(FactionFoundationFailureCode.DuplicateAffiliation));
    }

    [Test]
    public void RestoresExactRevisionAfterTerminalWritesAndMaxRevisionRejectsNextMutationWithoutChange()
    {
        PersonStore persons = Persons("p");
        FactionStore source = new FactionStore(persons);
        RegisterFaction(source, new FactionRecord(new FactionId("f"), "F", 0L));
        AddAndEnd(source, new FactionId("f"), new PersonId("p"), 1L, 2L);
        Assert.That(source.Revision, Is.EqualTo(3L));
        Assert.That(source.Count + source.AffiliationCount, Is.EqualTo(2));
        P12EFactionOwnerSnapshot snapshot = SnapshotFromOwner(source);
        Assert.That(snapshot.TryCreateStagedOwner(persons, 2L, out FactionStore staged, out P12EFactionSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(3L));

        P12EFactionOwnerSnapshot maxSnapshot = Snapshot(
            new[] { new P12EFactionSnapshotRecord("max-f", "Max", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            Array.Empty<P12EFactionAffiliationSnapshotRecord>(), long.MaxValue);
        Assert.That(maxSnapshot.TryCreateStagedOwner(new PersonStore(), 0L, out FactionStore maxStore, out failure),
            Is.True, failure.Message);
        Assert.That(maxStore.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(maxStore.TryRegister(new FactionRecord(new FactionId("later"), "Later", 0L), out FactionFoundationFailure overflow), Is.False);
        Assert.That(overflow.Code, Is.EqualTo(FactionFoundationFailureCode.RevisionOverflow));
        Assert.That(maxStore.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(maxStore.Count, Is.EqualTo(1));
        Assert.That(maxStore.AffiliationCount, Is.Zero);
    }

    [Test]
    public void SourceInsertionOrderAndRepeatedStageProduceDeterministicSectionOrderAndValues()
    {
        PersonStore persons = Persons("p-z", "p-a");
        FactionStore first = CreateOrderedOwner(persons, reverse: false);
        FactionStore second = CreateOrderedOwner(persons, reverse: true);
        P12EFactionOwnerSnapshot firstSnapshot = SnapshotFromOwner(first);
        P12EFactionOwnerSnapshot secondSnapshot = SnapshotFromOwner(second);
        Assert.That(FactionProjection(firstSnapshot), Is.EqualTo(FactionProjection(secondSnapshot)));
        Assert.That(AffiliationProjection(firstSnapshot), Is.EqualTo(AffiliationProjection(secondSnapshot)));
        Assert.That(firstSnapshot.TryCreateStagedOwner(persons, 3L, out FactionStore firstStage, out P12EFactionSnapshotFailure firstFailure), Is.True, firstFailure.Message);
        Assert.That(secondSnapshot.TryCreateStagedOwner(persons, 3L, out FactionStore secondStage, out P12EFactionSnapshotFailure secondFailure), Is.True, secondFailure.Message);
        Assert.That(FactionProjection(SnapshotFromOwner(firstStage)), Is.EqualTo(FactionProjection(SnapshotFromOwner(secondStage))));
        Assert.That(AffiliationProjection(SnapshotFromOwner(firstStage)), Is.EqualTo(AffiliationProjection(SnapshotFromOwner(secondStage))));
    }

    [Test]
    public void ActiveIndexIsRebuiltAndDuplicateActiveTypedPairRejectsStage()
    {
        PersonStore persons = Persons("p");
        P12EFactionOwnerSnapshot valid = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("active", "f", "p", 1L, null, null, true) }, 2L);
        Assert.That(valid.TryCreateStagedOwner(persons, 1L, out FactionStore staged, out P12EFactionSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged.TryGetAffiliation(new FactionId("f"), new PersonId("p"), out FactionAffiliationRecord indexed), Is.True);
        Assert.That(indexed.AffiliationId.Value, Is.EqualTo("active"));
        Assert.That(staged.AffiliationCount, Is.EqualTo(1));

        P12EFactionOwnerSnapshot duplicate = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[]
            {
                new P12EFactionAffiliationSnapshotRecord("active-a", "f", "p", 0L, null, null, true),
                new P12EFactionAffiliationSnapshotRecord("active-b", "f", "p", 0L, null, null, true)
            }, 3L);
        AssertStageFails(duplicate, persons, 0L, P12EFactionSnapshotFailureCode.DuplicateIdentity);
    }

    [Test]
    public void StagingRejectsMissingRootsAndInvalidDatesOrTypedIdentities()
    {
        PersonStore persons = Persons("p");
        P12EFactionOwnerSnapshot validFaction = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 3L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            Array.Empty<P12EFactionAffiliationSnapshotRecord>(), 1L);
        AssertStageFails(validFaction, persons, 2L, P12EFactionSnapshotFailureCode.InvalidTenure);

        AssertStageFails(Snapshot(Array.Empty<P12EFactionSnapshotRecord>(),
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "missing-f", "p", 1L, null, null, true) }, 1L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidReference);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 1L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "missing-p", 1L, null, null, true) }, 2L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidReference);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 2L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 1L, null, null, true) }, 2L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidTenure);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 3L, null, null, true) }, 2L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidTenure);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 2L, 1L, null, false) }, 2L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidTenure);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 1L, 3L, null, false) }, 2L),
            persons, 2L, P12EFactionSnapshotFailureCode.InvalidTenure);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord(" \t", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            Array.Empty<P12EFactionAffiliationSnapshotRecord>(), 1L), persons, 0L, P12EFactionSnapshotFailureCode.InvalidIdentity);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord(" \t", "f", "p", 0L, null, null, true) }, 2L),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidIdentity);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 1L, null, null, false) }, 2L),
            persons, 1L, P12EFactionSnapshotFailureCode.InvalidTenure);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 1L, null,
                (int)FactionAffiliationEndReason.Expulsion, true) }, 2L),
            persons, 1L, P12EFactionSnapshotFailureCode.InvalidTenure);
    }

    [Test]
    public void StagingRejectsInvalidPolicyAndEndEnumsButKeepsNullableTerminalReason()
    {
        PersonStore persons = Persons("p");
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, 99, true) },
            Array.Empty<P12EFactionAffiliationSnapshotRecord>(), 1L), persons, 1L, P12EFactionSnapshotFailureCode.InvalidPolicy);
        AssertStageFails(Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 0L, 1L, 99, false) }, 2L),
            persons, 1L, P12EFactionSnapshotFailureCode.InvalidTenure);
        P12EFactionOwnerSnapshot nullableReason = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 0L, 1L, null, false) }, 2L);
        Assert.That(nullableReason.TryCreateStagedOwner(persons, 1L, out FactionStore staged, out P12EFactionSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged.Affiliations[0].EndReason, Is.Null);
    }

    [Test]
    public void MetadataCountsDuplicatesNullRowsAndLateFailureRejectWholeCandidate()
    {
        PersonStore persons = Persons("p");
        P12EFactionOwnerSnapshot valid = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 0L, null, null, true) }, 2L);
        AssertStageFails(new P12EFactionOwnerSnapshot(null, valid.Affiliations), persons, 0L, P12EFactionSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(SnapshotSection(FactionsSection("f", 1L, valid.Factions.Records), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(SnapshotSection(valid.Factions, Section(FactionStoreCensusProvider.AffiliationsSectionId, 1, 3L, valid.Affiliations.Records)),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidRevision);
        AssertStageFails(SnapshotSection(valid.Factions, Section(FactionStoreCensusProvider.AffiliationsSectionId, 1, -1L, valid.Affiliations.Records)),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidRevision);
        AssertStageFails(SnapshotSection(Section(FactionStoreCensusProvider.FactionsSectionId, 2, 2L, valid.Factions.Records), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(SnapshotSection(Section<P12EFactionSnapshotRecord>(FactionStoreCensusProvider.FactionsSectionId, 1, 2L, null), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidCardinality);
        AssertStageFails(SnapshotSection(Section(FactionStoreCensusProvider.FactionsSectionId, 1, 2L, valid.Factions.Records, count: 2), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidCardinality);
        AssertStageFails(SnapshotSection(Section("wrong.owner.section", 1, 2L, valid.Factions.Records), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(SnapshotSection(Section(FactionStoreCensusProvider.FactionsSectionId, 1, 2L, new P12EFactionSnapshotRecord[] { null }), valid.Affiliations),
            persons, 0L, P12EFactionSnapshotFailureCode.InvalidIdentity);
        AssertStageFails(SnapshotSection(valid.Factions, Section(FactionStoreCensusProvider.AffiliationsSectionId, 1, 2L,
            new P12EFactionAffiliationSnapshotRecord[] { null })), persons, 0L, P12EFactionSnapshotFailureCode.InvalidIdentity);

        P12EFactionOwnerSnapshot duplicateFaction = Snapshot(
            new[]
            {
                new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true),
                new P12EFactionSnapshotRecord("f", "duplicate", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true)
            }, Array.Empty<P12EFactionAffiliationSnapshotRecord>(), 2L);
        AssertStageFails(duplicateFaction, persons, 0L, P12EFactionSnapshotFailureCode.DuplicateIdentity);
        P12EFactionOwnerSnapshot duplicateAffiliation = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[]
            {
                new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 0L, 0L, null, false),
                new P12EFactionAffiliationSnapshotRecord("a", "f", "p", 0L, 0L, null, false)
            }, 3L);
        AssertStageFails(duplicateAffiliation, persons, 0L, P12EFactionSnapshotFailureCode.DuplicateIdentity);

        // The second affiliation is invalid after the first valid row has already been converted;
        // the unpublished candidate must not escape, and neither dependency nor source changes.
        P12EFactionOwnerSnapshot lateFailure = Snapshot(
            new[] { new P12EFactionSnapshotRecord("f", "F", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[]
            {
                new P12EFactionAffiliationSnapshotRecord("first", "f", "p", 0L, null, null, true),
                new P12EFactionAffiliationSnapshotRecord("second", "missing-f", "p", 0L, null, null, true)
            }, 3L);
        int personCount = persons.Persons.Count;
        AssertStageFails(lateFailure, persons, 0L, P12EFactionSnapshotFailureCode.InvalidReference);
        Assert.That(persons.Persons.Count, Is.EqualTo(personCount));
        Assert.That(persons.Revision, Is.EqualTo(1L));

        List<FactionRecord> partialFactions = new List<FactionRecord>
        {
            new FactionRecord(new FactionId("factory-f"), "F", 0L)
        };
        List<FactionAffiliationRecord> partialAffiliations = new List<FactionAffiliationRecord>
        {
            new FactionAffiliationRecord(new FactionId("factory-f"), new PersonId("p"), 0L,
                affiliationId: new FactionAffiliationId("factory-a")),
            new FactionAffiliationRecord(new FactionId("factory-f"), new PersonId("p"), 0L,
                affiliationId: new FactionAffiliationId("factory-b"))
        };
        Assert.That(FactionStore.TryCreateFromP12EOwnerSnapshot(persons, partialFactions, partialAffiliations,
            3L, out FactionStore unpublished), Is.False);
        Assert.That(unpublished, Is.Null);
        Assert.That(partialFactions, Has.Count.EqualTo(1));
        Assert.That(partialAffiliations, Has.Count.EqualTo(2));
    }

    [Test]
    public void CaptureUsesExactCompletedBoundaryOwnerVectorAndRejectsStaleOrMismatchedEvidence()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        FactionStore owner = FindFactionOwner(token.OwnerSections);
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(runtime, owner, token, token.OwnerSections,
            out P12EFactionOwnerSnapshot snapshot, out P12EFactionSnapshotFailure captureFailure), Is.True, captureFailure.Message);
        Assert.That(snapshot.Factions.RecordCount, Is.Zero);
        Assert.That(snapshot.Affiliations.RecordCount, Is.Zero);
        Assert.That(snapshot.Factions.Revision, Is.EqualTo(snapshot.Affiliations.Revision));

        Assert.That(P12EFactionOwnerSnapshot.TryCapture(runtime, owner, token,
            new List<OwnerSectionCensusSnapshot>(token.OwnerSections), out _, out P12EFactionSnapshotFailure staleVector), Is.False);
        Assert.That(staleVector.Code, Is.EqualTo(P12EFactionSnapshotFailureCode.InvalidCaptureContext));
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(runtime, new FactionStore(new PersonStore()), token,
            token.OwnerSections, out _, out P12EFactionSnapshotFailure wrongOwner), Is.False);
        Assert.That(wrongOwner.Code, Is.EqualTo(P12EFactionSnapshotFailureCode.InvalidOwnerSectionVector));

        OwnerSectionCensusSnapshot factionWitness = FindFactionSection(token.OwnerSections);
        List<OwnerSectionCensusSnapshot> wrongWitnessVector = ReplaceSection(token.OwnerSections, factionWitness,
            new OwnerSectionCensusSnapshot(factionWitness.SectionId, factionWitness.SchemaVersion,
                factionWitness.Role, new object(), factionWitness.Cardinality, factionWitness.Revision));
        DailyCaptureEligibilityToken wrongWitnessToken = CopyToken(token, wrongWitnessVector);
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(runtime, owner, wrongWitnessToken, wrongWitnessVector,
            out _, out P12EFactionSnapshotFailure wrongWitness), Is.False);
        Assert.That(wrongWitness.Code, Is.EqualTo(P12EFactionSnapshotFailureCode.InvalidOwnerSectionVector));

        Assert.That(runtime.TryRegisterFaction(new FactionRecord(new FactionId("changed-after-token"), "Late", runtime.CurrentDay), out _), Is.True);
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(runtime, owner, token, token.OwnerSections,
            out P12EFactionOwnerSnapshot staleSnapshot, out P12EFactionSnapshotFailure staleFailure), Is.False);
        Assert.That(staleSnapshot, Is.Null);
        Assert.That(staleFailure.Code, Is.EqualTo(P12EFactionSnapshotFailureCode.InvalidOwnerSectionVector));
    }

    [Test]
    public void DelimiterInsideOneTypedIdentityRoundTripsExactly()
    {
        PersonStore persons = Persons("person\u001fpart");
        P12EFactionOwnerSnapshot snapshot = Snapshot(
            new[] { new P12EFactionSnapshotRecord("faction\u001fpart", "Exact", 0L,
                (int)FactionMembershipPolicy.LeaveAndRejoin, true) },
            new[] { new P12EFactionAffiliationSnapshotRecord("affiliation-exact", "faction\u001fpart",
                "person\u001fpart", 1L, null, null, true) }, 2L);

        Assert.That(snapshot.TryCreateStagedOwner(persons, 1L, out FactionStore staged, out P12EFactionSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(staged.Factions[0].Id.Value, Is.EqualTo("faction\u001fpart"));
        Assert.That(staged.Affiliations[0].PersonId.Value, Is.EqualTo("person\u001fpart"));
        Assert.That(staged.TryGetAffiliation(new FactionId("faction\u001fpart"), new PersonId("person\u001fpart"), out _), Is.True);
    }

    [Test]
    public void DistinctActiveTypedPairsWithSameLegacyKeyRejectWholeStage()
    {
        PersonStore persons = Persons("c", "b\u001fc");
        P12EFactionOwnerSnapshot snapshot = Snapshot(
            new[]
            {
                new P12EFactionSnapshotRecord("a\u001fb", "one", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true),
                new P12EFactionSnapshotRecord("a", "two", 0L, (int)FactionMembershipPolicy.LeaveAndRejoin, true)
            },
            new[]
            {
                new P12EFactionAffiliationSnapshotRecord("aff-one", "a\u001fb", "c", 0L, null, null, true),
                new P12EFactionAffiliationSnapshotRecord("aff-two", "a", "b\u001fc", 0L, null, null, true)
            }, 4L);

        Assert.That(snapshot.TryCreateStagedOwner(persons, 0L, out FactionStore staged, out P12EFactionSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EFactionSnapshotFailureCode.DuplicateIdentity));
    }

    private static PersonStore Persons(params string[] ids)
    {
        PersonStore result = new PersonStore();
        foreach (string id in ids)
            Assert.That(result.TryRegister(new PersonRuntime(new PersonId(id)), out PersonStoreFailure failure), Is.True, failure.ToString());
        return result;
    }

    private static P12EFactionOwnerSnapshot Snapshot(P12EFactionSnapshotRecord[] factions,
        P12EFactionAffiliationSnapshotRecord[] affiliations, long revision) => new P12EFactionOwnerSnapshot(
        Section(FactionStoreCensusProvider.FactionsSectionId, 1, revision, factions),
        Section(FactionStoreCensusProvider.AffiliationsSectionId, 1, revision, affiliations));

    private static P12EFactionSnapshotSection<T> Section<T>(string sectionId, int schemaVersion, long revision,
        IEnumerable<T> records, int? count = null, Func<T, T> copy = null) =>
        new P12EFactionSnapshotSection<T>(sectionId, schemaVersion, count ?? records?.Count() ?? 0, revision, records, copy);

    private static P12EFactionOwnerSnapshot SnapshotSection(
        P12EFactionSnapshotSection<P12EFactionSnapshotRecord> factions,
        P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord> affiliations) =>
        new P12EFactionOwnerSnapshot(factions, affiliations);

    private static P12EFactionSnapshotSection<P12EFactionSnapshotRecord> FactionsSection(string id, long revision,
        IEnumerable<P12EFactionSnapshotRecord> rows) => Section(id, 1, revision, rows);

    private static void AssertStageFails(P12EFactionOwnerSnapshot snapshot, PersonStore persons, long day,
        P12EFactionSnapshotFailureCode expected)
    {
        Assert.That(snapshot.TryCreateStagedOwner(persons, day, out FactionStore staged, out P12EFactionSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected));
    }

    private static void RegisterFaction(FactionStore store, FactionRecord faction) =>
        Assert.That(store.TryRegister(faction, out FactionFoundationFailure failure), Is.True, failure.ToString());

    private static void AddAndEnd(FactionStore store, FactionId faction, PersonId person, long joined, long ended)
    {
        Assert.That(FactionAffiliationSystem.TryProposeAdd(store, faction, person, joined,
            out FactionAffiliationAddTransition add, out FactionFoundationFailure addFailure), Is.True, addFailure.ToString());
        Assert.That(FactionAffiliationSystem.TryApplyAdd(store, add, out addFailure), Is.True, addFailure.ToString());
        Assert.That(FactionAffiliationSystem.TryProposeEnd(store, faction, person, ended,
            out FactionAffiliationEndTransition end, out FactionFoundationFailure endFailure), Is.True, endFailure.ToString());
        Assert.That(FactionAffiliationSystem.TryApplyEnd(store, end, out endFailure), Is.True, endFailure.ToString());
    }

    private static FactionStore CreateOrderedOwner(PersonStore persons, bool reverse)
    {
        FactionStore store = new FactionStore(persons);
        FactionRecord[] factionRows =
        {
            new FactionRecord(new FactionId("f-z"), "Z", 0L),
            new FactionRecord(new FactionId("f-a"), "A", 0L)
        };
        if (reverse) Array.Reverse(factionRows);
        foreach (FactionRecord row in factionRows) RegisterFaction(store, row);
        FactionAffiliationRecord[] affiliations =
        {
            new FactionAffiliationRecord(new FactionId("f-z"), new PersonId("p-z"), 1L, affiliationId: new FactionAffiliationId("id-z")),
            new FactionAffiliationRecord(new FactionId("f-a"), new PersonId("p-a"), 1L, affiliationId: new FactionAffiliationId("id-a")),
            new FactionAffiliationRecord(new FactionId("f-a"), new PersonId("p-z"), 2L, affiliationId: new FactionAffiliationId("id-a-z"))
        };
        if (reverse) Array.Reverse(affiliations);
        foreach (FactionAffiliationRecord row in affiliations)
            Assert.That(store.TryRegisterAffiliation(row, out FactionFoundationFailure failure), Is.True, failure.ToString());
        return store;
    }

    private static P12EFactionOwnerSnapshot SnapshotFromOwner(FactionStore owner)
    {
        List<P12EFactionSnapshotRecord> factions = owner.Factions.Select(row => new P12EFactionSnapshotRecord(
            row.Id.Value, row.DisplayName, row.CreatedAbsoluteDay, (int)row.MembershipPolicy, row.ExpulsionAllowed)).ToList();
        List<P12EFactionAffiliationSnapshotRecord> affiliations = owner.Affiliations.Select(row =>
            new P12EFactionAffiliationSnapshotRecord(row.AffiliationId.Value, row.FactionId.Value, row.PersonId.Value,
                row.JoinedAbsoluteDay, row.EndedAbsoluteDay, row.EndReason.HasValue ? (int?)row.EndReason.Value : null, row.IsActive)).ToList();
        return Snapshot(factions.ToArray(), affiliations.ToArray(), owner.Revision);
    }

    private static string FactionProjection(P12EFactionOwnerSnapshot snapshot) => string.Join("|",
        snapshot.Factions.Records.Select(row => $"{row.Id}/{row.DisplayName}/{row.CreatedAbsoluteDay}/{row.MembershipPolicy}/{row.ExpulsionAllowed}"));

    private static string AffiliationProjection(P12EFactionOwnerSnapshot snapshot) => string.Join("|",
        snapshot.Affiliations.Records.Select(row => $"{row.FactionId}/{row.PersonId}/{row.AffiliationId}/{row.JoinedAbsoluteDay}/{row.EndedAbsoluteDay}/{row.EndReason}/{row.IsActive}"));

    private static void AssertFaction(FactionRecord row, string id, string name, long day, FactionMembershipPolicy policy, bool expulsion)
    {
        Assert.That(row.Id.Value, Is.EqualTo(id));
        Assert.That(row.DisplayName, Is.EqualTo(name));
        Assert.That(row.CreatedAbsoluteDay, Is.EqualTo(day));
        Assert.That(row.MembershipPolicy, Is.EqualTo(policy));
        Assert.That(row.ExpulsionAllowed, Is.EqualTo(expulsion));
    }

    private static void AssertAffiliation(FactionAffiliationRecord row, string id, string faction, string person,
        long joined, long? ended, FactionAffiliationEndReason? reason, bool active)
    {
        Assert.That(row.AffiliationId.Value, Is.EqualTo(id));
        Assert.That(row.FactionId.Value, Is.EqualTo(faction));
        Assert.That(row.PersonId.Value, Is.EqualTo(person));
        Assert.That(row.JoinedAbsoluteDay, Is.EqualTo(joined));
        Assert.That(row.EndedAbsoluteDay, Is.EqualTo(ended));
        Assert.That(row.EndReason, Is.EqualTo(reason));
        Assert.That(row.IsActive, Is.EqualTo(active));
    }


    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("p12e-faction-snapshot-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        Assert.That(simulation.Runtime, Is.Not.Null);
        return simulation.Runtime;
    }

    private static FactionStore FindFactionOwner(IReadOnlyList<OwnerSectionCensusSnapshot> vector)
    {
        foreach (OwnerSectionCensusSnapshot section in vector)
            if (section?.SectionId == FactionStoreCensusProvider.FactionsSectionId)
                return section.OwnerInstanceIdentity as FactionStore;
        Assert.Fail("Faction records owner witness is missing.");
        return null;
    }

    private static OwnerSectionCensusSnapshot FindFactionSection(IReadOnlyList<OwnerSectionCensusSnapshot> vector)
    {
        foreach (OwnerSectionCensusSnapshot section in vector)
            if (section?.SectionId == FactionStoreCensusProvider.FactionsSectionId) return section;
        Assert.Fail("Faction records section witness is missing.");
        return null;
    }

    private static List<OwnerSectionCensusSnapshot> ReplaceSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        OwnerSectionCensusSnapshot oldSection,
        OwnerSectionCensusSnapshot replacement)
    {
        List<OwnerSectionCensusSnapshot> copy = new List<OwnerSectionCensusSnapshot>(sections);
        int index = copy.IndexOf(oldSection);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        copy[index] = replacement;
        return copy;
    }

    private static DailyCaptureEligibilityToken CopyToken(DailyCaptureEligibilityToken source,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections) => new DailyCaptureEligibilityToken(
        source.RuntimeInstanceIdentity, source.AdmissionContext, source.ConfigurationIdentity,
        source.CalendarIdentity, source.CompositionProfile, source.WorldId, source.AbsoluteDay,
        source.CompletedCoreSequence, source.MutationEpoch, sections);
}
