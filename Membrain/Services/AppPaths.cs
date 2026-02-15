using System.IO;

namespace Membrain.Services;

public static class AppPaths
{
    private const string AppFolder = "Membrain";

    public static string DataDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, AppFolder);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    public static string ClipboardHistoryPath => Path.Combine(DataDirectory, "clipboard-history.json");
    public static string UpdateSettingsPath => Path.Combine(DataDirectory, "update-settings.json");
}
