namespace Membrain.Models;

public sealed class AppSettings
{
    public string ActivationHotkey { get; set; } = "Ctrl+Shift+Space";
    public ScreenSide ScreenSide { get; set; } = ScreenSide.Left;
    public string ScrollUpKey { get; set; } = "Up";
    public string ScrollDownKey { get; set; } = "Down";
    public string SelectKey { get; set; } = "Enter";
    public int RetainedItemsLimit { get; set; } = 15;
    public int AutoHideSeconds { get; set; } = 6;
}
