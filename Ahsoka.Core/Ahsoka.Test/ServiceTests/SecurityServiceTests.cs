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
    public void TestBaseSecurityService()
    {
        // Test Data
        var key = ECDsaEncryption.GenerateNewKeyPair();
        var appid = Guid.NewGuid().ToByteArray(); // Real app uses TeamID / APPID
        var devicePublicKeyFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.ServiceSecurityFile);

        // Test the ECDsaEncryption Functions
        TestEncryption(key, appid);
      
        // Create Device and Service KeyFiles
        var keyInfo = CreateKeyFiles(key, appid,  ChallengeOptions.NotRequired, devicePublicKeyFile);
     
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
        var request = securityServiceClient.RequestToken(new TokenRequestInfo()
        {
            ApiKey = apiKey,
            ClientIds = { ((IAhsokaClientEndPoint)systemClient).GetClientId() }
        });

        // Wait for Token top be Recieved
        var token = securityServiceClient.WaitForToken(request.RequestId);
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
        catch 
        { 
            Assert.Fail("Should Succeed With Token"); 
        }


        // Turn off Security for later tests.
        TokenSource.RegisterTokenSource(null);
        systemConfig.Behaviors = EndPointBehaviors.AutoStart;

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();

        File.Delete(devicePublicKeyFile);

    }

    [TestMethod]
    public void TestChallengeSecurityService()
    {
        var key = ECDsaEncryption.GenerateNewKeyPair();
        var appid = Guid.NewGuid().ToByteArray(); // Real app uses TeamID / APPID
        var devicePublicKeyFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.ServiceSecurityFile);
        string challengeCode = Random.Shared.Next(100000, 999999).ToString(); // Challenge Code

        // Create Device and Service KeyFiles
        var config = CreateKeyFiles(key, appid, ChallengeOptions.RequireDisplayChallenge, devicePublicKeyFile);

        // Create Default Token Source
        TokenSource.Default = new AppKeyTokenSource(config);

        // Test Authorization Method 1
        // Authorize the Service using the Device Private Key....used for 
        // used for scenarios where sharing the devices key is reasonable (Mobile, Toolkit).
        string apiKey = TokenSource.Default.CreateApiKey(config.ApplicationId);

        // Clear Token Source
        TokenSource.RegisterTokenSource(null);

        // Enable Security on the System Service.
        var systemConfig = ConfigurationLoader.GetServiceConfig(SystemService.Name);
        systemConfig.Behaviors = EndPointBehaviors.AutoStart | EndPointBehaviors.SecurityEnabled;

        // Create Service Clients for Testing
        var securityServiceClient = new SecurityServiceClient();
        var systemClient = new SystemServiceClient(systemConfig);

        // Start client for NetworkService to test Real Calls
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                     .AddClients(securityServiceClient, systemClient)
                     .StartWithInternalServices();


        // Handle the Challenge Callback
        bool generateSecret = false;
        securityServiceClient.NotificationReceived += (sender, args) =>
        {
            if (args.NotificationObject is ChallengeRequestInfo info)
            {
                // Complete Challenge
                securityServiceClient.CompleteChallenge(new ChallengeCompleteInfo()
                {
                    ClientCode = info.ClientCode,
                    RequestId = info.RequestId,
                    AcceptRequest = true,
                    ClientIdSigned = generateSecret ? securityServiceClient.GetLocalSecret() : null // Returns Locally signed ClientID, Required for Accept.
                });
            }
        };

        // Begin By Requesting a Token for the SystemService from the Security Service
        var request = securityServiceClient.RequestToken(new TokenRequestInfo()
        {
            ApiKey = apiKey,
            ClientIds = { ((IAhsokaClientEndPoint)systemClient).GetClientId() },
            ClientCode = challengeCode,
            ClientDescription = "TestClient"
        });

        // Wait for Token top be Recieved..this token will be invalid
        var systemServiceToken = securityServiceClient.WaitForToken(request.RequestId);
        Assert.IsFalse(systemServiceToken.TokenIsValid);

        // Enable Local secret
        generateSecret = true;

        // Begin By Requesting a Token for the SystemService from the Security Service.
        request = securityServiceClient.RequestToken(new TokenRequestInfo()
        {
            ApiKey = apiKey,
            ClientIds = { ((IAhsokaClientEndPoint)systemClient).GetClientId() },
            ClientCode = challengeCode,
            ClientDescription = "TestClient"
        });

        // Wait for Token top be Recieved...this one WILL work
        systemServiceToken = securityServiceClient.WaitForToken(request.RequestId);

        // Validate Token
        Assert.IsNotNull(systemServiceToken);
        Assert.IsTrue(systemServiceToken.TokenIsValid);

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
            systemClient.SetToken(systemServiceToken);
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
    }

    private static SecurityConfiguration CreateKeyFiles(KeyValuePair<string, string> key, byte[] appId, ChallengeOptions challenge, string configurationFile)
    {
        SecurityConfiguration config = new SecurityConfiguration()
        {
            ApplicationId = appId,
            RequireChallenge = challenge,
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
        var authRequest = TokenSource.Default.RequestTokens(new TokenRequestInfo()
        {
            ApiKey = apiKey,
            ClientCode = "12345",
            ClientDescription = "Test Client",
            ClientIds = { clientId }

        }, config.ApplicationId, out byte[] _);

        // Validate Token
        var validResult = TokenSource.Default.IsTokenValid(authRequest.ClientToken, clientId);
        Assert.IsTrue(validResult);

        return apiKey;
    }
}

