using Ahsoka.Utility;
using System;

namespace Ahsoka.DeveloperTools;

public class NewItemViewModel : ViewModelBase
{
    public string Title { get; set; }
    public string Label { get; set; }
    public string ItemText { get; set; }
    public string Hint { get; set; }

    public Action ItemAccepted { get; set; }
    public Action ItemCancelled { get; set; }

    internal void Accept()
    {
        ItemAccepted?.Invoke();
    }

    internal void Cancel()
    {
        ItemCancelled?.Invoke();
    }
}
