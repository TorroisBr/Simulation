using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Immutable owner-local value for one PersonStore row. It deliberately stores
/// the exact ID string and no live PersonRuntime or NPC reference.
/// </summary>
internal sealed class PersonStoreOwnerSnapshotRow
{
    internal string PersonIdValue { get; }
    internal long? BirthAbsoluteDay { get; }
    internal long? DeathAbsoluteDay { get; }
    internal string ResidenceSettlementRuntimeId { get; }
    internal string MaterializedNpcRuntimeId { get; }
    internal long LifeResidenceRevision { get; }

    internal PersonStoreOwnerSnapshotRow(
        string personIdValue,
        long? birthAbsoluteDay,
        long? deathAbsoluteDay,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId,
        long lifeResidenceRevision)
    {
        PersonIdValue = personIdValue;
        BirthAbsoluteDay = birthAbsoluteDay;
        DeathAbsoluteDay = deathAbsoluteDay;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        MaterializedNpcRuntimeId = materializedNpcRuntimeId;
        LifeResidenceRevision = lifeResidenceRevision;
    }
}

/// <summary>
/// Detached schema-versioned values owned by PersonStore and its registered
/// PersonRuntime rows. This value carries no capture-token authority.
/// </summary>
internal sealed class PersonStoreOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal int MembershipCount { get; }
    internal int BindingCount { get; }
    internal IReadOnlyList<PersonStoreOwnerSnapshotRow> Rows { get; }

    internal PersonStoreOwnerSnapshot(
        int schemaVersion,
        long revision,
        int membershipCount,
        int bindingCount,
        IEnumerable<PersonStoreOwnerSnapshotRow> rows)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        MembershipCount = membershipCount;
        BindingCount = bindingCount;
        if (rows == null)
        {
            Rows = null;
            return;
        }

        List<PersonStoreOwnerSnapshotRow> copy = new List<PersonStoreOwnerSnapshotRow>();
        foreach (PersonStoreOwnerSnapshotRow row in rows)
        {
            copy.Add(row == null
                ? null
                : new PersonStoreOwnerSnapshotRow(
                    row.PersonIdValue,
                    row.BirthAbsoluteDay,
                    row.DeathAbsoluteDay,
                    row.ResidenceSettlementRuntimeId,
                    row.MaterializedNpcRuntimeId,
                    row.LifeResidenceRevision));
        }

        Rows = new ReadOnlyCollection<PersonStoreOwnerSnapshotRow>(copy);
    }
}

internal enum PersonStoreOwnerSnapshotFailureCode
{
    None = 0,
    MissingSnapshot,
    UnsupportedSchema,
    InvalidHeader,
    InvalidCardinality,
    ImpossibleRevision,
    NullPersonRow,
    InvalidPersonId,
    DuplicatePersonId,
    InvalidDates,
    InvalidResidence,
    InvalidMaterializedNpcId,
    DuplicateMaterializedNpcId,
    InvalidPersonRevision
}

public enum PersonStoreFailure
{
    None = 0,
    InvalidPerson = 1,
    DuplicatePersonId = 2,
    PersonNotRegistered = 3,
    AlreadyMaterialized = 4,
    InvalidNpcRuntimeId = 5,
    NpcAlreadyBoundToAnotherPerson = 6,
    BirthAbsoluteDayInFuture = 7,
    DeathAbsoluteDayInFuture = 8,
    RuntimeFaulted = 9,
    RevisionOverflow = 10
}

/// <summary>
/// World-owned registry of lightweight Persons and their optional materialization binding.
/// </summary>
public sealed class PersonStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private FactualReadAdmission factualReadAdmission;
    private readonly Dictionary<PersonId, PersonRuntime> personsById =
        new Dictionary<PersonId, PersonRuntime>();
    private readonly Dictionary<string, PersonRuntime> personsByNpcRuntimeId =
        new Dictionary<string, PersonRuntime>(StringComparer.Ordinal);
    private readonly List<PersonRuntime> persons = new List<PersonRuntime>();
    private readonly IReadOnlyList<PersonRuntime> personSnapshot;
    private long revision;

    public IReadOnlyList<PersonRuntime> Persons => personSnapshot;
    public int MaterializedBindingCount => personsByNpcRuntimeId.Count;
    public long Revision => revision;

    public PersonStore()
    {
        personSnapshot = persons.AsReadOnly();
    }

    /// <summary>
    /// Copies this owner's exact values and insertion order. The caller must
    /// hold the P12-B completed-boundary capture authority; this local method
    /// neither creates nor validates that token or its owner-section vector.
    /// </summary>
    internal PersonStoreOwnerSnapshot CaptureOwnerSnapshot()
    {
        List<PersonStoreOwnerSnapshotRow> rows = new List<PersonStoreOwnerSnapshotRow>(persons.Count);
        foreach (PersonRuntime person in persons)
        {
            rows.Add(person == null
                ? null
                : new PersonStoreOwnerSnapshotRow(
                    person.PersonId != null ? person.PersonId.Value : null,
                    person.BirthAbsoluteDay,
                    person.DeathAbsoluteDay,
                    person.ResidenceSettlementRuntimeId,
                    person.MaterializedNpcRuntimeId,
                    person.LifeResidenceRevision));
        }

        return new PersonStoreOwnerSnapshot(
            PersonStoreOwnerSnapshot.CurrentSchemaVersion,
            revision,
            persons.Count,
            MaterializedBindingCount,
            rows);
    }

    /// <summary>
    /// Builds a complete private owner from locally validated exact values.
    /// No gameplay mutation is replayed and cross-owner Person/NPC/City/day
    /// checks remain with the later merged D graph validator.
    /// </summary>
    internal static bool TryCreateFromOwnerSnapshot(
        PersonStoreOwnerSnapshot snapshot,
        out PersonStore stagedStore,
        out PersonStoreOwnerSnapshotFailureCode failure)
    {
        stagedStore = null;
        if (snapshot == null)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.MissingSnapshot;
            return false;
        }

        if (snapshot.SchemaVersion != PersonStoreOwnerSnapshot.CurrentSchemaVersion)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.UnsupportedSchema;
            return false;
        }

        if (snapshot.Revision < 0L || snapshot.Rows == null)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.InvalidHeader;
            return false;
        }

        if (snapshot.MembershipCount != snapshot.Rows.Count
            || snapshot.BindingCount < 0
            || snapshot.BindingCount > snapshot.MembershipCount)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.InvalidCardinality;
            return false;
        }

        long minimumRevision = (long)snapshot.MembershipCount
            + (long)snapshot.BindingCount
            - 1L;
        if (snapshot.Revision < minimumRevision)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.ImpossibleRevision;
            return false;
        }

        PersonStore staged = new PersonStore();
        foreach (PersonStoreOwnerSnapshotRow row in snapshot.Rows)
        {
            if (row == null)
            {
                failure = PersonStoreOwnerSnapshotFailureCode.NullPersonRow;
                return false;
            }

            if (string.IsNullOrWhiteSpace(row.PersonIdValue))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.InvalidPersonId;
                return false;
            }

            if (row.LifeResidenceRevision < 0L)
            {
                failure = PersonStoreOwnerSnapshotFailureCode.InvalidPersonRevision;
                return false;
            }

            if ((row.BirthAbsoluteDay.HasValue && row.BirthAbsoluteDay.Value < 0L)
                || (row.DeathAbsoluteDay.HasValue && row.DeathAbsoluteDay.Value < 0L)
                || (row.BirthAbsoluteDay.HasValue
                    && row.DeathAbsoluteDay.HasValue
                    && row.DeathAbsoluteDay.Value < row.BirthAbsoluteDay.Value))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.InvalidDates;
                return false;
            }

            if (row.ResidenceSettlementRuntimeId != null
                && string.IsNullOrWhiteSpace(row.ResidenceSettlementRuntimeId))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.InvalidResidence;
                return false;
            }

            if (row.MaterializedNpcRuntimeId != null
                && string.IsNullOrWhiteSpace(row.MaterializedNpcRuntimeId))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.InvalidMaterializedNpcId;
                return false;
            }

            PersonId personId = new PersonId(row.PersonIdValue);
            if (staged.personsById.ContainsKey(personId))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.DuplicatePersonId;
                return false;
            }

            if (row.MaterializedNpcRuntimeId != null
                && staged.personsByNpcRuntimeId.ContainsKey(row.MaterializedNpcRuntimeId))
            {
                failure = PersonStoreOwnerSnapshotFailureCode.DuplicateMaterializedNpcId;
                return false;
            }

            PersonRuntime person = PersonRuntime.CreateForOwnerSnapshot(
                personId,
                row.BirthAbsoluteDay,
                row.DeathAbsoluteDay,
                row.ResidenceSettlementRuntimeId,
                row.MaterializedNpcRuntimeId,
                row.LifeResidenceRevision);
            staged.persons.Add(person);
            staged.personsById.Add(personId, person);
            if (row.MaterializedNpcRuntimeId != null)
            {
                staged.personsByNpcRuntimeId.Add(row.MaterializedNpcRuntimeId, person);
            }
        }

        if (staged.MaterializedBindingCount != snapshot.BindingCount)
        {
            failure = PersonStoreOwnerSnapshotFailureCode.InvalidCardinality;
            return false;
        }

        // Restore after the derived indexes are rebuilt so no owner write is
        // replayed and saturated/gapped revisions remain exact.
        staged.revision = snapshot.Revision;
        stagedStore = staged;
        failure = PersonStoreOwnerSnapshotFailureCode.None;
        return true;
    }

    public bool TryRegister(PersonRuntime person, out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            failure = PersonStoreFailure.RuntimeFaulted;
            return false;
        }

        if (person == null || person.PersonId == null)
        {
            failure = PersonStoreFailure.InvalidPerson;
            return false;
        }

        if (personsById.ContainsKey(person.PersonId) == true)
        {
            failure = PersonStoreFailure.DuplicatePersonId;
            return false;
        }

        if (CanAdvanceRevisionForForwardWrite() == false)
        {
            failure = PersonStoreFailure.RevisionOverflow;
            return false;
        }

        personsById.Add(person.PersonId, person);
        persons.Add(person);
        revision++;
        return true;
    }

    public bool TryGet(PersonId personId, out PersonRuntime person)
    {
        person = null;
        return personId != null && personsById.TryGetValue(personId, out person);
    }

    public bool TryGetByMaterializedNpcRuntimeId(
        string npcRuntimeId,
        out PersonRuntime person)
    {
        person = null;
        return string.IsNullOrWhiteSpace(npcRuntimeId) == false
            && personsByNpcRuntimeId.TryGetValue(npcRuntimeId, out person);
    }

    /// <summary>
    /// Removes exactly the registration created for an operation that has not
    /// yet become externally observable. This is intentionally internal: the
    /// world must not expose arbitrary Person deletion as a public mutation.
    /// </summary>
    internal bool TryRollbackRegistration(PersonRuntime person)
    {
        if (!CanMutateThroughFactualReadAdmission()
            || person == null
            || person.IsMaterialized == true
            || personsById.TryGetValue(person.PersonId, out PersonRuntime registeredPerson) == false
            || ReferenceEquals(registeredPerson, person) == false)
        {
            return false;
        }

        if (CanAdvanceRevision() == false)
        {
            return false;
        }

        personsById.Remove(person.PersonId);
        persons.Remove(person);
        revision++;
        return true;
    }

    public int CountResidents(string settlementRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            return 0;
        }

        int count = 0;
        foreach (PersonRuntime person in persons)
        {
            if (person != null
                && string.Equals(
                    person.ResidenceSettlementRuntimeId,
                    settlementRuntimeId,
                    StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    public IReadOnlyList<PersonRuntime> GetResidents(string settlementRuntimeId)
    {
        List<PersonRuntime> residents = new List<PersonRuntime>();
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            return residents.AsReadOnly();
        }

        foreach (PersonRuntime person in persons)
        {
            if (person != null
                && string.Equals(
                    person.ResidenceSettlementRuntimeId,
                    settlementRuntimeId,
                    StringComparison.Ordinal))
            {
                residents.Add(person);
            }
        }

        return residents.AsReadOnly();
    }

    internal bool TryBindMaterializedNpc(
        PersonId personId,
        string npcRuntimeId,
        out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            failure = PersonStoreFailure.RuntimeFaulted;
            return false;
        }

        if (personId == null)
        {
            failure = PersonStoreFailure.InvalidPerson;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            failure = PersonStoreFailure.InvalidNpcRuntimeId;
            return false;
        }

        if (personsById.TryGetValue(personId, out PersonRuntime person) == false)
        {
            failure = PersonStoreFailure.PersonNotRegistered;
            return false;
        }

        if (person.IsMaterialized == true)
        {
            failure = PersonStoreFailure.AlreadyMaterialized;
            return false;
        }

        if (personsByNpcRuntimeId.ContainsKey(npcRuntimeId) == true)
        {
            failure = PersonStoreFailure.NpcAlreadyBoundToAnotherPerson;
            return false;
        }

        if (CanAdvanceRevisionForForwardWrite() == false)
        {
            failure = PersonStoreFailure.RevisionOverflow;
            return false;
        }

        if (person.TryBindMaterializedNpc(npcRuntimeId) == false)
        {
            failure = PersonStoreFailure.AlreadyMaterialized;
            return false;
        }

        personsByNpcRuntimeId.Add(npcRuntimeId, person);
        revision++;
        return true;
    }

    internal bool TryRollbackMaterializedNpcBinding(PersonId personId, string npcRuntimeId)
    {
        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            return false;
        }

        if (personId == null || string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        if (personsById.TryGetValue(personId, out PersonRuntime person) == false
            || personsByNpcRuntimeId.TryGetValue(npcRuntimeId, out PersonRuntime indexedPerson) == false
            || ReferenceEquals(indexedPerson, person) == false
            || CanAdvanceRevision() == false
            || person.TryUnbindMaterializedNpc(npcRuntimeId) == false)
        {
            return false;
        }

        personsByNpcRuntimeId.Remove(npcRuntimeId);
        revision++;
        return true;
    }

    private bool CanAdvanceRevisionForForwardWrite()
    {
        return revision < long.MaxValue - 1L;
    }

    private bool CanAdvanceRevision()
    {
        return revision < long.MaxValue;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    internal bool CanBindFactualReadAdmission(FactualReadAdmission admission)
    {
        return admission != null
            && (factualReadAdmission == null || ReferenceEquals(factualReadAdmission, admission));
    }

    internal bool TryBindFactualReadAdmission(FactualReadAdmission admission)
    {
        if (!CanBindFactualReadAdmission(admission))
            return false;
        if (factualReadAdmission == null)
            factualReadAdmission = admission;
        return true;
    }

    internal void TryUnbindFactualReadAdmission(FactualReadAdmission admission)
    {
        if (ReferenceEquals(factualReadAdmission, admission))
            factualReadAdmission = null;
    }

    private bool CanMutateThroughFactualReadAdmission()
    {
        return factualReadAdmission == null
            || factualReadAdmission.CanMutatePersonStore(this);
    }
}
