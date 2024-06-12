using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Ahsoka.Utility;

internal static class UpgradeEngine
{
    private static readonly Dictionary<string, List<UpgradeMethod>> upgradeDictionary = new();
    private static Dictionary<string, object> context;

    static UpgradeEngine()
    {
        Type[] types = ClassLoader.GetSubclasses(typeof(UpgradeMethod));
        foreach (Type typeItem in types)
        {
            UpgradeMethod method = ClassLoader.CreateType(typeItem) as UpgradeMethod;
            if (!upgradeDictionary.ContainsKey(method.Extension))
                upgradeDictionary.Add(method.Extension, new List<UpgradeMethod>());

            upgradeDictionary[method.Extension.ToLower()].Add(method);
        }
    }

    internal static string ExecuteUpgrade(string fileName, string contents)
    {
        string fileNameLocal = Path.GetFileName(fileName);
        context = new Dictionary<string, object>();
        context["FileName"] = fileName;

        string returnValue = contents;

        List<UpgradeMethod> methods = new();

        foreach (var item in upgradeDictionary)
            if (fileNameLocal.ToLowerInvariant().EndsWith(item.Key))
                methods = item.Value;

        if (methods.Count > 0)
        {

            JsonNode document = JsonNode.Parse(contents);
            var version = "0.0.0";
            if (document["version"] != null)
                version = document["version"].GetValue<string>();

            Version startVersion = new(version);

            foreach (UpgradeMethod method in from item in methods orderby item.Version select item)
                if (method.Version > startVersion)
                    method.UpgradeData(document, context);

            returnValue = document.ToJsonString();
        }

        return returnValue;
    }

    internal static string ExecuteUpgrade(string path)
    {
        string data = File.ReadAllText(path);
        return ExecuteUpgrade(path, data);
    }
}

internal abstract class UpgradeMethod
{
    protected const string packageUpgradeExtension = "packageinfo.json";
    protected const string canUpgradeExtension = "cancalibration.json";

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    /// <value>The path.</value>
    public string Extension { get; private set; }

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public Version Version { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpgradeMethod"/> class.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="version">The version.</param>
    protected UpgradeMethod(string path, string version)
    {
        Extension = path;
        Version = new Version(version);
    }

    public abstract void UpgradeData(JsonNode document, Dictionary<string, object> context);

}
