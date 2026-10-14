using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

public interface IAtomicFileSlotOperations
{
    void CreateDirectory(string path); bool Exists(string path); Stream CreateNewTemporary(string path);
    Stream OpenRead(string path); void FlushToDisk(Stream stream); void Replace(string source,string destination);
    void Move(string source,string destination); void Delete(string path);
}
public sealed class SystemAtomicFileSlotOperations : IAtomicFileSlotOperations
{
    public static readonly SystemAtomicFileSlotOperations Instance=new SystemAtomicFileSlotOperations();
    private SystemAtomicFileSlotOperations(){}
    public void CreateDirectory(string p)=>Directory.CreateDirectory(p); public bool Exists(string p)=>File.Exists(p);
    public Stream CreateNewTemporary(string p)=>new FileStream(p,FileMode.CreateNew,FileAccess.Write,FileShare.None,64*1024,FileOptions.WriteThrough);
    public Stream OpenRead(string p)=>new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.Read);
    public void FlushToDisk(Stream s){if(!(s is FileStream f))throw new IOException();f.Flush(true);}
    public void Replace(string s,string d)=>File.Replace(s,d,null); public void Move(string s,string d)=>File.Move(s,d); public void Delete(string p)=>File.Delete(p);
}
public enum AtomicSlotResult { Success, Missing, InvalidInput, StorageFailure }
public static class AtomicFileSlot
{
    public static AtomicSlotResult Read(string path,int maxBytes,IAtomicFileSlotOperations ops,out byte[] bytes)
    {
        bytes=null;if(string.IsNullOrWhiteSpace(path)||maxBytes<0||ops==null)return AtomicSlotResult.InvalidInput;
        try{if(!TryReadBounded(ops,path,maxBytes,out bytes))return AtomicSlotResult.StorageFailure;return AtomicSlotResult.Success;}
        catch(FileNotFoundException){return AtomicSlotResult.Missing;}catch(DirectoryNotFoundException){return AtomicSlotResult.Missing;}catch(Exception e)when(IsStorageException(e)){return AtomicSlotResult.StorageFailure;}
    }
    public static AtomicSlotResult Inspect(string path,IAtomicFileSlotOperations ops)
    {
        if(string.IsNullOrWhiteSpace(path)||ops==null)return AtomicSlotResult.InvalidInput;
        try{using(ops.OpenRead(path)){}return AtomicSlotResult.Success;}
        catch(FileNotFoundException){return AtomicSlotResult.Missing;}catch(DirectoryNotFoundException){return AtomicSlotResult.Missing;}catch(Exception e)when(IsStorageException(e)){return AtomicSlotResult.StorageFailure;}
    }
    public static AtomicSlotResult Write(string path,byte[] bytes,int maxBytes,IAtomicFileSlotOperations ops,Func<byte[],bool> validator)
    {
        if(string.IsNullOrWhiteSpace(path)||bytes==null||maxBytes<0||bytes.Length>maxBytes||ops==null||validator==null)return AtomicSlotResult.InvalidInput;
        string temp=null;bool owned=false;
        try{
            string dir=Path.GetDirectoryName(path);if(string.IsNullOrEmpty(dir))return AtomicSlotResult.InvalidInput;
            ops.CreateDirectory(dir);temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            Stream created=ops.CreateNewTemporary(temp);owned=true;using(created){created.Write(bytes,0,bytes.Length);ops.FlushToDisk(created);}
            if(!TryReadBounded(ops,temp,maxBytes,out var reread)||!FixedTimeBytesEqual(bytes,reread))return AtomicSlotResult.StorageFailure;
            if(!validator(reread))return AtomicSlotResult.StorageFailure;
            if(ops.Exists(path))ops.Replace(temp,path);else ops.Move(temp,path);owned=false;return AtomicSlotResult.Success;
        }catch(Exception e)when(IsStorageException(e)){return AtomicSlotResult.StorageFailure;}
        finally{if(owned&&!string.IsNullOrEmpty(temp)){try{if(ops.Exists(temp))ops.Delete(temp);}catch(Exception e)when(IsStorageException(e)){}}}
    }
    private static bool TryReadBounded(IAtomicFileSlotOperations ops,string path,int limit,out byte[] bytes)
    {
        bytes=null;using(Stream s=ops.OpenRead(path)){if(s.Length<0||s.Length>limit)return false;byte[] b=new byte[checked((int)s.Length)];int o=0;while(o<b.Length){int n=s.Read(b,o,b.Length-o);if(n<=0)return false;o+=n;}if(s.ReadByte()!=-1)return false;bytes=b;return true;}
    }
    private static bool FixedTimeBytesEqual(byte[] a,byte[] b){if(a==null||b==null||a.Length!=b.Length)return false;using(SHA256 h=SHA256.Create()){byte[] x=h.ComputeHash(a),y=h.ComputeHash(b);int d=0;for(int i=0;i<x.Length;i++)d|=x[i]^y[i];return d==0;}}
    internal static bool IsStorageException(Exception e)=>e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException||e is SecurityException||e is OverflowException;
}