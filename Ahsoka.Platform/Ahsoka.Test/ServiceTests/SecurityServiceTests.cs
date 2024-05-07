using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Security;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static Ahsoka.Services.Can.J1939Helper;

namespace Ahsoka.Test;

[TestClass]
public class SecurityServiceTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestSecurityService()
    {
        // Test Data
        var key = ECDsaEncryption.GenerateNewKeyPair();
        var appid = Guid.NewGuid().ToByteArray(); // Real app uses TeamID / APPID
        var devicePublicKeyFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.ServiceSecurityFile);

        // Test the ECDsaEncryption Functions
        TestEncryption(key, appid);
      
        // Create Device and Service KeyFiles
        var keyInfo = CreateKeyFiles(key, appid, devicePublicKeyFile);
     
        //Test TokenSource (AppTokenSource) used by Services.
        string apiKey = TestTokenSource(keyInfo, Guid.NewGuid().ToByteArray());

        // Clear Token Source
        TokenSource.RegisterTokenSource(null);

        // Enable Security on the System Service.
        var systemConfig = ConfigurationLoader.GetServiceConfig(SystemService.Name);
        systemConfig.Behaviors =  EndPointBehaviors.AutoStart | EndPointBehaviors.SecurityEnabled;

        // Create Service Clients for Testing
        var securityServiceClient = new SecurityServiceClient();
        var systemClient = new SystemServiceClient(systemConfig);

        // Start client for NetworkService to test Real Calls
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                     .AddClients(securityServiceClient, systemClient)
                     .StartWithInternalServices();

        // Begin By Requesting a Token for the SystemService from the Security Service
        var token = securityServiceClient.RequestToken(new TokenRequestInfo()
        {
            ApiKey = apiKey,
            ClientId = systemClient.GetClientId()
        });

        Assert.IsNotNull(token);

        // Test Accessing the System Service without the Token
        // This will throw an exception
        try
        {
            var info = systemClient.GetHardwareInformation();
            Assert.Fail("Should Fail Without Token");
        }
        catch { }
        

        // Set the Token, we should now have access to the Service
        try
        {
            systemClient.SetToken(token);
            var info = systemClient.GetHardwareInformation();
        }
        catch { Assert.Fail("Should Succeed With Token"); }


        // Turn off Security for later tests.
        TokenSource.RegisterTokenSource(null);
        systemConfig.Behaviors = EndPointBehaviors.AutoStart;

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();

        File.Delete(devicePublicKeyFile);


        /* // Test Authorization Method 2
        // Authorize using a Challenge Response.   
        // Used for scenarios where we want to provide local "Acceptance" of communication similar to BT, etc.
        Token challengeToken = null;
        AutoResetEvent waitForChallengeAccept = new AutoResetEvent(false);
        securityServiceClient.NotificationReceived += (sender, args) =>
        {
            if (args.NotificationObject is ChallengeRequestInfo info)
            {
                // Get Token Request on behalf of Client - Normally would have a UI Here.
                /// var authRequest = TokenSource.Default.CreateSignedTokenRequest(info.EndpointId);

                 // Complete Challenge
                 securityServiceClient.CompleteChallenge(new ChallengeCompleteInfo()
                 {
                     //ClientCode = info.ClientCode,
                    // TokenRequest = authRequest
                 });
            }
            else if (args.NotificationObject is Token token)
            {
                // Accept Challenge Token and Release WaitEvent
                challengeToken = token;
                waitForChallengeAccept.Set();
            }
        };


        // Wait for Token
        waitForChallengeAccept.WaitOne();


      
        */

        //systemClient.SetToken();


        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();

        File.Delete(devicePublicKeyFile);
    }

    private static SecurityConfiguration CreateKeyFiles(KeyValuePair<string, string> key, byte[] appId, string configurationFile)
    {
        SecurityConfiguration config = new SecurityConfiguration()
        {
            ApplicationId = appId,
            RequireChallenge = false,
            TokenPublicKey = key.Value,
            TokenPrivateKey = key.Key
        };


        // Create App Signing Key
        var appDir = Path.GetDirectoryName(configurationFile);
        if (!Directory.Exists(appDir))
            Directory.CreateDirectory(appDir);
        
        // Write Key File
        File.WriteAllText(configurationFile, JsonUtility.Serialize(config));

        return config;
    }

    private static void TestEncryption(KeyValuePair<string, string> key, byte[] appid)
    {
        var result = ECDsaEncryption.GetSignatureWithKey(appid, key.Key);
        string resultString = Convert.ToBase64String(result);
        result = Convert.FromBase64String(resultString);
        var valid = ECDsaEncryption.VerifyDataSignature(result, appid, key.Value);
        Assert.IsTrue(valid);
    }

    private static string TestTokenSource(SecurityConfiguration config, byte[] clientId)
    {
        // Create Default Token Source
        TokenSource.Default = new AppKeyTokenSource(config);

        // Test Authorization Method 1
        // Authorize the Service using the Device Private Key....used for 
        // used for scenarios where sharing the devices key is reasonable (Mobile, Toolkit).
        string apiKey = TokenSource.Default.CreateApiKey(config.ApplicationId);

        // Get a Token for the Client - Happens in Customer App or External Client
        // This version a 
        var authRequest = TokenSource.Default.UnlockEndPoint(clientId, apiKey, config.ApplicationId);

        // Validate Token
        var validResult = TokenSource.Default.IsRequestValid(authRequest.ClientToken, clientId);
        Assert.IsTrue(validResult);

        return apiKey;
    }
}

