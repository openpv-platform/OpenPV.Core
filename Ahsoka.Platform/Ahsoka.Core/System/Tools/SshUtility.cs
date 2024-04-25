using Ahsoka.Services.System;
using Ahsoka.Spark;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.IO;
using System.Text;

namespace Ahsoka.System;

internal class SshUtility
{
    /// <summary>
    /// Connection Funtion for creating a Connection using SSH to a Target
    /// </summary>
    /// <param name="info"></param>
    /// <param name="requireKey"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static SshClient CreateSshClient(IConnectionInfo info, bool requireKey = false)
    {
        PrivateKeyFile privateKeyFile = null;
        if (File.Exists(info.PrivateKeyFile))
            privateKeyFile = new PrivateKeyFile(info.PrivateKeyFile);

        if (requireKey && privateKeyFile == null)
            throw new ApplicationException("A private key file is required when requireKey == true.");

        int port = info.Port > 0 ? info.Port : 22;

        SshClient client;
        if (privateKeyFile != null)
            client = new SshClient(info.HostName, port, info.UserName, privateKeyFile); // Use Key
        else
            client = new SshClient(info.HostName, port, info.UserName, ""); // Use Empty Password

        // Fetch the Public Key
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        client.Connect();
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(5);

        return client;
    }

    /// <summary>
    /// Utility function for creating an SCP Connection to a Target.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="requireKey"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static ScpClient CreateScpClient(IConnectionInfo info, bool requireKey = false)
    {
        PrivateKeyFile privateKeyFile = null;
        if (File.Exists(info.PrivateKeyFile))
            privateKeyFile = new PrivateKeyFile(info.PrivateKeyFile);

        if (requireKey && privateKeyFile != null)
            throw new ApplicationException("A private key file is required when requireKey == true.");

        ScpClient client;
        int port = info.Port > 0 ? info.Port : 22;

        if (privateKeyFile != null)
            client = new(info.HostName, port, info.UserName, privateKeyFile); // Use Key
        else
            client = new(info.HostName, port, info.UserName, ""); // Use Empty Password

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(5);
        client.Connect();

        return client;
    }

    public static string AddSshPublicKey(IConnectionInfo info, KeyFileInfo keyFiles, IProgress<string> progress = null)
    {
        // Connect to Device
        SshClient sshClient = SshUtility.CreateSshClient(info);
        ScpClient scpClient = SshUtility.CreateScpClient(info);

        progress?.Report("Configuring SSH Client On Device");

        string homeFolder = "/home/root/";

        // Verify SSH Folder
        var commandResult = sshClient.RunCommand($"ls {homeFolder}.ssh");
        if (commandResult.Error != string.Empty)
        {
            var sshResult = sshClient.RunCommand($"mkdir -p -m 0644 {homeFolder}.ssh");
            if (sshResult.Error != string.Empty)
                throw new Exception(sshResult.Result);
        }

        string authFilePath = $"{homeFolder}.ssh/authorized_keys";
        string authFile = String.Empty;
        try
        {
            using MemoryStream authKeys = new();
            scpClient.Download(authFilePath, authKeys);
            authFile = UTF8Encoding.UTF8.GetString(authKeys.ToArray());
        }
        catch (ScpException) { } // File Doesn't Exist.


        // Add Our Key to the Auth File
        string publicKey = File.ReadAllText(keyFiles.PublicKeyPath);
        if (!authFile.Contains(publicKey))
        {
            StringBuilder builder = new(authFile);
            builder.Append(publicKey);
            builder.Append('\n');
            using MemoryStream keyFileData = new(UTF8Encoding.UTF8.GetBytes(builder.ToString()));
            scpClient.Upload(keyFileData, authFilePath);
        }

        // Test the Key by logging into the display with out new key
        // ssh - i ~/.ssh / custom_key_name SYSUSER @x.x.x.x
        // Test Login with Key File
        _ = SshUtility.CreateSshClient(info, true);

        // ***WARNING*****
        // Make sure to add ssh public key before configuring drop bear flags. If dropbear is
        // unable to authenticate, ssh loging will be unvailable until a re-flash
        //****************
        // For documentation on Drop bear flags: https://matt.ucc.asn.au/dropbear/dropbear.html
        string DropbearArgs = " -s";    // -s flag disables password logins and forces public key authentication
        _ = sshClient.RunCommand($"echo DROPBEAR_EXTRA_ARGS={DropbearArgs} > /etc/default/dropbear");

        return $"Device Locked to KeyFile {keyFiles.PrivateKeyPath}";
    }

    public static string UnlockDevice(IConnectionInfo info, IProgress<string> progress = null)
    {
        // Connect to Device
        SshClient sshClient = SshUtility.CreateSshClient(info);
        //ScpClient scpClient = RemoteToolsFactory.CreateScpClient(info);

        progress?.Report("Enable Logins on Device");

        // ***WARNING*****
        // Make sure to add ssh public key before configuring drop bear flags. If dropbear is
        // unable to authenticate, ssh loging will be unvailable until a re-flash
        //****************
        // For documentation on Drop bear flags: https://matt.ucc.asn.au/dropbear/dropbear.html
        string DropbearArgs = " -B";    // -B Allows Blank Logins
        _ = sshClient.RunCommand($"echo DROPBEAR_EXTRA_ARGS={DropbearArgs} > /etc/default/dropbear");

        _ = SshUtility.CreateSshClient(info, true);

        return $"Device UnLocked";
    }

    public static KeyFileInfo AddToSshConfiguration(KeyFileInfo keyFiles, string keyDescription, string hostName, string userName)
    {
        // Read Config File
        var configFile = new StringBuilder();
        bool shouldAddNameConfig = true;
        bool shouldAddHostNameConfig = true;
        if (File.Exists(keyFiles.ConfigPath))
        {
            var data = File.ReadAllText(keyFiles.ConfigPath);
            shouldAddNameConfig = !data.Contains($"Host {keyDescription}");
            shouldAddHostNameConfig = !data.Contains($"Host {hostName}");

            configFile.Append(data);
        }

        // Add Config to Login With Device Name
        if (shouldAddNameConfig)
        {
            string configSnippet = Properties.Resources.SSHConfigFile;
            configSnippet = configSnippet.Replace("KEYNAME", keyDescription).Replace("HOSTNAME", hostName).Replace("KEYFILEPATH", keyFiles.PrivateKeyPath).Replace("USERNAME", userName);
            configFile.Insert(0, configSnippet);
        }

        // Add a Second Config for Login with HostName
        if (shouldAddHostNameConfig && hostName != keyDescription)
        {
            string configSnippet = Properties.Resources.SSHConfigFile;
            configSnippet = configSnippet.Replace("KEYNAME", hostName).Replace("HOSTNAME", hostName).Replace("KEYFILEPATH", keyFiles.PrivateKeyPath).Replace("USERNAME", userName);
            configFile.Insert(0, configSnippet);
        }

        if (shouldAddHostNameConfig || shouldAddNameConfig)
            File.WriteAllText(keyFiles.ConfigPath, configFile.ToString());

        return keyFiles;

    }

    public static KeyFileInfo CreateKeyFile(string keyName)
    {
        var keyFiles = GetKeyFileNames(keyName);

        if (!File.Exists(keyFiles.PublicKeyPath))
        {
            // Generate Key Pair
            var keygen = new SshKeyGenerator(2048);

            if (!Directory.Exists(Path.GetDirectoryName(keyFiles.PrivateKeyPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(keyFiles.PrivateKeyPath));

            File.WriteAllText(keyFiles.PrivateKeyPath, keygen.ToPrivateKey());
            File.WriteAllText(keyFiles.PublicKeyPath, keygen.ToRfcPublicKey());
        }

        return keyFiles;
    }

    public static KeyFileInfo GetKeyFileNames(string keyName)
    {
        string sshKeyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh\\Ahsoka");
        string sshFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");

        return new KeyFileInfo()
        {
            PrivateKeyPath = Path.Combine(sshKeyFolder, keyName),
            ConfigPath = Path.Combine(sshFolder, "config"),
            Name = keyName
        };
    }
}
