using System;
using System.Collections.Generic;
using System.IO;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.Utility;
using Cygnus.Reach;

using SystemFileInfo = System.IO.FileInfo;

namespace Ahsoka.Services.Cygnus;

// TODO: BMG (Nov. 03, 2023) Create the underlying service base that is going to be used for this class.
/// <summary>
/// 
/// </summary>
public partial class CygnusServerService
{
    /// <summary>
    /// The File data service origin name for files that are going to be exposed through Cygnus
    /// </summary>
    public const string ReachConfigFiles = "Files" + ReachConfigSeparator;

    // NOTE: BMG (Jan. 03, 2024) This value is the median delay per packet when sending out characteristic notifications
    // through BLE. This number was calculated from 6729 separate packet time differences.
    private const long BLEMedianTransferPerPacketTicks = 160000;

    private KeyList FileValueKeys;

    /// <summary>
    /// Since in OpenPV we have less of a concern for concurrent operations, there is little reason why we shouldn't
    /// support multiple file transfer operations simultaneously. We are going to use this dictionary to keep track of
    /// which files have an active transfer as well as how far along the transfer we are.
    /// </summary>
    class ActiveFileTransferInfo
    {
        public uint FileId;
        public uint TransferId;
        public FileStream Stream;
        public uint MessagesPerAck;
        public uint FinalFileLength;
        public bool FileWriting;
        public uint TransferTimeoutMs;
        public long FileTimeoutTimeTicks;
    }

    private Dictionary<uint, ActiveFileTransferInfo> FileIdToActiveFileTransferDictionary = new();
    private Dictionary<uint, ActiveFileTransferInfo> TransferIdToActiveFileTransferDictionary = new();

    private void GetFileValueKeys()
    {
        // Find all the data service values that have an origin indicating that they are files that are going to be
        // exposed through the cygnus files api.
        AvailableKeySearchInfo searchInfo = new()
        {
            OriginFilter = CygnusService.Name,
            KeyFilter = ReachConfigFiles,
        };
        FileValueKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);

        if (FileValueKeys.Keys.Count > 0)
            servicesAvailable |= (UInt32)ServiceIds.Files;
    }

    private DiscoverFilesReply ProcessDiscoverFiles(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, DiscoverFiles request)
    {
        int i = 0;

        List<DiscoverFilesReply> responseList = new();
        do
        {
            DiscoverFilesReply responseObject = new();
            // for (; responseObject.FileInfos.Count < CygnusBufferSizes.NumMediumStructsInMsg && i < FileValueKeys.Keys.Count; i++)
            for (; i < FileValueKeys.Keys.Count; i++)
            {
                CygnusServiceFileInfo fileInfo = GetFileInfoFromId(i);

                responseObject.FileInfos.Add(new() {
                    Access = fileInfo.Permissions,
                    FileId = (UInt32)i,
                    FileName = fileInfo.Name,
                    StorageLocation = StorageLocation.Nonvolatile,
                    CurrentSizeBytes = (Int32)new SystemFileInfo(fileInfo.Path).Length,
                });
            }

            responseList.Add(responseObject);
        }
        while (i < FileValueKeys.Keys.Count);

        // SendMessageArrayInternal(messageHeader, responseList.ToArray());
        return responseList[0];
    }

    private FileTransferInitReply ProcessTransferInit(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, FileTransferInit request)
    {
        CygnusServiceFileInfo fileInfo = GetFileInfoFromId((int)request.FileId);
        FileTransferInitReply response = new();

        // If there is a transfer for the requested file already in progress, we are going to return an error indicating
        // the problem unless it's already timed out previously.
        if (FileIdToActiveFileTransferDictionary.TryGetValue(request.FileId, out ActiveFileTransferInfo transferInfo))
        {
            if (DateTime.Now.Ticks > transferInfo.FileTimeoutTimeTicks)
                CleanTransferId(transferInfo.TransferId);
            else
            {
                if (request.ReadWrite == 0)
                    response.Result = (int)ErrorCodes.ReadFailed;
                else
                    response.Result = (int)ErrorCodes.WriteFailed;
                response.ErrorMessage = "There is a transfer already active for the file requested.";
                return response;
            }
        }

        AccessLevel requestedAccess;
        if (request.ReadWrite == 1)
            requestedAccess = AccessLevel.Write;
        else
            requestedAccess = AccessLevel.Read;

        // If the request is to write
        if (!PermissionHasAccessLevel(fileInfo.Permissions, requestedAccess))
        {
            response.Result = (int)ErrorCodes.PermissionDenied;
            response.ErrorMessage = "Permission not available for requested file.";
            return response;
        }

        SystemFileInfo systemFileInfo = new SystemFileInfo(fileInfo.Path);

        if (request.RequestOffset > systemFileInfo.Length)
        {
            if (request.ReadWrite == 0)
                response.Result = (int)ErrorCodes.ReadFailed;
            else
                response.Result = (int)ErrorCodes.WriteFailed;
            response.ErrorMessage = "The transfer request offset extends past the size of the file.";
            return response;
        }

        if (request.ReadWrite == 0 && request.RequestOffset + request.TransferLength > systemFileInfo.Length)
        {
            response.Result = (int)ErrorCodes.ReadFailed;
            response.ErrorMessage = "Read request is extending past file size.";
            return response;
        }

        response.Result = 0;
        // TODO: BMG (Dec. 21, 2023) Is there any reason why the transfer ID should NOT be the file ID? Seems like a
        // built-in, guaranteed non-duplicate, and consistent value to use for this purpose.
        // response.TransferId = request.FileId;
        response.TransferId = request.TransferId;
        response.PreferredAckRate = request.MessagesPerAck;

        SetupTransferId(request, systemFileInfo);

        return response;
    }

    private FileTransferDataNotification ProcessTransferData(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, FileTransferData request)
    {
        FileTransferDataNotification response = new();
        if (!TransferIdToActiveFileTransferDictionary.ContainsKey(request.TransferId))
        {
            response.Result = (int)ErrorCodes.WriteFailed;
            response.ErrorMessage = "There is no transfer active for the file requested.";
            return response;
        }

        ActiveFileTransferInfo fileTransferInfo = TransferIdToActiveFileTransferDictionary[request.TransferId];

        uint messagesPerAck = fileTransferInfo.MessagesPerAck;
        if (request.MessageNumber > messagesPerAck)
        {
            response.Result = (int)ErrorCodes.WriteFailed;
            response.ErrorMessage = "The message number exceeds the messages per ack value set. Ending Transfer";
            CleanTransferId(fileTransferInfo.TransferId);
            return response;
        }

        FileStream fileStream = fileTransferInfo.Stream;
        ReadOnlySpan<byte> messageDataSpan = request.MessageData;
        fileStream.Write(messageDataSpan);

        uint finalFileLength = fileTransferInfo.FinalFileLength;
        if (request.MessageNumber == messagesPerAck || fileStream.Position == finalFileLength)
        {
            if (DateTime.Now.Ticks > fileTransferInfo.FileTimeoutTimeTicks)
            {
                response.Result = (int)ErrorCodes.WriteFailed;
                response.ErrorMessage = "The transfer has already timed out. Ending Transfer";
                CleanTransferId(fileTransferInfo.TransferId);
                return response;
            }

            response.Result = 0;
            response.TransferId = request.TransferId;
            response.IsComplete = fileStream.Position == finalFileLength;
            response.RetryOffset = (uint)fileStream.Position;

            if (response.IsComplete)
                CleanTransferId(request.TransferId);

            return response;
        }
        else
        {
            return response;
        }
    }

    private FileTransferData ProcessTransferDataNotification(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, FileTransferDataNotification request)
    {
        FileTransferData response = new();

        if (!TransferIdToActiveFileTransferDictionary.ContainsKey(request.TransferId))
        {
            response.Result = (int)ErrorCodes.ReadFailed;
            return response;
        }

        if (request.IsComplete)
        {
            CleanTransferId(request.TransferId);
            // SendMessageArrayInternal(messageHeader, new FileTransferData[1] { new() });
            return response;
        }

        ActiveFileTransferInfo fileTransferInfo = TransferIdToActiveFileTransferDictionary[request.TransferId];

        if (DateTime.Now.Ticks > fileTransferInfo.FileTimeoutTimeTicks)
        {
            response.Result = (int)ErrorCodes.ReadFailed;
            // response. = "The transfer has already timed out. Ending Transfer";
            CleanTransferId(fileTransferInfo.TransferId);
            return response;
        }

        FileStream fileStream = fileTransferInfo.Stream;
        int remainingFileBytes = (int)(fileStream.Length - fileStream.Position);
        int messageDataSize = (int)(fileTransferInfo.MessagesPerAck * CygnusBufferSizes.BigDataBufferSize);
        if (remainingFileBytes < messageDataSize)
            messageDataSize = remainingFileBytes;

        FileTransferData responseObject = new();
        responseObject.Result = 0;
        responseObject.TransferId = request.TransferId;
        responseObject.MessageNumber = 1;
        responseObject.MessageData = new byte[messageDataSize];
        Span<byte> messageDataSpan = responseObject.MessageData;
        fileStream.Read(messageDataSpan);

        // In addition to calculating the new transfer timeout, we are also going to compensate for now knowing when the
        // last packet is going to be sent out by using the median number of ticks between BLE notification sets.
        fileTransferInfo.FileTimeoutTimeTicks =
            // GetTimeoutFromNow(fileTransferInfo.TransferTimeoutMs) + (responseList.Count * BLEMedianTransferPerPacketTicks);
            GetTimeoutFromNow(fileTransferInfo.TransferTimeoutMs) + (messageDataSize / CygnusBufferSizes.BigDataBufferSize * BLEMedianTransferPerPacketTicks);

        // SendMessageArrayInternal(messageHeader, responseList.ToArray());
        return responseObject;
    }

    private CygnusServiceFileInfo GetFileInfoFromId(int fileInfoIndex)
    {
        KeyList keyList = new();
        keyList.Keys.Add(FileValueKeys.Keys[fileInfoIndex]);
        // In this case we know we are going to get a byte array because the idea is that we're always going to
        // store the serialized version of the protobuf message for these file messages.
        byte[] value = DataClient.GetKeyValues(keyList).Values[0].DataValue;

        using MemoryStream fileInfoStream = new(value);
        CygnusServiceFileInfo fileInfo = ProtoBuf.Serializer.Deserialize<CygnusServiceFileInfo>(fileInfoStream);

        return fileInfo;
    }

    private bool PermissionHasAccessLevel(AccessLevel filePermission, AccessLevel requestedAccess)
    {
        // If the permission bitwise-anded with the accesslevel leaves it unchanged then we at least have those
        // permissions.
        return (requestedAccess.EnumToInt() & filePermission.EnumToInt()) == requestedAccess.EnumToInt();
    }

    private long GetTimeoutFromNow(long timeoutMs)
    {
        return DateTime.Now.Ticks + (timeoutMs * TimeSpan.TicksPerMillisecond);
    }

    private void SetupTransferId(FileTransferInit transferInitStruct, SystemFileInfo systemFileInfo)
    {
        ActiveFileTransferInfo fileTransferInfo = new()
        {
            FileId = transferInitStruct.FileId,
            TransferId = transferInitStruct.TransferId,
            MessagesPerAck = transferInitStruct.MessagesPerAck,
            FinalFileLength = transferInitStruct.RequestOffset + transferInitStruct.TransferLength,
            FileWriting = transferInitStruct.ReadWrite == 1,
            TransferTimeoutMs = transferInitStruct.TimeoutInMs,
            FileTimeoutTimeTicks = GetTimeoutFromNow(transferInitStruct.TimeoutInMs),
        };
        if (!fileTransferInfo.FileWriting)
            fileTransferInfo.Stream = systemFileInfo.OpenRead();
        else
            fileTransferInfo.Stream = systemFileInfo.OpenWrite();

        // Setting the environment variables for the file transfer that is currently active.
        fileTransferInfo.Stream.Seek(transferInitStruct.RequestOffset, SeekOrigin.Begin);

        TransferIdToActiveFileTransferDictionary[transferInitStruct.TransferId] = fileTransferInfo;
        FileIdToActiveFileTransferDictionary[transferInitStruct.FileId] = fileTransferInfo;

        string readorwrite;
        if (!fileTransferInfo.FileWriting)
            readorwrite = "reading";
        else
            readorwrite = "writing";

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"CYGNUS_SERVER: Transfer ID {transferInitStruct.TransferId} setup for {readorwrite}");
    }

    private void CleanTransferId(uint transferId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"CYGNUS_SERVER: Transfer ID: {transferId} cleared");

        ActiveFileTransferInfo fileTransferInfo = TransferIdToActiveFileTransferDictionary[transferId];

        fileTransferInfo.Stream.Close();
        TransferIdToActiveFileTransferDictionary.Remove(fileTransferInfo.TransferId);
        FileIdToActiveFileTransferDictionary.Remove(fileTransferInfo.FileId);
    }
}
