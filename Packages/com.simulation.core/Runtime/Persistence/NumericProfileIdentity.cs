using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>Pure implementation of the existing P12 IEEE-754 numeric profile identity.</summary>
public static class P12ANumericProfileIdentity
{
    private const string WireIdentity = "p12.numeric.ieee754-binary32-binary64/canonical-le-wire";
    private const string ProfilePrefix = "p12.numeric.ieee754-binary32-binary64/canonical-le-wire-v1/";

    public static bool TryCreateCoreClr(IReadOnlyList<string> assemblyMvids, out string profileId, out int profileVersion)
    {
        profileId = null;
        profileVersion = 0;
        if (assemblyMvids == null || assemblyMvids.Count == 0)
            return false;

        int singleOne = BitConverter.ToInt32(BitConverter.GetBytes(1.0f), 0);
        int singleNegativeZero = BitConverter.ToInt32(BitConverter.GetBytes(-0.0f), 0);
        long doubleOne = BitConverter.ToInt64(BitConverter.GetBytes(1.0d), 0);
        long doubleNegativeZero = BitConverter.ToInt64(BitConverter.GetBytes(-0.0d), 0);
        if (singleOne != unchecked((int)0x3f800000)
            || singleNegativeZero != unchecked((int)0x80000000)
            || doubleOne != 0x3ff0000000000000L
            || doubleNegativeZero != unchecked((long)0x8000000000000000UL))
            return false;

        var normalized = new List<string>(assemblyMvids.Count);
        foreach (string value in assemblyMvids)
        {
            if (!Guid.TryParseExact(value, "D", out Guid id) || id == Guid.Empty)
                return false;
            normalized.Add(id.ToString("D").ToLowerInvariant());
        }
        normalized.Sort(StringComparer.Ordinal);
        for (int i = 1; i < normalized.Count; i++)
            if (StringComparer.Ordinal.Equals(normalized[i - 1], normalized[i]))
                return false;

        try
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
            {
                ContinuationWire.WriteString(writer, WireIdentity);
                ContinuationWire.WriteString(writer, "CoreCLR");
                ContinuationWire.WriteString(writer, string.Empty);
                writer.Write(normalized.Count);
                foreach (string mvid in normalized)
                    ContinuationWire.WriteString(writer, mvid);
                writer.Flush();
                using (SHA256 sha = SHA256.Create())
                    profileId = ProfilePrefix + BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty).ToLowerInvariant();
            }
            profileVersion = 1;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is P12ALimitException || exception is OverflowException)
        {
            profileId = null;
            profileVersion = 0;
            return false;
        }
    }
}