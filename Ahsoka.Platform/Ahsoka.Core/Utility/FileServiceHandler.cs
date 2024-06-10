using Ahsoka.System;

namespace Ahsoka.Utility;

internal interface IFileTransmit
{
    string FileName { get; set; }
    int FileSize { get; set; }
    int TimeoutInMs { get; set; }
}

internal interface IFileTransmitReply
{
    int UploadId { get; set; }
    string FileName { get; set; }
    int MaxBytesPerMessage { get; set; }
    MessageReplyStatus ReplyStatus { get; set; }
}

internal interface IFileTransmitData
{
    int UploadId { get; set; }
    byte[] MessageData { get; set; }
    int Crc32 { get; set; }
}

internal interface IFileTransmitDataReply
{
    int UploadId { get; set; }
    MessageReplyStatus ReplyStatus { get; set; }
}

internal interface IFileTransmitCancelled
{
    int UploadId { get; set; }
}

/// <summary>
/// Progress Information used in Sending and Receiving Files.
/// </summary>
public class FileTransmitProgressInfo
{
    /// <summary>
    /// Progress Message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Percent Complete of the Transmit.
    /// </summary>
    public int PercentComplete { get; set; }
}