using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ahsoka.Utility;

internal class ECDsaEncryption
{
    private static readonly ECCurve defaultCurve = ECCurve.NamedCurves.nistP256;

    public static KeyValuePair<string, string> GenerateNewKeyPair()
    {
        ECDsa ecdsa = GetECDsaProvider();

        // Create service provider instance with key length and return the private and public keys
        return new KeyValuePair<string, string>(ecdsa.ExportPkcs8PrivateKeyPem(), ecdsa.ExportSubjectPublicKeyInfoPem());
    }

    private static ECDsa GetECDsaProvider()
    {
        return ECDsa.Create(defaultCurve);
    }

    public static byte[] GetSignatureWithKey(byte[] fingerprintBytes, string privateKey)
    {
        ECDsa ecdsa = GetECDsaProvider();
        ecdsa.ImportFromPem(privateKey);
        return ecdsa.SignData(fingerprintBytes, HashAlgorithmName.SHA512);
    }

    public static bool VerifyDataSignature(byte[] signatureBytes, byte[] dataBytes, string publieKey)
    {
        ECDsa ecdsa = GetECDsaProvider();
        ecdsa.ImportFromPem(publieKey);
        return ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA512);
    }

}
