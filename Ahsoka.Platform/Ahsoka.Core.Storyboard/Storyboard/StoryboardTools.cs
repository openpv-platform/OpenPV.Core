using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Ahsoka.Storyboard;

internal class StoryboardTools
{
    public static void GenerateMessageClasses(string pathToSource, string pathToDestination, string nameSpace, string baseClass, ApplicationType type = ApplicationType.Dotnet)
    {
        string pathToEventData = pathToSource;
        XDocument document = null;
        if (Directory.Exists(pathToDestination))
            Directory.Delete(pathToDestination, true);
        Directory.CreateDirectory(pathToDestination);

        bool didGenenerate = false;

        StringBuilder outputFileData = new();
        StringBuilder metadataBuilder = new();
        StringBuilder metadataItems = new();
        var generatorExtensions = new GeneratorUtility("Ahsoka.Core.Storyboard", type);
        document = File.Exists(pathToEventData) ? XDocument.Load(pathToEventData) : new XDocument(new XElement("eventdefinitions"));

        if (document != null)
        {
            outputFileData.AppendLine("using Ahsoka.Storyboard;");
            outputFileData.AppendLine("using System;");
            outputFileData.AppendLine("using System.Collections.Generic;");
            outputFileData.AppendLine("using System.Linq;");
            generatorExtensions.ExtendHeader(outputFileData, nameSpace);

            outputFileData.AppendLine();
            outputFileData.AppendLine($"namespace {nameSpace};"); //need generic solution here
            outputFileData.AppendLine();

            foreach (var item in document.Descendants("event"))
            {        
                try
                {
                    string importName = item.Attribute("name").Value;
                    string importFormat = item.Attribute("format")?.Value ?? string.Empty;
                    string importType = item.Attribute("type").Value;                      

                    if (importType.Equals(StoryboardMessageDirection.In.GetMessageDirectionString()) || // Incoming
                        importType.Equals(StoryboardMessageDirection.Out.GetMessageDirectionString())) // Outgoing
                    {
                        StoryboardMessage storyboardMessage = new()
                        {
                            Name = importName,
                            Format = importFormat,
                            Direction = importType.Equals(StoryboardMessageDirection.Out.GetMessageDirectionString()) ? StoryboardMessageDirection.Out : StoryboardMessageDirection.In
                    };
                        GenerateClass(storyboardMessage, baseClass, outputFileData, metadataItems, metadataBuilder, generatorExtensions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Importing Item: {item} with error {ex.Message}");
                    throw new Exception(ex.Message);
                }
            }
            didGenenerate = true;
        }
        
        if (didGenenerate)
        {
            outputFileData.AppendLine();
            outputFileData.AppendLine(Properties.StoryboardResources.CSharpMetadataDictionary.Replace("%METADATA_BUILDER%", metadataBuilder.ToString()).Replace("%METADATA_BODY%", metadataItems.ToString()));
            File.WriteAllText(Path.Combine(pathToDestination, "GeneratedStoryboardObjects.cs"), outputFileData.ToString());
        }    
        else
            File.WriteAllText(Path.Combine(pathToDestination, "NoEventsToGenerate.cs"), "");
    }

    private static void GenerateClass(StoryboardMessage storyboardEvent, string baseClass, StringBuilder outputFileData, StringBuilder metadataItems, StringBuilder metadataBuilder, GeneratorUtility generatorExtensions = null)
    {
        if (String.IsNullOrEmpty(baseClass))
            baseClass = "StoryboardViewModelBase";

        string className = FormatName(storyboardEvent.Name);

        outputFileData.AppendLine($"public partial class {className} : {baseClass}");
        outputFileData.AppendLine("{");
        outputFileData.AppendLine($"\tpublic const string Name = \"{storyboardEvent.Name}\";");
        outputFileData.AppendLine($"\tpublic const string Format = \"{storyboardEvent.Format}\";");
        outputFileData.AppendLine($"\tpublic const StoryboardMessageDirection Direction = StoryboardMessageDirection.{storyboardEvent.Direction};");
        outputFileData.AppendLine();
        outputFileData.AppendLine("\t#region Auto Generated Properties");
        metadataItems.AppendLine($"\r\n\t\t// Decode Info for {className}");
        metadataItems.AppendLine($"\t\tmetadata.Add({className}.Name, new Dictionary<string, StoryboardPropertyInfo>()");
        metadataItems.AppendLine("\t\t{");

        string[] parts = storyboardEvent.Format.Split(' ');

        var position = 0; 
        for (int i = 0; i < parts.Length - 1; i += 2)
        {
            var name = parts[i + 1];
            var typePart = parts[i].Substring(0, 2);
            var arrayLength = Int32.Parse(parts[i].Substring(2));
            var typeLength = Int32.Parse(typePart.Substring(0,1));
            var sbType = arrayLength == 0 ? SBFieldType.String_1s : GetValueFromDescription<SBFieldType>(typePart);
            var type = StoryboardPropertyInfo.TypeTranslation.FirstOrDefault(x => x.Value == sbType).Key.Name;
            if (type == "Single") type = "float";
            bool isArray;
            if (arrayLength > 1)
                isArray = true;
            else
                isArray = false;
         
            WriteEventProperty(outputFileData, metadataItems, i, name, className, type, 
                sbType, position, typeLength, arrayLength, isArray, generatorExtensions);
            
            position += typeLength * arrayLength;
        }

        metadataItems.AppendLine("\t\t});");
        metadataBuilder.AppendLine($"\t\t\tcase {className}.Name:");
        metadataBuilder.AppendLine($"\t\t\t\treturn new {className}(data);");

        outputFileData.AppendLine("\t#endregion");
        outputFileData.AppendLine();
        outputFileData.AppendLine("\t#region Auto Generated Methods");
        outputFileData.AppendLine($"\tpublic override Dictionary<string, StoryboardPropertyInfo> GetMetadata() {{ return StoryboardMetadata.EventMetadata.GetMetadata(Name); }}");

        outputFileData.AppendLine($"\tpublic {className}() : base(Name, Format, Direction)");
        outputFileData.AppendLine($"\t{{");
        generatorExtensions.ExtendConstructor(outputFileData, className);
        outputFileData.AppendLine($"\t}}");

        outputFileData.AppendLine($"\tpublic {className}(StoryboardMessage message) : base(message)");
        outputFileData.AppendLine($"\t{{");
        generatorExtensions.ExtendConstructor(outputFileData, className);
        outputFileData.AppendLine($"\t}}");
        outputFileData.AppendLine("\t#endregion");
        outputFileData.AppendLine("}");
    }

    private static void WriteEventProperty(StringBuilder outputFileData, StringBuilder metadataItems, int index, string name, string className, string type,
        SBFieldType sbType, int bytePosition, int elementSize, int arrayLength, bool isArray, GeneratorUtility generatorExtensions = null)
    {
        string arrayString = isArray ? "[]" : "";

        outputFileData.AppendLine($"\tpublic {type}{arrayString} {FormatName(name)}");
        outputFileData.AppendLine("\t{");
        outputFileData.AppendLine($"\t\tget {{ return OnGetValue<{type}{arrayString}>(); }}");
        outputFileData.AppendLine("\t\tset { OnSetValue(value); ");
        generatorExtensions.ExtendSetter(outputFileData, name, type);
        outputFileData.AppendLine("\t\t}");
        outputFileData.AppendLine("\t}");

        metadataItems.AppendLine($"\t\t\t{{nameof({className}.{FormatName(name)}),new({index + 1}, SBFieldType.{sbType}, {bytePosition}, {elementSize}," +
            $" {arrayLength}, nameof({className}.{FormatName(name)}))}},");
    }

    public static string FormatName(string input)
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
