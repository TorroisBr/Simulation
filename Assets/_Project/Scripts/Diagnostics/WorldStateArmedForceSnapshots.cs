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
