using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

internal interface IP12AContinuationFileOperations : IAtomicFileSlotOperations { }
internal sealed class P12AContinuationFileOperations : IP12AContinuationFileOperations
{
    internal static readonly P12AContinuationFileOperations Instance=new P12AContinuationFileOperations();
    private P12AContinuationFileOperations(){}
    private IAtomicFileSlotOperations Inner=>SystemAtomicFileSlotOperations.Instance;
    public void CreateDirectory(string p)=>Inner.CreateDirectory(p); public bool Exists(string p)=>Inner.Exists(p);
    public Stream CreateNewTemporary(string p)=>Inner.CreateNewTemporary(p); public Stream OpenRead(string p)=>Inner.OpenRead(p);
    public void FlushToDisk(Stream s)=>Inner.FlushToDisk(s); public void Replace(string s,string d)=>Inner.Replace(s,d);
    public void Move(string s,string d)=>Inner.Move(s,d); public void Delete(string p)=>Inner.Delete(p);
}
internal static class P12AContinuationStore
{
    private const string DirectoryName="continuations"; private const string FileName="unity-bootstrap-daily-v1.simcont";
    internal static string SlotPath(string root){if(string.IsNullOrWhiteSpace(root))throw new ArgumentException(nameof(root));return Path.Combine(root,DirectoryName,FileName);}
    internal static bool TrySaveCurrent(byte[] b,P12ADailyV1SectionManifest m,P12ACompatibilityDescriptor c,out P12AResult r)=>TrySave(b,m,c,UnityEngine.Application.persistentDataPath,out r);
    internal static bool TryLoadCurrent(P12ADailyV1SectionManifest m,P12ACompatibilityDescriptor c,out P12AContinuationImage i,out P12AResult r)=>TryLoad(m,c,UnityEngine.Application.persistentDataPath,out i,out r);
    internal static bool TryLoadCurrent(P12ACompatibilityDescriptor c,out P12AContinuationImage i,out P12AResult r)=>TryLoad(null,c,UnityEngine.Application.persistentDataPath,out i,out r);
    internal static bool TryHasCurrentSlot(out bool e,out P12AResult r)=>TryHasSlot(UnityEngine.Application.persistentDataPath,out e,out r);
    internal static bool TryHasSlot(string root,out bool exists,out P12AResult result)=>TryHasSlot(root,P12AContinuationFileOperations.Instance,out exists,out result);
    internal static bool TryHasSlot(string root,IP12AContinuationFileOperations ops,out bool exists,out P12AResult result)
    {
        exists=false;result=new P12AResult(P12AResultCode.StorageFailure);
        if(string.IsNullOrWhiteSpace(root)||ops==null){result=new P12AResult(P12AResultCode.InvalidOwnerValue);return false;}
        AtomicSlotResult status=AtomicFileSlot.Inspect(SlotPath(root),ops);
        if(status==AtomicSlotResult.Success){exists=true;result=new P12AResult(P12AResultCode.Loaded);return true;}
        if(status==AtomicSlotResult.Missing){result=new P12AResult(P12AResultCode.NoSave);return true;}
        if(status==AtomicSlotResult.InvalidInput)result=new P12AResult(P12AResultCode.InvalidOwnerValue);
        return false;
    }
    internal static bool TrySave(byte[] b,P12ADailyV1SectionManifest m,P12ACompatibilityDescriptor c,string root,out P12AResult r)=>TrySave(b,m,c,root,P12AContinuationFileOperations.Instance,out r);
    internal static bool TrySave(byte[] bytes,P12ADailyV1SectionManifest manifest,P12ACompatibilityDescriptor compatibility,string root,IP12AContinuationFileOperations ops,out P12AResult result)
    {
        result=new P12AResult(P12AResultCode.StorageFailure);
        if(bytes==null||manifest==null||compatibility==null||string.IsNullOrWhiteSpace(root)||ops==null){result=new P12AResult(P12AResultCode.InvalidOwnerValue);return false;}
        if(bytes.Length>P12AContinuationCodec.MaxFrameBytes){result=new P12AResult(P12AResultCode.LimitExceeded);return false;}
        if(!P12AContinuationCodec.TryDecode(bytes,manifest,compatibility,out _,out var validation)){result=validation??new P12AResult(P12AResultCode.CorruptOrTruncated);return false;}
        AtomicSlotResult status=AtomicFileSlot.Write(SlotPath(root),bytes,P12AContinuationCodec.MaxFrameBytes,ops,reread=>P12AContinuationCodec.TryDecode(reread,manifest,compatibility,out _,out _));
        if(status==AtomicSlotResult.Success){result=new P12AResult(P12AResultCode.Saved);return true;}
        if(status==AtomicSlotResult.InvalidInput)result=new P12AResult(P12AResultCode.InvalidOwnerValue);
        return false;
    }
    internal static bool TryLoad(P12ADailyV1SectionManifest manifest,P12ACompatibilityDescriptor compatibility,string root,out P12AContinuationImage image,out P12AResult result)
    {
        image=null;result=new P12AResult(P12AResultCode.StorageFailure);
        if(compatibility==null||string.IsNullOrWhiteSpace(root)){result=new P12AResult(P12AResultCode.InvalidOwnerValue);return false;}
        AtomicSlotResult status=AtomicFileSlot.Read(SlotPath(root),P12AContinuationCodec.MaxFrameBytes,SystemAtomicFileSlotOperations.Instance,out byte[] bytes);
        if(status==AtomicSlotResult.Missing){result=new P12AResult(P12AResultCode.NoSave);return false;}
        if(status!=AtomicSlotResult.Success){if(status==AtomicSlotResult.InvalidInput)result=new P12AResult(P12AResultCode.InvalidOwnerValue);return false;}
        if(!P12AContinuationCodec.TryDecode(bytes,manifest,compatibility,out image,out result)){image=null;return false;}
        result=new P12AResult(P12AResultCode.Loaded);return true;
    }
}
