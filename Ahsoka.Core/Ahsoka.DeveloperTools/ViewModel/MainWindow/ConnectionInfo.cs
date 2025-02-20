using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Installer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Ahsoka.DeveloperTools;

internal partial class ConnectionInfo : ChildViewModelBase<ConnectionsViewModel>, ITargetConnectionInfo
{
    #region Fields
    string description;
    string qualifier;
    string hostName;
    int port;
    int securityPort;
    bool isFavorite;
    string userName;
    string privateKey;
    string privateAppKey;
    string apiKey;
    PlatformFamily family;
    #endregion

    #region Properties
    public bool IsFavorite
    {
        get { return isFavorite; }
        set
        {
            isFavorite = value;
            OnPropertyChanged();
        }
    }

    public string PlatformDescription
    {
        get
        {
            var hardwareInfo = HardwareInfo.GetHardwareInfo(PlatformFamily, PlatformQualifier);

            return hardwareInfo?.PlatformDescription;
        }
        set
        {
            if (value == null)
                return;

            var hardwareInfo = HardwareInfo.GetHardwareInfoByDescription(value);
            if (hardwareInfo != null)
            {
                PlatformFamily = hardwareInfo.PlatformFamily;
                PlatformQualifier = hardwareInfo.PlatformQualifier;
            }
            OnPropertyChanged();
        }
    }

    public List<String> SshKeys
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            List<string> returnList = new List<string>() { "(None)" };

            if (Path.Exists(folder))
                returnList.AddRange(Directory.GetFiles(folder, "*.pub", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension));
            return returnList;
        }
        set { }
    }

    public List<String> SigningKeys
    {
        get
        {
            string path = Path.GetDirectoryName(PackageInformation.GetPackageCertPath("Test", KeyTypes.PackageSigningKey));
            List<string> returnList = new List<string>() { "(None)" };

            if (Path.Exists(path))
                returnList.AddRange(Directory.GetFiles(path, "*.appcert").Select(Path.GetFileNameWithoutExtension));

            return returnList;
        }
        set { }
    }

    public PlatformFamily PlatformFamily
    {
        get { return family; }
        set
        {
            if (value != PlatformFamily.Any)
            {
                family = value;
            }
        }
    }

    public string Description
    {
        get { return description; }
        set
        {
            description = value;
            OnPropertyChanged();
        }
    }

    public string PlatformQualifier
    {
        get { return qualifier; }
        set
        {
            qualifier = value;
        }
    }

    public string HostName
    {
        get { return hostName; }
        set
        {
            hostName = value;
            OnPropertyChanged();

            needsReconnect = true;
        }
    }

    public int Port
    {
        get;
        set;
    }

    public int SystemServicePort
    {
        get { return port; }
        set
        {
            port = value;
            OnPropertyChanged();
            needsReconnect = true;
        }
    }

    public int SecurityServicePort
    {
        get { return securityPort; }
        set
        {
            securityPort = value;
            OnPropertyChanged();
            needsReconnect = true;
        }
    }

    public string UserName
    {
        get { return userName; }
        set
        {
            userName = value;
            OnPropertyChanged();
        }
    }

    public string PrivateKeyFile
    {
        get
        {
            string pathToKeys = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            string pathToKey = Path.Combine(pathToKeys, PrivateKey);
            if (File.Exists(pathToKey))
                return pathToKey;
            else
            {
                pathToKey = Path.Combine(pathToKeys, "Ahsoka", PrivateKey);
                if (File.Exists(pathToKey))
                    return pathToKey;
            }
            return string.Empty;
        }

    }

    public string PrivateKey
    {
        get
        {
            if (SshKeys.Contains(privateKey))
                return privateKey;
            else
                return "(None)"; ;
        }
        set
        {
            privateKey = value;
            OnPropertyChanged();
        }
    }

    public string PrivateAppKeyFile
    {
        get
        {
            string path = PackageInformation.GetPackageCertPath(PrivateAppKey, KeyTypes.PackageSigningKey);
            if (File.Exists(path))
                return path;

            return string.Empty;
        }

    }

    public string PrivateAppKey
    {
        get
        {
            if (SigningKeys.Contains(privateAppKey))
                return privateAppKey;
            else
                return "(None)";
        }
        set
        {
            privateAppKey = value;
            OnPropertyChanged();
        }
    }

    public string APIKey
    {
        get
        {
            return apiKey;
        }
        set
        {
            apiKey = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionAddress
    {
        get
        {
            return $"tcp://{HostName}:{SystemServicePort}";
        }
    }

    public override string ToString()
    {
        return Description.ToString();
    }

    public void FavoriteChanged()
    {
        this.IsFavorite = !IsFavorite;
    }
    #endregion

    #region  fields
    public ConnectionInfo(ConnectionsViewModel parentViewModel) : base(parentViewModel)
    {

    }
    #endregion
}
