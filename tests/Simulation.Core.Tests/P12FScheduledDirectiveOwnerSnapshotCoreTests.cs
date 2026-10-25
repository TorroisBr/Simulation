using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12FScheduledDirectiveOwnerSnapshotCoreTests
{
    [Test]
    public void CaptureAndStageUseSelectedEscapeDefinitionAndPreserveDirectiveState()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition escape = FindAction(definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        SimulationTime sourceTime = new SimulationTime();
        ScheduledDirectiveStore source = new ScheduledDirectiveStore(sourceTime);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-stable", 4L, ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison, "npc-not-present",
            escape.DefinitionId, DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        Assert.That(source.Add(directive), Is.True);
        Assert.That(directive.MarkSkipped(2L, "preserved disposition"), Is.True);

        IReadOnlyList<OwnerSectionCensusSnapshot> vector = CurrentVector(source);
        DailyCaptureEligibilityToken token = CreateToken(vector);
        Assert.That(P12FScheduledDirectiveOwnerSnapshot.TryCapture(source, token, vector,
            out P12FScheduledDirectiveOwnerSnapshot snapshot, out string failure), Is.True, failure);
        Assert.That(snapshot.Revision, Is.EqualTo(source.Revision));

        Assert.That(snapshot.TryStage(token, vector, new SimulationTime(), definitions,
            out ScheduledDirectiveStore staged, out failure), Is.True, failure);
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));
        Assert.That(staged.Directives, Has.Count.EqualTo(1));
        Assert.That(staged.Directives[0].DirectiveId, Is.EqualTo("directive-stable"));
        Assert.That(staged.Directives[0].ActionDefinitionId, Is.EqualTo(escape.DefinitionId));
        Assert.That(staged.Directives[0].ActorRuntimeId, Is.EqualTo("npc-not-present"),
            "P12-F preserves a directive whose actor is absent from the staged roster.");
        Assert.That(staged.Directives[0].State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(staged.Directives[0].ProcessedDay, Is.EqualTo(2L));
        Assert.That(staged.Directives[0].ResultReason, Is.EqualTo("preserved disposition"));
        Assert.That(source.Revision, Is.EqualTo(snapshot.Revision));
    }

    [Test]
    public void StagingRejectsActionMissingFromSelectedGraphAndNonEscapeBinding()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition nonEscape = FindAction(definitions, DailyV1NpcDefinitionRules.NormalActionTypeValue);

        P12FScheduledDirectiveOwnerSnapshot missingAction = ImageSnapshot("action.missing-from-profile");
        Assert.That(missingAction.TryStageForImage(new SimulationTime(), definitions,
            out ScheduledDirectiveStore missingStaged, out _), Is.False);
        Assert.That(missingStaged, Is.Null);

        P12FScheduledDirectiveOwnerSnapshot wrongType = ImageSnapshot(nonEscape.DefinitionId);
        Assert.That(wrongType.TryStageForImage(new SimulationTime(), definitions,
            out ScheduledDirectiveStore wrongTypeStaged, out _), Is.False);
        Assert.That(wrongTypeStaged, Is.Null);
    }

    [Test]
    public void CoreDirectiveCodecRoundTripsPendingAndTerminalRowsDeterministically()
    {
        P12FScheduledDirectiveOwnerSnapshot source = new P12FScheduledDirectiveOwnerSnapshot(new[]
        {
            new P12FScheduledDirectiveOwnerSnapshot.Row(
                "directive-pending", 3L, ScheduledDirectiveMode.RequestAction,
                ScheduledDirectiveOperation.EscapePrison, "npc-pending", "action.escape",
                ScheduledDirectiveState.Pending, -1L, null),
            new P12FScheduledDirectiveOwnerSnapshot.Row(
                "directive-terminal", 4L, ScheduledDirectiveMode.RequestAction,
                ScheduledDirectiveOperation.EscapePrison, "npc-terminal", "action.escape",
                ScheduledDirectiveState.Skipped, 4L, "preserved reason")
        }, 7L);

        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult result), Is.True, result?.Code.ToString());
        Assert.That(result, Is.Null);
        Assert.That(encoded.Id, Is.EqualTo(ScheduledDirectiveCensusProvider.SectionId));
        Assert.That(encoded.Revision, Is.EqualTo(7L));

        Assert.That(P12AFScheduledDirectiveCodec.TryDecode(encoded,
            out P12FScheduledDirectiveOwnerSnapshot decoded, out result), Is.True, result?.Code.ToString());
        Assert.That(decoded.Rows, Has.Count.EqualTo(2));
        Assert.That(decoded.Rows[0].State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(decoded.Rows[0].ProcessedDay, Is.EqualTo(-1L));
        Assert.That(decoded.Rows[1].State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(decoded.Rows[1].ProcessedDay, Is.EqualTo(4L));
        Assert.That(decoded.Rows[1].ResultReason, Is.EqualTo("preserved reason"));

        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(decoded,
            out P12AOwnerSection reencoded, out result), Is.True, result?.Code.ToString());
        Assert.That(reencoded.Revision, Is.EqualTo(encoded.Revision));
        Assert.That(reencoded.Rows, Has.Count.EqualTo(encoded.Rows.Count));
        for (int i = 0; i < encoded.Rows.Count; i++)
        {
            Assert.That(reencoded.Rows[i].OwnerKey, Is.EqualTo(encoded.Rows[i].OwnerKey));
            Assert.That(reencoded.Rows[i].Revision, Is.EqualTo(encoded.Rows[i].Revision));
            CollectionAssert.AreEqual(encoded.Rows[i].Payload, reencoded.Rows[i].Payload);
        }
    }

    [Test]
    public void CoreDirectiveCodecRejectsDuplicateOwnerIds()
    {
        P12FScheduledDirectiveOwnerSnapshot duplicate = new P12FScheduledDirectiveOwnerSnapshot(new[]
        {
            new P12FScheduledDirectiveOwnerSnapshot.Row(
                "duplicate", 2L, ScheduledDirectiveMode.RequestAction,
                ScheduledDirectiveOperation.EscapePrison, "npc-a", "action.escape",
                ScheduledDirectiveState.Pending, -1L, null),
            new P12FScheduledDirectiveOwnerSnapshot.Row(
                "duplicate", 3L, ScheduledDirectiveMode.RequestAction,
                ScheduledDirectiveOperation.EscapePrison, "npc-b", "action.escape",
                ScheduledDirectiveState.Pending, -1L, null)
        }, 2L);

        Assert.That(P12AFScheduledDirectiveCodec.TryEncode(duplicate, out _, out P12AResult failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    private static P12FScheduledDirectiveOwnerSnapshot ImageSnapshot(string actionId) =>
        new P12FScheduledDirectiveOwnerSnapshot(new[]
        {
            new P12FScheduledDirectiveOwnerSnapshot.Row(
                "directive-image", 4L, ScheduledDirectiveMode.RequestAction,
                ScheduledDirectiveOperation.EscapePrison, "npc-missing",
                actionId, ScheduledDirectiveState.Pending, -1L, null)
        }, 1L);

    private static DailyV1ActionDefinition FindAction(
        DailyV1EffectiveDefinitions definitions, int actionType)
    {
        foreach (DailyV1ActionDefinition definition in definitions.Actions.Values)
            if (definition?.Values?.actionType?.Value == actionType) return definition;
        Assert.Fail("The selected Daily-v1 graph is missing action type " + actionType + ".");
        return null;
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CurrentVector(ScheduledDirectiveStore owner)
    {
        OwnerSectionCensusWitness witness = new ScheduledDirectiveCensusProvider(owner).GetCurrentCensus();
        return new[]
        {
            new OwnerSectionCensusSnapshot(witness.SectionId, witness.SchemaVersion,
                OwnerSectionRole.Required, witness.OwnerInstanceIdentity,
                witness.Cardinality, witness.Revision)
        };
    }

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections) =>
        new DailyCaptureEligibilityToken(new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, sections);
}
