using System;

/// <summary>
/// Stable semantic identity for one individual person in a simulation world.
/// </summary>
public sealed class PersonId : IEquatable<PersonId>
{
    private readonly string value;

    public string Value => value;

    public PersonId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PersonId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out PersonId personId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            personId = null;
            return false;
        }

        personId = new PersonId(value);
        return true;
    }

    public bool Equals(PersonId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PersonId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(PersonId left, PersonId right)
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

    public static bool operator !=(PersonId left, PersonId right)
    {
        return (left == right) == false;
    }
}
