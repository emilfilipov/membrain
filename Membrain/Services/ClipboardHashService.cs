using System.Security.Cryptography;
using System.Text;

namespace Membrain.Services;

public static class ClipboardHashService
{
    public static string ComputeTextHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return "text:" + ComputeHexSha256(bytes);
    }

    public static string ComputeImageHash(byte[] bytes)
    {
        return "image:" + ComputeHexSha256(bytes);
    }

    private static string ComputeHexSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
