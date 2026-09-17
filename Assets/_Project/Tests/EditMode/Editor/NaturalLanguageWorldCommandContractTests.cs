using System;
using NUnit.Framework;

public sealed class NaturalLanguageWorldCommandContractTests
{
    [Test]
    public void ResolvedTranslationContainsExactlyOneTypedWorldCommand()
    {
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.Resolved(
            RelocateCommand(),
            new[] { NpcEntity("npc-1") });

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved));
        Assert.That(result.Command, Is.Not.Null);
        Assert.That(result.Command.Payload, Is.TypeOf<RelocateNpcWorldCommandPayload>());
        Assert.That(result.Candidates, Is.Empty);
        Assert.That(result.MissingFields, Is.Empty);
        Assert.That(result.ResolvedEntities, Has.Count.EqualTo(1));
    }

    [Test]
    public void AmbiguousTranslationDoesNotChooseCandidate()
    {
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.Ambiguous(
            new[] { new WorldCommandTranslationCandidate(NpcEntity("npc-1")), new WorldCommandTranslationCandidate(NpcEntity("npc-2")) });

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void MissingInformationReturnsStructuredMissingFields()
    {
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.MissingInformation(
            new[] { "amount", "target owner" },
            new[] { new WorldCommandTranslationDiagnostic("MissingField", "Amount is required.", "amount") });

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.MissingInformation));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.MissingFields, Is.EqualTo(new[] { "amount", "target owner" }));
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo("amount"));
    }

    [Test]
    public void UnsupportedIntentDoesNotCreateWorldCommand()
    {
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.Unsupported(
            new[] { new WorldCommandTranslationDiagnostic("UnsupportedIntent", "Normal travel is not a WorldCommand.") });

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Unsupported));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("UnsupportedIntent"));
    }

    [Test]
    public void TranslationDoesNotMutateWorld()
    {
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.MissingInformation(new[] { "target" }));
        string before = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext()));

        translator.Translate(new NaturalLanguageWorldCommandRequest("adicione ouro", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        string after = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext()));
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void TranslationDoesNotMutateKnowledge()
    {
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Unsupported(
            new[] { new WorldCommandTranslationDiagnostic("UnsupportedIntent", "No knowledge command was created.") }));
        WorldCommandTranslationContext context = new WorldCommandTranslationContext();

        NaturalLanguageTranslationResult result = translator.Translate(
            new NaturalLanguageWorldCommandRequest("João sabe que existe a caverna", WorldCommandOrigin.GM), context);

        Assert.That(result.Command, Is.Null);
        Assert.That(context.EntityResolver, Is.Null);
        Assert.That(context.DefinitionLookup, Is.Null);
    }

    [Test]
    public void TranslationDoesNotConsumeRuntimeId()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        string first = allocator.AllocateNpcId();
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Invalid(
            new[] { new WorldCommandTranslationDiagnostic("InvalidInput", "Input is invalid.") }));

        translator.Translate(new NaturalLanguageWorldCommandRequest("", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        Assert.That(first, Is.EqualTo("npc-000001"));
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000002"));
    }

    [Test]
    public void TranslationDoesNotConsumeWorldCommandId()
    {
        WorldCommandIdAllocator allocator = new WorldCommandIdAllocator();
        string first = allocator.Allocate();
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Invalid(
            new[] { new WorldCommandTranslationDiagnostic("InvalidInput", "Input is invalid.") }));

        translator.Translate(new NaturalLanguageWorldCommandRequest("", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        Assert.That(first, Is.EqualTo("world-command-000001"));
        Assert.That(allocator.Allocate(), Is.EqualTo("world-command-000002"));
    }

    [Test]
    public void TranslationDoesNotRecordWorldCommand()
    {
        WorldCommandRecordStore records = new WorldCommandRecordStore();
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Resolved(RelocateCommand()));

        translator.Translate(new NaturalLanguageWorldCommandRequest("coloque npc-1 em location-1", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        Assert.That(records.Records, Is.Empty);
    }

    [Test]
    public void TranslationDoesNotRecordDecision()
    {
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Unsupported(
            new[] { new WorldCommandTranslationDiagnostic("UnsupportedIntent", "No decision was recorded.") }));
        NpcDecisionStore decisions = new NpcDecisionStore();

        translator.Translate(new NaturalLanguageWorldCommandRequest("decida por João", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        Assert.That(decisions.Decisions, Is.Empty);
    }

    [Test]
    public void TranslationDoesNotRecordEvent()
    {
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Unsupported(
            new[] { new WorldCommandTranslationDiagnostic("UnsupportedIntent", "No event was recorded.") }));
        HistoryStore history = new HistoryStore();
        DomainEventStore events = new DomainEventStore(history, new HistoryPolicy());

        translator.Translate(new NaturalLanguageWorldCommandRequest("gere um evento", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        Assert.That(events.Events, Is.Empty);
        Assert.That(history.HistoricalEvents, Is.Empty);
    }

    [Test]
    public void TranslationDoesNotChangeWorldStateDigest()
    {
        FixedTranslator translator = new FixedTranslator(NaturalLanguageTranslationResult.Resolved(RelocateCommand()));
        WorldStateSnapshotContext context = new WorldStateSnapshotContext();
        string before = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(context));

        translator.Translate(new NaturalLanguageWorldCommandRequest("coloque npc-1 em location-1", WorldCommandOrigin.GM), new WorldCommandTranslationContext());

        string after = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(context));
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void TranslationResultDiagnosticsAreStructured()
    {
        WorldCommandTranslationDiagnostic diagnostic = new WorldCommandTranslationDiagnostic(
            "AmbiguousNpc",
            "More than one NPC matches.",
            "npc",
            WorldCommandTranslationDiagnosticSeverity.Warning);
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.Ambiguous(
            new[] { new WorldCommandTranslationCandidate(NpcEntity("npc-1")) },
            new[] { diagnostic });

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("AmbiguousNpc"));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo("npc"));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WorldCommandTranslationDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].Message, Is.Not.Empty);
    }

    [Test]
    public void TranslationOriginIsPreservedInRequest()
    {
        NaturalLanguageWorldCommandRequest request = new NaturalLanguageWorldCommandRequest(
            "coloque npc-1 em location-1",
            WorldCommandOrigin.Table,
            WorldCommandAuthorityMode.Declare);

        Assert.That(request.Text, Is.EqualTo("coloque npc-1 em location-1"));
        Assert.That(request.Origin, Is.EqualTo(WorldCommandOrigin.Table));
        Assert.That(request.PreferredAuthority, Is.EqualTo(WorldCommandAuthorityMode.Declare));
    }

    [Test]
    public void ResolvedCommandStillRequiresWorldCommandPreview()
    {
        NaturalLanguageTranslationResult result = NaturalLanguageTranslationResult.Resolved(RelocateCommand());
        WorldCommandService service = new WorldCommandService();

        WorldCommandPreview preview = service.Preview(result.Command);

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved));
        Assert.That(preview.IsValid, Is.False);
        Assert.That(service.RecordStore.Records, Is.Empty);
    }

    private static WorldCommand RelocateCommand()
    {
        return new WorldCommand(
            WorldCommandKind.RelocateNpc,
            WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload("npc-1", "location-1"));
    }

    private static WorldCommandTranslationEntity NpcEntity(string runtimeId)
    {
        return new WorldCommandTranslationEntity(
            WorldCommandTranslationEntityKind.Npc,
            runtimeId,
            "npc-definition",
            "João");
    }

    private sealed class FixedTranslator : IWorldCommandNaturalLanguageTranslator
    {
        private readonly NaturalLanguageTranslationResult result;

        public FixedTranslator(NaturalLanguageTranslationResult result)
        {
            this.result = result;
        }

        public NaturalLanguageTranslationResult Translate(
            NaturalLanguageWorldCommandRequest request,
            WorldCommandTranslationContext context)
        {
            return result;
        }
    }
}
