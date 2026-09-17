using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class NpcResidenceMigrationTests
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
    public void MigrationDecrementsOriginPopulation()
    {
        CityRuntime origin = CreateCity("migration-origin-decrement", 10);
        CityRuntime destination = CreateCity("migration-destination-decrement", 20);
        NpcRuntime npc = CreateResident(origin, "migration-decrement");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(origin.CurrentPopulation, Is.EqualTo(9));
    }

    [Test]
    public void MigrationIncrementsDestinationPopulation()
    {
        CityRuntime origin = CreateCity("migration-origin-increment", 10);
        CityRuntime destination = CreateCity("migration-destination-increment", 20);
        NpcRuntime npc = CreateResident(origin, "migration-increment");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(destination.CurrentPopulation, Is.EqualTo(21));
    }

    [Test]
    public void MigrationChangesResidence()
    {
        CityRuntime origin = CreateCity("migration-origin-residence", 10);
        CityRuntime destination = CreateCity("migration-destination-residence", 20);
        NpcRuntime npc = CreateResident(origin, "migration-residence");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(destination.RuntimeId));
    }

    [Test]
    public void MigrationDoesNotChangeCurrentLocation()
    {
        CityRuntime origin = CreateCity("migration-origin-location", 10);
        CityRuntime destination = CreateCity("migration-destination-location", 20);
        NpcRuntime npc = CreateResident(origin, "migration-location");
        SpatialLocationRuntime currentLocation = npc.CurrentLocation;

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(npc.CurrentLocation, Is.SameAs(currentLocation));
    }

    [Test]
    public void MigrationDoesNotChangeCurrentCity()
    {
        CityRuntime origin = CreateCity("migration-origin-current-city", 10);
        CityRuntime destination = CreateCity("migration-destination-current-city", 20);
        NpcRuntime npc = CreateResident(origin, "migration-current-city");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(npc.CurrentCity, Is.SameAs(origin));
    }

    [Test]
    public void MigrationDoesNotStartTravel()
    {
        CityRuntime origin = CreateCity("migration-origin-no-travel", 10);
        CityRuntime destination = CreateCity("migration-destination-no-travel", 20);
        NpcRuntime npc = CreateResident(origin, "migration-no-travel");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(npc.IsTraveling, Is.False);
        Assert.That(npc.DestinationCity, Is.Null);
        Assert.That(npc.TravelDaysRemaining, Is.EqualTo(0));
    }

    [Test]
    public void MigrationDoesNotCreateTravelParty()
    {
        CityRuntime origin = CreateCity("migration-origin-no-party", 10);
        CityRuntime destination = CreateCity("migration-destination-no-party", 20);
        NpcRuntime npc = CreateResident(origin, "migration-no-party");

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        Assert.That(npc.ActiveTravelPartyId, Is.Null);
    }

    [Test]
    public void MigrationProposalIsPure()
    {
        CityRuntime origin = CreateCity("migration-origin-pure", 10);
        CityRuntime destination = CreateCity("migration-destination-pure", 20);
        NpcRuntime npc = CreateResident(origin, "migration-pure");
        int originPopulation = origin.CurrentPopulation;
        long originRevision = origin.Population.Revision;
        int destinationPopulation = destination.CurrentPopulation;
        long destinationRevision = destination.Population.Revision;
        string residence = npc.ResidenceSettlementRuntimeId;
        CityRuntime currentCity = npc.CurrentCity;

        Propose(origin, destination, npc);

        Assert.That(origin.CurrentPopulation, Is.EqualTo(originPopulation));
        Assert.That(origin.Population.Revision, Is.EqualTo(originRevision));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationPopulation));
        Assert.That(destination.Population.Revision, Is.EqualTo(destinationRevision));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residence));
        Assert.That(npc.CurrentCity, Is.SameAs(currentCity));
    }

    [Test]
    public void MigrationCannotChangeResidenceWithoutAuthoritativeRoster()
    {
        CityRuntime origin = CreateCity("migration-required-roster-origin", 10);
        CityRuntime destination = CreateCity("migration-required-roster-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-required-roster");
        int originPopulation = origin.CurrentPopulation;
        int destinationPopulation = destination.CurrentPopulation;
        long originRevision = origin.Population.Revision;
        long destinationRevision = destination.Population.Revision;

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            out NpcResidenceMigrationTransition transition,
            out NpcResidenceMigrationFailure proposalFailure);

        Assert.That(proposed, Is.False);
        Assert.That(proposalFailure, Is.EqualTo(NpcResidenceMigrationFailure.AuthoritativeRosterRequired));
        Assert.That(transition, Is.Null);

        transition = Propose(origin, destination, npc);
        bool applied = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            transition,
            out NpcResidenceMigrationFailure applyFailure);

        Assert.That(applied, Is.False);
        Assert.That(applyFailure, Is.EqualTo(NpcResidenceMigrationFailure.AuthoritativeRosterRequired));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originPopulation));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationPopulation));
        Assert.That(origin.Population.Revision, Is.EqualTo(originRevision));
        Assert.That(destination.Population.Revision, Is.EqualTo(destinationRevision));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
    }

    [Test]
    public void MigrationRejectsNpcOutsideAuthoritativeRoster()
    {
        CityRuntime origin = CreateCity("migration-roster-membership-origin", 10);
        CityRuntime destination = CreateCity("migration-roster-membership-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-roster-membership");
        AuthoritativeNpcRoster authoritativeRoster =
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new NpcRuntime[0]);

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            authoritativeRoster,
            out NpcResidenceMigrationTransition transition,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.NpcNotInAuthoritativeRoster));
        Assert.That(transition, Is.Null);
        Assert.That(origin.CurrentPopulation, Is.EqualTo(10));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(20));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
    }

    [Test]
    public void MigrationProposalIsDeterministic()
    {
        CityRuntime firstOrigin = CreateCity("migration-origin-deterministic", 10);
        CityRuntime firstDestination = CreateCity("migration-destination-deterministic", 20);
        NpcRuntime firstNpc = CreateResident(firstOrigin, "migration-deterministic");
        NpcResidenceMigrationTransition first = Propose(firstOrigin, firstDestination, firstNpc);

        CityRuntime secondOrigin = CreateCity("migration-origin-deterministic", 10);
        CityRuntime secondDestination = CreateCity("migration-destination-deterministic", 20);
        NpcRuntime secondNpc = CreateResident(secondOrigin, "migration-deterministic");
        NpcResidenceMigrationTransition second = Propose(secondOrigin, secondDestination, secondNpc);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void MigrationProposalDoesNotConsumeRng()
    {
        CityRuntime origin = CreateCity("migration-origin-rng", 10);
        CityRuntime destination = CreateCity("migration-destination-rng", 20);
        NpcRuntime npc = CreateResident(origin, "migration-rng");
        Random random = new Random(1234);
        int first = random.Next();

        Propose(origin, destination, npc);

        Random expectedRandom = new Random(1234);
        Assert.That(first, Is.EqualTo(expectedRandom.Next()));
        Assert.That(random.Next(), Is.EqualTo(expectedRandom.Next()));
    }

    [Test]
    public void MigrationProposalDoesNotAdvanceTime()
    {
        CityRuntime origin = CreateCity("migration-origin-time", 10);
        CityRuntime destination = CreateCity("migration-destination-time", 20);
        NpcRuntime npc = CreateResident(origin, "migration-time");
        SimulationTime time = new SimulationTime(17L);

        Propose(origin, destination, npc);

        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
    }

    [Test]
    public void OriginUnderflowRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-underflow", 1);
        CityRuntime destination = CreateCity("migration-destination-underflow", 20);
        NpcRuntime npc = CreateResident(origin, "migration-underflow");
        ApplyPopulationChange(origin.Population, new PopulationChangeSet(0, 1, 0, 0));
        int destinationBefore = destination.CurrentPopulation;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.OriginUnderflow));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(0));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationBefore));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
    }

    [Test]
    public void DestinationOverflowRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-overflow", 10);
        CityRuntime destination = CreateCity("migration-destination-overflow", int.MaxValue);
        NpcRuntime npc = CreateResident(origin, "migration-overflow");
        int originBefore = origin.CurrentPopulation;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.DestinationOverflow));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originBefore));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(int.MaxValue));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
    }

    [Test]
    public void OriginStaleRevisionRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-stale", 10);
        CityRuntime destination = CreateCity("migration-destination-stale", 20);
        NpcRuntime npc = CreateResident(origin, "migration-origin-stale-npc");
        NpcResidenceMigrationTransition transition = Propose(origin, destination, npc);
        ApplyPopulationChange(origin.Population, new PopulationChangeSet(1, 0, 0, 0));
        int destinationBefore = destination.CurrentPopulation;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;

        bool applied = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out NpcResidenceMigrationFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.OriginStaleState));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationBefore));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
    }

    [Test]
    public void DestinationStaleRevisionRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-destination-stale", 10);
        CityRuntime destination = CreateCity("migration-destination-destination-stale", 20);
        NpcRuntime npc = CreateResident(origin, "migration-destination-stale-npc");
        NpcResidenceMigrationTransition transition = Propose(origin, destination, npc);
        ApplyPopulationChange(destination.Population, new PopulationChangeSet(1, 0, 0, 0));
        int originBefore = origin.CurrentPopulation;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;

        bool applied = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out NpcResidenceMigrationFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.DestinationStaleState));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originBefore));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
    }

    [Test]
    public void OriginRevisionOverflowRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-revision-overflow", 10);
        CityRuntime destination = CreateCity("migration-destination-revision-overflow", 20);
        NpcRuntime npc = CreateResident(origin, "migration-origin-revision-overflow-npc");
        SetRevision(origin.Population, long.MaxValue);

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.OriginRevisionOverflow));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(10));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(20));
    }

    [Test]
    public void DestinationRevisionOverflowRejectsAtomically()
    {
        CityRuntime origin = CreateCity("migration-origin-destination-revision-overflow", 10);
        CityRuntime destination = CreateCity("migration-destination-revision-overflow", 20);
        NpcRuntime npc = CreateResident(origin, "migration-destination-revision-overflow-npc");
        SetRevision(destination.Population, long.MaxValue);

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.DestinationRevisionOverflow));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(10));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(20));
    }

    [Test]
    public void FailedMigrationLeavesBothPopulationsUnchanged()
    {
        CityRuntime origin = CreateCity("migration-origin-failed-unchanged", 10);
        CityRuntime destination = CreateCity("migration-destination-failed-unchanged", 20);
        NpcRuntime npc = CreateResident(origin, "migration-failed-unchanged");
        NpcResidenceMigrationTransition transition = Propose(origin, destination, npc);
        ApplyPopulationChange(destination.Population, new PopulationChangeSet(1, 0, 0, 0));
        int originBefore = origin.CurrentPopulation;
        int destinationBefore = destination.CurrentPopulation;
        long originRevisionBefore = origin.Population.Revision;
        long destinationRevisionBefore = destination.Population.Revision;

        Assert.That(NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out _), Is.False);
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originBefore));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationBefore));
        Assert.That(origin.Population.Revision, Is.EqualTo(originRevisionBefore));
        Assert.That(destination.Population.Revision, Is.EqualTo(destinationRevisionBefore));
    }

    [Test]
    public void FailedMigrationLeavesResidenceUnchanged()
    {
        CityRuntime origin = CreateCity("migration-origin-residence-unchanged", 10);
        CityRuntime destination = CreateCity("migration-destination-residence-unchanged", 20);
        NpcRuntime npc = CreateResident(origin, "migration-residence-unchanged");
        NpcResidenceMigrationTransition transition = Propose(origin, destination, npc);
        string residenceBefore = npc.ResidenceSettlementRuntimeId;
        ApplyPopulationChange(origin.Population, new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out _), Is.False);
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
    }

    [Test]
    public void AppliedMigrationCannotApplyTwice()
    {
        CityRuntime origin = CreateCity("migration-origin-twice", 10);
        CityRuntime destination = CreateCity("migration-destination-twice", 20);
        NpcRuntime npc = CreateResident(origin, "migration-twice");
        NpcResidenceMigrationTransition transition = Propose(origin, destination, npc);
        Apply(origin, destination, npc, transition);
        int originAfterFirst = origin.CurrentPopulation;
        int destinationAfterFirst = destination.CurrentPopulation;
        string residenceAfterFirst = npc.ResidenceSettlementRuntimeId;

        bool appliedAgain = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out NpcResidenceMigrationFailure failure);

        Assert.That(appliedAgain, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.OriginStaleState));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(originAfterFirst));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationAfterFirst));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceAfterFirst));
    }

    [Test]
    public void SameOriginAndDestinationRejected()
    {
        CityRuntime city = CreateCity("migration-same-city", 10);
        NpcRuntime npc = CreateResident(city, "migration-same-city-npc");

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            city,
            city,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.SameSettlement));
    }

    [Test]
    public void WrongOriginResidenceRejected()
    {
        CityRuntime origin = CreateCity("migration-wrong-origin", 10);
        CityRuntime actualResidence = CreateCity("migration-actual-origin", 10);
        CityRuntime destination = CreateCity("migration-wrong-destination", 20);
        NpcRuntime npc = CreateResident(actualResidence, "migration-wrong-origin-npc");

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.ResidenceMismatch));
    }

    [Test]
    public void DeadNpcCannotMigrateResidence()
    {
        CityRuntime origin = CreateCity("migration-dead-origin", 10);
        CityRuntime destination = CreateCity("migration-dead-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-dead");
        Assert.That(npc.TryApplyDeath(), Is.True);

        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out _,
            out NpcResidenceMigrationFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.DeadNpc));
    }

    [Test]
    public void InconsistentTransitionIsRejectedAtomically()
    {
        CityRuntime origin = CreateCity("migration-invalid-origin", 10);
        CityRuntime destination = CreateCity("migration-invalid-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-invalid-transition");
        NpcResidenceMigrationTransition invalid = new NpcResidenceMigrationTransition(
            npc.RuntimeId,
            origin.RuntimeId,
            destination.RuntimeId,
            origin.Population.Revision,
            destination.Population.Revision,
            origin.CurrentPopulation,
            origin.CurrentPopulation,
            destination.CurrentPopulation,
            destination.CurrentPopulation + 1);

        Assert.That(NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            invalid,
            out NpcResidenceMigrationFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(NpcResidenceMigrationFailure.InvalidTransition));
        Assert.That(origin.CurrentPopulation, Is.EqualTo(10));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(20));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
    }

    [Test]
    public void TravelArrivalDoesNotChangeResidence()
    {
        CityRuntime origin = CreateCity("migration-travel-origin", 10);
        CityRuntime destination = CreateCity("migration-travel-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-travel-arrival");
        Assert.That(npc.StartTravel(destination, 2), Is.True);

        Assert.That(npc.AdvanceTravelDay(out _), Is.False);
        Assert.That(npc.AdvanceTravelDay(out CityRuntime arrivedCity), Is.True);

        Assert.That(arrivedCity, Is.SameAs(destination));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(origin.RuntimeId));
    }

    [Test]
    public void TravelArrivalDoesNotChangeResidentPopulation()
    {
        CityRuntime origin = CreateCity("migration-travel-pop-origin", 10);
        CityRuntime destination = CreateCity("migration-travel-pop-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-travel-population");
        int originPopulation = origin.CurrentPopulation;
        int destinationPopulation = destination.CurrentPopulation;
        Assert.That(npc.StartTravel(destination, 1), Is.True);

        Assert.That(npc.AdvanceTravelDay(out _), Is.True);

        Assert.That(origin.CurrentPopulation, Is.EqualTo(originPopulation));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(destinationPopulation));
    }

    [Test]
    public void TravelingResidentStillCountsAsResident()
    {
        CityRuntime origin = CreateCity("migration-travel-query-origin", 10);
        CityRuntime destination = CreateCity("migration-travel-query-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-travel-query");
        Assert.That(npc.StartTravel(destination, 1), Is.True);

        SettlementPopulationPresenceSummary summary = SettlementPopulationPresenceQuery.BuildSummary(
            origin,
            new[] { npc });

        Assert.That(summary.NamedResidentCount, Is.EqualTo(1));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(0));
    }

    [Test]
    public void VisitorPresentAtDestinationDoesNotBecomeResident()
    {
        CityRuntime origin = CreateCity("migration-visitor-origin", 10);
        CityRuntime destination = CreateCity("migration-visitor-destination", 20);
        NpcRuntime visitor = new NpcRuntime(
            "migration-visitor",
            SimulationTestFactory.CreateNpc("migration-visitor"));
        Assert.That(visitor.StartTravel(origin, 1), Is.False);
        destination.AddImportantNpc(visitor);

        SettlementPopulationPresenceSummary summary = SettlementPopulationPresenceQuery.BuildSummary(
            destination,
            new[] { visitor });

        Assert.That(visitor.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(summary.NamedResidentCount, Is.EqualTo(0));
        Assert.That(summary.NamedPresentCount, Is.EqualTo(1));
    }

    [Test]
    public void WorldStateDigestChangesAfterSuccessfulMigration()
    {
        CityRuntime origin = CreateCity("migration-digest-origin", 10);
        CityRuntime destination = CreateCity("migration-digest-destination", 20);
        NpcRuntime npc = CreateResident(origin, "migration-digest");
        WorldStateSnapshotContext context = new WorldStateSnapshotContext(
            npcs: new[] { npc },
            cities: new[] { origin, destination });
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(context);

        Apply(origin, destination, npc, Propose(origin, destination, npc));

        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(context);
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);

        Assert.That(
            WorldStateSnapshotDigest.Compute(after),
            Is.Not.EqualTo(WorldStateSnapshotDigest.Compute(before)));
        Assert.That(diff.Differences, Has.Some.Property("Field").EqualTo("ResidenceSettlementRuntimeId"));
    }

    [Test]
    public void PopulationFoundationNetZeroSemanticsStillPass()
    {
        CityRuntime city = CreateCity("migration-net-zero", 10);
        SettlementPopulationTransition transition = ProposePopulationChange(
            city.Population,
            new PopulationChangeSet(5, 5, 0, 0));

        Assert.That(SettlementPopulationSystem.TryApply(city.Population, transition, out _), Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void ProductionPopulationBoundaryHasNoUncheckedPopulationAfterMutator()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        Assert.That(
            typeof(SettlementPopulationRuntime).GetMethod("CommitValidatedTransition", flags),
            Is.Null);
        Assert.That(
            typeof(SettlementPopulationRuntime).GetMethod("TryApplyValidatedTransition", flags),
            Is.Null);
        Assert.That(
            typeof(SettlementPopulationRuntime).GetMethod("TryApplyPairedMigration", flags),
            Is.Not.Null);
    }

    private static NpcResidenceMigrationTransition Propose(
        CityRuntime origin,
        CityRuntime destination,
        NpcRuntime npc)
    {
        bool proposed = NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            Roster(npc),
            out NpcResidenceMigrationTransition transition,
            out NpcResidenceMigrationFailure failure);
        Assert.That(proposed, Is.True, failure.ToString());
        return transition;
    }

    private static void Apply(
        CityRuntime origin,
        CityRuntime destination,
        NpcRuntime npc,
        NpcResidenceMigrationTransition transition)
    {
        bool applied = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            Roster(npc),
            transition,
            out NpcResidenceMigrationFailure failure);
        Assert.That(applied, Is.True, failure.ToString());
    }

    private static AuthoritativeNpcRoster Roster(NpcRuntime npc)
    {
        return SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc });
    }

    private static NpcRuntime CreateResident(CityRuntime origin, string runtimeId)
    {
        NpcRuntime npc = new NpcRuntime(
            runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId));
        bool bound = SettlementPopulationMembershipSystem.TryBindExistingResident(
            origin,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure failure);
        Assert.That(bound, Is.True, failure.ToString());
        origin.AddImportantNpc(npc);
        return npc;
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        cityData.initialPopulation = population;
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId));
    }

    private static SettlementPopulationTransition ProposePopulationChange(
        SettlementPopulationRuntime population,
        PopulationChangeSet changes)
    {
        bool proposed = SettlementPopulationSystem.TryPropose(
            population,
            changes,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure failure);
        Assert.That(proposed, Is.True, failure.ToString());
        return transition;
    }

    private static void ApplyPopulationChange(
        SettlementPopulationRuntime population,
        PopulationChangeSet changes)
    {
        SettlementPopulationTransition transition = ProposePopulationChange(population, changes);
        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out _), Is.True);
    }

    private static void SetRevision(SettlementPopulationRuntime population, long revision)
    {
        FieldInfo field = typeof(SettlementPopulationRuntime).GetField(
            "revision",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(population, revision);
    }
}
