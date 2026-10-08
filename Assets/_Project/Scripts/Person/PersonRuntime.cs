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
    private long lifeResidenceRevision;
    private Func<PersonRuntime, bool> p12LifecycleMutationAdmission;
    private Action<PersonRuntime> p12LifecycleMutationCommitted;

    public PersonId PersonId { get; }
    public long? BirthAbsoluteDay => birthAbsoluteDay;
    public bool HasKnownBirthDay => birthAbsoluteDay.HasValue;
    public long? DeathAbsoluteDay => deathAbsoluteDay;
    public string MaterializedNpcRuntimeId => materializedNpcRuntimeId;
    public bool IsMaterialized => string.IsNullOrWhiteSpace(materializedNpcRuntimeId) == false;
    public string ResidenceSettlementRuntimeId => residenceSettlementRuntimeId;
    internal long LifeResidenceRevision => lifeResidenceRevision;

    internal void BindP12LifecycleMutationBoundary(
        Func<PersonRuntime, bool> admission,
        Action<PersonRuntime> committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12LifecycleMutationAdmission != null || p12LifecycleMutationCommitted != null)
            throw new InvalidOperationException("PersonRuntime is already bound to a P12 lifecycle boundary.");
        p12LifecycleMutationAdmission = admission;
        p12LifecycleMutationCommitted = committed;
    }

    internal bool IsP12LifecycleBound => p12LifecycleMutationAdmission != null;

    internal bool CanAdvanceP12LifeResidenceRevision(int increments = 1)
    {
        return increments >= 0
            && (p12LifecycleMutationAdmission == null
                || lifeResidenceRevision <= long.MaxValue - increments);
    }

    internal bool CanCommitP12LifeResidenceMutation()
    {
        if (p12LifecycleMutationAdmission == null)
            return true;
        if (lifeResidenceRevision == long.MaxValue)
            return false;
        try { return p12LifecycleMutationAdmission(this); }
        catch { return false; }
    }

    private void NotifyP12LifeResidenceMutationCommitted()
    {
        if (lifeResidenceRevision < long.MaxValue)
            lifeResidenceRevision++;
        if (p12LifecycleMutationCommitted == null)
            return;
        try { p12LifecycleMutationCommitted(this); }
        catch { }
    }

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

    /// <summary>
    /// Creates an unpublished exact-value owner row for staged continuation.
    /// The caller validates local data first; runtime admission and mutation
    /// callbacks are bound later by the enclosing unpublished graph composer.
    /// </summary>
    internal static PersonRuntime CreateForOwnerSnapshot(
        PersonId personId,
        long? birthAbsoluteDay,
        long? deathAbsoluteDay,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId,
        long lifeResidenceRevision)
    {
        PersonRuntime person = new PersonRuntime(
            personId,
            birthAbsoluteDay,
            deathAbsoluteDay);
        person.residenceSettlementRuntimeId = residenceSettlementRuntimeId;
        person.materializedNpcRuntimeId = materializedNpcRuntimeId;
        person.lifeResidenceRevision = lifeResidenceRevision;
        return person;
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

        if (!CanCommitP12LifeResidenceMutation())
            return false;

        deathAbsoluteDay = absoluteDay;
        NotifyP12LifeResidenceMutationCommitted();
        return true;
    }

    internal bool RecordDeathAfterValidation(long absoluteDay)
    {
        if (absoluteDay < 0L
            || deathAbsoluteDay.HasValue
            || (birthAbsoluteDay.HasValue && absoluteDay < birthAbsoluteDay.Value)
            || !CanCommitP12LifeResidenceMutation())
            return false;
        deathAbsoluteDay = absoluteDay;
        NotifyP12LifeResidenceMutationCommitted();
        return true;
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
        string normalized = string.IsNullOrWhiteSpace(settlementRuntimeId)
            ? null
            : settlementRuntimeId;
        if (string.Equals(residenceSettlementRuntimeId, normalized, StringComparison.Ordinal))
            return true;
        if (!CanCommitP12LifeResidenceMutation())
            return false;
        residenceSettlementRuntimeId = normalized;
        NotifyP12LifeResidenceMutationCommitted();
        return true;
    }
}
