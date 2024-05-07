using EnovationCloud.Core.Framework;
using SimpleWifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.Services.Security;

/// <summary>
/// Token Source Interface
/// </summary>
public interface ITokenSource
{
    /// <summary>
    /// Generates an API Key using the 
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    string CreateApiKey(byte[] appId);

    /// <summary>
    /// Get a token for the given Endpoint ID
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="apiKey"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Token UnlockEndPoint(byte[] clientId, string apiKey, byte[] appId);

    /// <summary>
    /// Validates a Token Given the Signed Data (clientId)
    /// </summary>
    /// <param name="clientToken"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    bool IsRequestValid(byte[] clientToken, byte[] clientId);
}

/// <summary>
/// Singleton for Application for Token Source 
/// </summary>
public static class TokenSource
{
    /// <summary>
    /// Token Source for all Running Services
    /// </summary>
    public static ITokenSource Default { get; internal set; }

    internal static void RegisterTokenSource(ITokenSource tokenSource)
    {
        if (tokenSource != null && Default != null )
            throw new ArgumentException("Token Source can not be reset");
        Default = tokenSource;
    }
}