using Ahsoka.Core.Utility;
using EnovationCloud.Core.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Core.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class SparkCommands
{
    [CommandLineMethod(@"     --CreateSparkManifest: 
            [WorkingDirectory] Folder To Process - Will Create a ReleaseName.release.json file
            [ReleaseName] Name of Release
            [Version] Optional Version of Release")]
    private static void CreateSparkManifest(string workingDirectory, string releaseName, string version)
    {
        SparkTools tool = new();
        tool.CreateManifest(workingDirectory, releaseName, version);
    }

    [CommandLineMethod(@"     --CreateSparkSubscription:Subscribe the Display to the Spark Application
            [TargetConnectionInfoFile] - Connection Info File (JSON)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)
            [SparkSubscriptionRequest] - Spark Subscription Info File (JSON).")]
    private static void CreateSparkSubscription(string targetConnectionInfoFile,
            string sparkConnectionInfoFile,
            string sparkSubscriptionInfoFile)
    {
        // Send Deprecated Command.
        Console.WriteLine(@"The --CreateSparkSubscription Command is no longer supported.  Use '--ProvisionDisplay' instead.");
    }

    [CommandLineMethod(@"     --UploadToSpark: 
            [WorkingDirectory] Uploads Application Package to Spark - Should include 1 or more *.release.json files
            [Url] Spark URL to Upload To.
            [Login] Spark Login.
            [Password] Spark Password.
            [Version] Override the Version in Release Files with this version.")]
    private static void UploadToSpark(string workingDirectory, string url, string login, string password, string version = null)
    {
        var uploadTool = new SparkTools();
        uploadTool.Upload(url, login, password, workingDirectory, version);
    }

    [CommandLineMethod(@"     --UploadToSparkWithInfo: 
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON).
            [WorkingDirectory] Uploads Application Package to Spark - Should include 1 or more *.release.json files.
            [Version] Override the Version in Release Files with this version.")]
    private static void UploadToSparkWithInfo(string sparkConnectionInfoFile, string workingDirectory, string version = null)
    {
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);

        var uploadTool = new SparkTools();
        uploadTool.Upload(sparkInfo.Url, sparkInfo.Login, sparkInfo.Password, workingDirectory, version);
    }

    [CommandLineMethod(@"     --DownloadFromSpark: 
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON).
            [DownloadFolder] Folder to place downloads. All Files in the Manifest will be downloaded to the folder.
            [ChannelName] Release Name to Download.
            [ChannelVersion] Version to Download.
            [FileName] File to Download (leave empty to download all items).")]
    private static void DownloadFromSpark(string sparkConnectionInfoFile, string downloadFolder, string channelName, string channelVersion, string fileName = null)
    {
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);

        var uploadTool = new SparkTools();
        uploadTool.Download(sparkInfo.Url, sparkInfo.Login, sparkInfo.Password, downloadFolder, channelName, channelVersion, fileName);
    }
}