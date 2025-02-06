using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Ahsoka.Services.Network.Platform;

internal class NetworkProfileHelper
{
    static NetworkProfileHelper()
    {

    }

    internal static void AddNetworkProfile(WirelessConnectionRequest connectionRequest)
    {
        // Get Profiles
        var profiles = GetNetworkProfiles(connectionRequest.AdapterId);

        // Add if Not Already There.
        if (!profiles.NetworkProfiles.Any(x => x.Ssid == connectionRequest.Ssid))
        {
            if (!connectionRequest.Encrypted)
            {
                connectionRequest.PassKey = EncryptNetworkPasskey(connectionRequest.PassKey);
                connectionRequest.Encrypted = true;
            }

            // Add to Top
            profiles.NetworkProfiles.Insert(0, connectionRequest);

            // Save Profiles
            SetNetworkProfiles(profiles);
        }
    }

    internal static NetworkProfilesList GetNetworkProfiles(string adapterID)
    {
        string path = GetProfileFilePath(adapterID);

        if (File.Exists(path))
        {
            string fileData = File.ReadAllText(path);
            NetworkProfilesList networkProfilesList = JsonUtility.DeserializeWithTypeInfo(fileData, CoreJsonSerializerContext.Default.NetworkProfilesList);

            if (networkProfilesList != null && networkProfilesList.NetworkProfiles != null)
                return networkProfilesList;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "GetNetworkProfiles: networkProfilesList or networkProfilesList.NetworkProfiles is null...");
        }
        return new NetworkProfilesList() { AdapterId = adapterID };
    }

    internal static void SetNetworkProfiles(NetworkProfilesList profileList)
    {
        // Delete Network Profile Folder and Create New Folder
        string path = GetProfileFilePath(profileList.AdapterId);

        foreach (var profile in profileList.NetworkProfiles)
        {
            if (!profile.Encrypted)
            {
                profile.PassKey = EncryptNetworkPasskey(profile.PassKey);
                profile.Encrypted = true;
            }
        }

        File.WriteAllText(path, JsonUtility.Serialize(profileList));
    }


    private static string GetProfileFilePath(string adapterID)
    {
        string profilePath = SystemInfo.HardwareInfo.TargetPathInfo.GetServiceDataPath(ServiceDataPaths.NetworkProfileFolder);

        string path = Path.Combine(profilePath, adapterID + Path.PathSeparator);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        // Get File 
        path = Path.Combine(path, "NetworkProfiles.json");

        return path;
    }

    internal static string EncryptNetworkPasskey(string passKey)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToByteArray();
        aes.Padding = PaddingMode.ANSIX923;
        using MemoryStream msEncrypt = new();
        using CryptoStream csEncrypt = new(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using (StreamWriter swEncrypt = new(csEncrypt))
        {
            swEncrypt.Write(passKey);
        }
        var encrypted = msEncrypt.ToArray();
        var combined = new byte[aes.IV.Length + encrypted.Length];
        Array.Copy(aes.IV, 0, combined, 0, aes.IV.Length);
        Array.Copy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);
        return Convert.ToBase64String(combined);
    }

    internal static WirelessConnectionRequest DecryptNetwork(WirelessConnectionRequest connectionRequest)
    {
        if (connectionRequest.Encrypted)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = SystemInfo.HardwareInfo.FactoryInfo.DeviceId.ToByteArray();
            aes.Padding = PaddingMode.ANSIX923;

            var bytes = Convert.FromBase64String(connectionRequest.PassKey);
            byte[] IV = new byte[16];
            byte[] passKey = new byte[bytes.Length - IV.Length];
            Array.Copy(bytes, 0, IV, 0, IV.Length);
            Array.Copy(bytes, 16, passKey, 0, bytes.Length - IV.Length);
            aes.IV = IV;

            using MemoryStream msDecrypt = new(passKey);
            using CryptoStream csDecrypt = new(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            connectionRequest.PassKey = srDecrypt.ReadToEnd();
            connectionRequest.Encrypted = false;
        }
        return connectionRequest;
    }
}