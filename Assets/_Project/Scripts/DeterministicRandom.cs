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

public sealed class DeterministicRandomSource : IAuthoritativeRandomSource
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;
    private const double UnitScale = 1.0 / 4294967296.0;

    public int Seed { get; }

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
