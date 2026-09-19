using System;

/// <summary>
/// Lightweight individual identity. Rich behavior remains on a materialized NpcRuntime.
/// </summary>
public sealed class PersonRuntime
{
    private string materializedNpcRuntimeId;
    private string residenceSettlementRuntimeId;
    private readonly long? birthAbsoluteDay;
    private long? deathAbsoluteDay;

    public PersonId PersonId { get; }
    public long? BirthAbsoluteDay => birthAbsoluteDay;
    public bool HasKnownBirthDay => birthAbsoluteDay.HasValue;
    public long? DeathAbsoluteDay => deathAbsoluteDay;
    public string MaterializedNpcRuntimeId => materializedNpcRuntimeId;
    public bool IsMaterialized => string.IsNullOrWhiteSpace(materializedNpcRuntimeId) == false;
    public string ResidenceSettlementRuntimeId => residenceSettlementRuntimeId;

    public PersonRuntime(PersonId personId)
        : this(personId, null, null)
    {
    }

    public PersonRuntime(PersonId personId, long? birthAbsoluteDay)
        : this(personId, birthAbsoluteDay, null)
    {
    }

    public PersonRuntime(
        PersonId personId,
        long? birthAbsoluteDay,
        long? deathAbsoluteDay)
    {
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));

        if (birthAbsoluteDay.HasValue && birthAbsoluteDay.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(birthAbsoluteDay), "BirthAbsoluteDay cannot be negative.");
        }

        if (deathAbsoluteDay.HasValue && deathAbsoluteDay.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(deathAbsoluteDay), "DeathAbsoluteDay cannot be negative.");
        }

        if (birthAbsoluteDay.HasValue
            && deathAbsoluteDay.HasValue
            && deathAbsoluteDay.Value < birthAbsoluteDay.Value)
        {
            throw new ArgumentException(
                "DeathAbsoluteDay cannot be earlier than BirthAbsoluteDay.",
                nameof(deathAbsoluteDay));
        }

        this.birthAbsoluteDay = birthAbsoluteDay;
        this.deathAbsoluteDay = deathAbsoluteDay;
    }

    public bool IsDeadAt(long absoluteDay)
    {
        return absoluteDay >= 0L
            && deathAbsoluteDay.HasValue
            && deathAbsoluteDay.Value <= absoluteDay;
    }

    internal bool TryRecordDeath(long absoluteDay)
    {
        if (absoluteDay < 0L
            || deathAbsoluteDay.HasValue
            || (birthAbsoluteDay.HasValue && absoluteDay < birthAbsoluteDay.Value))
        {
            return false;
        }

        deathAbsoluteDay = absoluteDay;
        return true;
    }

    internal void RecordDeathAfterValidation(long absoluteDay)
    {
        deathAbsoluteDay = absoluteDay;
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
