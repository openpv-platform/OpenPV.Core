using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

internal abstract class SparkToolBase : OpenPVToolBase
{
    readonly ReleaseDTO viewModel;
    readonly bool extractInPlace;

    public SparkToolBase(ReleaseDTO viewModel, bool extractInPlace)
    {
        this.viewModel = viewModel;
        this.extractInPlace = extractInPlace;
    }

    protected override bool GetIsDownloaded()
    {
        bool isDownloaded = extractInPlace ? Directory.Exists(Path.GetDirectoryName(LocalPath)) : File.Exists(LocalPath);
        if (isDownloaded && File.Exists(GetVersionFilePath()))
        {
            string fileVersion = File.ReadAllText(GetVersionFilePath());
            return fileVersion == Version;
        }

        return false;
    }

    protected async Task ExecuteDownloadInternal(IToolViewModel toolVM, string path, CancellationTokenSource cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                ReleaseDTO token = SparkTeamClient.SharedSparkClient.GetDownloadToken(viewModel.ReleaseID, DateTime.Now.AddMinutes(15));
                var contentItem = token.Contents.FirstOrDefault(x => path.EndsWith(x.Path));

                var myDownloadUrl = contentItem.DownloadToken;
                DownloadUtility download = new(myDownloadUrl, path);
                download.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    toolVM.DownloadPercent = (int)progressPercentage;
                };

                await download.StartDownload(cancellationToken);

                if (extractInPlace)
                {
                    string extractPath = Path.GetDirectoryName(LocalPath);

                    ZipFile.ExtractToDirectory(path, extractPath);
                    File.Delete(LocalPath);
                }

                File.WriteAllText(GetVersionFilePath(), Version);

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            if (result.Result != "")
            {
                File.Delete(LocalPath);
            }
        });


    }

}
