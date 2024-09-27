using Ahsoka.Installer;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;
using System.IO;
using System.Threading;

namespace Ahsoka.Utility;

// Handles the Client side of a File Transfer
internal class FileTransferClientHelper
{
    readonly int defaultTimeout = 5000;
    CancellationTokenSource cancellationTokenSource;
    readonly SystemServiceClient client;
    MessageReplyStatus finalResult = MessageReplyStatus.Timeout;

    public FileTransferClientHelper(SystemServiceClient client)
    {
        this.client = client;
    }

    public MessageReplyStatus LoadFile(Stream stream, bool autoload, IProgress<FileTransmitProgressInfo> progress)
    {
        bool didFinish = false;
        cancellationTokenSource = new CancellationTokenSource();

        LoadApplication loadAppBegin = new()
        {
            Autoload = autoload,
            FileName = "autoload" + PackageType.opvEncrypted.GetFileExtension(),
            TimeoutInMs = defaultTimeout,
            FileSize = (int)stream.Length,
        };

        progress.Report(new FileTransmitProgressInfo() { Message = $"Starting File Transfer for {loadAppBegin.FileName}\n", PercentComplete = 0 });
        var reply = client.SendNotificationWithResponse<LoadApplicationReply>(SystemMessageTypes.Ids.LoadApplication, loadAppBegin);

        try
        {
            byte[] buffer = new byte[reply.MaxBytesPerMessage];

            int lastPercent = 0;

            // If Successful Proceed with Upload.
            if (reply.ReplyStatus == MessageReplyStatus.Success)
            {
                while (stream.Position < stream.Length &&
                    !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Read and Send Data
                    var count = stream.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        // Handle Last Message with a Resize
                        if (buffer.Length > count)
                            Array.Resize<byte>(ref buffer, count);

                        LoadApplicationData data = new()
                        {
                            UploadId = reply.UploadId,
                            MessageData = buffer
                        };

                        var result = client.SendNotificationWithResponse<LoadApplicationDataReply>(SystemMessageTypes.Ids.LoadApplicationData, data, 10000);
                        if (result == null || result.ReplyStatus is MessageReplyStatus.Cancelled or MessageReplyStatus.Undetermined or MessageReplyStatus.Error)
                        {
                            if (result != null)
                                finalResult = result.ReplyStatus;

                            throw new TimeoutException();
                        }
                        else
                            finalResult = result.ReplyStatus;
                    }

                    int percentComplete = (int)(stream.Position / (float)stream.Length * 100);
                    if (lastPercent != percentComplete && percentComplete % 5 == 0)
                    {
                        string dots = new('.', percentComplete / 5);
                        progress.Report(new FileTransmitProgressInfo() { PercentComplete = lastPercent, Message = $"\n{dots} {percentComplete}% Uploaded" });
                        lastPercent = percentComplete;
                    }
                }

                didFinish = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore Cancellation and Handle in Finally
        }
        catch (TimeoutException)
        {
            progress.Report(new FileTransmitProgressInfo() { Message = $"Timed out sending Application Data to Target", PercentComplete = 99 });
            throw;
        }
        finally
        {
            // Notify Of Cancellation
            if (!didFinish)
            {
                LoadApplicationCancelled cancelNotification = new()
                {
                    UploadId = reply.UploadId,
                };

                // Notify Service to Cancel the Upload
                client.SendNotificationWithResponse<LoadApplicationDataReply>(SystemMessageTypes.Ids.LoadApplicationCancelled, cancelNotification, 1000);
            }

            progress.Report(new FileTransmitProgressInfo() { Message = $"\nFinished File Transfer for File:{loadAppBegin.FileName} with Result:{finalResult}\n", PercentComplete = 100 });
        }

        return finalResult;
    }

    public void CancelTransfer()
    {
        cancellationTokenSource?.Cancel();
    }
}
