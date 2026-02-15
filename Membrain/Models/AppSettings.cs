namespace Membrain.Models;

public sealed class AppSettings
{
    public string ActivationHotkey { get; set; } = "CapsLock+D";
    public ScreenSide ScreenSide { get; set; } = ScreenSide.Left;
    public string ScrollUpKey { get; set; } = "Up";
    public string ScrollDownKey { get; set; } = "Down";
    public string SelectKey { get; set; } = "S";
    public string OpenSettingsKey { get; set; } = "A";
    public string HideStripKey { get; set; } = "D";
    public string SettingsSaveKey { get; set; } = "A";
    public string SettingsUpdateKey { get; set; } = "S";
    public string SettingsBackKey { get; set; } = "D";
    public int RetainedItemsLimit { get; set; } = 15;
    public int AutoHideSeconds { get; set; } = 6;
}
