using System;

/// <summary>
/// Stable semantic identity for one property in a simulation world.
/// </summary>
public sealed class PropertyId : IEquatable<PropertyId>
{
    private readonly string value;

    public string Value => value;

    public PropertyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PropertyId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out PropertyId propertyId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            propertyId = null;
            return false;
        }

        propertyId = new PropertyId(value);
        return true;
    }

    public bool Equals(PropertyId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(PropertyId left, PropertyId right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(PropertyId left, PropertyId right)
    {
        return (left == right) == false;
    }
}

public enum PropertyFoundationFailureCode
{
    None = 0,
    InvalidPropertyId,
    InvalidPersonId,
    InvalidOwnershipRecord,
    DuplicatePropertyId,
    InvalidOwner,
    PropertyNotFound,
    SameOwner,
    InvalidTransition,
    StaleOwnership,
    RevisionOverflow
}

/// <summary>
/// Structured failure for expected property ownership operations.
/// </summary>
public sealed class PropertyFoundationFailure : IEquatable<PropertyFoundationFailure>
{
    private static readonly PropertyFoundationFailure none =
        new PropertyFoundationFailure(PropertyFoundationFailureCode.None, string.Empty);

    private PropertyFoundationFailure(PropertyFoundationFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PropertyFoundationFailure None => none;

    public PropertyFoundationFailureCode Code { get; }

    public string Message { get; }

    public bool IsFailure => Code != PropertyFoundationFailureCode.None;

    public static PropertyFoundationFailure Create(
        PropertyFoundationFailureCode code,
        string message)
    {
        return code == PropertyFoundationFailureCode.None
            ? None
            : new PropertyFoundationFailure(code, message);
    }

    public bool Equals(PropertyFoundationFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyFoundationFailure);
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

/// <summary>
/// Immutable world-truth reference to the current semantic owner of property.
/// Owners are either Persons or explicitly opened estates; materialized
/// execution state is never an owner identity.
/// </summary>
public sealed class PropertyOwnerReference : IEquatable<PropertyOwnerReference>
{
    private PropertyOwnerReference(PersonId personId, EstateId estateId)
    {
        if ((personId == null) == (estateId == null))
        {
            throw new ArgumentException(
                "Exactly one PersonId or EstateId owner is required.");
        }

        PersonId = personId;
        EstateId = estateId;
    }

    public PersonId PersonId { get; }

    public EstateId EstateId { get; }

    public bool IsPersonOwner => PersonId != null;

    public bool IsEstateOwner => EstateId != null;

    public static PropertyOwnerReference ForPerson(PersonId personId)
    {
        return new PropertyOwnerReference(
            personId ?? throw new ArgumentNullException(nameof(personId)),
            null);
    }

    public static PropertyOwnerReference ForEstate(EstateId estateId)
    {
        return new PropertyOwnerReference(
            null,
            estateId ?? throw new ArgumentNullException(nameof(estateId)));
    }

    public bool Equals(PropertyOwnerReference other)
    {
        return other != null
            && PersonId == other.PersonId
            && EstateId == other.EstateId;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyOwnerReference);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((PersonId != null ? PersonId.GetHashCode() : 0) * 397)
                ^ (EstateId != null ? EstateId.GetHashCode() : 0);
        }
    }

    public static bool operator ==(
        PropertyOwnerReference left,
        PropertyOwnerReference right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(
        PropertyOwnerReference left,
        PropertyOwnerReference right)
    {
        return (left == right) == false;
    }
}

/// <summary>
/// Immutable world-truth ownership relation. It intentionally references a
/// PersonId or EstateId rather than a materialized execution object, so dormant or dead Persons
/// remain represented without depending on execution materialization.
/// </summary>
public sealed class PropertyOwnershipRecord : IEquatable<PropertyOwnershipRecord>
{
    public PropertyOwnershipRecord(PropertyId propertyId, PersonId ownerPersonId)
        : this(propertyId, PropertyOwnerReference.ForPerson(ownerPersonId))
    {
    }

    public PropertyOwnershipRecord(
        PropertyId propertyId,
        PropertyOwnerReference owner)
    {
        PropertyId = propertyId ?? throw new ArgumentNullException(nameof(propertyId));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public PropertyId PropertyId { get; }

    public PropertyOwnerReference Owner { get; }

    public PersonId OwnerPersonId => Owner.PersonId;

    public bool Equals(PropertyOwnershipRecord other)
    {
        return other != null
            && PropertyId == other.PropertyId
            && Equals(Owner, other.Owner);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyOwnershipRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((PropertyId != null ? PropertyId.GetHashCode() : 0) * 397)
                ^ (Owner != null ? Owner.GetHashCode() : 0);
        }
    }
}

/// <summary>
/// Optimistic proposal for an explicit property ownership transfer. The
/// transition is a generic world command; no inheritance or legal policy is
/// inferred by this foundation.
/// </summary>
public sealed class PropertyOwnershipTransition : IEquatable<PropertyOwnershipTransition>
{
    internal PropertyOwnershipRecord ExpectedRecord { get; }

    public PropertyId PropertyId { get; }

    public PropertyOwnerReference ExpectedOwner { get; }

    public PropertyOwnerReference NewOwner { get; }

    public long ExpectedStoreRevision { get; }

    internal PropertyOwnershipTransition(
        PropertyOwnershipRecord expectedRecord,
        PropertyOwnerReference newOwner,
        long expectedStoreRevision)
    {
        ExpectedRecord = expectedRecord;
        PropertyId = expectedRecord?.PropertyId;
        ExpectedOwner = expectedRecord?.Owner;
        NewOwner = newOwner;
        ExpectedStoreRevision = expectedStoreRevision;
    }

    public bool Equals(PropertyOwnershipTransition other)
    {
        return other != null
            && PropertyId == other.PropertyId
            && Equals(ExpectedOwner, other.ExpectedOwner)
            && Equals(NewOwner, other.NewOwner)
            && ExpectedStoreRevision == other.ExpectedStoreRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PropertyOwnershipTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = PropertyId != null ? PropertyId.GetHashCode() : 0;
            hash = (hash * 397)
                ^ (ExpectedOwner != null ? ExpectedOwner.GetHashCode() : 0);
            hash = (hash * 397) ^ (NewOwner != null ? NewOwner.GetHashCode() : 0);
            return (hash * 397) ^ ExpectedStoreRevision.GetHashCode();
        }
    }
}
