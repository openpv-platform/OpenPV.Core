using Ahsoka.Installer;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Ahsoka.Services.Security;

/// <summary>
/// Standard Source Provider which uses the Applications Private Key for signing / validating tokens.
/// </summary>
internal class AppKeyTokenSource : ITokenSource
{
    Dictionary<Guid, KeyValuePair<Guid,bool>> validClients = new();
    SecurityConfiguration configuration;

    public AppKeyTokenSource(SecurityConfiguration configuration, string pathToDeviceInfoFile = null)
    {
        this.configuration = configuration;
    }

    public string CreateApiKey(byte[] appId)
    {
        if (configuration.TokenPublicKey == null)
            throw new ApplicationException("Private Key is not available on target devices, API Keys can only be created by the OpenPV Developer Toolkit.");
        
        var tokenData = ECDsaEncryption.GetSignatureWithKey(appId, configuration.TokenPrivateKey);
        return Convert.ToBase64String(tokenData);
    }

    public Token UnlockEndPoint(byte[] clientId, string apiKey, byte[] appId)
    {
        // Verify API Key
        byte[] signature = Convert.FromBase64String(apiKey);
        string publicKey = configuration.TokenPublicKey;
        var valid = ECDsaEncryption.VerifyDataSignature(signature, appId, publicKey);

        Guid tokenGuid = Guid.NewGuid();
        validClients[new Guid(clientId)] = new KeyValuePair<Guid, bool>(tokenGuid, !configuration.RequireChallenge);

        if (valid)
        {
            return new Token()
            {
                ClientToken = tokenGuid.ToByteArray(),
                ClientId = clientId,
            };
        }

        return null;
    }

    public bool IsRequestValid(byte[] clientToken, byte[] clientId)
    {
        if (clientToken == null)
            return false;

        if (validClients.TryGetValue(new Guid(clientId), out var validToken))
        {
            if (new Guid(clientToken) == validToken.Key)
                return validToken.Value;
        }

        return false;
    }
}