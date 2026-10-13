using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

internal interface IP12AContinuationFileOperations
{
    void CreateDirectory(string path);
    bool Exists(string path);
    Stream CreateNewTemporary(string path);
    Stream OpenRead(string path);
    void FlushToDisk(Stream stream);
    void Replace(string source, string destination);
    void Move(string source, string destination);
    void Delete(string path);
}

internal sealed class P12AContinuationFileOperations : IP12AContinuationFileOperations
{
    internal static readonly P12AContinuationFileOperations Instance = new P12AContinuationFileOperations();
    private P12AContinuationFileOperations() { }

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public bool Exists(string path) => File.Exists(path);
    public Stream CreateNewTemporary(string path) => new FileStream(path, FileMode.CreateNew,
        FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.WriteThrough);
    public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    public void FlushToDisk(Stream stream)
    {
        if (!(stream is FileStream file)) throw new IOException();
        file.Flush(true);
    }
    public void Replace(string source, string destination) => File.Replace(source, destination, null);
    public void Move(string source, string destination) => File.Move(source, destination);
    public void Delete(string path) => File.Delete(path);
}

/// <summary>Single-slot atomic storage for the selected Daily-v1 continuation.</summary>
internal static class P12AContinuationStore
{
    private const string DirectoryName = "continuations";
    private const string FileName = "unity-bootstrap-daily-v1.simcont";
    internal static string SlotPath(string persistentDataPath)
    {
        if (string.IsNullOrWhiteSpace(persistentDataPath)) throw new ArgumentException(nameof(persistentDataPath));
        return Path.Combine(persistentDataPath, DirectoryName, FileName);
    }

    internal static bool TrySaveCurrent(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, out P12AResult result) =>
        TrySave(bytes, manifest, compatibility, UnityEngine.Application.persistentDataPath, out result);

    internal static bool TryLoadCurrent(P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, out P12AContinuationImage image,
        out P12AResult result) =>
        TryLoad(manifest, compatibility, UnityEngine.Application.persistentDataPath, out image, out result);

    internal static bool TryLoadCurrent(P12ACompatibilityDescriptor compatibility,
        out P12AContinuationImage image, out P12AResult result) =>
        TryLoad(null, compatibility, UnityEngine.Application.persistentDataPath, out image, out result);

    internal static bool TryHasCurrentSlot(out bool exists, out P12AResult result)
        => TryHasSlot(UnityEngine.Application.persistentDataPath, out exists, out result);

    internal static bool TryHasSlot(string persistentDataPath, out bool exists, out P12AResult result)
        => TryHasSlot(persistentDataPath, P12AContinuationFileOperations.Instance, out exists, out result);

    internal static bool TryHasSlot(string persistentDataPath,
        IP12AContinuationFileOperations fileOperations, out bool exists, out P12AResult result)
    {
        exists = false;
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (string.IsNullOrWhiteSpace(persistentDataPath) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }
        try
        {
            string slot = SlotPath(persistentDataPath);
            using (fileOperations.OpenRead(slot)) { }
            exists = true;
            result = new P12AResult(P12AResultCode.Loaded);
            return true;
        }
        catch (FileNotFoundException)
        {
            result = new P12AResult(P12AResultCode.NoSave);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            result = new P12AResult(P12AResultCode.NoSave);
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            exists = false;
            return false;
        }
    }

    internal static bool TrySave(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath, out P12AResult result)
        => TrySave(bytes, manifest, compatibility, persistentDataPath,
            P12AContinuationFileOperations.Instance, out result);

    internal static bool TrySave(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath,
        IP12AContinuationFileOperations fileOperations, out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (bytes == null || manifest == null || compatibility == null
            || string.IsNullOrWhiteSpace(persistentDataPath) || fileOperations == null
            || bytes.Length > P12AContinuationCodec.MaxFrameBytes)
        {
            result = new P12AResult(bytes != null && bytes.Length > P12AContinuationCodec.MaxFrameBytes
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
            return false;
        }
        if (!P12AContinuationCodec.TryDecode(bytes, manifest, compatibility,
                out _, out P12AResult validation))
        {
            result = validation ?? new P12AResult(P12AResultCode.CorruptOrTruncated);
            return false;
        }

        string slot = null;
        string temporary = null;
        bool temporaryCreatedByThisAttempt = false;
        try
        {
            slot = SlotPath(persistentDataPath);
            string directory = Path.GetDirectoryName(slot);
            fileOperations.CreateDirectory(directory);
            temporary = slot + "." + Guid.NewGuid().ToString("N") + ".tmp";
            Stream created = fileOperations.CreateNewTemporary(temporary);
            temporaryCreatedByThisAttempt = true;
            using (created)
            {
                created.Write(bytes, 0, bytes.Length);
                fileOperations.FlushToDisk(created);
            }

            if (!TryReadBounded(fileOperations, temporary, out byte[] reread)
                || !FixedTimeBytesEqual(bytes, reread)
                || !P12AContinuationCodec.TryDecode(reread, manifest, compatibility, out _, out _))
            {
                result = new P12AResult(P12AResultCode.StorageFailure);
                return false;
            }

            if (fileOperations.Exists(slot)) fileOperations.Replace(temporary, slot);
            else fileOperations.Move(temporary, slot);
            temporaryCreatedByThisAttempt = false;
            result = new P12AResult(P12AResultCode.Saved);
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            result = new P12AResult(P12AResultCode.StorageFailure);
            return false;
        }
        finally
        {
            if (temporaryCreatedByThisAttempt && !string.IsNullOrEmpty(temporary))
            {
                try { if (fileOperations.Exists(temporary)) fileOperations.Delete(temporary); }
                catch (Exception exception) when (IsStorageException(exception)) { }
            }
        }
    }

    internal static bool TryLoad(P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath,
        out P12AContinuationImage image, out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (compatibility == null || string.IsNullOrWhiteSpace(persistentDataPath))
        { result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
        try
        {
            string slot = SlotPath(persistentDataPath);
            IP12AContinuationFileOperations fileOperations = P12AContinuationFileOperations.Instance;
            if (!TryReadBounded(fileOperations, slot, out byte[] bytes))
            { result = new P12AResult(P12AResultCode.StorageFailure); return false; }
            if (!P12AContinuationCodec.TryDecode(bytes, manifest, compatibility, out image, out result))
            { image = null; return false; }
            result = new P12AResult(P12AResultCode.Loaded);
            return true;
        }
        catch (FileNotFoundException)
        {
            image = null;
            result = new P12AResult(P12AResultCode.NoSave);
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            image = null;
            result = new P12AResult(P12AResultCode.NoSave);
            return false;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            image = null;
            result = new P12AResult(P12AResultCode.StorageFailure);
            return false;
        }
    }

    private static bool TryReadBounded(IP12AContinuationFileOperations fileOperations,
        string path, out byte[] bytes)
    {
        bytes = null;
        using (Stream stream = fileOperations.OpenRead(path))
        {
            if (stream.Length < 0L || stream.Length > P12AContinuationCodec.MaxFrameBytes) return false;
            int length = checked((int)stream.Length);
            byte[] candidate = new byte[length];
            int offset = 0;
            while (offset < candidate.Length)
            {
                int read = stream.Read(candidate, offset, candidate.Length - offset);
                if (read <= 0) return false;
                offset += read;
            }
            if (stream.ReadByte() != -1) return false;
            bytes = candidate;
            return true;
        }
    }

    private static bool FixedTimeBytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        using (SHA256 sha = SHA256.Create())
        {
            byte[] leftHash = sha.ComputeHash(left);
            byte[] rightHash = sha.ComputeHash(right);
            int difference = 0;
            for (int index = 0; index < leftHash.Length; index++) difference |= leftHash[index] ^ rightHash[index];
            return difference == 0;
        }
    }

    private static bool IsStorageException(Exception exception) => exception is IOException
        || exception is UnauthorizedAccessException || exception is ArgumentException
        || exception is NotSupportedException || exception is SecurityException
        || exception is OverflowException;
}
