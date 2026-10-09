using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12EPoliticalClaimOwnerSnapshotTests
{
    [Test]
    public void EmptySectionsStageExplicitlyAndPreserveExactRevision()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>(),
            new List<P12EPoliticalClaimRecognitionRow>(), 0L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.RecognitionCount, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(staged, Is.Not.SameAs(roots.Source));
    }

    [Test]
    public void ClaimsAndRecognitionStageEveryValueAndDoNotRecomputeRevisionFromCounts()
    {
        Roots roots = new Roots();
        List<P12EPoliticalClaimRow> claims = new List<P12EPoliticalClaimRow>
        {
            new P12EPoliticalClaimRow("claim-person", "claimant", (int)PoliticalClaimType.StatusRecognition,
                (int)PoliticalClaimTargetKind.Person, "target", (int)PoliticalClaimBasis.Genealogy, "basis", 2L,
                (int)PoliticalClaimStatus.Active, null, new[] { "a", "z" }),
            new P12EPoliticalClaimRow("claim-property", "claimant", (int)PoliticalClaimType.PropertyEntitlement,
                (int)PoliticalClaimTargetKind.Property, "property", (int)PoliticalClaimBasis.PropertyOwnership, "", 1L,
                (int)PoliticalClaimStatus.Resolved, 8L, new string[0]),
            new P12EPoliticalClaimRow("claim-office", "claimant", (int)PoliticalClaimType.OfficeEntitlement,
                (int)PoliticalClaimTargetKind.Office, "office", (int)PoliticalClaimBasis.OfficeIncumbency, "", 0L,
                (int)PoliticalClaimStatus.Active, null, new string[0]),
            new P12EPoliticalClaimRow("claim-institution", "claimant", (int)PoliticalClaimType.InstitutionalAuthority,
                (int)PoliticalClaimTargetKind.Institution, "institution", (int)PoliticalClaimBasis.Other, "", 0L,
                (int)PoliticalClaimStatus.Active, null, new string[0])
        };
        List<P12EPoliticalClaimRecognitionRow> recognitions = new List<P12EPoliticalClaimRecognitionRow>
        {
            Recognition("claim-person", "institution", new[]
            {
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Unrecognized, 3L, "old"),
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Recognized, 4L, "current")
            }),
            Recognition("claim-person", "institution-2", new[]
            {
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Contested, 5L, "second")
            })
        };
        const long exactRevision = 19L;
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(claims, recognitions, exactRevision);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.EqualTo(4));
        Assert.That(staged.RecognitionCount, Is.EqualTo(2));
        Assert.That(staged.Revision, Is.EqualTo(exactRevision));
        Assert.That(staged.TryGet(new PoliticalClaimId("claim-person"), out PoliticalClaimRecord personClaim), Is.True);
        Assert.That(personClaim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
        Assert.That(personClaim.EvidenceReferences, Is.EqualTo(new[] { "a", "z" }));
        Assert.That(staged.TryGet(new PoliticalClaimId("claim-property"), out PoliticalClaimRecord terminal), Is.True);
        Assert.That(terminal.ResolutionAbsoluteDay, Is.EqualTo(8L));
        Assert.That(staged.TryGetRecognition(new PoliticalClaimId("claim-person"), new InstitutionId("institution"),
            out PoliticalClaimRecognitionRecord current), Is.True);
        Assert.That(current.History, Has.Count.EqualTo(2));
        Assert.That(current.History[0].Reason, Is.EqualTo("old"));
        Assert.That(current.History[1].State, Is.EqualTo(PoliticalClaimRecognitionState.Recognized));
        Assert.That(current.Reason, Is.EqualTo("current"));
    }

    [Test]
    public void StageRejectsDuplicateRecognitionAndMalformedTerminalHistory()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimRow claim = new P12EPoliticalClaimRow("claim-person", "claimant",
            (int)PoliticalClaimType.StatusRecognition, (int)PoliticalClaimTargetKind.Person, "target",
            (int)PoliticalClaimBasis.Other, "", 0L, (int)PoliticalClaimStatus.Active, null, new string[0]);
        string recognitionId = PoliticalClaimRecognitionRecord.BuildRecognitionId(
            new PoliticalClaimId("claim-person"), new InstitutionId("institution"));
        P12EPoliticalClaimRecognitionRow terminalMismatch = new P12EPoliticalClaimRecognitionRow(
            "claim-person", "institution", recognitionId, (int)PoliticalClaimRecognitionState.Recognized,
            5L, "current", new[]
        {
            new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Rejected, 4L, "stale")
        });
        P12EPoliticalClaimRecognitionRow valid = Recognition("claim-person", "institution", new[]
        {
            new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Recognized, 5L, "current")
        });
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>{ claim },
            new List<P12EPoliticalClaimRecognitionRow>{ valid, valid.Copy() }, 2L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.DuplicateIdentity));
        snapshot = Snapshot(new List<P12EPoliticalClaimRow>{ claim }, new List<P12EPoliticalClaimRecognitionRow>{ terminalMismatch }, 1L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
    }

    [Test]
    public void StageAcceptsMaxRevisionAndStoreRejectsNextMutationAtomically()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>(),
            new List<P12EPoliticalClaimRecognitionRow>(), long.MaxValue);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
        PoliticalClaimRecord next = new PoliticalClaimRecord(new PoliticalClaimId("overflow"), new PersonId("claimant"),
            PoliticalClaimType.StatusRecognition, PoliticalClaimTarget.ForPerson(new PersonId("target")),
            PoliticalClaimBasis.Other, "", 0L, null);
        Assert.That(staged.TryRegister(next, out PoliticalClaimFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PoliticalClaimFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void StageRejectsMissingSectionsUnsupportedSchemaAndDisagreeingRevisions()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> claims =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 0, 1L,
                new List<P12EPoliticalClaimRow>(), row => row?.Copy());
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> recognitions =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 0, 2L,
                new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy());
        P12EPoliticalClaimOwnerSnapshot snapshot = new P12EPoliticalClaimOwnerSnapshot(claims, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.InvalidRevision));

        recognitions = new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
            PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 2, 0, 1L,
            new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy());
        snapshot = new P12EPoliticalClaimOwnerSnapshot(claims, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema));

        snapshot = new P12EPoliticalClaimOwnerSnapshot(null, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema));
    }

    private static P12EPoliticalClaimRecognitionRow Recognition(string claim, string institution,
        IEnumerable<P12EPoliticalClaimRecognitionHistoryRow> history)
    {
        string id = PoliticalClaimRecognitionRecord.BuildRecognitionId(new PoliticalClaimId(claim), new InstitutionId(institution));
        P12EPoliticalClaimRecognitionHistoryRow last = null;
        foreach (var item in history) last = item;
        return new P12EPoliticalClaimRecognitionRow(claim, institution, id, last.State, last.Day, last.Reason, history);
    }

    private static P12EPoliticalClaimOwnerSnapshot Snapshot(List<P12EPoliticalClaimRow> claims,
        List<P12EPoliticalClaimRecognitionRow> recognitions, long revision) => new P12EPoliticalClaimOwnerSnapshot(
        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(PoliticalClaimStoreCensusProvider.ClaimsSectionId,
            1, claims.Count, revision, claims, row => row?.Copy()),
        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
            1, recognitions.Count, revision, recognitions, row => row?.Copy()));

    private sealed class Roots
    {
        internal readonly PersonStore People = new PersonStore();
        internal readonly InstitutionStore Institutions = new InstitutionStore();
        internal readonly OfficeStore Offices;
        internal readonly PropertyOwnershipStore Properties;
        internal readonly PoliticalClaimStore Source = new PoliticalClaimStore();
        internal Roots()
        {
            RegisterPerson("claimant"); RegisterPerson("target");
            Assert.That(Institutions.TryRegister(new InstitutionRecord(new InstitutionId("institution"), "institution"), out _), Is.True);
            Assert.That(Institutions.TryRegister(new InstitutionRecord(new InstitutionId("institution-2"), "institution-2"), out _), Is.True);
            Offices = new OfficeStore(Institutions);
            Assert.That(Offices.TryRegister(new OfficeRecord(new OfficeId("office"), new InstitutionId("institution"), "office"), out _), Is.True);
            Properties = new PropertyOwnershipStore(People);
            Assert.That(Properties.TryRegister(new PropertyOwnershipRecord(new PropertyId("property"), new PersonId("claimant")), out _), Is.True);
        }
        private void RegisterPerson(string id)
        {
            Assert.That(People.TryRegister(new PersonRuntime(new PersonId(id), 0L), out PersonStoreFailure failure), Is.True, failure.ToString());
        }
    }
}
