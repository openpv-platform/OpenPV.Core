using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;


internal class UpgradeItemsPackage_2_7 : UpgradeMethod
{
    public UpgradeItemsPackage_2_7() : base(packageUpgradeExtension, "2.8.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        if (document["canCalibrationFiles"] != null && document["canCalibrationFiles"]["1"] != null)
        {
            string calibrationPath = document["canCalibrationFiles"]["1"].GetValue<string>();
            document["canCalibration"] = calibrationPath;
        }
    }
}