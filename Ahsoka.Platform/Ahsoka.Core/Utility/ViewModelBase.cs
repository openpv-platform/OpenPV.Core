using Ahsoka.Services.Data;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ahsoka.Utility;

/// <summary>
/// This attributes contains basic information that is going to be used by the data service when writing and reading
/// values.
/// </summary>
public class DataServiceInfoAttribute : Attribute
{
    /// <summary>
    /// The Origin Name used for the event in the data service.
    /// </summary>
    public string Origin { get; private set; }

    /// <summary>
    /// A boolean indicating whether or not to subscribe to related values.
    /// </summary>
    public bool Subscribe { get; private set; }

    /// <summary>
    /// Constructor that takes the name of the origin that is going to be used and whether or not we should subscribe to
    /// values that use this origin.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="subscribe"></param>
    public DataServiceInfoAttribute(string origin, bool subscribe = false)
    {
        Origin = origin;
        Subscribe = subscribe;
    }
}

/// <summary>
/// The view model property attribute that should be inherited from byt child classes based on the ViewModelBase
/// </summary>
public class ViewModelPropertyBaseAttribute : Attribute
{
}

/// <summary>
/// Base View Model used in Ahsoka Applications which adds basic INotifyPropertyChanged support.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged, IHasParent
{
    IHasParent IHasParent.Parent => null;

    /// <summary>
    /// Event Handler raised when a Property has Changed or OnPropertyChanged() is called.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private DataServiceClient dataClient = null;
    /// <summary>
    /// The data service client that is going to be used to write the values.
    /// </summary>
    public DataServiceClient DataClient
    {
        get
        {
            return dataClient;
        }
        set
        {
            dataClient = value;

            DataServiceInfoAttribute DSInfo =
                (DataServiceInfoAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DataServiceInfoAttribute));
            bool subscribeToValue = DSInfo != null && DSInfo.Subscribe;

            foreach (var item in GetType().GetProperties())
            {
                var attribute = item.GetCustomAttribute<ViewModelPropertyBaseAttribute>();
                if (attribute != null)
                {
                    KeyValue keyValue = GenerateKeyValue(item.GetValue(this), item.Name);
                    OnSetDataServiceValue(keyValue);
                    if (subscribeToValue)
                        OnSubscribeDataServiceValue(keyValue.KeyInfo);
                }
            }
        }
    }

    string DataServiceOriginName;

    /// <summary>
    /// The base abstract class that should be used for any view models in the project.
    /// </summary>
    public ViewModelBase()
    {
        DataServiceInfoAttribute originNameAttribute =
            (DataServiceInfoAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DataServiceInfoAttribute));
        if (originNameAttribute != null)
            DataServiceOriginName = originNameAttribute.Origin;
        else
            DataServiceOriginName = GetType().Name;
    }

    /// <summary>
    /// Raises the PropertyChanged event for the Member Specified
    /// </summary>
    /// <param name="memberName">Member Name which changed [Defaults to Caller Name]</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string memberName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }

    /// <summary>
    /// Sets the value given a member name into the data service.
    /// </summary>
    /// <param name="newValue">The new value that is going to be assigned in the data service.</param>
    /// <param name="keyString">The key string of the value that is going to be assigned.</param>
    protected virtual KeyValue GenerateKeyValue(object newValue, string keyString)
    {
        KeyInfo keyInfo = new() {
            Key = keyString,
            Origin = DataServiceOriginName,
            MininumAccessLevel = System.DataAccessLevel.Public,
        };
        KeyValue keyValue = new() {
            KeyInfo = keyInfo,
        };

        keyValue.SetValue(newValue);

        return keyValue;
    }

    /// <summary>
    /// Sets the value given a member name into the data service.
    /// </summary>
    /// <param name="keyValue">The Key value that is going to be set on the data service.</param>
    protected virtual void OnSetDataServiceValue(KeyValue keyValue)
    {
        KeyValueList keyValueList = new();
        keyValueList.Values.Add(keyValue);
        DataClient.SetKeyValues(keyValueList);
    }

    /// <summary>
    /// Subscribes to a given value.
    /// </summary>
    /// <param name="keyInfo">The key info of the data service value that we are going to be subscribed to.</param>
    protected virtual void OnSubscribeDataServiceValue(KeyInfo keyInfo)
    {
        KeySubscriptionRequest subRequest = new();
        subRequest.SubscribeToKeys.Add(keyInfo);
        DataClient.SubscribeToKeys(subRequest);
    }

    /// <summary>
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="baseValue">Reference to the Field to be Set</param>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected virtual void OnSetValue<T>(ref T baseValue, T newValue, [CallerMemberName] string memberName = "") where T : struct
    {
        if (!baseValue.Equals(newValue))
        {
            if (DataClient != null)
            {
                KeyValue keyValue = GenerateKeyValue(newValue, memberName);
                OnSetDataServiceValue(keyValue);
            }

            baseValue = newValue;
            OnPropertyChanged(memberName);
        }
    }

    /// <summary>
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="baseValue">Reference to the Field to be Set</param>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected virtual void OnSetObject<T>(ref T baseValue, T newValue, [CallerMemberName] string memberName = "") where T : class
    {
        var result = baseValue?.Equals(newValue) ?? false;
        if (result == false)
        {
            baseValue = newValue;
            OnPropertyChanged(memberName);
        }
    }
}

/// <summary>
///  Interface for traversing ViewModels
/// </summary>
public interface IHasParent
{
    /// <summary>
    /// Parent View Model
    /// </summary>
    IHasParent Parent { get; }
}

/// <summary>
/// Extension of ViewModelBase which adds support for passing a Parent Model
/// allowing traversal up a Model Tree.
/// </summary>
/// <typeparam name="ParentType"></typeparam>
public class ChildViewModelBase<ParentType> : ViewModelBase, IHasParent where ParentType : IHasParent
{
    /// <summary>
    /// Reference to the Parent Model
    /// </summary>
    public T FindParent<T>() where T : class
    {
        IHasParent vm = ((IHasParent)this).Parent;
        while (vm is not null and not null)
        {
            if (vm.Parent.GetType() == typeof(T))
                return vm.Parent as T;

            vm = vm.Parent;
        }

        return null;
    }

    /// <summary>
    /// Reference to the Parent Model
    /// </summary>
    [JsonIgnore]
    public ParentType ParentViewModel { get; init; }

    IHasParent IHasParent.Parent => ParentViewModel;

    /// <summary>
    /// Constructor which takes a reference to the Parent Model.
    /// </summary>
    /// <param name="parentModel"></param>
    public ChildViewModelBase(ParentType parentModel)
    {
        ParentViewModel = parentModel;
    }
}