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
                .Select(Normalize)
                .Where(item => item != null)
                .Select(item => item!)
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
        var itemList = items.ToList();
        var json = JsonSerializer.Serialize(itemList, JsonOptions);
        File.WriteAllText(AppPaths.ClipboardHistoryPath, json);
        CleanupOrphanedImages(itemList);
    }

    private static ClipboardItem? Normalize(ClipboardItem item)
    {
        try
        {
            if (item.Kind == ClipboardItemKind.Image)
            {
                if (string.IsNullOrWhiteSpace(item.ImagePath) || !File.Exists(item.ImagePath))
                {
                    return null;
                }

                var hash = string.IsNullOrWhiteSpace(item.ContentHash)
                    ? ClipboardHashService.ComputeImageHash(File.ReadAllBytes(item.ImagePath))
                    : item.ContentHash;

                return new ClipboardItem
                {
                    Kind = ClipboardItemKind.Image,
                    ContentHash = hash,
                    ImagePath = item.ImagePath,
                    ImagePixelWidth = item.ImagePixelWidth,
                    ImagePixelHeight = item.ImagePixelHeight,
                    CapturedAtUtc = item.CapturedAtUtc
                };
            }

            if (string.IsNullOrWhiteSpace(item.Text))
            {
                return null;
            }

            var textHash = string.IsNullOrWhiteSpace(item.ContentHash)
                ? ClipboardHashService.ComputeTextHash(item.Text)
                : item.ContentHash;

            return new ClipboardItem
            {
                Kind = ClipboardItemKind.Text,
                ContentHash = textHash,
                Text = item.Text,
                CapturedAtUtc = item.CapturedAtUtc
            };
        }
        catch
        {
            return null;
        }
    }

    private static void CleanupOrphanedImages(IReadOnlyCollection<ClipboardItem> items)
    {
        var usedImages = new HashSet<string>(
            items
                .Where(item => item.Kind == ClipboardItemKind.Image && !string.IsNullOrWhiteSpace(item.ImagePath))
                .Select(item => item.ImagePath!),
            StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(AppPaths.ClipboardImagesDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(AppPaths.ClipboardImagesDirectory, "*.png"))
        {
            if (usedImages.Contains(file))
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }
}
