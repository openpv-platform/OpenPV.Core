using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ahsoka.Utility;


internal class AesEncryption
{
    public static MemoryStream Encrypt(MemoryStream data, byte[] key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write, true))
            {
                byte[] plainBytes = data.ToArray();
                csEncrypt.Write(plainBytes, 0, plainBytes.Length);
            }

            msEncrypt.Seek(0, SeekOrigin.Begin);
            return msEncrypt;
        }
    }
    public static MemoryStream Decrypt(MemoryStream encryptedData, byte[] key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using (var csDecrypt = new CryptoStream(encryptedData, decryptor, CryptoStreamMode.Read))
            {
                var msPlain = new MemoryStream();
                csDecrypt.CopyTo(msPlain);
                return msPlain;
            }
        }
    }

    /// <summary>
    /// Function for fetching an AES Key Created from the local Device ID. Currently this is not secure
    /// but should be able to be secured once we have HSM's onboard
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    internal static AesKey KeyFromDeviceId(Guid guid)
    {
        List<byte> originalBytes = [.. guid.ToByteArray(), .. guid.ToByteArray().Reverse()];

        byte[] key = new byte[32]; // 256-bit key
        byte[] iv = new byte[16]; // 128-bit IV

        byte[] keyData = originalBytes.ToArray();
        
        Array.Copy(keyData, key, 32);
        Array.Copy(keyData, 8, iv, 0, 8);
        Array.Copy(keyData, 0, iv, 8, 8);

        return new AesKey() { Key = key, IV = iv };
    }

    internal static MemoryStream DecryptFile(string fileToDecrypt, Guid deviceId)
    {
        if (File.Exists(fileToDecrypt))
        {
            var data = File.ReadAllBytes(fileToDecrypt);
            var key = KeyFromDeviceId(deviceId);
            return Decrypt(new MemoryStream(data), key.Key, key.IV);
        }
        return null;
    }

    internal static MemoryStream EncryptFile(string fileToEncrypt, Guid deviceId)
    {
        if (File.Exists(fileToEncrypt))
        {
            var data = File.ReadAllBytes(fileToEncrypt);
            var key = KeyFromDeviceId(deviceId);
            return Encrypt(new MemoryStream(data), key.Key, key.IV);
        }
        return null;
    }

    public class AesKey
    {
        public byte[] Key { get; set; } = new byte[32];
        public byte[] IV { get; set; } = new byte[16];
    }
}