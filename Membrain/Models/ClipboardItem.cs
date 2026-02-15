namespace Membrain.Models;

public sealed class ClipboardItem
{
    public required string Text { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }

    public string Preview
    {
        get
        {
            return Text.Replace("\r", "").Trim();
        }
    }

    public string Timestamp => CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss");
}
