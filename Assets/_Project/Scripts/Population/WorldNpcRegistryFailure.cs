/// <summary>
/// Rejection reasons for explicit world-owned NPC registration changes.
/// </summary>
public enum WorldNpcRegistryFailure
{
    None = 0,
    InvalidNpc = 1,
    InvalidRuntimeId = 2,
    DuplicateRuntimeId = 3,
    NpcNotRegistered = 4,
    NpcHasResidence = 5,
    BirthDayInFuture = 6
}
