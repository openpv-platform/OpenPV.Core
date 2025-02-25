using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;

internal static class UpgradeEngine
{
    

    static UpgradeEngine()
    {

    }

    internal static string ExecuteUpgrade(string fileName, string contents)
    {
        Dictionary<string, List<UpgradeMethod>> upgradeDictionary = LoadSubclasses();
        Dictionary<string, object> context;
        

        string fileNameLocal = Path.GetFileName(fileName);
        context = new Dictionary<string, object>();
        context["FileName"] = fileName;

        string returnValue = contents;

        List<UpgradeMethod> methods = new();

        foreach (var item in upgradeDictionary)
            if (fileNameLocal.ToLowerInvariant().EndsWith(item.Key.ToLowerInvariant()))
                methods = item.Value;

        if (methods.Count > 0)
        {

            JsonNode document = JsonNode.Parse(contents, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });
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

    private static Dictionary<string, List<UpgradeMethod>> LoadSubclasses()
    {
        var upgradeDictionary = new Dictionary<string, List<UpgradeMethod>>(StringComparer.OrdinalIgnoreCase);
        Type[] types = ClassLoader.GetSubclasses(typeof(UpgradeMethod));
        foreach (Type typeItem in types)
        {
            UpgradeMethod method = ClassLoader.CreateType(typeItem) as UpgradeMethod;
            if (!upgradeDictionary.ContainsKey(method.Extension))
                upgradeDictionary.Add(method.Extension, new List<UpgradeMethod>());

            upgradeDictionary[method.Extension].Add(method);
        }
        return upgradeDictionary;
    }
}

/// <summary>
/// Allows upgrades to json documents when loading
/// </summary>
public abstract class UpgradeMethod
{
    internal const string packageUpgradeExtension = "packageinfo.json";

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
    public UpgradeMethod(string path, string version)
    {
        Extension = path;
        Version = new Version(version);
    }

    /// <summary>
    /// Executes the upgrade on a raw Json document
    /// </summary>
    /// <param name="document"></param>
    /// <param name="context"></param>
    public abstract void UpgradeData(JsonNode document, Dictionary<string, object> context);

}
