using System.Globalization;
using System.Windows.Input;

namespace Membrain.Services;

public static class KeyParser
{
    public static bool TryParseSingleKey(string value, out Key key)
    {
        key = Key.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Enum.TryParse<Key>(value, true, out key) && key != Key.None)
        {
            return true;
        }

        if (value.Length == 1)
        {
            var c = value.ToUpperInvariant()[0];
            if (char.IsLetterOrDigit(c) && Enum.TryParse<Key>(c.ToString(CultureInfo.InvariantCulture), true, out key))
            {
                return true;
            }
        }

        return false;
    }
}
