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
    NpcPersonBindingInvalid = 6,
    NpcBoundToPerson = 7,
    RuntimeFaulted = 8,
    AlreadyOwnedByAnotherRuntime = 9
}
