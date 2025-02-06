namespace Ahsoka.Core.Utility;

/// <summary>
///  Interface for traversing ViewModels
/// </summary>
public interface IViewModelBehavior
{
    /// <summary>
    /// Initialize the Behavior for the View Model
    /// </summary>
    /// <param name="viewModelBase"></param>
    void InitBehavior(ViewModelBase viewModelBase);

    /// <summary>
    /// Notificatin from View Model that Data Changed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="baseValue"></param>
    /// <param name="newValue"></param>
    /// <param name="propertyId"></param>
    /// <param name="memberName"></param>
    void OnSetValue<T>(T baseValue, T newValue, uint propertyId, string memberName);
}
