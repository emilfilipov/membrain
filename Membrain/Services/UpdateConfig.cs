namespace Membrain.Services;

public static class UpdateConfig
{
    public static string? GitHubRepoUrl => GetEnv("MEMBRAIN_GITHUB_REPO");
    public static string? GitHubToken => GetEnv("MEMBRAIN_GITHUB_TOKEN");
    public static string? UpdateUrl => GetEnv("MEMBRAIN_UPDATE_URL");

    public static bool IncludePrerelease =>
        string.Equals(GetEnv("MEMBRAIN_PRERELEASE"), "true", StringComparison.OrdinalIgnoreCase);

    private static string? GetEnv(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
