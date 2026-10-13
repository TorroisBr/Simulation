using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12AContinuationCodecTests
{
    private GameObject simulationObject;
    private P12CP9GenesisManifestSnapshot p9Manifest;

    [SetUp]
    public void SetUp()
    {
        p9Manifest = null;
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        if (simulationObject != null) UnityEngine.Object.DestroyImmediate(simulationObject);
        p9Manifest = null;
        SimulationTestFactory.CleanupDefinitions();
    }

    private static readonly P12ADailyV1SectionManifest Manifest = new P12ADailyV1SectionManifest(
        new[]
        {
            new P12ASectionContract("a.required", 1, 0, OwnerSectionRole.Required, null),
            new P12ASectionContract("z.empty", 1, 0, OwnerSectionRole.ExplicitlyEmpty, null)
        });

    [Test]
    public void EncodeDecodeEncodeIsByteDeterministicAndDeepCopied()
    {
        P12ACompatibilityDescriptor descriptor = Descriptor();
        P12AContinuationImage original = Image(descriptor, new byte[] { 1, 2, 3, 4 });
        Assert.That(P12AContinuationCodec.TryEncode(original, Manifest, out byte[] first, out P12AResult saved), Is.True);
        Assert.That(saved.Code, Is.EqualTo(P12AResultCode.Encoded));
        Assert.That(P12AContinuationCodec.TryDecode(first, Manifest, descriptor, out P12AContinuationImage decoded,
            out P12AResult loaded), Is.True);
        Assert.That(loaded.Code, Is.EqualTo(P12AResultCode.Decoded));
        Assert.That(decoded.Header.PoliticalWorldRevision, Is.EqualTo(73L),
            "The causal political precondition must survive exact byte continuation.");
        Assert.That(P12AContinuationCodec.TryEncode(decoded, Manifest, out byte[] second, out _), Is.True);
        Assert.That(second, Is.EqualTo(first));
        Assert.That(decoded.Sections[0].Rows[0].OwnerKey, Is.EqualTo("owner:alpha"));
        Assert.That(decoded.Sections[0].Rows[0].Revision, Is.EqualTo(7));
        Assert.That(decoded.Sections[0].Rows[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(decoded.Sections[1].Rows, Is.Empty);
    }

    [Test]
    public void DurableStore_RereadsAndValidatesBeforeAtomicReplacement()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "P12AContinuationStoreTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            P12ACompatibilityDescriptor descriptor = Descriptor();
            Assert.That(P12AContinuationCodec.TryEncode(Image(descriptor, new byte[] { 1, 2, 3 }),
                Manifest, out byte[] bytes, out _), Is.True);
            Assert.That(P12AContinuationStore.TrySave(bytes, Manifest, descriptor, root,
                out P12AResult saveResult), Is.True, saveResult?.Code.ToString());
            Assert.That(saveResult.Code, Is.EqualTo(P12AResultCode.Saved));
            string slot = P12AContinuationStore.SlotPath(root);
            Assert.That(File.Exists(slot), Is.True);
            byte[] durable = File.ReadAllBytes(slot);
            Assert.That(durable, Is.EqualTo(bytes));

            byte[] corrupt = (byte[])bytes.Clone();
            corrupt[0] ^= 0x80;
            Assert.That(P12AContinuationStore.TrySave(corrupt, Manifest, descriptor, root, out _), Is.False);
            Assert.That(File.ReadAllBytes(slot), Is.EqualTo(durable),
                "Rejected replacement bytes must leave the existing slot unchanged.");
            Assert.That(P12AContinuationStore.TryLoad(Manifest, descriptor, root,
                out P12AContinuationImage loaded, out P12AResult loadResult), Is.True,
                loadResult?.Code.ToString());
            Assert.That(loadResult.Code, Is.EqualTo(P12AResultCode.Loaded));
            Assert.That(P12AContinuationCodec.TryEncode(loaded, Manifest, out byte[] roundTrip, out _), Is.True);
            Assert.That(roundTrip, Is.EqualTo(durable));
        }
        finally
        {
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
        }
    }

    [Test]
    public void DurableStore_MissingSlotDoesNotCreateOne()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "P12AContinuationStoreTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            Assert.That(P12AContinuationStore.TryLoad(Manifest, Descriptor(), root,
                out P12AContinuationImage loaded, out P12AResult result), Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(result.Code, Is.EqualTo(P12AResultCode.NoSave));
            Assert.That(System.IO.Directory.Exists(root), Is.False);
        }
        finally
        {
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
        }
    }

    [Test]
    public void DurableStore_AtomicReplaceFailurePreservesPreviousSlotAndCleansOwnTemporary()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "P12AContinuationStoreTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            P12ACompatibilityDescriptor descriptor = Descriptor();
            Assert.That(P12AContinuationCodec.TryEncode(Image(descriptor, new byte[] { 1, 2, 3 }),
                Manifest, out byte[] original, out _), Is.True);
            Assert.That(P12AContinuationCodec.TryEncode(Image(descriptor, new byte[] { 7, 8, 9 }),
                Manifest, out byte[] replacement, out _), Is.True);
            Assert.That(P12AContinuationStore.TrySave(original, Manifest, descriptor, root, out _), Is.True);
            string slot = P12AContinuationStore.SlotPath(root);

            Assert.That(P12AContinuationStore.TrySave(replacement, Manifest, descriptor, root,
                new ReplaceFailureFileOperations(), out P12AResult failure), Is.False);
            Assert.That(failure.Code, Is.EqualTo(P12AResultCode.StorageFailure));
            Assert.That(File.ReadAllBytes(slot), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(slot), "*.tmp"), Is.Empty);
        }
        finally
        {
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
        }
    }

    [Test]
    public void DecoderRejectsDigestMismatchBeforeReturningImage()
    {
        P12ACompatibilityDescriptor descriptor = Descriptor();
        Assert.That(P12AContinuationCodec.TryEncode(Image(descriptor, new byte[] { 9 }), Manifest,
            out byte[] bytes, out _), Is.True);
        bytes[12] ^= 0x40;
        Assert.That(P12AContinuationCodec.TryDecode(bytes, Manifest, descriptor,
            out P12AContinuationImage decoded, out P12AResult result), Is.False);
        Assert.That(decoded, Is.Null);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.DigestMismatch));
    }

    [Test]
    public void ManifestRejectsPopulatedExactEmptySectionAndMissingSection()
    {
        P12ACompatibilityDescriptor descriptor = Descriptor();
        P12AContinuationImage populatedEmpty = new P12AContinuationImage(Header(descriptor), CausalRoots(), new[]
        {
            Section("a.required", OwnerSectionRole.Required, new P12AOwnerRow("owner:alpha", 7, new byte[] { 1 })),
            Section("z.empty", OwnerSectionRole.ExplicitlyEmpty, new P12AOwnerRow("unexpected", 0, Array.Empty<byte>()))
        });
        Assert.That(P12AContinuationCodec.TryEncode(populatedEmpty, Manifest, out _, out P12AResult emptyResult), Is.False);
        Assert.That(emptyResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));

        P12AContinuationImage missing = new P12AContinuationImage(Header(descriptor), CausalRoots(), new[]
        {
            Section("a.required", OwnerSectionRole.Required, new P12AOwnerRow("owner:alpha", 7, new byte[] { 1 }))
        });
        Assert.That(P12AContinuationCodec.TryEncode(missing, Manifest, out _, out P12AResult missingResult), Is.False);
        Assert.That(missingResult.Code, Is.EqualTo(P12AResultCode.MissingOrDuplicateSection));
    }

    [Test]
    public void DuplicateOwnerKeysAreRejected()
    {
        P12AContinuationImage duplicate = new P12AContinuationImage(Header(Descriptor()), CausalRoots(), new[]
        {
            Section("a.required", OwnerSectionRole.Required,
                new P12AOwnerRow("same", 0, new byte[0]), new P12AOwnerRow("same", 0, new byte[0])),
            Section("z.empty", OwnerSectionRole.ExplicitlyEmpty)
        });
        Assert.That(P12AContinuationCodec.TryEncode(duplicate, Manifest, out _, out P12AResult result), Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    [Test]
    public void CompatibilityMismatchIsClassifiedByRuntimeVersusContentIdentity()
    {
        P12ACompatibilityDescriptor source = Descriptor();
        P12AContinuationImage image = Image(source, new byte[] { 1 });
        Assert.That(P12AContinuationCodec.TryEncode(image, Manifest, out byte[] bytes, out _), Is.True);
        P12ACompatibilityDescriptor buildMismatch = new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "LinuxPlayer", "Mono", "build",
            new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1, "content", "providers",
            "daily", 1, "calendar", "p9", "p8");
        Assert.That(P12AContinuationCodec.TryDecode(bytes, Manifest, buildMismatch, out _, out P12AResult buildResult), Is.False);
        Assert.That(buildResult.Code, Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        P12ACompatibilityDescriptor contentMismatch = new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "changed-config", "6000.3.9f1", "WindowsEditor", "Mono", "build",
            new[] { "00000000-0000-0000-0000-000000000001" }, "numeric", 1, "content", "providers",
            "daily", 1, "calendar", "p9", "p8");
        Assert.That(P12AContinuationCodec.TryDecode(bytes, Manifest, contentMismatch, out _, out P12AResult contentResult), Is.False);
        Assert.That(contentResult.Code, Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
    }

    [Test]
    public void CompatibilityDescriptorRequiresAProjectAssemblyIdentity()
    {
        Assert.Throws<ArgumentException>(() => new P12ACompatibilityDescriptor(
            "UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1", "WindowsEditor", "Mono", "build",
            Array.Empty<string>(), "numeric", 1, "content", "providers", "daily", 1, "calendar", "p9", "p8"));
    }

    [Test]
    public void NullableStringsAndStringLimitAreCanonical()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, null);
            P12AWire.WriteString(writer, string.Empty);
            byte[] bytes = stream.ToArray();
            Assert.That(BitConverter.ToInt32(bytes, 0), Is.EqualTo(-1));
            Assert.That(BitConverter.ToInt32(bytes, 4), Is.Zero);
            stream.Position = 0;
            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(stream, P12AWire.Utf8, true))
            {
                Assert.That(P12AWire.ReadString(reader, out string absent), Is.True);
                Assert.That(absent, Is.Null);
                Assert.That(P12AWire.ReadString(reader, out string presentEmpty), Is.True);
                Assert.That(presentEmpty, Is.EqualTo(string.Empty));
            }
        }
        string atLimit = new string('x', P12AContinuationCodec.MaxStringBytes);
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
            Assert.DoesNotThrow(() => P12AWire.WriteString(writer, atLimit));
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
            Assert.Throws<P12ALimitException>(() => P12AWire.WriteString(writer, atLimit + "x"));
    }

    [Test]
    public void OwnerSectionAtExactPayloadLimitEncodes()
    {
        const int ownerKeyUtf8Bytes = 11; // "owner:alpha"
        int payloadBytes = P12AContinuationCodec.MaxSectionBytes
            - sizeof(int) - sizeof(int) - ownerKeyUtf8Bytes - sizeof(long) - sizeof(int);
        byte[] payload = new byte[payloadBytes];
        P12AContinuationImage image = Image(Descriptor(), payload);
        Assert.That(P12AContinuationCodec.TryEncode(image, Manifest, out byte[] bytes, out P12AResult result),
            Is.True, result?.Code.ToString());
        Assert.That(bytes, Is.Not.Null);
    }

    [Test]
    public void RuntimeIdAllocatorCensusSectionsRoundTripAllTypedCountersAndRevisions()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        allocator.AllocateEventId(); allocator.AllocateEventId();
        allocator.AllocateDecisionId(); allocator.AllocateTravelPartyId();
        RuntimeIdAllocatorSnapshot source = allocator.CaptureSnapshot();
        string alias = "alias/" + RuntimeIdAllocatorCensusProvider.DecisionsSectionId;
        string[] sectionIds =
        {
            RuntimeIdAllocatorCensusProvider.EventsSectionId,
            RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
            RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId
        };
        long[] revisions = { 2, 1, 1 };
        List<P12AOwnerSection> sections = new List<P12AOwnerSection>();
        for (int i = 0; i < sectionIds.Length; i++)
        {
            Assert.That(P12ACausalRootOwnerCodec.TryEncodeRuntimeIdAllocator(source, sectionIds[i], revisions[i],
                alias, out P12AOwnerSection encoded, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
            Assert.That(P12ACausalRootOwnerCodec.TryDecodeRuntimeIdAllocator(encoded,
                out RuntimeIdAllocatorSnapshot decoded, out P12AResult decodeError), Is.True, decodeError?.Code.ToString());
            AssertAllocatorSnapshotsEqual(source, decoded);
            sections.Add(encoded);
        }
        P12AOwnerSection wrongRevision = new P12AOwnerSection(sections[0].Id, sections[0].Major,
            sections[0].Minor, sections[0].Role, sections[0].AliasGroup, sections[0].Revision + 1,
            sections[0].Rows);
        Assert.That(P12ACausalRootOwnerCodec.TryDecodeRuntimeIdAllocator(wrongRevision, out _, out _), Is.False);
    }

    [Test]
    public void SimulationRecordSequenceSnapshotRoundTripsWithItsCensusRevision()
    {
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        sequence.Allocate(); sequence.Allocate(); sequence.Allocate();
        SimulationRecordSequenceSnapshot source = sequence.CaptureSnapshot();
        Assert.That(P12ACausalRootOwnerCodec.TryEncodeRecordSequence(source, 3, null,
            out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(P12ACausalRootOwnerCodec.TryDecodeRecordSequence(section,
            out SimulationRecordSequenceSnapshot decoded, out P12AResult decodeError), Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.SchemaId, Is.EqualTo(source.SchemaId));
        Assert.That(decoded.SchemaVersion, Is.EqualTo(source.SchemaVersion));
        Assert.That(decoded.NextSequence, Is.EqualTo(source.NextSequence));
        Assert.That(P12ACausalRootOwnerCodec.TryEncodeRecordSequence(source, 2, null, out _, out _), Is.False);
    }

    [Test]
    public void ActorChoiceCodecPreservesOrderedTerminalHistoryAndStoreSequence()
    {
        ActorChoiceInput rejected = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000001"), "command-1", 1,
            new PersonId("person-1"), "action-a", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4, ActorChoiceInputStatus.Rejected,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Rejected, 4, 0,
                    failure: ActorChoiceFailure.ActionUnavailable)
            });
        ActorChoiceInput returned = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000002"), "command-2", 2,
            new PersonId("person-2"), "action-b", WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare, 5, ActorChoiceInputStatus.AttemptReturned,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.DispatchStarted, 5, 1,
                    decisionRecordId: "decision-2"),
                new ActorChoiceDisposition(2, ActorChoiceDispositionKind.AttemptReturned, 5, 1,
                    attemptOutcome: ActorChoiceAttemptOutcome.Succeeded,
                    returnedResultStatus: NpcActionResultType.Success)
            });
        P12FActorChoiceSnapshot source = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { rejected, returned }, 3, 5);

        Assert.That(P12AActorChoiceCodec.TryEncode(source, out P12AOwnerSection encoded,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
        Assert.That(encoded.Revision, Is.EqualTo(5));
        Assert.That(encoded.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "input/actor-choice-000001", "input/actor-choice-000002"
        }));
        Assert.That(encoded.Rows.Select(row => row.Revision), Is.All.Zero);
        Assert.That(P12AActorChoiceCodec.TryDecode(encoded,
            out P12FActorChoiceSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.NextInputSequence, Is.EqualTo(3));
        Assert.That(decoded.CensusRevision, Is.EqualTo(5));
        Assert.That(decoded.Inputs.Select(input => input.InputSequence), Is.EqualTo(new long[] { 1, 2 }));
        Assert.That(decoded.Inputs[0].Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(decoded.Inputs[1].Dispositions[0].DecisionRecordId, Is.EqualTo("decision-2"));
        Assert.That(decoded.Inputs[1].Dispositions[1].ReturnedResultStatus, Is.EqualTo(NpcActionResultType.Success));
        Assert.That(P12AActorChoiceCodec.TryEncode(decoded, out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(EncodeRows(encodedAgain), Is.EqualTo(EncodeRows(encoded)));
    }

    [Test]
    public void ActorChoiceCodecSupportsTheExactEmptyInitialOwner()
    {
        P12FActorChoiceSnapshot source = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, Array.Empty<ActorChoiceInput>(), 1, 0);
        Assert.That(P12AActorChoiceCodec.TryEncode(source, out P12AOwnerSection encoded,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(encoded.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(encoded.Rows, Is.Empty);
        Assert.That(P12AActorChoiceCodec.TryDecode(encoded,
            out P12FActorChoiceSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.NextInputSequence, Is.EqualTo(1));
        Assert.That(decoded.CensusRevision, Is.Zero);
    }

    [Test]
    public void ActorChoiceCodecRejectsRevisionBelowRetainedInputCount()
    {
        ActorChoiceInput rejected = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000001"), "command-1", 1,
            new PersonId("person-1"), "action-a", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4, ActorChoiceInputStatus.Rejected,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Rejected, 4, 0,
                    failure: ActorChoiceFailure.ActionUnavailable)
            });
        P12FActorChoiceSnapshot impossible = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { rejected }, 2, 0);
        Assert.That(P12AActorChoiceCodec.TryEncode(impossible, out _, out _), Is.False);

        P12FActorChoiceSnapshot valid = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { rejected }, 2, 1);
        Assert.That(P12AActorChoiceCodec.TryEncode(valid, out P12AOwnerSection encoded, out _), Is.True);
        P12AOwnerSection lowRevision = new P12AOwnerSection(encoded.Id, encoded.Major, encoded.Minor,
            encoded.Role, encoded.AliasGroup, 0, encoded.Rows);
        Assert.That(P12AActorChoiceCodec.TryDecode(lowRevision, out _, out _), Is.False);
    }

    [Test]
    public void ActorChoiceCodecRejectsPendingOrDeferredDailyHistory()
    {
        ActorChoiceInput pending = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000001"), "command-1", 1,
            new PersonId("person-1"), "action-a", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4, ActorChoiceInputStatus.Pending,
            Array.Empty<ActorChoiceDisposition>());
        P12FActorChoiceSnapshot pendingSnapshot = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { pending }, 2, 1);
        Assert.That(P12AActorChoiceCodec.TryEncode(pendingSnapshot, out _, out _), Is.False);

        ActorChoiceInput deferred = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000001"), "command-1", 1,
            new PersonId("person-1"), "action-a", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4, ActorChoiceInputStatus.Pending,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Deferred, 4, 0,
                    deferralReason: ActorChoiceDeferralReason.Traveling)
            });
        P12FActorChoiceSnapshot deferredSnapshot = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { deferred }, 2, 2);
        Assert.That(P12AActorChoiceCodec.TryEncode(deferredSnapshot, out _, out _), Is.False);
    }

    [Test]
    public void ActorChoiceCodecRejectsRowsWhoseOwnerKeyDoesNotMatchInputIdentity()
    {
        ActorChoiceInput rejected = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-000001"), "command-1", 1,
            new PersonId("person-1"), "action-a", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 4, ActorChoiceInputStatus.Rejected,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Rejected, 4, 0,
                    failure: ActorChoiceFailure.ActorUnavailable)
            });
        P12FActorChoiceSnapshot source = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { rejected }, 2, 2);
        Assert.That(P12AActorChoiceCodec.TryEncode(source, out P12AOwnerSection encoded, out _), Is.True);
        P12AOwnerSection tampered = new P12AOwnerSection(encoded.Id, encoded.Major, encoded.Minor,
            encoded.Role, encoded.AliasGroup, encoded.Revision,
            new[] { new P12AOwnerRow("input/actor-choice-other", encoded.Rows[0].Revision,
                encoded.Rows[0].Payload) });
        Assert.That(P12AActorChoiceCodec.TryDecode(tampered, out _, out _), Is.False);
    }

    [Test]
    public void ScheduledDirectiveCodecPreservesPendingAndTerminalRows()
    {
        P12FScheduledDirectiveOwnerSnapshot source = new P12FScheduledDirectiveOwnerSnapshot(new[]
        {
            new P12FScheduledDirectiveOwnerSnapshot.Row("directive-pending", 3L,
                ScheduledDirectiveMode.RequestAction, ScheduledDirectiveOperation.EscapePrison,
                "npc-a", "action-a", ScheduledDirectiveState.Pending, -1L, null),
            new P12FScheduledDirectiveOwnerSnapshot.Row("directive-terminal", 4L,
                ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison,
                "npc-b", "action-b", ScheduledDirectiveState.Skipped, 2L, "preserved reason")
        }, 2L);

        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True,
            encodeError?.Code.ToString());
        Assert.That(encoded.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "directive/directive-pending", "directive/directive-terminal"
        }));
        Assert.That(P12AFScheduledDirectiveCodec.TryDecode(encoded,
            out P12FScheduledDirectiveOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.Rows[0].State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(decoded.Rows[0].ProcessedDay, Is.EqualTo(-1L));
        Assert.That(decoded.Rows[0].ResultReason, Is.Null);
        Assert.That(decoded.Rows[1].State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(decoded.Rows[1].ProcessedDay, Is.EqualTo(2L));
        Assert.That(decoded.Rows[1].ResultReason, Is.EqualTo("preserved reason"));
        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(EncodeRows(encodedAgain), Is.EqualTo(EncodeRows(encoded)));
    }

    [Test]
    public void ScheduledDirectiveCodecRejectsImpossibleEmptyOwnerRevision()
    {
        P12FScheduledDirectiveOwnerSnapshot impossible = new P12FScheduledDirectiveOwnerSnapshot(
            Array.Empty<P12FScheduledDirectiveOwnerSnapshot.Row>(), 1L);
        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(impossible, out _, out _), Is.False);

        P12AOwnerSection encodedEmpty = new P12AOwnerSection(
            ScheduledDirectiveCensusProvider.SectionId, ScheduledDirectiveCensusProvider.SchemaVersion,
            0, OwnerSectionRole.Required, null, 1L, Array.Empty<P12AOwnerRow>());
        Assert.That(P12AFScheduledDirectiveCodec.TryDecode(encodedEmpty, out _, out _), Is.False);
    }

    [Test]
    public void PoliticalKnowledgeCodecPreservesTypedHoldersObservationsAndProvenance()
    {
        PoliticalKnowledgeRuntime factionHolder = new PoliticalKnowledgeRuntime(
            PoliticalKnowledgeHolder.ForFaction(new FactionId("holder-faction")));
        PoliticalKnowledgeRuntime institutionHolder = new PoliticalKnowledgeRuntime(
            PoliticalKnowledgeHolder.ForInstitution(new InstitutionId("holder-institution")));
        PoliticalKnowledgeRuntime personHolder = new PoliticalKnowledgeRuntime(
            PoliticalKnowledgeHolder.ForPerson(new PersonId("holder-person")));

        Assert.That(personHolder.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            new PoliticalClaimId("claim-1"), true, new PersonId("claimant-1"),
            PoliticalClaimType.OfficeEntitlement, PoliticalClaimTarget.ForOffice(new OfficeId("office-1")),
            PoliticalClaimBasis.Genealogy, 1L, PoliticalClaimStatus.Active, null,
            PoliticalClaimRecognitionState.Unrecognized, null, null, 2L, 3L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.SharedByPerson, "claim-share",
                new PersonId("source-person"), null))), Is.True);
        Assert.That(personHolder.RecordFactionObservation(new FactionKnowledgeObservation(
            new FactionId("faction-1"), true, 1L, 2L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.InitialScenarioKnowledge, string.Empty))), Is.True);
        Assert.That(personHolder.RecordFactionAffiliationObservation(new FactionAffiliationKnowledgeObservation(
            new FactionId("faction-1"), new PersonId("member-1"), false, 2L, 4L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.SharedByInstitution, "affiliation-share",
                null, new InstitutionId("source-institution")))), Is.True);
        Assert.That(personHolder.RecordOfficeVacancyObservation(new OfficeVacancyKnowledgeObservation(
            new OfficeId("office-1"), new InstitutionId("institution-1"), true, 3L, 5L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.InstitutionalRecord, null,
                null, new InstitutionId("institution-1")), true)), Is.True);
        Assert.That(personHolder.RecordPersonDeathObservation(new PersonDeathKnowledgeObservation(
            new PersonId("person-deceased"), true, 4L, 5L, 6L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "direct-death"))), Is.True);

        P12FPoliticalKnowledgeOwnerSnapshot source = new P12FPoliticalKnowledgeOwnerSnapshot(
            new[] { factionHolder, institutionHolder, personHolder }, 8L);
        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True,
            encodeError?.Code.ToString());
        Assert.That(encoded.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "holder/Faction:holder-faction",
            "holder/Institution:holder-institution",
            "holder/Person:holder-person"
        }));
        Assert.That(P12AFPoliticalKnowledgeCodec.TryDecode(encoded,
            out P12FPoliticalKnowledgeOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.Revision, Is.EqualTo(8L));
        IReadOnlyList<PoliticalKnowledgeRuntime> restored = decoded.CopyDetachedRuntimes();
        Assert.That(restored.Select(runtime => runtime.Holder.Kind), Is.EqualTo(new[]
        {
            PoliticalKnowledgeHolderKind.Faction,
            PoliticalKnowledgeHolderKind.Institution,
            PoliticalKnowledgeHolderKind.Person
        }));
        IReadOnlyList<PoliticalKnowledgeObservation> observations = restored[2].Observations;
        Assert.That(observations, Has.Count.EqualTo(5));
        PoliticalClaimKnowledgeObservation claim = (PoliticalClaimKnowledgeObservation)observations[0];
        Assert.That(claim.ClaimId.Value, Is.EqualTo("claim-1"));
        Assert.That(claim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Office));
        Assert.That(claim.Target.TargetId, Is.EqualTo("office-1"));
        Assert.That(claim.Provenance.SourcePersonId.Value, Is.EqualTo("source-person"));
        Assert.That(((FactionKnowledgeObservation)observations[1]).Provenance.SourceReference, Is.EqualTo(string.Empty));
        FactionAffiliationKnowledgeObservation affiliation = (FactionAffiliationKnowledgeObservation)observations[2];
        Assert.That(affiliation.Provenance.SourceInstitutionId.Value, Is.EqualTo("source-institution"));
        OfficeVacancyKnowledgeObservation office = (OfficeVacancyKnowledgeObservation)observations[3];
        Assert.That(office.IsRecognizedVacant, Is.True);
        PersonDeathKnowledgeObservation death = (PersonDeathKnowledgeObservation)observations[4];
        Assert.That(death.DeathAbsoluteDay, Is.EqualTo(4L));

        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(EncodeRows(encodedAgain), Is.EqualTo(EncodeRows(encoded)));
    }

    [Test]
    public void PoliticalKnowledgeEmptyOwnerStagesFromImageWithoutCaptureContext()
    {
        P12FPoliticalKnowledgeOwnerSnapshot source = new P12FPoliticalKnowledgeOwnerSnapshot(
            Array.Empty<PoliticalKnowledgeRuntime>(), 0L);
        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True,
            encodeError?.Code.ToString());
        Assert.That(P12AFPoliticalKnowledgeCodec.TryDecode(encoded,
            out P12FPoliticalKnowledgeOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());

        PersonStore people = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        PoliticalClaimStore claims = new PoliticalClaimStore();
        FactionStore factions = new FactionStore(people);
        OfficeStore offices = new OfficeStore(institutions);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(decoded.TryStageForImage(people, institutions, claims, factions, offices, properties,
            0L, out PoliticalKnowledgeStore staged, out string failure), Is.True, failure);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
    }

    [Test]
    public void PoliticalKnowledgeDetachedNullRuntimeFailsEncodingAndStagingWithoutThrowing()
    {
        P12FPoliticalKnowledgeOwnerSnapshot snapshot = new P12FPoliticalKnowledgeOwnerSnapshot(
            new PoliticalKnowledgeRuntime[] { null }, 1L);

        Assert.That(snapshot.CopyDetachedRuntimes(), Is.Null);
        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(snapshot,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.False);
        Assert.That(encoded, Is.Null);
        Assert.That(encodeError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));

        PersonStore people = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        PoliticalClaimStore claims = new PoliticalClaimStore();
        FactionStore factions = new FactionStore(people);
        OfficeStore offices = new OfficeStore(institutions);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(snapshot.TryStageForImage(people, institutions, claims, factions, offices, properties,
            0L, out PoliticalKnowledgeStore staged, out string failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Does.Contain("null owner row"));
    }

    [Test]
    public void ExpeditionCodecRejectsBlankParticipantAndLocalReferenceIdentities()
    {
        P12FExpeditionOwnerSnapshotRecord valid = new P12FExpeditionOwnerSnapshotRecord(
            "expedition-1", "site-1", "location-a", "location-b", "route-a-b",
            "party-1", "decision-origin", new[] { "npc-a", "npc-b" },
            new[] { "npc-a" }, new[] { "npc-b" }, ExpeditionState.TravelingToSite,
            null, new[] { "place-first" }, new[] { "connection-first" },
            ExpeditionObjectiveType.Explore, null, null, null, 3, 2, false, true);
        P12FExpeditionOwnerSnapshotRecord blankMember = new P12FExpeditionOwnerSnapshotRecord(
            "expedition-2", "site-1", "location-a", "location-b", "route-a-b",
            "party-1", "decision-origin", new[] { "npc-a", " " },
            new[] { "npc-a" }, new[] { " " }, ExpeditionState.TravelingToSite,
            null, new[] { "place-first" }, new[] { "connection-first" },
            ExpeditionObjectiveType.Explore, null, null, null, 3, 2, false, true);
        P12FExpeditionOwnerSnapshot source = new P12FExpeditionOwnerSnapshot(
            P12FExpeditionOwnerSnapshot.CurrentSchemaVersion, 1, new[] { valid, blankMember });
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(source, out _, out _), Is.False);

        P12FExpeditionOwnerSnapshotRecord blankPlace = new P12FExpeditionOwnerSnapshotRecord(
            "expedition-1", "site-1", "location-a", "location-b", "route-a-b",
            "party-1", "decision-origin", new[] { "npc-a", "npc-b" },
            new[] { "npc-a" }, new[] { "npc-b" }, ExpeditionState.TravelingToSite,
            null, new[] { " " }, new[] { "connection-first" },
            ExpeditionObjectiveType.Explore, null, null, null, 3, 2, false, true);
        source = new P12FExpeditionOwnerSnapshot(
            P12FExpeditionOwnerSnapshot.CurrentSchemaVersion, 1, new[] { blankPlace });
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(source, out _, out _), Is.False);

        byte[] malformedPayload;
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.expedition/v1");
            P12AWire.WriteString(writer, "expedition-1");
            P12AWire.WriteString(writer, "site-1");
            P12AWire.WriteString(writer, "location-a");
            P12AWire.WriteString(writer, "location-b");
            P12AWire.WriteString(writer, "route-a-b");
            P12AWire.WriteString(writer, "party-1");
            P12AWire.WriteString(writer, "decision-origin");
            writer.Write(2); P12AWire.WriteString(writer, "npc-a"); P12AWire.WriteString(writer, "npc-b");
            writer.Write(1); P12AWire.WriteString(writer, "npc-a");
            writer.Write(1); P12AWire.WriteString(writer, "npc-b");
            writer.Write((ushort)ExpeditionState.TravelingToSite);
            P12AWire.WriteString(writer, null);
            writer.Write(1); P12AWire.WriteString(writer, " ");
            writer.Write(1); P12AWire.WriteString(writer, "connection-1");
            writer.Write((ushort)ExpeditionObjectiveType.Explore);
            P12AWire.WriteString(writer, null); P12AWire.WriteString(writer, null); P12AWire.WriteString(writer, null);
            writer.Write(3); writer.Write(2); writer.Write((byte)0); writer.Write((byte)1);
            writer.Flush();
            malformedPayload = stream.ToArray();
        }
        P12AOwnerSection malformed = new P12AOwnerSection(ExpeditionCensusProvider.SectionId,
            ExpeditionCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null, 1L,
            new[] { new P12AOwnerRow("expedition/expedition-1", 0L, malformedPayload) });
        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeExpeditions(malformed, out _, out _), Is.False);
    }

    [Test]
    public void TravelPartyCodecPreservesOrderedRolesCostsAndFloatBits()
    {
        P12FTravelPartyOwnerSnapshotRecord party = new P12FTravelPartyOwnerSnapshotRecord(
            "party-1", "location-a", "location-b", "route-a-b",
            new[] { "npc-traveler" }, new[] { "npc-escort" },
            new[] { "npc-traveler", "npc-escort" },
            new[]
            {
                new TravelPartyMemberCost("npc-traveler", -0.0f),
                new TravelPartyMemberCost("npc-escort", 12.25f)
            }, 3, "decision-origin");
        P12FTravelPartyOwnerSnapshot source = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, 4, new[] { party });

        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeTravelParties(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeTravelParties(encoded,
            out P12FTravelPartyOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.Parties[0].TravelerRuntimeIds, Is.EqualTo(new[] { "npc-traveler" }));
        Assert.That(decoded.Parties[0].EscortRuntimeIds, Is.EqualTo(new[] { "npc-escort" }));
        Assert.That(decoded.Parties[0].MemberRuntimeIds, Is.EqualTo(new[] { "npc-traveler", "npc-escort" }));
        Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(decoded.Parties[0].MemberCosts[0].Amount), 0),
            Is.EqualTo(BitConverter.ToInt32(BitConverter.GetBytes(-0.0f), 0)));
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeTravelParties(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(EncodeRows(encodedAgain), Is.EqualTo(EncodeRows(encoded)));
    }

    [Test]
    public void TravelPartyCodecRejectsMemberOrderThatCannotBeReconstructedFromRoles()
    {
        P12FTravelPartyOwnerSnapshotRecord party = new P12FTravelPartyOwnerSnapshotRecord(
            "party-1", "location-a", "location-b", "route-a-b",
            new[] { "npc-traveler" }, new[] { "npc-escort" },
            new[] { "npc-escort", "npc-traveler" },
            new[]
            {
                new TravelPartyMemberCost("npc-traveler", 1.0f),
                new TravelPartyMemberCost("npc-escort", 2.0f)
            }, 3, null);
        P12FTravelPartyOwnerSnapshot source = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, 1, new[] { party });
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeTravelParties(source, out _, out _), Is.False);
    }

    [Test]
    public void ExpeditionCodecPreservesProgressAndOrderedGraphReferences()
    {
        P12FExpeditionOwnerSnapshotRecord expedition = new P12FExpeditionOwnerSnapshotRecord(
            "expedition-1", "site-1", "location-a", "location-b", "route-a-b",
            "party-1", "decision-origin", new[] { "npc-a", "npc-b" },
            new[] { "npc-a" }, new[] { "npc-b" }, ExpeditionState.TravelingToSite,
            null, new[] { "place-first", "place-second" }, new[] { "connection-first" },
            ExpeditionObjectiveType.Explore, null, null, null, 3, 2, false, true);
        P12FExpeditionOwnerSnapshot source = new P12FExpeditionOwnerSnapshot(
            P12FExpeditionOwnerSnapshot.CurrentSchemaVersion, 2, new[] { expedition });

        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeExpeditions(encoded,
            out P12FExpeditionOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.Expeditions[0].MemberRuntimeIds, Is.EqualTo(new[] { "npc-a", "npc-b" }));
        Assert.That(decoded.Expeditions[0].VisitedLocalPlaceRuntimeIds,
            Is.EqualTo(new[] { "place-first", "place-second" }));
        Assert.That(decoded.Expeditions[0].Progress, Is.EqualTo(2));
        Assert.That(decoded.Expeditions[0].ObjectiveCompleted, Is.False);
        Assert.That(decoded.Expeditions[0].AllowContinueAfterCompletion, Is.True);
        Assert.That(decoded.Expeditions[0].CurrentLocalPlaceRuntimeId, Is.Null);
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(EncodeRows(encodedAgain), Is.EqualTo(EncodeRows(encoded)));
    }

    private static byte[][] EncodeRows(P12AOwnerSection section)
    {
        List<byte[]> rows = new List<byte[]>();
        foreach (P12AOwnerRow row in section.Rows)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, row.OwnerKey);
                writer.Write(row.Revision);
                writer.Write(row.PayloadLength);
                row.WritePayload(writer);
                writer.Flush();
                rows.Add(stream.ToArray());
            }
        }
        return rows.ToArray();
    }

    [Test]
    public void DeterministicRandomRootRoundTripsWithoutCreatingTheSourceProvider()
    {
        DeterministicRandomRootSnapshot source = new DeterministicRandomSource(17).CaptureSnapshot();
        Assert.That(P12ACausalRootOwnerCodec.TryEncodeRandomRoot(source, out byte[] encoded,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(P12ACausalRootOwnerCodec.TryDecodeRandomRoot(encoded,
            out DeterministicRandomRootSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.SchemaId, Is.EqualTo(source.SchemaId));
        Assert.That(decoded.SchemaVersion, Is.EqualTo(source.SchemaVersion));
        Assert.That(decoded.ProviderId, Is.EqualTo(source.ProviderId));
        Assert.That(decoded.ProviderVersion, Is.EqualTo(source.ProviderVersion));
        Assert.That(decoded.AlgorithmId, Is.EqualTo(source.AlgorithmId));
        Assert.That(decoded.AlgorithmVersion, Is.EqualTo(source.AlgorithmVersion));
        Assert.That(decoded.Seed, Is.EqualTo(source.Seed));
        byte[] trailing = new byte[encoded.Length + 1];
        Array.Copy(encoded, trailing, encoded.Length);
        Assert.That(P12ACausalRootOwnerCodec.TryDecodeRandomRoot(trailing, out _, out _), Is.False);
    }

    [Test]
    public void P9GenesisManifestCodecPreservesTheTypedProfileAndRejectsTrailingBytes()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        simulationObject = new GameObject("p12a-p9-manifest-codec-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, simulation.Bootstrap.Manifest,
            out P12CP9GenesisManifestSnapshot original, out string captureError), Is.True, captureError);
        Assert.That(P12ACP9ManifestCodec.TryEncode(original, out byte[] bytes, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ACP9ManifestCodec.TryDecode(bytes, out P12CP9GenesisManifestSnapshot decoded,
            out P12AResult decodeError), Is.True, decodeError?.Code.ToString());

        Assert.That(decoded.SnapshotContract, Is.EqualTo(original.SnapshotContract));
        Assert.That(decoded.SnapshotSchemaVersion, Is.EqualTo(original.SnapshotSchemaVersion));
        Assert.That(decoded.P12AdmissionProfile, Is.EqualTo(original.P12AdmissionProfile));
        Assert.That(decoded.ContractIdentity, Is.EqualTo(original.ContractIdentity));
        Assert.That(decoded.SchemaVersion, Is.EqualTo(original.SchemaVersion));
        Assert.That(decoded.Fingerprint, Is.EqualTo(original.Fingerprint));
        Assert.That(decoded.SelectedP9ProfileFingerprint, Is.EqualTo(original.SelectedP9ProfileFingerprint));
        Assert.That(decoded.SelectedP9ContractIdentity, Is.EqualTo(original.SelectedP9ContractIdentity));
        Assert.That(decoded.SelectedP9SchemaVersion, Is.EqualTo(original.SelectedP9SchemaVersion));
        Assert.That(decoded.EffectiveConfiguration, Is.EqualTo(original.EffectiveConfiguration));
        Assert.That(decoded.CanonicalProvenanceRecords, Is.EqualTo(original.CanonicalProvenanceRecords));
        Assert.That(decoded.AuthoredDefinitionIds, Is.EqualTo(original.AuthoredDefinitionIds));
        Assert.That(decoded.OutputOwners, Is.EqualTo(original.OutputOwners));
        Assert.That(decoded.StageDependencyRecords, Is.EqualTo(original.StageDependencyRecords));
        Assert.That(decoded.CalendarMonthsPerYear, Is.EqualTo(original.CalendarMonthsPerYear));
        Assert.That(decoded.CalendarWeeksPerMonth, Is.EqualTo(original.CalendarWeeksPerMonth));
        Assert.That(decoded.CalendarDaysPerWeek, Is.EqualTo(original.CalendarDaysPerWeek));
        Assert.That(decoded.MonthLengths, Is.EqualTo(original.MonthLengths));
        Assert.That(decoded.Seed, Is.EqualTo(original.Seed));
        Assert.That(decoded.SeedSource, Is.EqualTo(original.SeedSource));
        Assert.That(decoded.StageOrder, Is.EqualTo(original.StageOrder));
        Assert.That(decoded.FirstSimulatedBoundary, Is.EqualTo(original.FirstSimulatedBoundary));
        Assert.That(P12ACP9ManifestCodec.TryEncode(decoded, out byte[] reencoded, out _), Is.True);
        Assert.That(reencoded, Is.EqualTo(bytes));

        byte[] trailing = new byte[bytes.Length + 1];
        Array.Copy(bytes, trailing, bytes.Length);
        Assert.That(P12ACP9ManifestCodec.TryDecode(trailing, out P12CP9GenesisManifestSnapshot rejected,
            out _), Is.False);
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void DailyV1ManifestRequiresCompleteFixedAndDynamicProviderFamilies()
    {
        List<OwnerSectionCensusSnapshot> snapshots = new List<OwnerSectionCensusSnapshot>();
        Dictionary<string, object> ownersByAlias = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string id in P12ADailyV1Census.FixedRequiredSectionIds)
            snapshots.Add(CensusRow(id, OwnerSectionRole.Required, CensusOwner(id, ownersByAlias), 0, 1));
        foreach (string id in P12ADailyV1Census.FixedEmptySectionIds)
            snapshots.Add(CensusRow(id, OwnerSectionRole.ExplicitlyEmpty, CensusOwner(id, ownersByAlias), 0, 0));
        string npc = "npc-0001";
        foreach (string prefix in P12ADailyV1Census.NpcSectionPrefixes)
            snapshots.Add(CensusRow(prefix + npc, OwnerSectionRole.Required,
                CensusOwner(prefix + npc, ownersByAlias), 0, 1));
        snapshots.Add(CensusRow("p12b.npc-residence/" + npc, OwnerSectionRole.Required,
            CensusOwner("p12b.npc-residence/" + npc, ownersByAlias), 1, 1));
        foreach (string prefix in P12ADailyV1Census.CitySectionPrefixes)
        {
            string citySuffix = prefix == "p12b.city.important-npcs/" ? "city-0001" : "9:city-0001";
            string id = prefix + citySuffix;
            snapshots.Add(CensusRow(id, OwnerSectionRole.Required, CensusOwner(id, ownersByAlias), 0, 1));
        }

        Assert.That(snapshots.Count, Is.EqualTo(88));
        Assert.That(P12ADailyV1Census.TryCreate(snapshots, out P12ADailyV1Census census, out P12AResult error),
            Is.True, error?.Code.ToString());
        Assert.That(census.Sections.Count, Is.EqualTo(88));
        Assert.That(census.Sections[Array.FindIndex(census.Sections.ToArray(), value =>
            value.Id == "p12f.local-topology-knowledge.places/" + npc)].AliasGroup,
            Is.EqualTo("alias/p12f.local-topology-knowledge.connections/" + npc));

        snapshots.RemoveAt(snapshots.Count - 1);
        Assert.That(P12ADailyV1Census.TryCreate(snapshots, out _, out _), Is.False,
            "A partial City family must not produce a Daily-v1 manifest.");
    }

    [Test]
    public void DailyV1ImageComposerBuildsExactEmptyRowsAndRequiresCompleteOwnerSections()
    {
        List<OwnerSectionCensusSnapshot> snapshots = new List<OwnerSectionCensusSnapshot>();
        Dictionary<string, object> ownersByAlias = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string id in P12ADailyV1Census.FixedRequiredSectionIds)
            snapshots.Add(CensusRow(id, OwnerSectionRole.Required, CensusOwner(id, ownersByAlias), 0, 1));
        foreach (string id in P12ADailyV1Census.FixedEmptySectionIds)
            snapshots.Add(CensusRow(id, OwnerSectionRole.ExplicitlyEmpty, CensusOwner(id, ownersByAlias), 0, 0));
        const string npc = "npc-0001";
        foreach (string prefix in P12ADailyV1Census.NpcSectionPrefixes)
            snapshots.Add(CensusRow(prefix + npc, OwnerSectionRole.Required,
                CensusOwner(prefix + npc, ownersByAlias), 0, 1));
        snapshots.Add(CensusRow("p12b.npc-residence/" + npc, OwnerSectionRole.Required,
            CensusOwner("p12b.npc-residence/" + npc, ownersByAlias), 1, 1));
        foreach (string prefix in P12ADailyV1Census.CitySectionPrefixes)
        {
            string suffix = prefix == "p12b.city.important-npcs/" ? "city-0001" : "9:city-0001";
            string id = prefix + suffix;
            snapshots.Add(CensusRow(id, OwnerSectionRole.Required, CensusOwner(id, ownersByAlias), 0, 1));
        }

        List<P12AOwnerSection> requiredSections = new List<P12AOwnerSection>();
        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
        {
            if (snapshot.Role == OwnerSectionRole.ExplicitlyEmpty) continue;
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Cardinality);
            for (int i = 0; i < snapshot.Cardinality; i++)
                rows.Add(new P12AOwnerRow(snapshot.SectionId + ":" + i,
                    snapshot.Revision, Array.Empty<byte>()));
            requiredSections.Add(new P12AOwnerSection(snapshot.SectionId,
                snapshot.SchemaVersion, 0, snapshot.Role,
                P12ADailyV1Census.ExpectedAliasGroupForSection(snapshot.SectionId),
                snapshot.Revision, rows));
        }

        P12AContinuationHeader header = Header(Descriptor());
        Assert.That(P12ADailyV1ImageComposer.TryCompose(header, CausalRoots(), snapshots,
            requiredSections, out P12AContinuationImage image,
            out P12ADailyV1SectionManifest manifest, out P12AResult result),
            Is.True, result?.SectionCode + ":" + result?.Code);
        Assert.That(image.Sections, Has.Count.EqualTo(snapshots.Count));
        Assert.That(image.Sections.Count(section => section.Role == OwnerSectionRole.ExplicitlyEmpty),
            Is.EqualTo(P12ADailyV1Census.FixedEmptySectionIds.Count));
        Assert.That(manifest.Matches(image.Sections, out _), Is.True);
        Assert.That(P12AContinuationCodec.TryEncode(image, manifest, out byte[] bytes, out _), Is.True);
        Assert.That(P12AContinuationCodec.TryDecode(bytes, manifest, header.Compatibility,
            out P12AContinuationImage decoded, out _), Is.True);
        Assert.That(decoded.Sections.Select(section => section.Id),
            Is.EqualTo(image.Sections.Select(section => section.Id)));

        Assert.That(P12ADailyV1ImageComposer.TryCompose(header, CausalRoots(), snapshots,
            requiredSections.Take(requiredSections.Count - 1).ToArray(), out _, out _, out _), Is.False,
            "A missing required owner section must not become an image.");
        P12AExactEmptyOwnerCodec.TryEncode(snapshots,
            out IReadOnlyList<P12AOwnerSection> exactEmpty, out _);
        Assert.That(P12ADailyV1ImageComposer.TryCompose(header, CausalRoots(), snapshots,
            requiredSections.Concat(exactEmpty).ToArray(), out _, out _, out _), Is.False,
            "Fixed exact-empty sections are owned by the composer and cannot be supplied twice.");
    }

    [Test]
    public void CityOwnerCodecPreservesTypedIdsHistoryAndCanonicalMapRows()
    {
        const string cityId = "city-test-1";
        SettlementPopulationTransition transition = new SettlementPopulationTransition(
            cityId, 4L, 10, new PopulationChangeSet(2, 1, 1, 0), 2L, 12);
        P12DCityRootOwnerSnapshot source = new P12DCityRootOwnerSnapshot(
            cityId,
            "city-definition-1",
            "location-test-1",
            3L,
            new[] { "npc-z", "npc-a" },
            cityId,
            MarketLiquidityMode.Open,
            8L,
            new[]
            {
                new P12DCityMarketItemSnapshot("item-z", 5, 10, 1.25f),
                new P12DCityMarketItemSnapshot("item-a", 2, 4, 0.75f)
            },
            "population-" + cityId,
            ConsumptionPaymentMode.Free,
            cityId,
            12,
            4L,
            1L,
            new[]
            {
                new PopulationOperationReceiptSnapshot("population-op-1", "fingerprint-1", transition)
            });

        Assert.That(P12ACityOwnerCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodedResult),
            Is.True, encodedResult == null ? "encode" : encodedResult.Code.ToString());
        Assert.That(sections, Has.Count.EqualTo(4));
        P12AOwnerSection market = sections.Single(section =>
            section.Id.StartsWith(CityMarketCensusProvider.SectionIdPrefix, StringComparison.Ordinal));
        Assert.That(market.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "market-item/item-a", "market-item/item-z" }));

        Dictionary<string, P12AOwnerSection> byId = sections.ToDictionary(section => section.Id,
            StringComparer.Ordinal);
        Assert.That(P12ACityOwnerCodec.TryDecode(
            byId[CityNpcPresenceCensusProvider.SectionIdFor(cityId)],
            market,
            byId[SettlementPopulationCensusProvider.AggregateSectionPrefix + "11:" + cityId],
            byId[SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + "11:" + cityId],
            out P12DCityRootOwnerSnapshot restored, out P12AResult decodedResult),
            Is.True, decodedResult == null ? "decode" : decodedResult.Code.ToString());

        P12AOwnerSection unsortedMarket = new P12AOwnerSection(market.Id, market.Major, market.Minor,
            market.Role, market.AliasGroup, market.Revision, market.Rows.Reverse());
        Assert.That(P12ACityOwnerCodec.TryDecode(
            byId[CityNpcPresenceCensusProvider.SectionIdFor(cityId)],
            unsortedMarket,
            byId[SettlementPopulationCensusProvider.AggregateSectionPrefix + "11:" + cityId],
            byId[SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + "11:" + cityId],
            out _, out P12AResult orderingResult), Is.False);
        Assert.That(orderingResult.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));

        Assert.That(restored.CityRuntimeId, Is.EqualTo(source.CityRuntimeId));
        Assert.That(restored.CityDefinitionId, Is.EqualTo(source.CityDefinitionId));
        Assert.That(restored.LegacyLocationRuntimeId, Is.EqualTo(source.LegacyLocationRuntimeId));
        Assert.That(restored.ImportantNpcRuntimeIds,
            Is.EqualTo(new[] { "npc-z", "npc-a" }), "membership order is owner-defined");
        Assert.That(restored.MarketItems.Select(item => item.ItemDefinitionId),
            Is.EqualTo(new[] { "item-a", "item-z" }));
        Assert.That(restored.MarketItems[0].CurrentPrice, Is.EqualTo(0.75f));
        Assert.That(restored.PopulationReceiptRevision, Is.EqualTo(1L));
        Assert.That(restored.PopulationReceipts.Single().Identity, Is.EqualTo("population-op-1"));
        Assert.That(restored.PopulationReceipts.Single().Transition.Equals(transition), Is.True);
        Assert.That(restored.PopulationEconomicRuntimeId, Is.EqualTo("population-" + cityId));
        Assert.That(restored.MarketCounterpartyHasAccount, Is.False);
        Assert.That(restored.PopulationEconomyHasAccount, Is.False);
    }

    [Test]
    public void SelectedDailyV1RuntimeProducesExactPromotedCensusManifest()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        simulationObject = new GameObject("p12a-census-manifest-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        Assert.That(protocol.TryCaptureQuiescentOwnerSectionSnapshot(
            out IReadOnlyList<OwnerSectionCensusSnapshot> snapshot,
            out long mutationEpoch,
            out ContinuationCensusFailure failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.GreaterThanOrEqualTo(0));
        Assert.That(snapshot, Has.Count.EqualTo(299));
        Assert.That(P12ADailyV1Census.TryCreate(snapshot, out P12ADailyV1Census census, out P12AResult error),
            Is.True, error?.Code.ToString());
        Assert.That(census.Sections, Has.Count.EqualTo(299));
        Assert.That(census.Sections.Count(section => section.Role == OwnerSectionRole.ExplicitlyEmpty), Is.EqualTo(13));
        Assert.That(census.Sections.Count(section => section.Role == OwnerSectionRole.Required), Is.EqualTo(286));

        Assert.That(P12CSpatialAuthoritySnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            SimulationGenesisPipeline.GeographyProfileContractIdentity, 2,
            runtime.SpatialAuthorityStore, out P12CSpatialAuthoritySnapshot geography,
            out P12CSpatialAuthoritySnapshotFailure geographyFailure), Is.True, geographyFailure.ToString());
        Assert.That(P12ACSpatialAuthorityCodec.TryComputeFingerprint(geography, out string p8Fingerprint), Is.True);
        P12CSpatialHexSnapshot sourceHex = geography.Hexes[0];
        P12CSpatialAuthoritySnapshot changedGeography = new P12CSpatialAuthoritySnapshot(
            geography.SchemaId, geography.SchemaVersion, geography.AdmissionProfile,
            geography.P9ProfileContractIdentity, geography.P9ProfileSchemaVersion, geography.OwnerRevision,
            geography.CoordinateConventionVersion, geography.CoordinateCanonicalOrder,
            new[] { new P12CSpatialHexSnapshot(sourceHex.Id, sourceHex.Q + 1, sourceHex.R,
                sourceHex.TerrainDefinitionId, sourceHex.AuthoredRevisionToken) },
            geography.Locations, geography.ScaleContext);
        Assert.That(P12ACSpatialAuthorityCodec.TryComputeFingerprint(changedGeography,
            out string changedP8Fingerprint), Is.True);
        Assert.That(changedP8Fingerprint, Is.Not.EqualTo(p8Fingerprint),
            "The compatibility identity binds the exact P8-A typed geography values.");
        Assert.That(P12CWorldIdentitySnapshot.TryCapture(runtime.WorldId,
            out P12CWorldIdentitySnapshot worldIdentity, out string worldIdentityError), Is.True, worldIdentityError);
        RuntimeIdAllocatorSnapshot allocatorSnapshot = simulation.Bootstrap.RuntimeIdAllocator.CaptureSnapshot();
        SimulationRecordSequenceSnapshot recordSequenceSnapshot = simulation.Bootstrap.RecordSequence.CaptureSnapshot();
        SimulationActiveSession activeSession = (SimulationActiveSession)typeof(TesteSimulacao)
            .GetMethod("CaptureActiveSession", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(simulation, null);
        object randomOwner = activeSession.RandomSource;
        Assert.That(randomOwner, Is.TypeOf<DeterministicRandomSource>());
        DeterministicRandomRootSnapshot randomSnapshot =
            ((DeterministicRandomSource)randomOwner).CaptureSnapshot();
        P12CP9GenesisManifestSnapshot p9Manifest = GetP9Manifest();
        P12ACompatibilityDescriptor descriptor = Descriptor(p8Fingerprint);
        P12AContinuationHeader header = new P12AContinuationHeader(
            worldIdentity.WorldIdValue, runtime.CurrentDay, 1L, -1L,
            p9Manifest.SelectedP9ContractIdentity, p9Manifest.Fingerprint,
            "P8-A", p8Fingerprint, 37L, descriptor);
        Assert.That(P12ACausalRootImageAdapter.TryEncode(header, worldIdentity,
            allocatorSnapshot, recordSequenceSnapshot, geography, p9Manifest, randomSnapshot,
            out P12ACausalRootPayload typedRoots, out IReadOnlyList<P12AOwnerSection> typedCSections,
            out P12AResult rootEncodeError), Is.True, rootEncodeError?.Code.ToString());

        Dictionary<string, P12AOwnerSection> typedCById = typedCSections.ToDictionary(
            section => section.Id, StringComparer.Ordinal);
        List<P12AOwnerSection> imageSections = new List<P12AOwnerSection>(census.Sections.Count);
        foreach (P12ACensusSectionValue section in census.Sections)
        {
            if (typedCById.TryGetValue(section.Id, out P12AOwnerSection typedCSection))
            {
                Assert.That(typedCSection.Major, Is.EqualTo(section.SchemaVersion));
                Assert.That(typedCSection.Role, Is.EqualTo(section.Role));
                Assert.That(typedCSection.AliasGroup, Is.EqualTo(section.AliasGroup));
                Assert.That(typedCSection.Rows.Count, Is.EqualTo(section.Cardinality));
                Assert.That(typedCSection.Revision, Is.EqualTo(section.Revision));
                imageSections.Add(typedCSection);
                continue;
            }
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(section.Cardinality);
            for (int i = 0; i < section.Cardinality; i++)
                rows.Add(new P12AOwnerRow("row-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    section.Revision, Array.Empty<byte>()));
            imageSections.Add(new P12AOwnerSection(section.Id, section.SchemaVersion, 0, section.Role,
                section.AliasGroup, section.Revision, rows));
        }
        P12AContinuationImage syntheticImage = new P12AContinuationImage(
            header, typedRoots, imageSections);
        Assert.That(census.MatchesImage(syntheticImage), Is.True);
        Assert.That(P12ADailyV1Census.TryCreateForImage(syntheticImage,
            out P12ADailyV1Census imageCensus, out P12AResult imageError), Is.True, imageError?.Code.ToString());
        Assert.That(census.LogicallyEquals(imageCensus), Is.True);
        Assert.That(P12AContinuationCodec.TryEncode(syntheticImage, census.Manifest,
            out byte[] frame, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(BitConverter.ToUInt32(frame, 16), Is.EqualTo(300),
            "The frame carries one typed C-root section alongside, not inside, the 299 census provider rows.");
        Assert.That(P12AContinuationCodec.TryDecode(frame, census.Manifest, descriptor,
            out P12AContinuationImage decodedImage, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decodedImage.Sections, Has.Count.EqualTo(299));
        Assert.That(decodedImage.CausalRoots.P9Manifest.Fingerprint, Is.EqualTo(GetP9Manifest().Fingerprint));
        Assert.That(decodedImage.CausalRoots.RandomRoot.Seed, Is.EqualTo(GetP9Manifest().Seed));
        Assert.That(P12ACausalRootImageAdapter.TryDecode(decodedImage,
            out P12ACausalRootImageSnapshots decodedC, out P12AResult rootDecodeError), Is.True,
            rootDecodeError?.Code.ToString());
        AssertAllocatorSnapshotsEqual(allocatorSnapshot, decodedC.Allocator);
        Assert.That(decodedC.RecordSequence.NextSequence, Is.EqualTo(recordSequenceSnapshot.NextSequence));
        Assert.That(decodedC.WorldIdentity.WorldIdValue, Is.EqualTo(worldIdentity.WorldIdValue));
        Assert.That(P12ACSpatialAuthorityCodec.TryComputeFingerprint(decodedC.SpatialAuthority,
            out string decodedP8Fingerprint), Is.True);
        Assert.That(decodedP8Fingerprint, Is.EqualTo(p8Fingerprint));
        Assert.That(P12ACausalRootImageAdapter.TryStage(decodedImage,
            out P12CStagedContinuationRoot detachedC, out P12AResult stageResult), Is.True,
            stageResult?.Code.ToString());
        Assert.That(detachedC.IsDetached, Is.True);
        Assert.That(detachedC.StagingAttempt, Is.Null);
        Assert.That(detachedC.WorldIdentity, Is.Not.SameAs(runtime.WorldId));

        List<P12AOwnerSection> untrustedAliases = new List<P12AOwnerSection>();
        foreach (P12AOwnerSection section in syntheticImage.Sections)
        {
            string alias = section.Id == "p12b.justice-records" || section.Id == "p12b.justice-p18-receipts"
                ? null : section.AliasGroup;
            untrustedAliases.Add(new P12AOwnerSection(section.Id, section.Major, section.Minor, section.Role,
                alias, section.Revision, section.Rows));
        }
        P12AContinuationImage forgedAliasImage = new P12AContinuationImage(syntheticImage.Header,
            syntheticImage.CausalRoots, untrustedAliases);
        Assert.That(P12ADailyV1Census.TryCreateForImage(forgedAliasImage, out _, out _), Is.False,
            "Alias equivalence is checked against the fixed owner mapping, not regenerated from image claims.");

        Assert.That(geography.SchemaId, Is.EqualTo(P12CSpatialAuthoritySnapshot.CurrentSchemaId));
        Assert.That(geography.SchemaVersion, Is.EqualTo(P12CSpatialAuthoritySnapshot.CurrentSchemaVersion));
        Assert.That(geography.AdmissionProfile, Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(geography.P9ProfileContractIdentity, Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        Assert.That(geography.P9ProfileSchemaVersion, Is.EqualTo(2));
        Assert.That(geography.OwnerRevision, Is.EqualTo(1));
        Assert.That(geography.CoordinateConventionVersion, Is.EqualTo(HexCoordinate.ConventionVersion));
        Assert.That(geography.CoordinateCanonicalOrder, Is.EqualTo(HexCoordinate.CanonicalOrder));
        Assert.That(geography.Hexes, Has.Count.EqualTo(1));
        Assert.That(geography.Locations, Has.Count.EqualTo(1));
        Assert.That(geography.ScaleContext.DistancePerNeighborStep, Is.GreaterThan(0m));
        string p8Alias = census.Sections.Single(section => section.Id == "p8a.hexes").AliasGroup;
        Assert.That(P12ACSpatialAuthorityCodec.TryEncode(geography, p8Alias,
            out P12AOwnerSection hexSection, out P12AOwnerSection locationSection,
            out P12AOwnerSection scaleSection, out P12AResult spatialEncodeError), Is.True,
            spatialEncodeError == null ? null : spatialEncodeError.Code + "/" + spatialEncodeError.SectionCode
                + "/" + spatialEncodeError.StableOwnerKey);
        Assert.That(P12ACSpatialAuthorityCodec.TryDecode(hexSection, locationSection, scaleSection,
            out P12CSpatialAuthoritySnapshot decodedGeography, out P12AResult spatialDecodeError), Is.True,
            spatialDecodeError?.Code.ToString());
        Assert.That(decodedGeography.Hexes[0].Id, Is.EqualTo(geography.Hexes[0].Id));
        Assert.That(decodedGeography.Hexes[0].Q, Is.EqualTo(geography.Hexes[0].Q));
        Assert.That(decodedGeography.Hexes[0].R, Is.EqualTo(geography.Hexes[0].R));
        Assert.That(decodedGeography.Hexes[0].TerrainDefinitionId, Is.EqualTo(geography.Hexes[0].TerrainDefinitionId));
        Assert.That(decodedGeography.Hexes[0].AuthoredRevisionToken, Is.EqualTo(geography.Hexes[0].AuthoredRevisionToken));
        Assert.That(decodedGeography.Locations[0].Id, Is.EqualTo(geography.Locations[0].Id));
        Assert.That(decodedGeography.Locations[0].AnchorHexId, Is.EqualTo(geography.Locations[0].AnchorHexId));
        Assert.That(decodedGeography.ScaleContext.DistancePerNeighborStep,
            Is.EqualTo(geography.ScaleContext.DistancePerNeighborStep));
    }

    private P12AContinuationImage Image(P12ACompatibilityDescriptor descriptor, byte[] payload)
    {
        P12AContinuationImage image = new P12AContinuationImage(Header(descriptor), CausalRoots(), new[]
        {
            Section("a.required", OwnerSectionRole.Required, new P12AOwnerRow("owner:alpha", 7, payload)),
            Section("z.empty", OwnerSectionRole.ExplicitlyEmpty)
        });
        payload[0] = 0xff;
        return image;
    }

    private static P12AOwnerSection Section(string id, OwnerSectionRole role, params P12AOwnerRow[] rows)
    { return new P12AOwnerSection(id, 1, 0, role, null, 0, rows); }

    private static OwnerSectionCensusSnapshot CensusRow(string id, OwnerSectionRole role,
        object owner, int cardinality, long revision)
    { return new OwnerSectionCensusSnapshot(id, 1, role, owner, cardinality, revision); }

    private static object CensusOwner(string id, Dictionary<string, object> ownersByAlias)
    {
        string alias = P12ADailyV1Census.ExpectedAliasGroupForSection(id);
        if (alias == null) return new object();
        if (!ownersByAlias.TryGetValue(alias, out object owner))
        { owner = new object(); ownersByAlias.Add(alias, owner); }
        return owner;
    }

    private P12AContinuationHeader Header(P12ACompatibilityDescriptor descriptor)
    {
        P12CP9GenesisManifestSnapshot manifest = GetP9Manifest();
        return new P12AContinuationHeader("world:00112233445566778899aabbccddeeff", 12, 4, 10,
            manifest.SelectedP9ContractIdentity, manifest.Fingerprint, "P8-A", descriptor.P8Fingerprint,
            73L, descriptor);
    }

    private P12ACausalRootPayload CausalRoots()
    {
        P12CP9GenesisManifestSnapshot manifest = GetP9Manifest();
        return new P12ACausalRootPayload(manifest,
            new DeterministicRandomSource(manifest.Seed).CaptureSnapshot());
    }

    private P12CP9GenesisManifestSnapshot GetP9Manifest()
    {
        if (p9Manifest != null) return p9Manifest;
        if (simulationObject == null)
        {
            SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
                "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
            Assert.That(config, Is.Not.Null);
            simulationObject = new GameObject("p12a-p9-fixture");
            TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
            typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(simulation, config);
            typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
            simulation.Start();
        }
        TesteSimulacao owner = simulationObject.GetComponent<TesteSimulacao>();
        Assert.That(owner, Is.Not.Null);
        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, owner.Bootstrap.Manifest,
            out p9Manifest, out string diagnostic), Is.True, diagnostic);
        return p9Manifest;
    }

    private P12ACompatibilityDescriptor Descriptor(string p8Fingerprint = "p8")
    {
        return new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1", 1, "config", "6000.3.9f1",
            "WindowsEditor", "Mono", "build", new[] { "00000000-0000-0000-0000-000000000001" },
            "numeric", 1, "content", "providers", "daily", 1, "calendar", GetP9Manifest().Fingerprint, p8Fingerprint);
    }

    private static void AssertAllocatorSnapshotsEqual(RuntimeIdAllocatorSnapshot expected,
        RuntimeIdAllocatorSnapshot actual)
    {
        Assert.That(actual.SchemaId, Is.EqualTo(expected.SchemaId));
        Assert.That(actual.SchemaVersion, Is.EqualTo(expected.SchemaVersion));
        Assert.That(actual.Counters, Has.Count.EqualTo(expected.Counters.Count));
        for (int i = 0; i < expected.Counters.Count; i++)
        {
            Assert.That(actual.Counters[i].FamilyId, Is.EqualTo(expected.Counters[i].FamilyId));
            Assert.That(actual.Counters[i].NextSequence, Is.EqualTo(expected.Counters[i].NextSequence));
        }
    }

    private sealed class ReplaceFailureFileOperations : IP12AContinuationFileOperations
    {
        private readonly IP12AContinuationFileOperations inner = P12AContinuationFileOperations.Instance;
        public void CreateDirectory(string path) => inner.CreateDirectory(path);
        public bool Exists(string path) => inner.Exists(path);
        public Stream CreateNewTemporary(string path) => inner.CreateNewTemporary(path);
        public Stream OpenRead(string path) => inner.OpenRead(path);
        public void FlushToDisk(Stream stream) => inner.FlushToDisk(stream);
        public void Replace(string source, string destination) => throw new IOException("Injected replace failure.");
        public void Move(string source, string destination) => inner.Move(source, destination);
        public void Delete(string path) => inner.Delete(path);
    }
}
