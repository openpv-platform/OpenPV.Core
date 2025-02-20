using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Ahsoka.Core.Utility;


internal class RSAEncryption
{
    public const int defaultKeyLength = 2048;

    public static KeyValuePair<string, string> GenerateNewKeyPair(bool generateAsPEMKey = true, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);

        // Create service provider instance with key length and return the private and public keys
        if (generateAsPEMKey)
            return new KeyValuePair<string, string>(rsa.ExportRSAPrivateKeyPem(), rsa.ExportRSAPublicKeyPem());
        else
            return new KeyValuePair<string, string>(rsa.ToXmlString(true), rsa.ToXmlString(false));
    }

    private static RSA GetRSAProvider(int keyLength)
    {
        return RSA.Create(keyLength);
    }

    public static byte[] EncryptWithPEMKey(byte[] bytes, string PEMFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(PEMFormattedKey);
        return rsa.Encrypt(bytes, RSAEncryptionPadding.OaepSHA512);
    }

    public static byte[] DecryptWithPEMKey(byte[] bytes, string PEMFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(PEMFormattedKey);
        return rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA512);
    }

    public static byte[] GetSignatureWithKey(byte[] fingerprintBytes, string key, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(key);
        return rsa.SignData(fingerprintBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
    }

    public static bool VerifyDataSignature(byte[] signatureBytes, byte[] dataBytes, string key, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(key);
        return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
    }

    public static string ConvertXMLToPrivatePEM(string xmlFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.FromXmlString(xmlFormattedKey);
        return rsa.ExportRSAPrivateKeyPem();
    }

    public static string ConvertXMLToPublicPEM(string xmlFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.FromXmlString(xmlFormattedKey);
        return rsa.ExportRSAPublicKeyPem();
    }

    public static string ConvertPEMToPrivateXML(string pemFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(pemFormattedKey);
        return rsa.ToXmlString(true);
    }

    public static string ConvertPEMToPublicXML(string pemFormattedKey, int keyLength = defaultKeyLength)
    {
        RSA rsa = GetRSAProvider(keyLength);
        rsa.ImportFromPem(pemFormattedKey);
        return rsa.ToXmlString(false);
    }

    public static string GetChecksum(Stream data)
    {
        using var hash = SHA256.Create();
        var hashedInputBytes = hash.ComputeHash(data);
        return Convert.ToBase64String(hashedInputBytes);
    }
}
