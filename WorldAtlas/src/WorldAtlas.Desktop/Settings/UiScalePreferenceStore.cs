using System.Text.Json;

namespace WorldAtlas.Desktop.Settings;

public sealed record UiScaleOption(int Percentage, string Label);

public sealed class UiScalePreferenceStore
{
    private sealed record SettingsDocument(int UiScalePercent);

    public static IReadOnlyList<UiScaleOption> Options { get; } =
    [
        new(100, "100%"),
        new(125, "125%"),
        new(150, "150%"),
        new(175, "175%")
    ];

    public string SettingsPath { get; }

    public UiScalePreferenceStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorldAtlas", "settings.json");
    }

    public int Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return 100;
            }

            var settings = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(SettingsPath));
            return settings is not null && IsSupported(settings.UiScalePercent) ? settings.UiScalePercent : 100;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return 100;
        }
    }

    public void Save(int percentage)
    {
        if (!IsSupported(percentage))
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "Supported Atlas scale values are 100, 125, 150, and 175 percent.");
        }

        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("The settings path must include a directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{SettingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new SettingsDocument(percentage)));
            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static bool IsSupported(int percentage) => percentage is 100 or 125 or 150 or 175;
}
