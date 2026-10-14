using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class PersistenceFoundationTests
{
    private static P12ACompatibilityDescriptorV2 V2(ushort host=1)=>new P12ACompatibilityDescriptorV2("UnityBootstrap-Daily-v1",1,host,".NET 8","8.0.1.0","Windows","win-x64","x64","net8.0",new[]{new P12AAssemblyIdentityV2("Simulation.Core","1.0.0.0",Guid.Parse("4b37a0ac-246b-47c5-9bbc-fd79704e0666").ToString("D"))},"numeric",1,"config","content","providers","calendar",1,"calendar-hash","p9","p8");
    [Test] public void V1DescriptorMatchesIndependentPreExtractionCanonicalVector()
    {
        var source=new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1",1,"config","6000.3.9f1","WindowsEditor","Mono","build",new[]{"00000000-0000-0000-0000-000000000001"},"numeric",1,"content","providers","daily",1,"calendar","p9","p8");
        const string expected="17000000556E697479426F6F7473747261702D4461696C792D76310100000006000000636F6E6669670A000000363030302E332E3966310D00000057696E646F7773456469746F72040000004D6F6E6F050000006275696C64010000002400000030303030303030302D303030302D303030302D303030302D303030303030303030303031070000006E756D657269630100000007000000636F6E74656E740900000070726F766964657273050000006461696C79010000000800000063616C656E646172020000007039020000007038";
        Assert.That(Convert.ToHexString(source.CanonicalBytes),Is.EqualTo(expected));
        Assert.That(P12ACompatibilityDescriptor.TryReadCanonical(source.CanonicalBytes,out var parsed),Is.True);
        Assert.That(parsed.MismatchCode(new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1",1,"changed","6000.3.9f1","WindowsEditor","Mono","build",new[]{"00000000-0000-0000-0000-000000000001"},"numeric",1,"content","providers","daily",1,"calendar","p9","p8")),Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
        Assert.That(parsed.MismatchCode(new P12ACompatibilityDescriptor("UnityBootstrap-Daily-v1",1,"config","6000.3.9f1","LinuxPlayer","Mono","build",new[]{"00000000-0000-0000-0000-000000000001"},"numeric",1,"content","providers","daily",1,"calendar","p9","p8")),Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
    }
    [Test] public void V2RoundTripsCanonicalBytesAndClassifiesContentAndRuntime()
    {
        var expected=V2();Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(expected.CanonicalBytes,out var actual,out var result),Is.True);
        Assert.That(result,Is.EqualTo(P12AResultCode.Decoded));Assert.That(actual.CanonicalBytes,Is.EqualTo(expected.CanonicalBytes));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(expected.CanonicalBytes,expected,out _),Is.EqualTo(P12AResultCode.Decoded));
        Assert.That(expected.MismatchCode(V2(2)),Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        Assert.That(P12ACompatibilityAdmission.AdmitDotNetV2(V2(2).CanonicalBytes,expected,out _),Is.EqualTo(P12AResultCode.IncompatibleBuildOrRuntime));
        var changed=V2();var b=changed.CanonicalBytes; // Field changes are tested by a second canonical value below.
        var content=new P12ACompatibilityDescriptorV2("UnityBootstrap-Daily-v1",1,1,".NET 8","8.0.1.0","Windows","win-x64","x64","net8.0",new[]{new P12AAssemblyIdentityV2("Simulation.Core","1.0.0.0","4b37a0ac-246b-47c5-9bbc-fd79704e0666")},"numeric",1,"config","other","providers","calendar",1,"calendar-hash","p9","p8");
        Assert.That(expected.MismatchCode(content),Is.EqualTo(P12AResultCode.IncompatibleContentOrConfiguration));
        Assert.That(b,Is.Not.Null);
    }
    [Test] public void V2DiscriminatorNeverFallsBackAndUnityRejectsRecognizedV2()
    {
        byte[] valid=V2(2).CanonicalBytes;Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(valid),Is.EqualTo(P12AResultCode.UnsupportedSchema));
        byte[] bad=(byte[])valid.Clone();bad[4]=(byte)'X';Assert.That(P12ACompatibilityAdmission.ClassifyUnityV2(bad),Is.EqualTo(P12AResultCode.CorruptOrTruncated));
        byte[] unknown=(byte[])valid.Clone();unknown[12]=3;Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(unknown,out _,out var code),Is.False);Assert.That(code,Is.EqualTo(P12AResultCode.UnsupportedSchema));
    }
    [Test] public void AtomicSlotUsesDefaultFilesystemAndValidatorRunsOnceBeforeReplacement()
    {
        string root=Path.Combine(Path.GetTempPath(),"core-p12-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(root,"slot.bin");var ops=SystemAtomicFileSlotOperations.Instance;
        try{
            byte[] original={1,2,3};int calls=0;Assert.That(AtomicFileSlot.Write(path,original,32,ops,b=>{calls++;return true;}),Is.EqualTo(AtomicSlotResult.Success));Assert.That(calls,Is.EqualTo(1));
            Assert.That(AtomicFileSlot.Read(path,32,ops,out var read),Is.EqualTo(AtomicSlotResult.Success));Assert.That(read,Is.EqualTo(original));
            byte[] replacement={4,5};Assert.That(AtomicFileSlot.Write(path,replacement,32,ops,b=>{calls++;return false;}),Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(File.ReadAllBytes(path),Is.EqualTo(original));Assert.That(calls,Is.EqualTo(2));Assert.That(Directory.GetFiles(root,"*.tmp").Length,Is.Zero);
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Test] public void InspectDistinguishesMissingWithoutCreatingDirectory()
    {
        string root=Path.Combine(Path.GetTempPath(),"core-p12-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(root,"nested","slot");
        Assert.That(AtomicFileSlot.Inspect(path,SystemAtomicFileSlotOperations.Instance),Is.EqualTo(AtomicSlotResult.Missing));Assert.That(Directory.Exists(root),Is.False);
    }
    [TestCase("create-directory")][TestCase("create-temporary")][TestCase("write")][TestCase("flush")][TestCase("read")][TestCase("replace")]
    public void StorageFaultsPreserveDestinationAndCleanOnlyOwnedTemporary(string fault)
    {
        string root=Path.Combine(Path.GetTempPath(),"core-p12-fault-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(root,"slot.bin");
        Directory.CreateDirectory(root);byte[] prior={9,8,7};File.WriteAllBytes(path,prior);var ops=new FaultOperations(fault);
        try{Assert.That(AtomicFileSlot.Write(path,new byte[]{1,2,3},32,ops,_=>true),Is.EqualTo(AtomicSlotResult.StorageFailure));Assert.That(File.ReadAllBytes(path),Is.EqualTo(prior));Assert.That(Directory.GetFiles(root,"*.tmp").Length,Is.Zero);}
        finally{Directory.Delete(root,true);}
    }
    [Test] public void FailedCreateNewDoesNotDeleteForeignTemporaryCollision()
    {
        string root=Path.Combine(Path.GetTempPath(),"core-p12-collision-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(root,"slot.bin");
        Directory.CreateDirectory(root);byte[] prior={6};File.WriteAllBytes(path,prior);string collision=null;var ops=new CollisionOperations(p=>collision=p);
        try{Assert.That(AtomicFileSlot.Write(path,new byte[]{1},32,ops,_=>true),Is.EqualTo(AtomicSlotResult.StorageFailure));Assert.That(File.ReadAllBytes(path),Is.EqualTo(prior));Assert.That(File.ReadAllBytes(collision),Is.EqualTo(new byte[]{4,5}));}
        finally{Directory.Delete(root,true);}
    }
    private sealed class FaultOperations : IAtomicFileSlotOperations
    {
        private readonly string fault;private readonly IAtomicFileSlotOperations inner=SystemAtomicFileSlotOperations.Instance;
        internal FaultOperations(string fault){this.fault=fault;}
        public void CreateDirectory(string p){if(fault=="create-directory")throw new IOException();inner.CreateDirectory(p);}public bool Exists(string p)=>inner.Exists(p);
        public Stream CreateNewTemporary(string p){if(fault=="create-temporary")throw new IOException();Stream s=inner.CreateNewTemporary(p);return fault=="write"?new ThrowingWriteStream(s):s;}
        public Stream OpenRead(string p){if(fault=="read")throw new IOException();return inner.OpenRead(p);}public void FlushToDisk(Stream s){if(fault=="flush")throw new IOException();inner.FlushToDisk(s);}
        public void Replace(string s,string d){if(fault=="replace")throw new IOException();inner.Replace(s,d);}public void Move(string s,string d)=>inner.Move(s,d);public void Delete(string p)=>inner.Delete(p);
    }
    private sealed class ThrowingWriteStream : Stream
    {
        private readonly Stream inner;internal ThrowingWriteStream(Stream inner){this.inner=inner;}
        public override bool CanRead=>inner.CanRead;public override bool CanSeek=>inner.CanSeek;public override bool CanWrite=>inner.CanWrite;
        public override long Length=>inner.Length;public override long Position{get=>inner.Position;set=>inner.Position=value;}public override void Flush()=>inner.Flush();
        public override int Read(byte[] b,int o,int c)=>inner.Read(b,o,c);public override long Seek(long o,SeekOrigin so)=>inner.Seek(o,so);public override void SetLength(long v)=>inner.SetLength(v);
        public override void Write(byte[] b,int o,int c)=>throw new IOException();protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private sealed class CollisionOperations : IAtomicFileSlotOperations
    {
        private readonly Action<string> observe;private readonly IAtomicFileSlotOperations inner=SystemAtomicFileSlotOperations.Instance;
        internal CollisionOperations(Action<string> observe){this.observe=observe;}
        public void CreateDirectory(string p)=>inner.CreateDirectory(p);public bool Exists(string p)=>inner.Exists(p);
        public Stream CreateNewTemporary(string p){File.WriteAllBytes(p,new byte[]{4,5});observe(p);throw new IOException();}
        public Stream OpenRead(string p)=>inner.OpenRead(p);public void FlushToDisk(Stream s)=>inner.FlushToDisk(s);public void Replace(string s,string d)=>inner.Replace(s,d);
        public void Move(string s,string d)=>inner.Move(s,d);public void Delete(string p)=>inner.Delete(p);
    }
}
