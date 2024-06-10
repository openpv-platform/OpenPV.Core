#pragma warning disable CS1591
using Ahsoka.Utility;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Ahsoka.Installer;

/// <summary>
///  This is the Object we use to READ packages
/// </summary>
public class PackageContents : PackageHeader
{
    public byte[] SignatureData { get; internal set; }
    public bool IsSignatureValid { get; internal set; }
    public UInt32 HeaderCRC { get; internal set; }

    internal Stream ContentsStream { get; set; }
    internal string PrivateKey { get; set; }

    [JsonConstructor]
    public PackageContents()
    {

    }

    public static PackageContents ReadPackageContents(Stream packageStream, string privateKey)
    {
        using BinaryReader reader = new(packageStream, Encoding.UTF8, true);

        int headerSize = reader.ReadInt32();

        var headerData = new MemoryStream(reader.ReadBytes(headerSize));

        // Decode Header and Translate into Contents Object.
        PackageHeader header = ProtoBuf.Serializer.Deserialize<PackageHeader>(headerData);
        PackageContents information = JsonUtility.Deserialize<PackageContents>(JsonUtility.Serialize(header));

        information.PrivateKey = privateKey;

        if (information.PrivateKey != null)
        {
            if (header.SecurityModelNumberData?.Length > 0)
                information.SecurityModelNumber = UTF8Encoding.UTF8.GetString(RSAEncryption.DecryptWithPEMKey(header.SecurityModelNumberData, privateKey));

            if (header.SecurityPassKeyData?.Length > 0)
                information.SecurityPassKey = UTF8Encoding.UTF8.GetString(RSAEncryption.DecryptWithPEMKey(header.SecurityPassKeyData, privateKey));

            if (header.SecuritySerialNumberData?.Length > 0)
                information.SecuritySerialNumber = UTF8Encoding.UTF8.GetString(RSAEncryption.DecryptWithPEMKey(header.SecuritySerialNumberData, privateKey));
        }

        information.ContentsStream = packageStream;
        information.SignatureData = reader.ReadBytes(256);

        long offset = packageStream.Position;
        foreach (var item in information.Components)
        {
            item.DataFileOffset = offset;
            offset += item.DataSize;
        }

        // Read Signature
        byte[] decryptedSignature = RSAEncryption.DecryptWithPEMKey(information.SignatureData, privateKey);

        information.HeaderCRC = BitConverter.ToUInt32(decryptedSignature, 0);
        information.IsSignatureValid = information.HeaderCRC == Crc32.CalculateCrc32(headerData);

        return information;
    }

}

