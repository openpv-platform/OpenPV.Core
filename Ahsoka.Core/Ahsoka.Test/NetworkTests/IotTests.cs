using Ahsoka.ServiceFramework;
using Ahsoka.Services.Iot;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class IotTests : LinearTestBase
{
    public TestContext TestContext { get; set; }
    Services.Iot.ClientMessage topicMessageResult = null;
    const string topic = "AhsokaTestTopic";
    const string url = "18144f318ca0488dacc077721d7778ee.s2.eu.hivemq.cloud";
    const int port = 8883;
    const string deviceName = "AhsokaDevice";
    const string devicePassword = "bedzyq-cimwez-Nobxe0";
    const string badPassword = "badPassword";

    readonly AutoResetEvent messageEvent = new(false);

    [TestMethod]
    public void TestSpaMQtt()
    {
        // Start client for NetworkService
        var client = new IotServiceClient();
        client.NotificationReceived += Client_NotificationReceived;
        client.Start();

        // Create Client for Spa
        var clientInfo = new ClientInfo()
        {
            BrokerAddress = "mqttprod.controlmyspa.com",
            BrokerPort = port,
            UseCustomTlsCert = true, // Use Custom PEM and PKCS8 Files for 
            CustomTlsRootCAPath = @"Ahsoka\Resources\exampleCert\ca_root_cert.pem",
            CustomTlsPublicKeyPath = @"Ahsoka\Resources\exampleCert\gateway_cert.pem",
            CustomTlsCertPrivateKeyPath = @"Ahsoka\Resources\exampleCert\gateway_key.pkcs8"
        };

        // Connect and Validate Failure
        var clientState = client.CreateClient(clientInfo);
        if (clientState.ClientState == ClientState.Connected)
        {
            TestSubscription(client, clientState);

            TestDisconnect(client, clientState);
        }

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }


    [TestMethod]
    public void TestIotService()
    {
        // Start client for NetworkService
        var client = new IotServiceClient();
        client.NotificationReceived += Client_NotificationReceived; 
        client.Start();

        var clientState = TestConnections(client);

        if (clientState.ClientState == ClientState.Connected)
        {
            TestSubscription(client, clientState);

            TestDisconnect(client, clientState);
        }

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    private void TestDisconnect(IotServiceClient client, ClientConnectionInfo clientState)
    {
        // Disconnect
        var disconnect = client.DestroyClient(new Services.Iot.ClientMessage() { ClientId = clientState.ClientId });
        Assert.IsTrue(disconnect.ClientState == ClientState.Disconnected);

        // Cover Null Case
        client.DestroyClient(new Services.Iot.ClientMessage() { ClientId = string.Empty });
    }

    private void TestSubscription(IotServiceClient client, ClientConnectionInfo clientState)
    {
        var result = client.SubscribeTopic(new TopicSubscription()
        {
            ClientId = clientState.ClientId,
            TopicName = topic,
            QualityOfService = TopicQualityOfService.ExactlyOnce
        });

        if (result.Status != TopicResultStatus.Success)
            Console.WriteLine(result.ResultText);

        result = client.PublishTopic(new TopicMessage()
        {
            TopicName = topic,
            ClientId = clientState.ClientId,
            PayloadString = JsonUtility.Serialize(new Services.Iot.ClientMessage() { ClientId = clientState.ClientId })
        });

        if (result.Status != TopicResultStatus.Success)
            Console.WriteLine(result.ResultText);


        messageEvent.WaitOne();

        client.UnSubscribeTopic(new TopicSubscription()
        {
            ClientId = clientState.ClientId,
            TopicName = topic,
            QualityOfService = TopicQualityOfService.ExactlyOnce
        });

        if (result.Status != TopicResultStatus.Success)
            Console.WriteLine(result.ResultText);


        Assert.AreEqual(topicMessageResult.ClientId, clientState.ClientId);
    }

    private ClientConnectionInfo TestConnections(IotServiceClient client)
    {
        // Create Client
        var clientInfo = new ClientInfo()
        {
            BrokerAddress = url,
            BrokerPort = port,
            DeviceUserName = deviceName,
            DevicePassword = badPassword
        };

        // Connect and Validate Failure
        var badResult = client.CreateClient(clientInfo);
        Assert.IsTrue(badResult.ClientState != ClientState.Connected);

        // Fix Password and ReConnect...wait for ConnectedState
        clientInfo.DevicePassword = devicePassword;
        var goodResult = client.CreateClient(clientInfo);
        Assert.IsTrue(goodResult.ClientState == ClientState.Connected);
        Assert.IsTrue(goodResult.ClientId != String.Empty);

        return goodResult;
    }

    private void Client_NotificationReceived(object sender, NotificationEventArgs<IotMessageTypes.Ids> e)
    {
        if (e.TransportId == IotMessageTypes.Ids.TopicReceivedNotification)
        {
            TopicMessage result = e.NotificationObject as TopicMessage;
            topicMessageResult = JsonUtility.Deserialize<Services.Iot.ClientMessage>(result.PayloadString);
            messageEvent.Set();
        }
    }
}
