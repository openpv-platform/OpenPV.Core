using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace Ahsoka.Utility;



internal class UpgradeItemsPackage_3_0 : UpgradeMethod
{
    public UpgradeItemsPackage_3_0() : base(packageUpgradeExtension, "3.1.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        string filePath = context["FileName"].ToString();
        string directory = Path.GetDirectoryName(filePath);

        if (document["canCalibration"] != null)
        {
            string calibrationPath = document["canCalibration"].GetValue<string>();

            var serviceInfo = document["serviceInfo"];
            if (serviceInfo == null)
                serviceInfo = document["serviceInfo"] = new JsonObject();

            var runtimeConfig = serviceInfo["runtimeConfiguration"];
            if (runtimeConfig == null)
                runtimeConfig = document["runtimeConfiguration"] = new JsonObject();

            var services = runtimeConfig["services"];
            if (services == null)
                services = document["services"] = new JsonArray();

            foreach (var item in services.AsArray())
            {
                if (item["serviceName"]?.ToString() == "CanService")
                {
                    item["configurationFile"] = calibrationPath;
                    return;
                }
            }

            services.AsArray().Add(new JsonObject
            {
                ["configurationFile"] = calibrationPath,
                ["serviceName"] = "CanService",
                ["tcpListenAddress"] = "*",
                ["tcpConnectionAddress"] = "localhost",
                ["bleServiceUuidBase"] = "96730000-8f5d-4cef-8289-744c8faa1a66",
                ["dataChannel"] = "5007",
                ["notificationChannel"] = "6007"
            });


        }

    }
}