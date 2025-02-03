using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ahsoka.Core.Utility;

/// <summary>
/// Base View Model used in Ahsoka Applications which adds basic INotifyPropertyChanged support.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged, IHasParent
{
    IHasParent IHasParent.Parent => null;
    List<IViewModelBehavior> behaviors = new();

    /// <summary>
    /// Event Handler raised when a Property has Changed or OnPropertyChanged() is called.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// The base abstract class that should be used for any view models in the project.
    /// </summary>
    public ViewModelBase()
    {
        OnConstruct();
    }

    /// <summary>
    /// Constructor Extension Method 
    /// </summary>
    protected virtual void OnConstruct()
    {

    }

    /// <summary>
    /// Adds a Behavior Extension to the View Model
    /// </summary>
    /// <param name="behavior"></param>
    public void AddBehavior(IViewModelBehavior behavior)
    {
        behavior.InitBehavior(this);
        this.behaviors.Add(behavior);
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
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="baseValue">Reference to the Field to be Set</param>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="propertyId">Unique Id for this Property if available</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected virtual void OnSetValue<T>(ref T baseValue, T newValue, uint propertyId = 0, [CallerMemberName] string memberName = "") where T : struct
    {
        if (!baseValue.Equals(newValue))
        {
            RunSetBehaviors(baseValue, newValue, propertyId, memberName);

            baseValue = newValue;
            OnPropertyChanged(memberName);
        }
    }

    /// <summary>
    /// Run SetBehaviors for the Model 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="baseValue"></param>
    /// <param name="newValue"></param>
    /// <param name="propertyId">Unique Id for this Property if available</param>
    /// <param name="memberName"></param>
    protected void RunSetBehaviors<T>(T baseValue, T newValue, uint propertyId, string memberName)
    {
        foreach (var behavior in behaviors)
            behavior.OnSetValue(baseValue, newValue, propertyId, memberName);
    }


    /// <summary>
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="baseValue">Reference to the Field to be Set</param>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="propertyId">Unique Id for this Property if available</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected virtual void OnSetObject<T>(ref T baseValue, T newValue, uint propertyId = 0, [CallerMemberName] string memberName = "") where T : class
    {
        var result = baseValue?.Equals(newValue) ?? false;
        if (result == false)
        {
            RunSetBehaviors(baseValue, newValue, propertyId, memberName);

            baseValue = newValue;
            OnPropertyChanged(memberName);
        }
    }

    /// <summary>
    /// Returns a list of Properties
    /// </summary>
    /// <returns></returns>
    public SystemParameterData GetPropertyMap()
    {
        SystemParameterData parms = new();
        OnGetPropertyMap(parms);
        parms.AssignKeys(); // Use Namespace for Keys
        return parms;
    }

    /// <summary>
    /// Populate Property List - Base Uses Reflection
    /// </summary>
    /// <param name="parms"></param>
    protected virtual void OnGetPropertyMap(SystemParameterData parms)
    {
        parms.SystemName = this.GetType().Namespace;
        string name = this.GetType().Name;
        foreach (var item in this.GetType().GetProperties())
        {
            Type type = item.PropertyType;
            parms.Add(new ParameterData()
            {
                ObjectName = name,
                Name = item.Name,
                DefaultValue = item.GetValue(this),
                ValueType = ParameterData.GetValueType(item.PropertyType)
            });
        }
    }
}
