using Ahsoka.Core.Utility;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ahsoka.Core;

/// <summary>
/// Proto Generator for Creating Open .proto files from Local Classes
/// </summary>
public static class ProtoGenerator
{
    /// <summary>
    /// Generates a file from a service with optional support for "other" classes.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="packageName"></param>
    /// <param name="messageIds"></param>
    /// <param name="otherTypes"></param>
    /// <returns></returns>
    public static byte[] GenerateProtoFile(this AhsokaMessagesBase messages, string packageName, Type messageIds, params Type[] otherTypes)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(Properties.Resources.ProtoTemplate.Replace("%PACKAGE_NAME%", packageName));

        if (messageIds != null)
            CreateMessageList(sb, "    ", messageIds);

        List<Type> objectsToAdd = new();
        List<Type> enumerationsToAdd = new();

        if (messages != null)
            foreach (var info in messages.Info)
            {
                if (info.Value.TransportId.EnumToInt() >= 0)
                {
                    // Add Types and Children
                    HandleType(objectsToAdd, enumerationsToAdd, info.Value.RequestType);
                    HandleType(objectsToAdd, enumerationsToAdd, info.Value.ReplyType);
                }
            }

        foreach (var item in otherTypes)
            if (item.IsEnum)
                enumerationsToAdd.Add(item);
            else
                HandleType(objectsToAdd, enumerationsToAdd, item);

        foreach (var item in enumerationsToAdd)
            CreateProtoEnum(sb, item);

        foreach (var item in objectsToAdd)
            CreateProtoMessage(sb, item);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void CreateProtoMessage(StringBuilder sb, Type item)
    {
        sb.AppendLine($"message {item.Name}\r\n{{");
        GenerateProtoMessageItems(sb, item);
        sb.AppendLine("}\r\n");
    }

    private static void GenerateProtoMessageItems(StringBuilder sb, Type item)
    {
        // Recurse Members with ProtoContract
        foreach (var info in item.GetProperties())
        {
            // Its a Proto Member..add it
            var protoMember = info.GetCustomAttribute<ProtoMemberAttribute>();
            if (protoMember != null)
            {
                string name = String.IsNullOrEmpty(protoMember.Name) ? info.Name.ToSnakeCase() : protoMember.Name;
                sb.AppendLine($"   {GetMemberType(info, item)} {name} = {(int)protoMember.Tag};");
            }
        }
    }

    private static string GetMemberType(PropertyInfo info, Type item)
    {
        if (info.PropertyType.IsArray)
        {
            if (info.PropertyType == typeof(byte[]))
                return "bytes";
            else
                return $"repeated {info.PropertyType.Name.ToLower().Replace("[]", "")}";
        }
        else if (info.PropertyType.IsGenericType)
        {
            var count = info.PropertyType.GetGenericArguments().Length;
            if (count == 1)
            {
                var listType = info.PropertyType.GenericTypeArguments.FirstOrDefault();
                return $"repeated {GetTypeName(listType)}";
            }
            else if (count == 2)
            {
                var indexType = info.PropertyType.GenericTypeArguments[0];
                var dataType = info.PropertyType.GenericTypeArguments[1];
                return $"map<{GetTypeName(indexType)},{GetTypeName(dataType)}>";
            }
            else
                throw new ApplicationException("Unsupported Genertic with Proto Member Property");
        }
        else
            return GetTypeName(info.PropertyType);
    }

    private static string GetTypeName(Type propertyType)
    {
        if (propertyType == typeof(byte[]))
            return "bytes";
        else if ((propertyType.IsEnum || propertyType.IsClass) && propertyType != typeof(string))
            return propertyType.Name;
        else
            return propertyType.Name.ToLower().Replace("boolean", "bool").Replace("single", "float");
    }

    private static void HandleType(List<Type> objectsToAdd, List<Type> enumerationsToAdd, Type typeToProcess)
    {
        var protoAttribute = typeToProcess.GetCustomAttribute<ProtoContractAttribute>();
        if (protoAttribute != null)
        {
            // Is Proto...Add it.
            if (!objectsToAdd.Contains(typeToProcess))
            {
                objectsToAdd.Add(typeToProcess);

                // Recurse Members with ProtoContract
                foreach (var info in typeToProcess.GetProperties())
                {
                    // Its a Proto Member..add it
                    var protoMember = info.GetCustomAttribute<ProtoMemberAttribute>();
                    if (protoMember != null)
                    {
                        if (info.PropertyType.IsGenericType)
                        {
                            foreach (var item in info.PropertyType.GenericTypeArguments)
                            {
                                if (item.IsEnum)
                                    if (!enumerationsToAdd.Contains(info.PropertyType))
                                        enumerationsToAdd.Add(info.PropertyType);
                                if (item.IsClass)
                                    HandleType(objectsToAdd, enumerationsToAdd, item);
                            }
                        }
                        else if (info.PropertyType.IsClass)
                            HandleType(objectsToAdd, enumerationsToAdd, info.PropertyType);
                        else if (info.PropertyType.IsEnum)
                            if (!enumerationsToAdd.Contains(info.PropertyType))
                                enumerationsToAdd.Add(info.PropertyType);
                    }
                }
            }
        }
    }

    private static void CreateMessageList(StringBuilder sb, string prefix, Type enumType)
    {
        var enumerationName = enumType.FullName;
        enumerationName = enumerationName.Replace(enumType.Namespace + ".", "").Replace("+Ids", "");

        sb.AppendLine($"message {enumerationName}\r\n{{\r\n{prefix}enum Ids\r\n    {{");
        GenerateProtoEnumItems(sb, $"{prefix}    ", enumType);
        sb.AppendLine("    }\r\n}\r\n");
    }

    private static void CreateProtoEnum(StringBuilder sb, Type enumType)
    {
        sb.AppendLine($"enum {enumType.Name}\r\n{{");
        GenerateProtoEnumItems(sb, $"    ", enumType);
        sb.AppendLine("}\r\n");
    }

    private static void GenerateProtoEnumItems(StringBuilder sb, string prefix, Type messageIds)
    {
        foreach (var item in Enum.GetValues(messageIds))
        {
            sb.AppendLine($"{prefix}{item.ToString().ToSnakeCase().ToUpperInvariant()} = {(int)item};");
        }
    }

    internal static void GenerateProtoFile(string serviceName, string outputFileName)
    {
        if (serviceName == "Services")
        {
            var data = GenerateProtoFile(null, "AhsokaServiceFramework", null,
                typeof(AhsokaMessageHeader),
                typeof(EmptyNotification),
                typeof(InvalidResponseNotification),
                typeof(ClientRegistration),
                typeof(CoreTransportIds),
                typeof(ServiceErrorThrownResponse));
            File.WriteAllBytes(outputFileName, data);
            return;
        }
        else if (serviceName == "Configuration")
        {
            var data = GenerateProtoFile(null, "AhsokaCore", null, typeof(RuntimeConfiguration));
            File.WriteAllBytes(outputFileName, data);
            return;
        }

        // Find Services
        var types = ClassLoader.GetItemsWithAttribute<AhsokaServiceAttribute>();

        var type = types.FirstOrDefault(x => x.Key.ServiceName == serviceName);
        if (type.Value == null)
            throw new ArgumentException($"Could not find service to match {serviceName}");
        else
        {
            IAhsokaEndPoint service = ClassLoader.CreateType(type.Value) as IAhsokaEndPoint;
            var data = service.ServiceMessageInfo.GetProtoMessageFile();
            File.WriteAllBytes(outputFileName, data);
        }

    }
}