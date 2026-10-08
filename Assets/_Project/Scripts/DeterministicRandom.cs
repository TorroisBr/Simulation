using System;

public interface IAuthoritativeRandomSource
{
    float NextUnit(string streamKey, long drawIndex = 0L);
    DeterministicRandomStream CreateStream(string streamKey);
}

public sealed class DeterministicRandomState
{
    public int Seed { get; }
    public string StreamKey { get; }
    public long DrawIndex { get; private set; }

    public DeterministicRandomState(int seed, string streamKey, long drawIndex = 0L)
    {
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            throw new ArgumentException("A deterministic random stream requires a non-empty key.", nameof(streamKey));
        }

        if (drawIndex < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(drawIndex));
        }

        Seed = seed;
        StreamKey = streamKey;
        DrawIndex = drawIndex;
    }

    internal long ConsumeIndex()
    {
        long current = DrawIndex;
        DrawIndex++;
        return current;
    }
}

public sealed class DeterministicRandomStream
{
    private readonly IAuthoritativeRandomSource source;

    public DeterministicRandomState State { get; }

    internal DeterministicRandomStream(
        IAuthoritativeRandomSource source,
        DeterministicRandomState state)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public float NextUnit()
    {
        return source.NextUnit(State.StreamKey, State.ConsumeIndex());
    }
}

public sealed class DeterministicRandomRootSnapshot
{
    public string SchemaId { get; }
    public int SchemaVersion { get; }
    public string ProviderId { get; }
    public int ProviderVersion { get; }
    public string AlgorithmId { get; }
    public int AlgorithmVersion { get; }
    public int Seed { get; }

    internal DeterministicRandomRootSnapshot(
        string schemaId,
        int schemaVersion,
        string providerId,
        int providerVersion,
        string algorithmId,
        int algorithmVersion,
        int seed)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;
        ProviderId = providerId;
        ProviderVersion = providerVersion;
        AlgorithmId = algorithmId;
        AlgorithmVersion = algorithmVersion;
        Seed = seed;
    }
}

public sealed class DeterministicRandomSource : IAuthoritativeRandomSource
{
    private const string RootSnapshotSchemaId = "deterministic-random-root";
    private const int RootSnapshotSchemaVersion = 1;
    private const string RootSnapshotProviderId = "simulation/deterministic-random-source";
    private const int RootSnapshotProviderVersion = 1;
    private const string RootSnapshotAlgorithmId = "fnv1a64-keyed-utf16";
    private const int RootSnapshotAlgorithmVersion = 1;

    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;
    private const double UnitScale = 1.0 / 4294967296.0;

    public int Seed { get; }

    public DeterministicRandomRootSnapshot CaptureSnapshot()
    {
        return new DeterministicRandomRootSnapshot(
            RootSnapshotSchemaId,
            RootSnapshotSchemaVersion,
            RootSnapshotProviderId,
            RootSnapshotProviderVersion,
            RootSnapshotAlgorithmId,
            RootSnapshotAlgorithmVersion,
            Seed);
    }

    internal static bool TryCreateStagedFromSnapshot(
        DeterministicRandomRootSnapshot snapshot,
        out DeterministicRandomSource source,
        out string diagnostic)
    {
        source = null;
        diagnostic = null;

        if (snapshot == null)
        {
            diagnostic = "A deterministic random root snapshot is required.";
            return false;
        }

        if (!string.Equals(snapshot.SchemaId, RootSnapshotSchemaId, StringComparison.Ordinal) ||
            snapshot.SchemaVersion != RootSnapshotSchemaVersion)
        {
            diagnostic = "The deterministic random root snapshot schema is unsupported.";
            return false;
        }

        if (!string.Equals(snapshot.ProviderId, RootSnapshotProviderId, StringComparison.Ordinal) ||
            snapshot.ProviderVersion != RootSnapshotProviderVersion)
        {
            diagnostic = "The deterministic random root snapshot provider is unsupported.";
            return false;
        }

        if (!string.Equals(snapshot.AlgorithmId, RootSnapshotAlgorithmId, StringComparison.Ordinal) ||
            snapshot.AlgorithmVersion != RootSnapshotAlgorithmVersion)
        {
            diagnostic = "The deterministic random root snapshot algorithm is unsupported.";
            return false;
        }

        source = new DeterministicRandomSource(snapshot.Seed);
        diagnostic = string.Empty;
        return true;
    }

    public DeterministicRandomSource(int seed = 0)
    {
        Seed = seed;
    }

    public float NextUnit(string streamKey, long drawIndex = 0L)
    {
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            throw new ArgumentException("A deterministic random draw requires a non-empty stream key.", nameof(streamKey));
        }

        if (drawIndex < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(drawIndex));
        }

        ulong hash = FnvOffsetBasis;
        Mix(ref hash, unchecked((ulong)(uint)Seed));
        Mix(ref hash, streamKey);
        Mix(ref hash, unchecked((ulong)drawIndex));

        uint value = unchecked((uint)(hash ^ (hash >> 32)));
        return (float)((value + 0.5d) * UnitScale);
    }

    public DeterministicRandomStream CreateStream(string streamKey)
    {
        return new DeterministicRandomStream(
            this,
            new DeterministicRandomState(Seed, streamKey));
    }

    private static void Mix(ref ulong hash, ulong value)
    {
        for (int i = 0; i < sizeof(ulong); i++)
        {
            hash ^= (byte)(value >> (i * 8));
            hash *= FnvPrime;
        }
    }

    private static void Mix(ref ulong hash, string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            hash ^= (byte)character;
            hash *= FnvPrime;
            hash ^= (byte)(character >> 8);
            hash *= FnvPrime;
        }
    }
}
