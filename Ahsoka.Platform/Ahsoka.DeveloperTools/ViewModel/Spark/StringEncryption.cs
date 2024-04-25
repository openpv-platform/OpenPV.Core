using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Ahsoka.DeveloperTools.Spark;

public static class StringEncryption
{
    static readonly byte[] entropy = Encoding.Unicode.GetBytes("C931A392-9766-4BC0-B987-D1A33E791339");

    public static string EncryptString(SecureString input)
    {
        byte[] encryptedData;
        IDataProtector protector = CreateProtector();
        encryptedData = protector.Protect(Encoding.Unicode.GetBytes(ToInsecureString(input)));
        return Convert.ToBase64String(encryptedData);
    }

    public static SecureString DecryptString(string encryptedData)
    {
        try
        {
            byte[] decryptedData;
            IDataProtector protector = CreateProtector();
            decryptedData = protector.Unprotect(Convert.FromBase64String(encryptedData));
            return ToSecureString(Encoding.Unicode.GetString(decryptedData));
        }
        catch
        {
            return new SecureString();
        }
    }
    private static IDataProtector CreateProtector()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(appData, "AhsokaData", "SecureData");
        var info = new DirectoryInfo(path);

        var provider = DataProtectionProvider.Create(info, configuration =>
        {
            configuration.SetApplicationName("AhsokaData");
            if (OperatingSystem.IsWindows())
                configuration.ProtectKeysWithDpapi();
        });

        var purpose = Uri.EscapeDataString("AhsokaData_" + Convert.ToBase64String(entropy));
        var protector = provider.CreateProtector(purpose);
        return protector;
    }



    public static void CreateSecureFile(string path, string data)
    {
        byte[] encryptedData;
        var protector = CreateProtector();
        encryptedData = protector.Protect(Encoding.Unicode.GetBytes(data));
        File.WriteAllBytes(path, encryptedData);
    }

    public static SecureString ReadSecureFile(string path)
    {
        try
        {
            byte[] decryptedData;
            var protector = CreateProtector();
            var encryptedData = File.ReadAllBytes(path);
            decryptedData = protector.Unprotect(encryptedData);
            return ToSecureString(Encoding.Unicode.GetString(decryptedData));
        }
        catch
        {
            return new SecureString();
        }
    }

    public static SecureString ToSecureString(string input)
    {
        SecureString secure = new();
        foreach (char c in input)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }

    public static string ToInsecureString(SecureString input)
    {
        string returnValue = string.Empty;
        IntPtr ptr = Marshal.SecureStringToBSTR(input);
        try
        {
            returnValue = Marshal.PtrToStringBSTR(ptr);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(ptr);
        }
        return returnValue;
    }

    public static bool IsEqualTo(this SecureString ss1, SecureString ss2)
    {
        if (ss1 == null || ss2 == null)
            return false;

        IntPtr bstr1 = IntPtr.Zero;
        IntPtr bstr2 = IntPtr.Zero;
        try
        {
            bstr1 = Marshal.SecureStringToBSTR(ss1);
            bstr2 = Marshal.SecureStringToBSTR(ss2);
            int length1 = Marshal.ReadInt32(bstr1, -4);
            int length2 = Marshal.ReadInt32(bstr2, -4);
            if (length1 == length2)
            {
                for (int x = 0; x < length1; ++x)
                {
                    byte b1 = Marshal.ReadByte(bstr1, x);
                    byte b2 = Marshal.ReadByte(bstr2, x);
                    if (b1 != b2) return false;
                }
            }
            else return false;
            return true;
        }
        finally
        {
            if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
            if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
        }
    }
}
