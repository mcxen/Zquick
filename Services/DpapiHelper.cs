using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Zquick.Services;

public static class DpapiHelper
{
    private const int CryptProtectUiForbidden = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string ProtectString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return "";
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        var inBlob = new DataBlob();
        var outBlob = new DataBlob();

        try
        {
            inBlob.cbData = plainBytes.Length;
            inBlob.pbData = Marshal.AllocHGlobal(plainBytes.Length);
            Marshal.Copy(plainBytes, 0, inBlob.pbData, plainBytes.Length);

            if (!CryptProtectData(
                    ref inBlob,
                    "Zquick",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out outBlob))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DPAPI 加密失败");
            }

            byte[] encrypted = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, encrypted, 0, outBlob.cbData);
            return Convert.ToBase64String(encrypted);
        }
        finally
        {
            if (inBlob.pbData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(inBlob.pbData);
            }

            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    public static string UnprotectString(string protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return "";
        }

        byte[] cipherBytes = Convert.FromBase64String(protectedBase64);
        var inBlob = new DataBlob();
        var outBlob = new DataBlob();

        try
        {
            inBlob.cbData = cipherBytes.Length;
            inBlob.pbData = Marshal.AllocHGlobal(cipherBytes.Length);
            Marshal.Copy(cipherBytes, 0, inBlob.pbData, cipherBytes.Length);

            if (!CryptUnprotectData(
                    ref inBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out outBlob))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DPAPI 解密失败");
            }

            byte[] plainBytes = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, plainBytes, 0, outBlob.cbData);
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            if (inBlob.pbData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(inBlob.pbData);
            }

            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }
}
