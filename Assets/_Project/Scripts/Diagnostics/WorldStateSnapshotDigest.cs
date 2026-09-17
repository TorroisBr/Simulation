using System;
using System.Security.Cryptography;
using System.Text;

public static class WorldStateSnapshotDigest
{
    public static string Compute(WorldStateSnapshot snapshot)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(WorldStateCanonicalWriter.Write(snapshot));
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] digest = sha256.ComputeHash(bytes);
            StringBuilder result = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest)
            {
                result.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }
}
