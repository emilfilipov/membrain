using System.IO;
using System.Text.Json;
using Membrain.Models;

namespace Membrain.Services;

public static class UpdateSettingsStore
{
    private sealed class StoredUpdateSettings
    {
        // Backward-compat migration: old settings used to store repo URL in JSON.
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
        StoredUpdateSettings? stored = null;

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                stored = JsonSerializer.Deserialize<StoredUpdateSettings>(json);
            }
            catch
            {
                stored = null;
            }
        }

        var repoUrl = CredentialStore.LoadRepoUrl();
        if (string.IsNullOrWhiteSpace(repoUrl) && !string.IsNullOrWhiteSpace(stored?.RepoUrl))
        {
            repoUrl = stored.RepoUrl;
            CredentialStore.SaveRepoUrl(repoUrl);
        }

        return new UpdateSettings
        {
            RepoUrl = repoUrl,
            Token = CredentialStore.LoadToken(),
            IncludePrerelease = stored?.IncludePrerelease ?? false
        };
    }

    public static void Save(UpdateSettings settings)
    {
        var stored = new StoredUpdateSettings
        {
            IncludePrerelease = settings.IncludePrerelease
        };

        CredentialStore.SaveRepoUrl(settings.RepoUrl);
        CredentialStore.SaveToken(settings.Token);

        var json = JsonSerializer.Serialize(stored, JsonOptions);
        File.WriteAllText(AppPaths.UpdateSettingsPath, json);
    }
}
