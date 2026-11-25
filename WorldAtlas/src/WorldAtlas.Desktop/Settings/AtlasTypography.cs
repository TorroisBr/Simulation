using Avalonia;

namespace WorldAtlas.Desktop.Settings;

public static class AtlasTypography
{
    private static readonly (string Key, double BaseSize)[] FontSizes =
    [
        ("AtlasFontTiny", 11),
        ("AtlasFontSmall", 12),
        ("AtlasFontBody", 14),
        ("AtlasFontMedium", 17),
        ("AtlasFontLarge", 21),
        ("AtlasFontTitle", 25),
        ("AtlasMapFontSize", 14)
    ];

    private static readonly (string Key, double BaseSize)[] ControlSizes =
    [
        ("AtlasCountBadgeWidth", 26),
        ("AtlasCountBadgeHeight", 22)
    ];

    public static double HeaderHeight(int percentage) => 128d + (percentage - 100d) * 0.5d;

    public static void Apply(int percentage)
    {
        if (!UiScalePreferenceStore.IsSupported(percentage))
        {
            throw new ArgumentOutOfRangeException(nameof(percentage));
        }

        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var multiplier = percentage / 100d;
        foreach (var (key, baseSize) in FontSizes)
        {
            resources[key] = Math.Round(baseSize * multiplier, 1);
        }

        foreach (var (key, baseSize) in ControlSizes)
        {
            resources[key] = Math.Round(baseSize * multiplier, 1);
        }
    }
}
