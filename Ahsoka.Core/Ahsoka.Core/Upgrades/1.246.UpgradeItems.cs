using Ahsoka.Installer;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;


internal class UpgradeItems_1_246 : UpgradeMethod
{
    public UpgradeItems_1_246() : base(packageUpgradeExtension, "1.247.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        string packageType = document["packageType"].GetValue<string>();
        if (packageType == "pvx1Encrypted")
            document["packageType"] = PackageType.opvEncrypted.ToString();
    }
}
