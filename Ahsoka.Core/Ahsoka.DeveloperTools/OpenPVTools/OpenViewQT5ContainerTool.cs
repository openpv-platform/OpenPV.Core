namespace Ahsoka.DeveloperTools;

internal class OpenViewQT5ContainerTool : DockerBaseTool
{
    public OpenViewQT5ContainerTool() :
        base("openpv/openview_qt_creator_3_0", "Ahsoka.QTCreator.30", false)
    {
        this.IncludedProfiles = PluginsViewModel.profileWSL;

        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = false;
        this.InstallText = "This process will download the OpenView Select QT5 Container to your Operating System (Requires Docker)";
    }

    protected override string GetLaunchScript(string imageName, string containerName, string workingDirectoryPath)
    {
        return Properties.Resources.docker_run_ui.Replace("$IMAGE_NAME", imageName)
                                                 .Replace("$CONTAINER_NAME", containerName)
                                                 .Replace("$LOCAL_PATH", workingDirectoryPath)
                                                 .Replace("$ENTRY_POINT", "./entrypoint.sh");

    }
}
