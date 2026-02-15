using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Membrain.Services;

public sealed class GlobalHotKeyManager
{
    private readonly IntPtr _windowHandle;

    public GlobalHotKeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool Register(int id, HotkeyDefinition hotkey)
    {
        var virtualKey = KeyInterop.VirtualKeyFromKey(hotkey.Key);
        return RegisterHotKey(_windowHandle, id, (uint)hotkey.Modifiers, (uint)virtualKey);
    }

    public void Unregister(int id)
    {
        UnregisterHotKey(_windowHandle, id);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
