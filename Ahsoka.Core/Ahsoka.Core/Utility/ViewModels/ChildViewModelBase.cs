using Newtonsoft.Json;

namespace Ahsoka.Utility;

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