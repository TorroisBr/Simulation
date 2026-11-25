using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using WorldAtlas.Desktop;

[assembly: AvaloniaTestApplication(typeof(WorldAtlas.Tests.TestAppBuilder))]

namespace WorldAtlas.Tests;

public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
