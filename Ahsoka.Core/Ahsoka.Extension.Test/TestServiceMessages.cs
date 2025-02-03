using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.Diagnostic;

internal class TestServiceMessages : AhsokaMessagesBase
{
    public TestServiceMessages()
    {

    }

    protected override void OnGetEvents(out string service, List<EventData> values, PackageInformation packageInfo)
    {
        base.OnGetEvents(out service, values, packageInfo);

        service = "MyService";
        values.Add(new EventData() { Name = "MyServiceEvent", Description = "Never Occurs" });
    }
}