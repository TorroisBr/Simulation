using System.Globalization;
using NUnit.Framework;

public sealed class StableKeyEncodingTests
{
    [Test]
    public void EncodePreservesOrderAndEscapesDelimiterBearingComponentsByLength()
    {
        Assert.That(StableKeyEncoding.Encode("a|b", "c"), Is.EqualTo("3:a|b1:c"));
        Assert.That(StableKeyEncoding.Encode("a", "b|c"), Is.EqualTo("1:a3:b|c"));
        Assert.That(StableKeyEncoding.Encode("a|b", "c"), Is.Not.EqualTo(
            StableKeyEncoding.Encode("a", "b|c")));
    }

    [Test]
    public void EncodeTreatsNullComponentsAsEmptyAndNullArrayAsNoComponents()
    {
        Assert.That(StableKeyEncoding.Encode(null, string.Empty, "x"), Is.EqualTo("0:0:1:x"));
        Assert.That(StableKeyEncoding.Encode((string[])null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void EncodeUsesInvariantCultureForComponentLengths()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.That(StableKeyEncoding.Encode("ab", "c"), Is.EqualTo("2:ab1:c"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
