using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.System;
using Cygnus.Reach;
using CygnusParameterInfo = Cygnus.Reach.ParameterInfo;

namespace Ahsoka.Services.Cygnus;

// TODO: BMG (Nov. 03, 2023) Create the underlying service base that is going to be used for this class.
/// <summary>
/// 
/// </summary>
public partial class CygnusServerService
{
    /// <summary>
    /// The File data service origin name for enum definitions that are going to be used for some Cygnus parameters
    /// </summary>
    public const string ReachConfigEnums = "Enums" + ReachConfigSeparator;

    private KeyList ParameterValueKeys;
    private uint ParameterValueKeysChecksum;
    private KeyList EnumValueKeys;

    private void GetParameterValueKeys()
    {
        // We are not going to expose the data service configuration values for the Cygnus server as parameters.
        AvailableKeySearchInfo searchInfo = new()
        {
            InvertFilters = true,
            OriginFilter = CygnusService.Name,
        };
        ParameterValueKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);
        using MemoryStream DataValueKeysStream = new();
        // TODO: BMG (Dec. 01, 2023) We can technically optimize here by ignoring the ValueIds, through I'm unsure
        // if it'll actually make a difference in the end...
        ProtoBuf.Serializer.Serialize(DataValueKeysStream, ParameterValueKeys);
        ParameterValueKeysChecksum = BitConverter.ToUInt32(SHA256.Create().ComputeHash(DataValueKeysStream), 5);

        // Find all the data service values that have an origin indicating that they are descriptors for enums that
        // live in the parameter api that is exposed through cygnus.
        // NOTE: BMG (Dec. 21, 2023) This is how we are handling keeping maintaining this information without making
        // any cygnus specific changes to what information we are storing within the data service.
        searchInfo = new AvailableKeySearchInfo()
        {
            OriginFilter = ReachConfigStreams,
        };
        EnumValueKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);

        if (ParameterValueKeys.Keys.Count > 0)
            servicesAvailable |= (UInt32)ServiceIds.ParameterRepo;
    }

    private ParameterInfoResponse ProcessDiscoverParameters(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, ParameterInfoRequest request)
    {
        if (request.ParameterIds != null && request.ParameterIds.Length > CygnusBufferSizes.ParameterBufferCount)
        {
            // Throw an error or something to return to the client.
            throw new NotImplementedException("The error response for Discover Parameters is not implemented.");
        }

        bool parameterIDsProvided = request.ParameterIds != null && request.ParameterIds.Length != 0;

        List<ParameterInfoResponse> responseList = new();
        lock(ParameterValueKeys)
        {
            var moreParametersAvailable = (int i) => (!parameterIDsProvided && i < ParameterValueKeys.Keys.Count) ||
                                                     (parameterIDsProvided && i < request.ParameterIds.Length);
            int i = 0;

            while(moreParametersAvailable(i))
            {
                ParameterInfoResponse responseObject = new ParameterInfoResponse();
                CygnusParameterInfo description;

                // for (; responseObject.ParameterInfos.Count < CygnusBufferSizes.CountParamDescInResponse && moreParametersAvailable(i); i++)
                for (; moreParametersAvailable(i); i++)
                {
                    int dataValueKeyIndex = i;
                    if (parameterIDsProvided)
                        dataValueKeyIndex = (int)request.ParameterIds[i];
                    GetParameterDescriptionFromIndex(dataValueKeyIndex, out description);
                    responseObject.ParameterInfos.Add(description);
                }

                responseList.Add(responseObject);
            }
        }

        // SendMessageArrayInternal(messageHeader, responseList.ToArray());
        return responseList[0];
    }

    private ParamExInfoResponse ProcessDiscoverParametersEx(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, ParameterInfoRequest request)
    {
        if (request.ParameterIds != null && request.ParameterIds.Length > CygnusBufferSizes.ParameterBufferCount)
        {
            // Throw an error or something to return to the client.
            throw new NotImplementedException("The error response for Discover Parameters is not implemented.");
        }

        // TODO: BMG (Dec. 21, 2023) Technically this is not implemented yet as there is not a current scheme for how we
        // are going to handle storing enums and bitfields for Cygnus.
        return new ParamExInfoResponse();
    }

    private ParameterReadResult ProcessReadParameters(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, ParameterRead request)
    {
        if (request.ParameterIds != null && request.ParameterIds.Length > CygnusBufferSizes.ParameterBufferCount)
        {
            // Throw an error or something to return to the client.
            throw new NotImplementedException("The error response for Discover Parameters is not implemented.");
        }

        bool parameterIDsProvided = request.ParameterIds != null && request.ParameterIds.Length != 0;

        List<ParameterReadResult> responseList = new();
        lock(ParameterValueKeys)
        {
            var moreParametersAvailable = (int i) => (!parameterIDsProvided && i < ParameterValueKeys.Keys.Count) ||
                                                     (parameterIDsProvided && i < request.ParameterIds.Length);
            int i = 0;

            while (moreParametersAvailable(i))
            {
                ParameterReadResult responseObject = new ParameterReadResult();
                ParameterValue value;

                // for (; responseObject.Values.Count < CygnusBufferSizes.NumMediumStructsInMsg && moreParametersAvailable(i); i++)
                for (; moreParametersAvailable(i); i++)
                {
                    int dataValueKeyIndex = i;
                    if (parameterIDsProvided)
                        dataValueKeyIndex = (int)request.ParameterIds[i];
                    GetParameterValueFromIndex(dataValueKeyIndex, out value);
                    responseObject.Values.Add(value);
                }

                responseList.Add(responseObject);
            }
        }

        // SendMessageArrayInternal(messageHeader, responseList.ToArray());
        return responseList[0];
    }

    private ParameterWriteResult ProcessWriteParameters(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, ParameterWrite request)
    {
        if (request.Values != null && request.Values.Count > CygnusBufferSizes.NumMediumStructsInMsg)
        {
            // Throw an error or something to return to the client.
            throw new NotImplementedException("The error response for Discover Parameters is not implemented.");
        }

        lock(ParameterValueKeys)
        {
            for (int i = 0; i < request.Values.Count; i++)
            {
                SetParameterValueFromIndex((int)request.Values[i].ParameterId, request.Values[i]);
            }
        }

        return new ParameterWriteResult() { Result = 0 };
    }

    private void GetDataTypeAndSizeFromKey(KeyInfo keyInfo, CygnusParameterInfo parameterInfo)
    {
        KeyList getKeyValueList = new();
        getKeyValueList.Keys.Add(keyInfo);
        object value = DataClient.GetKeyValues(getKeyValueList).Values[0].GetValue();

        if (value.GetType() == typeof(bool))
        {
            parameterInfo.DataType = ParameterDataType.Bool;
            parameterInfo.SizeInBytes = sizeof(bool);
            parameterInfo.RangeMin = 0;
            parameterInfo.RangeMax = 1;
        }
        else if (value.GetType() == typeof(Int64))
        {
            parameterInfo.DataType = ParameterDataType.Int64;
            parameterInfo.SizeInBytes = sizeof(Int64);
            parameterInfo.RangeMin = Int64.MinValue;
            parameterInfo.RangeMax = Int64.MaxValue;
        }
        else if (value.GetType() == typeof(UInt64))
        {
            parameterInfo.DataType = ParameterDataType.Uint64;
            parameterInfo.SizeInBytes = sizeof(UInt64);
            parameterInfo.RangeMin = UInt64.MinValue;
            parameterInfo.RangeMax = UInt64.MaxValue;
        }
        else if (value.GetType() == typeof(float))
        {
            parameterInfo.DataType = ParameterDataType.Float32;
            parameterInfo.SizeInBytes = sizeof(float);
            parameterInfo.RangeMin = float.MinValue;
            parameterInfo.RangeMax = float.MaxValue;
        }
        else if (value.GetType() == typeof(double))
        {
            parameterInfo.DataType = ParameterDataType.Float64;
            parameterInfo.SizeInBytes = sizeof(double);
            parameterInfo.RangeMin = double.MinValue;
            parameterInfo.RangeMax = double.MaxValue;
        }
        else if (value.GetType() == typeof(byte[]))
        {
            parameterInfo.DataType = ParameterDataType.ByteArray;
            parameterInfo.SizeInBytes = (uint)((byte[])value).Length;
            // parameterInfo.RangeMin = 0;
            // parameterInfo.RangeMax = 1;
        }
        else if (value.GetType() == typeof(string))
        {
            parameterInfo.DataType = ParameterDataType.String;
            parameterInfo.SizeInBytes = (uint)((string)value).Length;
            // parameterInfo.RangeMin = 0;
            // parameterInfo.RangeMax = 1;
        }
        else { throw new ArgumentException($"{value.GetType()} Is Not Supported byt Data Service"); }
    }

    private void GetParameterDescriptionFromIndex(int dataValueKeyIndex, out CygnusParameterInfo parameterInfo)
    {
        KeyInfo keyInfo = ParameterValueKeys.Keys[dataValueKeyIndex];
        parameterInfo = new CygnusParameterInfo();
        GetDataTypeAndSizeFromKey(keyInfo, parameterInfo);

        parameterInfo.Name = $"{keyInfo.Origin}_{keyInfo.Key}";
        parameterInfo.Id = (uint)dataValueKeyIndex;
        if (keyInfo.MininumAccessLevel == DataAccessLevel.Public)
            parameterInfo.Access = AccessLevel.ReadWrite;
        else
            parameterInfo.Access = AccessLevel.Read;
    }

    private void GetValueFromKey(KeyInfo keyInfo, out ParameterValue parameterValue)
    {
        parameterValue = new ParameterValue();

        KeyList getKeyValueList = new();
        getKeyValueList.Keys.Add(keyInfo);
        object value =  DataClient.GetKeyValues(getKeyValueList).Values[0].GetValue();

        if (value.GetType() == typeof(bool))
        {
            parameterValue.BoolValue = (bool)value;
        }
        else if (value.GetType() == typeof(Int64))
        {
            parameterValue.Sint64Value = (Int64)value;
        }
        else if (value.GetType() == typeof(UInt64))
        {
            parameterValue.Uint64Value = (UInt64)value;
        }
        else if (value.GetType() == typeof(float))
        {
            parameterValue.Float32Value = (float)value;
        }
        else if (value.GetType() == typeof(double))
        {
            parameterValue.Float64Value = (double)value;
        }
        else if (value.GetType() == typeof(byte[]))
        {
            parameterValue.BytesValue = (byte[])value;
        }
        else if (value.GetType() == typeof(string))
        {
            parameterValue.StringValue = (string)value;
        }
        else { throw new ArgumentException($"{value.GetType()} Is Not Supported byt Data Service"); }
    }

    private void GetParameterValueFromIndex(int dataValueKeyIndex, out ParameterValue parameterValue)
    {
        GetValueFromKey(ParameterValueKeys.Keys[dataValueKeyIndex], out parameterValue);
        parameterValue.ParameterId = (uint)dataValueKeyIndex;
    }

    private void SetParameterValueFromIndex(int dataValueKeyIndex, ParameterValue parameterValue)
    {
        KeyValueList keyValueList = new();
        KeyInfo keyInfo = ParameterValueKeys.Keys[dataValueKeyIndex];
        keyInfo.PublisherId = DataClient.SubscriberIds[0];

        // TODO: BMG (Dec. 04, 2023) At the moment, we don't want to allow for writes at anything over the public level
        // of access.
        if (keyInfo.MininumAccessLevel != DataAccessLevel.Public)
            // TODO: BMG (Dec. 04, 2023) Need to change this to return an error for Cygnus.
            return;

        KeyValue keyValue = new() { KeyInfo = keyInfo };
        keyValueList.Values.Add(keyValue);

        if (parameterValue.ShouldSerializeUint32Value())
            keyValue.SetValue( parameterValue.Uint32Value);
        else if (parameterValue.ShouldSerializeSint32Value())
            keyValue.SetValue(parameterValue.Sint32Value);
        else if (parameterValue.ShouldSerializeFloat32Value())
            keyValue.SetValue(parameterValue.Float32Value);
        else if (parameterValue.ShouldSerializeUint64Value())
            keyValue.SetValue(parameterValue.Uint64Value);
        else if (parameterValue.ShouldSerializeSint64Value())
            keyValue.SetValue(parameterValue.Sint64Value);
        else if (parameterValue.ShouldSerializeFloat64Value())
            keyValue.SetValue(parameterValue.Float64Value);
        else if (parameterValue.ShouldSerializeBoolValue())
            keyValue.SetValue(parameterValue.BoolValue);
        else if (parameterValue.ShouldSerializeStringValue())
            keyValue.SetValue(parameterValue.StringValue);
        else if (parameterValue.ShouldSerializeEnumValue())
            keyValue.SetValue(parameterValue.EnumValue);
        else if (parameterValue.ShouldSerializeBitfieldValue())
            keyValue.SetValue(parameterValue.BitfieldValue);
        else if (parameterValue.ShouldSerializeBytesValue())
            keyValue.SetValue(parameterValue.BytesValue);
        else { throw new ArgumentException($"An unsupported value was provided to this function."); }

        DataClient.SetKeyValues(keyValueList);
    }
}
