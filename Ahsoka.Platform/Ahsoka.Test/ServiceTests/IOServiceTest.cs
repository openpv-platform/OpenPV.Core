using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO;
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ahsoka.Test;

[TestClass]
public class IOServiceTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestIOService()
    {
        // Start client for NetworkService
        var client = new IOServiceClient();
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                    .AddClients(client)
                    .StartWithInternalServices();

        var pollingInterval = client.GetPollingInterval();
        Assert.IsTrue(pollingInterval.PollingIntervalMs == 500);

        // Test Setting Polling Interval
        client.SetPollingInterval(new PollingInterval() { PollingIntervalMs = 1000});
        pollingInterval = client.GetPollingInterval();
        Assert.IsTrue(pollingInterval.PollingIntervalMs == 1000);

        /*
            This test, for the most part, is just a preliminary test
        */
        AnalogInputList AInList = client.RequestAnalogInputs();
        Assert.AreEqual(AInList.AnalogInputs.Count, 2);

        AnalogOutputList AOutList = client.RequestAnalogOutputs();
        Assert.AreEqual(AOutList.AnalogOutputs.Count, 0);

        DigitalInputList DInList = client.RequestDigitalInputs();
        Assert.AreEqual(DInList.DigitalInputs.Count, 2);

        DigitalOutputList DOutList = client.RequestDigitalOutputs();
        Assert.AreEqual(DOutList.DigitalOutputs.Count, 2);

        // Becuase Windows platform has no available IO Pins yet, test responses from fake IO Pins
        GetInputResponse AInResponse = client.GetAnalogInput(AInList.AnalogInputs.First());
        Assert.AreEqual(AInResponse.ErrorDescription, "Windows Analog Inputs Not Implemented");

        GetInputResponse DInResponse = client.GetDigitalInput(DInList.DigitalInputs.First());
        Assert.AreEqual(DInResponse.ErrorDescription, "Windows Digital Inputs Not Implemented");

        // Same Test for Outputs
        AnalogOutput FakeAnalogOut = new();
        SetOutputResponse AOutResponse = client.SetAnalogOut(FakeAnalogOut);
        Assert.AreEqual(AOutResponse.ErrorDescription, "Output Pin Not Found!");

        SetOutputResponse DOutResponse = client.SetDigitalOut(DOutList.DigitalOutputs.First());
        Assert.AreEqual(DOutResponse.ErrorDescription, "Windows Digital Outputs Not Implemented");

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();
    }

    // private static void TestGeneralMarketCapabilities(IOServiceClient client)
    // {
    //     /*
    //         This Test will verify the IO Pin count for each requested list
    //         based on known RCD hardware capabilities.
    //         The General Market RCD is capable of reading 2 analog and 2 digital input pins,
    //         and setting 2 digital output pins. It does not have Analog Outputs.
    //         The Balboa Varaint has no GPIO.
    //     */
    //     DigitalInputList dInList = client.RequestDigitalInputs();
    //     AnalogInputList aInList = client.RequestAnalogInputs();
    //     AnalogOutputList aOutList = client.RequestAnalogOutputs();
    //     DigitalOutputList dOutList = client.RequestDigitalOutputs();

    //     // Hardware config not the same anymore
    //     // should we have default data???
    //     // This test might not be valid anymore
    //     HardwareConfig config = client.GetHardwareConfiguration();
    //     if (config.HardwareCircuit == "RCD-GM")
    //     {
    //         if (dInList.DigitalInputs.Count != 2)
    //             _testResults.Add("Digital Input Count", $"Failed: expected count: 2, actual count: {dInList.DigitalInputs.Count}");

    //         if (aInList.AnalogInputs.Count != 2)
    //             _testResults.Add("Analog Input Count", $"Failed: expected count: 2, actual count: {aInList.AnalogInputs.Count}");

    //         if (dOutList.DigitalOutputs.Count != 2)
    //             _testResults.Add("Digital Output Count", $"Failed: expected count: 2, actual count: {dOutList.DigitalOutputs.Count}");

    //         if (aOutList.AnalogOutputs.Count != 0)
    //             _testResults.Add("Analog Output Count", $"Failed: expected count: 0, actual count: {aOutList.AnalogOutputs.Count}");
    //     }

    //     else if (config.HardwareCircuit == "RCD-Balboa")
    //     {
    //         if (dInList.DigitalInputs.Count != 0)
    //             _testResults.Add("Digital Input Count", $"Failed: expected count: 0, actual count: {dInList.DigitalInputs.Count}");

    //         if (aInList.AnalogInputs.Count != 0)
    //             _testResults.Add("Analog Input Count", $"Failed: expected count: 0, actual count: {aInList.AnalogInputs.Count}");

    //         if (dOutList.DigitalOutputs.Count != 0)
    //             _testResults.Add("Digital Output Count", $"Failed: expected count: 0, actual count: {dOutList.DigitalOutputs.Count}");

    //         if (aOutList.AnalogOutputs.Count != 0)
    //             _testResults.Add("Analog Output Count", $"Failed: expected count: 0, actual count: {aOutList.AnalogOutputs.Count}");
    //     }

    // }

    // private static void TestInputPinDefualtValues(IOServiceClient client)
    // {
    //     /*
    //         This test will verify known default values for the RCD
    //         General Market IO Pins.
    //         Each AnalogInput is pulled high so should sit around 5V
    //         without any external connection while each Digital Input is
    //         pulled low.
    //     */
    //     DigitalInputList dInList = client.RequestDigitalInputs();
    //     AnalogInputList aInList = client.RequestAnalogInputs();

    //     foreach( AnalogInput a in aInList.AnalogInputs )
    //     {
    //         GetInputResponse response = client.GetAnalogInput(a);
    //         if (response.Value > 5000 || response.Value < 4000)
    //             _testResults.Add("Analog Input Defualt Value", $"Failed: expected value: 4-5V, actual value (in milivolts): {response.Value}");
    //     }

    //     foreach( DigitalInput d in dInList.DigitalInputs )
    //     {
    //         GetInputResponse response = client.GetDigitalInput(d);
    //         if ( response.Value > 1000)
    //             _testResults.Add("Digital Input Defualt Value", $"Failed: expected value: < 1V, actual value (in milivolts): {response.Value}");
    //     }
    // }

    // private static void TestToggleOutputs(IOServiceClient client)
    // {
    //     /*
    //         This test will toggle the digital outputs and verify the return status
    //         of each call. Furthermore, it will verify that the return status of 
    //         a call to toggle the analog outputs describes the error
    //     */

    //     AnalogOutput FakeAnalogOut = new AnalogOutput();
    //     FakeAnalogOut.Pin = 1;
    //     FakeAnalogOut.Name = "FakeAnalogOutput1";
    //     SetOutputResponse FakeResponse = client.SetAnalogOut(FakeAnalogOut);
    //     if ( String.IsNullOrEmpty(FakeResponse.ErrorDescription) )
    //         _testResults.Add("Fake Analog Output Test", $"Failed: no error returned.");

    //     DigitalOutputList dOutList = client.RequestDigitalOutputs();
    //     // will toggle both outputs 4 times
    //     for (int i=0; i<4; i++)
    //     {
    //         for (int j=0; j < dOutList.DigitalOutputs.Count ; j++)
    //         {
    //             // First set LOW
    //             dOutList.DigitalOutputs[j].State = PinState.Low;
    //             SetOutputResponse response = client.SetDigitalOut(dOutList.DigitalOutputs[j]);
    //             if ( !String.IsNullOrEmpty(response.ErrorDescription) )
    //             {
    //                 _testResults.Add("Set Output Pin LOW", $"Failed with error: {response.ErrorDescription}");
    //                 return;
    //             }
    //             Thread.Sleep(1000);     // wait for 1 sec in case user is expecting to see output toggle on a scope

    //             // Then set HIGH
    //             dOutList.DigitalOutputs[j].State = PinState.High;
    //             response = client.SetDigitalOut(dOutList.DigitalOutputs[j]);
    //             if ( !String.IsNullOrEmpty(response.ErrorDescription) )
    //             {
    //                 _testResults.Add("Set Output Pin HIGH", $"Failed with error: {response.ErrorDescription}");
    //                 return;
    //             }
    //             Thread.Sleep(1000);

    //         }
    //     }

    // }

}

