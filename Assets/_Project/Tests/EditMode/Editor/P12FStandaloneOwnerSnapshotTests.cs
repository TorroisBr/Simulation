using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12FStandaloneOwnerSnapshotTests
{
    [Test]
    public void PoliticalKnowledgeEmptyAndPopulatedCaptureStagesExactDetachedRevision()
    {
        PoliticalRoots sourceRoots = CreatePoliticalRoots();
        PoliticalKnowledgeStore source = new PoliticalKnowledgeStore(sourceRoots.People, sourceRoots.Institutions,
            sourceRoots.Claims, sourceRoots.Factions, sourceRoots.Offices, sourceRoots.Properties);
        DailyCaptureEligibilityToken emptyToken = Token(source,
            PoliticalKnowledgeStoreCensusProvider.SectionId, PoliticalKnowledgeStoreCensusProvider.SchemaVersion,
            out IReadOnlyList<OwnerSectionCensusSnapshot> emptyVector);
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(source, emptyToken, emptyVector,
            out P12FPoliticalKnowledgeOwnerSnapshot empty, out string failure), Is.True, failure);
        PoliticalRoots emptyTargets = CreatePoliticalRoots();
        Assert.That(empty.TryStage(emptyToken, emptyVector, emptyTargets.People, emptyTargets.Institutions,
            emptyTargets.Claims, emptyTargets.Factions, emptyTargets.Offices, emptyTargets.Properties, 0L,
            out PoliticalKnowledgeStore emptyStaged, out failure), Is.True, failure);
        Assert.That(emptyStaged.Count, Is.Zero);
        Assert.That(emptyStaged.Revision, Is.Zero);

        PersonId holder = new PersonId("p12f-political-holder");
        Assert.That(sourceRoots.People.TryRegister(new PersonRuntime(holder), out PersonStoreFailure personFailure),
            Is.True, personFailure.ToString());
        Assert.That(source.TryRegisterHolder(PoliticalKnowledgeHolder.ForPerson(holder), 0L,
            out PoliticalKnowledgeFailure registerFailure), Is.True, registerFailure.ToString());
        long sourceRevision = source.Revision;
        DailyCaptureEligibilityToken token = Token(source,
            PoliticalKnowledgeStoreCensusProvider.SectionId, PoliticalKnowledgeStoreCensusProvider.SchemaVersion,
            out IReadOnlyList<OwnerSectionCensusSnapshot> vector);
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(source, token, vector,
            out P12FPoliticalKnowledgeOwnerSnapshot snapshot, out failure), Is.True, failure);
        Assert.That(source.Revision, Is.EqualTo(sourceRevision), "capture is read-only");

        PoliticalRoots targets = CreatePoliticalRoots();
        Assert.That(targets.People.TryRegister(new PersonRuntime(new PersonId(holder.Value)), out personFailure),
            Is.True, personFailure.ToString());
        Assert.That(snapshot.TryStage(token, vector, targets.People, targets.Institutions, targets.Claims,
            targets.Factions, targets.Offices, targets.Properties, 0L,
            out PoliticalKnowledgeStore staged, out failure), Is.True, failure);
        Assert.That(staged.Revision, Is.EqualTo(sourceRevision));
        Assert.That(staged.Count, Is.EqualTo(1));
        Assert.That(staged.TryGet(PoliticalKnowledgeHolder.ForPerson(holder), out _), Is.True);
        Assert.That(staged.TryGet(PoliticalKnowledgeHolder.ForPerson(holder), out PoliticalKnowledgeRuntime stagedRow), Is.True);
        Assert.That(stagedRow, Is.Not.SameAs(source.Runtimes[0]));
    }

    [Test]
    public void PoliticalKnowledgeRejectsWrongVectorAndStaleOwnerWithoutChangingSource()
    {
        PoliticalRoots roots = CreatePoliticalRoots();
        PoliticalKnowledgeStore source = new PoliticalKnowledgeStore(roots.People, roots.Institutions,
            roots.Claims, roots.Factions, roots.Offices, roots.Properties);
        DailyCaptureEligibilityToken token = Token(source,
            PoliticalKnowledgeStoreCensusProvider.SectionId, PoliticalKnowledgeStoreCensusProvider.SchemaVersion,
            out IReadOnlyList<OwnerSectionCensusSnapshot> vector);
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(source, token,
            new List<OwnerSectionCensusSnapshot>(vector), out _, out _), Is.False);

        Assert.That(source.TryRegisterHolder(PoliticalKnowledgeHolder.ForPerson(new PersonId("stale-holder")), 0L,
            out _), Is.False, "unknown holder is rejected before mutation");
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(source, token, vector, out _, out _), Is.True);
        Assert.That(source.Count, Is.Zero);
        Assert.That(source.Revision, Is.Zero);

        PersonId validHolder = new PersonId("p12f-stale-holder");
        Assert.That(roots.People.TryRegister(new PersonRuntime(validHolder), out PersonStoreFailure personFailure),
            Is.True, personFailure.ToString());
        Assert.That(source.TryRegisterHolder(PoliticalKnowledgeHolder.ForPerson(validHolder), 0L, out _), Is.True);
        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(source, token, vector, out _, out _), Is.False,
            "owner revision/cardinality drift from the exact vector fails closed");
    }

    [Test]
    public void ScheduledDirectiveEmptyCaptureStagesAgainstSuppliedSimulationTime()
    {
        ScheduledDirectiveStore source = new ScheduledDirectiveStore(new SimulationTime());
        DailyCaptureEligibilityToken token = Token(source,
            ScheduledDirectiveCensusProvider.SectionId, ScheduledDirectiveCensusProvider.SchemaVersion,
            out IReadOnlyList<OwnerSectionCensusSnapshot> vector);
        Assert.That(P12FScheduledDirectiveOwnerSnapshot.TryCapture(source, token, vector,
            out P12FScheduledDirectiveOwnerSnapshot snapshot, out string failure), Is.True, failure);

        SimulationTime stagedTime = new SimulationTime();
        Assert.That(snapshot.TryStage(token, vector, stagedTime,
            new Dictionary<string, NpcRuntime>(), new Dictionary<string, NpcActionData>(),
            out ScheduledDirectiveStore staged, out failure), Is.True, failure);
        Assert.That(staged.Directives, Is.Empty);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(source.Revision, Is.Zero);
    }

    [Test]
    public void ScheduledDirectiveCapturePreservesTerminalFieldsAndStagesWithoutProcessing()
    {
        SimulationTime sourceTime = new SimulationTime();
        ScheduledDirectiveStore source = new ScheduledDirectiveStore(sourceTime);
        NpcActionData action = SimulationTestFactory.CreateAction("p12f-directive-action",
            NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective("directive-terminal", 4L,
            ScheduledDirectiveMode.RequestAction, ScheduledDirectiveOperation.EscapePrison,
            "npc-missing-is-preserved", action);
        Assert.That(source.Add(directive), Is.True);
        Assert.That(directive.MarkSkipped(2L, "preserved terminal reason"), Is.True);
        long sourceRevision = source.Revision;
        DailyCaptureEligibilityToken token = Token(source,
            ScheduledDirectiveCensusProvider.SectionId, ScheduledDirectiveCensusProvider.SchemaVersion,
            out IReadOnlyList<OwnerSectionCensusSnapshot> vector);

        Assert.That(P12FScheduledDirectiveOwnerSnapshot.TryCapture(source, token, vector,
            out P12FScheduledDirectiveOwnerSnapshot snapshot, out string failure), Is.True, failure);
        Assert.That(source.Revision, Is.EqualTo(sourceRevision), "capture does not dispatch or mutate directives");
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Skipped));

        NpcActionData stagedAction = SimulationTestFactory.CreateAction("p12f-directive-action",
            NpcActionType.EscapePrison, NpcActionCategory.Justice);
        Dictionary<string, NpcActionData> actions = new Dictionary<string, NpcActionData>
        {
            { stagedAction.DefinitionId, stagedAction }
        };
        SimulationTime stagedTime = new SimulationTime();
        Assert.That(snapshot.TryStage(token, vector, stagedTime,
            new Dictionary<string, NpcRuntime>(), actions,
            out ScheduledDirectiveStore staged, out failure), Is.True, failure);
        Assert.That(staged.Revision, Is.EqualTo(sourceRevision));
        Assert.That(staged.Directives, Has.Count.EqualTo(1));
        Assert.That(staged.Directives[0], Is.Not.SameAs(directive));
        Assert.That(staged.Directives[0].Action, Is.SameAs(stagedAction));
        Assert.That(staged.Directives[0].ActorRuntimeId, Is.EqualTo("npc-missing-is-preserved"));
        Assert.That(staged.Directives[0].State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(staged.Directives[0].ProcessedDay, Is.EqualTo(2L));
        Assert.That(staged.Directives[0].ResultReason, Is.EqualTo("preserved terminal reason"));
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
    }

    private static PoliticalRoots CreatePoliticalRoots()
    {
        PersonStore people = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        PoliticalClaimStore claims = new PoliticalClaimStore();
        FactionStore factions = new FactionStore(people);
        OfficeStore offices = new OfficeStore(institutions);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        return new PoliticalRoots(people, institutions, claims, factions, offices, properties);
    }

    private static DailyCaptureEligibilityToken Token(
        object owner, string sectionId, int schema, out IReadOnlyList<OwnerSectionCensusSnapshot> vector)
    {
        vector = CurrentVector(owner, sectionId, schema);
        return new DailyCaptureEligibilityToken(new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, vector);
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CurrentVector(object owner, string sectionId, int schema)
    {
        OwnerSectionCensusWitness witness = owner is PoliticalKnowledgeStore political
            ? new PoliticalKnowledgeStoreCensusProvider(political).GetCurrentCensus()
            : new ScheduledDirectiveCensusProvider((ScheduledDirectiveStore)owner).GetCurrentCensus();
        return new[] { new OwnerSectionCensusSnapshot(sectionId, schema, OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision) };
    }

    private sealed class PoliticalRoots
    {
        internal PersonStore People { get; }
        internal InstitutionStore Institutions { get; }
        internal PoliticalClaimStore Claims { get; }
        internal FactionStore Factions { get; }
        internal OfficeStore Offices { get; }
        internal PropertyOwnershipStore Properties { get; }

        internal PoliticalRoots(PersonStore people, InstitutionStore institutions, PoliticalClaimStore claims,
            FactionStore factions, OfficeStore offices, PropertyOwnershipStore properties)
        {
            People = people;
            Institutions = institutions;
            Claims = claims;
            Factions = factions;
            Offices = offices;
            Properties = properties;
        }
    }
}
