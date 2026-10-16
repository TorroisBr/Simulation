using System;
using System.Collections.Generic;

[Serializable]
public sealed class ActionExecutionParticipant
{
    private readonly string runtimeId;
    private readonly ActionExecutionParticipantRole role;

    public string RuntimeId => runtimeId;
    public ActionExecutionParticipantRole Role => role;

    public ActionExecutionParticipant(string runtimeId, ActionExecutionParticipantRole role)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Action execution participant requires a RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.role = role;
    }
}

public enum ActionExecutionParticipantRole
{
    Performer,
    Support,
    Target,
    Participant
}

[Serializable]
public sealed class ActionExecutionContext
{
    private readonly string actionDefinitionId;
    private readonly IReadOnlyList<ActionExecutionParticipant> participants;
    private readonly string targetLocationRuntimeId;
    private readonly string targetRouteRuntimeId;
    private readonly string originDecisionId;

    public string ActionDefinitionId => actionDefinitionId;
    public IReadOnlyList<ActionExecutionParticipant> Participants => participants;
    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public string TargetRouteRuntimeId => targetRouteRuntimeId;
    public string OriginDecisionId => originDecisionId;

    public ActionExecutionContext(
        string actionDefinitionId,
        IEnumerable<ActionExecutionParticipant> participants,
        string targetLocationRuntimeId = null,
        string targetRouteRuntimeId = null,
        string originDecisionId = null)
    {
        this.actionDefinitionId = actionDefinitionId;
        this.participants = CaptureParticipants(participants);
        this.targetLocationRuntimeId = NormalizeOptionalId(targetLocationRuntimeId);
        this.targetRouteRuntimeId = NormalizeOptionalId(targetRouteRuntimeId);
        this.originDecisionId = NormalizeOptionalId(originDecisionId);
    }

    private static IReadOnlyList<ActionExecutionParticipant> CaptureParticipants(
        IEnumerable<ActionExecutionParticipant> source)
    {
        List<ActionExecutionParticipant> snapshot = new List<ActionExecutionParticipant>();

        if (source != null)
        {
            foreach (ActionExecutionParticipant participant in source)
            {
                snapshot.Add(participant);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

[Serializable]
public sealed class ActionParticipationRequirements
{
    public const int Unlimited = -1;

    public int MinPerformers { get; }
    public int MaxPerformers { get; }
    public int MinSupports { get; }
    public int MaxSupports { get; }
    public int MinTargets { get; }
    public int MaxTargets { get; }

    public ActionParticipationRequirements(
        int minPerformers = 0,
        int maxPerformers = Unlimited,
        int minSupports = 0,
        int maxSupports = Unlimited,
        int minTargets = 0,
        int maxTargets = Unlimited)
    {
        MinPerformers = minPerformers;
        MaxPerformers = maxPerformers;
        MinSupports = minSupports;
        MaxSupports = maxSupports;
        MinTargets = minTargets;
        MaxTargets = maxTargets;
    }

    public static bool IsUnlimited(int maximum)
    {
        return maximum == Unlimited;
    }
}

public static class ActionExecutionValidator
{
    public static bool Validate(
        ActionExecutionContext context,
        ActionParticipationRequirements requirements,
        out string reason)
    {
        return TryValidate(context, requirements, out reason);
    }

    public static bool TryValidate(
        ActionExecutionContext context,
        ActionParticipationRequirements requirements,
        out string reason)
    {
        reason = string.Empty;

        if (context == null)
        {
            reason = "Execution context is null.";
            return false;
        }

        if (requirements == null)
        {
            reason = "Participation requirements are null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.ActionDefinitionId) == true)
        {
            reason = "ActionDefinitionId is empty.";
            return false;
        }

        if (ValidateBounds(requirements.MinPerformers, requirements.MaxPerformers) == false
            || ValidateBounds(requirements.MinSupports, requirements.MaxSupports) == false
            || ValidateBounds(requirements.MinTargets, requirements.MaxTargets) == false)
        {
            reason = "Participation requirement bounds are invalid.";
            return false;
        }

        int performerCount = 0;
        int supportCount = 0;
        int targetParticipantCount = 0;
        HashSet<string> participantRoleKeys = new HashSet<string>(StringComparer.Ordinal);

        if (context.Participants == null)
        {
            reason = "Execution participants are null.";
            return false;
        }

        foreach (ActionExecutionParticipant participant in context.Participants)
        {
            if (participant == null || string.IsNullOrWhiteSpace(participant.RuntimeId) == true)
            {
                reason = "Execution participant RuntimeId is empty.";
                return false;
            }

            if (Enum.IsDefined(typeof(ActionExecutionParticipantRole), participant.Role) == false)
            {
                reason = "Execution participant role is invalid.";
                return false;
            }

            string participantRoleKey = participant.RuntimeId + "\u001f" + (int)participant.Role;

            if (participantRoleKeys.Add(participantRoleKey) == false)
            {
                reason = "Duplicate execution participant RuntimeId and Role.";
                return false;
            }

            switch (participant.Role)
            {
                case ActionExecutionParticipantRole.Performer:
                    performerCount++;
                    break;
                case ActionExecutionParticipantRole.Support:
                    supportCount++;
                    break;
                case ActionExecutionParticipantRole.Target:
                    targetParticipantCount++;
                    break;
            }
        }

        if (Satisfies(performerCount, requirements.MinPerformers, requirements.MaxPerformers) == false)
        {
            reason = "Performer participation requirement was not satisfied.";
            return false;
        }

        if (Satisfies(supportCount, requirements.MinSupports, requirements.MaxSupports) == false)
        {
            reason = "Support participation requirement was not satisfied.";
            return false;
        }

        if (Satisfies(targetParticipantCount, requirements.MinTargets, requirements.MaxTargets) == false)
        {
            reason = "Target participation requirement was not satisfied.";
            return false;
        }

        return true;
    }

    private static bool ValidateBounds(int minimum, int maximum)
    {
        return minimum >= 0
            && (ActionParticipationRequirements.IsUnlimited(maximum) || maximum >= minimum);
    }

    private static bool Satisfies(int count, int minimum, int maximum)
    {
        return count >= minimum
            && (ActionParticipationRequirements.IsUnlimited(maximum) || count <= maximum);
    }
}
