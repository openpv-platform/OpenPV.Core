using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;



internal class UpgradeItemsPackage_4_0 : UpgradeMethod
{
    public UpgradeItemsPackage_4_0() : base(packageUpgradeExtension, "4.1.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        if (document["screenConfiguration"] == null)
        {
            string logoPath = string.Empty;
            string orientation = "DefaultOrientation";
            if (document["startupLogoPath"] != null)
                logoPath = document["startupLogoPath"].ToString();

            if (document["systemOrientation"] != null)
                orientation = document["systemOrientation"].ToString();


            var displayConfig = document["screenConfiguration"] = new JsonObject();
            displayConfig["bootLogoPath"] = logoPath;
            displayConfig["orientation"] = orientation;
        }

    }
}

