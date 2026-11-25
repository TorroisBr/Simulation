using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Parsing;

namespace WorldAtlas.Tests;

internal static class ObservationFixture
{
    private static readonly ObservationFileReader Reader = new();

    public static byte[] ReadBytes(string name) =>
        File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo", name));

    public static ObservationDocument Read(string name) => Reader.Read(ReadBytes(name));
}
