using System.Globalization;
using System.Windows.Input;

namespace Membrain.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Ctrl = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    CapsLock = 0x0010
}

public readonly record struct HotkeyDefinition(HotkeyModifiers Modifiers, Key Key)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        if (Modifiers.HasFlag(HotkeyModifiers.CapsLock)) parts.Add("CapsLock");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    public static bool TryParse(string value, out HotkeyDefinition hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value
            .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index].ToLowerInvariant();
            switch (token)
            {
                case "ctrl":
                case "control":
                    modifiers |= HotkeyModifiers.Ctrl;
                    break;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "win":
                case "windows":
                case "meta":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                case "capslock":
                case "caps":
                    modifiers |= HotkeyModifiers.CapsLock;
                    break;
                default:
                    return false;
            }
        }

        var keyToken = tokens[^1];
        if (!TryParseKey(keyToken, out var key))
        {
            return false;
        }

        if (modifiers == HotkeyModifiers.None)
        {
            return false;
        }

        hotkey = new HotkeyDefinition(modifiers, key);
        return true;
    }

    private static bool TryParseKey(string token, out Key key)
    {
        key = Key.None;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (Enum.TryParse<Key>(token, true, out key) && key != Key.None)
        {
            return true;
        }

        if (token.Length == 1)
        {
            var c = token.ToUpperInvariant()[0];
            if (char.IsLetterOrDigit(c) && Enum.TryParse<Key>(c.ToString(CultureInfo.InvariantCulture), true, out key))
            {
                return true;
            }
        }

        return false;
    }
}
