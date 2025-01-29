using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.System;

/// <summary>
///  Class used to validate that a License is valid on a 
///  given Target.
/// </summary>
public static class ModuleLicenseManager
{
    static ModuleLicenseList validLicenses;

    /// <summary>
    ///  Validate a License is Valid for this Hardware
    /// </summary>
    /// <param name="licenseID"></param>
    /// <returns></returns>
    public static bool IsLicensed(string licenseID)
    {
        // Init Locally if Not Previously Initialized
        if (validLicenses == null)
            InitWithPublicKey(Properties.Resources.ModuleLicensePublicKeyPEM);

        return validLicenses.Licenses.Any(x => x.LicenseId == licenseID);
    }

    /// <summary>
    ///  Get LicenseID
    /// </summary>
    /// <param name="licenseName"></param>
    /// <returns></returns>
    public static string GetLicenseID(string licenseName)
    {
        // Init Locally if Not Previously Initialized
        if (validLicenses == null)
            InitWithPublicKey(Properties.Resources.ModuleLicensePublicKeyPEM);

        return validLicenses.Licenses.FirstOrDefault(x => x.LicenseName == licenseName)?.LicenseId;
    }

    internal static void InitWithPublicKey(string publieyKeyPEM)
    {
        string licenseFile = SystemInfo.HardwareInfo.TargetPathInfo.GetFactoryPath(FactoryPaths.ModuleLicenseFile);
        if (!File.Exists(licenseFile))
        {
            validLicenses = new ModuleLicenseList() { Licenses = new List<ModuleLicense>() };
            return;
        }

        // Decode Data
        using var data = new MemoryStream(File.ReadAllBytes(licenseFile));
        ModuleLicenseInfo info = ProtoBuf.Serializer.Deserialize<ModuleLicenseInfo>(data);

        // TODO: Change Key to Private Spark Key
        bool isValid = RSAEncryption.VerifyDataSignature(info.Signature, info.LicenseData, publieyKeyPEM);

        if (!isValid)
        {
            validLicenses = new ModuleLicenseList();
            return;
        }

        using var licenseListData = new MemoryStream(info.LicenseData);
        validLicenses = ProtoBuf.Serializer.Deserialize<ModuleLicenseList>(licenseListData);

        // Reject Licenses
        if (validLicenses.DeviceId != SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToString())
            validLicenses = new ModuleLicenseList();
    }

    internal static MemoryStream CreateLicenseFile(string privateKey, string deviceID, Dictionary<string, string> licenseValues)
    {
        // Create Sample License File
        var licenseList = new ModuleLicenseList
        {
            DeviceId = deviceID,
            Licenses = new()
        };

        foreach (var item in licenseValues)
            licenseList.Licenses.Add(new ModuleLicense() { LicenseId = item.Key, LicenseName = item.Value });

        // Serialize, Sign and Save the License Data with Untrusted Key
        var licenseData = new MemoryStream();
        ProtoBuf.Serializer.Serialize(licenseData, licenseList);

        var moduleLicenseInfo = new ModuleLicenseInfo()
        {
            LicenseData = licenseData.ToArray(),
            Signature = RSAEncryption.GetSignatureWithKey(licenseData.ToArray(), privateKey),
            DeviceId = deviceID
        };

        var licenseInfoData = new MemoryStream();
        ProtoBuf.Serializer.Serialize(licenseInfoData, moduleLicenseInfo);
        return licenseInfoData;
    }
}


