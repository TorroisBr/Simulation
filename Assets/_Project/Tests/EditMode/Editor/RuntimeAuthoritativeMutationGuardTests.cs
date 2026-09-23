using System;
using System.Reflection;
using NUnit.Framework;

public sealed class RuntimeAuthoritativeMutationGuardTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void StandaloneSimulationTimeRemainsMutable()
    {
        SimulationTime time = new SimulationTime(4L);

        time.AdvanceDay();

        Assert.That(time.AbsoluteDay, Is.EqualTo(5L));
        Assert.That(time.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.None));
        Assert.That(time.AbsoluteDay, Is.EqualTo(6L));
    }

    [Test]
    public void RuntimeStartsHealthyAndBindsItsSimulationTime()
    {
        SimulationTime time = new SimulationTime();
        SimulationRuntime runtime = new SimulationRuntime(time, null, null);

        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(runtime.IsMutationFaulted, Is.False);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(time.AbsoluteDay, Is.EqualTo(1L));
    }

    [Test]
    public void FaultIsStickyAndBlocksRuntimeAndDirectClockAdvancement()
    {
        SimulationTime time = new SimulationTime(7L);
        SimulationRuntime runtime = new SimulationRuntime(time, null, null);

        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Faulted));
        Assert.That(runtime.MutationFaultReason, Is.EqualTo(AuthoritativeMutationFaultReason.RollbackRestoreFailed));
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(time.TryAdvanceDay(out SimulationTimeAdvanceFailure timeFailure), Is.False);
        Assert.That(timeFailure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.Throws<InvalidOperationException>(() => runtime.AdvanceDay());
        Assert.Throws<InvalidOperationException>(() => time.AdvanceDay());
        Assert.That(runtime.CurrentDay, Is.EqualTo(7L));
    }

    [Test]
    public void FaultedAdvanceDaysRejectsBeforeAdvancingAnyDay()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(12L), null, null);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(
            runtime.TryAdvanceDays(5, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure),
            Is.False);

        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(12L));
        Assert.Throws<InvalidOperationException>(() => runtime.AdvanceDays(5));
        Assert.That(runtime.CurrentDay, Is.EqualTo(12L));
    }

    [Test]
    public void FaultingOneRuntimeDoesNotAffectAnotherRuntime()
    {
        SimulationRuntime worldA = new SimulationRuntime(new SimulationTime(2L), null, null);
        SimulationRuntime worldB = new SimulationRuntime(new SimulationTime(9L), null, null);
        MarkFaulted(worldA, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(worldA.IsMutationFaulted, Is.True);
        Assert.That(worldB.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(worldA.TryAdvanceDay(out _), Is.False);
        Assert.That(worldB.TryAdvanceDay(out _), Is.True);
        Assert.That(worldA.CurrentDay, Is.EqualTo(2L));
        Assert.That(worldB.CurrentDay, Is.EqualTo(10L));
    }

    [Test]
    public void RuntimeRejectsReusingBoundSimulationTime()
    {
        SimulationTime sharedTime = new SimulationTime();
        _ = new SimulationRuntime(sharedTime, null, null);

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(sharedTime, null, null));
    }

    [Test]
    public void ExposedPersonStoreRejectsMutationWhileFaultedButRemainsReadable()
    {
        PersonStore people = new PersonStore();
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, personStore: people);
        PersonId personId = new PersonId("faulted-person-store");
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(people.TryRegister(new PersonRuntime(personId), out PersonStoreFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        Assert.That(people.Persons.Count, Is.Zero);
        Assert.That(people.TryGet(personId, out _), Is.False);
    }

    [Test]
    public void PersonDeathEntryPointRejectsBeforeChangingUnresidentPerson()
    {
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("faulted-death-person"));
        Assert.That(people.TryRegister(person, out _), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(3L), null, null, personStore: people);
        Assert.That(runtime.TryProposePersonDeath(person.PersonId, out PersonDeathTransition transition, out _), Is.True);

        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(runtime.TryApplyPersonDeath(transition, out PersonDeathLifecycleFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonDeathLifecycleFailure.RuntimeFaulted));
        Assert.That(person.IsDeadAt(runtime.CurrentDay), Is.False);
    }

    [Test]
    public void ExposedArmedForceStoreRejectsMutationWithoutRevisionChange()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        long revisionBefore = runtime.ArmedForceStore.Revision;
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(
            runtime.ArmedForceStore.TryRegister(
                new ArmedForceRecord(new ArmedForceId("faulted-force"), "Faulted", 0L),
                out ArmedForceFoundationFailure failure),
            Is.False);

        Assert.That(failure.Code, Is.EqualTo(ArmedForceFoundationFailureCode.RuntimeFaulted));
        Assert.That(runtime.ArmedForceStore.Count, Is.Zero);
        Assert.That(runtime.ArmedForceStore.Revision, Is.EqualTo(revisionBefore));
        Assert.That(runtime.ArmedForceStore.TryGet(new ArmedForceId("faulted-force"), out _), Is.False);
    }

    [Test]
    public void RuntimeOwnedGenealogyRejectsMutationWithoutRevisionChange()
    {
        PersonStore people = new PersonStore();
        PersonRuntime parent = new PersonRuntime(new PersonId("guarded-parent"));
        PersonRuntime child = new PersonRuntime(new PersonId("guarded-child"));
        Assert.That(people.TryRegister(parent, out _), Is.True);
        Assert.That(people.TryRegister(child, out _), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, personStore: people);
        Assert.That(runtime.TryAddParentage(parent.PersonId, child.PersonId, out _), Is.True);
        long worldRevision = runtime.PoliticalWorldRevision;
        int genealogyCount = runtime.GenealogyRecords.Count;
        PersonRuntime secondChild = new PersonRuntime(new PersonId("guarded-second-child"));
        Assert.That(runtime.TryRegisterPerson(secondChild, out _), Is.True);
        worldRevision = runtime.PoliticalWorldRevision;
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(runtime.TryAddParentage(parent.PersonId, secondChild.PersonId, out PersonGenealogyFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonGenealogyFailure.RuntimeFaulted));
        Assert.That(runtime.GenealogyRecords.Count, Is.EqualTo(genealogyCount));
        Assert.That(runtime.PoliticalWorldRevision, Is.EqualTo(worldRevision));
        Assert.That(runtime.ContainsParentage(parent.PersonId, child.PersonId), Is.True);
    }

    [Test]
    public void RuntimeOwnedPlaceContentRejectsMutationAndRemainsReadable()
    {
        PlaceContentStore placeContent = new PlaceContentStore();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null, placeContentStore: placeContent);
        LocalPlaceRuntime place = new LocalPlaceRuntime("guarded-content-place");
        PlaceContentOwnerReference owner = PlaceContentOwnerReference.ForLocalPlace(place, "macro-location");
        PlaceContentRuntime contents = placeContent.GetOrCreate(owner);
        Assert.That(placeContent.Places.Count, Is.EqualTo(1));
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(placeContent.TryAddStack(
            owner,
            null,
            1,
            PlaceContentPersistencePolicy.Durable,
            out PlaceContentStackRuntime stack), Is.False);
        Assert.That(stack, Is.Null);
        Assert.That(contents.StackedContent.Count, Is.Zero);
        Assert.That(placeContent.TryGet(owner, out PlaceContentRuntime readContent), Is.True);
        Assert.That(readContent, Is.SameAs(contents));
    }

    [Test]
    public void FaultedRuntimeBlocksPersonBindingBeforeNpcOrPersonMutation()
    {
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("faulted-binding-person"));
        Assert.That(people.TryRegister(person, out _), Is.True);
        NpcRuntime npc = new NpcRuntime(
            "faulted-binding-npc",
            SimulationTestFactory.CreateNpc("faulted-binding-definition"));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, new[] { npc }, personStore: people);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(
            runtime.TryBindExistingNpcToPerson(person.PersonId, npc.RuntimeId, out PersonMaterializationFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.RuntimeFaulted));
        Assert.That(npc.PersonId, Is.Null);
        Assert.That(person.IsMaterialized, Is.False);
    }

    [Test]
    public void FaultedRuntimeBlocksExistingPersonResidenceBinding()
    {
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("faulted-residence-person"));
        Assert.That(people.TryRegister(person, out _), Is.True);
        CityRuntime city = SimulationTestFactory.CreateCity("faulted-residence-city", "faulted-residence-location");
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), new[] { city }, null, personStore: people);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(
            runtime.TryBindExistingPersonResident(person.PersonId, city, out PersonResidenceMembershipFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(PersonResidenceMembershipFailure.RuntimeFaulted));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
    }

    [Test]
    public void OrdinaryInvalidDayCountDoesNotFaultRuntime()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);

        Assert.That(
            runtime.TryAdvanceDays(-1, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure),
            Is.False);

        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));
        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(runtime.TryAdvanceDay(out _), Is.True);
    }

    private static void MarkFaulted(
        SimulationRuntime runtime,
        AuthoritativeMutationFaultReason reason)
    {
        MethodInfo markFaulted = typeof(SimulationRuntime).GetMethod(
            "MarkAuthoritativeMutationFaulted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(markFaulted, Is.Not.Null);
        markFaulted.Invoke(runtime, new object[] { reason });
    }
}
