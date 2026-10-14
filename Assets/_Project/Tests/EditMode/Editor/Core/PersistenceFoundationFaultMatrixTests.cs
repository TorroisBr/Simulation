using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class PersistenceFoundationFaultMatrixTests
{
    [TestCase("create-temporary")]
    [TestCase("flush")]
    [TestCase("short-read")]
    [TestCase("long-read")]
    [TestCase("changed-reread")]
    [TestCase("validator-false")]
    [TestCase("validator-throw")]
    [TestCase("move")]
    public void AtomicWriteFaultMatrixPreservesExactDestinationAndOwnTemporary(string fault)
    {
        using (var fixture = new SlotFixture(destinationExists: fault != "move"))
        {
            byte[] previous = fixture.ExistingDestination ? new byte[] { 8, 7, 6 } : null;
            if (previous != null)
                File.WriteAllBytes(fixture.Path, previous);
            var ops = new FaultOperations(fault);
            int callbacks = 0;
            AtomicSlotResult result = AtomicFileSlot.Write(fixture.Path, new byte[] { 1, 2, 3, 4 }, 64, ops,
                _ => { callbacks++; if (fault == "validator-throw") throw new IOException("validator fault"); return fault != "validator-false"; });

            Assert.That(result, Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(callbacks, Is.EqualTo(fault == "validator-false" || fault == "validator-throw" || fault == "move" ? 1 : 0));
            if (previous != null)
                Assert.That(File.ReadAllBytes(fixture.Path), Is.EqualTo(previous));
            else
                Assert.That(File.Exists(fixture.Path), Is.False);
            Assert.That(Directory.GetFiles(fixture.Directory, "*.tmp"), Is.Empty,
                "Unexpected temporary(s): " + string.Join(",", Directory.GetFiles(fixture.Directory, "*.tmp")));
        }
    }

    [Test]
    public void ReplaceAndCleanupFailuresPreserveSlotAndReportOnlyUnremovedOwnedTemporary()
    {
        using (var fixture = new SlotFixture(destinationExists: true))
        {
            byte[] previous = { 3, 1, 4 };
            File.WriteAllBytes(fixture.Path, previous);
            var ops = new FaultOperations("replace-cleanup");
            Assert.That(AtomicFileSlot.Write(fixture.Path, new byte[] { 2, 7 }, 64, ops, _ => true),
                Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(File.ReadAllBytes(fixture.Path), Is.EqualTo(previous));
            string[] temporary = Directory.GetFiles(fixture.Directory, "*.tmp");
            Assert.That(temporary, Has.Length.EqualTo(1));
            Assert.That(ops.DeleteCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void InspectAndBoundsKeepMissingInaccessibleAndInvalidDistinct()
    {
        using (var fixture = new SlotFixture(destinationExists: false))
        {
            var ops = new FaultOperations("inaccessible");
            Assert.That(AtomicFileSlot.Inspect(fixture.Path, ops), Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(AtomicFileSlot.Read(fixture.Path, 8, SystemAtomicFileSlotOperations.Instance, out _),
                Is.EqualTo(AtomicSlotResult.Missing));
            Assert.That(Directory.Exists(fixture.Directory), Is.True);
            File.WriteAllBytes(fixture.Path, new byte[] { 1, 2, 3 });
            Assert.That(AtomicFileSlot.Read(fixture.Path, 2, SystemAtomicFileSlotOperations.Instance, out _),
                Is.EqualTo(AtomicSlotResult.StorageFailure));
            Assert.That(AtomicFileSlot.Write(fixture.Path, new byte[] { 1, 2 }, 1,
                SystemAtomicFileSlotOperations.Instance, _ => { Assert.Fail("Oversized writes must not invoke the validator."); return false; }),
                Is.EqualTo(AtomicSlotResult.InvalidInput));
        }
    }

    [Test]
    public void SuccessfulWriteCallsValidatorBetweenRereadAndReplacement()
    {
        using (var fixture = new SlotFixture(destinationExists: false))
        {
            var ops = new FaultOperations("record");
            int callbackCount = 0;
            Assert.That(AtomicFileSlot.Write(fixture.Path, new byte[] { 5, 4, 3 }, 64, ops, bytes =>
            {
                callbackCount++;
                Assert.That(bytes, Is.EqualTo(new byte[] { 5, 4, 3 }));
                ops.Events.Add("validator");
                return true;
            }), Is.EqualTo(AtomicSlotResult.Success));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(ops.Events, Is.EqualTo(new[] { "mkdir", "create", "flush", "open-temp", "validator", "exists-slot", "move" }));
            Assert.That(File.ReadAllBytes(fixture.Path), Is.EqualTo(new byte[] { 5, 4, 3 }));
        }
    }

    private sealed class SlotFixture : IDisposable
    {
        internal readonly string Directory;
        internal readonly string Path;
        internal readonly bool ExistingDestination;
        internal SlotFixture(bool destinationExists)
        {
            ExistingDestination = destinationExists;
            Directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "core-p12-fault-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            Path = System.IO.Path.Combine(Directory, "slot.bin");
        }
        public void Dispose() { if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true); }
    }

    private sealed class FaultOperations : IAtomicFileSlotOperations
    {
        private readonly string fault;
        private readonly IAtomicFileSlotOperations inner = SystemAtomicFileSlotOperations.Instance;
        internal readonly List<string> Events = new List<string>();
        internal int DeleteCalls;
        internal FaultOperations(string fault) { this.fault = fault; }
        public void CreateDirectory(string path) { Events.Add("mkdir"); if (fault == "create-directory") throw new IOException(); inner.CreateDirectory(path); }
        public bool Exists(string path)
        {
            if (path.EndsWith(".tmp", StringComparison.Ordinal))
                return inner.Exists(path);
            Events.Add("exists-slot");
            return inner.Exists(path);
        }
        public Stream CreateNewTemporary(string path)
        {
            Events.Add("create");
            if (fault == "collision")
            {
                File.WriteAllBytes(path, new byte[] { 0xC0, 0xFF, 0xEE });
                throw new IOException("foreign temp collision");
            }
            if (fault == "create" || fault == "create-temporary")
                throw new IOException();
            return inner.CreateNewTemporary(path);
        }
        public Stream OpenRead(string path)
        {
            if (fault == "inaccessible")
                throw new UnauthorizedAccessException();
            if (path.EndsWith(".tmp", StringComparison.Ordinal))
                Events.Add("open-temp");
            Stream stream = inner.OpenRead(path);
            if (!path.EndsWith(".tmp", StringComparison.Ordinal))
                return stream;
            if (fault == "short-read")
                return new AlteredReadStream(stream, shortRead: true, longRead: false, changed: false);
            if (fault == "long-read")
                return new AlteredReadStream(stream, shortRead: false, longRead: true, changed: false);
            if (fault == "changed-reread")
                return new AlteredReadStream(stream, shortRead: false, longRead: false, changed: true);
            return stream;
        }
        public void FlushToDisk(Stream stream) { Events.Add("flush"); if (fault == "flush") throw new IOException(); inner.FlushToDisk(stream); }
        public void Replace(string source, string destination) { Events.Add("replace"); if (fault == "replace" || fault == "replace-cleanup") throw new IOException(); inner.Replace(source, destination); }
        public void Move(string source, string destination) { Events.Add("move"); if (fault == "move") throw new IOException(); inner.Move(source, destination); }
        public void Delete(string path) { DeleteCalls++; Events.Add("delete"); if (fault == "replace-cleanup") throw new IOException(); inner.Delete(path); }
    }

    private sealed class AlteredReadStream : Stream
    {
        private readonly Stream inner;
        private readonly bool shortRead, longRead, changed;
        private bool stopped, changedOnce;
        internal AlteredReadStream(Stream inner, bool shortRead, bool longRead, bool changed)
        { this.inner = inner; this.shortRead = shortRead; this.longRead = longRead; this.changed = changed; }
        public override bool CanRead => inner.CanRead; public override bool CanSeek => inner.CanSeek; public override bool CanWrite => false;
        public override long Length => inner.Length; public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (shortRead && stopped)
                return 0;
            int requested = shortRead ? Math.Max(1, count / 2) : count;
            int read = inner.Read(buffer, offset, requested);
            if (shortRead && read > 0)
                stopped = true;
            if (changed && read > 0 && !changedOnce)
            { buffer[offset] ^= 0x40; changedOnce = true; }
            return read;
        }
        public override int ReadByte()
        {
            int value = inner.ReadByte();
            return longRead && value == -1 ? 0 : value;
        }
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}