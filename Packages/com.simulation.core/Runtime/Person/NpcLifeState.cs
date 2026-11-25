using System;

public enum NpcLifeState
{
    Alive,
    Dead
}

public enum NpcInjurySeverity
{
    None,
    Hurt,
    Injured,
    SeriouslyInjured,
    Incapacitated
}

public static class NpcInjuryRules
{
    public static float GetCapabilityMultiplier(NpcInjurySeverity severity)
    {
        switch (severity)
        {
            case NpcInjurySeverity.Hurt: return 0.85f;
            case NpcInjurySeverity.Injured: return 0.65f;
            case NpcInjurySeverity.SeriouslyInjured: return 0.4f;
            case NpcInjurySeverity.Incapacitated: return 0.1f;
            case NpcInjurySeverity.None: return 1f;
            default:
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown injury severity.");
        }
    }

    public static bool IsValid(NpcInjurySeverity severity) =>
        Enum.IsDefined(typeof(NpcInjurySeverity), severity);

    public static bool IsValid(NpcLifeState lifeState) =>
        Enum.IsDefined(typeof(NpcLifeState), lifeState);
}
