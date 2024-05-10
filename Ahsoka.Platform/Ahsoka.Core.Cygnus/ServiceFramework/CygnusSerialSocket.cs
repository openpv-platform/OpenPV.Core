using System;
using System.Collections.Generic;
using System.IO;
using Ahsoka.Services.Cygnus;
using Ahsoka.Utility;
using Cygnus.Reach;

#pragma warning disable CS1591

namespace Ahsoka.ServiceFramework;

#region Socket
// A simple wrapper around the serialization of Ahsoka/Cygnus packets according to the specification outlined here:
//   <SERIALIZE_AHSOKA_PROTO>.md
public abstract class CygnusSerialSocket : AhsokaSerialSocket
{
    // NOTE: BMG (Jan. 18, 2024) These are the values that are going to be the default to the current Cygnus server BLE
    // communication interface.
    private BufferSizes CygnusBufferSizes => CygnusServerService.CygnusBufferSizes;

    private byte[] GetByteArrayFromProtoObject<T>(T messageStruct)
    {
        using MemoryStream messageStream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(messageStream, messageStruct);
        return messageStream.ToArray();
    }

    private T GetProtoObjectFromByteArray<T>(byte[] messageDataArray)
    {
        using MemoryStream messageStruct = new MemoryStream(messageDataArray);
        return ProtoBuf.Serializer.Deserialize<T>(messageStruct);
    }

    // TODO: BMG (Nov. 21, 2023) If this is going to be used long term, reduce the number of allocations made.
    public override void SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification)
    {
        using MemoryStream headerStream = new MemoryStream(header);
        AhsokaMessageHeader ahsokaHeader = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(headerStream);

        ReachMessage reachMessageStruct = new ReachMessage();
        reachMessageStruct.Header = new ReachMessageHeader();
        reachMessageStruct.Header.MessageType = (UInt32)ahsokaHeader.TransportId;
        reachMessageStruct.Header.EndpointId = ahsokaHeader.EndpointId;
        reachMessageStruct.Header.TransactionId = (UInt32)ahsokaHeader.ClientMessageId;

        List<byte[]> transmitMessages = new();

        switch (ahsokaHeader.TransportId.IntToEnum<ReachMessageTypes>())
        {
        case ReachMessageTypes.DiscoverParameters:
            ParameterInfoResponse pIIn = GetProtoObjectFromByteArray<ParameterInfoResponse>(data);
            if (pIIn.ParameterInfos.Count > CygnusBufferSizes.CountParamDescInResponse)
                for (int i = 0; i < pIIn.ParameterInfos.Count;)
                {
                    ParameterInfoResponse pIOutItem = new();
                    for (; pIOutItem.ParameterInfos.Count < CygnusBufferSizes.CountParamDescInResponse && i < pIIn.ParameterInfos.Count; i++)
                        pIOutItem.ParameterInfos.Add(pIIn.ParameterInfos[i]);
                    transmitMessages.Add(GetByteArrayFromProtoObject(pIOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.DiscoverParamEx:
            ParamExInfoResponse pExIn = GetProtoObjectFromByteArray<ParamExInfoResponse>(data);
            if (pExIn.Enumerations.Count > CygnusBufferSizes.ParamInfoEnumCount)
                for (int i = 0; i < pExIn.Enumerations.Count;)
                {
                    ParamExInfoResponse pExOutItem = new();
                    for (; pExOutItem.Enumerations.Count < CygnusBufferSizes.CountParamDescInResponse && i < pExIn.Enumerations.Count; i++)
                        pExOutItem.Enumerations.Add(pExIn.Enumerations[i]);
                    pExOutItem.AssociatedPid = pExIn.AssociatedPid;
                    pExOutItem.DataType = pExIn.DataType;
                    transmitMessages.Add(GetByteArrayFromProtoObject(pExOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.ReadParameters:
            ParameterReadResult readIn = GetProtoObjectFromByteArray<ParameterReadResult>(data);
            if (readIn.Values.Count > CygnusBufferSizes.NumMediumStructsInMsg)
                for (int i = 0; i < readIn.Values.Count;)
                {
                    ParameterReadResult readOutItem = new();
                    for (; readOutItem.Values.Count < CygnusBufferSizes.NumMediumStructsInMsg && i < readIn.Values.Count; i++)
                        readOutItem.Values.Add(readIn.Values[i]);
                    readOutItem.ReadTimestamp = readIn.ReadTimestamp;
                    transmitMessages.Add(GetByteArrayFromProtoObject(readOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.DiscoverFiles:
            DiscoverFilesReply filesIn = GetProtoObjectFromByteArray<DiscoverFilesReply>(data);
            if (filesIn.FileInfos.Count > CygnusBufferSizes.NumMediumStructsInMsg)
                for (int i = 0; i < filesIn.FileInfos.Count;)
                {
                    DiscoverFilesReply filesOutItem = new();
                    for (; filesOutItem.FileInfos.Count < CygnusBufferSizes.NumMediumStructsInMsg && i < filesIn.FileInfos.Count; i++)
                        filesOutItem.FileInfos.Add(filesIn.FileInfos[i]);
                    transmitMessages.Add(GetByteArrayFromProtoObject(filesOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.TransferDataNotification:
            if (data.Length == 0)
                return;

            // NOTE: BMG (Dec. 28, 2023) For some reason, the header transport ID is going to change between when it's
            // received and sent. This is only the case for the TransferData and TransferDataNotification message types.
            reachMessageStruct.Header.MessageType = (uint)ReachMessageTypes.TransferData;
            FileTransferData fileDataIn = GetProtoObjectFromByteArray<FileTransferData>(data);
            if (fileDataIn.MessageData.Length > CygnusBufferSizes.BigDataBufferSize)
            {
                using MemoryStream fileDataStream = new MemoryStream(data);
                Span<byte> fileDataSpan;
                FileTransferData fileDataOutItem;
                int remainingFileBytes;
                int messageDataSize;
                while ((remainingFileBytes = (int)(fileDataStream.Length - fileDataStream.Position)) != 0)
                {
                    fileDataOutItem = new();
                    if (remainingFileBytes < CygnusBufferSizes.BigDataBufferSize)
                        messageDataSize = remainingFileBytes;
                    else
                        messageDataSize = (int)CygnusBufferSizes.BigDataBufferSize;
                    fileDataOutItem.MessageData = new byte[messageDataSize];
                    fileDataSpan = fileDataOutItem.MessageData;
                    fileDataStream.Read(fileDataSpan);
                    fileDataOutItem.Result = fileDataIn.Result;
                    fileDataOutItem.TransferId = fileDataIn.TransferId;
                    fileDataOutItem.MessageNumber = (uint)transmitMessages.Count + 1;

                    transmitMessages.Add(GetByteArrayFromProtoObject(fileDataOutItem));
                }
            }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.DiscoverCommands:
            DiscoverCommandsResult cmdIn = GetProtoObjectFromByteArray<DiscoverCommandsResult>(data);
            if (cmdIn.AvailableCommands.Count > CygnusBufferSizes.NumMediumStructsInMsg)
                for (int i = 0; i < cmdIn.AvailableCommands.Count;)
                {
                    DiscoverCommandsResult cmdOutItem = new();
                    for (; cmdOutItem.AvailableCommands.Count < CygnusBufferSizes.NumMediumStructsInMsg && i < cmdIn.AvailableCommands.Count; i++)
                        cmdOutItem.AvailableCommands.Add(cmdIn.AvailableCommands[i]);
                    transmitMessages.Add(GetByteArrayFromProtoObject(cmdOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.TransferData:
            // NOTE: BMG (Dec. 29, 2023) In the case of the transfer data, there are cases where a response isn't
            // expected. If the return data for this request is empty, we are just going to short-circuit the function
            // and not return anything. If there is data, we can expect it to be a TransferDataNotification.
            // if (data.Length == 0)
            //     return;
            // NOTE: BMG (Dec. 28, 2023) For some reason, the header transport ID is going to change between when it's
            // received and sent. This is only the case for the TransferData and TransferDataNotification message types.
            reachMessageStruct.Header.MessageType = (uint)ReachMessageTypes.TransferDataNotification;
            transmitMessages.Add(data);
            break;

        case ReachMessageTypes.CliNotification:
            CLIData cliDataIn = GetProtoObjectFromByteArray<CLIData>(data);
            if (cliDataIn.MessageData.Length > CygnusBufferSizes.BigDataBufferSize)
                for (int i = 0; i < cliDataIn.MessageData.Length;)
                {
                    CLIData cliDataOutItem = new();
                    int remainingCharacters = cliDataIn.MessageData.Length - i;
                    int readCharacterCount = 0;
                    if (remainingCharacters > (int)CygnusBufferSizes.BigDataBufferSize)
                        readCharacterCount = (int)CygnusBufferSizes.BigDataBufferSize;
                    else
                        readCharacterCount = remainingCharacters;
                    cliDataOutItem.MessageData = cliDataIn.MessageData.Substring(i, readCharacterCount);
                    i += readCharacterCount;
                    transmitMessages.Add(GetByteArrayFromProtoObject(cliDataOutItem));
                }
            else
                transmitMessages.Add(data);
            break;

        case ReachMessageTypes.Ping:
        case ReachMessageTypes.GetDeviceInfo:
        case ReachMessageTypes.WriteParameters:
        case ReachMessageTypes.TransferInit:
        case ReachMessageTypes.DiscoverStreams:
        case ReachMessageTypes.OpenStream:
        case ReachMessageTypes.CloseStream:
        case ReachMessageTypes.StreamDataNotification:
        case ReachMessageTypes.SendCommand:
        default:
            transmitMessages.Add(data);
            break;
        }

        if (transmitMessages.Count == 1)
            reachMessageStruct.Header.NumberOfObjects = 0;
        else
            reachMessageStruct.Header.NumberOfObjects = (uint)transmitMessages.Count;

        if (ahsokaHeader.TransportId.IntToEnum<ReachMessageTypes>() == ReachMessageTypes.TransferData && reachMessageStruct.Header.NumberOfObjects == 0)
            reachMessageStruct.Header.NumberOfObjects = 1;

        for (int i = 0; i < transmitMessages.Count; i++)
        {
            reachMessageStruct.Header.RemainingObjects = (uint)(transmitMessages.Count - 1 - i);
            // After we've set the number of objects, and there is no more objects remaining, reset the number of
            // objects in the reach header.
            if (transmitMessages[i] == null || transmitMessages[i].Length == 0)
                reachMessageStruct.Payload = null;
            else
                reachMessageStruct.Payload = transmitMessages[i];

            using MemoryStream reachHeaderStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(reachHeaderStream, reachMessageStruct);

            byte[] transmitBuffer = reachHeaderStream.ToArray();

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"CYGNUS_SERIAL: Sending Byte -> {BitConverter.ToString(transmitBuffer).Replace("-", " ")}");

            if (!TransmitOverInterface(transmitBuffer, clientId))
            {
                // We should maybe throw an error or log something in this case because we were unable to successfully
                // transmit the value through the underlying interface.
            }
        }

    }

    public override bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, int timeOutInMs = int.MaxValue)
    {
        if (timeOutInMs > IAhsokaSocket.MaxTimeoutValue)
            timeOutInMs = IAhsokaSocket.MaxTimeoutValue;

        if (!ReceiveOverInterface(out byte[] packetData, out clientId)) {
            // There was no message available for us to retrieve from the underlying interface.
            header = null;
            data = null;
            clientId = null;
            return false;
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"CYGNUS_SERIAL: Receiving Byte <- {BitConverter.ToString(packetData).Replace("-", " ")}");

        using MemoryStream packetDataStream = new MemoryStream(packetData);
        ReachMessage reachMessageStruct = ProtoBuf.Serializer.Deserialize<ReachMessage>(packetDataStream);

        AhsokaMessageHeader ahsokaHeader = new AhsokaMessageHeader();
        ahsokaHeader.TransportId = (Int32)reachMessageStruct.Header.MessageType;
        ahsokaHeader.EndpointId = reachMessageStruct.Header.EndpointId;
        // ahsokaHeader.ClientMessageId = (Int32)reachMessageStruct.Header.RemainingObjects;
        // CurrentTransactionId = reachMessageStruct.Header.TransactionId;
        ahsokaHeader.ClientMessageId = (Int32)reachMessageStruct.Header.TransactionId;
        // ahsokaHeader.RemainingMessages = (Int32)reachMessageStruct.Header.RemainingObjects;

        // NOTE: BMG (Nov. 21, 2023) This isn't going to be used at the moment because we are just going to assume that
        // every message is going to be contained within a single BLE 244-byte packet.
        // ahsokaHeader.MessageSize = reachMessageStruct.Payload.Length;

        using MemoryStream headerStream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(headerStream, ahsokaHeader);
        header = headerStream.ToArray();
        if (reachMessageStruct.Payload != null)
            data = reachMessageStruct.Payload;
        else
            data = Array.Empty<byte>();

        return true;
    }
}
#endregion
#pragma warning restore CS1591