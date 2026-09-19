using System;

/// <summary>
/// Lightweight individual identity. Rich behavior remains on a materialized NpcRuntime.
/// </summary>
public sealed class PersonRuntime
{
    private string materializedNpcRuntimeId;
    private string residenceSettlementRuntimeId;
    private readonly long? birthAbsoluteDay;

    public PersonId PersonId { get; }
    public long? BirthAbsoluteDay => birthAbsoluteDay;
    public bool HasKnownBirthDay => birthAbsoluteDay.HasValue;
    public string MaterializedNpcRuntimeId => materializedNpcRuntimeId;
    public bool IsMaterialized => string.IsNullOrWhiteSpace(materializedNpcRuntimeId) == false;
    public string ResidenceSettlementRuntimeId => residenceSettlementRuntimeId;

    public PersonRuntime(PersonId personId)
        : this(personId, null)
    {
    }

    public PersonRuntime(PersonId personId, long? birthAbsoluteDay)
    {
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));

        if (birthAbsoluteDay.HasValue && birthAbsoluteDay.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(birthAbsoluteDay), "BirthAbsoluteDay cannot be negative.");
        }

        this.birthAbsoluteDay = birthAbsoluteDay;
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

    /// <summary>
    /// Changes residence only through a world-owned membership boundary. The
    /// identifier is intentionally not publicly mutable.
    /// </summary>
    internal bool TrySetResidenceSettlementRuntimeId(string settlementRuntimeId)
    {
        residenceSettlementRuntimeId = string.IsNullOrWhiteSpace(settlementRuntimeId)
            ? null
            : settlementRuntimeId;
        return true;
    }
}
