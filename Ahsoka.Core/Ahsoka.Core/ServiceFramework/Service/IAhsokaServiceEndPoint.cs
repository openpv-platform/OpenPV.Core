namespace Ahsoka.ServiceFramework;


/// <summary>
/// Interface for AhsokaService EndPoints
/// </summary>
public interface IAhsokaServiceEndPoint : IAhsokaEndPoint
{
    /// <summary>
    /// Messages Received by Service
    /// </summary>
    int MessageReceiveCount { get;  }

    /// <summary>
    /// Messages Sent by Service
    /// </summary>
    int MessageSendCount { get;  }

    /// <summary>
    /// Method allowing EndPoints to Receive Service Messages from a ServiceSocket
    /// </summary>
    /// <param name="serviceSocket"></param>
    /// <param name="message"></param>
    void HandleServiceRequest(IAhsokaServiceSocket serviceSocket, AhsokaServiceMessage message);
}

