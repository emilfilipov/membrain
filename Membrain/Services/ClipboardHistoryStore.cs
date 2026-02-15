using System.IO;
using System.Text.Json;
using Membrain.Models;

namespace Membrain.Services;

public static class ClipboardHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static IReadOnlyList<ClipboardItem> Load(int maxItems)
    {
        var path = AppPaths.ClipboardHistoryPath;
        if (!File.Exists(path))
        {
            return Array.Empty<ClipboardItem>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<ClipboardItem>>(json) ?? new List<ClipboardItem>();
            return list
                .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                .OrderByDescending(item => item.CapturedAtUtc)
                .Take(Math.Clamp(maxItems, 1, 500))
                .ToList();
        }
        catch
        {
            return Array.Empty<ClipboardItem>();
        }
    }

    public static void Save(IEnumerable<ClipboardItem> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(AppPaths.ClipboardHistoryPath, json);
    }
}
