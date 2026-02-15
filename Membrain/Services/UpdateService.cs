using Membrain.Models;
using Velopack;
using Velopack.Sources;

namespace Membrain.Services;

public sealed class UpdateService
{
    public UpdateManager? Manager { get; private set; }
    public string? SourceLabel { get; private set; }
    public UpdateSettings Settings { get; private set; }

    public bool IsConfigured => Manager != null;

    public UpdateService()
    {
        Settings = UpdateSettingsStore.Load();
        ApplySettings(Settings);
    }

    public void UpdateSettings(UpdateSettings settings)
    {
        Settings = settings;
        UpdateSettingsStore.Save(settings);
        ApplySettings(settings);
    }

    private void ApplySettings(UpdateSettings settings)
    {
        var repoUrl = settings.RepoUrl ?? UpdateConfig.GitHubRepoUrl;
        var token = settings.Token ?? UpdateConfig.GitHubToken;
        var includePrerelease = settings.IncludePrerelease || UpdateConfig.IncludePrerelease;

        if (!string.IsNullOrWhiteSpace(repoUrl))
        {
            SourceLabel = repoUrl;
            Manager = new UpdateManager(new GithubSource(repoUrl, token, includePrerelease));
            return;
        }

        if (!string.IsNullOrWhiteSpace(UpdateConfig.UpdateUrl))
        {
            SourceLabel = UpdateConfig.UpdateUrl;
            Manager = new UpdateManager(UpdateConfig.UpdateUrl);
            return;
        }

        SourceLabel = null;
        Manager = null;
    }
}
