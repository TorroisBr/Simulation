using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

public interface IAtomicFileSlotOperations
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

public sealed class SystemAtomicFileSlotOperations : IAtomicFileSlotOperations
{
    public static readonly SystemAtomicFileSlotOperations Instance = new SystemAtomicFileSlotOperations();

    private SystemAtomicFileSlotOperations()
    {
    }

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool Exists(string path) => File.Exists(path);

    public Stream CreateNewTemporary(string path) =>
        new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            64 * 1024, FileOptions.WriteThrough);

    public Stream OpenRead(string path) =>
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    public void FlushToDisk(Stream stream)
    {
        if (!(stream is FileStream fileStream))
            throw new IOException();

        fileStream.Flush(true);
    }

    public void Replace(string source, string destination) => File.Replace(source, destination, null);

    public void Move(string source, string destination) => File.Move(source, destination);

    public void Delete(string path) => File.Delete(path);
}

public enum AtomicSlotResult
{
    Success,
    Missing,
    InvalidInput,
    StorageFailure
}

public static class AtomicFileSlot
{
    public static AtomicSlotResult Read(
        string path,
        int maxBytes,
        IAtomicFileSlotOperations operations,
        out byte[] bytes)
    {
        bytes = null;
        if (string.IsNullOrWhiteSpace(path) || maxBytes < 0 || operations == null)
            return AtomicSlotResult.InvalidInput;

        try
        {
            if (!TryReadBounded(operations, path, maxBytes, out bytes))
                return AtomicSlotResult.StorageFailure;

            return AtomicSlotResult.Success;
        }
        catch (FileNotFoundException)
        {
            return AtomicSlotResult.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return AtomicSlotResult.Missing;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return AtomicSlotResult.StorageFailure;
        }
    }

    public static AtomicSlotResult Inspect(string path, IAtomicFileSlotOperations operations)
    {
        if (string.IsNullOrWhiteSpace(path) || operations == null)
            return AtomicSlotResult.InvalidInput;

        try
        {
            using (operations.OpenRead(path))
            {
            }

            return AtomicSlotResult.Success;
        }
        catch (FileNotFoundException)
        {
            return AtomicSlotResult.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return AtomicSlotResult.Missing;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return AtomicSlotResult.StorageFailure;
        }
    }

    public static AtomicSlotResult Write(
        string path,
        byte[] bytes,
        int maxBytes,
        IAtomicFileSlotOperations operations,
        Func<byte[], bool> validator)
    {
        if (string.IsNullOrWhiteSpace(path) || bytes == null || maxBytes < 0
            || bytes.Length > maxBytes || operations == null || validator == null)
            return AtomicSlotResult.InvalidInput;

        string temporaryPath = null;
        bool ownsTemporary = false;
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return AtomicSlotResult.InvalidInput;

            operations.CreateDirectory(directory);
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            Stream created = operations.CreateNewTemporary(temporaryPath);
            ownsTemporary = true;

            using (created)
            {
                created.Write(bytes, 0, bytes.Length);
                operations.FlushToDisk(created);
            }

            if (!TryReadBounded(operations, temporaryPath, maxBytes, out byte[] reread)
                || !FixedTimeBytesEqual(bytes, reread))
                return AtomicSlotResult.StorageFailure;

            if (!validator(reread))
                return AtomicSlotResult.StorageFailure;

            if (operations.Exists(path))
                operations.Replace(temporaryPath, path);
            else
                operations.Move(temporaryPath, path);

            ownsTemporary = false;
            return AtomicSlotResult.Success;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return AtomicSlotResult.StorageFailure;
        }
        finally
        {
            if (ownsTemporary && !string.IsNullOrEmpty(temporaryPath))
            {
                try
                {
                    if (operations.Exists(temporaryPath))
                        operations.Delete(temporaryPath);
                }
                catch (Exception exception) when (IsStorageException(exception))
                {
                }
            }
        }
    }

    private static bool TryReadBounded(
        IAtomicFileSlotOperations operations,
        string path,
        int limit,
        out byte[] bytes)
    {
        bytes = null;
        using (Stream stream = operations.OpenRead(path))
        {
            if (stream.Length < 0 || stream.Length > limit)
                return false;

            byte[] buffer = new byte[checked((int)stream.Length)];
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read <= 0)
                    return false;

                offset += read;
            }

            if (stream.ReadByte() != -1)
                return false;

            bytes = buffer;
            return true;
        }
    }

    private static bool FixedTimeBytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;

        using (SHA256 hash = SHA256.Create())
        {
            byte[] leftDigest = hash.ComputeHash(left);
            byte[] rightDigest = hash.ComputeHash(right);
            int difference = 0;
            for (int i = 0; i < leftDigest.Length; i++)
                difference |= leftDigest[i] ^ rightDigest[i];

            return difference == 0;
        }
    }

    internal static bool IsStorageException(Exception exception) =>
        exception is IOException
        || exception is UnauthorizedAccessException
        || exception is ArgumentException
        || exception is NotSupportedException
        || exception is SecurityException
        || exception is OverflowException;
}