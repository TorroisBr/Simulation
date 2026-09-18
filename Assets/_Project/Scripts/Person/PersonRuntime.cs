using System;

/// <summary>
/// Lightweight individual identity. Rich behavior remains on a materialized NpcRuntime.
/// </summary>
public sealed class PersonRuntime
{
    private string materializedNpcRuntimeId;

    public PersonId PersonId { get; }
    public string MaterializedNpcRuntimeId => materializedNpcRuntimeId;
    public bool IsMaterialized => string.IsNullOrWhiteSpace(materializedNpcRuntimeId) == false;

    public PersonRuntime(PersonId personId)
    {
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
    }

    internal bool TryBindMaterializedNpc(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true || IsMaterialized == true)
        {
            return false;
        }

        materializedNpcRuntimeId = npcRuntimeId;
        return true;
    }

    internal bool TryUnbindMaterializedNpc(string npcRuntimeId)
    {
        if (string.Equals(materializedNpcRuntimeId, npcRuntimeId, StringComparison.Ordinal) == false)
        {
            return false;
        }

        materializedNpcRuntimeId = null;
        return true;
    }
}
