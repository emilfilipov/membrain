using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Membrain.Models;
using Membrain.Services;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfTextDataFormat = System.Windows.TextDataFormat;

namespace Membrain;

public partial class MainWindow : Window
{
    private const int WmHotKey = 0x0312;
    private const int WmClipboardUpdate = 0x031D;
    private const int ToggleHotkeyId = 0x4242;

    private readonly ObservableCollection<ClipboardItem> _history = new();
    private readonly UpdateService _updateService = new();
    private readonly DispatcherTimer _autoHideTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    private AppSettings _settings;
    private GlobalHotKeyManager? _hotKeyManager;
    private KeyboardHookManager? _keyboardHook;
    private HwndSource? _hwndSource;
    private Forms.NotifyIcon? _trayIcon;
    private Drawing.Icon? _trayAppIcon;
    private Drawing.Font? _trayMenuFont;

    private HotkeyDefinition _activationHotkey;
    private Key _scrollUpKey;
    private Key _scrollDownKey;
    private Key _selectKey;
    private bool _activationUsesLowLevelHook;

    private bool _isShuttingDown;
    private bool _updateBusy;
    private bool _suppressNextClipboardCapture;
    private string? _suppressedClipboardText;
    private DateTimeOffset _lastOverlayInteractionUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastActivationToggleUtc = DateTimeOffset.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();
        _activationHotkey = ParseActivationHotkey(_settings.ActivationHotkey);
        _scrollUpKey = ParseSingleKey(_settings.ScrollUpKey, Key.Up);
        _scrollDownKey = ParseSingleKey(_settings.ScrollDownKey, Key.Down);
        _selectKey = ParseSingleKey(_settings.SelectKey, Key.Enter);

        foreach (var item in ClipboardHistoryStore.Load(_settings.RetainedItemsLimit))
        {
            _history.Add(item);
        }

        HistoryList.ItemsSource = _history;

        LoadSettingsIntoUi();
        LoadUpdateSettingsIntoUi();

        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _autoHideTimer.Start();

        PreviewMouseDown += (_, _) => RegisterOverlayInteraction();
        PreviewMouseMove += (_, _) => RegisterOverlayInteraction();
        PreviewKeyDown += (_, _) => RegisterOverlayInteraction();

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        AddClipboardFormatListener(handle);

        _hotKeyManager = new GlobalHotKeyManager(handle);
        RegisterActivationHotkey();

        _keyboardHook = new KeyboardHookManager
        {
            KeyDownHandler = HandleGlobalKeyDown
        };

        ApplyWindowIcon();
        InitializeTrayIcon();
        PositionWindow();
        HideOverlay();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isShuttingDown)
        {
            e.Cancel = true;
            HideOverlay();
            return;
        }

        CleanupNativeResources();
    }

    private void CleanupNativeResources()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(handle);
        }

        _hotKeyManager?.Unregister(ToggleHotkeyId);
        _keyboardHook?.Dispose();
        _hwndSource?.RemoveHook(WndProc);

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayAppIcon?.Dispose();
        _trayAppIcon = null;

        _trayMenuFont?.Dispose();
        _trayMenuFont = null;
    }

    private void InitializeTrayIcon()
    {
        _trayAppIcon = LoadAppIcon();
        _trayMenuFont = new Drawing.Font("Bahnschrift", 9.0f, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Point);
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayAppIcon ?? Drawing.SystemIcons.Application,
            Text = "Membrain",
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = _trayMenuFont,
            Renderer = new Forms.ToolStripProfessionalRenderer(new TrayMenuColorTable()),
            BackColor = Drawing.Color.FromArgb(12, 20, 28),
            ForeColor = Drawing.Color.FromArgb(237, 243, 248)
        };

        var toggleItem = new Forms.ToolStripMenuItem("Toggle")
        {
            ForeColor = Drawing.Color.FromArgb(237, 243, 248),
            BackColor = Drawing.Color.Transparent
        };
        toggleItem.Click += (_, _) => Dispatcher.Invoke(ToggleOverlay);

        var exitItem = new Forms.ToolStripMenuItem("Exit")
        {
            ForeColor = Drawing.Color.FromArgb(237, 243, 248),
            BackColor = Drawing.Color.Transparent
        };
        exitItem.Click += (_, _) => Dispatcher.Invoke(ExitApplication);

        menu.Items.Add(toggleItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleOverlay);
    }

    private string GetAppIconPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "assets", "app.ico");
    }

    private Drawing.Icon? LoadAppIcon()
    {
        var iconPath = GetAppIconPath();
        if (!File.Exists(iconPath))
        {
            return null;
        }

        try
        {
            return new Drawing.Icon(iconPath);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyWindowIcon()
    {
        var iconPath = GetAppIconPath();
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }
        catch
        {
        }
    }

    private void ExitApplication()
    {
        _isShuttingDown = true;
        Close();
    }

    private bool HandleGlobalKeyDown(int virtualKey)
    {
        var key = KeyInterop.KeyFromVirtualKey(virtualKey);
        if (_activationUsesLowLevelHook && IsActivationHotkeyPressed(key))
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastActivationToggleUtc >= TimeSpan.FromMilliseconds(350))
            {
                _lastActivationToggleUtc = now;
                Dispatcher.Invoke(ToggleOverlay);
            }
            return true;
        }

        if (!IsVisible || SettingsPanel.Visibility == Visibility.Visible)
        {
            return false;
        }

        if (key == _scrollUpKey)
        {
            Dispatcher.Invoke(SelectPreviousItem);
            return true;
        }

        if (key == _scrollDownKey)
        {
            Dispatcher.Invoke(SelectNextItem);
            return true;
        }

        if (key == _selectKey)
        {
            Dispatcher.Invoke(CopySelectedToClipboard);
            return true;
        }

        if (key == Key.Escape)
        {
            Dispatcher.Invoke(HideOverlay);
            return true;
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WmHotKey when wParam.ToInt32() == ToggleHotkeyId:
                ToggleOverlay();
                handled = true;
                break;

            case WmClipboardUpdate:
                CaptureClipboardText();
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private void RegisterActivationHotkey()
    {
        if (_hotKeyManager == null)
        {
            return;
        }

        _hotKeyManager.Unregister(ToggleHotkeyId);
        _activationUsesLowLevelHook = _activationHotkey.Modifiers.HasFlag(HotkeyModifiers.CapsLock);
        if (_activationUsesLowLevelHook)
        {
            return;
        }

        var registered = _hotKeyManager.Register(ToggleHotkeyId, _activationHotkey);
        if (!registered)
        {
            SetStatus("Hotkey unavailable. Pick another combination.");
        }
    }

    private bool IsActivationHotkeyPressed(Key key)
    {
        if (key != _activationHotkey.Key)
        {
            return false;
        }

        if (_activationHotkey.Modifiers.HasFlag(HotkeyModifiers.Ctrl) && !IsAnyKeyDown(0x11))
        {
            return false;
        }

        if (_activationHotkey.Modifiers.HasFlag(HotkeyModifiers.Alt) && !IsAnyKeyDown(0x12))
        {
            return false;
        }

        if (_activationHotkey.Modifiers.HasFlag(HotkeyModifiers.Shift) && !IsAnyKeyDown(0x10))
        {
            return false;
        }

        if (_activationHotkey.Modifiers.HasFlag(HotkeyModifiers.Win) && !IsWinPressed())
        {
            return false;
        }

        if (_activationHotkey.Modifiers.HasFlag(HotkeyModifiers.CapsLock) && !IsCapsLockOn())
        {
            return false;
        }

        return true;
    }

    private static bool IsAnyKeyDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static bool IsWinPressed()
    {
        return (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
    }

    private static bool IsCapsLockOn()
    {
        return (GetKeyState(0x14) & 0x0001) == 0x0001;
    }

    private void CaptureClipboardText()
    {
        try
        {
            if (!WpfClipboard.ContainsText())
            {
                return;
            }

            var text = WpfClipboard.GetText(WpfTextDataFormat.UnicodeText).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (_suppressNextClipboardCapture && string.Equals(text, _suppressedClipboardText, StringComparison.Ordinal))
            {
                _suppressNextClipboardCapture = false;
                _suppressedClipboardText = null;
                return;
            }

            if (_history.Count > 0 && string.Equals(_history[0].Text, text, StringComparison.Ordinal))
            {
                return;
            }

            _history.Insert(0, new ClipboardItem
            {
                Text = text,
                CapturedAtUtc = DateTimeOffset.UtcNow
            });

            TrimHistoryAndPersist();
        }
        catch
        {
            // Another app can lock clipboard briefly.
        }
    }

    private void TrimHistoryAndPersist()
    {
        var limit = Math.Clamp(_settings.RetainedItemsLimit, 1, 500);
        while (_history.Count > limit)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        ClipboardHistoryStore.Save(_history);
    }

    private void ToggleOverlay()
    {
        if (IsVisible)
        {
            HideOverlay();
        }
        else
        {
            ShowOverlay();
        }
    }

    private void ShowOverlay()
    {
        PositionWindow();
        Show();
        Activate();
        Focus();
        RegisterOverlayInteraction();

        if (_history.Count > 0)
        {
            HistoryList.SelectedIndex = 0;
            HistoryList.ScrollIntoView(HistoryList.SelectedItem);
        }
    }

    private void HideOverlay()
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        Hide();
    }

    private void RegisterOverlayInteraction()
    {
        _lastOverlayInteractionUtc = DateTimeOffset.UtcNow;
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsVisible || SettingsPanel.Visibility == Visibility.Visible)
        {
            return;
        }

        var timeout = Math.Clamp(_settings.AutoHideSeconds, 1, 3600);
        if (DateTimeOffset.UtcNow - _lastOverlayInteractionUtc >= TimeSpan.FromSeconds(timeout))
        {
            HideOverlay();
        }
    }

    private void PositionWindow()
    {
        var cursor = Forms.Cursor.Position;
        var screen = Forms.Screen.FromPoint(cursor);
        var bounds = screen.Bounds;

        Height = bounds.Height;
        Top = bounds.Top;

        Left = _settings.ScreenSide == ScreenSide.Left
            ? bounds.Left
            : bounds.Right - Width;
    }

    private void SelectPreviousItem()
    {
        if (_history.Count == 0)
        {
            return;
        }

        var current = HistoryList.SelectedIndex;
        if (current <= 0)
        {
            current = 0;
        }
        else
        {
            current -= 1;
        }

        HistoryList.SelectedIndex = current;
        HistoryList.ScrollIntoView(HistoryList.SelectedItem);
        RegisterOverlayInteraction();
    }

    private void SelectNextItem()
    {
        if (_history.Count == 0)
        {
            return;
        }

        var current = HistoryList.SelectedIndex;
        if (current < 0)
        {
            current = 0;
        }
        else
        {
            current = Math.Min(_history.Count - 1, current + 1);
        }

        HistoryList.SelectedIndex = current;
        HistoryList.ScrollIntoView(HistoryList.SelectedItem);
        RegisterOverlayInteraction();
    }

    private void CopySelectedToClipboard()
    {
        if (HistoryList.SelectedItem is not ClipboardItem item)
        {
            return;
        }

        _suppressNextClipboardCapture = true;
        _suppressedClipboardText = item.Text;
        WpfClipboard.SetText(item.Text);
        SetStatus("Copied selected entry.");
        HideOverlay();
    }

    private void LoadSettingsIntoUi()
    {
        ActivationHotkeyTextBox.Text = _settings.ActivationHotkey;
        ScrollUpKeyTextBox.Text = _settings.ScrollUpKey;
        ScrollDownKeyTextBox.Text = _settings.ScrollDownKey;
        SelectKeyTextBox.Text = _settings.SelectKey;
        RetainedItemsLimitTextBox.Text = _settings.RetainedItemsLimit.ToString();
        AutoHideSecondsTextBox.Text = _settings.AutoHideSeconds.ToString();

        foreach (var item in SideComboBox.Items)
        {
            if (item is ComboBoxItem combo &&
                combo.Content is string value &&
                value.Equals(_settings.ScreenSide.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SideComboBox.SelectedItem = combo;
                break;
            }
        }

        SideComboBox.SelectedIndex = SideComboBox.SelectedIndex < 0 ? 0 : SideComboBox.SelectedIndex;
    }

    private void LoadUpdateSettingsIntoUi()
    {
        var updateSettings = _updateService.Settings;
        UpdateRepoTextBox.Text = updateSettings.RepoUrl ?? string.Empty;
        IncludePrereleaseCheckBox.IsChecked = updateSettings.IncludePrerelease;
    }

    private void ApplyUpdateSettingsFromUi()
    {
        var token = UpdateTokenBox.Password.Trim();
        var existingToken = _updateService.Settings.Token;
        var effectiveToken = string.IsNullOrWhiteSpace(token) ? existingToken : token;

        _updateService.UpdateSettings(new UpdateSettings
        {
            RepoUrl = string.IsNullOrWhiteSpace(UpdateRepoTextBox.Text)
                ? null
                : UpdateRepoTextBox.Text.Trim(),
            Token = effectiveToken,
            IncludePrerelease = IncludePrereleaseCheckBox.IsChecked == true
        });

        UpdateTokenBox.Clear();
    }

    private HotkeyDefinition ParseActivationHotkey(string value)
    {
        if (HotkeyDefinition.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return new HotkeyDefinition(HotkeyModifiers.CapsLock, Key.D);
    }

    private static Key ParseSingleKey(string value, Key fallback)
    {
        if (KeyParser.TryParseSingleKey(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private void SetStatus(string text)
    {
        StatusTextBlock.Text = text;
    }

    private void SetUpdateStatus(string text)
    {
        UpdateStatusTextBlock.Text = text;
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
        RegisterOverlayInteraction();
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var activation = ActivationHotkeyTextBox.Text.Trim();
        if (!HotkeyDefinition.TryParse(activation, out var newHotkey))
        {
            SetStatus("Invalid activation hotkey. Example: CapsLock+D");
            return;
        }

        if (!KeyParser.TryParseSingleKey(ScrollUpKeyTextBox.Text.Trim(), out var scrollUp))
        {
            SetStatus("Invalid scroll up key.");
            return;
        }

        if (!KeyParser.TryParseSingleKey(ScrollDownKeyTextBox.Text.Trim(), out var scrollDown))
        {
            SetStatus("Invalid scroll down key.");
            return;
        }

        if (!KeyParser.TryParseSingleKey(SelectKeyTextBox.Text.Trim(), out var selectKey))
        {
            SetStatus("Invalid select key.");
            return;
        }

        if (!int.TryParse(RetainedItemsLimitTextBox.Text.Trim(), out var retained))
        {
            SetStatus("Retained items must be a number.");
            return;
        }

        retained = Math.Clamp(retained, 1, 500);

        if (!int.TryParse(AutoHideSecondsTextBox.Text.Trim(), out var autoHideSeconds))
        {
            SetStatus("Auto-hide seconds must be a number.");
            return;
        }

        autoHideSeconds = Math.Clamp(autoHideSeconds, 1, 3600);

        var side = SideComboBox.SelectedItem is ComboBoxItem selected &&
                   selected.Content is string sideValue &&
                   sideValue.Equals("Right", StringComparison.OrdinalIgnoreCase)
            ? ScreenSide.Right
            : ScreenSide.Left;

        _settings = new AppSettings
        {
            ActivationHotkey = newHotkey.ToString(),
            ScreenSide = side,
            ScrollUpKey = scrollUp.ToString(),
            ScrollDownKey = scrollDown.ToString(),
            SelectKey = selectKey.ToString(),
            RetainedItemsLimit = retained,
            AutoHideSeconds = autoHideSeconds
        };

        SettingsStore.Save(_settings);

        _activationHotkey = newHotkey;
        _scrollUpKey = scrollUp;
        _scrollDownKey = scrollDown;
        _selectKey = selectKey;

        RegisterActivationHotkey();
        PositionWindow();
        TrimHistoryAndPersist();
        ApplyUpdateSettingsFromUi();
        RegisterOverlayInteraction();
        SetStatus($"Saved settings. Retaining {retained} items. Auto-hide: {autoHideSeconds}s.");
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateBusy)
        {
            return;
        }

        ApplyUpdateSettingsFromUi();

        if (!_updateService.IsConfigured || _updateService.Manager == null)
        {
            SetUpdateStatus("Updates are not configured.");
            return;
        }

        _updateBusy = true;
        CheckUpdatesButton.IsEnabled = false;

        try
        {
            var manager = _updateService.Manager;
            if (!manager.IsInstalled)
            {
                SetUpdateStatus("Install via Setup.exe to enable updates.");
                return;
            }

            var pending = manager.UpdatePendingRestart;
            if (pending != null)
            {
                SetUpdateStatus("Update is ready. Restart to apply.");
                var restart = WpfMessageBox.Show(this, "An update is ready. Restart now?", "Membrain Updates", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (restart == MessageBoxResult.Yes)
                {
                    manager.ApplyUpdatesAndRestart(pending, Array.Empty<string>());
                }
                return;
            }

            SetUpdateStatus("Checking for updates...");
            var updateInfo = await manager.CheckForUpdatesAsync();
            if (updateInfo == null)
            {
                SetUpdateStatus("Up to date.");
                return;
            }

            var version = updateInfo.TargetFullRelease.Version.ToString();
            var confirm = WpfMessageBox.Show(this, $"Update {version} is available. Install now?", "Membrain Updates", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                SetUpdateStatus($"Update available: {version}");
                return;
            }

            SetUpdateStatus("Downloading update...");
            await manager.DownloadUpdatesAsync(updateInfo, progress =>
            {
                Dispatcher.Invoke(() =>
                {
                    SetUpdateStatus($"Downloading update... {progress}%");
                });
            }, CancellationToken.None);

            SetUpdateStatus("Restarting to apply update...");
            manager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            SetUpdateStatus($"Update check failed: {ex.Message}");
        }
        finally
        {
            _updateBusy = false;
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void ClipboardCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { DataContext: ClipboardItem item })
        {
            HistoryList.SelectedItem = item;
            RegisterOverlayInteraction();
            CopySelectedToClipboard();
        }
    }

    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopySelectedToClipboard();
    }

    private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
    {
        CopySelectedToClipboard();
    }

    private void HistoryList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            SelectPreviousItem();
        }
        else if (e.Delta < 0)
        {
            SelectNextItem();
        }

        e.Handled = true;
        RegisterOverlayInteraction();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    private sealed class TrayMenuColorTable : Forms.ProfessionalColorTable
    {
        public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.FromArgb(12, 20, 28);
        public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.FromArgb(12, 20, 28);
        public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.FromArgb(12, 20, 28);
        public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.FromArgb(12, 20, 28);
        public override Drawing.Color MenuBorder => Drawing.Color.FromArgb(48, 69, 88);
        public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(51, 80, 106);
        public override Drawing.Color MenuItemSelected => Drawing.Color.FromArgb(26, 42, 55);
        public override Drawing.Color MenuItemSelectedGradientBegin => Drawing.Color.FromArgb(26, 42, 55);
        public override Drawing.Color MenuItemSelectedGradientEnd => Drawing.Color.FromArgb(26, 42, 55);
        public override Drawing.Color MenuItemPressedGradientBegin => Drawing.Color.FromArgb(17, 28, 37);
        public override Drawing.Color MenuItemPressedGradientMiddle => Drawing.Color.FromArgb(17, 28, 37);
        public override Drawing.Color MenuItemPressedGradientEnd => Drawing.Color.FromArgb(17, 28, 37);
        public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(48, 69, 88);
        public override Drawing.Color SeparatorLight => Drawing.Color.FromArgb(48, 69, 88);
    }
}
