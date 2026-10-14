using System;
using System.IO;
using System.Security;

internal interface IP12AContinuationFileOperations : IAtomicFileSlotOperations { }

internal sealed class P12AContinuationFileOperations : IP12AContinuationFileOperations
{
    internal static readonly P12AContinuationFileOperations Instance = new P12AContinuationFileOperations();
    private P12AContinuationFileOperations() { }

    private IAtomicFileSlotOperations Inner => SystemAtomicFileSlotOperations.Instance;
    public void CreateDirectory(string path) => Inner.CreateDirectory(path);
    public bool Exists(string path) => Inner.Exists(path);
    public Stream CreateNewTemporary(string path) => Inner.CreateNewTemporary(path);
    public Stream OpenRead(string path) => Inner.OpenRead(path);
    public void FlushToDisk(Stream stream) => Inner.FlushToDisk(stream);
    public void Replace(string source, string destination) => Inner.Replace(source, destination);
    public void Move(string source, string destination) => Inner.Move(source, destination);
    public void Delete(string path) => Inner.Delete(path);
}

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
        P12ACompatibilityDescriptor compatibility, out P12AContinuationImage image, out P12AResult result) =>
        TryLoad(manifest, compatibility, UnityEngine.Application.persistentDataPath, out image, out result);

    internal static bool TryLoadCurrent(P12ACompatibilityDescriptor compatibility,
        out P12AContinuationImage image, out P12AResult result) =>
        TryLoad(null, compatibility, UnityEngine.Application.persistentDataPath, out image, out result);

    internal static bool TryHasCurrentSlot(out bool exists, out P12AResult result) =>
        TryHasSlot(UnityEngine.Application.persistentDataPath, out exists, out result);

    internal static bool TryHasSlot(string persistentDataPath, out bool exists, out P12AResult result) =>
        TryHasSlot(persistentDataPath, P12AContinuationFileOperations.Instance, out exists, out result);

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
            AtomicSlotResult status = AtomicFileSlot.Inspect(SlotPath(persistentDataPath), fileOperations);
            if (status == AtomicSlotResult.Success)
            {
                exists = true;
                result = new P12AResult(P12AResultCode.Loaded);
                return true;
            }
            if (status == AtomicSlotResult.Missing)
            {
                result = new P12AResult(P12AResultCode.NoSave);
                return true;
            }
            if (status == AtomicSlotResult.InvalidInput)
                result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            exists = false;
            result = new P12AResult(P12AResultCode.StorageFailure);
            return false;
        }
    }

    internal static bool TrySave(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath, out P12AResult result) =>
        TrySave(bytes, manifest, compatibility, persistentDataPath,
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

        try
        {
            AtomicSlotResult status = AtomicFileSlot.Write(SlotPath(persistentDataPath), bytes,
                P12AContinuationCodec.MaxFrameBytes, fileOperations,
                reread => P12AContinuationCodec.TryDecode(reread, manifest, compatibility, out _, out _));
            if (status == AtomicSlotResult.Success)
            {
                result = new P12AResult(P12AResultCode.Saved);
                return true;
            }
            if (status == AtomicSlotResult.InvalidInput)
                result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            result = new P12AResult(P12AResultCode.StorageFailure);
            return false;
        }
    }

    internal static bool TryLoad(P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath,
        out P12AContinuationImage image, out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (compatibility == null || string.IsNullOrWhiteSpace(persistentDataPath))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            AtomicSlotResult status = AtomicFileSlot.Read(SlotPath(persistentDataPath),
                P12AContinuationCodec.MaxFrameBytes, SystemAtomicFileSlotOperations.Instance, out byte[] bytes);
            if (status == AtomicSlotResult.Missing)
            {
                result = new P12AResult(P12AResultCode.NoSave);
                return false;
            }
            if (status != AtomicSlotResult.Success)
            {
                if (status == AtomicSlotResult.InvalidInput)
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue);
                return false;
            }
            if (!P12AContinuationCodec.TryDecode(bytes, manifest, compatibility, out image, out result))
            {
                image = null;
                return false;
            }
            result = new P12AResult(P12AResultCode.Loaded);
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            image = null;
            result = new P12AResult(P12AResultCode.StorageFailure);
            return false;
        }
    }

    private static bool IsStorageException(Exception exception) => exception is IOException
        || exception is UnauthorizedAccessException || exception is ArgumentException
        || exception is NotSupportedException || exception is SecurityException
        || exception is OverflowException;
}
