using System.Globalization;
using System.Text;

/// <summary>Shared length-prefixed encoding for deterministic stable identities.</summary>
public static class StableKeyEncoding
{
    public static string Encode(params string[] parts)
    {
        StringBuilder builder = new StringBuilder();
        foreach (string part in parts ?? System.Array.Empty<string>())
        {
            string value = part ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        return builder.ToString();
    }
}
