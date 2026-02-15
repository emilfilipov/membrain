using System.Runtime.InteropServices;
using System.Text;

namespace Membrain.Services;

public static class CredentialStore
{
    private const uint CredTypeGeneric = 1;
    private const uint CredPersistLocalMachine = 2;
    private const string TargetName = "Membrain.UpdateToken";

    public static void SaveToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            DeleteToken();
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(token);
        var blobPtr = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blobPtr, bytes.Length);

            var cred = new NativeCredentialWrite
            {
                Type = CredTypeGeneric,
                TargetName = TargetName,
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blobPtr,
                Persist = CredPersistLocalMachine,
                UserName = Environment.UserName
            };

            if (!NativeMethods.CredWrite(ref cred, 0))
            {
                throw new InvalidOperationException("Failed to save token to Windows Credential Manager.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }
    }

    public static string? LoadToken()
    {
        if (!NativeMethods.CredRead(TargetName, CredTypeGeneric, 0, out var credPtr))
        {
            return null;
        }

        try
        {
            var cred = Marshal.PtrToStructure<NativeCredentialRead>(credPtr);
            if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize == 0)
            {
                return null;
            }

            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, (int)cred.CredentialBlobSize);
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            NativeMethods.CredFree(credPtr);
        }
    }

    public static void DeleteToken()
    {
        NativeMethods.CredDelete(TargetName, CredTypeGeneric, 0);
    }

    private static class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredWrite([In] ref NativeCredentialWrite userCredential, uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern void CredFree([In] IntPtr cred);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredentialWrite
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCredentialRead
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }
}
