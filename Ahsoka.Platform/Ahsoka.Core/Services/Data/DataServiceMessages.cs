using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Data;

internal class DataServiceMessages : AhsokaMessagesBase
{
    public DataServiceMessages()
    {
        this.RegisterServiceRequest(DataMessageTypes.Ids.SearchAvailableKeys, typeof(AvailableKeySearchInfo), typeof(KeyList));
        this.RegisterServiceRequest(DataMessageTypes.Ids.GetKeyValues, typeof(KeyList), typeof(KeyValueList));
        this.RegisterServiceRequest(DataMessageTypes.Ids.SetKeyValues, typeof(KeyValueList), typeof(EmptyNotification));
        this.RegisterServiceRequest(DataMessageTypes.Ids.SubscribeToKeys, typeof(KeySubscriptionRequest), typeof(KeySubscriberInfo));
        this.RegisterServiceRequest(DataMessageTypes.Ids.UnsubscribeToKeys, typeof(KeySubscriptionRequest), typeof(EmptyNotification));
        this.RegisterServiceNotification(DataMessageTypes.Ids.NotifyKeyValueChanged, typeof(KeyValueNotification));
        this.RegisterServiceNotification(DataMessageTypes.Ids.NotifyKeyAdded, typeof(KeyList));
    }
}
