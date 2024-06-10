#pragma warning disable CS1591
#pragma warning disable CS0618 // Type or member is obsolete

using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Ahsoka.Services.Iot;


[AhsokaService(IotService.Name)]
public class IotService : AhsokaTCPServiceBase<IotMessageTypes.Ids>
{
    #region Fields
    public const string Name = "IotService";
    readonly Dictionary<string, InternalClient> clients = new();
    #endregion

    #region Methods
    public IotService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
    }

    public IotService(ServiceConfiguration config) :
        base(config, new IotServiceMessages())
    {
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<IotMessageTypes.Ids>())
        {
            case IotMessageTypes.Ids.CreateClient:

                HandleCreateClient(messageHeader, message as ClientInfo);
                break;

            case IotMessageTypes.Ids.DestroyClient:

                HandleDestroyClient(messageHeader, message as ClientMessage);
                break;

            case IotMessageTypes.Ids.SubscribeTopic:

                HandleTopic(messageHeader, message as TopicSubscription, true);
                break;

            case IotMessageTypes.Ids.UnsubscribeTopic:

                HandleTopic(messageHeader, message as TopicSubscription, false);
                break;

            case IotMessageTypes.Ids.PublishTopic:

                HandlePublishTopic(messageHeader, message as TopicMessage);
                break;
        }
    }

    private void HandlePublishTopic(AhsokaMessageHeader messageHeader, TopicMessage topicMessage)
    {
        TopicResult result = null;
        var client = GetClient(topicMessage.ClientId);
        try
        {
            client.NativeClient.PublishBinaryAsync(topicMessage.TopicName, topicMessage.Payload);
            result = new TopicResult() { ClientId = topicMessage.ClientId, Status = TopicResultStatus.Success };
        }
        catch (Exception ex)
        {
            result = new TopicResult() { ClientId = topicMessage.ClientId, Status = TopicResultStatus.Error, ResultText = ex.Message };
        }

        SendMessageInternal(messageHeader, result);
    }

    private void HandleTopic(AhsokaMessageHeader messageHeader, TopicSubscription topicSubscription, bool subscribe)
    {
        TopicResult result = null;
        var client = GetClient(topicSubscription.ClientId);
        try
        {
            if (client.State == ClientState.Connected)
            {
                if (subscribe)
                    client.NativeClient.SubscribeAsync(topicSubscription.TopicName, (MqttQualityOfServiceLevel)(int)topicSubscription.QualityOfService);
                else
                    client.NativeClient.UnsubscribeAsync(topicSubscription.TopicName);

                result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Success, ResultText = "Success" };
            }
            else
                result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Error, ResultText = "Not Connected" };

        }
        catch (Exception ex)
        {
            result = new TopicResult() { ClientId = topicSubscription.ClientId, Status = TopicResultStatus.Error, ResultText = ex.Message };
        }

        SendMessageInternal(messageHeader, result);
    }

    private void HandleCreateClient(AhsokaMessageHeader messageHeader, ClientInfo clientInfo)
    {
        // Create Tracker
        var internalClient = CreateClientTracker(messageHeader, clientInfo);

        // Connect to Server
        internalClient.NativeClient = new MqttFactory().CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
             .WithClientId(internalClient.ClientId)
             .WithCleanSession(true)
             .WithCredentials(internalClient.ClientInfo.DeviceUserName, internalClient.ClientInfo.DevicePassword)
             .WithTcpServer(internalClient.ClientInfo.BrokerAddress, internalClient.ClientInfo.BrokerPort)
             .WithTimeout(TimeSpan.FromSeconds(5));

        if (clientInfo.UseCustomTlsCert)
        {
            if (!File.Exists(clientInfo.CustomTlsRootCAPath)
                || !File.Exists(clientInfo.CustomTlsPublicKeyPath)
                || !File.Exists(clientInfo.CustomTlsCertPrivateKeyPath))
            {
                SendConnectionResult(messageHeader, ClientState.ConnectionError, internalClient, "Missing Certificate Files, Please pass CAFile (.pem), CertPublic(.pem) and CertPrivateKey(.pvcks8");
                return;
            }


            // Certificate based authentication
            var caCert = new X509Certificate2(internalClient.ClientInfo.CustomTlsRootCAPath);
            var publicKey = new X509Certificate2(internalClient.ClientInfo.CustomTlsPublicKeyPath);

            var rsa = RSA.Create();
            var data = File.ReadAllBytes(internalClient.ClientInfo.CustomTlsCertPrivateKeyPath);
            rsa.ImportPkcs8PrivateKey(data, out int bytesRead);

            var keyPair = publicKey.CopyWithPrivateKey(rsa);
            var clientCert = new X509Certificate2(keyPair.Export(X509ContentType.Pfx));
            var tlsOptions = new MqttClientOptionsBuilderTlsParameters()
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                Certificates = new List<X509Certificate>()
                {
                    clientCert, caCert
                },
            };

            tlsOptions.CertificateValidationHandler += (certContext) =>
            {
                // Since we are using the 
                X509Chain chain = new();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                chain.ChainPolicy.VerificationTime = DateTime.Now;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
                chain.ChainPolicy.CustomTrustStore.Add(caCert);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                var x5092 = new X509Certificate2(certContext.Certificate);
                var result = chain.Build(x5092);
                return result;
            };

            optionsBuilder = optionsBuilder.WithTls(tlsOptions);
        }
        else
        {
            optionsBuilder = optionsBuilder.WithTls();
        }

        var options = optionsBuilder.Build();

        internalClient.NativeClient.ApplicationMessageReceivedAsync += (e) =>
        {
            var reply = CreateReplyMessageHeader(messageHeader, IotMessageTypes.Ids.TopicReceivedNotification);

            var notif = new TopicMessage()
            {
                ClientId = e.ClientId,
                TopicName = e.ApplicationMessage.Topic,
                Payload = e.ApplicationMessage.PayloadSegment.ToArray(),
            };

            SendMessageInternal(reply, notif, true);

            return Task.CompletedTask;
        };

        try
        {

            var connectResult = internalClient.NativeClient.ConnectAsync(options).GetAwaiter().GetResult();

            if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                SendConnectionResult(messageHeader, ClientState.Connected, internalClient);
            else
            {
                SendConnectionResult(messageHeader, ClientState.ConnectionError, internalClient, connectResult.ReasonString.ToString());
                clients.Remove(internalClient.ClientId);
            }
        }
        catch (Exception ex)
        {
            SendConnectionResult(messageHeader, ClientState.ConnectionError, internalClient, ex.Message);
            clients.Remove(internalClient.ClientId);

        }
    }

    private void HandleDestroyClient(AhsokaMessageHeader messageHeader, ClientMessage clientMessage)
    {
        // Get Client
        var client = GetClient(clientMessage.ClientId);
        if (client != null)
        {
            try
            {
                var result = client.NativeClient.DisconnectAsync();
            }
            catch { }

            clients.Remove(client.ClientId);
            SendConnectionResult(messageHeader, ClientState.Disconnected, client);
        }
        else
            SendMessageInternal(messageHeader);
    }

    private void SendConnectionResult(AhsokaMessageHeader header, ClientState state, InternalClient client, string resultText = null)
    {
        client.State = state;

        var notif = new ClientConnectionInfo()
        {
            ClientId = client.ClientId,
            ClientState = state,
            BrokerAddress = client.ClientInfo.BrokerAddress,
            BrokerPort = client.ClientInfo.BrokerPort,
            ResultText = resultText
        };

        SendMessageInternal(header, notif);
    }

    private InternalClient GetClient(string clientId)
    {
        return clients.GetValueOrDefault(clientId);
    }

    private InternalClient CreateClientTracker(AhsokaMessageHeader header, ClientInfo clientInfo)
    {
        var data = new InternalClient()
        {
            ClientId = new Guid(header.ClientId).ToString(), // Use Client ID for Cloud Clients too.
            ClientInfo = clientInfo,
            NativeClient = null,
            State = ClientState.Connected,
        };

        clients[data.ClientId] = data;

        return data;
    }


    #endregion

    internal class InternalClient
    {
        public string ClientId { get; set; }
        public ClientState State { get; set; }
        public ClientInfo ClientInfo { get; set; }
        public IMqttClient NativeClient { get; set; }
    }
}


