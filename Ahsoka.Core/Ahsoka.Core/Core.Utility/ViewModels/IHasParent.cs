namespace Ahsoka.Core.Utility;

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
