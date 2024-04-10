using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ahsoka.Storyboard;

internal class StoryboardTools
{
    public static void GenerateMessageClasses(string pathToSource, string pathToDestination, string nameSpace, string baseClass)
    {
        string pathToEventData = pathToSource;
        XDocument document = null;
        if (Directory.Exists(pathToDestination))
            Directory.Delete(pathToDestination, true);
        Directory.CreateDirectory(pathToDestination);

        bool didGenenerate = false;

        document = File.Exists(pathToEventData) ? XDocument.Load(pathToEventData) : new XDocument(new XElement("eventdefinitions"));

        if (document != null)
        {
            foreach (var item in document.Descendants("event"))
            {
                try
                {
                    string importName = item.Attribute("name").Value;
                    string importType = item.Attribute("type").Value;
                    string importFormat = item.Attribute("format")?.Value ?? string.Empty;

                    if (importType.Equals(StoryboardMessageDirection.In.GetMessageDirectionString()) || // Incoming
                        importType.Equals(StoryboardMessageDirection.Out.GetMessageDirectionString())) // Outgoing
                    {
                        StoryboardMessage storyboardMessage = new(importName, importFormat, importType, null);
                        GenerateClass(storyboardMessage, pathToDestination, nameSpace, baseClass);
                        didGenenerate = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Importing Item: {item} with error {ex.Message}");
                    throw new Exception(ex.Message);
                }
            }
        }

        if (!didGenenerate)
            File.WriteAllText(Path.Combine(pathToDestination,"NoEventsToGenerate.cs"), "");
    }

    private static void GenerateClass(StoryboardMessage storyboardEvent, string pathToDestination, string nameSpace, string baseClass)
    {
        if (String.IsNullOrEmpty(baseClass))
            baseClass = "StoryboardViewModel";

        string className = FormatName(storyboardEvent.EventAttribute.EventName);
        string pathToClass = Path.Combine(pathToDestination, className) + ".cs";

        using StreamWriter sw = new(pathToClass);
        sw.WriteLine("using Ahsoka.Storyboard;");
        sw.WriteLine("using System;");
        sw.WriteLine();
        sw.WriteLine($"namespace {nameSpace};"); //need generic solution here
        sw.WriteLine();
        sw.WriteLine($"[SBMessageEventName(\"{storyboardEvent.EventAttribute.EventName}\", \"{storyboardEvent.EventAttribute.EventDirection}\")]");
        sw.WriteLine($"public partial class {className} : {baseClass}");
        sw.WriteLine("{");
        string[] parts = storyboardEvent.Format.Split(' ');

        StringWriter fieldWriter = new();
        StringWriter propertyWriter = new();

        for (int i = 0; i < parts.Length - 1; i += 2)
        {
            var name = parts[i + 1];
            var typePart = parts[i].Substring(0, 2);
            var arrayLength = Int32.Parse(parts[i].Substring(2));
            var type = arrayLength == 0 ? "string" :
                StoryboardMessageSerializer.TypeTranslation.FirstOrDefault(x => x.Value == GetValueFromDescription<SBFieldType>(typePart)).Key.Name;
            if (type == "Single") type = "float";
            bool isArray;
            if (arrayLength > 1)
                isArray = true;
            else
                isArray = false;

            WriteEventProperty(fieldWriter, propertyWriter, i, name, type, arrayLength, isArray);
        }

        sw.WriteLine("\t#region Auto Generated Fields");
        sw.Write(fieldWriter.ToString());
        sw.WriteLine("\t#endregion");
        sw.WriteLine();
        sw.WriteLine("\t#region Auto Generated Properties");
        sw.WriteLine(propertyWriter.ToString());
        sw.WriteLine("\t#endregion");
        sw.WriteLine("}");
    }

    private static string FormatName(string input)
    {
        string name = input;
        while (name.IndexOf("_") > 0)
        {
            var index = name.IndexOf("_");
            if (index == 0)
                name = name.Substring(1);
            else if (index != name.Length - 1)
                name = name.Substring(0, index) + name.Substring(index + 1, 1).ToUpper() + name.Substring(index + 2);
            else
                name = name.Substring(0, index);
        }
        name = name.Substring(0, 1).ToUpper() + name.Substring(1);
        return name;
    }

    private static void WriteEventProperty(StringWriter fieldWriter,
        StringWriter propertyWriter, int index, string name, string type, int arrayLength, bool isArray)
    {
        string arrayString = isArray ? "[]" : "";
        fieldWriter.WriteLine($"\tprivate {type}{arrayString} _{name};");

        string getValue = $"get {{ return _{name};}}";
        string setValue = type == "string" || isArray ?
            $"set {{ OnSetObject<{type}{arrayString}>(ref _{name}, value);}}" :
            $"set {{ OnSetValue<{type}{arrayString}>(ref _{name}, value);}}";

        propertyWriter.Write($"\r\n\t[SBField({index + 1}, {arrayLength}, \"{name}\")]");
        propertyWriter.WriteLine($"\r\n\tpublic {type}{arrayString} {FormatName(name)} \r\n\t{{\r\n\t\t{getValue}\r\n\t\t{setValue} \r\n\t}}");
    }

    public static T GetValueFromDescription<T>(string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field,
            typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException("Not found.", nameof(description));
        // Or return default(T);
    }
}
