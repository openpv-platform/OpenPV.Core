using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;


internal class UpgradeNeworkPackage_4_0 : UpgradeMethod
{
    public UpgradeNeworkPackage_4_0() : base(packageUpgradeExtension, "4.1.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        var serviceInfo = document["serviceInfo"];
        if (serviceInfo != null)
        {
            var config = serviceInfo["runtimeConfiguration"];
            if (config != null)
            {
                var items = config["services"].AsArray();
                foreach (var item in items)
                {
                    var serviceName = item["serviceName"];
                    if (serviceName != null && serviceName.AsValue().ToString() == "NetworkService")
                    {
                        item["serviceName"] = "WiFiClientService";
                    }
                }
            }
        }

    }
}


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


        var screenConfig = document["screenConfiguration"];
        {
            var displayConfig = screenConfig["orientation"];
            if (displayConfig.AsValue().ToString() == "DefaultOrientation")
                screenConfig["orientation"] = int.Parse("0");
            else
                screenConfig["orientation"] = int.Parse(displayConfig.AsValue().ToString().Replace("rotate", ""));

        }

    }
}

