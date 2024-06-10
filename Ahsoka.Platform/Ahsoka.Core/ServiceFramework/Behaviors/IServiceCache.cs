using Ahsoka.System;
using System;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
public interface IServiceCache
{
    /// <summary>
    /// Default Implementation used by All Services
    /// </summary>
    static IServiceCache Default
    {
        get { return defaultCache; }
        set
        {
            if (defaultCache != null && value != null)
                throw new InvalidOperationException("The Default Cache has been set and can not be re-assigned");
                
            defaultCache = value;
        }
    }

    private static IServiceCache defaultCache;
    

    /// <summary>
    /// Send a Key Value to the ServiceCache
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="publisherId"></param>
    /// <returns></returns>
    int UpdateCacheValue(string serviceName, string key, object value, string publisherId);
}