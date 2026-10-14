using System;

public sealed class MarketPriceParityVector
{
    public readonly int BasePriceBits;
    public readonly int DesiredAmount;
    public readonly int Amount;
    public readonly int ExpectedPriceBits;

    public MarketPriceParityVector(int basePriceBits, int desiredAmount, int amount, int expectedPriceBits)
    {
        BasePriceBits = basePriceBits;
        DesiredAmount = desiredAmount;
        Amount = amount;
        ExpectedPriceBits = expectedPriceBits;
    }
}

public static class MarketPriceParityVectors
{
    private static readonly MarketPriceParityVector[] values =
    {
        new MarketPriceParityVector(unchecked((int)0x40000000), 3, 6, 0x3F800000),
        new MarketPriceParityVector(0x3FC00000, 3, 4, 0x3F900000),
        new MarketPriceParityVector(0x40000000, 9, 3, 0x40C00000),
        new MarketPriceParityVector(0x40000000, 1, 3, 0x3F800000),
        new MarketPriceParityVector(0x3C23D70A, 3, 2, 0x3C75C28F),
        new MarketPriceParityVector(unchecked((int)0x80000000), 1, 2, 0x3BA3D70A),
        new MarketPriceParityVector(0x7F800000, 1, 2, 0x7F800000),
        new MarketPriceParityVector(unchecked((int)0xFF800000), 1, 2, 0x3BA3D70A),
        new MarketPriceParityVector(0x7FC00000, 1, 2, 0x7FC00000)
    };

    public static MarketPriceParityVector[] All => (MarketPriceParityVector[])values.Clone();

    public static float ToSingle(int bits) => BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

    public static int ToBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
}
