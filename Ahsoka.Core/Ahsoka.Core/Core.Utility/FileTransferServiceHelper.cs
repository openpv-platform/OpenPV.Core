using Ahsoka.Core.Hardware;
using Ahsoka.Services.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace Ahsoka.Core.Utility;

// Handles a Service Side of a File Transfer 
internal class FileTransferServiceHelper
{
    #region Fields
    readonly IFileTransmit transmitInfo;
    readonly int uploadId;
    readonly FileStream fileStream;
    readonly int maxBytesPerMessage = 128 * 1024;
    const string tempExtension = ".temp";
    readonly Timer timer = new();
    int timeoutInMs;
    DateTime timeoutTime;

    static readonly Dictionary<int, FileTransferServiceHelper> transfersInProgress = new();
    #endregion

    #region Properties
    public IFileTransmit TransmitInfo { get => transmitInfo; }
    #endregion

    #region Constructor and Factory
    private FileTransferServiceHelper(int uploadId, IFileTransmit transmitInfo)
    {
        this.transmitInfo = transmitInfo;
        this.uploadId = uploadId;

        // Open File
        string watcherPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder);
        if (!Directory.Exists(watcherPath))
            Directory.CreateDirectory(watcherPath);

        string filePath = Path.Combine(watcherPath, transmitInfo.FileName + tempExtension);
        this.fileStream = new FileStream(filePath, FileMode.Create);

        // Capture Timeout
        timeoutInMs = transmitInfo.TimeoutInMs;

        // Setup Timeout Timer
        this.timer.Interval = 1000; // Check Timeout every 1s
        this.timer.Enabled = true;
        this.timer.AutoReset = true;

        timer.Elapsed += Timer_Elapsed;
    }

    public static FileTransferServiceHelper CreateFileTransferHelper(IFileTransmit transmitInfo)
    {
        int uploadId = HashCode32.GetHashCode32(transmitInfo.FileName);
        var valueExists = transfersInProgress.ContainsKey(uploadId);
        if (!valueExists)
        {
            // Create transfer object and add it to the tracker.
            var helper = new FileTransferServiceHelper(uploadId, transmitInfo);
            transfersInProgress.Add(uploadId, helper);

            return helper;
        }
        else
            return null;
    }

    internal static bool TryGetFileTransferHelper(int uploadID, out FileTransferServiceHelper transferHelper)
    {
        return transfersInProgress.TryGetValue(uploadID, out transferHelper);
    }
    #endregion

    #region Message Handling Methods
    internal T GetLoadReply<T>() where T : IFileTransmitReply, new()
    {
        // Start Timer for Timeouts
        timer.Start();

        return new T()
        {
            FileName = this.TransmitInfo.FileName,
            UploadId = this.uploadId,
            MaxBytesPerMessage = maxBytesPerMessage,
            ReplyStatus = MessageReplyStatus.Success
        };
    }

    internal T HandleData<T>(IFileTransmitData message) where T : class, IFileTransmitDataReply, new()
    {
        // Error on no Upload
        if (message.UploadId != uploadId)
            return null;

        // Stop Timer
        timeoutTime = DateTime.Now.AddMilliseconds(timeoutInMs);

        T result = new();

        try
        {
            fileStream.Write(message.MessageData, 0, message.MessageData.Length);
        }
        catch
        {
            result.ReplyStatus = MessageReplyStatus.Error;
            return result;
        }


        if (fileStream.Length >= TransmitInfo.FileSize)
        {
            EndTransfer();
            result.ReplyStatus = MessageReplyStatus.Success;
            return result;
        }
        else
        {
            // Start Thead for next packet.
            result.ReplyStatus = MessageReplyStatus.Continue;
            return result;
        }
    }

    internal T HandleClientCancellation<T>(IFileTransmitCancelled message) where T : class, IFileTransmitDataReply, new()
    {
        EndTransfer();

        return new() { ReplyStatus = MessageReplyStatus.Cancelled };
    }
    #endregion

    #region Private Methods
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        // Check Timeout
        if (DateTime.Now > timeoutTime)
            EndTransfer();
    }

    private void EndTransfer()
    {
        timer.Stop();

        // Remove File and Transfer Object
        using (fileStream) { }
        transfersInProgress.Remove(this.uploadId);

        // Move File to Final Name
        string filePath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.WatchFolder), TransmitInfo.FileName);

        if (File.Exists(filePath)) // "Overwrite"
            File.Delete(filePath);

        if (File.Exists(filePath + tempExtension))
            File.Move(filePath + tempExtension, filePath);
    }
    #endregion
}
