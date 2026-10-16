using System;
using System.IO;
using System.Security;

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
        TryHasSlot(persistentDataPath, SystemAtomicFileSlotOperations.Instance, out exists, out result);

    internal static bool TryHasSlot(string persistentDataPath,
        IAtomicFileSlotOperations fileOperations, out bool exists, out P12AResult result)
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
            SystemAtomicFileSlotOperations.Instance, out result);

    internal static bool TrySave(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath,
        IAtomicFileSlotOperations fileOperations, out P12AResult result)
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

    /// <summary>Writes the .NET-v2 Daily-v1 slot at an explicit host-owned root.</summary>
    internal static bool TrySaveDotNetV2(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptorV2 compatibility, string persistentDataPath,
        IAtomicFileSlotOperations fileOperations, out P12AResult result)
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
        if (!P12AContinuationCodec.TryDecodeDotNetV2(bytes, manifest, compatibility,
                out _, out P12AResult validation))
        {
            result = validation ?? new P12AResult(P12AResultCode.CorruptOrTruncated);
            return false;
        }

        try
        {
            AtomicSlotResult status = AtomicFileSlot.Write(SlotPath(persistentDataPath), bytes,
                P12AContinuationCodec.MaxFrameBytes, fileOperations,
                reread => P12AContinuationCodec.TryDecodeDotNetV2(reread, manifest,
                    compatibility, out _, out _));
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
        out P12AContinuationImage image, out P12AResult result) =>
        TryLoad(manifest, compatibility, persistentDataPath,
            SystemAtomicFileSlotOperations.Instance, out image, out result);

    internal static bool TryLoad(P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor compatibility, string persistentDataPath,
        IAtomicFileSlotOperations fileOperations,
        out P12AContinuationImage image, out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (compatibility == null || string.IsNullOrWhiteSpace(persistentDataPath) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            AtomicSlotResult status = AtomicFileSlot.Read(SlotPath(persistentDataPath),
                P12AContinuationCodec.MaxFrameBytes, fileOperations, out byte[] bytes);
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

    /// <summary>Loads only a compatible .NET-v2 image from an explicit host-owned root.</summary>
    internal static bool TryLoadDotNetV2(P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptorV2 compatibility, string persistentDataPath,
        IAtomicFileSlotOperations fileOperations,
        out P12AContinuationImage image, out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.StorageFailure);
        if (compatibility == null || string.IsNullOrWhiteSpace(persistentDataPath) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            AtomicSlotResult status = AtomicFileSlot.Read(SlotPath(persistentDataPath),
                P12AContinuationCodec.MaxFrameBytes, fileOperations, out byte[] bytes);
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
            if (!P12AContinuationCodec.TryDecodeDotNetV2(bytes, manifest, compatibility,
                    out image, out result))
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
