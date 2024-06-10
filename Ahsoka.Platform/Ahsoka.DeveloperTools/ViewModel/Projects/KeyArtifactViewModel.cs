using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.System;
using Ahsoka.Utility;
using System.IO;
using static Ahsoka.Spark.SparkProjectTools;
using Ahsoka.Spark;
using Ahsoka.Installer;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class KeyArtifactViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    private ArtifactDTO model;

    public ArtifactDTO Model { get => model; set => model = value; }

    public KeyArtifactViewModel(SparkProjectViewModel teamVM, ArtifactDTO dto) : base(teamVM)
    {
        this.Model = dto;
    }

    public string ArtifactName
    {
        get { return this.Model.ArtifactName; }
    }

    public KeyTypes ArtifactType
    {
        get { return ArtifactName.Contains(deviceKey) ? KeyTypes.DeviceKey : (ArtifactName.Contains(serviceKey) ? KeyTypes.ServiceKey : KeyTypes.PackageSigningKey); }
    }

    public string DeviceKeyName
    {
        get { return ArtifactName.Replace(deviceKey, "").Replace(appCert, "").Replace(serviceKey,""); }
    }

    public string ArtifactCreator
    {
        get { return this.Model.ArtifactCreator; }
    }

    public bool IsLocal
    {
        get
        {
            if (ArtifactType ==  KeyTypes.DeviceKey)
            {
                var path = SshUtility.GetKeyFileNames(DeviceKeyName);
                return File.Exists(path.PrivateKeyPath) && File.Exists(path.PublicKeyPath);
            }
            else
            {
                var path = PackageInformation.GetPackageCertPath(DeviceKeyName, ArtifactType);
                return File.Exists(path);
            }
        }
    }

    public override string ToString()
    {
        return $"{DeviceKeyName} ({IsLocal})";
    }

}