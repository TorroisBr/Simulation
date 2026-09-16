using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class OppositionConflictBindingTests
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
    public void OppositionConflictRequiresMatchingOppositionSideId()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = new Conflict("missing-opposition-side");
        conflict.AddSide("enemy-a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        conflict.AddSide("enemy-b", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void OppositionConflictRequiresAllNamedParticipants()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void OppositionConflictRejectsUnexpectedNamedParticipant()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);
        ConflictSide oppositionSide = FindSide(conflict, fixture.Opposition.OppositionSideId);
        oppositionSide.AddNpc(fixture.OppositionNpc);
        oppositionSide.AddNpc(fixture.UnexpectedNpc);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void OppositionConflictRequiresAllAggregateSourceIds()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void OppositionConflictRejectsUnexpectedAggregateSourceId()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });
        FindSide(conflict, fixture.Opposition.OppositionSideId).AddAggregate(
            new AggregateParticipantSnapshot("random-dragon-army", 5f));

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void OppositionParticipantOrderDoesNotMatter()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        fixture.Opposition.AddNamedParticipant(fixture.OppositionLieutenant);
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);
        ConflictSide oppositionSide = FindSide(conflict, fixture.Opposition.OppositionSideId);
        oppositionSide.AddAggregate(new AggregateParticipantSnapshot("undead-horde", 1f));
        oppositionSide.AddNpc(fixture.OppositionLieutenant);
        oppositionSide.AddNpc(fixture.OppositionNpc);

        Assert.That(fixture.ResolveAtStore(
            conflict,
            new ConflictResolutionConstraints { ForcedWinningSideId = "expedition" },
            out ConflictResolutionResult result,
            out string reason), Is.True, reason);
        Assert.That(result.WinningSideId, Is.EqualTo("expedition"));
    }

    [Test]
    public void ValidOppositionConflictBindingCanResolve()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });

        Assert.That(fixture.ResolveAtStore(
            conflict,
            new ConflictResolutionConstraints { ForcedWinningSideId = "expedition" },
            out ConflictResolutionResult result,
            out string reason), Is.True, reason);
        Assert.That(result, Is.Not.Null);
        Assert.That(fixture.Opposition.IsResolved, Is.True);
    }

    [Test]
    public void InvalidOppositionBindingDoesNotMutateNpcConsequences()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = "expedition"
        };
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(fixture.Performer.RuntimeId)
        {
            ForceDeath = true
        });

        Assert.That(fixture.ResolveAtStore(conflict, constraints, out _, out string reason), Is.False, reason);
        Assert.That(fixture.Performer.IsAlive, Is.True);
        Assert.That(fixture.Performer.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
    }

    [Test]
    public void InvalidOppositionBindingDoesNotResolveOpposition()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);

        Assert.That(fixture.ResolveAtStore(
            conflict,
            new ConflictResolutionConstraints { ForcedWinningSideId = "expedition" },
            out _,
            out string reason), Is.False, reason);
        Assert.That(fixture.Opposition.IsActive, Is.True);
        Assert.That(fixture.ContentRuntime.ActiveOppositions, Contains.Item(fixture.Opposition));
    }

    [Test]
    public void InvalidOppositionBindingDoesNotChangeAccessState()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        PlaceAccessState before = fixture.ContentRuntime.AccessState;
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
        Assert.That(fixture.ContentRuntime.AccessState, Is.EqualTo(before));
    }

    [Test]
    public void InvalidOppositionBindingDoesNotRecordConflictEvent()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 10f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
        Assert.That(fixture.World.Records.Events.Events, Has.None.TypeOf<ConflictResolvedEvent>());
    }

    [Test]
    public void ForcedOutcomeCannotBypassOppositionBinding()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddNamedParticipant(fixture.OppositionNpc);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer }, includeOppositionParticipants: false);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = "expedition",
            ForcedOverallOutcome = ConflictOutcomeType.Victory
        };

        Assert.That(fixture.ResolveAtStore(conflict, constraints, out _, out string reason), Is.False, reason);
        Assert.That(fixture.Opposition.IsActive, Is.True);
    }

    [Test]
    public void ExpeditionConflictRejectsNpcOutsideExpedition()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        NpcRuntime foreign = fixture.World.CreateNpc("binding-foreign", fixture.World.CityA, 100f);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer, foreign });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void ExpeditionConflictAllowsSubsetOfMembersWhenAtLeastOnePerformerParticipates()
    {
        BindingFixture fixture = new BindingFixture(includeSupport: true, includeThirdMember: true);
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer, fixture.Support });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.True, reason);
    }

    [Test]
    public void ExpeditionConflictRequiresAtLeastOnePerformer()
    {
        BindingFixture fixture = new BindingFixture(includeSupport: true);
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Support });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void ExpeditionConflictRejectsAggregateAllyWithoutExplicitExpeditionBinding()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });
        FindSide(conflict, "expedition").AddAggregate(new AggregateParticipantSnapshot("mercenary-army", 100f));

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void ExpeditionConflictRejectsUnrelatedThirdSide()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        NpcRuntime thirdParty = fixture.World.CreateNpc("binding-third-party", fixture.World.CityB, 100f);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });
        ConflictSide thirdSide = conflict.AddSide(
            "random-third-faction",
            ConflictObjectiveType.Defeat,
            ConflictStakes.Meaningful);
        thirdSide.AddNpc(thirdParty);

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void ExpeditionConflictNamedNpcMustResolveToRegisteredSameInstance()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        NpcRuntime fakePerformer = new NpcRuntime(
            fixture.Performer.RuntimeId,
            SimulationTestFactory.CreateNpc("binding-fake-performer"));
        Conflict conflict = fixture.CreateConflict(Array.Empty<NpcRuntime>());
        FindSide(conflict, "expedition").AddNpc(fakePerformer);

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void DeadExpeditionNpcCannotParticipateInNewConflict()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Assert.That(fixture.Performer.TryApplyDeath(), Is.True);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void InvalidExpeditionBindingDoesNotMutateOppositionOrObjective()
    {
        BindingFixture fixture = new BindingFixture(ExpeditionObjectiveRuntime.Eliminate("binding-opposition"));
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        NpcRuntime foreign = fixture.World.CreateNpc("binding-objective-foreign", fixture.World.CityA, 100f);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer, foreign });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.False, reason);
        Assert.That(fixture.Opposition.IsActive, Is.True);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.False);
    }

    [Test]
    public void ValidExpeditionConflictStillCompletesEliminateObjective()
    {
        BindingFixture fixture = new BindingFixture(ExpeditionObjectiveRuntime.Eliminate("binding-opposition"));
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void ValidExpeditionConflictStillUnlocksExistingTreasure()
    {
        BindingFixture fixture = new BindingFixture(ExpeditionObjectiveRuntime.Eliminate("binding-opposition"));
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        ItemData treasure = SimulationTestFactory.CreateItem("binding-treasure");
        Assert.That(fixture.Content.TryAddStack(
            fixture.Owner,
            treasure,
            3,
            PlaceContentPersistencePolicy.Durable,
            out _), Is.True);
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });

        Assert.That(fixture.ResolveAtExpedition(conflict, out _, out string reason), Is.True, reason);
        Assert.That(fixture.ContentRuntime.AccessState, Is.EqualTo(PlaceAccessState.Accessible));
        Assert.That(fixture.ContentRuntime.GetAmount(treasure), Is.EqualTo(3));
    }

    [Test]
    public void ExplicitContradictoryConflictLocationIsRejected()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(
            new[] { fixture.Performer },
            locationRuntimeId: fixture.World.CityB.Location.RuntimeId);

        Assert.That(fixture.ResolveAtStore(conflict, out _, out string reason), Is.False, reason);
    }

    [Test]
    public void NullConflictLocationRemainsAllowedWhenCurrentContractAllowsIt()
    {
        BindingFixture fixture = new BindingFixture();
        fixture.Opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("undead-horde", 1f));
        Conflict conflict = fixture.CreateConflict(new[] { fixture.Performer });

        Assert.That(fixture.ResolveAtStore(
            conflict,
            new ConflictResolutionConstraints { ForcedWinningSideId = "expedition" },
            out _,
            out string reason), Is.True, reason);
    }

    private static ConflictSide FindSide(Conflict conflict, string sideId)
    {
        foreach (ConflictSide side in conflict.Sides)
        {
            if (side != null && string.Equals(side.SideId, sideId, StringComparison.Ordinal))
            {
                return side;
            }
        }

        Assert.Fail("Conflict side not found: " + sideId);
        return null;
    }

    private sealed class BindingFixture
    {
        public SpatialTravelFixture World { get; }
        public PlaceContentStore Content { get; }
        public PlaceContentOwnerReference Owner { get; }
        public PlaceContentRuntime ContentRuntime => Content.GetOrCreate(Owner);
        public PlaceOppositionRuntime Opposition { get; }
        public NpcRuntime Performer { get; }
        public NpcRuntime Support { get; }
        public NpcRuntime ThirdMember { get; }
        public NpcRuntime OppositionNpc { get; }
        public NpcRuntime OppositionLieutenant { get; }
        public NpcRuntime UnexpectedNpc { get; }
        public ExpeditionStore Expeditions { get; }
        public ExpeditionSystem System { get; }
        public ExpeditionRuntime Expedition { get; }
        public ConflictResolutionService Resolver { get; }

        public BindingFixture(
            ExpeditionObjectiveRuntime objective = null,
            bool includeSupport = false,
            bool includeThirdMember = false)
        {
            World = new SpatialTravelFixture();
            Performer = World.CreateNpc("binding-performer", World.CityA, 100f);
            Performer.SetCurrentPresence(World.Site.Location);

            if (includeSupport == true)
            {
                Support = World.CreateNpc("binding-support", World.CityA, 100f);
                Support.SetCurrentPresence(World.Site.Location);
            }

            if (includeThirdMember == true)
            {
                ThirdMember = World.CreateNpc("binding-third-member", World.CityA, 100f);
                ThirdMember.SetCurrentPresence(World.Site.Location);
            }

            OppositionNpc = CreateUnregisteredNpc("binding-lich");
            OppositionLieutenant = CreateUnregisteredNpc("binding-lieutenant");
            UnexpectedNpc = CreateUnregisteredNpc("binding-unexpected");
            Opposition = new PlaceOppositionRuntime("binding-opposition");
            Content = new PlaceContentStore(World.Records.Allocator, World.IdentityRegistry);
            Owner = PlaceContentOwnerReference.ForExplorableSite(World.Site);
            Assert.That(Content.TryAddOpposition(Owner, Opposition, out string oppositionReason), Is.True, oppositionReason);

            Expeditions = new ExpeditionStore();
            TravelPartyStore parties = new TravelPartyStore();
            TravelPartySystem partySystem = new TravelPartySystem(
                parties,
                World.Records.Allocator,
                World.IdentityRegistry,
                World.Travel,
                World.Records.Time,
                World.Records.Sequence,
                World.Records.EventRecorder);
            World.SetTravelPartySystem(partySystem);
            System = new ExpeditionSystem(
                Expeditions,
                World.Records.Allocator,
                World.IdentityRegistry,
                World.Sites,
                partySystem,
                parties,
                World.Knowledge,
                World.Records.Time,
                World.Records.EventRecorder,
                null,
                Content,
                null);

            List<string> members = new List<string> { Performer.RuntimeId };
            List<string> supports = new List<string>();
            if (Support != null)
            {
                members.Add(Support.RuntimeId);
                supports.Add(Support.RuntimeId);
            }

            if (ThirdMember != null)
            {
                members.Add(ThirdMember.RuntimeId);
                supports.Add(ThirdMember.RuntimeId);
            }

            Expedition = new ExpeditionRuntime(
                "binding-expedition",
                World.Site.RuntimeId,
                World.CityA.Location.RuntimeId,
                World.Site.Location.RuntimeId,
                World.SiteRoute.RuntimeId,
                "binding-travel-party",
                null,
                members,
                new[] { Performer.RuntimeId },
                supports,
                ExpeditionState.Exploring,
                objective ?? ExpeditionObjectiveRuntime.Explore());
            Assert.That(Expeditions.Add(Expedition), Is.True);

            Resolver = new ConflictResolutionService(
                new ConflictResolver(
                    new FixedCapabilityModel(100f),
                    new SequenceConflictRandomSource(0.5f, 0.5f)),
                null,
                World.Records.EventRecorder);
        }

        public Conflict CreateConflict(
            IEnumerable<NpcRuntime> expeditionParticipants,
            bool includeOppositionParticipants = true,
            string expeditionSideId = "expedition",
            string locationRuntimeId = null)
        {
            Conflict conflict = new Conflict(
                "binding-conflict-" + Guid.NewGuid().ToString("N"),
                locationRuntimeId);
            ConflictSide expeditionSide = conflict.AddSide(
                expeditionSideId,
                ConflictObjectiveType.Defeat,
                ConflictStakes.Meaningful);
            if (expeditionParticipants != null)
            {
                foreach (NpcRuntime participant in expeditionParticipants)
                {
                    expeditionSide.AddNpc(participant);
                }
            }

            ConflictSide oppositionSide = conflict.AddSide(
                Opposition.OppositionSideId,
                ConflictObjectiveType.Defend,
                ConflictStakes.Meaningful);
            if (includeOppositionParticipants == true)
            {
                foreach (NpcRuntime participant in Opposition.NamedParticipants)
                {
                    oppositionSide.AddNpc(participant);
                }

                foreach (AggregateParticipantSnapshot aggregate in Opposition.AggregateParticipants)
                {
                    oppositionSide.AddAggregate(aggregate);
                }
            }

            return conflict;
        }

        public bool ResolveAtStore(
            Conflict conflict,
            out ConflictResolutionResult result,
            out string reason)
        {
            return ResolveAtStore(conflict, null, out result, out reason);
        }

        public bool ResolveAtStore(
            Conflict conflict,
            ConflictResolutionConstraints constraints,
            out ConflictResolutionResult result,
            out string reason)
        {
            return Content.TryResolveOpposition(
                Owner,
                Opposition,
                conflict,
                Resolver,
                constraints,
                out result,
                out reason);
        }

        public bool ResolveAtExpedition(
            Conflict conflict,
            out ConflictResolutionResult result,
            out string reason)
        {
            return System.TryResolvePlaceOpposition(
                Expedition,
                Owner,
                Opposition,
                conflict,
                Resolver,
                out result,
                out reason);
        }

        private static NpcRuntime CreateUnregisteredNpc(string id)
        {
            return new NpcRuntime(id, SimulationTestFactory.CreateNpc(id));
        }
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        private readonly float capability;

        public FixedCapabilityModel(float capability)
        {
            this.capability = capability;
        }

        public CapabilityEvaluationResult Evaluate(
            NpcRuntime participant,
            CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(capability, capability, null);
        }
    }
}
