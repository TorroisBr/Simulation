using NUnit.Framework;

public sealed class ActionExecutionTests
{
    [Test]
    public void SharedActionContextPreservesRoleValidationAndStableTargetIdentity()
    {
        ActionExecutionContext context = new ActionExecutionContext("travel/joint-civil",
            new[]
            {
                new ActionExecutionParticipant("npc:performer", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc:support", ActionExecutionParticipantRole.Support),
                new ActionExecutionParticipant("npc:target", ActionExecutionParticipantRole.Target)
            }, "location:target", "route:target", "decision:origin");
        ActionParticipationRequirements requirements = new ActionParticipationRequirements(
            minPerformers: 1, maxPerformers: 1, minSupports: 1, maxSupports: 1,
            minTargets: 1, maxTargets: 1);

        Assert.That(ActionExecutionValidator.TryValidate(context, requirements, out string reason), Is.True, reason);
        Assert.That(context.TargetLocationRuntimeId, Is.EqualTo("location:target"));
        Assert.That(context.TargetRouteRuntimeId, Is.EqualTo("route:target"));
        Assert.That(context.OriginDecisionId, Is.EqualTo("decision:origin"));

        ActionExecutionContext duplicateRole = new ActionExecutionContext("travel/joint-civil",
            new[]
            {
                new ActionExecutionParticipant("npc:one", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc:one", ActionExecutionParticipantRole.Performer)
            });
        Assert.That(ActionExecutionValidator.TryValidate(duplicateRole,
            new ActionParticipationRequirements(minPerformers: 1), out _), Is.False);
    }
}
