using System;

public enum PropertyTransferFailureCode
{
    None = 0,
    InvalidStore = 1,
    InvalidPropertyId = 2,
    PropertyNotFound = 3,
    InvalidNewOwner = 4,
    PersonNotRegistered = 5,
    NewOwnerNotLiving = 6,
    SameOwner = 7,
    InvalidTransferDay = 8,
    InvalidTransition = 9,
    StalePropertyStore = 10,
    StalePropertyOwnership = 11,
    RevisionOverflow = 12
}

public sealed class PropertyTransferFailure : IEquatable<PropertyTransferFailure>
{
    private static readonly PropertyTransferFailure none =
        new PropertyTransferFailure(PropertyTransferFailureCode.None, string.Empty);

    private PropertyTransferFailure(PropertyTransferFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PropertyTransferFailure None => none;
    public PropertyTransferFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PropertyTransferFailureCode.None;

    public static PropertyTransferFailure Create(
        PropertyTransferFailureCode code,
        string message)
    {
        return code == PropertyTransferFailureCode.None
            ? None
            : new PropertyTransferFailure(code, message);
    }

    public bool Equals(PropertyTransferFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyTransferFailure);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
        }
    }

    public override string ToString()
    {
        return Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
    }
}

public sealed class PropertyOwnershipTransferHistoryRecord
{
    public PropertyOwnershipTransferHistoryRecord(
        PropertyId propertyId,
        PersonId previousOwnerPersonId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay)
    {
        PropertyId = propertyId ?? throw new ArgumentNullException(nameof(propertyId));
        PreviousOwnerPersonId = previousOwnerPersonId
            ?? throw new ArgumentNullException(nameof(previousOwnerPersonId));
        NewOwnerPersonId = newOwnerPersonId
            ?? throw new ArgumentNullException(nameof(newOwnerPersonId));
        if (transferAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transferAbsoluteDay),
                "TransferAbsoluteDay cannot be negative.");
        }

        TransferAbsoluteDay = transferAbsoluteDay;
    }

    public PropertyId PropertyId { get; }
    public PersonId PreviousOwnerPersonId { get; }
    public PersonId NewOwnerPersonId { get; }
    public long TransferAbsoluteDay { get; }

    internal static int Compare(
        PropertyOwnershipTransferHistoryRecord left,
        PropertyOwnershipTransferHistoryRecord right)
    {
        int property = string.CompareOrdinal(left.PropertyId.Value, right.PropertyId.Value);
        if (property != 0) return property;
        int day = left.TransferAbsoluteDay.CompareTo(right.TransferAbsoluteDay);
        if (day != 0) return day;
        int previous = string.CompareOrdinal(
            left.PreviousOwnerPersonId.Value,
            right.PreviousOwnerPersonId.Value);
        return previous != 0
            ? previous
            : string.CompareOrdinal(left.NewOwnerPersonId.Value, right.NewOwnerPersonId.Value);
    }
}

public sealed class PropertyOwnershipTransferTransition : IEquatable<PropertyOwnershipTransferTransition>
{
    internal PropertyOwnershipRecord ExpectedOwnership { get; }
    internal PersonRuntime ExpectedNewOwner { get; }
    public PropertyId PropertyId { get; }
    public PersonId ExpectedOwnerPersonId { get; }
    public PersonId NewOwnerPersonId { get; }
    public long TransferAbsoluteDay { get; }
    public long ExpectedPropertyStoreRevision { get; }

    internal PropertyOwnershipTransferTransition(
        PropertyOwnershipRecord expectedOwnership,
        PersonRuntime expectedNewOwner,
        long transferAbsoluteDay,
        long expectedPropertyStoreRevision)
    {
        ExpectedOwnership = expectedOwnership;
        ExpectedNewOwner = expectedNewOwner;
        PropertyId = expectedOwnership?.PropertyId;
        ExpectedOwnerPersonId = expectedOwnership?.OwnerPersonId;
        NewOwnerPersonId = expectedNewOwner?.PersonId;
        TransferAbsoluteDay = transferAbsoluteDay;
        ExpectedPropertyStoreRevision = expectedPropertyStoreRevision;
    }

    public bool Equals(PropertyOwnershipTransferTransition other)
    {
        return other != null
            && PropertyId == other.PropertyId
            && ExpectedOwnerPersonId == other.ExpectedOwnerPersonId
            && NewOwnerPersonId == other.NewOwnerPersonId
            && TransferAbsoluteDay == other.TransferAbsoluteDay
            && ExpectedPropertyStoreRevision == other.ExpectedPropertyStoreRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyOwnershipTransferTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = PropertyId != null ? PropertyId.GetHashCode() : 0;
            hash = (hash * 397)
                ^ (ExpectedOwnerPersonId != null ? ExpectedOwnerPersonId.GetHashCode() : 0);
            hash = (hash * 397)
                ^ (NewOwnerPersonId != null ? NewOwnerPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ TransferAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedPropertyStoreRevision.GetHashCode();
        }
    }
}
