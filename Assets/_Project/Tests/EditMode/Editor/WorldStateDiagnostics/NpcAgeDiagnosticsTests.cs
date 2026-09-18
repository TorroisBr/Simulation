using System;
using NUnit.Framework;

public sealed class NpcAgeDiagnosticsTests
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
    public void SnapshotRepresentsKnownAndUnknownBirthWithoutInventingAge()
    {
        NpcRuntime known = CreateNpc("diagnostics-known", 5L);
        NpcRuntime unknown = CreateNpc("diagnostics-unknown");
        WorldStateSnapshot snapshot = Capture(new SimulationTime(17L), new[] { known, unknown }, UniformCalendar());

        WorldStateNpcSnapshot knownSnapshot = FindNpc(snapshot, known.RuntimeId);
        WorldStateNpcSnapshot unknownSnapshot = FindNpc(snapshot, unknown.RuntimeId);
        Assert.That(knownSnapshot.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(knownSnapshot.AgeInDays, Is.EqualTo(12L));
        Assert.That(knownSnapshot.CompletedYears, Is.EqualTo(1L));
        Assert.That(unknownSnapshot.BirthAbsoluteDay, Is.Null);
        Assert.That(unknownSnapshot.AgeInDays, Is.Null);
        Assert.That(unknownSnapshot.CompletedYears, Is.Null);

        string formatted = WorldStateDiagnostics.Format(snapshot);
        Assert.That(formatted, Does.Contain("Birth absolute day: 5"));
        Assert.That(formatted, Does.Contain("Completed years: 1"));
        Assert.That(formatted, Does.Contain("Birth absolute day: ~"));
        Assert.That(WorldStateDiagnostics.Export(snapshot), Is.EqualTo(WorldStateDiagnostics.Export(snapshot)));
        Assert.That(WorldStateDiagnostics.Validate(snapshot).HasErrors, Is.False);
    }

    [Test]
    public void BirthdayProducesDeterministicCompletedYearsDiff()
    {
        NpcRuntime npc = CreateNpc("diagnostics-birthday", 5L);
        WorldStateSnapshot before = Capture(new SimulationTime(16L), new[] { npc }, UniformCalendar());
        WorldStateSnapshot after = Capture(new SimulationTime(17L), new[] { npc }, UniformCalendar());

        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);

        Assert.That(Contains(diff, "CompletedYears"), Is.True);
        Assert.That(Contains(diff, "BirthAbsoluteDay"), Is.False);
        Assert.That(WorldStateDiagnostics.Export(before), Does.Contain("NPC|diagnostics-birthday"));
    }

    [Test]
    public void FutureBirthIsReportedBySnapshotInvariantValidator()
    {
        NpcRuntime npc = CreateNpc("diagnostics-future", 11L);
        WorldStateSnapshot snapshot = Capture(new SimulationTime(10L), new[] { npc }, UniformCalendar());

        WorldStateInvariantReport report = WorldStateDiagnostics.Validate(snapshot);

        Assert.That(report.HasErrors, Is.True);
        Assert.That(ContainsIssue(report, "BirthDayInFuture"), Is.True);
        Assert.That(FindNpc(snapshot, npc.RuntimeId).BirthAbsoluteDay, Is.EqualTo(11L));
        Assert.That(FindNpc(snapshot, npc.RuntimeId).CompletedYears, Is.Null);
    }

    [Test]
    public void DeadNpcWithKnownBirthRemainsValidAndReportsChronologicalAge()
    {
        NpcRuntime npc = CreateNpc("diagnostics-dead", 0L);
        Assert.That(npc.TryApplyDeath(), Is.True);
        WorldStateSnapshot snapshot = Capture(new SimulationTime(12L), new[] { npc }, UniformCalendar());

        WorldStateNpcSnapshot npcSnapshot = FindNpc(snapshot, npc.RuntimeId);
        Assert.That(npcSnapshot.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npcSnapshot.BirthAbsoluteDay, Is.EqualTo(0L));
        Assert.That(npcSnapshot.CompletedYears, Is.EqualTo(1L));
        Assert.That(WorldStateDiagnostics.Validate(snapshot).HasErrors, Is.False);
    }

    private static WorldStateSnapshot Capture(
        SimulationTime time,
        NpcRuntime[] npcs,
        CalendarDefinition calendar)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            time,
            npcs,
            calendarDefinition: calendar));
    }

    private static NpcRuntime CreateNpc(string runtimeId, long? birthAbsoluteDay = null)
    {
        return new NpcRuntime(
            runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId),
            null,
            0f,
            birthAbsoluteDay);
    }

    private static CalendarDefinition UniformCalendar()
    {
        return new CalendarDefinition(12, 1, 1);
    }

    private static WorldStateNpcSnapshot FindNpc(WorldStateSnapshot snapshot, string runtimeId)
    {
        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            if (string.Equals(npc.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                return npc;
            }
        }

        Assert.Fail("NPC snapshot not found: " + runtimeId);
        return null;
    }

    private static bool Contains(WorldStateDiff diff, string field)
    {
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == "NPC" && difference.Field == field)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIssue(WorldStateInvariantReport report, string code)
    {
        foreach (WorldStateInvariantIssue issue in report.Issues)
        {
            if (issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
