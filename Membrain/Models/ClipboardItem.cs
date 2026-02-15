namespace Membrain.Models;

public sealed class ClipboardItem
{
    public ClipboardItemKind Kind { get; init; } = ClipboardItemKind.Text;
    public string ContentHash { get; init; } = string.Empty;
    public string? Text { get; init; }
    public string? ImagePath { get; init; }
    public int ImagePixelWidth { get; init; }
    public int ImagePixelHeight { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }

    public bool IsImage =>
        Kind == ClipboardItemKind.Image &&
        !string.IsNullOrWhiteSpace(ImagePath);

    public string Preview
    {
        get
        {
            if (IsImage)
            {
                return ImagePixelWidth > 0 && ImagePixelHeight > 0
                    ? $"Image {ImagePixelWidth}x{ImagePixelHeight}"
                    : "Image";
            }

            return (Text ?? string.Empty).Replace("\r", "").Trim();
        }
    }

    public string Timestamp => CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss");
}
