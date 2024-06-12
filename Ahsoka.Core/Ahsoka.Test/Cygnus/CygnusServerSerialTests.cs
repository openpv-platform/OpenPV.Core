using System.Collections.Generic;
using System.Text;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Cygnus;
using Ahsoka.Services.Data;
using Ahsoka.Services.System;
using Cygnus.Reach;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahsoka.Test;

// [TestClass]
public class CygnusServerSerialTests : LinearTestBase
{
    protected DataServiceClient dataClient = new DataServiceClient();
    protected SystemServiceClient systemClient = new SystemServiceClient();
    protected CygnusServiceClient CygnusClient = new CygnusServiceClient();
    private TCPCygnusServerClient TestClient = new();
    private KeyValueList KeyValueList = new();

    private int ExampleCommandCallback(out string message)
    {
        message = "This is a test.";
        return 0;
    }

    public CygnusServerSerialTests()
    {
        AhsokaRuntime.CreateBuilder().AddClients(dataClient, systemClient, CygnusClient).StartWithInternalServices();

        KeyValueList.Values.Add( // 0
            new() {
                KeyInfo = new()
                {
                    Origin = SystemService.Name,
                    Key = SystemServiceDataKeys.SDKVersion,
                },
                StringValue = "sdkVersion",
            }
        );
        KeyValueList.Values.Add( // 1
            new() {
                KeyInfo = new()
                {
                    Origin = SystemService.Name,
                    Key = SystemServiceDataKeys.ApplicationVersion,
                },
                StringValue = "appVersion",
            }
        );
        KeyValueList.Values.Add( // 2
            new() {
                KeyInfo = new()
                {
                    Origin = SystemService.Name,
                    Key = SystemServiceDataKeys.ApplicationDescription,
                },
                StringValue = "appDescription",
            }
        );
        KeyValueList.Values.Add( // 3
            new() {
                KeyInfo = new()
                {
                    Origin = SystemService.Name,
                    Key = SystemServiceDataKeys.CurrentApplicationName,
                },
                StringValue = "appName",
            });
        KeyValueList.Values.Add( // 4
            new(){
                KeyInfo = new()
                {
                    Origin = TestClient.ServiceConfig.ServiceName,
                    Key = "value1"
                },
                IntegerValue = 1,
            });
        KeyValueList.Values.Add( // 5
            new() {
                KeyInfo = new KeyInfo
                {
                    Origin = TestClient.ServiceConfig.ServiceName,
                    Key = "value2"
                },
                StringValue = "test",
            });
        dataClient.SetKeyValues(KeyValueList);

        CygnusClient.RegsiterCommandCallback(
            new CygnusServiceCommandInfo() { Name = "Test Command" },
            dataClient,
            ExampleCommandCallback
        );

        CygnusClient.EnableTransport(CygnusInterface.TCPSerial);
        TestClient.Connect();
    }

    [TestMethod]
    public void TestPing()
    {
        byte[] PingData1 = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        PingResponse response = TestClient.Ping(PingData1);
        for (int i = 0; i < PingData1.Length; i++)
        {
            Assert.IsTrue(PingData1[i] == response.EchoData[i]);
        }

        byte[] PingData2 = Encoding.UTF8.GetBytes("Cygnus");
        response = TestClient.Ping(PingData2);
        for (int i = 0; i < PingData1.Length; i++)
        {
            Assert.IsTrue(PingData2[i] == response.EchoData[i]);
        }

        TestClient.Disconnect();
        AhsokaRuntime.ShutdownAll();
    }

    [TestMethod]
    public void TestGetDeviceInfo()
    {
        DeviceInfoResponse deviceInfoResponse = TestClient.GetDeviceInfo();

        string setFirmwareVersion       = KeyValueList.Values[0].StringValue;
        string setApplicationIdentifier = KeyValueList.Values[1].StringValue;
        string setDeviceDescription     = KeyValueList.Values[2].StringValue;
        string setDeviceName            = KeyValueList.Values[3].StringValue;

        Assert.IsTrue(deviceInfoResponse.DeviceName.Equals(setDeviceName));
        Assert.IsTrue(Encoding.UTF8.GetString(deviceInfoResponse.ApplicationIdentifier).Equals(setApplicationIdentifier));
        Assert.IsTrue(deviceInfoResponse.DeviceDescription.Equals(setDeviceDescription));
        Assert.IsTrue(deviceInfoResponse.FirmwareVersion.Equals(setFirmwareVersion));

        TestClient.Disconnect();
        AhsokaRuntime.ShutdownAll();
    }

    [TestMethod]
    public void TestDiscoverParameters()
    {
        KeyInfo value1Info = KeyValueList.Values[4].KeyInfo;
        KeyInfo value2Info = KeyValueList.Values[5].KeyInfo;

        List<ParameterInfo> parameterInfoResponses = TestClient.DiscoverParameters();

        int value1Index = parameterInfoResponses.FindIndex((ParameterInfo pi) => pi.Name == $"{value1Info.Origin}_{value1Info.Key}");
        Assert.IsTrue(value1Index != -1);
        Assert.IsTrue(parameterInfoResponses[value1Index].DataType == ParameterDataType.Int32);

        int value2Index = parameterInfoResponses.FindIndex((ParameterInfo pi) => pi.Name == $"{value2Info.Origin}_{value2Info.Key}");
        Assert.IsTrue(value2Index != -1);
        Assert.IsTrue(parameterInfoResponses[value2Index].DataType == ParameterDataType.String);

        TestClient.Disconnect();
        AhsokaRuntime.ShutdownAll();
    }

    [TestMethod]
    public void TestReadParameters()
    {
        KeyInfo value1Info = KeyValueList.Values[4].KeyInfo;
        KeyInfo value2Info = KeyValueList.Values[5].KeyInfo;

        List<ParameterInfo> parameterInfoResponse = TestClient.DiscoverParameters();

        List<uint> requestValues = new() {
            parameterInfoResponse.Find((ParameterInfo pi) => pi.Name == $"{value1Info.Origin}_{value1Info.Key}").Id,
            parameterInfoResponse.Find((ParameterInfo pi) => pi.Name == $"{value2Info.Origin}_{value2Info.Key}").Id,
        };

        List<ParameterValue> parameterValueResponse = TestClient.ReadParameters(requestValues);

        Assert.IsTrue(parameterValueResponse[0].Sint32Value == KeyValueList.Values[4].IntegerValue);
        Assert.IsTrue(parameterValueResponse[1].StringValue == KeyValueList.Values[5].StringValue);
    }
}
