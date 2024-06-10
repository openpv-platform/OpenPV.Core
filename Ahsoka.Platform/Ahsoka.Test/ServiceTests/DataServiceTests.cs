using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.Services.System;
using Ahsoka.System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using Ahsoka.Services.IO;
using ShimSkiaSharp;

namespace Ahsoka.Test;

[TestClass]
public class DataServiceTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestDataService()
    {
        //var config = ConfigurationLoader.CreateDefaultConfiguration();
        //config.DataServiceEnabled =  ServiceEnableDisable.Enabled;
        //ConfigurationLoader.SetRuntimeConfiguration(config);

        // Start IO Service
        // Start client for NetworkService
        var dataClient = new DataServiceClient();
     
        // Create a System Service and Start it.
        // We only start this so we have Service Data in the Global Data Service
        var client = new SystemServiceClient(); 
        client.GetHardwareInformation();

        // Get All Keys
        var keys = dataClient.SearchAvailableKeys(new AvailableKeySearchInfo() { OriginFilter = SystemService.Name, MininumAccessLevel = DataAccessLevel.Public });
        Assert.IsTrue(keys.Keys.Count == 0);

        // Get All Keys
        keys = dataClient.SearchAvailableKeys(new AvailableKeySearchInfo() { OriginFilter = SystemService.Name, KeyFilter = SystemServiceDataKeys.Available });
        Assert.IsTrue(keys.Keys.Count == 1);

        // Ask Data Service for Known Values in the SystemService
        var keyInfo = keys.GetKeyInfo(SystemService.Name, SystemServiceDataKeys.Available);
        Assert.IsTrue(keyInfo != null);

        var keyValues = dataClient.GetKeyValues(keyInfo.ToKeyList());
        Assert.IsTrue(keyValues.Values.First().BooleanValue == true);

        string guidOrigin = Guid.NewGuid().ToString(); // Set a Custom Origin so we can subscribe

        var floatKI = new KeyInfo() { Origin = guidOrigin, PublisherId= guidOrigin, Key = "FloatValue", MininumAccessLevel = DataAccessLevel.Diagnostic };
        var intKI = new KeyInfo() { Origin = guidOrigin, PublisherId = guidOrigin, Key = "IntegerValue" };
        var stringKI = new KeyInfo() { Origin = guidOrigin, PublisherId = guidOrigin, Key = "StringValue" };
        var uintKI = new KeyInfo() { Origin = guidOrigin, PublisherId = guidOrigin, Key = "UnsignedIntegerValue" };
        var dataKI = new KeyInfo() { Origin = guidOrigin, PublisherId = guidOrigin, Key = "DataValue" };

        // Test Setting and Getting Each Key Type...Test Single First.
        dataClient.SetKeyValues(new KeyValue { FloatValue = 1234.0, KeyInfo = floatKI }.ToKeyValueList());

        // Set Values for Others using Single Collection Style Call
        var keyValueList = new KeyValueList();
        keyValueList.Values.AddRange(new KeyValue[]
        {
            new KeyValue { IntegerValue = 1234, KeyInfo = intKI },
            new KeyValue { StringValue = "1234.0", KeyInfo = stringKI },
            new KeyValue { UnsignedIntegerValue = 1234, KeyInfo = uintKI },
            new KeyValue { DataValue = new byte[] { 1, 2, 3, 4 }, KeyInfo = dataKI }
        });

        dataClient.SetKeyValues(keyValueList);

        // Now Check All Values
        var keyList = new KeyList();
        keyList.Keys.AddRange(new KeyInfo[] { floatKI, intKI, stringKI, uintKI, dataKI });

        // Get Values and Validate
        keyValues = dataClient.GetKeyValues(keyList);
        Assert.IsTrue(keyValues.Values.First(x => x.KeyInfo.Key == floatKI.Key).FloatValue == 1234.0);
        Assert.IsTrue(keyValues.Values.First(x => x.KeyInfo.Key == intKI.Key).IntegerValue == 1234);
        Assert.IsTrue(keyValues.Values.First(x => x.KeyInfo.Key == stringKI.Key).StringValue == "1234.0");
        Assert.IsTrue(keyValues.Values.First(x => x.KeyInfo.Key == uintKI.Key).UnsignedIntegerValue == 1234);
        Assert.IsTrue(keyValues.Values.First(x => x.KeyInfo.Key == dataKI.Key).DataValue[3] == 4);

        // Subscriptions
        var subscriptionList = new KeySubscriptionRequest();
        subscriptionList.SubscribeToKeys.AddRange(new KeyInfo[] { floatKI, intKI, stringKI, uintKI, dataKI });
        var subscriberInfo = dataClient.SubscribeToKeys(subscriptionList);

        // Notify a Key and See if We Recieve Notification
        var resetEvent = new AutoResetEvent(false);
        dataClient.NotificationReceived += (sender, notificationInfo) =>
        {
            if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
            {
                KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
                if (notif.KeyValues.FirstOrDefault().KeyInfo.Key == floatKI.Key && notif.KeyValues.FirstOrDefault().FloatValue == 4321.0)
                    resetEvent.Set();
            }
        };

        // Update Key and wait for Response
        var value = new KeyValue { FloatValue = 4321, KeyInfo = floatKI  }.ToKeyValueList();
       
        dataClient.SetKeyValues(value);

        // Wait for Notification to Arrive
        resetEvent.WaitOne();

        // Get Subscription List
        var subscriptionConfirmation = dataClient.SearchAvailableKeys(new AvailableKeySearchInfo() { SubscriberIdFilter = subscriberInfo.SubscriberId });
        Assert.IsTrue(subscriptionConfirmation.Keys.FirstOrDefault(x => x.Key == floatKI.Key) != null);
        Assert.IsTrue(subscriptionConfirmation.Keys.Count == 5); // Should be ONLY the Items We subscribed To above

        // Unsubscribe to Keys
        subscriptionList.SubscriberId = subscriberInfo.SubscriberId;
        dataClient.UnsubscribeToKeys(subscriptionList);

        // Verify We Disconnected
        subscriptionConfirmation = dataClient.SearchAvailableKeys(new AvailableKeySearchInfo() { SubscriberIdFilter = subscriberInfo.SubscriberId });
        Assert.IsTrue(subscriptionConfirmation.Keys.Count == 0); // Should be ONLY the Items We subscribed To above

        // Stop the Runtimes
        AhsokaRuntime.ShutdownAll();
    }

   // [TestMethod]
    public void TestDataServiceIOValues()
    {

        // Start clients
        var dataClient = new DataServiceClient();
        var ioclient = new IOServiceClient();
        AhsokaRuntime.CreateBuilder()
                   .AddClients(dataClient, ioclient)
                   .StartWithInternalServices();

        // Get IO Key Values 
        var keys = dataClient.SearchAvailableKeys(new AvailableKeySearchInfo() { OriginFilter = IOService.Name });
        Assert.IsTrue(keys.Keys.Count >= 1);

        // Ask Data Service for Known Values in the SystemService
        var keyInfo = keys.GetKeyInfo(IOService.Name, SystemServiceDataKeys.Available);
        Assert.IsTrue(keyInfo != null);

        var keyValues = dataClient.GetKeyValues(keyInfo.ToKeyList());
        Assert.IsTrue(keyValues.Values.First().BooleanValue == true);

        // Subscriptions
        var subscriptionList = new KeySubscriptionRequest();
        //subscriptionList.SubscribeToKeys.AddRange(new KeyInfo[] { floatKI, intKI, stringKI, uintKI, dataKI });
        //var subscriberInfo = dataClient.SubscribeToKeys(subscriptionList);

        // Notify a Key and See if We Recieve Notification
        AutoResetEvent resetEvent = new (false);
        dataClient.NotificationReceived += (sender, notificationInfo) =>
        {
            KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
           // if (notif.KeyValues.FirstOrDefault().KeyInfo.Key == floatKI.Key && notif.KeyValues.FirstOrDefault().FloatValue == 4321.0)
           //     resetEvent.Set();
        };

       
        // Wait for Notification to Arrive
        resetEvent.WaitOne();

        // Stop the Runtimes
        AhsokaRuntime.ShutdownAll();

    }
}

  

