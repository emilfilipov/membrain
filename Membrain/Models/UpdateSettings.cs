namespace Membrain.Models;

public sealed class UpdateSettings
{
    public string? RepoUrl { get; set; }
    public string? Token { get; set; }
    public bool IncludePrerelease { get; set; }
}
