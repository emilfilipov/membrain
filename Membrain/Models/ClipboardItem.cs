namespace Membrain.Models;

public sealed class ClipboardItem
{
    public required string Text { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }

    public string Preview
    {
        get
        {
            var flattened = Text.Replace("\r", " ").Replace("\n", " ").Trim();
            if (flattened.Length <= 80)
            {
                return flattened;
            }

            return flattened[..80] + "...";
        }
    }

    public string Timestamp => CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss");
}
