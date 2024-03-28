using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.System;
using Ahsoka.Utility;
using System.IO;

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

    public string DeviceKeyName
    {
        get { return ArtifactName.Replace("_DeviceKey", ""); }
    }

    public string ArtifactCreator
    {
        get { return this.Model.ArtifactCreator; }
    }

    public bool IsLocal
    {
        get
        {
            var path = SshUtility.GetKeyFileNames(model.ArtifactName.Replace("_DeviceKey", ""));
            return File.Exists(path.PrivateKeyPath) && File.Exists(path.PublicKeyPath);
        }
    }

    public override string ToString()
    {
        return $"{DeviceKeyName} ({IsLocal})";
    }

}