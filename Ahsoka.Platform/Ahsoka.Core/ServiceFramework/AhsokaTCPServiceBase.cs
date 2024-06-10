using Ahsoka.System;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public abstract class AhsokaTCPServiceBase<TTransportIds> : AhsokaServiceBase<TTransportIds, TCPServiceSocket>
    where TTransportIds : Enum
{
    protected AhsokaTCPServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {
    }

    internal static bool IsServiceStarted { get; private set; }

    protected override void OnEndPointConnected()
    {
        base.OnEndPointConnected();
        IsServiceStarted = true;
    }

    protected override void OnEndPointDisconnected()
    {
        base.OnEndPointDisconnected();
        IsServiceStarted = false;
    }
}

#pragma warning restore CS1591
