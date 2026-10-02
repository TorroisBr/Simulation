using System;

/// <summary>Stable identity for one causal world continuation.</summary>
public sealed class WorldId : IEquatable<WorldId>
{
    private const string Prefix = "world:";
    private readonly string value;

    public string Value => value;

    public WorldId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WorldId cannot be empty.", nameof(value));

        this.value = Prefix + value.ToString("N").ToLowerInvariant();
    }

    private WorldId(string canonicalValue)
    {
        value = canonicalValue;
    }

    public static WorldId Parse(string value)
    {
        if (!TryParse(value, out WorldId worldId))
            throw new FormatException("WorldId must use the canonical world:<32 lowercase hexadecimal digits> form.");
        return worldId;
    }

    public static bool TryParse(string value, out WorldId worldId)
    {
        worldId = null;
        if (value == null || value.Length != Prefix.Length + 32
            || !value.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        for (int index = Prefix.Length; index < value.Length; index++)
        {
            char character = value[index];
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                return false;
        }

        if (!Guid.TryParseExact(value.Substring(Prefix.Length), "N", out Guid parsed)
            || parsed == Guid.Empty
            || !string.Equals(Prefix + parsed.ToString("N").ToLowerInvariant(), value, StringComparison.Ordinal))
            return false;

        worldId = new WorldId(value);
        return true;
    }

    public bool Equals(WorldId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as WorldId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(WorldId left, WorldId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(WorldId left, WorldId right) => (left == right) == false;
}
