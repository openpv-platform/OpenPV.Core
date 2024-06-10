using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Utility;


internal class UpgradeItems_1_255 : UpgradeMethod
{
    public UpgradeItems_1_255() : base(canUpgradeExtension, "1.254.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        foreach (var item in document["nodes"].AsArray())
        {
            if (item["isSelf"] != null && item["isSelf"].AsValue().ToString() == "true")
                item["nodeType"] = "Self";
        }

        foreach (var item in document["messages"].AsArray())
        {
            item["userDefined"] = "true";
        }
    }
}
