/// <summary>
/// Core-owned stable section identity and schema for the selected P8-A
/// geography image. Unity census providers and the shared image codec consume
/// the same values.
/// </summary>
internal static class P12P8AImageSectionContract
{
    internal const string HexSectionId = "p8a.hexes";
    internal const string LocationSectionId = "p8a.locations";
    internal const string ScaleContextSectionId = "p8a.scale-context";
    internal const int SchemaVersion = 1;
}