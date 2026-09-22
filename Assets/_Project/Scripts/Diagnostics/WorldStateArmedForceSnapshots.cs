using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class WorldStateArmedForceSnapshot
{
    public string ArmedForceId { get; }
    public string DisplayName { get; }
    public long CreatedAbsoluteDay { get; }
    public ArmedForceLifecycleState LifecycleState { get; }
    public long? TerminatedAbsoluteDay { get; }
    public string ParentForceId { get; }
    public bool IsDetached { get; }
    /// <summary>Legacy opaque compatibility projection, never a spatial authority.</summary>
    public string OperationalLocationReference { get; }
    public string CommanderPersonId { get; }

    public WorldStateArmedForceSnapshot(
        string armedForceId,
        string displayName,
        long createdAbsoluteDay,
        ArmedForceLifecycleState lifecycleState,
        long? terminatedAbsoluteDay,
        string parentForceId,
        bool isDetached,
        string operationalLocationReference,
        string commanderPersonId)
    {
        ArmedForceId = armedForceId;
        DisplayName = displayName;
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        TerminatedAbsoluteDay = terminatedAbsoluteDay;
        ParentForceId = parentForceId;
        IsDetached = isDetached;
        OperationalLocationReference = operationalLocationReference;
        CommanderPersonId = commanderPersonId;
    }
}

public sealed class WorldStateArmedForceCharacteristicSnapshot
{
    public string Key { get; }
    public string Value { get; }

    public WorldStateArmedForceCharacteristicSnapshot(string key, string value)
    {
        Key = key;
        Value = value;
    }
}

public sealed class WorldStateArmedForceContingentSnapshot
{
    public string ContingentId { get; }
    public string ForceId { get; }
    public long Amount { get; }
    public string OriginDomain { get; }
    public string OriginValue { get; }
    public string ServiceType { get; }
    public IReadOnlyList<WorldStateArmedForceCharacteristicSnapshot> Characteristics { get; }

    public WorldStateArmedForceContingentSnapshot(
        string contingentId,
        string forceId,
        long amount,
        string originDomain,
        string originValue,
        string serviceType,
        IEnumerable<WorldStateArmedForceCharacteristicSnapshot> characteristics = null)
    {
        ContingentId = contingentId;
        ForceId = forceId;
        Amount = amount;
        OriginDomain = originDomain;
        OriginValue = originValue;
        ServiceType = serviceType;
        List<WorldStateArmedForceCharacteristicSnapshot> values = characteristics == null
            ? new List<WorldStateArmedForceCharacteristicSnapshot>()
            : new List<WorldStateArmedForceCharacteristicSnapshot>(characteristics);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Key, right?.Key));
        Characteristics = new ReadOnlyCollection<WorldStateArmedForceCharacteristicSnapshot>(values);
    }
}

public sealed class WorldStateArmedForcePersonReferenceSnapshot
{
    public string ReferenceId { get; }
    public string ForceId { get; }
    public string PersonId { get; }
    public string RoleKey { get; }

    public WorldStateArmedForcePersonReferenceSnapshot(
        string referenceId,
        string forceId,
        string personId,
        string roleKey)
    {
        ReferenceId = referenceId;
        ForceId = forceId;
        PersonId = personId;
        RoleKey = roleKey;
    }
}

public sealed class WorldStateArmedForcePositionSnapshot
{
    public string ArmedForceId { get; }
    public SpatialReference CurrentPosition { get; }
    public string CurrentPositionStableKey => CurrentPosition?.StableKey;

    public WorldStateArmedForcePositionSnapshot(
        string armedForceId,
        SpatialReference currentPosition)
    {
        ArmedForceId = armedForceId;
        CurrentPosition = currentPosition;
    }
}

public sealed class WorldStateManpowerCohortSnapshot
{
    public ManpowerInjuryState InjuryState { get; }
    public ManpowerCustodyState CustodyState { get; }
    public string CustodianForceId { get; }
    public ManpowerAvailabilityState AvailabilityState { get; }
    public long Amount { get; }

    public WorldStateManpowerCohortSnapshot(ContingentManpowerCohort cohort)
    {
        InjuryState = cohort.InjuryState;
        CustodyState = cohort.CustodyState;
        CustodianForceId = cohort.CustodianForceId?.Value;
        AvailabilityState = cohort.AvailabilityState;
        Amount = cohort.Amount;
    }
}

public sealed class WorldStateContingentManpowerSnapshot
{
    public string ContingentId { get; }
    public string SourceId { get; }
    public long Revision { get; }
    public long LivingRosterAmount { get; }
    public long AvailableAmount { get; }
    public string Fingerprint { get; }
    public bool SourceResolved { get; }
    public long? SourceCapacity { get; }
    public long? SourceFactualLivingAmount { get; }
    public string SourceFingerprint { get; }
    public IReadOnlyList<WorldStateManpowerCohortSnapshot> Cohorts { get; }

    public WorldStateContingentManpowerSnapshot(
        ContingentManpowerState state,
        ManpowerSourceCapacitySnapshot source,
        bool sourceResolved)
    {
        ContingentId = state.ContingentId.Value;
        SourceId = state.SourceId?.Value;
        Revision = state.Revision;
        LivingRosterAmount = state.LivingRosterAmount;
        AvailableAmount = state.AvailableAmount;
        Fingerprint = state.Fingerprint;
        SourceResolved = state.SourceId == null || sourceResolved;
        SourceCapacity = source?.Capacity;
        SourceFactualLivingAmount = source?.FactualLivingAmount;
        SourceFingerprint = source?.Fingerprint;
        List<WorldStateManpowerCohortSnapshot> values = new List<WorldStateManpowerCohortSnapshot>();
        foreach (ContingentManpowerCohort cohort in state.Cohorts)
            values.Add(new WorldStateManpowerCohortSnapshot(cohort));
        values.Sort((a, b) =>
        {
            int c = a.InjuryState.CompareTo(b.InjuryState);
            if (c != 0) return c;
            c = a.CustodyState.CompareTo(b.CustodyState);
            if (c != 0) return c;
            c = StringComparer.Ordinal.Compare(a.CustodianForceId, b.CustodianForceId);
            return c != 0 ? c : a.AvailabilityState.CompareTo(b.AvailabilityState);
        });
        Cohorts = new ReadOnlyCollection<WorldStateManpowerCohortSnapshot>(values);
    }
}
