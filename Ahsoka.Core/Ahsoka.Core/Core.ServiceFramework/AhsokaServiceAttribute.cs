using System;

namespace Ahsoka.Core;

/// <summary>
/// Attribute used to Describe Services
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AhsokaServiceAttribute : Attribute
{
    /// <summary>
    /// Name of the Service / Service Configuration
    /// </summary>
    public string ServiceName { get; init; }

    /// <summary>
    /// Constructor for Specifying Service
    /// </summary>
    /// <param name="serviceName"></param>
    public AhsokaServiceAttribute(string serviceName) { ServiceName = serviceName; }
}