using System;

namespace Ahsoka.Application.Storyboard;
public partial class DEVFrontendToBackend
{
    public override void OnUpdateModel(string target)
    {
        Console.WriteLine("Backend Received: DEVFrontendToBackend");
        Console.WriteLine("\tVarString: {0}", VarString);
    }
}
