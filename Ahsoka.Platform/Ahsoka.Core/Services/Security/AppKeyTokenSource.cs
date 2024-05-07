using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Services.Security;

/// <summary>
/// Standard Source Provider which uses the Applications Private Key for signing / validating tokens.
/// </summary>
internal class AppKeyTokenSource : ITokenSource
{
    Dictionary<Guid,TokenInfo> clientInfo = new();
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

    public Token RequestTokens(TokenRequestInfo tokenRequest, byte[] appId, out byte[] requestId )
    {
        // Generate Request Id
        requestId = new Guid().ToByteArray();

        // Verify API Key
        byte[] signature = Convert.FromBase64String(tokenRequest.ApiKey);
        string publicKey = configuration.TokenPublicKey;
        var valid = ECDsaEncryption.VerifyDataSignature(signature, appId, publicKey);
        bool initialTokenState = configuration.RequireChallenge != ChallengeOptions.RequireDisplayChallenge;
        Guid tokenGuid = Guid.NewGuid();

        var clients = tokenRequest.ClientIds.Select(x => new Guid(x)).ToList();

        clientInfo[tokenGuid] = new TokenInfo()
        {
            RequestID = new Guid(requestId),
            ClientIDs = clients,
            Token = tokenGuid,
            ClientCode = tokenRequest.ClientCode,
            IsUnlocked = initialTokenState
        };

        if (valid)
        {
            var returnValue = new Token()
            {
                ClientToken = tokenGuid.ToByteArray(),
                TokenIsValid = initialTokenState,
                RequestId = requestId
            };

            return returnValue;
        }
        return null;
    }

    public Token EnableToken(byte[] requestId, string clientCode)
    {
        var validToken = clientInfo.FirstOrDefault(x => x.Value.RequestID == requestId.ToGuid()).Value;
        if (validToken != null)
        {
            if (validToken.ClientCode == clientCode)
                validToken.IsUnlocked = true;

            return new Token()
            {
                ClientToken = validToken.Token.ToByteArray(),
                TokenIsValid = true,
                RequestId = requestId
            };
        }

        return new Token() { RequestId = requestId };
    }

    public bool IsTokenValid(byte[] clientToken, byte[] clientId)
    {
        if (clientToken == null)
            return false;

        if (clientInfo.TryGetValue(clientToken.ToGuid(),out TokenInfo validToken))
        {
            if (validToken.ClientIDs.Contains(clientId.ToGuid()))
                return validToken.IsUnlocked;
        }

        return false;
    }

    class TokenInfo
    {
        public Guid RequestID { get; set; }
        public List<Guid> ClientIDs { get; set; }
        public Guid Token { get; set; }
        public bool IsUnlocked { get; set; }
        public string ClientCode { get; set; }
    }
}