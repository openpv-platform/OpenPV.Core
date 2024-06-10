using System;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

// TODO: BMG (Nov. 03, 2023) Create the underlying service base that is going to be used for this class.
/// <summary>
/// 
/// </summary>
public partial class CygnusServerService
{
    /// <summary>
    /// The File data service origin name for streams that are going to be exposed through Cygnus
    /// </summary>
    public const string ReachConfigStreams = "Streams" + ReachConfigSeparator;

    private KeyList StreamValueKeys;

    private void GetStreamValueKeys()
    {
        // Find all the data service values that have an origin indicating that they are streams that are going to
        // be exposed through the cygnus streams api.
        AvailableKeySearchInfo searchInfo = new()
        {
            OriginFilter = CygnusService.Name,
            KeyFilter = ReachConfigStreams,
        };
        StreamValueKeys = DataClient.SearchAvailableKeys(searchInfo, timeoutInMs: DataServiceTimeout);

        if (StreamValueKeys.Keys.Count > 0)
            servicesAvailable |= (UInt32)ServiceIds.Streams;
    }

    // TODO: BMG (Nov. 03, 2023) At the time of writing, this function didn't have a structure that was associated with
    // its request. Need to add it here once there is a structure defined.
    private object ProcessDiscoverStreams(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, object request)
    {
        throw new NotImplementedException();
    }

    // TODO: BMG (Nov. 03, 2023) At the time of writing, this function didn't have a structure that was associated with
    // its request. Need to add it here once there is a structure defined.
    private object ProcessOpenStreams(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, object request)
    {
        throw new NotImplementedException();
    }

    // TODO: BMG (Nov. 03, 2023) At the time of writing, this function didn't have a structure that was associated with
    // its request. Need to add it here once there is a structure defined.
    private object ProcessCloseStreams(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, object request)
    {
        throw new NotImplementedException();
    }

    // TODO: BMG (Nov. 03, 2023) At the time of writing, this function didn't have a structure that was associated with
    // its request. Need to add it here once there is a structure defined.
    private object ProcessStreamDataNotification(CygnusInterface interfaceType, AhsokaMessageHeader messageHeader, object request)
    {
        throw new NotImplementedException();
    }
}
