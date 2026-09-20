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
    InvalidPropertyId = 1,
    InvalidPersonId = 2,
    InvalidOwnershipRecord = 3,
    DuplicatePropertyId = 4,
    PersonNotRegistered = 5,
    RevisionOverflow = 6
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
/// Immutable world-truth property ownership. The owner is a PersonId rather
/// than a materialized execution object, so dormant or dead Persons remain
/// owners until a later explicit property transition exists.
/// </summary>
public sealed class PropertyOwnershipRecord : IEquatable<PropertyOwnershipRecord>
{
    public PropertyOwnershipRecord(PropertyId propertyId, PersonId ownerPersonId)
    {
        PropertyId = propertyId ?? throw new ArgumentNullException(nameof(propertyId));
        OwnerPersonId = ownerPersonId ?? throw new ArgumentNullException(nameof(ownerPersonId));
    }

    public PropertyId PropertyId { get; }
    public PersonId OwnerPersonId { get; }

    public bool Equals(PropertyOwnershipRecord other)
    {
        return other != null
            && PropertyId == other.PropertyId
            && OwnerPersonId == other.OwnerPersonId;
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
                ^ (OwnerPersonId != null ? OwnerPersonId.GetHashCode() : 0);
        }
    }
}
