using System;
using NUnit.Framework;

public sealed class SuccessionCandidateFoundationTests
{
    [Test]
    public void CandidatesAreDirectChildrenAliveMatureAndDeterministicallyOrdered()
    {
        PersonStore people = new PersonStore();
        PersonRuntime subject = Register(people, "subject", 0L);
        PersonRuntime matureB = Register(people, "child-b", 0L);
        PersonRuntime matureA = Register(people, "child-a", 0L);
        PersonRuntime immature = Register(people, "child-immature", 9_999L);
        PersonRuntime dead = new PersonRuntime(new PersonId("child-dead"), 0L, 5L);
        Assert.That(people.TryRegister(dead, out _), Is.True);

        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(subject.PersonId, matureB.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(subject.PersonId, matureA.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(subject.PersonId, immature.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(subject.PersonId, dead.PersonId, out _), Is.True);

        SimulationCalendar calendar = new SimulationCalendar(CalendarDefinition.CreateDefault());
        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            people,
            genealogy,
            new SuccessionSubject(subject.PersonId),
            10_000L,
            calendar,
            18L,
            out SuccessionCandidateSnapshot snapshot,
            out SuccessionCandidateQueryFailure failure), Is.True, failure.ToString());

        Assert.That(snapshot.Candidates, Has.Count.EqualTo(2));
        Assert.That(snapshot.Candidates[0].CandidatePersonId, Is.EqualTo(matureA.PersonId));
        Assert.That(snapshot.Candidates[1].CandidatePersonId, Is.EqualTo(matureB.PersonId));
        Assert.That(snapshot.ContainsCandidate(dead.PersonId), Is.False);
        Assert.That(snapshot.ContainsCandidate(immature.PersonId), Is.False);
    }

    [Test]
    public void CandidateDiscoveryDoesNotRequireNpcMaterializationOrChooseASelectedPerson()
    {
        PersonStore people = new PersonStore();
        PersonRuntime subject = Register(people, "dormant-subject", 0L);
        PersonRuntime child = Register(people, "dormant-child", 0L);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(subject.PersonId, child.PersonId, out _), Is.True);

        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            people,
            genealogy,
            new SuccessionSubject(subject.PersonId),
            10_000L,
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            18L,
            out SuccessionCandidateSnapshot snapshot,
            out SuccessionCandidateQueryFailure failure), Is.True, failure.ToString());

        Assert.That(subject.IsMaterialized, Is.False);
        Assert.That(child.IsMaterialized, Is.False);
        Assert.That(snapshot.Candidates[0].CandidatePersonId, Is.EqualTo(child.PersonId));
        Assert.That(snapshot, Is.Not.Null);
    }

    [Test]
    public void CandidateSnapshotFingerprintChangesWhenGenealogyChanges()
    {
        PersonStore people = new PersonStore();
        PersonRuntime subject = Register(people, "fingerprint-subject", 0L);
        PersonRuntime first = Register(people, "fingerprint-first", 0L);
        PersonRuntime second = Register(people, "fingerprint-second", 0L);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(subject.PersonId, first.PersonId, out _), Is.True);

        SimulationCalendar calendar = new SimulationCalendar(CalendarDefinition.CreateDefault());
        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            people,
            genealogy,
            new SuccessionSubject(subject.PersonId),
            10_000L,
            calendar,
            18L,
            out SuccessionCandidateSnapshot before,
            out _), Is.True);
        Assert.That(genealogy.TryRemoveParentage(subject.PersonId, first.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(subject.PersonId, second.PersonId, out _), Is.True);
        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            people,
            genealogy,
            new SuccessionSubject(subject.PersonId),
            10_000L,
            calendar,
            18L,
            out SuccessionCandidateSnapshot after,
            out _), Is.True);

        Assert.That(before.DiscoveryFingerprint, Is.Not.EqualTo(after.DiscoveryFingerprint));
        Assert.That(after.ContainsCandidate(first.PersonId), Is.False);
        Assert.That(after.ContainsCandidate(second.PersonId), Is.True);
    }

    [Test]
    public void CandidateEndpointsMustExistInPersonStore()
    {
        PersonStore people = new PersonStore();
        PersonRuntime subject = Register(people, "missing-endpoint-subject", 0L);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            subject.PersonId,
            new PersonId("missing-child"),
            out _), Is.True);

        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            people,
            genealogy,
            new SuccessionSubject(subject.PersonId),
            10_000L,
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            18L,
            out _,
            out SuccessionCandidateQueryFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SuccessionCandidateQueryFailureCode.CandidateNotRegistered));
    }

    [Test]
    public void CandidateFingerprintIsUnambiguousForDelimitedPersonIds()
    {
        SimulationCalendar calendar = new SimulationCalendar(CalendarDefinition.CreateDefault());

        PersonStore oneChildPeople = new PersonStore();
        PersonRuntime oneChildSubject = Register(oneChildPeople, "delimiter-subject", 0L);
        PersonRuntime combinedIdChild = Register(oneChildPeople, "a;b", 0L);
        GenealogyStore oneChildGenealogy = new GenealogyStore();
        Assert.That(oneChildGenealogy.TryAddParentage(
            oneChildSubject.PersonId,
            combinedIdChild.PersonId,
            out _), Is.True);
        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            oneChildPeople,
            oneChildGenealogy,
            new SuccessionSubject(oneChildSubject.PersonId),
            10_000L,
            calendar,
            18L,
            out SuccessionCandidateSnapshot oneChild,
            out _), Is.True);

        PersonStore twoChildrenPeople = new PersonStore();
        PersonRuntime twoChildrenSubject = Register(twoChildrenPeople, "delimiter-subject", 0L);
        PersonRuntime firstChild = Register(twoChildrenPeople, "a", 0L);
        PersonRuntime secondChild = Register(twoChildrenPeople, "b", 0L);
        GenealogyStore twoChildrenGenealogy = new GenealogyStore();
        Assert.That(twoChildrenGenealogy.TryAddParentage(
            twoChildrenSubject.PersonId,
            firstChild.PersonId,
            out _), Is.True);
        Assert.That(twoChildrenGenealogy.TryAddParentage(
            twoChildrenSubject.PersonId,
            secondChild.PersonId,
            out _), Is.True);
        Assert.That(SuccessionCandidateSystem.TryBuildCandidates(
            twoChildrenPeople,
            twoChildrenGenealogy,
            new SuccessionSubject(twoChildrenSubject.PersonId),
            10_000L,
            calendar,
            18L,
            out SuccessionCandidateSnapshot twoChildren,
            out _), Is.True);

        Assert.That(oneChild.DiscoveryFingerprint, Is.Not.EqualTo(twoChildren.DiscoveryFingerprint));
    }

    private static PersonRuntime Register(PersonStore people, string id, long birthAbsoluteDay)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), birthAbsoluteDay);
        Assert.That(people.TryRegister(person, out _), Is.True);
        return person;
    }
}
