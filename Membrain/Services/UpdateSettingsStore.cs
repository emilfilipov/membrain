using System.IO;
using System.Text.Json;
using Membrain.Models;

namespace Membrain.Services;

public static class UpdateSettingsStore
{
    private sealed class StoredUpdateSettings
    {
        public string? RepoUrl { get; set; }
        public bool IncludePrerelease { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static UpdateSettings Load()
    {
        var path = AppPaths.UpdateSettingsPath;
        if (!File.Exists(path))
        {
            return new UpdateSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            var stored = JsonSerializer.Deserialize<StoredUpdateSettings>(json);
            if (stored == null)
            {
                return new UpdateSettings();
            }

            return new UpdateSettings
            {
                RepoUrl = stored.RepoUrl,
                Token = CredentialStore.LoadToken(),
                IncludePrerelease = stored.IncludePrerelease
            };
        }
        catch
        {
            return new UpdateSettings();
        }
    }

    public static void Save(UpdateSettings settings)
    {
        var stored = new StoredUpdateSettings
        {
            RepoUrl = settings.RepoUrl,
            IncludePrerelease = settings.IncludePrerelease
        };

        CredentialStore.SaveToken(settings.Token);

        var json = JsonSerializer.Serialize(stored, JsonOptions);
        File.WriteAllText(AppPaths.UpdateSettingsPath, json);
    }
}
