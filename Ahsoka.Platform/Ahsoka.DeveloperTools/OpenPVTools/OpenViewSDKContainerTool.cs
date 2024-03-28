namespace Ahsoka.DeveloperTools;

internal class OpenViewSDKContainerTool : DockerBaseTool
{
    public OpenViewSDKContainerTool() :
        base("openpv/openview_sdk_2_0", "Ahsoka.Platform.2.0", true)
    {
        this.IncludedProfiles = PluginsViewModel.profileCPP;
        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = false;
        this.InstallText = "This process will download the 2.0 OpenView Select SDK Container to your Operating System (Requires Docker)";
    }

    protected override string GetLaunchScript(string imageName, string containerName, string workingDirectoryPath)
    {
        return Properties.Resources.docker_run_shell.Replace("$IMAGE_NAME", imageName)
                                                 .Replace("$CONTAINER_NAME", containerName)
                                                 .Replace("$LOCAL_PATH", workingDirectoryPath)
                                                 .Replace("$ENTRY_POINT", "/bin/bash");
    }
}