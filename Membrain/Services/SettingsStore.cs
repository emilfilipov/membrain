using System.IO;
using System.Text.Json;
using Membrain.Models;

namespace Membrain.Services;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        var path = AppPaths.SettingsPath;
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        settings.RetainedItemsLimit = Math.Clamp(settings.RetainedItemsLimit, 1, 500);
        settings.AutoHideSeconds = Math.Clamp(settings.AutoHideSeconds, 1, 3600);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(AppPaths.SettingsPath, json);
    }
}
